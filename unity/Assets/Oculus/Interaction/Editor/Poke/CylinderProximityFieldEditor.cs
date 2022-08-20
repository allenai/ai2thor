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

using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.Editor
{
    [CustomEditor(typeof(CylinderProximityField))]
    public class CylinderProximityFieldEditor : UnityEditor.Editor
    {
        private const int SEGMENTS_PER_UNIT = 5;

        private SerializedProperty _curvedPlaneProperty;

        private void OnEnable()
        {
            _curvedPlaneProperty = serializedObject.FindProperty("_curvedPlane");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            CylinderProximityField proxField = target as CylinderProximityField;
            if (_curvedPlaneProperty.objectReferenceValue != null &&
                _curvedPlaneProperty.objectReferenceValue != proxField)
            {
                GUIStyle italicLabel = new GUIStyle(GUI.skin.label);
                italicLabel.fontStyle = FontStyle.Italic;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"{typeof(ICurvedPlane).Name} properties overridden by " +
                    $"{_curvedPlaneProperty.objectReferenceValue.GetType().Name}", italicLabel);
                EditorGUILayout.PropertyField(_curvedPlaneProperty);
            }
            else
            {
                DrawDefaultInspector();
            }
            serializedObject.ApplyModifiedProperties();
        }

        public void OnSceneGUI()
        {
            ICurvedPlane curvedPlane = _curvedPlaneProperty.objectReferenceValue as ICurvedPlane;
            if (curvedPlane == null)
            {
                curvedPlane = target as ICurvedPlane;
            }

            if (curvedPlane.Cylinder == null ||
                curvedPlane.ArcDegrees <= 0f)
            {
                return;
            }

            Handles.color = EditorConstants.PRIMARY_COLOR;

            // Handle infinite height using scene camera Y
            float top, bottom;
            if (curvedPlane.Top <= curvedPlane.Bottom)
            {
                if (SceneView.lastActiveSceneView != null &&
                    SceneView.lastActiveSceneView.camera != null)
                {
                    Vector3 cameraPos =
                        curvedPlane.Cylinder.transform.InverseTransformPoint(
                            SceneView.lastActiveSceneView.camera.transform.position);
                    bottom = cameraPos.y - 10;
                    top = cameraPos.y + 10;
                }
                else
                {
                    bottom = -30;
                    top = 30;
                }
            }
            else
            {
                bottom = curvedPlane.Bottom;
                top = curvedPlane.Top;
            }

            float height = top - bottom;
            float width = curvedPlane.ArcDegrees * Mathf.Deg2Rad * curvedPlane.Cylinder.Radius;
            int verticalSegments = Mathf.Max(2, Mathf.CeilToInt(SEGMENTS_PER_UNIT * height));
            int horizontalSegments = Mathf.Max(2, Mathf.FloorToInt(SEGMENTS_PER_UNIT * width));

            for (int v = 0; v <= verticalSegments; ++v)
            {
                float y = Mathf.Lerp(bottom, top, (float)v / verticalSegments);
                DrawArc(curvedPlane, y);
            }

            for (int h = 0; h <= horizontalSegments; ++h)
            {
                float x = Mathf.Lerp(-curvedPlane.ArcDegrees / 2,
                                     curvedPlane.ArcDegrees / 2,
                                     (float)h / horizontalSegments);
                DrawLine(curvedPlane, bottom, top, x);
            }
        }

        private void DrawArc(ICurvedPlane curvedPlane, float y)
        {
            Vector3 center = curvedPlane.Cylinder.transform.TransformPoint(new Vector3(0, y, 0));
            Vector3 forward = curvedPlane.Cylinder.transform.TransformDirection(
                Quaternion.Euler(0, curvedPlane.Rotation - curvedPlane.ArcDegrees / 2, 0) *
                Vector3.forward);

            Handles.DrawWireArc(center,
                     curvedPlane.Cylinder.transform.up,
                     forward,
                     curvedPlane.ArcDegrees,
                     curvedPlane.Cylinder.Radius * curvedPlane.Cylinder.transform.lossyScale.z
#if UNITY_2020_2_OR_NEWER
                     , EditorConstants.LINE_THICKNESS
#endif
                     );
        }

        private void DrawLine(ICurvedPlane curvedPlane, float bottom, float top, float deg)
        {
            Vector3 forward = Quaternion.Euler(0, curvedPlane.Rotation + deg, 0) *
                Vector3.forward * curvedPlane.Cylinder.Radius;

            Vector3 p1 = curvedPlane.Cylinder.transform.TransformPoint((Vector3.up * bottom) + forward);
            Vector3 p2 = curvedPlane.Cylinder.transform.TransformPoint((Vector3.up * top) + forward);

#if UNITY_2020_2_OR_NEWER
            Handles.DrawLine(p1, p2, EditorConstants.LINE_THICKNESS);
#else
            Handles.DrawLine(p1, p2);
#endif
        }
    }
}
