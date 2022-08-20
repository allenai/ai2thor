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
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Oculus.Interaction.UnityCanvas
{
    [RequireComponent(typeof(Canvas))]
    [DisallowMultipleComponent]
    public class CanvasRenderTexture : UIBehaviour
    {
        public enum DriveMode
        {
            Auto,
            Manual
        }

        public const int DEFAULT_UI_LAYERMASK = 1 << 5; //Hardcoded as the UI layer in Unity.

        private static readonly Vector2Int DEFAULT_TEXTURE_RES = new Vector2Int(128, 128);

        [Tooltip("If you need extra resolution, you can use this as a whole-integer multiplier of the final resolution used to render the texture.")]
        [Range(1, 3)]
        [Delayed]
        [SerializeField]
        private int _renderScale = 1;

        [SerializeField]
        private DriveMode _dimensionsDriveMode = DriveMode.Auto;

        [Tooltip("The exact pixel resolution of the texture used for interface rendering.  If set to auto this will take the size of the attached " +
        "RectTransform into consideration, in addition to the configured pixel-to-meter ratio.")]
        [Delayed]
        [SerializeField]
        private Vector2Int _resolution = DEFAULT_TEXTURE_RES;

        [SerializeField]
        private int _pixelsPerUnit = 100;

        [SerializeField]
        private RenderingMode _renderingMode = RenderingMode.AlphaCutout;

        [Tooltip("Whether or not mip-maps should be auto-generated for the texture.  Can help aliasing if the texture can be " +
        "viewed from many difference distances.")]
        [SerializeField]
        private bool _generateMipMaps = false;

        [Tooltip(
        "Requires MSAA.  Provides limited transparency useful for anti-aliasing soft edges of UI elements.")]
        [SerializeField]
        private bool _useAlphaToMask = true;

        [Range(0, 1)]
        [SerializeField]
        private float _alphaCutoutThreshold = 0.5f;

        [Tooltip(
        "Uses a more expensive image sampling technique for improved quality at the cost of performance.")]
        [SerializeField]
        protected bool _enableSuperSampling = true;

        [Tooltip(
        "Attempts to anti-alias the edges of the underlay by using alpha blending.  Can cause borders of " +
        "darkness around partially transparent objects.")]
        [SerializeField]
        private bool _doUnderlayAntiAliasing = false;

        [Tooltip(
        "OVR Layers can provide a buggy or less ideal workflow while in the editor.  This option allows you " +
        "emulate the layer rendering while in the editor, while still using the OVR Layer rendering in a build.")]
        [SerializeField]
        private bool _emulateWhileInEditor = true;

        [Header("Rendering Settings")]
        [Tooltip("The layers to render when the rendering texture is created.  All child renderers should be part of this mask.")]
        [SerializeField]
        private LayerMask _renderingLayers = DEFAULT_UI_LAYERMASK;

        [SerializeField, Optional]
        private Material _defaultUIMaterial = null;

        public RenderingMode RenderingMode => _renderingMode;

        public LayerMask RenderingLayers => _renderingLayers;

        public bool UseAlphaToMask => _useAlphaToMask;

        public bool DoUnderlayAntiAliasing => _doUnderlayAntiAliasing;

        public bool EnableSuperSampling => _enableSuperSampling;

        public float AlphaCutoutThreshold => _alphaCutoutThreshold;

        public Action<Texture> OnUpdateRenderTexture = delegate { };

        public bool UseEditorEmulation()
        {
            return Application.isEditor ? _emulateWhileInEditor : false;
        }

        public int RenderScale
        {
            get
            {
                return _renderScale;
            }
            set
            {
                if (_renderScale < 1 || _renderScale > 3)
                {
                    throw new ArgumentException($"Render scale must be between 1 and 3, but was {value}");
                }

                if (_renderScale == value)
                {
                    return;
                }

                _renderScale = value;

                if (isActiveAndEnabled && Application.isPlaying)
                {
                    UpdateCamera();
                }
            }
        }

        public Material DefaultUIMaterial => _defaultUIMaterial;

        public Camera OverlayCamera => _camera;

        public Texture Texture => _tex;

        private RenderTexture _tex;
        private Camera _camera;

        protected bool _started = false;

        public Vector2Int CalcAutoResolution()
        {
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return DEFAULT_TEXTURE_RES;
            }

            Vector2 size = rectTransform.sizeDelta;
            size.x *= rectTransform.lossyScale.x;
            size.y *= rectTransform.lossyScale.y;

            int x = Mathf.RoundToInt(UnitsToPixels(size.x));
            int y = Mathf.RoundToInt(UnitsToPixels(size.y));
            return new Vector2Int(Mathf.Max(x, 1), Mathf.Max(y, 1));
        }

        public Vector2Int GetBaseResolutionToUse()
        {
            if (_dimensionsDriveMode == DriveMode.Auto)
            {
                return CalcAutoResolution();
            }
            else
            {
                return _resolution;
            }
        }

        public Vector2Int GetScaledResolutionToUse()
        {
            return new Vector2Int(
                Mathf.RoundToInt(GetBaseResolutionToUse().x * (float)_renderScale),
                Mathf.RoundToInt(GetBaseResolutionToUse().y * (float)_renderScale)
            );
        }

        public float PixelsToUnits(float pixels)
        {
            return (1f / _pixelsPerUnit) * pixels;
        }

        public float UnitsToPixels(float units)
        {
            return _pixelsPerUnit * units;
        }

        public bool ShouldUseOVROverlay
        {
            get
            {
                switch (_renderingMode)
                {
                    case RenderingMode.OVR_Underlay:
                    case RenderingMode.OVR_Overlay:
                        return !UseEditorEmulation();
                    default:
                        return false;
                }
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (Application.isPlaying && _started)
            {
                EditorApplication.delayCall += () =>
                {
                    UpdateCamera();
                };
            }
        }
#endif

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (_started)
            {
                UpdateCamera();
            }
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.EndStart(ref _started);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_started)
            {
                if (_defaultUIMaterial == null)
                {
                    _defaultUIMaterial = new Material(Shader.Find("UI/Default (Overlay)"));
                }
                UpdateCamera();
            }
        }

        protected override void OnDisable()
        {
            if (_started)
            {
                if (_camera?.gameObject != null)
                {
                    Destroy(_camera.gameObject);
                }
                if (_tex != null)
                {
                    DestroyImmediate(_tex);
                }
            }
            base.OnDisable();
        }

        protected void UpdateCamera()
        {
            if (!Application.isPlaying || !_started)
            {
                return;
            }

            Profiler.BeginSample("InterfaceRenderer.UpdateCamera");
            try
            {
                if (_camera == null)
                {
                    GameObject cameraObj = CreateChildObject("__Camera");
                    _camera = cameraObj.AddComponent<Camera>();

                    _camera.orthographic = true;
                    _camera.nearClipPlane = -0.1f;
                    _camera.farClipPlane = 0.1f;
                    _camera.backgroundColor = new Color(0, 0, 0, 0);
                    _camera.clearFlags = CameraClearFlags.SolidColor;
                }

                UpdateRenderTexture();
                UpdateOrthoSize();
                UpdateCameraCullingMask();
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        protected void UpdateRenderTexture()
        {
            Profiler.BeginSample("InterfaceRenderer.UpdateRenderTexture");
            try
            {
                var resolutionToUse = GetScaledResolutionToUse();

                //Never generate mips when using OVROverlay, they are not used
                bool desiredMipsSetting = ShouldUseOVROverlay ? false : _generateMipMaps;

                if (_tex == null ||
                    _tex.width != resolutionToUse.x ||
                    _tex.height != resolutionToUse.y ||
                    _tex.autoGenerateMips != desiredMipsSetting)
                {
                    if (_tex != null)
                    {
                        _camera.targetTexture = null;
                        DestroyImmediate(_tex);
                    }

                    _tex = new RenderTexture(resolutionToUse.x, resolutionToUse.y, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                    _tex.filterMode = FilterMode.Bilinear;
                    _tex.autoGenerateMips = desiredMipsSetting;
                    _camera.targetTexture = _tex;

                    OnUpdateRenderTexture(_tex);
                }
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        private void UpdateOrthoSize()
        {
            if (_camera != null)
            {
                _camera.orthographicSize = PixelsToUnits(GetBaseResolutionToUse().y) * 0.5f;
            }
        }

        private void UpdateCameraCullingMask()
        {
            if (_camera != null)
            {
                _camera.cullingMask = _renderingLayers.value;
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

        public static class Properties
        {
            public static readonly string DimensionDriveMode = nameof(_dimensionsDriveMode);
            public static readonly string Resolution = nameof(_resolution);
            public static readonly string RenderScale = nameof(_renderScale);
            public static readonly string PixelsPerUnit = nameof(_pixelsPerUnit);
            public static readonly string RenderLayers = nameof(_renderingLayers);
            public static readonly string RenderingMode = nameof(_renderingMode);
            public static readonly string GenerateMips = nameof(_generateMipMaps);
            public static readonly string UseAlphaToMask = nameof(_useAlphaToMask);
            public static readonly string AlphaCutoutThreshold = nameof(_alphaCutoutThreshold);
            public static readonly string UseExpensiveSuperSample = nameof(_enableSuperSampling);
            public static readonly string DoUnderlayAA = nameof(_doUnderlayAntiAliasing);
            public static readonly string EmulateWhileInEditor = nameof(_emulateWhileInEditor);
            public static readonly string DefaultUIMaterial = nameof(_defaultUIMaterial);
        }

        #region Inject

        public void InjectAllCanvasRenderTexture(int pixelsPerUnit,
                                                 int renderScale,
                                                 LayerMask renderingLayers,
                                                 RenderingMode renderingMode,
                                                 bool doUnderlayAntiAliasing,
                                                 float alphaCutoutThreshold,
                                                 bool useAlphaToMask)
        {
            InjectPixelsPerUnit(pixelsPerUnit);
            InjectRenderScale(renderScale);
            InjectRenderingLayers(renderingLayers);
            InjectRenderingMode(renderingMode);
            InjectDoUnderlayAntiAliasing(doUnderlayAntiAliasing);
            InjectAlphaCutoutThreshold(alphaCutoutThreshold);
            InjectUseAlphaToMask(useAlphaToMask);
        }

        public void InjectPixelsPerUnit(int pixelsPerUnit)
        {
            _pixelsPerUnit = pixelsPerUnit;
        }

        public void InjectRenderScale(int renderScale)
        {
            _renderScale = renderScale;
        }

        public void InjectRenderingMode(RenderingMode renderingMode)
        {
            _renderingMode = renderingMode;
        }

        public void InjectRenderingLayers(LayerMask renderingLayers)
        {
            _renderingLayers = renderingLayers;
        }

        public void InjectDoUnderlayAntiAliasing(bool doUnderlayAntiAliasing)
        {
            _doUnderlayAntiAliasing = doUnderlayAntiAliasing;
        }

        public void InjectUseAlphaToMask(bool useAlphaToMask)
        {
            _useAlphaToMask = useAlphaToMask;
        }

        public void InjectAlphaCutoutThreshold(float alphaCutoutThreshold)
        {
            _alphaCutoutThreshold = alphaCutoutThreshold;
        }

        public void InjectOptionalDefaultUIMaterial(Material defaultUIMaterial)
        {
            _defaultUIMaterial = defaultUIMaterial;
        }

        public void InjectOptionalGenerateMipMaps(bool generateMipMaps)
        {
            _generateMipMaps = generateMipMaps;
        }

        #endregion
    }
}
