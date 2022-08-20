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
    [CustomEditor(typeof(CylinderSurface))]
    [CanEditMultipleObjects]
    public class CylinderEditor : UnityEditor.Editor
    {
        private const float DRAW_SURFACE_ANGULAR_RESOLUTION = 5f;

        private ArcHandle _arcEndHandle = new ArcHandle();
        private ArcHandle _arcStartHandle = new ArcHandle();

        private Vector3[] _surfaceEdges;

        CylinderSurface _surface;

        private void OnEnable()
        {
            _arcStartHandle.SetColorWithRadiusHandle(EditorConstants.PRIMARY_COLOR_DISABLED, 0f);
            _arcEndHandle.SetColorWithRadiusHandle(EditorConstants.PRIMARY_COLOR, 0f);
            _surface = (target as CylinderSurface);
        }

        public void OnSceneGUI()
        {
            DrawEndsCaps(_surface);

            float oldArcStart = _surface.ArcOffset;
            float newArcStart = DrawArcEditor(_surface, _arcStartHandle,
                oldArcStart, Quaternion.LookRotation(_surface.OriginalDir, _surface.Direction));

            _surface.ArcOffset = newArcStart;
            _surface.ArcLength -= newArcStart - oldArcStart;

            _surface.ArcLength = DrawArcEditor(_surface, _arcEndHandle,
                _surface.ArcLength, Quaternion.LookRotation(_surface.StartArcDir, _surface.Direction));

            if (Event.current.type == EventType.Repaint)
            {
                DrawSurfaceVolume(_surface);
            }
        }

        private void DrawEndsCaps(CylinderSurface surface)
        {
            EditorGUI.BeginChangeCheck();

            Quaternion handleRotation = (surface.RelativeTo ?? surface.transform).rotation;

            Vector3 startPosition = Handles.PositionHandle(surface.StartPoint, handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(surface, "Change Start Cylinder Position");
                surface.StartPoint = startPosition;
            }
            EditorGUI.BeginChangeCheck();
            Vector3 endPosition = Handles.PositionHandle(surface.EndPoint, handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(surface, "Change Start Cylinder Position");
                surface.EndPoint = endPosition;
            }
        }

        private void DrawSurfaceVolume(CylinderSurface surface)
        {
            Vector3 start = surface.StartPoint;
            Vector3 end = surface.EndPoint;
            float radius = surface.Radius;

            Handles.color = EditorConstants.PRIMARY_COLOR;
            Handles.DrawWireArc(end,
                surface.Direction,
                surface.StartArcDir,
                surface.ArcLength,
                radius);

            Handles.DrawLine(start, end);
            Handles.DrawLine(start, start + surface.StartArcDir * radius);
            Handles.DrawLine(start, start + surface.EndArcDir * radius);
            Handles.DrawLine(end, end + surface.StartArcDir * radius);
            Handles.DrawLine(end, end + surface.EndArcDir * radius);

            int edgePoints = Mathf.CeilToInt((2 * surface.ArcLength) / DRAW_SURFACE_ANGULAR_RESOLUTION) + 3;
            if (_surfaceEdges == null
                || _surfaceEdges.Length != edgePoints)
            {
                _surfaceEdges = new Vector3[edgePoints];
            }

            Handles.color = EditorConstants.PRIMARY_COLOR_DISABLED;
            int i = 0;
            for (float angle = 0f; angle < surface.ArcLength; angle += DRAW_SURFACE_ANGULAR_RESOLUTION)
            {
                Vector3 direction = Quaternion.AngleAxis(angle, surface.Direction) * surface.StartArcDir;
                _surfaceEdges[i++] = start + direction * radius;
                _surfaceEdges[i++] = end + direction * radius;
            }
            _surfaceEdges[i++] = start + surface.EndArcDir * radius;
            _surfaceEdges[i++] = end + surface.EndArcDir * radius;
            Handles.DrawPolyLine(_surfaceEdges);
        }

        private float DrawArcEditor(CylinderSurface surface, ArcHandle handle, float inputAngle, Quaternion rotation)
        {
            handle.radius = surface.Radius;
            handle.angle = inputAngle;

            Matrix4x4 handleMatrix = Matrix4x4.TRS(
                surface.StartPoint,
                rotation,
                Vector3.one
            );

            using (new Handles.DrawingScope(handleMatrix))
            {
                EditorGUI.BeginChangeCheck();
                handle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(surface, "Change Cylinder Properties");
                    return handle.angle;
                }
            }
            return inputAngle;
        }
    }
}
