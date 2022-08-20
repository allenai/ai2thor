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

using System;
using UnityEngine.Assertions;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.UnityCanvas
{
    public class CanvasCylinder : CanvasRenderTextureMesh, ICurvedPlane
    {
        [Serializable]
        public struct MeshGenerationSettings
        {
            [Delayed]
            public float VerticesPerDegree;

            [Delayed]
            public int MaxHorizontalResolution;

            [Delayed]
            public int MaxVerticalResolution;
        }

        public const int MIN_RESOLUTION = 2;

        [SerializeField]
        private MeshGenerationSettings _meshGeneration = new MeshGenerationSettings()
        {
            VerticesPerDegree = 1.4f,
            MaxHorizontalResolution = 128,
            MaxVerticalResolution = 32
        };

        [SerializeField]
        private Cylinder _cylinder;

        [SerializeField]
        private CylinderOrientation _orientation;

        protected override OVROverlay.OverlayShape OverlayShape => OVROverlay.OverlayShape.Cylinder;

        public float Radius => _cylinder.Radius;
        public Cylinder Cylinder => _cylinder;
        public float ArcDegrees { get; private set; }
        public float Rotation { get; private set; }
        public float Bottom { get; private set; }
        public float Top { get; private set; }

        private float CylinderRelativeScale => _cylinder.transform.lossyScale.x / transform.lossyScale.x;

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            Assert.IsNotNull(_cylinder);
            this.EndStart(ref _started);
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            _meshGeneration.MaxHorizontalResolution = Mathf.Max(MIN_RESOLUTION,
                _meshGeneration.MaxHorizontalResolution);
            _meshGeneration.MaxVerticalResolution = Mathf.Max(MIN_RESOLUTION,
                _meshGeneration.MaxVerticalResolution);
            _meshGeneration.VerticesPerDegree = Mathf.Max(0, _meshGeneration.VerticesPerDegree);

            if (Application.isPlaying && _started)
            {
                EditorApplication.delayCall += () =>
                {
                    UpdateImposter();
                };
            }
        }
#endif

        protected override void UpdateImposter()
        {
            base.UpdateImposter();
            UpdateMeshPosition();
            UpdateCurvedPlane();
        }

        protected override void UpdateOverlayPositionAndScale()
        {
            if (_overlay == null)
            {
                return;
            }

            Vector2Int resolution = _canvasRenderTexture.GetBaseResolutionToUse();
            _overlay.transform.localPosition = new Vector3(0, 0, -Radius) - _runtimeOffset;
            _overlay.transform.localScale =
                new Vector3(_canvasRenderTexture.PixelsToUnits(resolution.x) / transform.lossyScale.x,
                            _canvasRenderTexture.PixelsToUnits(resolution.y) / transform.lossyScale.y,
                            Radius);
        }

        protected override Vector3 MeshInverseTransform(Vector3 localPosition)
        {
            float angle = Mathf.Atan2(localPosition.x, localPosition.z + Radius);
            float x = angle * Radius;
            float y = localPosition.y;
            return new Vector3(x, y);
        }

        protected override void GenerateMesh(out List<Vector3> verts,
                                             out List<int> tris,
                                             out List<Vector2> uvs)
        {
            verts = new List<Vector3>();
            tris = new List<int>();
            uvs = new List<Vector2>();

            Vector2 worldSize = GetWorldSize();
            float scaledRadius = Radius * CylinderRelativeScale;

            float xPos = worldSize.x * 0.5f;
            float xNeg = -xPos;
            float yPos = worldSize.y * 0.5f;
            float yNeg = -yPos;

            Vector2Int GetClampedResolution(float arcMax, float axisMax)
            {
                int horizontalResolution = Mathf.Max(2,
                    Mathf.RoundToInt(_meshGeneration.VerticesPerDegree *
                    Mathf.Rad2Deg * arcMax / scaledRadius));
                int verticalResolution =
                    Mathf.Max(2, Mathf.RoundToInt(horizontalResolution * axisMax / arcMax));

                horizontalResolution = Mathf.Clamp(horizontalResolution, 2,
                    _meshGeneration.MaxHorizontalResolution);
                verticalResolution = Mathf.Clamp(verticalResolution, 2,
                    _meshGeneration.MaxVerticalResolution);

                return new Vector2Int(horizontalResolution, verticalResolution);
            }

            Vector3 GetCurvedPoint(float u, float v)
            {
                float x = Mathf.Lerp(xNeg, xPos, u);
                float y = Mathf.Lerp(yNeg, yPos, v);

                float angle;
                Vector3 point;

                switch (_orientation)
                {
                    default:
                    case CylinderOrientation.Vertical:
                        angle = x / scaledRadius;
                        point.x = Mathf.Sin(angle) * scaledRadius;
                        point.y = y;
                        point.z = Mathf.Cos(angle) * scaledRadius - scaledRadius;
                        break;
                    case CylinderOrientation.Horizontal:
                        angle = y / scaledRadius;
                        point.x = x;
                        point.y = Mathf.Sin(angle) * scaledRadius;
                        point.z = Mathf.Cos(angle) * scaledRadius - scaledRadius;
                        break;
                }
                return point;
            }

            Vector2Int resolution;
            switch (_orientation)
            {
                default:
                case CylinderOrientation.Vertical:
                    resolution = GetClampedResolution(xPos, yPos);
                    break;
                case CylinderOrientation.Horizontal:
                    resolution = GetClampedResolution(yPos, xPos);
                    break;
            }

            for (int y = 0; y < resolution.y; y++)
            {
                for (int x = 0; x < resolution.x; x++)
                {
                    float u = x / (resolution.x - 1.0f);
                    float v = y / (resolution.y - 1.0f);

                    verts.Add(GetCurvedPoint(u, v));
                    uvs.Add(new Vector2(u, v));
                }
            }

            for (int y = 0; y < resolution.y - 1; y++)
            {
                for (int x = 0; x < resolution.x - 1; x++)
                {
                    int v00 = x + y * resolution.x;
                    int v10 = v00 + 1;
                    int v01 = v00 + resolution.x;
                    int v11 = v00 + 1 + resolution.x;

                    tris.Add(v00);
                    tris.Add(v11);
                    tris.Add(v10);

                    tris.Add(v00);
                    tris.Add(v01);
                    tris.Add(v11);
                }
            }
        }

        private void UpdateMeshPosition()
        {
            Vector3 posInCylinder = _cylinder.transform.InverseTransformPoint(transform.position);

            Vector3 localYOffset = new Vector3(0, posInCylinder.y, 0);
            Vector3 localCancelY = posInCylinder - localYOffset;

            // If canvas position is on cylinder center axis, project forward.
            // Otherwise, project canvas onto cylinder wall from center axis.
            Vector3 projection = Mathf.Approximately(localCancelY.sqrMagnitude, 0f) ?
                                 Vector3.forward : localCancelY.normalized;

            Vector3 localUp;
            switch (_orientation)
            {
                default:
                case CylinderOrientation.Vertical:
                    localUp = Vector3.up;
                    break;
                case CylinderOrientation.Horizontal:
                    localUp = Vector3.right;
                    break;
            }

            transform.position = _cylinder.transform.TransformPoint((projection * _cylinder.Radius) + localYOffset);
            transform.rotation = _cylinder.transform.rotation * Quaternion.LookRotation(projection, localUp);

            if (_meshCollider != null &&
                _meshCollider.transform != transform &&
                !transform.IsChildOf(_meshCollider.transform))
            {
                _meshCollider.transform.position = transform.position;
                _meshCollider.transform.rotation = transform.rotation;
                _meshCollider.transform.localScale *= transform.lossyScale.x / _meshCollider.transform.lossyScale.x;
            }
        }

        private Vector2 GetWorldSize()
        {
            Vector2Int resolution = _canvasRenderTexture.GetBaseResolutionToUse();
            float width = _canvasRenderTexture.PixelsToUnits(Mathf.RoundToInt(resolution.x));
            float height = _canvasRenderTexture.PixelsToUnits(Mathf.RoundToInt(resolution.y));
            return new Vector2(width, height) / transform.lossyScale;
        }

        private void UpdateCurvedPlane()
        {
            // Get world size in cylinder space
            Vector2 cylinderSize = GetWorldSize() / CylinderRelativeScale;

            float arcSize, axisSize;
            switch (_orientation)
            {
                default:
                case CylinderOrientation.Vertical:
                    arcSize = cylinderSize.x;
                    axisSize = cylinderSize.y;
                    break;
                case CylinderOrientation.Horizontal:
                    arcSize = cylinderSize.y;
                    axisSize = cylinderSize.x;
                    break;
            }

            Vector3 posInCylinder = Cylinder.transform.InverseTransformPoint(transform.position);
            Rotation = Mathf.Atan2(posInCylinder.x, posInCylinder.z) * Mathf.Rad2Deg;
            ArcDegrees = (arcSize * 0.5f / Radius) * 2f * Mathf.Rad2Deg;
            Top = posInCylinder.y + (axisSize * 0.5f);
            Bottom = posInCylinder.y - (axisSize * 0.5f);
        }

        #region Inject

        public void InjectAllCanvasCylinder(CanvasRenderTexture canvasRenderTexture,
                                            Cylinder cylinder,
                                            CylinderOrientation orientation)
        {
            InjectAllCanvasRenderTextureMesh(canvasRenderTexture);
            InjectCylinder(cylinder);
            InjectOrientation(orientation);
        }

        public void InjectCylinder(Cylinder cylinder)
        {
            _cylinder = cylinder;
        }

        public void InjectOrientation(CylinderOrientation orientation)
        {
            _orientation = orientation;
        }

        #endregion
    }
}
