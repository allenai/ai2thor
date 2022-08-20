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
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ControllerOffset))]
    public class ControllerOffsetEditor : UnityEditor.Editor
    {
        private Transform _gripPoint;
        private ControllerOffset _controllerOffset;

        private SerializedProperty _offsetPositionProperty;
        private SerializedProperty _rotationProperty;

        private Pose _cachedPose;

        private const float THICKNESS = 2f;

        private void OnEnable()
        {
            _controllerOffset = target as ControllerOffset;
            _offsetPositionProperty = serializedObject.FindProperty("_offset");
            _rotationProperty = serializedObject.FindProperty("_rotation");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            Transform point = EditorGUILayout.ObjectField("Optional Calculate Offset To", _gripPoint, typeof(Transform), true) as Transform;
            if (point != _gripPoint)
            {
                _gripPoint = point;
                if (_gripPoint != null)
                {
                    Pose offset = _controllerOffset.transform.Delta(_gripPoint);
                    _rotationProperty.quaternionValue = offset.rotation;
                    _offsetPositionProperty.vector3Value = offset.position;
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void OnSceneGUI()
        {
            GetEditorOffset(ref _cachedPose);
            Pose wristPose = _controllerOffset.transform.GetPose();
            _cachedPose.Postmultiply(wristPose);
            DrawAxis(_cachedPose);
        }

        private void DrawAxis(in Pose pose)
        {
            float scale = HandleUtility.GetHandleSize(pose.position);

#if UNITY_2020_2_OR_NEWER
            Handles.color = Color.red;
            Handles.DrawLine(pose.position, pose.position + pose.right * scale, THICKNESS);
            Handles.color = Color.green;
            Handles.DrawLine(pose.position, pose.position + pose.up * scale, THICKNESS);
            Handles.color = Color.blue;
            Handles.DrawLine(pose.position, pose.position + pose.forward * scale, THICKNESS);
#else
            Handles.color = Color.red;
            Handles.DrawLine(pose.position, pose.position + pose.right * scale);
            Handles.color = Color.green;
            Handles.DrawLine(pose.position, pose.position + pose.up * scale);
            Handles.color = Color.blue;
            Handles.DrawLine(pose.position, pose.position + pose.forward * scale);
#endif
        }

        private void GetEditorOffset(ref Pose pose)
        {
            pose.position = _offsetPositionProperty.vector3Value;
            pose.rotation = _rotationProperty.quaternionValue;
        }

    }
}
