/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using Oculus.Interaction.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.HandGrab.SnapSurfaces.Editor
{
    [CustomEditor(typeof(BezierSurface))]
    [CanEditMultipleObjects]
    public class BezierSurfaceEditor : UnityEditor.Editor
    {
        BezierSurface _surface;

        private int _selectedIndex = -1;
        private const float PICK_SIZE = 0.1f;
        private const float AXIS_SIZE = 0.5f;
        private const int CURVE_STEPS = 50;

        private bool IsSelectedIndexValid => _selectedIndex >= 0 && _selectedIndex < _surface.ControlPoints.Count;

        private void OnEnable()
        {
            _surface = (target as BezierSurface);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Add ControlPoint At Start"))
            {
                AddControlPoint(true);
            }
            if (GUILayout.Button("Add ControlPoint At End"))
            {
                AddControlPoint(false);
            }

            if (!IsSelectedIndexValid)
            {
                _selectedIndex = -1;
                GUILayout.Label($"No Selected Point");
            }
            else
            {
                GUILayout.Label($"Selected Point: {_selectedIndex}");
                if (GUILayout.Button("Align Selected Tangent"))
                {
                    AlignTangent(_selectedIndex);
                }
                if (GUILayout.Button("Smooth Selected Tangent"))
                {
                    SmoothTangent(_selectedIndex);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }


        public void OnSceneGUI()
        {
            Handles.color = EditorConstants.PRIMARY_COLOR;

            Pose relativePose = _surface.transform.GetPose();
            DrawEndsCaps(_surface.ControlPoints, relativePose);
            if (Event.current.type == EventType.Repaint)
            {
                DrawCurve(_surface.ControlPoints, relativePose);
            }
        }

        private void AddControlPoint(bool addFirst)
        {
            BezierControlPoint controlPoint = BezierControlPoint.DEFAULT;
            if (_surface.ControlPoints.Count == 1)
            {
                controlPoint = _surface.ControlPoints[0];
                controlPoint.pose.position += Vector3.forward;
            }
            else if (_surface.ControlPoints.Count > 1)
            {
                BezierControlPoint firstControlPoint;
                BezierControlPoint secondControlPoint;
                if (addFirst)
                {
                    firstControlPoint = _surface.ControlPoints[1];
                    secondControlPoint = _surface.ControlPoints[0];
                }
                else
                {
                    firstControlPoint = _surface.ControlPoints[_surface.ControlPoints.Count - 2];
                    secondControlPoint = _surface.ControlPoints[_surface.ControlPoints.Count - 1];
                }

                controlPoint.pose.position = 2 * secondControlPoint.pose.position - firstControlPoint.pose.position;
                controlPoint.pose.rotation = secondControlPoint.pose.rotation;
            }

            if (addFirst)
            {
                _surface.ControlPoints.Insert(0, controlPoint);
                _selectedIndex = 0;
            }
            else
            {
                _surface.ControlPoints.Add(controlPoint);
                _selectedIndex = _surface.ControlPoints.Count - 1;
            }
            AlignTangent(_selectedIndex);
        }

        private void AlignTangent(int index)
        {
            BezierControlPoint controlPoint = _surface.ControlPoints[index];
            BezierControlPoint nextControlPoint = _surface.ControlPoints[(index + 1) % _surface.ControlPoints.Count];

            controlPoint.tangentPoint = (nextControlPoint.pose.position - controlPoint.pose.position) * 0.5f;
            _surface.ControlPoints[index] = controlPoint;
        }


        private void SmoothTangent(int index)
        {
            BezierControlPoint controlPoint = _surface.ControlPoints[index];
            BezierControlPoint prevControlPoint = _surface.ControlPoints[(index + _surface.ControlPoints.Count - 1) % _surface.ControlPoints.Count];

            Vector3 prevTangent = prevControlPoint.pose.position + prevControlPoint.tangentPoint;
            controlPoint.tangentPoint = (controlPoint.pose.position - prevTangent) * 0.5f;
            _surface.ControlPoints[index] = controlPoint;
        }

        private void DrawEndsCaps(List<BezierControlPoint> controlPoints, in Pose relativePose)
        {
            Handles.color = EditorConstants.PRIMARY_COLOR;
            for (int i = 0; i < controlPoints.Count; i++)
            {
                DrawControlPoint(i, relativePose);
            }

            Handles.color = EditorConstants.PRIMARY_COLOR_DISABLED;
            if (IsSelectedIndexValid)
            {
                DrawControlPointHandles(_selectedIndex, relativePose);
                DrawTangentLine(_selectedIndex, relativePose);
            }
        }

        private void DrawCurve(List<BezierControlPoint> controlPoints, in Pose relativePose)
        {
            Handles.color = EditorConstants.PRIMARY_COLOR;
            for (int i = 0; i < controlPoints.Count && controlPoints.Count > 1; i++)
            {
                BezierControlPoint fromControlPoint = _surface.ControlPoints[i];
                Pose from = fromControlPoint.WorldSpacePose(relativePose);

                BezierControlPoint toControlPoint = _surface.ControlPoints[(i + 1) % controlPoints.Count];
                if (toControlPoint.disconnected)
                {
                    continue;
                }

                Pose to = toControlPoint.WorldSpacePose(relativePose);
                Vector3 tangent = from.position + relativePose.rotation * fromControlPoint.tangentPoint;
                DrawBezier(from.position, tangent, to.position, CURVE_STEPS);
            }
        }

        private void DrawBezier(Vector3 start, Vector3 middle, Vector3 end, int steps)
        {
            Vector3 from = start;
            Vector3 to;
            float t;
            for (int i = 1; i < steps; i++)
            {
                t = i / (steps - 1f);
                to = BezierSurface.EvaluateBezier(start, middle, end, t);

#if UNITY_2020_2_OR_NEWER
                Handles.DrawLine(from, to, EditorConstants.LINE_THICKNESS);
#else
                Handles.DrawLine(from, to);
#endif
                from = to;
            }
        }

        private void DrawTangentLine(int index, in Pose relativePose)
        {
            BezierControlPoint controlPoint = _surface.ControlPoints[index];
            Pose pose = controlPoint.WorldSpacePose(relativePose);
            Vector3 center = pose.position;
            Vector3 tangent = pose.position + relativePose.rotation * controlPoint.tangentPoint;

#if UNITY_2020_2_OR_NEWER
            Handles.DrawLine(center, tangent, EditorConstants.LINE_THICKNESS);
#else
            Handles.DrawLine(center, tangent);
#endif
        }

        private void DrawControlPoint(int index, in Pose relativePose)
        {
            BezierControlPoint controlPoint = _surface.ControlPoints[index];
            Pose pose = controlPoint.WorldSpacePose(relativePose);
            float handleSize = HandleUtility.GetHandleSize(pose.position);

            Handles.color = EditorConstants.PRIMARY_COLOR;
            if (Handles.Button(pose.position, pose.rotation, handleSize * PICK_SIZE, handleSize * PICK_SIZE, Handles.DotHandleCap))
            {
                _selectedIndex = index;
            }

            Handles.color = Color.red;
            Handles.DrawLine(pose.position, pose.position + pose.right * handleSize * AXIS_SIZE);
            Handles.color = Color.green;
            Handles.DrawLine(pose.position, pose.position + pose.up * handleSize * AXIS_SIZE);
            Handles.color = Color.blue;
            Handles.DrawLine(pose.position, pose.position + pose.forward * handleSize * AXIS_SIZE);
        }

        private void DrawControlPointHandles(int index, in Pose relativePose)
        {
            BezierControlPoint controlPoint = _surface.ControlPoints[index];
            Pose pose = controlPoint.WorldSpacePose(relativePose);
            if (Tools.current == Tool.Move)
            {
                EditorGUI.BeginChangeCheck();
                Quaternion pointRotation = Tools.pivotRotation == PivotRotation.Global ? Quaternion.identity : pose.rotation;
                pose.position = Handles.PositionHandle(pose.position, pointRotation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_surface, "Change ControlPoint Position");
                    controlPoint.pose.position = Quaternion.Inverse(relativePose.rotation) * (pose.position - relativePose.position);
                    _surface.ControlPoints[index] = controlPoint;
                }
            }
            else if (Tools.current == Tool.Rotate)
            {
                EditorGUI.BeginChangeCheck();
                pose.rotation = Handles.RotationHandle(pose.rotation, pose.position);
                pose.rotation.Normalize();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_surface, "Change ControlPoint Rotation");
                    controlPoint.pose.rotation = (Quaternion.Inverse(relativePose.rotation) * pose.rotation);
                    _surface.ControlPoints[index] = controlPoint;
                }
            }

            Vector3 tangent = pose.position + relativePose.rotation * controlPoint.tangentPoint;
            Quaternion tangentRotation = Tools.pivotRotation == PivotRotation.Global ? Quaternion.identity : relativePose.rotation;
            EditorGUI.BeginChangeCheck();
            tangent = Handles.PositionHandle(tangent, tangentRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_surface, "Change ControlPoint Tangent");
                controlPoint.tangentPoint = Quaternion.Inverse(relativePose.rotation) * (tangent - pose.position);
                _surface.ControlPoints[index] = controlPoint;
            }
        }
    }
}
