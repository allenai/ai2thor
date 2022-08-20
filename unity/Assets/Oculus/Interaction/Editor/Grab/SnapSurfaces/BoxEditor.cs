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

using Oculus.Interaction.Editor;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Oculus.Interaction.HandGrab.SnapSurfaces.Editor
{
    [CustomEditor(typeof(BoxSurface))]
    [CanEditMultipleObjects]
    public class BoxEditor : UnityEditor.Editor
    {
        private BoxBoundsHandle _boxHandle = new BoxBoundsHandle();
        private BoxSurface _surface;

        private void OnEnable()
        {
            _boxHandle.handleColor = EditorConstants.PRIMARY_COLOR;
            _boxHandle.wireframeColor = EditorConstants.PRIMARY_COLOR_DISABLED;
            _boxHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Z;

            _surface = (target as BoxSurface);
        }

        public void OnSceneGUI()
        {
            DrawRotator(_surface);
            DrawBoxEditor(_surface);
            DrawSlider(_surface);

            if (Event.current.type == EventType.Repaint)
            {
                DrawSnapLines(_surface);
            }
        }

        private void DrawSnapLines(BoxSurface surface)
        {
            Handles.color = EditorConstants.PRIMARY_COLOR;

            Vector3 rightAxis = surface.Rotation * Vector3.right;
            Vector3 forwardAxis = surface.Rotation * Vector3.forward;
            Vector3 forwardOffset = forwardAxis * surface.Size.z;

            Vector3 bottomLeft = surface.transform.position - rightAxis * surface.Size.x * (1f - surface.WidthOffset);
            Vector3 bottomRight = surface.transform.position + rightAxis * surface.Size.x * (surface.WidthOffset);
            Vector3 topLeft = bottomLeft + forwardOffset;
            Vector3 topRight = bottomRight + forwardOffset;

            Handles.DrawLine(bottomLeft + rightAxis * surface.SnapOffset.y, bottomRight + rightAxis * surface.SnapOffset.x);
            Handles.DrawLine(topLeft - rightAxis * surface.SnapOffset.x, topRight - rightAxis * surface.SnapOffset.y);
            Handles.DrawLine(bottomLeft - forwardAxis * surface.SnapOffset.z, topLeft - forwardAxis * surface.SnapOffset.w);
            Handles.DrawLine(bottomRight + forwardAxis * surface.SnapOffset.w, topRight + forwardAxis * surface.SnapOffset.z);
        }

        private void DrawSlider(BoxSurface surface)
        {
            Handles.color = EditorConstants.PRIMARY_COLOR;

            EditorGUI.BeginChangeCheck();
            Vector3 rightDir = surface.Rotation * Vector3.right;
            Vector3 forwardDir = surface.Rotation * Vector3.forward;
            Vector3 bottomRight = surface.transform.position
                + rightDir * surface.Size.x * (surface.WidthOffset);
            Vector3 bottomLeft = surface.transform.position
                - rightDir * surface.Size.x * (1f - surface.WidthOffset);
            Vector3 topRight = bottomRight + forwardDir * surface.Size.z;

            Vector3 rightHandle = DrawOffsetHandle(bottomRight + rightDir * surface.SnapOffset.x, rightDir);
            Vector3 leftHandle = DrawOffsetHandle(bottomLeft + rightDir * surface.SnapOffset.y, -rightDir);
            Vector3 topHandle = DrawOffsetHandle(topRight + forwardDir * surface.SnapOffset.z, forwardDir);
            Vector3 bottomHandle = DrawOffsetHandle(bottomRight + forwardDir * surface.SnapOffset.w, -forwardDir);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(surface, "Change Offset Box");
                Vector4 offset = surface.SnapOffset;
                offset.x = DistanceToHandle(bottomRight, rightHandle, rightDir);
                offset.y = DistanceToHandle(bottomLeft, leftHandle, rightDir);
                offset.z = DistanceToHandle(topRight, topHandle, forwardDir);
                offset.w = DistanceToHandle(bottomRight, bottomHandle, forwardDir);
                surface.SnapOffset = offset;
            }
        }

        private Vector3 DrawOffsetHandle(Vector3 point, Vector3 dir)
        {
            float size = HandleUtility.GetHandleSize(point) * 0.2f;
            return Handles.Slider(point, dir, size, Handles.ConeHandleCap, 0f);
        }

        private float DistanceToHandle(Vector3 origin, Vector3 handlePoint, Vector3 dir)
        {
            float distance = Vector3.Distance(origin, handlePoint);
            if (Vector3.Dot(handlePoint - origin, dir) < 0f)
            {
                distance = -distance;
            }
            return distance;
        }

        private void DrawRotator(BoxSurface surface)
        {
            EditorGUI.BeginChangeCheck();
            Quaternion rotation = Handles.RotationHandle(surface.Rotation, surface.transform.position);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(surface, "Change Rotation Box");
                surface.Rotation = rotation;
            }
        }

        private void DrawBoxEditor(BoxSurface surface)
        {
            Quaternion rot = surface.Rotation;
            Vector3 size = surface.Size;

            Vector3 snapP = surface.transform.position;

            _boxHandle.size = size;
            float widthPos = Mathf.Lerp(-size.x * 0.5f, size.x * 0.5f, surface.WidthOffset);
            _boxHandle.center = new Vector3(widthPos, 0f, size.z * 0.5f);

            Matrix4x4 handleMatrix = Matrix4x4.TRS(
                snapP,
                rot,
                Vector3.one
            );

            using (new Handles.DrawingScope(handleMatrix))
            {
                EditorGUI.BeginChangeCheck();
                _boxHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(surface, "Change Box Properties");

                    surface.Size = _boxHandle.size;
                    float width = _boxHandle.size.x;
                    surface.WidthOffset = width != 0f ? (_boxHandle.center.x + width * 0.5f) / width : 0f;
                }
            }
        }
        private float RemapClamped(float value, (float, float) from, (float, float) to)
        {
            value = Mathf.Clamp(value, from.Item1, from.Item2);
            return to.Item1 + (value - from.Item1) * (to.Item2 - to.Item1) / (from.Item2 - from.Item1);
        }
    }
}
