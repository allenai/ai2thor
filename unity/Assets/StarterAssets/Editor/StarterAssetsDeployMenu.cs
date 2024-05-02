using System;
using System.Linq;
using StarterAssetsPackageChecker;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
#if STARTER_ASSETS_PACKAGES_CHECKED
using Cinemachine;
#endif

namespace StarterAssets
{
    // This class needs to be a scriptable object to support dynamic determination of StarterAssets install path
    public partial class StarterAssetsDeployMenu : ScriptableObject
    {
        public const string MenuRoot = "Tools/Starter Assets";

        // prefab names
        private const string MainCameraPrefabName = "MainCamera";
        private const string PlayerCapsulePrefabName = "PlayerCapsule";

        // names in hierarchy
        private const string CinemachineVirtualCameraName = "PlayerFollowCamera";

        // tags
        private const string PlayerTag = "Player";
        private const string MainCameraTag = "MainCamera";
        private const string CinemachineTargetTag = "CinemachineTarget";

        private static GameObject _cinemachineVirtualCamera;
        
        /// <summary>
        /// Deletes the scripting define set by the Package Checker.
        /// See Assets/Editor/PackageChecker/PackageChecker.cs for more information
        /// </summary>
        [MenuItem(MenuRoot + "/Reinstall Dependencies", false)]
        static void ResetPackageChecker()
        {
            PackageChecker.RemovePackageCheckerScriptingDefine();
        }

#if STARTER_ASSETS_PACKAGES_CHECKED
        private static void CheckCameras(Transform targetParent, string prefabFolder)
        {
            CheckMainCamera(prefabFolder);

            GameObject vcam = GameObject.Find(CinemachineVirtualCameraName);

            if (!vcam)
            {
                if (TryLocatePrefab(CinemachineVirtualCameraName, new string[]{prefabFolder}, new[] { typeof(CinemachineVirtualCamera) }, out GameObject vcamPrefab, out string _))
                {
                    HandleInstantiatingPrefab(vcamPrefab, out vcam);
                    _cinemachineVirtualCamera = vcam;
                }
                else
                {
                    Debug.LogError("Couldn't find Cinemachine Virtual Camera prefab");
                }
            }
            else
            {
                _cinemachineVirtualCamera = vcam;
            }

            GameObject[] targets = GameObject.FindGameObjectsWithTag(CinemachineTargetTag);
            GameObject target = targets.FirstOrDefault(t => t.transform.IsChildOf(targetParent));
            if (target == null)
            {
                target = new GameObject("PlayerCameraRoot");
                target.transform.SetParent(targetParent);
                target.transform.localPosition = new Vector3(0f, 1.375f, 0f);
                target.tag = CinemachineTargetTag;
                Undo.RegisterCreatedObjectUndo(target, "Created new cinemachine target");
            }

            CheckVirtualCameraFollowReference(target, _cinemachineVirtualCamera);
        }

        private static void CheckMainCamera(string inFolder)
        {
            GameObject[] mainCameras = GameObject.FindGameObjectsWithTag(MainCameraTag);

            if (mainCameras.Length < 1)
            {
                // if there are no MainCameras, add one
                if (TryLocatePrefab(MainCameraPrefabName, new string[]{inFolder}, new[] { typeof(CinemachineBrain), typeof(Camera) }, out GameObject camera, out string _))
                {
                    HandleInstantiatingPrefab(camera, out _);
                }
                else
                {
                    Debug.LogError("Couldn't find Starter Assets Main Camera prefab");
                }
            }
            else
            {
                // make sure the found camera has a cinemachine brain (we only need 1)
                if (!mainCameras[0].TryGetComponent(out CinemachineBrain cinemachineBrain))
                    mainCameras[0].AddComponent<CinemachineBrain>();
            }
        }

        private static void CheckVirtualCameraFollowReference(GameObject target,
            GameObject cinemachineVirtualCamera)
        {
            var serializedObject =
                new SerializedObject(cinemachineVirtualCamera.GetComponent<CinemachineVirtualCamera>());
            var serializedProperty = serializedObject.FindProperty("m_Follow");
            serializedProperty.objectReferenceValue = target.transform;
            serializedObject.ApplyModifiedProperties();
        }

        private static bool TryLocatePrefab(string name, string[] inFolders, System.Type[] requiredComponentTypes, out GameObject prefab, out string path)
        {
            // Locate the player armature
            string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab", inFolders);
            for (int i = 0; i < allPrefabs.Length; ++i)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(allPrefabs[i]);

                if (assetPath.Contains("/StarterAssets/"))
                {
                    Object loadedObj = AssetDatabase.LoadMainAssetAtPath(assetPath);

                    if (PrefabUtility.GetPrefabAssetType(loadedObj) != PrefabAssetType.NotAPrefab &&
                        PrefabUtility.GetPrefabAssetType(loadedObj) != PrefabAssetType.MissingAsset)
                    {
                        GameObject loadedGo = loadedObj as GameObject;
                        bool hasRequiredComponents = true;
                        foreach (var componentType in requiredComponentTypes)
                        {
                            if (!loadedGo.TryGetComponent(componentType, out _))
                            {
                                hasRequiredComponents = false;
                                break;
                            }
                        }

                        if (hasRequiredComponents)
                        {
                             if (loadedGo.name == name)
                             {
                                 prefab = loadedGo;
                                 path = assetPath;
                                 return true;
                             }                           
                        }
                    }
                }
            }

            prefab = null;
            path = null;
            return false;
        }

        private static void HandleInstantiatingPrefab(GameObject prefab, out GameObject prefabInstance)
        {
            prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(prefabInstance, "Instantiate Starter Asset Prefab");

            prefabInstance.transform.localPosition = Vector3.zero;
            prefabInstance.transform.localEulerAngles = Vector3.zero;
            prefabInstance.transform.localScale = Vector3.one;
        }
#endif
    }
}