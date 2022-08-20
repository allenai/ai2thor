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

using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    public class HandConfidenceVisual : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;
        private IHand Hand { get; set; }

        [SerializeField]
        private MaterialPropertyBlockEditor _handMaterialPropertyBlockEditor;

        [SerializeField]
        private float _speed = 5f;
        public float Speed
        {
            get
            {
                return _speed;
            }
            set
            {
                _speed = value;
            }
        }

        private readonly int _handConfidenceId = Shader.PropertyToID("_JointsGlow");
        private float[] _jointsConfidence = new float[18];

        protected bool _started = false;
        private float _lastTime;

        private void Awake()
        {
            Hand = _hand as IHand;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(Hand);
            Assert.IsNotNull(_handMaterialPropertyBlockEditor);
            _lastTime = Time.time;
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated += UpdateVisual;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated -= UpdateVisual;
            }
        }

        private void UpdateVisual()
        {
            float changeRate = (Time.time - _lastTime) * Speed;
            _lastTime = Time.time;

            float handConfidence = Hand.IsHighConfidence ? 0f : 1f;
            _jointsConfidence[0] = Mathf.Lerp(_jointsConfidence[0], handConfidence, changeRate);

            FillConfidence(HandFinger.Thumb, 1, 4);
            FillConfidence(HandFinger.Index, 5, 3);
            FillConfidence(HandFinger.Middle, 8, 3);
            FillConfidence(HandFinger.Ring, 11, 3);
            FillConfidence(HandFinger.Pinky, 14, 4);

            void FillConfidence(HandFinger finger, int offset, int lenght)
            {
                int confidence = Hand.GetFingerIsHighConfidence(finger) ? 0 : 1;
                for (int i = offset; i < offset + lenght; i++)
                {
                    _jointsConfidence[i] = Mathf.Lerp(_jointsConfidence[i], confidence, changeRate);
                }
            }
            _handMaterialPropertyBlockEditor.MaterialPropertyBlock.SetFloatArray(_handConfidenceId, _jointsConfidence);
            _handMaterialPropertyBlockEditor.UpdateMaterialPropertyBlock();
        }

        #region Inject
        public void InjectAllHandConfidenceVisual(IHand hand,
            MaterialPropertyBlockEditor handMaterialPropertyBlockEditor)
        {
            InjectHand(hand);
            InjectHandMaterialPropertyBlockEditor(handMaterialPropertyBlockEditor);
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as MonoBehaviour;
            Hand = hand;
        }

        public void InjectHandMaterialPropertyBlockEditor(MaterialPropertyBlockEditor handMaterialPropertyBlockEditor)
        {
            _handMaterialPropertyBlockEditor = handMaterialPropertyBlockEditor;
        }

        #endregion
    }
}
