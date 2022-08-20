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

using Oculus.Interaction.Grab;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    public class GrabStrengthIndicator : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IHandGrabber), typeof(IInteractor))]
        private MonoBehaviour _handGrabInteractor;
        private IHandGrabber HandGrab { get; set; }
        private IInteractor Interactor { get; set; }

        [SerializeField]
        private MaterialPropertyBlockEditor _handMaterialPropertyBlockEditor;

        [SerializeField]
        private float _glowLerpSpeed = 2f;
        [SerializeField]
        private float _glowColorLerpSpeed = 2f;

        [SerializeField]
        private Color _fingerGlowColorWithInteractable;
        [SerializeField]
        private Color _fingerGlowColorWithNoInteractable;
        [SerializeField]
        private Color _fingerGlowColorHover;

        #region public properties
        public float GlowLerpSpeed
        {
            get
            {
                return _glowLerpSpeed;
            }
            set
            {
                _glowLerpSpeed = value;
            }
        }


        public float GlowColorLerpSpeed
        {
            get
            {
                return _glowColorLerpSpeed;
            }
            set
            {
                _glowColorLerpSpeed = value;
            }
        }

        public Color FingerGlowColorWithInteractable
        {
            get
            {
                return _fingerGlowColorWithInteractable;
            }
            set
            {
                _fingerGlowColorWithInteractable = value;
            }
        }

        public Color FingerGlowColorWithNoInteractable
        {
            get
            {
                return _fingerGlowColorWithNoInteractable;
            }
            set
            {
                _fingerGlowColorWithNoInteractable = value;
            }
        }

        public Color FingerGlowColorHover
        {
            get
            {
                return _fingerGlowColorHover;
            }
            set
            {
                _fingerGlowColorHover = value;
            }
        }
        #endregion

        private readonly int[] _handShaderGlowPropertyIds = new int[]
        {
                Shader.PropertyToID("_ThumbGlowValue"),
                Shader.PropertyToID("_IndexGlowValue"),
                Shader.PropertyToID("_MiddleGlowValue"),
                Shader.PropertyToID("_RingGlowValue"),
                Shader.PropertyToID("_PinkyGlowValue"),
        };

        private readonly int _fingerGlowColorPropertyId = Shader.PropertyToID("_FingerGlowColor");

        private Color _currentGlowColor;

        protected bool _started = false;

        private void Awake()
        {
            HandGrab = _handGrabInteractor as IHandGrabber;
            Interactor = _handGrabInteractor as IInteractor;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            Assert.IsNotNull(_handMaterialPropertyBlockEditor);
            Assert.IsNotNull(HandGrab);
            Assert.IsNotNull(Interactor);

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Interactor.WhenPostprocessed += UpdateVisual;
                _currentGlowColor = _fingerGlowColorWithNoInteractable;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Interactor.WhenPostprocessed -= UpdateVisual;
            }
        }

        private void UpdateVisual()
        {
            bool isSelecting = Interactor.State == InteractorState.Select;
            bool isSelectingInteractable = Interactor.HasSelectedInteractable;
            bool hasHoverTarget = Interactor.HasCandidate;

            Color desiredGlowColor = _fingerGlowColorHover;
            if (isSelecting)
            {
                desiredGlowColor = isSelectingInteractable
                    ? _fingerGlowColorWithInteractable
                    : _fingerGlowColorWithNoInteractable;
            }

            _currentGlowColor = Color.Lerp(_currentGlowColor, desiredGlowColor,
                Time.deltaTime * _glowColorLerpSpeed);
            _handMaterialPropertyBlockEditor.MaterialPropertyBlock.SetColor(_fingerGlowColorPropertyId, _currentGlowColor);

            for (int i = 0; i < Constants.NUM_FINGERS; ++i)
            {
                if ((isSelecting && !isSelectingInteractable) ||
                    (!isSelecting && !hasHoverTarget))
                {
                    UpdateGlowValue(i, 0f);
                    continue;
                }

                float glowValue = 0f;
                HandFinger finger = (HandFinger)i;
                if ((HandGrab.SupportedGrabTypes & GrabTypeFlags.Pinch) != 0
                    && HandGrab.TargetInteractable != null
                    && (HandGrab.TargetInteractable.SupportedGrabTypes & GrabTypeFlags.Pinch) != 0
                    && HandGrab.TargetInteractable.PinchGrabRules[finger] != GrabAPI.FingerRequirement.Ignored)
                {
                    glowValue = Mathf.Max(glowValue, HandGrab.HandGrabApi.GetFingerPinchStrength(finger));
                }

                if ((HandGrab.SupportedGrabTypes & GrabTypeFlags.Palm) != 0
                    && HandGrab.TargetInteractable != null
                    && (HandGrab.TargetInteractable.SupportedGrabTypes & GrabTypeFlags.Palm) != 0
                    && HandGrab.TargetInteractable.PalmGrabRules[finger] != GrabAPI.FingerRequirement.Ignored)
                {
                    glowValue = Mathf.Max(glowValue, HandGrab.HandGrabApi.GetFingerPalmStrength(finger));
                }

                UpdateGlowValue(i, glowValue);
            }

            _handMaterialPropertyBlockEditor.UpdateMaterialPropertyBlock();
        }

        private void UpdateGlowValue(int fingerIndex, float glowValue)
        {
            float currentGlowValue = _handMaterialPropertyBlockEditor.MaterialPropertyBlock.GetFloat(_handShaderGlowPropertyIds[fingerIndex]);
            float newGlowValue = Mathf.MoveTowards(currentGlowValue, glowValue, _glowLerpSpeed * Time.deltaTime);
            _handMaterialPropertyBlockEditor.MaterialPropertyBlock.SetFloat(_handShaderGlowPropertyIds[fingerIndex], newGlowValue);
        }

        #region Inject

        public void InjectAllGrabStrengthIndicator(IHandGrabber handGrab, IInteractor interactor,
            MaterialPropertyBlockEditor handMaterialPropertyBlockEditor)
        {
            InjectHandGrab(handGrab);
            InjectInteractor(interactor);
            InjectHandMaterialPropertyBlockEditor(handMaterialPropertyBlockEditor);
        }

        public void InjectHandGrab(IHandGrabber handGrab)
        {
            HandGrab = handGrab;
        }

        public void InjectInteractor(IInteractor interactor)
        {
            _handGrabInteractor = interactor as MonoBehaviour;
            Interactor = interactor;
        }

        public void InjectHandMaterialPropertyBlockEditor(MaterialPropertyBlockEditor handMaterialPropertyBlockEditor)
        {
            _handMaterialPropertyBlockEditor = handMaterialPropertyBlockEditor;
        }

        #endregion
    }
}
