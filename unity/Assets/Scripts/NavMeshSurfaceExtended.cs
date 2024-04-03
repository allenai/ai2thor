    using System;
    using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

    public class NavMeshSurfaceExtended : NavMeshSurface {
        

        public NavMeshBuildSettings buildSettings { get; private set; }

        // static readonly List<NavMeshSurfaceExtended> s_NavMeshSurfaces = new List<NavMeshSurfaceExtended>();

        // /// <summary> Gets the list of all the <see cref="NavMeshSurface"/> components that are currently active in the scene. </summary>
        // public static new List<NavMeshSurfaceExtended> activeSurfaces
        // {
        //     get { return s_NavMeshSurfaces; }
        // }
        // Dictionary<int, NavMeshData> navmeshes = new Dictionary<int, NavMeshData>();

        public void BuildNavMesh(NavMeshBuildSettings buildSettings)
        {
            var sources = CollectSources();
            this.buildSettings = buildSettings;

            // Use unscaled bounds - this differs in behaviour from e.g. collider components.
            // But is similar to reflection probe - and since navmesh data has no scaling support - it is the right choice here.
            var sourcesBounds = new Bounds(center, Abs(size));
            if (collectObjects == CollectObjects.All || collectObjects == CollectObjects.Children)
            {
                sourcesBounds = CalculateWorldBounds(sources);
            }
            
            #if UNITY_EDITOR
                Debug.Log($"NavMeshSurface, building NavMehs with buildSettings:  agentRadius: {buildSettings.agentRadius} agentHeight: {buildSettings.agentHeight}, sourcesBounds: center: {sourcesBounds.center} extents: {sourcesBounds.extents}");
            #endif
            var data = NavMeshBuilder.BuildNavMeshData(buildSettings,
                    sources, sourcesBounds, transform.position, transform.rotation);

            if (data != null)
            {
                data.name = gameObject.name;
                RemoveData();
                navMeshData = data;
                if (isActiveAndEnabled) {
                    #if UNITY_EDITOR
                        Debug.Log($"NavMeshSurface: AddData happened.");
                    #endif
                    AddData();
                }
            }
        }

        /// <summary> Creates an instance of the NavMesh data and activates it in the navigation system. </summary>
        /// <remarks> The instance is created at the position and with the orientation of the GameObject. </remarks>
//         public void AddData()
//         {
// #if UNITY_EDITOR
//             var isInPreviewScene = EditorSceneManager.IsPreviewSceneObject(this);
//             var isPrefab = isInPreviewScene || EditorUtility.IsPersistent(this);
//             if (isPrefab)
//             {
//                 //Debug.LogFormat("NavMeshData from {0}.{1} will not be added to the NavMesh world because the gameObject is a prefab.",
//                 //    gameObject.name, name);
//                 return;
//             }
// #endif
//             if (m_NavMeshDataInstance.valid)
//                 return;

//             if (m_NavMeshData != null)
//             {
//                 m_NavMeshDataInstance = NavMesh.AddNavMeshData(m_NavMeshData, transform.position, transform.rotation);
//                 m_NavMeshDataInstance.owner = this;
//             }

//             m_LastPosition = transform.position;
//             m_LastRotation = transform.rotation;
//         }



        static Vector3 Abs(Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        Bounds CalculateWorldBounds(List<NavMeshBuildSource> sources)
        {
            // Use the unscaled matrix for the NavMeshSurface
            Matrix4x4 worldToLocal = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            worldToLocal = worldToLocal.inverse;

            var result = new Bounds();
            foreach (var src in sources)
            {
                switch (src.shape)
                {
                    case NavMeshBuildSourceShape.Mesh:
                        {
                            var m = src.sourceObject as Mesh;
                            result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, m.bounds));
                            break;
                        }
                    case NavMeshBuildSourceShape.Terrain:
                        {
#if NMC_CAN_ACCESS_TERRAIN
                            // Terrain pivot is lower/left corner - shift bounds accordingly
                            var t = src.sourceObject as TerrainData;
                            result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, new Bounds(0.5f * t.size, t.size)));
#else
                            Debug.LogWarning("The NavMesh cannot be properly baked for the terrain because the necessary functionality is missing. Add the com.unity.modules.terrain package through the Package Manager.");
#endif
                            break;
                        }
                    case NavMeshBuildSourceShape.Box:
                    case NavMeshBuildSourceShape.Sphere:
                    case NavMeshBuildSourceShape.Capsule:
                    case NavMeshBuildSourceShape.ModifierBox:
                        result.Encapsulate(GetWorldBounds(worldToLocal * src.transform, new Bounds(Vector3.zero, src.size)));
                        break;
                }
            }
            // Inflate the bounds a bit to avoid clipping co-planar sources
            result.Expand(0.1f);
            return result;
        }

        static Bounds GetWorldBounds(Matrix4x4 mat, Bounds bounds)
        {
            var absAxisX = Abs(mat.MultiplyVector(Vector3.right));
            var absAxisY = Abs(mat.MultiplyVector(Vector3.up));
            var absAxisZ = Abs(mat.MultiplyVector(Vector3.forward));
            var worldPosition = mat.MultiplyPoint(bounds.center);
            var worldSize = absAxisX * bounds.size.x + absAxisY * bounds.size.y + absAxisZ * bounds.size.z;
            return new Bounds(worldPosition, worldSize);
        }

        void AppendModifierVolumes(ref List<NavMeshBuildSource> sources)
        {
#if UNITY_EDITOR
            var myStage = StageUtility.GetStageHandle(gameObject);
            if (!myStage.IsValid()) {
                return;
            }
#endif
            // Modifiers
            List<NavMeshModifierVolume> modifiers;
            if (collectObjects == CollectObjects.Children)
            {
                modifiers = new List<NavMeshModifierVolume>(GetComponentsInChildren<NavMeshModifierVolume>());
                modifiers.RemoveAll(x => !x.isActiveAndEnabled);
            }
            else
            {
                modifiers = NavMeshModifierVolume.activeModifiers;
            }

            foreach (var m in modifiers)
            {
                if ((layerMask & (1 << m.gameObject.layer)) == 0) {
                    continue;
                }
                if (!m.AffectsAgentType(agentTypeID)) {
                    continue;
                }
#if UNITY_EDITOR
                if (!myStage.Contains(m.gameObject)) {
                    continue;
                }
#endif
                var mcenter = m.transform.TransformPoint(m.center);
                var scale = m.transform.lossyScale;
                var msize = new Vector3(m.size.x * Mathf.Abs(scale.x), m.size.y * Mathf.Abs(scale.y), m.size.z * Mathf.Abs(scale.z));

                var src = new NavMeshBuildSource();
                src.shape = NavMeshBuildSourceShape.ModifierBox;
                src.transform = Matrix4x4.TRS(mcenter, m.transform.rotation, Vector3.one);
                src.size = msize;
                src.area = m.area;
                sources.Add(src);
            }
        }



        public List<NavMeshBuildSource> CollectSources()
        {
            var sources = new List<NavMeshBuildSource>();
            var markups = new List<NavMeshBuildMarkup>();

            List<NavMeshModifier> modifiers;
            if (collectObjects == CollectObjects.Children)
            {
                modifiers = new List<NavMeshModifier>(GetComponentsInChildren<NavMeshModifier>());
                modifiers.RemoveAll(x => !x.isActiveAndEnabled);
            }
            else
            {
                modifiers = NavMeshModifier.activeModifiers;
            }

            foreach (var m in modifiers)
            {
                if ((layerMask & (1 << m.gameObject.layer)) == 0) {
                    continue;
                }
                if (!m.AffectsAgentType(agentTypeID)) {
                    continue;
                }
                var markup = new NavMeshBuildMarkup();
                markup.root = m.transform;
                markup.overrideArea = m.overrideArea;
                markup.area = m.area;
                markup.ignoreFromBuild = m.ignoreFromBuild;
                markups.Add(markup);
            }

#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                if (collectObjects == CollectObjects.All)
                {
                    UnityEditor.AI.NavMeshBuilder.CollectSourcesInStage(
                        null, layerMask, useGeometry, defaultArea, markups, gameObject.scene, sources);
                }
                else if (collectObjects == CollectObjects.Children)
                {
                    UnityEditor.AI.NavMeshBuilder.CollectSourcesInStage(
                        transform, layerMask, useGeometry, defaultArea, markups, gameObject.scene, sources);
                }
                else if (collectObjects == CollectObjects.Volume)
                {
                    Matrix4x4 localToWorld = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                    
                    var worldBounds = GetWorldBounds(localToWorld, new Bounds(center, size));

                    UnityEditor.AI.NavMeshBuilder.CollectSourcesInStage(
                        worldBounds, layerMask, useGeometry, defaultArea, markups, gameObject.scene, sources);
                }
            }
            else
#endif
            {
                if (collectObjects == CollectObjects.All)
                {
                    NavMeshBuilder.CollectSources(null, layerMask, useGeometry, defaultArea, markups, sources);
                }
                else if (collectObjects == CollectObjects.Children)
                {
                    NavMeshBuilder.CollectSources(transform, layerMask, useGeometry, defaultArea, markups, sources);
                }
                else if (collectObjects == CollectObjects.Volume)
                {
                    Matrix4x4 localToWorld = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                    var worldBounds = GetWorldBounds(localToWorld, new Bounds(center, size));
                    NavMeshBuilder.CollectSources(worldBounds, layerMask, useGeometry, defaultArea, markups, sources);
                }
            }

            if (ignoreNavMeshAgent) {
                sources.RemoveAll((x) => (x.component != null && x.component.gameObject.GetComponent<NavMeshAgent>() != null));
            }

            if (ignoreNavMeshObstacle) {
                sources.RemoveAll((x) => (x.component != null && x.component.gameObject.GetComponent<NavMeshObstacle>() != null));
            }

            AppendModifierVolumes(ref sources);

            return sources;
        }

    }