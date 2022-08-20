/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction
{
    [ExecuteAlways]
    public class DebugGizmos : MonoBehaviour
    {
        private List<Vector4> _points = new List<Vector4>();
        private List<Color> _colors = new List<Color>();
        private int _index = 0;
        private bool _addedSegmentSinceLastUpdate = false;

#if UNITY_EDITOR
        private bool _drewGizmos = false;
        private int _sceneRepaint = 0;
#endif

        protected static DebugGizmos _root = null;

        protected static DebugGizmos Root
        {
            get
            {
                if (_root == null)
                {
                    // Use Find instead of FindObjectsByType<> as the extra parameter
                    // is unsupported by later versions of Unity
                    GameObject polylineGizmosGO = GameObject.Find("Polyline Gizmos");
                    if (polylineGizmosGO != null)
                    {
                        DebugGizmos gizmos = polylineGizmosGO.GetComponent<DebugGizmos>();
                        if (gizmos != null)
                        {
                            _root = gizmos;
#if UNITY_EDITOR
                            if (_root.isActiveAndEnabled)
                            {
                                _root.HookUpToEditorEvents();
                            }
#endif
                        }
                    }
                }

                if (_root == null)
                {
                    GameObject go = new GameObject("Polyline Gizmos");
                    _root = go.AddComponent<DebugGizmos>();
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        EditorUtility.SetDirty(_root);
                    }

                    _root.HookUpToEditorEvents();
#endif
                }

                return _root;
            }
        }

        protected virtual void OnEnable()
        {
            if (_root == null)
            {
                return;
            }

#if !UNITY_EDITOR
            if (_root != this)
            {
                Destroy(this);
            }
#else
            if (_root == this)
            {
                if (!Application.isPlaying)
                {
                    HookUpToEditorEvents();
                }
            }
            else
            {
                enabled = false;
                if (Application.isPlaying)
                {
                    Destroy(this);
                }
                else
                {
                    EditorApplication.update += MarkForDestroy;
                }
            }
#endif
        }


#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (_sceneRepaint == 0)
            {
                _drewGizmos = true;
                _sceneRepaint = 2;
                SceneView.RepaintAll();
            }
        }

        private void MarkForDestroy()
        {
            EditorApplication.update -= MarkForDestroy;
            DestroyImmediate(this);
        }

        private void HookUpToEditorEvents()
        {
            if (Application.isPlaying)
            {
                return;
            }
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
            Camera.onPreCull += HandlePreCullRender;
        }

        private void HandlePreCullRender(Camera cam)
        {
            if (_drewGizmos && !_addedSegmentSinceLastUpdate)
            {
                ClearSegments();
            }

            _addedSegmentSinceLastUpdate = false;
            _drewGizmos = false;

            RenderSegments();

            if (_sceneRepaint > 0)
            {
                _sceneRepaint--;
            }
        }
        private void PlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                EditorApplication.playModeStateChanged -= PlayModeStateChanged;
                Camera.onPreCull -= HandlePreCullRender;
            }
        }
#endif

        private PolylineRenderer _polylineRenderer;

        private PolylineRenderer Renderer
        {
            get
            {
                if (_polylineRenderer == null)
                {
                    _polylineRenderer = new PolylineRenderer(null, _renderSinglePass);
                }

                return _polylineRenderer;
            }
        }

        protected virtual void OnDisable()
        {
            if (_polylineRenderer != null)
            {
                _polylineRenderer.Cleanup();
                _polylineRenderer = null;
            }

            if (Application.isPlaying)
            {
                return;
            }

    #if UNITY_EDITOR
            if (_root == this)
            {
                EditorApplication.playModeStateChanged -= PlayModeStateChanged;
                Camera.onPreCull -= HandlePreCullRender;
                _root = null;
            }
    #endif
        }

        protected void ClearSegments()
        {
            _index = 0;
        }

        protected void RenderSegments()
        {
            Renderer.SetLines(_points, _colors, _index);
            Renderer.RenderLines();
        }

        protected virtual void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            RenderSegments();
            ClearSegments();
        }

        protected void AddSegment(Vector3 p0, Vector3 p1, float width, Color color0, Color color1)
        {
            if (!_addedSegmentSinceLastUpdate)
            {
                ClearSegments();
                _addedSegmentSinceLastUpdate = true;
            }

            while (_index + 2 > _points.Count)
            {
                _points.Add(new Vector4());
                _colors.Add(new Color());
            }

            _points[_index] = new Vector4(p0.x, p0.y, p0.z, width);
            _points[_index + 1] = new Vector4(p1.x, p1.y, p1.z, width);
            _colors[_index] = color0;
            _colors[_index + 1] = color1;

            _index += 2;
        }

        private static bool _renderSinglePass = true;
        public static bool RenderSinglePass {
            get
            {
                return _renderSinglePass;
            }

            set
            {
                if (_renderSinglePass == value)
                {
                    return;
                }
                _renderSinglePass = value;
                if (Root != null)
                {
                    Destroy(Root);
                }
            }
        }

        public static Color Color = Color.black;
        public static float LineWidth = 0.1f;

        public static void DrawPoint(Vector3 p0, Transform t = null)
        {
            if (t != null)
            {
                p0 = t.TransformPoint(p0);
            }

            Root.AddSegment(p0, p0, LineWidth, Color, Color);
        }

        public static void DrawLine(Vector3 p0, Vector3 p1, Transform t = null)
        {
            if (t != null)
            {
                p0 = t.TransformPoint(p0);
                p1 = t.TransformPoint(p1);
            }

            Root.AddSegment(p0, p1, LineWidth, Color, Color);
        }

        public static void DrawWireCube(Vector3 center, float size, Transform t = null)
        {
            for (int i = 0; i < CUBE_SEGMENTS.Count; i += 2)
            {
                Vector3 p0 = CUBE_POINTS[CUBE_SEGMENTS[i]] * size + center;
                Vector3 p1 = CUBE_POINTS[CUBE_SEGMENTS[i + 1]] * size + center;
                DrawLine(p0, p1, t);
            }
        }

        public static void DrawAxis(Vector3 position, Quaternion rotation, float size = 1.0f)
        {
            Color _saveColor = Color;
            Color = Color.red;
            DrawLine(position, position + rotation*Vector3.right * size);
            Color = Color.green;
            DrawLine(position, position + rotation*Vector3.up * size);
            Color = Color.blue;
            DrawLine(position, position + rotation*Vector3.forward * size);
            Color = _saveColor;
        }

        public static void DrawAxis(Pose pose, float size = 1.0f)
        {
            DrawAxis(pose.position, pose.rotation, size);
        }

        public static void DrawAxis(Transform t, float size = 1.0f)
        {
            DrawAxis(t.GetPose(), size);
        }

        private readonly static IReadOnlyList<Vector3> CUBE_POINTS = new List<Vector3>()
        {
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f)
        };

        private readonly static IReadOnlyList<int> CUBE_SEGMENTS = new List<int>()
        {
            0,
            1,
            1,
            5,
            3,
            5,
            0,
            3,
            0,
            2,
            1,
            4,
            3,
            6,
            5,
            7,
            2,
            4,
            4,
            7,
            7,
            6,
            6,
            2
        };
    }
}
