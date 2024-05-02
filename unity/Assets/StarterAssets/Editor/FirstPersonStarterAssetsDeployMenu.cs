using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace StarterAssets
{
    public partial class StarterAssetsDeployMenu : ScriptableObject
    {
#if STARTER_ASSETS_PACKAGES_CHECKED
        /// <summary>
        /// Check the capsule, main camera, cinemachine virtual camera, camera target and references
        /// </summary>
        [MenuItem(MenuRoot + "/Reset First Person Controller", false)]
        static void ResetFirstPersonControllerCapsule()
        {
            var firstPersonControllers = FindObjectsOfType<FirstPersonController>();
            var player = firstPersonControllers.FirstOrDefault(controller => controller.CompareTag(PlayerTag));

            GameObject playerGameObject = null;

            // player
            if (player == null)
            {
                if (TryLocatePrefab(PlayerCapsulePrefabName, null, new []{typeof(FirstPersonController)}, out GameObject prefab, out string _))
                {
                    HandleInstantiatingPrefab(prefab, out playerGameObject);
                }
                else
                {
                    Debug.LogError("Couldn't find player armature prefab");
                }
            }
            else
            {
                playerGameObject = player.gameObject;
            }

            if (playerGameObject != null)
            {
                // cameras
                CheckCameras(playerGameObject.transform, GetFirstPersonPrefabPath());
            }
        }
        
        static string GetFirstPersonPrefabPath()
        {
            if (TryLocatePrefab(PlayerCapsulePrefabName, null, new[] { typeof(FirstPersonController), typeof(StarterAssetsInputs) }, out GameObject _, out string prefabPath))
            {
                var pathString = new StringBuilder();
                var currentDirectory = new FileInfo(prefabPath).Directory;
                while (currentDirectory.Name != "Assets")
                {
                    pathString.Insert(0, $"/{currentDirectory.Name}");
                    currentDirectory = currentDirectory.Parent;
                }

                pathString.Insert(0, currentDirectory.Name);
                return pathString.ToString();
            }

            return null;
        }
#endif
    }
}