#if UNITY_EDITOR

using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.XR.Interaction.Toolkit.Utilities
{
    /// <summary>
    /// Editor utility methods for locating Component instances.
    /// </summary>
    static class EditorComponentLocatorUtility
    {
        /// <summary>
        /// Returns the first active loaded object of the given type in the same Scene as the GameObject,
        /// biasing towards being hierarchically related to the GameObject.
        /// </summary>
        /// <typeparam name="T">The Component type to find.</typeparam>
        /// <param name="gameObject">The <see cref="GameObject"/> in the Scene to search.</param>
        /// <returns>
        /// Returns the object that matches the specified type in the Scene.
        /// Otherwise, returns <see langword="null"/> if no object matches the type in the Scene.
        /// </returns>
        /// <remarks>
        /// This method can be used when finding a Component to reference in the same Scene
        /// since serialization of cross scene references are not supported.
        /// </remarks>
        public static T FindSceneComponentOfType<T>(GameObject gameObject) where T : Component
        {
            if (gameObject == null)
                return null;

            // 1. Search children first since those can be serialized with a prefab.
            // 2. Search parents for logical ownership.
            // 3. Search the rest of the Scene.
            var component = gameObject.GetComponentInChildren<T>(true);
            if (component != null)
                return component;

            component = gameObject.GetComponentInParent<T>();
            if (component != null)
                return component;

            return FindSceneComponentOfType<T>(gameObject.scene);
        }

        /// <summary>
        /// Returns the first active loaded object of the given type in the same Scene.
        /// </summary>
        /// <typeparam name="T">The Component type to find.</typeparam>
        /// <param name="scene">The <see cref="Scene"/> to search.</param>
        /// <returns>
        /// Returns the object that matches the specified type in the Scene.
        /// Otherwise, returns <see langword="null"/> if no object matches the type in the Scene.
        /// </returns>
        /// <remarks>
        /// This method can be used when finding a Component to reference in the same Scene
        /// since serialization of cross scene references are not supported.
        /// </remarks>
        public static T FindSceneComponentOfType<T>(Scene scene) where T : Component
        {
            var currentStage = StageUtility.GetCurrentStageHandle();
            var components = currentStage.FindComponentsOfType<T>();
            foreach (var component in components)
            {
                if (component.gameObject.scene == scene)
                {
                    return component;
                }
            }

            return null;
        }
    }
}

#endif