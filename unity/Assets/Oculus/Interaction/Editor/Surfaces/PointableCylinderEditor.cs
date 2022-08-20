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

using UnityEngine;
using UnityEditor;
using Oculus.Interaction.Surfaces;

namespace Oculus.Interaction.Editor
{
    [CustomEditor(typeof(PointableCylinder))]
    public class PointableCylinderEditor : UnityEditor.Editor
    {
        private const int NUM_SEGMENTS = 30;

        private static readonly Color ValidColor = Color.green * 0.8f;

        private static readonly Color InvalidColor = Color.red * 0.8f;

        public void OnSceneGUI()
        {
            PointableCylinder cylinder = target as PointableCylinder;

            if (cylinder.Cylinder != null)
            {
                Draw(cylinder);
            }
        }

        public static void Draw(PointableCylinder pointable)
        {
            Color prevColor = Handles.color;
            Handles.color = pointable.IsValid ? ValidColor : InvalidColor;

            float gizmoHeight = pointable.Height;
            float camYOffset = 0;
            bool infiniteHeight = pointable.Height <= 0;

            if (infiniteHeight && SceneView.lastActiveSceneView?.camera != null)
            {
                gizmoHeight = 1000f;
                Vector3 sceneCamPos = SceneView.lastActiveSceneView.camera.transform.position;
                camYOffset = pointable.Cylinder.transform.InverseTransformPoint(sceneCamPos).y;
            }

            for (int i = 0; i < 2; ++i)
            {
                bool isTop = i == 1;
                float y = isTop ? gizmoHeight / 2 : -gizmoHeight / 2;
                int numSegments = (int)(NUM_SEGMENTS * Mathf.Max(pointable.Radius / 2, 1));
                Vector3 prevSegmentWorld = Vector3.zero;

                for (int seg = 0; seg <= numSegments; ++seg)
                {
                    float ratio = (float)seg / numSegments * Mathf.PI * 2;
                    float x = Mathf.Cos(ratio) * pointable.Radius;
                    float z = Mathf.Sin(ratio) * pointable.Radius;
                    Vector3 curSegmentLocal = new Vector3(x, y + camYOffset, z);
                    Vector3 curSegmentWorld = pointable.Cylinder.transform.TransformPoint(curSegmentLocal);

                    if (isTop) // Draw connecting lines from top circle
                    {
                        Vector3 bottomVert = new Vector3(curSegmentLocal.x,
                                                         curSegmentLocal.y - gizmoHeight,
                                                         curSegmentLocal.z);
                        bottomVert = pointable.Cylinder.transform.TransformPoint(bottomVert);
                        Handles.DrawLine(curSegmentWorld, bottomVert);
                    }

                    if (seg > 0 && !infiniteHeight)
                    {
                        Handles.DrawLine(curSegmentWorld, prevSegmentWorld);
                    }

                    prevSegmentWorld = curSegmentWorld;
                }
            }

            Handles.color = prevColor;
        }
    }
}
