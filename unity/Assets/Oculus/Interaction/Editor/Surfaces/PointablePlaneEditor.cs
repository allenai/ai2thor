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
    [CustomEditor(typeof(PointablePlane))]
    public class PointablePlaneEditor : UnityEditor.Editor
    {
        private const int NUM_SEGMENTS = 20;
        private const float FADE_DISTANCE = 20f;

        private static readonly Color Color = Color.green * 0.8f;

        public void OnSceneGUI()
        {
            PointablePlane plane = target as PointablePlane;
            Draw(plane);
        }

        public static void Draw(PointablePlane plane)
        {
            Vector3 origin = plane.transform.position;

            if (SceneView.lastActiveSceneView?.camera != null)
            {
                Vector3 sceneCamPos = SceneView.lastActiveSceneView.camera.transform.position;
                if (plane.ClosestSurfacePoint(sceneCamPos, out SurfaceHit hit, 0))
                {
                    origin = hit.Point;
                }
            }

            DrawLines(origin, plane.Normal, plane.transform.up, FADE_DISTANCE, Color);
            DrawLines(origin, plane.Normal, -plane.transform.up, FADE_DISTANCE, Color);
            DrawLines(origin, plane.Normal, plane.transform.right, FADE_DISTANCE, Color);
            DrawLines(origin, plane.Normal, -plane.transform.right, FADE_DISTANCE, Color);
        }

        private static void DrawLines(in Vector3 origin,
                                      in Vector3 normal,
                                      in Vector3 direction,
                                      in float fadeDistance,
                                      in Color baseColor)
        {
            Color prevColor = Handles.color;
            Color color = baseColor;
            Vector3 offsetOrigin = origin;

            for (int i = 0; i < NUM_SEGMENTS; ++i)
            {
                Handles.color = color;

                Vector3 cross = Vector3.Cross(normal, direction).normalized;
                float interval = fadeDistance / NUM_SEGMENTS;

                for (int j = -NUM_SEGMENTS; j < NUM_SEGMENTS; ++j)
                {
                    float horizStart = interval * j;
                    float horizEnd = horizStart + interval;

                    Vector3 start = offsetOrigin + cross * horizStart;
                    Vector3 end = offsetOrigin + cross * horizEnd;

                    color.a = 1f - Mathf.Abs((float)j / NUM_SEGMENTS);
                    color.a *= 1f - ((float)i / NUM_SEGMENTS);
                    color.a *= color.a;

                    Handles.color = color;
                    Handles.DrawLine(start, end);
                }

                offsetOrigin += direction * interval;
                color = baseColor;
            }

            Handles.color = prevColor;
        }
    }
}
