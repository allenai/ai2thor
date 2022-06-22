using System;

namespace UnityEngine.XR.Interaction.Toolkit.Utilities
{
    /// <summary>
    /// Allows a class inheriting from <see cref="ScriptableSettings{T}"/> to specify that its instance Asset
    /// should be saved under "Assets/[<see cref="path"/>]/Resources/ScriptableSettings/".
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    class ScriptableSettingsPathAttribute : Attribute
    {
        /// <summary>
        /// The path where this ScriptableSettings should be stored
        /// </summary>
        public string path { get; private set; }

        /// <summary>
        /// Initialize a new ScriptableSettingsPathAttribute
        /// </summary>
        /// <param name="path">The path where the ScriptableSettings should be stored</param>
        public ScriptableSettingsPathAttribute(string path = "")
        {
            this.path = path;
        }
    }
}
