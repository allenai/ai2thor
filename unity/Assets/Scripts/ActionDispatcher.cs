
using System.Reflection;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json.Linq;


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
    to be invoked from the Python side using keyword args which don't preserve order.

    These restrictions shouldn't present themselves as creating duplicate public
    actions with different orders, but identically named parameters would lead to
    confusion and should be avoided.


    Ambiguous Actions

    The following method signatures are not permitted since they can create ambiguity as 
    to which method to dispatch to:

    case 1:
        methods:
            public void Teleport(ServerAction)
            public void Teleport(float x, float y, float z)
        reason:
            Mixing ServerAction methods and non-server action methods creates ambiguity
            if one param is omitted from the (x,y,z) method.  You could default back to 
            ServerAction method, but you can't be sure that is what the user intended.

    case 2:
        methods:
            public void LookUp(float degrees)
            public void LookUp(float degrees, bool forceThing=false)
        reason:
            This is valid C# and if you have code LookUp(0.0f) it will bind to the first
            method, though there is still ambiguity since a user could have wanted to dispatch
            to the second method which has an optional forceThing parameter. i.e. if this
            case is not prevented, the optional value in the second method becomes required.

    case 3:
        methods:
            BaseClass
                public virtual void LookUp(float degrees)
            SubClass
                public void LookUp(float degrees, bool forceThing=false)
        reason:
            This is similar to case #2, but the methods are spread across two classes.
            The way to resolve this is to define the following methods:
            Subclass
                public override void LookUp(float degrees) 
                public void LookUp(float degrees, bool forceThing)
            
            Within the override method, the author can dispatch to LookUp with a default 
            value for forceThing.

*/
public static class ActionDispatcher {
    private static Dictionary<Type, Dictionary<string, List<MethodInfo>>> allMethodDispatchTable = new Dictionary<Type, Dictionary<string, List<MethodInfo>>>();
    private static Dictionary<Type, MethodInfo[]> methodCache = new Dictionary<Type, MethodInfo[]>();

    // look through all methods on a target type and attempt to get the MethodInfo
    // any ambiguous method will throw an exception.  This is used during testing.
    public static List<string> FindAmbiguousActions(Type targetType) {
        List<string> actions = new List<string>();
        System.Reflection.MethodInfo[] allMethods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        HashSet<string> methodNames = new HashSet<string>();
        foreach (var method in allMethods) {
            methodNames.Add(method.Name);
        }
        foreach (var methodName in methodNames) {
            try {
                Dictionary<string, object> act = new Dictionary<string, object>();
                act["action"] = methodName;
                DynamicServerAction dynamicServerAction = new DynamicServerAction(act);
                MethodInfo m = getDispatchMethod(targetType, dynamicServerAction);
            } catch (AmbiguousActionException) {
                actions.Add(methodName);
            }
        }
        return actions;
    }

    private static MethodInfo[] getMethods(Type targetType) {
        if (!methodCache.ContainsKey(targetType)) {
            var methods = new List<MethodInfo>();
            foreach (MethodInfo mi in targetType.GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
                if (mi.ReturnType == typeof(void)) {
                    methods.Add(mi);
                }
            }

            methodCache[targetType] = methods.ToArray();
        }
        return methodCache[targetType];
    }

    // Find public/void methods that have matching Method names and identical parameter names,
    // but either different parameter order or parameter types which make dispatching
    // ambiguous. This method is used during testing to find conflicts.
    public static Dictionary<string, List<string>> FindMethodVariableNameConflicts(Type targetType) {
        MethodInfo[] allMethods = getMethods(targetType);
        Dictionary<string, List<string>> methodConflicts = new Dictionary<string, List<string>>();

        for (int i = 0; i < allMethods.Length - 1; i++) {
            MethodInfo methodOut = allMethods[i];
            HashSet<string> paramSet = new HashSet<string>();
            ParameterInfo[] methodOutParams = methodOut.GetParameters();
            foreach (var p in methodOutParams) {
                paramSet.Add(p.Name);
            }
            for (int j = i + 1; j < allMethods.Length; j++) {
                MethodInfo methodIn = allMethods[j];
                ParameterInfo[] methodInParams = allMethods[j].GetParameters();
                if (methodIn.Name == methodOut.Name && methodOutParams.Length == methodInParams.Length) {
                    bool allVariableNamesMatch = true;
                    bool allParamsMatch = true;
                    for (int k = 0; k < methodInParams.Length; k++) {
                        var mpIn = methodInParams[k];
                        var mpOut = methodOutParams[k];
                        // we assume the method is overriding if everything matches
                        if (mpIn.Name != mpOut.Name || mpOut.ParameterType != mpIn.ParameterType) {
                            allParamsMatch = false;
                        }
                        if (!paramSet.Contains(mpIn.Name)) {
                            allVariableNamesMatch = false;
                            break;
                        }
                    }
                    if (allVariableNamesMatch && !allParamsMatch) {
                        methodConflicts[methodOut.Name] = new List<string>(paramSet);
                    }
                }
            }
        }
        return methodConflicts;
    }

    private static Dictionary<string, List<MethodInfo>> getMethodDispatchTable(Type targetType) {
        if (!allMethodDispatchTable.ContainsKey(targetType)) {
            allMethodDispatchTable[targetType] = new Dictionary<string, List<MethodInfo>>();
        }

        return allMethodDispatchTable[targetType];
    }

    private static List<MethodInfo> getCandidateMethods(Type targetType, string action) {
        Dictionary<string, List<MethodInfo>> methodDispatchTable = getMethodDispatchTable(targetType);
        if (!methodDispatchTable.ContainsKey(action)) {
            List<MethodInfo> methods = new List<MethodInfo>();

            List<Type> hierarchy = new List<Type>();

            // not completely generic
            Type ht = targetType;
            while (ht != typeof(object)) {
                hierarchy.Add(ht);
                ht = ht.BaseType;
            }

            foreach (MethodInfo mi in getMethods(targetType)) {
                if (mi.Name != action) {
                    continue;
                }
                bool replaced = false;

                // we do this to handle the case of a child method hiding a method in the parent
                // in which case both methods will show up.  This happens if virtual, override or new
                // are not used
                ParameterInfo[] sourceParams = mi.GetParameters();

                for (int j = 0; j < methods.Count && !replaced; j++) {
                    bool signatureMatch = true;
                    ParameterInfo[] targetParams = methods[j].GetParameters();
                    int minCommon = Math.Min(sourceParams.Length, targetParams.Length);
                    for (int k = 0; k < minCommon; k++) {
                        if (sourceParams[k].ParameterType != targetParams[k].ParameterType) {
                            signatureMatch = false;
                            break;
                        }
                    }

                    if (sourceParams.Length > targetParams.Length && !sourceParams[minCommon].HasDefaultValue) {
                        signatureMatch = false;
                    } else if (targetParams.Length > sourceParams.Length && !targetParams[minCommon].HasDefaultValue) {
                        signatureMatch = false;
                    }

                    // if the method is more specific and the parameters match
                    // we will dispatch to this method instead of the base type
                    if (signatureMatch) {
                        // this happens if one method has a trailing optional value and all 
                        // other parameter types match
                        if (targetParams.Length != sourceParams.Length) {
                            throw new AmbiguousActionException("Signature match found in the same class");
                        }

                        replaced = true;
                        if (hierarchy.IndexOf(mi.DeclaringType) < hierarchy.IndexOf(methods[j].DeclaringType)) {
                            methods[j] = mi;
                        }
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
            }

            // must perform assignment here, since an exception could be thrown during
            // the creation of the list. if the list were assigned directly to the allMethodDispatchTable
            // its possible that it would be only partially populated
            methodDispatchTable[action] = methods;
        }

        return methodDispatchTable[action];
    }

    public static MethodInfo getDispatchMethod(Type targetType, DynamicServerAction dynamicServerAction) {

        List<MethodInfo> actionMethods = getCandidateMethods(targetType, dynamicServerAction.action);
        MethodInfo matchedMethod = null;
        int bestMatchCount = -1; // we do this so that 

        // This is where the the actual matching occurs.  The matching is done strictly based on
        // variable names.  In the future, this could be modified to include type information from
        // the inbound JSON object by mapping JSON types to csharp primitive types 
        // (i.e. number -> [short, float, int], bool -> bool, string -> string, dict -> object, list -> list)
        foreach (var method in actionMethods) {
            int matchCount = 0;
            ParameterInfo[] mParams = method.GetParameters();

            // mixing a ServerAction action with non-server action creates an ambiguous situation
            // if one parameter is missing from the overloaded method its not clear whether the caller
            // intended to call the ServerAction action or was simply missing on of the parameters for the overloaded
            // variant
            if (actionMethods.Count > 1 && mParams.Length == 1 && mParams[0].ParameterType == typeof(ServerAction)) {
                throw new AmbiguousActionException("Mixing a ServerAction method with overloaded methods is not permitted");
            }

            // default to ServerAction method
            // this is also necessary, to allow Initialize to be
            // called in the AgentManager and an Agent, since we
            // pass a ServerAction through
            if (matchedMethod == null && mParams.Length == 1 && mParams[0].ParameterType == typeof(ServerAction)) {
                matchedMethod = method;
            } else {
                foreach (var p in method.GetParameters()) {
                    if (dynamicServerAction.ContainsKey(p.Name)) {
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

        return matchedMethod;
    }

    public static IEnumerable<MethodInfo> getMatchingMethodOverwrites(Type targetType, DynamicServerAction dynamicServerAction) {
        return getCandidateMethods(targetType, dynamicServerAction.action)
            .Select(
                method => (
                    method,
                    count: method
                        .GetParameters().Count(param => dynamicServerAction.ContainsKey(param.Name))
                )
            )
            .OrderByDescending(tuple => tuple.count)
            .Select((tuple) => tuple.method);
    }

    public static void Dispatch<T>(T target, DynamicServerAction dynamicServerAction) where T : MonoBehaviour {
        MethodInfo method = getDispatchMethod(target.GetType(), dynamicServerAction);

        if (method == null) {
            throw new InvalidActionException();
        }

        List<string> missingArguments = null;
        System.Reflection.ParameterInfo[] methodParams = method.GetParameters();
        object[] arguments = new object[methodParams.Length];
        var physicsSimulationProperties = dynamicServerAction.physicsSimulationProperties;
        var usePhysicsSimulationProperties = physicsSimulationProperties != null;
        if (methodParams.Length == 1 && methodParams[0].ParameterType == typeof(ServerAction)) {
            ServerAction serverAction = dynamicServerAction.ToObject<ServerAction>();
            serverAction.dynamicServerAction = dynamicServerAction;
            arguments[0] = serverAction;
        } else {
            var paramDict = methodParams.ToDictionary(param => param.Name, param => param);
            var argumentKeys = dynamicServerAction.ArgumentKeys().ToList();
            if (usePhysicsSimulationProperties) {
                argumentKeys.Add(DynamicServerAction.physicsSimulationPropsVariable);
            }
            var invalidArgs = argumentKeys
                .Where(argName => !paramDict.ContainsKey(argName))
                .ToList();
            if (invalidArgs.Count > 0) {
                Func<ParameterInfo, string> paramToString =
                    (ParameterInfo param) =>
                        $"{param.ParameterType.Name} {param.Name}{(param.HasDefaultValue ? " = " + param.DefaultValue : "")}";
                var matchingMethodOverWrites = getMatchingMethodOverwrites(target.GetType(), dynamicServerAction).Select(
                    m =>
                        $"{m.ReturnType.Name} {m.Name}(" +
                            string.Join(", ",
                                m.GetParameters()
                                .Select(paramToString)
                            )
                            + ")"
                );

                throw new InvalidArgumentsException(
                    dynamicServerAction.ArgumentKeys(),
                    invalidArgs,
                    methodParams.Select(paramToString),
                    matchingMethodOverWrites
                );
            }
            for (int i = 0; i < methodParams.Length; i++) {
                System.Reflection.ParameterInfo pi = methodParams[i];
                if (dynamicServerAction.ContainsKey(pi.Name)) {
                    try {
                        arguments[i] = dynamicServerAction.GetValue(pi.Name).ToObject(pi.ParameterType);
                    } catch (ArgumentException ex) {
                        throw new ToObjectArgumentActionException(
                            parameterName: pi.Name,
                            parameterType: pi.ParameterType,
                            parameterValueAsStr: dynamicServerAction.GetValue(pi.Name).ToString(),
                            ex: ex
                        );
                    }
                } else {
                    if (!pi.HasDefaultValue) {
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
        if (!usePhysicsSimulationProperties) {
            method.Invoke(target, arguments);
        }
        else {
            // physicsSimulationProperties.fixedDeltaTime = physicsSimulationProperties.fixedDeltaTime == null? Time.fixedDeltaTime: physicsSimulationProperties.fixedDeltaTime;
            var action = (System.Collections.IEnumerator)(method.Invoke(target, arguments));
            if (!physicsSimulationProperties.autoSimulation) {
                PhysicsSceneManager.unrollSimulatePhysics(
                    action, 
                    physicsSimulationProperties.fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime)
                );
            }
            else {
                target.StartCoroutine(action);
            }
        }
    }

    public static System.Collections.IEnumerator invokeActionUnderPhysicsSimulation<T>(
        MethodInfo method,
        T target,
        object[] args,
        PhysicsSimulationParams physicsSimulationProperties) where T : MonoBehaviour {

    yield return null;

    }

}



public class MethodParamComparer : IComparer<MethodInfo> {

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
        foreach (var p in method.GetParameters()) {
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
public class AmbiguousActionException : Exception {
    public AmbiguousActionException(string message) : base(message) { }
}

[Serializable]
public class ToObjectArgumentActionException : Exception {
    public string parameterName;
    public ArgumentException innerException;
    public Type parameterType;
    public string parameterValueAsStr;

    public ToObjectArgumentActionException(
        string parameterName,
        Type parameterType,
        string parameterValueAsStr,
        ArgumentException ex
    ) {
        this.parameterName = parameterName;
        this.parameterType = parameterType;
        this.parameterValueAsStr = parameterValueAsStr;
        this.innerException = ex;
    }
}

[Serializable]
public class MissingArgumentsActionException : Exception {
    public List<string> ArgumentNames;
    public MissingArgumentsActionException(List<string> argumentNames) {
        this.ArgumentNames = argumentNames;
    }
}

[Serializable]
public class InvalidArgumentsException : Exception {
    public IEnumerable<string> ArgumentNames;
    public IEnumerable<string> InvalidArgumentNames;
    public IEnumerable<string> ParameterNames;
    public IEnumerable<string> PossibleOverwrites;
    public InvalidArgumentsException(
            IEnumerable<string> argumentNames,
            IEnumerable<string> invalidArgumentNames,
            IEnumerable<string> parameterNames = null,
            IEnumerable<string> possibleOverwrites = null
        ) {
        this.ArgumentNames = argumentNames;
        this.InvalidArgumentNames = invalidArgumentNames;
        this.ParameterNames = parameterNames ?? new List<string>();
        this.PossibleOverwrites = possibleOverwrites ?? new List<string>();
    }
}