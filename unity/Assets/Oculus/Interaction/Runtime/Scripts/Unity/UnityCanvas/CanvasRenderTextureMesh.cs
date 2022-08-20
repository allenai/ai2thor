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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;

namespace Oculus.Interaction.UnityCanvas
{
    [DisallowMultipleComponent]
    public abstract class CanvasRenderTextureMesh : MonoBehaviour
    {
        private static readonly int MainTexShaderID = Shader.PropertyToID("_MainTex");

        [SerializeField]
        protected CanvasRenderTexture _canvasRenderTexture;

        [SerializeField, Optional]
        protected MeshCollider _meshCollider = null;

        [Tooltip("If non-zero it will cause the position of the canvas to be offset by this amount at runtime, while " +
         "the renderer will remain where it was at edit time. This can be used to prevent the two representations from overlapping.")]
        [SerializeField]
        protected Vector3 _runtimeOffset = new Vector3(0, 0, 0);

        protected OVROverlay _overlay;

        private Material _material = null;
        private MeshFilter _imposterFilter;
        private MeshRenderer _imposterRenderer;

        protected bool _started = false;

        protected abstract Vector3 MeshInverseTransform(Vector3 localPosition);

        protected abstract void GenerateMesh(out List<Vector3> verts, out List<int> tris, out List<Vector2> uvs);

        protected abstract void UpdateOverlayPositionAndScale();

        protected abstract OVROverlay.OverlayShape OverlayShape { get; }

        /// <summary>
        /// Transform a position in world space relative to the imposter to an associated position relative
        /// to the original canvas in world space.
        /// </summary>
        public Vector3 ImposterToCanvasTransformPoint(Vector3 worldPosition)
        {
            Vector3 localToImposter =
                _imposterFilter.transform.InverseTransformPoint(worldPosition);
            Vector3 canvasLocalPosition = MeshInverseTransform(localToImposter) /
                                          _canvasRenderTexture.transform.localScale.x;
            Vector3 transformedWorldPosition = _canvasRenderTexture.transform.TransformPoint(canvasLocalPosition);
            return transformedWorldPosition;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(_canvasRenderTexture);
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                CreateMaterial();
                UpdateImposter();

                _canvasRenderTexture.OnUpdateRenderTexture += HandleUpdateRenderTexture;
                if (_canvasRenderTexture.Texture != null)
                {
                    HandleUpdateRenderTexture(_canvasRenderTexture.Texture);
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _canvasRenderTexture.OnUpdateRenderTexture -= HandleUpdateRenderTexture;
                if (_material != null)
                {
                    Destroy(_material);
                    _material = null;
                }
            }
        }

        protected virtual void HandleUpdateRenderTexture(Texture texture)
        {
            if (_imposterRenderer != null)
            {
                _imposterRenderer.material = _material;

                var block = new MaterialPropertyBlock();
                _imposterRenderer.GetPropertyBlock(block);

                block.SetTexture(MainTexShaderID, _canvasRenderTexture.Texture);

                if (_canvasRenderTexture.RenderingMode == RenderingMode.AlphaCutout &&
                    !_canvasRenderTexture.UseAlphaToMask)
                {
                    block.SetFloat("_Cutoff", _canvasRenderTexture.AlphaCutoutThreshold);
                }
                if (_canvasRenderTexture.RenderingMode == RenderingMode.OVR_Underlay &&
                    _canvasRenderTexture.UseEditorEmulation())
                {
                    block.SetFloat("_Cutoff", 0.5f);
                }
                _imposterRenderer.SetPropertyBlock(block);
            }

            UpdateOverlay();
            UpdateImposter();
        }

        protected virtual void UpdateImposter()
        {
            Profiler.BeginSample("InterfaceRenderer.UpdateImposter");
            try
            {
                if (_imposterFilter == null || _imposterRenderer == null)
                {
                    _imposterFilter = gameObject.AddComponent<MeshFilter>();
                    _imposterRenderer = gameObject.AddComponent<MeshRenderer>();
                }
                else
                {
                    _imposterRenderer.gameObject.SetActive(true);
                }

                if (_material != null)
                {
                    _imposterRenderer.material = _material;
                }

                GenerateMesh(out List<Vector3> verts, out List<int> tris, out List<Vector2> uvs);

                Mesh mesh = new Mesh();
                mesh.SetVertices(verts);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(tris, 0);

                mesh.RecalculateBounds();
                mesh.RecalculateNormals();

                _imposterFilter.mesh = mesh;
                if (_meshCollider != null)
                {
                    _meshCollider.sharedMesh = _imposterFilter.sharedMesh;
                }
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        protected void CreateMaterial()
        {
            Profiler.BeginSample("InterfaceRenderer.UpdateMaterial");
            try
            {
                string shaderName;
                switch (_canvasRenderTexture.RenderingMode)
                {
                    case RenderingMode.AlphaBlended:
                        shaderName = "Hidden/Imposter_AlphaBlended";
                        break;
                    case RenderingMode.AlphaCutout:
                        if (_canvasRenderTexture.UseAlphaToMask)
                        {
                            shaderName = "Hidden/Imposter_AlphaToMask";
                        }
                        else
                        {
                            shaderName = "Hidden/Imposter_AlphaCutout";
                        }

                        break;
                    case RenderingMode.Opaque:
                        shaderName = "Hidden/Imposter_Opaque";
                        break;
                    case RenderingMode.OVR_Underlay:
                        if (_canvasRenderTexture.UseEditorEmulation())
                        {
                            shaderName = "Hidden/Imposter_AlphaCutout";
                        }
                        else if (_canvasRenderTexture.DoUnderlayAntiAliasing)
                        {
                            shaderName = "Hidden/Imposter_Underlay_AA";
                        }
                        else
                        {

                            shaderName = "Hidden/Imposter_Underlay";
                        }

                        break;
                    case RenderingMode.OVR_Overlay:
                        shaderName = "Hidden/Imposter_AlphaCutout";
                        break;
                    default:
                        throw new Exception();
                }

                _material = new Material(Shader.Find(shaderName));
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        protected void UpdateOverlay()
        {
            Profiler.BeginSample("InterfaceRenderer.UpdateOverlay");
            try
            {
                if (!_canvasRenderTexture.ShouldUseOVROverlay)
                {
                    _overlay?.gameObject?.SetActive(false);
                    return;
                }

                if (_overlay == null)
                {
                    GameObject overlayObj = CreateChildObject("__Overlay");
                    _overlay = overlayObj.AddComponent<OVROverlay>();
                    _overlay.isAlphaPremultiplied = !Application.isMobilePlatform;
                }
                else
                {
                    _overlay.gameObject.SetActive(true);
                }

                bool useUnderlayRendering = _canvasRenderTexture.RenderingMode == RenderingMode.OVR_Underlay;
                _overlay.textures = new Texture[1] { _canvasRenderTexture.Texture };
                _overlay.noDepthBufferTesting = useUnderlayRendering;
                _overlay.currentOverlayType = useUnderlayRendering ? OVROverlay.OverlayType.Underlay : OVROverlay.OverlayType.Overlay;
                _overlay.currentOverlayShape = OverlayShape;
                _overlay.useExpensiveSuperSample = _canvasRenderTexture.EnableSuperSampling;

                UpdateOverlayPositionAndScale();
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        protected GameObject CreateChildObject(string name)
        {
            GameObject obj = new GameObject(name);

            obj.transform.SetParent(transform);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;

            return obj;
        }

        #region Inject

        public void InjectAllCanvasRenderTextureMesh(CanvasRenderTexture canvasRenderTexture)
        {
            InjectCanvasRenderTexture(canvasRenderTexture);
        }

        public void InjectCanvasRenderTexture(CanvasRenderTexture canvasRenderTexture)
        {
            _canvasRenderTexture = canvasRenderTexture;
        }

        public void InjectOptionalMeshCollider(MeshCollider meshCollider)
        {
            _meshCollider = meshCollider;
        }

        #endregion
    }
}
