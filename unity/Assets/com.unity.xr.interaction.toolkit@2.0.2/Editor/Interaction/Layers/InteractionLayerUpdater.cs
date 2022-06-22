using System;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using Object = UnityEngine.Object;

namespace UnityEditor.XR.Interaction.Toolkit
{
    /// <summary>
    /// Class that holds methods to update the Unity interaction <see cref="LayerMask"/> to the equivalent
    /// <see cref="InteractionLayerMask"/> in all editable Interactables and Interactors in the project.
    /// </summary>
    /// <see cref="XRBaseInteractable.interactionLayerMask"/>
    /// <see cref="XRBaseInteractable.interactionLayers"/>
    /// <see cref="XRBaseInteractor.interactionLayerMask"/>
    /// <see cref="XRBaseInteractor.interactionLayers"/>
    [InitializeOnLoad]
    static class InteractionLayerUpdater
    {
        static List<XRBaseInteractable> s_InteractableList;
        static List<XRBaseInteractor> s_InteractorList;

        static InteractionLayerUpdater()
        {
            if (!Application.isBatchMode)
                EditorApplication.update += OnUpdate;
        }
        
        static void OnUpdate()
        {
            if (EditorApplication.isCompiling)
                return;

            EditorApplication.update -= OnUpdate;

            if (!XRInteractionEditorSettings.instance.interactionLayerUpdaterShown)
                RunIfUserWantsTo();
        }
        
        /// <summary>
        /// Displays a dialog message asking if you want to update the Interaction Layers in the project.
        /// </summary>
        /// <returns>Returns whether you selected to update.</returns>
        static bool AskIfUserWantsToUpdate()
        {
            const string titleText = "XR InteractionLayerMask Update Required";
            const string messageText = "This project may contain an obsolete method to validate interactions between XR Interactors and Interactables. " +
                                       "\n\nThis Update is only required for older projects updating the XR Interaction Toolkit package, if this package was newly installed please cancel this operation. " +
                                       "\n\nIf you choose \'Go Ahead\', Unity will update all Interactors and Interactables in Prefabs and scenes to use the new Interaction Layer instead of the Unity physics Layer. " +
                                       "\n\nYou can always manually run the XR InteractionLayerMask Updater from the XR Interaction Toolkit Settings (menu: Edit > Project Settings > XR Interaction Toolkit). ";
            const string okText = "I Made a Backup, Go Ahead!";
            const string cancelText = "No Thanks";
            return EditorUtility.DisplayDialog(titleText, messageText, okText, cancelText);
        }

        /// <summary>
        /// Checks if the Unity Interaction Layer Mask property is overriden in the supplied object.
        /// </summary>
        /// <param name="target">The object to check if the Unity Interaction Layer is overriden.</param>
        /// <returns>Returns whether the property is overriden.</returns>
        static bool IsInteractionLayerMaskPropertyOverriden(Object target)
        {
            const string layerBitsPropertyPath = "m_InteractionLayerMask.m_Bits";
            
            var propertyModifications = PrefabUtility.GetPropertyModifications(target);
            if (propertyModifications == null)
                return false;

            foreach (var propertyModification in propertyModifications)
            {
                if (!PrefabUtility.IsDefaultOverride(propertyModification) &&
                    string.Equals(propertyModification.propertyPath, layerBitsPropertyPath, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Try to update the <see cref="InteractionLayerMask"/> in the supplied object; stores the used Unity Layers in the supplied Layer Mask.
        /// </summary>
        /// <param name="target">The Unity object to update the Interaction Layer Mask, usually an Interactable or an Interactor.</param>
        /// <param name="usedUnityLayers">The used unity layers will be stored in this mask.</param>
        /// <returns>Returns whether the supplied object was update.</returns>
        internal static bool TryUpdateInteractionLayerMaskProperty(Object target, ref LayerMask usedUnityLayers)
        {
            const string layerPropertyPath = "m_InteractionLayerMask";
            const string interactionLayerPropertyPath = "m_InteractionLayers.m_Bits";
            
            if (PrefabUtility.IsPartOfImmutablePrefab(target))
                return false;
            
            // it isn't a regular object in the scene and not a missing prefab instance
            // and not a component in the source prefab or neither an overriden component (so it's a variant or nested prefab)
            // and the unity layer mask it isn't an overriden property?
            var nearestPrefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(target);
            if (nearestPrefabRoot != null && !PrefabUtility.IsPrefabAssetMissing(target) 
                                          && (PrefabUtility.IsPartOfVariantPrefab(nearestPrefabRoot) || !PrefabUtility.IsAddedComponentOverride(target)) 
                                          && !IsInteractionLayerMaskPropertyOverriden(target))
                return false;

            var serializedObject = new SerializedObject(target);
            var layerProperty = serializedObject.FindProperty(layerPropertyPath);
            var interactionLayerProperty = serializedObject.FindProperty(interactionLayerPropertyPath);

            if (layerProperty == null || interactionLayerProperty == null || layerProperty.propertyType != SerializedPropertyType.LayerMask 
                || interactionLayerProperty.propertyType != SerializedPropertyType.Integer)
                return false;

            // updates the used unity layers; this is only to identify what unity layers are being used as Interaction Layer
            // - Nothing option (value 0) is ignored since it'll not add any new layer
            // - Everything option (value -1) is ignored since all bits/layers are being considered but we can't assume this since
            // these layers are also being used for physics and rendering. If a unity layer is being used as an Interaction Layer then
            // it should be specifically being used in some serializedProperty being updated and we'll eventually get its value
            // otherwise it's safe to not store its value since the flow to update the actual serializedProperty will be performed next
            var unityLayerMaskValue = layerProperty.intValue;
            if (unityLayerMaskValue != 0 && unityLayerMaskValue != -1)
                usedUnityLayers.value |= unityLayerMaskValue;

            // updates the interaction layer mask property if its value is different from the unity layer mask
            if (interactionLayerProperty.intValue == unityLayerMaskValue)
                return false;
            
            interactionLayerProperty.longValue = (uint)unityLayerMaskValue;
            serializedObject.ApplyModifiedProperties();
            return true;
        }

        /// <summary>
        /// Try to update the Interaction Layers in all Interactors and Interactables in the supplied game object;
        /// stores the used unity layers in the supplied layer mask.
        /// </summary>
        /// <param name="gameObject">The game object to be updated.</param>
        /// <param name="usedUnityLayers">The used unity layers will be stored in this mask.</param>
        /// <returns>Returns whether the supplied game object has been updated.</returns>
        static bool TryUpdate(GameObject gameObject, ref LayerMask usedUnityLayers)
        {
            var updated = false;
            
            // update Interactables
            s_InteractableList.Clear();
            gameObject.GetComponentsInChildren(s_InteractableList);
            foreach (var interactable in s_InteractableList)
                updated |= TryUpdateInteractionLayerMaskProperty(interactable, ref usedUnityLayers);

            // update Interactors
            s_InteractorList.Clear();
            gameObject.GetComponentsInChildren(s_InteractorList);
            foreach (var interactor in s_InteractorList)
                updated |= TryUpdateInteractionLayerMaskProperty(interactor, ref usedUnityLayers);

            return updated;
        }
        
        /// <summary>
        /// Opens or creates a Scene in the Editor.
        /// </summary>
        /// <param name="scenePath">The path of the Scene. This should be relative to the Project folder. If the path
        /// is null or empty then it creates a new scene.</param>
        /// <param name="mode">Allows you to select how to open the specified Scene, and whether to keep existing
        /// Scenes in the Hierarchy. See SceneManagement.OpenSceneMode for more information about the options.</param>
        static void OpenOrCreateScene(string scenePath, OpenSceneMode mode = OpenSceneMode.Single)
        {
            if (string.IsNullOrEmpty(scenePath))
            {
                var newSceneMode = mode == OpenSceneMode.Single ? NewSceneMode.Single : NewSceneMode.Additive;
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, newSceneMode);
            }
            else
            {
                EditorSceneManager.OpenScene(scenePath, mode);
            }
        }
        
        /// <summary>
        /// Scans project for Interactables and Interactors in prefabs and update their Unity Layer Mask to the correspondent
        /// Interaction Layer Mask.
        /// </summary>
        /// <returns>Returns a layer mask containing all unity layers used by the scanned prefabs.</returns>
        static LayerMask UpdatePrefabs()
        {
            const string prefabFilter = "t:Prefab";
            const string titleString = "Updating Prefabs";
                
            var prefabGuids = AssetDatabase.FindAssets(prefabFilter);
            var prefabGuidsLength = prefabGuids.Length;
            LayerMask usedUnityLayers = 0;

            for (var i = 0; i < prefabGuidsLength; i++)
            {
                var prefabGuid = prefabGuids[i];
                var prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                
                EditorUtility.DisplayProgressBar(titleString, prefabPath, i / (float)prefabGuidsLength);
                
                // Check to make sure the asset is actually writable.
                var packageInfo = PackageManager.PackageInfo.FindForAssetPath(prefabPath);
                if (packageInfo != null && packageInfo.source != PackageSource.Embedded && packageInfo.source != PackageSource.Local)
                    continue;

                var rootGameObject = PrefabUtility.LoadPrefabContents(prefabPath);
                if (TryUpdate(rootGameObject, ref usedUnityLayers))
                {
                    PrefabUtility.SaveAsPrefabAsset(rootGameObject, prefabPath);
                    Debug.Log(prefabPath + " updated");
                }

                PrefabUtility.UnloadPrefabContents(rootGameObject);
            }
            
            EditorUtility.ClearProgressBar();
            return usedUnityLayers;
        }

        /// <summary>
        /// Scans project Scenes for Interactables and Interactors and update their Interaction Layer Mask
        /// to match the correspondent Unity Layer Mask configured on them.
        /// </summary>
        /// <returns>Returns a layer mask containing all unity layers used by the scanned Scene objects.</returns>
        static LayerMask UpdateScenes()
        {
            const string sceneFilter = "t:Scene";
            const string titleString = "Updating Scenes";
            
            var sceneGuids = AssetDatabase.FindAssets(sceneFilter);
            var sceneGuidsLength = sceneGuids.Length;
            var rootGameObjects = new List<GameObject>();
            LayerMask usedUnityLayers = 0;

            // store active and opened scenes
            var oldActiveScenePath = SceneManager.GetActiveScene().path;
            var oldOpenedScenePaths = new string[SceneManager.sceneCount];
            for (var i = 0; i < SceneManager.sceneCount; i++)
                oldOpenedScenePaths[i] = SceneManager.GetSceneAt(i).path;

            for (var i = 0; i < sceneGuidsLength; i++)
            {
                var sceneGuid = sceneGuids[i];
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                
                EditorUtility.DisplayProgressBar(titleString, scenePath, i / (float)sceneGuidsLength);
                
                // Check to make sure the asset is actually writable.
                var packageInfo = PackageManager.PackageInfo.FindForAssetPath(scenePath);
                if (packageInfo != null && packageInfo.source != PackageSource.Embedded && packageInfo.source != PackageSource.Local)
                    continue;

                var sceneUpdated = false;
                var scene = EditorSceneManager.OpenScene(scenePath);
                scene.GetRootGameObjects(rootGameObjects);
                foreach (var gameObject in rootGameObjects)
                    sceneUpdated |= TryUpdate(gameObject, ref usedUnityLayers);

                if (sceneUpdated)
                {
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log(scenePath + " updated");
                }

                rootGameObjects.Clear();
            }
            
            // restore active and opened scenes
            if (oldOpenedScenePaths.Length <= 0)
            {
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
            }
            else
            {
                OpenOrCreateScene(oldOpenedScenePaths[0]);
                for (var i = 1; i < oldOpenedScenePaths.Length; i++)
                    OpenOrCreateScene(oldOpenedScenePaths[i], OpenSceneMode.Additive);

                if (!string.IsNullOrEmpty(oldActiveScenePath))
                {
                    var newActiveScene = SceneManager.GetSceneByPath(oldActiveScenePath);
                    SceneManager.SetActiveScene(newActiveScene);
                }
            }

            EditorUtility.ClearProgressBar();
            return usedUnityLayers;
        }

        /// <summary>
        /// Copies the layer names and its index from the supplied Unity Layer Mask to the InteractionLayerSettings
        /// asset.
        /// </summary>
        /// <param name="unityLayerMask">The Unity Layer Mask to be copied from.</param>
        static void CopyLayersToInteractionLayerSettings(LayerMask unityLayerMask)
        {
            const string layerNamesPropertyPath = "m_LayerNames";
            
            var interactionLayerSettingsSo = new SerializedObject(InteractionLayerSettings.instance);
            var layerNamesProperty = interactionLayerSettingsSo.FindProperty(layerNamesPropertyPath);
            
            // built-in Interaction Layer names are not editable, so they are ignored 
            for (var i = InteractionLayerSettings.k_BuiltInLayerSize; i < InteractionLayerSettings.k_LayerSize; i++)
            {
                var layerBit = 1 << i;
                if ((unityLayerMask.value & layerBit) == 0)
                    continue;

                var layerName = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(layerName))
                    continue;

                var interactionLayerNameProperty = layerNamesProperty.GetArrayElementAtIndex(i);
                interactionLayerNameProperty.stringValue = layerName;
            }

            interactionLayerSettingsSo.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// Asks you if you want to run the Interaction Layer updater.
        /// </summary>
        internal static void RunIfUserWantsTo()
        {
            if (AskIfUserWantsToUpdate() && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                s_InteractableList = new List<XRBaseInteractable>();
                s_InteractorList = new List<XRBaseInteractor>();
                LayerMask usedUnityLayers = 0;

                usedUnityLayers.value |= UpdatePrefabs();
                usedUnityLayers.value |=UpdateScenes();
                CopyLayersToInteractionLayerSettings(usedUnityLayers);

                s_InteractableList = null;
                s_InteractorList = null;
            }
            
            // register updater was shown
            var editorXriToolkitSettings = XRInteractionEditorSettings.instance;
            if (!editorXriToolkitSettings.interactionLayerUpdaterShown)
            {
                editorXriToolkitSettings.interactionLayerUpdaterShown = true;
                EditorUtility.SetDirty(editorXriToolkitSettings);
            }
        }
    }
}