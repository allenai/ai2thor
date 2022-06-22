using System;
using UnityEngine.Assertions;

namespace UnityEngine.XR.Interaction.Toolkit.Utilities
{
    /// <summary>
    /// Extension methods for Type objects
    /// </summary>
    static class TypeExtensions
    {
        /// <summary>
        /// Gets the first attribute of a given type.
        /// </summary>
        /// <typeparam name="TAttribute">Attribute type to return</typeparam>
        /// <param name="type">The type whose attribute will be returned</param>
        /// <param name="inherit">Whether to search this type's inheritance chain to find the attribute</param>
        /// <returns>The first <typeparamref name="TAttribute"/> found</returns>
        public static TAttribute GetAttribute<TAttribute>(this Type type, bool inherit = false)
            where TAttribute : Attribute
        {
            Assert.IsTrue(type.IsDefined(typeof(TAttribute), inherit), "Attribute not found");
            return (TAttribute) type.GetCustomAttributes(typeof(TAttribute), inherit)[0];
        }
    }
}
