
using System.Reflection;
using System.Collections.Generic;
using System;
using UnityEngine;


/*
    The ActionDispatcher takes a dynamic object with an 'action' property and 
    maps this to a method.  Matching is performed using the parameter names. 
    In the case of method overloading, the best match is returned based on the
    number of matched named parameters.  For a method to qualify for dispatching
    it must be public and have a return type of void.  The following method 
    definitions are permitted:

    public void MoveAhead()
    public void MoveAhead(string direction)
    public void MoveAhead(ServerAction action)
    public void MoveAhead(float moveMagnitude, rotation=0.0f)


    Creating the following overloaded set of functions will not work as expected:

    public void Teleport(int x, int y)
    public void Teleport(int x, short y)

    as well the following scenario should also be avoided:

    public void ObjectVisible(bool foo, int x, int y)
    public void ObjectVisible(int x, int y, bool foo)


    The reason for the aforementioned restrictions is twofold, we pass the arguments to
    Unity serialized using json.  This restricts the types that can be passed to
    C# as well even if we serialized using a different format, Python does not 
    have all the same primitives, such as 'short'.  Second, we allow actions
    to be invoked from the Python side using keyword args which doesn't preserve order.

    These restrictions shouldn't present themselves as creating duplicate public
    actions with different orders, but identically named parameters would lead to
    confusion should be avoided.
*/
public static class ActionDispatcher {
    private static Dictionary<Type, Dictionary<string, List<MethodInfo>>> methodDispatchTable = new Dictionary<Type, Dictionary<string, List<MethodInfo>>>();


    private static MethodInfo getDispatchMethod(System.Object target, dynamic serverCommand) {
        Type t = target.GetType();
        if (!methodDispatchTable.ContainsKey(t)) {
            System.Reflection.MethodInfo[] allMethods = target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            List<Type> hierarchy = new List<Type>();
            // not completely generic
            Type ht = t;
            while (ht != typeof(object)) {
                hierarchy.Add(ht);
                ht = ht.BaseType;
            }

            Dictionary<string, List<MethodInfo>> methodDispatch = new Dictionary<string, List<MethodInfo>>();
            foreach(MethodInfo mi in allMethods) {
                if (mi.ReturnType != typeof(void)) {
                    // We only allow dispatching to public void methods
                    continue;
                }
                if (methodDispatch.ContainsKey(mi.Name)) {
                    List<MethodInfo> methods = methodDispatch[mi.Name];
                    bool replaced = false;
                    // we do this to handle the case of a child method hiding a method in the parent
                    // in which case both methods will show up.  This happens if virtual, override or new
                    // are not used
                    for(int j = 0; j < methods.Count && !replaced; j++) {
                        bool signatureMatch = true;
                        ParameterInfo[] sourceParams = mi.GetParameters();
                        ParameterInfo[] targetParams = methods[j].GetParameters();
                        if (targetParams.Length == sourceParams.Length) {
                            for(int k = 0; k < sourceParams.Length; k++) {
                                if (sourceParams[k].ParameterType != targetParams[k].ParameterType) {
                                    signatureMatch = false;
                                }

                            }
                        } else {
                            signatureMatch = false;
                        }

                        // if the method is more specific and the parameters match
                        // we will dispatch to this method instead of the base type
                        if (hierarchy.IndexOf(mi.DeclaringType) < hierarchy.IndexOf(methods[j].DeclaringType) && signatureMatch) { 
                            methods[j] = mi;
                        }
                    }
                    if (!replaced) {
                        // we sort the list of methods so that we evaluate
                        // methods with fewer and possible no params first
                        // and then match methods with greater params
                        methods.Add(mi);
                        MethodParamComparer mc = new MethodParamComparer();
                        methods.Sort(mc);
                    }
                } else {
                    methodDispatch[mi.Name] = new List<MethodInfo>();
                    methodDispatch[mi.Name].Add(mi);
                }
            }

            methodDispatchTable[t] = methodDispatch;
        }

        List<MethodInfo> actionMethods = null;
        methodDispatchTable[t].TryGetValue(serverCommand.action.ToString(), out actionMethods);
        MethodInfo matchedMethod = null;
        int bestMatchCount = -1; // we do this so that 

        if (actionMethods != null) {
            foreach (var method in actionMethods) {
                int matchCount = 0;
                ParameterInfo[] mParams = method.GetParameters();
                // default to ServerAction method
                // this is also necessary, to allow Initialize to be
                // called in the AgentManager and an Agent, since we
                // pass a ServerAction through
                if (matchedMethod == null && mParams.Length == 1 && mParams[0].ParameterType == typeof(ServerAction)) {
                    matchedMethod = method;
                } else {
                    HashSet<string> actionParams = new HashSet<string>();
                    foreach(var p in serverCommand.Properties()) {
                        actionParams.Add(p.Name);
                    }

                    foreach(var p in method.GetParameters()) {
                        if (actionParams.Contains(p.Name)) {
                            matchCount++;
                        }
                    }
                }

                // preference is given to the method that matches all parameters for a method
                // even if another method has the same matchCount (but has more parameters)
                if (matchCount > bestMatchCount) {
                    bestMatchCount = matchCount;
                    matchedMethod = method;
                }
            }

        }

        return matchedMethod;
    }

    public static void Dispatch(System.Object target, dynamic serverCommand) {

        MethodInfo method = getDispatchMethod(target, serverCommand);

        if (method == null) {
            throw new InvalidActionException();
        }

        List<string> missingArguments = null; 
        System.Reflection.ParameterInfo[] methodParams = method.GetParameters();
        object[] arguments = new object[methodParams.Length];
        if (methodParams.Length == 1 && methodParams[0].ParameterType == typeof(ServerAction)) {
            arguments[0] = serverCommand.ToObject(methodParams[0].ParameterType);
        }  else {
            for(int i = 0; i < methodParams.Length; i++) {
                System.Reflection.ParameterInfo pi = methodParams[i];
                // allows for passing in a ServerAtion as a dynamic to ProcessControlCommand
                if (serverCommand.GetType() == pi.ParameterType){
                    arguments[i] = serverCommand;
                } else if (serverCommand.ContainsKey(pi.Name)) {
                    arguments[i] = serverCommand[pi.Name].ToObject(pi.ParameterType);
                } else {
                    if (!pi.HasDefaultValue)  {
                        if (missingArguments == null) {
                            missingArguments = new List<string>();
                        }
                        missingArguments.Add(pi.Name);
                    }
                    arguments[i] = Type.Missing;
                }
            }
        }
        if (missingArguments != null) {
            throw new MissingArgumentsActionException(missingArguments);
        }

        method.Invoke(target, arguments);
    }



}

public class MethodParamComparer: IComparer<MethodInfo> {


    public int Compare(MethodInfo a, MethodInfo b) {
        int requiredParamCountA = requiredParamCount(a);
        int requiredParamCountB = requiredParamCount(b);
        int result = requiredParamCountA.CompareTo(requiredParamCountB);
        if (result == 0) {
            result = paramCount(a).CompareTo(paramCount(b));
        }
        return result;
    }

    private static int paramCount(MethodInfo method) {
        return method.GetParameters().Length;
    }

    private static int requiredParamCount(MethodInfo method) {
        int count = 0;
        foreach(var p  in method.GetParameters()) {
            if (!p.HasDefaultValue) {
                count++;
            }
        }

        return count;
    }

}


[Serializable]
public class InvalidActionException : Exception { }

[Serializable]
public class MissingArgumentsActionException : Exception {
    public List<string> ArgumentNames;
    public MissingArgumentsActionException(List<string> argumentNames) {
        this.ArgumentNames = argumentNames;
    }
}