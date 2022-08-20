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
    [CustomEditor(typeof(ConicalFrustum))]
    public class ConicalFrustumEditor : UnityEditor.Editor
    {
        private ConicalFrustum _frustum;

        private SerializedProperty _minLengthProperty;
        private SerializedProperty _maxLengthProperty;

        private const float SURFACE_SPACING = 10f;

        private void Awake()
        {
            _frustum = target as ConicalFrustum;

            _minLengthProperty = serializedObject.FindProperty("_minLength");
            _maxLengthProperty = serializedObject.FindProperty("_maxLength");
        }

        private void OnSceneGUI()
        {
            if (Event.current.type == EventType.Repaint)
            {
                Handles.color = EditorConstants.PRIMARY_COLOR;
                DrawConeFrustrum();
            }
        }

        private void DrawConeFrustrum()
        {
            Vector3 origin = _frustum.Pose.position;
            Vector3 direction = _frustum.Pose.forward;
            Vector3 tangent = _frustum.Pose.up;

            float minLength = _minLengthProperty.floatValue;
            float maxLength = _maxLengthProperty.floatValue;

            Vector3 start = origin + direction * minLength;
            Vector3 end = origin + direction * maxLength;

            float minRadius = _frustum.ConeFrustumRadiusAtLength(minLength);
            float maxRadius = _frustum.ConeFrustumRadiusAtLength(maxLength);

            Handles.DrawLine(start, end);

            for (float i = 0; i < 360; i += SURFACE_SPACING)
            {
                Vector3 rotatedTangent = Quaternion.AngleAxis(i, direction) * tangent;
                Handles.DrawLine(
                    start + rotatedTangent * minRadius,
                    end + rotatedTangent * maxRadius);
            }
            Handles.DrawWireDisc(start, direction, minRadius);
            Handles.DrawWireDisc(end, direction, maxRadius);
        }
    }
}
