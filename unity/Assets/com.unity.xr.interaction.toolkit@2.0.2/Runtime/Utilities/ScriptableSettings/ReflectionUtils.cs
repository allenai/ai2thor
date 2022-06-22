using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine.XR.Interaction.Toolkit.Utilities
{
    /// <summary>
    /// Utility methods for common reflection-based operations
    /// </summary>
    static class ReflectionUtils
    {
        static Assembly[] s_Assemblies;
        static List<Type[]> s_TypesPerAssembly;

        static Assembly[] GetCachedAssemblies()
        {
            if (s_Assemblies == null)
                s_Assemblies = AppDomain.CurrentDomain.GetAssemblies();

            return s_Assemblies;
        }

        /// <summary>
        /// Iterate through all assemblies and execute a method on each one
        /// Catches ReflectionTypeLoadExceptions in each iteration of the loop
        /// </summary>
        /// <param name="callback">The callback method to execute for each assembly</param>
        public static void ForEachAssembly(Action<Assembly> callback)
        {
            var assemblies = GetCachedAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    callback(assembly);
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip any assemblies that don't load properly -- suppress errors
                }
            }
        }
    }
}
