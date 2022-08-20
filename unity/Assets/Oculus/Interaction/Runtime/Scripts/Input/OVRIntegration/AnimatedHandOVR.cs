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

namespace Oculus.Interaction.Input
{
    public class AnimatedHandOVR : MonoBehaviour
    {
        public enum AllowThumbUp
        {
            Always,
            GripRequired,
            TriggerAndGripRequired,
        }
        public const string ANIM_LAYER_NAME_POINT = "Point Layer";
        public const string ANIM_LAYER_NAME_THUMB = "Thumb Layer";
        public const string ANIM_PARAM_NAME_FLEX = "Flex";

        public const float INPUT_RATE_CHANGE = 20.0f;

        [SerializeField]
        private OVRInput.Controller _controller = OVRInput.Controller.None;
        [SerializeField]
        private Animator _animator = null;
        [SerializeField]
        private AllowThumbUp _allowThumbUp = AllowThumbUp.TriggerAndGripRequired;

        private int _animLayerIndexThumb = -1;
        private int _animLayerIndexPoint = -1;
        private int _animParamIndexFlex = -1;

        private bool _isPointing = false;
        private bool _isGivingThumbsUp = false;
        private float _pointBlend = 0.0f;
        private float _thumbsUpBlend = 0.0f;

        private const float TRIGGER_MAX = 0.95f;

        protected virtual void Start()
        {
            _animLayerIndexPoint = _animator.GetLayerIndex(ANIM_LAYER_NAME_POINT);
            _animLayerIndexThumb = _animator.GetLayerIndex(ANIM_LAYER_NAME_THUMB);
            _animParamIndexFlex = Animator.StringToHash(ANIM_PARAM_NAME_FLEX);
        }

        protected virtual void Update()
        {
            UpdateCapTouchStates();

            _pointBlend = InputValueRateChange(_isPointing, _pointBlend);
            _thumbsUpBlend = InputValueRateChange(_isGivingThumbsUp, _thumbsUpBlend);

            UpdateAnimStates();
        }

        private void UpdateCapTouchStates()
        {
            _isPointing = !OVRInput.Get(OVRInput.NearTouch.PrimaryIndexTrigger, _controller)
               && OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, _controller) == 0f;

            bool triggerThumbsUp = _allowThumbUp == AllowThumbUp.Always ||
                (_allowThumbUp == AllowThumbUp.GripRequired
                    && OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, _controller) >= TRIGGER_MAX) ||
                (_allowThumbUp == AllowThumbUp.TriggerAndGripRequired
                    && OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, _controller) >= TRIGGER_MAX
                    && OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, _controller) >= TRIGGER_MAX);

            _isGivingThumbsUp = !OVRInput.Get(OVRInput.NearTouch.PrimaryThumbButtons, _controller)
                && !OVRInput.Get(OVRInput.Button.One, _controller)
                && !OVRInput.Get(OVRInput.Button.Two, _controller)
                && !OVRInput.Get(OVRInput.Button.Three, _controller)
                && !OVRInput.Get(OVRInput.Button.Four, _controller)
                && !OVRInput.Get(OVRInput.Button.PrimaryThumbstick, _controller)
                && OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, _controller).magnitude == 0
                && triggerThumbsUp;
        }

        /// <summary>
        /// Based on InputValueRateChange from OVR Samples it ensures
        /// the animation blending happens with controlled timing instead of instantly
        /// </summary>
        /// <param name="isDown">Direction of the animation</param>
        /// <param name="value">Value to change</param>
        /// <returns>The input value increased or decreased at a fixed rate</returns>
        private float InputValueRateChange(bool isDown, float value)
        {
            float rateDelta = Time.deltaTime * INPUT_RATE_CHANGE;
            float sign = isDown ? 1.0f : -1.0f;
            return Mathf.Clamp01(value + rateDelta * sign);
        }

        private void UpdateAnimStates()
        {
            // Flex
            // blend between open hand and fully closed fist
            float flex = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, _controller);
            _animator.SetFloat(_animParamIndexFlex, flex);

            // Point
            _animator.SetLayerWeight(_animLayerIndexPoint, _pointBlend);

            // Thumbs up
            _animator.SetLayerWeight(_animLayerIndexThumb, _thumbsUpBlend);

            float pinch = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, _controller);
            _animator.SetFloat("Pinch", pinch);
        }


        #region Inject

        public void InjectAllAnimatedHandOVR(OVRInput.Controller controller, Animator animator)
        {
            InjectController(controller);
            InjectAnimator(animator);
        }

        public void InjectController(OVRInput.Controller controller)
        {
            _controller = controller;
        }

        public void InjectAnimator(Animator animator)
        {
            _animator = animator;
        }

        #endregion
    }
}
