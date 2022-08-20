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

namespace Oculus.Interaction.GrabAPI
{
    /// <summary>
    /// The HandGrabAPI wraps under the hood several IFingerAPIs to detect if
    /// the fingers are grabbing or not. It differentiates between pinch and
    /// palm grabs but via Inject it is possible to modify the detectors.
    /// </summary>
    public class HandGrabAPI : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;
        public IHand Hand { get; private set; }

        private IFingerAPI _fingerPinchGrabAPI = new FingerPinchGrabAPI();
        private IFingerAPI _fingerPalmGrabAPI = new FingerPalmGrabAPI();

        private bool _started;

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(Hand);
            Assert.IsNotNull(_fingerPinchGrabAPI);
            Assert.IsNotNull(_fingerPalmGrabAPI);
            this.EndStart(ref _started);
        }

        private void OnEnable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated += OnHandUpdated;
            }
        }

        private void OnDisable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated -= OnHandUpdated;
            }
        }

        private void OnHandUpdated()
        {
            _fingerPinchGrabAPI.Update(Hand);
            _fingerPalmGrabAPI.Update(Hand);
        }

        public HandFingerFlags HandPinchGrabbingFingers()
        {
            return HandGrabbingFingers(_fingerPinchGrabAPI);
        }

        public HandFingerFlags HandPalmGrabbingFingers()
        {
            return HandGrabbingFingers(_fingerPalmGrabAPI);
        }

        private HandFingerFlags HandGrabbingFingers(IFingerAPI fingerAPI)
        {
            HandFingerFlags grabbingFingers = HandFingerFlags.None;

            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                HandFinger finger = (HandFinger)i;

                bool isGrabbing = fingerAPI.GetFingerIsGrabbing(finger);
                if (isGrabbing)
                {
                    grabbingFingers |= (HandFingerFlags)(1 << i);
                }
            }

            return grabbingFingers;
        }

        public bool IsHandPinchGrabbing(in GrabbingRule fingers)
        {
            HandFingerFlags pinchFingers = HandPinchGrabbingFingers();
            return IsSustainingGrab(fingers, pinchFingers);
        }

        public bool IsHandPalmGrabbing(in GrabbingRule fingers)
        {
            HandFingerFlags palmFingers = HandPalmGrabbingFingers();
            return IsSustainingGrab(fingers, palmFingers);
        }

        public bool IsSustainingGrab(in GrabbingRule fingers, HandFingerFlags grabbingFingers)
        {
            bool anyHolding = false;
            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                HandFinger finger = (HandFinger)i;
                HandFingerFlags fingerFlag = (HandFingerFlags)(1 << i);

                bool isFingerGrabbing = (grabbingFingers & fingerFlag) != 0;
                if (fingers[finger] == FingerRequirement.Required)
                {
                    anyHolding |= isFingerGrabbing;
                    if (fingers.UnselectMode == FingerUnselectMode.AnyReleased
                        && !isFingerGrabbing)
                    {
                        return false;
                    }

                    if (fingers.UnselectMode == FingerUnselectMode.AllReleased
                        && isFingerGrabbing)
                    {
                        return true;
                    }
                }
                else if (fingers[finger] == FingerRequirement.Optional)
                {
                    anyHolding |= isFingerGrabbing;
                }
            }

            return anyHolding;
        }

        /// <summary>
        /// Determine whether the state of any of the finger pinches have changed this frame to
        /// the target pinching state (on/off).
        /// </summary>
        /// <param name="fingers">Finger rules to check.</param>
        public bool IsHandSelectPinchFingersChanged(in GrabbingRule fingers)
        {
            return IsHandSelectFingersChanged(fingers, _fingerPinchGrabAPI);
        }

        /// <summary>
        /// Determine whether the state of any of the finger grabs have changed this frame to
        /// the target grabbing state (on/off).
        /// </summary>
        /// <param name="fingers">Finger rules to check.</param>
        public bool IsHandSelectPalmFingersChanged(in GrabbingRule fingers)
        {
            return IsHandSelectFingersChanged(fingers, _fingerPalmGrabAPI);
        }

        public bool IsHandUnselectPinchFingersChanged(in GrabbingRule fingers)
        {
            return IsHandUnselectFingersChanged(fingers, _fingerPinchGrabAPI);
        }

        public bool IsHandUnselectPalmFingersChanged(in GrabbingRule fingers)
        {
            return IsHandUnselectFingersChanged(fingers, _fingerPalmGrabAPI);
        }

        private bool IsHandSelectFingersChanged(in GrabbingRule fingers, IFingerAPI fingerAPI)
        {
            bool selectsWithOptionals = fingers.SelectsWithOptionals;
            bool anyFingerBeganGrabbing = false;

            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                HandFinger finger = (HandFinger)i;
                if (fingers[finger] == FingerRequirement.Required)
                {
                    if (!fingerAPI.GetFingerIsGrabbing(finger))
                    {
                        return false;
                    }

                    if (fingerAPI.GetFingerIsGrabbingChanged(finger, true))
                    {
                        anyFingerBeganGrabbing = true;
                    }
                }
                else if (selectsWithOptionals
                    && fingers[finger] == FingerRequirement.Optional)
                {
                    if (fingerAPI.GetFingerIsGrabbingChanged(finger, true))
                    {
                        return true;
                    }
                }
            }

            return anyFingerBeganGrabbing;
        }

        private bool IsHandUnselectFingersChanged(in GrabbingRule fingers, IFingerAPI fingerAPI)
        {
            bool isAnyFingerGrabbing = false;
            bool anyFingerStoppedGrabbing = false;
            bool selectsWithOptionals = fingers.SelectsWithOptionals;
            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                HandFinger finger = (HandFinger)i;
                if (fingers[finger] == FingerRequirement.Ignored)
                {
                    continue;
                }

                isAnyFingerGrabbing |= fingerAPI.GetFingerIsGrabbing(finger);
                if (fingers[finger] == FingerRequirement.Required)
                {
                    if (fingerAPI.GetFingerIsGrabbingChanged(finger, false))
                    {
                        anyFingerStoppedGrabbing = true;
                        if (fingers.UnselectMode == FingerUnselectMode.AnyReleased)
                        {
                            return true;
                        }
                    }
                }
                else if (fingers[finger] == FingerRequirement.Optional)
                {
                    if (fingerAPI.GetFingerIsGrabbingChanged(finger, false))
                    {
                        anyFingerStoppedGrabbing = true;
                        if (fingers.UnselectMode == FingerUnselectMode.AnyReleased
                            && selectsWithOptionals)
                        {
                            return true;
                        }
                    }
                }
            }

            return !isAnyFingerGrabbing && anyFingerStoppedGrabbing;
        }

        public Vector3 GetPinchCenter()
        {
            return WristOffsetToWorldPoint(_fingerPinchGrabAPI.GetCenterOffset());
        }

        public Vector3 GetPalmCenter()
        {
            return WristOffsetToWorldPoint(_fingerPalmGrabAPI.GetCenterOffset());
        }

        private Vector3 WristOffsetToWorldPoint(Vector3 offset)
        {
            if (!Hand.GetJointPose(HandJointId.HandWristRoot, out Pose wristPose))
            {
                return offset;
            }

            return wristPose.position + wristPose.rotation * offset;
        }

        public float GetHandPinchScore(in GrabbingRule fingers,
            bool includePinching = true)
        {
            return GetHandGrabScore(fingers, includePinching, _fingerPinchGrabAPI);
        }

        public float GetHandPalmScore(in GrabbingRule fingers,
            bool includeGrabbing = true)
        {
            return GetHandGrabScore(fingers, includeGrabbing, _fingerPalmGrabAPI);
        }

        public float GetFingerPinchStrength(HandFinger finger)
        {
            return _fingerPinchGrabAPI.GetFingerGrabScore(finger);
        }

        public float GetFingerPalmStrength(HandFinger finger)
        {
            return _fingerPalmGrabAPI.GetFingerGrabScore(finger);
        }

        private float GetHandGrabScore(in GrabbingRule fingers,
            bool includeGrabbing, IFingerAPI fingerAPI)
        {
            float requiredMin = 1.0f;
            float optionalMax = 0f;
            bool usesOptionals = fingers.SelectsWithOptionals;
            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                HandFinger finger = (HandFinger)i;
                if (!includeGrabbing && fingerAPI.GetFingerIsGrabbing((HandFinger)i))
                {
                    continue;
                }

                if (fingers[finger] == FingerRequirement.Ignored)
                {
                    continue;
                }

                if (fingers[finger] == FingerRequirement.Optional)
                {
                    optionalMax = Mathf.Max(optionalMax, fingerAPI.GetFingerGrabScore(finger));
                }
                else if (fingers[finger] == FingerRequirement.Required)
                {
                    requiredMin = Mathf.Min(requiredMin, fingerAPI.GetFingerGrabScore(finger));
                }
            }

            return usesOptionals ? optionalMax : requiredMin;
        }

        #region Inject

        public void InjectAllHandGrabAPI(IHand hand)
        {
            InjectHand(hand);
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as MonoBehaviour;
            Hand = hand;
        }

        public void InjectOptionalFingerPinchAPI(IFingerAPI fingerPinchAPI)
        {
            _fingerPinchGrabAPI = fingerPinchAPI;
        }

        public void InjectOptionalFingerGrabAPI(IFingerAPI fingerGrabAPI)
        {
            _fingerPalmGrabAPI = fingerGrabAPI;
        }

        #endregion
    }
}
