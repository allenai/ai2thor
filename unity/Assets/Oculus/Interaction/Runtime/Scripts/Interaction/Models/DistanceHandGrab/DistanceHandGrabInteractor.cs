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
using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.Input;
using Oculus.Interaction.Throw;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// The DistanceHandGrabInteractor allows grabbing DistanceHandGrabInteractables at a distance.
    /// Similarly to the HandGrabInteractor it operates with HandGrabPoses to specify the final pose of the hand
    /// and as well as attracting objects at a distance it will held them in the same manner the HandGrabInteractor does.
    /// The DistanceHandGrabInteractor does not need a collider and uses conical frustums to detect far-away objects.
    /// </summary>
    public class DistanceHandGrabInteractor :
        PointerInteractor<DistanceHandGrabInteractor, DistanceHandGrabInteractable>
        , IHandGrabState, IHandGrabber, IDistanceInteractor
    {
        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;
        public IHand Hand { get; private set; }

        [SerializeField]
        private HandGrabAPI _handGrabApi;

        [SerializeField]
        private Transform _grabOrigin;

        [Header("Distance selection volumes")]
        [SerializeField]
        private DistantPointDetectorFrustums _detectionFrustums;

        [Header("Grabbing")]
        [SerializeField]
        private GrabTypeFlags _supportedGrabTypes = GrabTypeFlags.Pinch;
        [SerializeField]
        private bool _hoverOnZeroStrength = true;
        public bool HoverOnZeroStregnth
        {
            get
            {
                return _hoverOnZeroStrength;
            }
            set
            {
                _hoverOnZeroStrength = value;
            }
        }

        [SerializeField, Optional]
        private Transform _gripPoint;

        [SerializeField, Optional]
        private Transform _pinchPoint;

        [SerializeField]
        private float _detectionDelay = 0f;

        [SerializeField, Interface(typeof(IVelocityCalculator)), Optional]
        private MonoBehaviour _velocityCalculator;

        public IVelocityCalculator VelocityCalculator { get; set; }

        private HandGrabTarget _currentTarget = new HandGrabTarget();
        private HandGrabResult _cachedResult = new HandGrabResult();

        private IMovement _movement;

        private HandGrabTarget _immediateTarget = new HandGrabTarget();
        private DistanceHandGrabInteractable _stableCandidate;
        private DistanceHandGrabInteractable _pointedCandidate;
        private float _hoverStartTime;
        private Vector3 _originalHitPoint;

        private Pose _wristToGrabAnchorOffset = Pose.identity;
        private Pose _wristPose = Pose.identity;
        private Pose _gripPose = Pose.identity;
        private Pose _pinchPose = Pose.identity;

        private bool _handGrabShouldSelect = false;
        private bool _handGrabShouldUnselect = false;

        private HandGrabbableData _lastInteractableData =
            new HandGrabbableData();

        #region IHandGrabber

        public HandGrabAPI HandGrabApi => _handGrabApi;
        public GrabTypeFlags SupportedGrabTypes => _supportedGrabTypes;
        public IHandGrabbable TargetInteractable => Interactable;

        #endregion

        public ConicalFrustum PointerFrustum => _detectionFrustums.SelectionFrustum;

        #region IHandGrabSource

        public virtual bool IsGrabbing => HasSelectedInteractable
            && (_movement == null || _movement.Stopped);

        private float _grabStrength;
        public float FingersStrength => _grabStrength;
        public float WristStrength => _grabStrength;

        public Pose WristToGrabPoseOffset => _wristToGrabAnchorOffset;

        public HandFingerFlags GrabbingFingers() =>
            Grab.HandGrab.GrabbingFingers(this, SelectedInteractable);

        public HandGrabTarget HandGrabTarget { get; private set; }
        public System.Action<IHandGrabState> WhenHandGrabStarted { get; set; } = delegate { };
        public System.Action<IHandGrabState> WhenHandGrabEnded { get; set; } = delegate { };

        #endregion

        private DistantPointDetector _detector;

        #region editor events

        protected virtual void Reset()
        {
            _hand = this.GetComponentInParent<IHand>() as MonoBehaviour;
            _handGrabApi = this.GetComponentInParent<HandGrabAPI>();
        }

        #endregion

        protected override void Awake()
        {
            base.Awake();
            Hand = _hand as IHand;
            VelocityCalculator = _velocityCalculator as IVelocityCalculator;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            Assert.IsNotNull(Hand, "Hand can not be null");
            Assert.IsNotNull(_handGrabApi, "HandGrabAPI can not be null");
            Assert.IsNotNull(_grabOrigin);
            Assert.IsNotNull(PointerFrustum, "The selector frustum can not be null");
            if (_velocityCalculator != null)
            {
                Assert.IsNotNull(VelocityCalculator, "The provided Velocity Calculator is not an IVelocityCalculator");
            }

            _detector = new DistantPointDetector(_detectionFrustums);
            this.EndStart(ref _started);
        }

        #region life cycle

        protected override void DoPreprocess()
        {
            base.DoPreprocess();

            _wristPose = _grabOrigin.GetPose();

            if (Hand.Handedness == Handedness.Left)
            {
                _wristPose.rotation *= Quaternion.Euler(180f, 0f, 0f);
            }

            if (_gripPoint != null)
            {
                _gripPose = _gripPoint.GetPose();
            }
            if (_pinchPoint != null)
            {
                _pinchPose = _pinchPoint.GetPose();
            }
        }

        public override bool ShouldSelect
        {
            get
            {
                if (State != InteractorState.Hover)
                {
                    return false;
                }

                return _candidate == _interactable && _handGrabShouldSelect;
            }
        }

        public override bool ShouldUnselect
        {
            get
            {
                if (State != InteractorState.Select)
                {
                    return false;
                }

                return _handGrabShouldUnselect;
            }
        }

        protected override void DoHoverUpdate()
        {
            base.DoHoverUpdate();

            _handGrabShouldSelect = false;
            if (Interactable == null)
            {
                HandGrabTarget = null;
                _wristToGrabAnchorOffset = Pose.identity;
                _grabStrength = 0f;
                return;
            }

            _wristToGrabAnchorOffset = GetGrabAnchorOffset(_currentTarget.Anchor, _wristPose);
            _grabStrength = Grab.HandGrab.ComputeHandGrabScore(this, Interactable,
                out GrabTypeFlags hoverGrabTypes);
            HandGrabTarget = _currentTarget;

            if (Interactable != null
                && Grab.HandGrab.ComputeShouldSelect(this, Interactable, out GrabTypeFlags selectingGrabTypes))
            {
                _handGrabShouldSelect = true;
            }
        }

        protected override void DoSelectUpdate()
        {
            DistanceHandGrabInteractable interactable = _selectedInteractable;
            _handGrabShouldUnselect = false;
            if (interactable == null)
            {
                _grabStrength = 0f;
                _currentTarget.Clear();
                _handGrabShouldUnselect = true;
                return;
            }

            _grabStrength = 1f;
            Pose grabPose = PoseUtils.Multiply(_wristPose, _wristToGrabAnchorOffset);
            _movement.UpdateTarget(grabPose);
            _movement.Tick();

            Grab.HandGrab.StoreGrabData(this, interactable, ref _lastInteractableData);
            if (Grab.HandGrab.ComputeShouldUnselect(this, interactable))
            {
                _handGrabShouldUnselect = true;
            }
        }

        protected override void InteractableSelected(DistanceHandGrabInteractable interactable)
        {
            if (interactable == null)
            {
                base.InteractableSelected(interactable);
                return;
            }

            _wristToGrabAnchorOffset = GetGrabAnchorOffset(_currentTarget.Anchor, _wristPose);
            Pose grabPose = PoseUtils.Multiply(_wristPose, _wristToGrabAnchorOffset);
            Pose interactableGrabStartPose = _currentTarget.WorldGrabPose;
            _movement = interactable.GenerateMovement(interactableGrabStartPose, grabPose);
            base.InteractableSelected(interactable);
        }

        protected override void InteractableUnselected(DistanceHandGrabInteractable interactable)
        {
            base.InteractableUnselected(interactable);

            _movement = null;

            ReleaseVelocityInformation throwVelocity = VelocityCalculator != null ?
                VelocityCalculator.CalculateThrowVelocity(interactable.transform) :
                new ReleaseVelocityInformation(Vector3.zero, Vector3.zero, Vector3.zero);
            interactable.ApplyVelocities(throwVelocity.LinearVelocity, throwVelocity.AngularVelocity);
        }

        protected override Pose ComputePointerPose()
        {
            if (SelectedInteractable != null)
            {
                return _movement.Pose;
            }

            if (Interactable != null)
            {
                HandGrabTarget.GrabAnchor anchorMode = _currentTarget.Anchor;
                return anchorMode == HandGrabTarget.GrabAnchor.Pinch ? _pinchPose :
                    anchorMode == HandGrabTarget.GrabAnchor.Palm ? _gripPose :
                    _wristPose;
            }

            return _wristPose;
        }

        protected override void HandlePointerEventRaised(PointerEvent evt)
        {
            base.HandlePointerEventRaised(evt);

            if (SelectedInteractable == null)
            {
                return;
            }

            if (evt.Identifier != Identifier &&
                (evt.Type == PointerEventType.Select || evt.Type == PointerEventType.Unselect))
            {
                Pose grabPose = PoseUtils.Multiply(_wristPose, _wristToGrabAnchorOffset);
                if (SelectedInteractable.ResetGrabOnGrabsUpdated)
                {
                    if (SelectedInteractable.CalculateBestPose(grabPose, Hand.Scale, Hand.Handedness,
                        ref _cachedResult))
                    {
                        HandGrabTarget.GrabAnchor anchor = _currentTarget.Anchor;
                        _currentTarget.Set(SelectedInteractable.RelativeTo,
                            SelectedInteractable.HandAlignment, anchor, _cachedResult);
                    }
                }

                Pose fromPose = _currentTarget.WorldGrabPose;
                _movement = SelectedInteractable.GenerateMovement(fromPose, grabPose);
                SelectedInteractable.PointableElement.ProcessPointerEvent(
                    new PointerEvent(Identifier, PointerEventType.Move, fromPose));
            }
        }

        #endregion


        private Pose GetGrabAnchorPose(DistanceHandGrabInteractable interactable, GrabTypeFlags grabTypes,
            out HandGrabTarget.GrabAnchor anchorMode)
        {
            if (_gripPoint != null && (grabTypes & GrabTypeFlags.Palm) != 0)
            {
                anchorMode = HandGrabTarget.GrabAnchor.Palm;
            }
            else if (_pinchPoint != null && (grabTypes & GrabTypeFlags.Pinch) != 0)
            {
                anchorMode = HandGrabTarget.GrabAnchor.Pinch;
            }
            else
            {
                anchorMode = HandGrabTarget.GrabAnchor.Wrist;
            }

            if (interactable.UsesHandPose())
            {
                return _wristPose;
            }
            else if (anchorMode == HandGrabTarget.GrabAnchor.Pinch)
            {
                return _pinchPose;
            }
            else if (anchorMode == HandGrabTarget.GrabAnchor.Palm)
            {
                return _gripPose;
            }
            else
            {
                return _wristPose;
            }
        }

        private Pose GetGrabAnchorOffset(HandGrabTarget.GrabAnchor anchor, in Pose from)
        {
            if (anchor == HandGrabTarget.GrabAnchor.Pinch)
            {
                return PoseUtils.Delta(from, _pinchPose);
            }
            else if (anchor == HandGrabTarget.GrabAnchor.Palm)
            {
                return PoseUtils.Delta(from, _gripPose);
            }

            return PoseUtils.Delta(from, _wristPose);
        }

        protected override DistanceHandGrabInteractable ComputeCandidate()
        {
            if (_stableCandidate != null
                && _detector.IsPointingWithoutAid(_stableCandidate.Colliders))
            {
                RefreshTarget(_stableCandidate, ref _currentTarget);
                return _stableCandidate;
            }

            if (_stableCandidate != null
                && !_detector.ComputeIsPointing(_stableCandidate.Colliders, false,
                        out float score, out Vector3 bestHitPoint))
            {
                _currentTarget.Clear();
                _stableCandidate = null;
            }

            DistanceHandGrabInteractable candidate = ComputeBestHandGrabTarget(ref _immediateTarget, _stableCandidate == null);
            if (candidate != _pointedCandidate)
            {
                _pointedCandidate = candidate;
                if (candidate != null)
                {
                    _hoverStartTime = Time.time;
                }
            }

            if ((_stableCandidate == null
                    && candidate != null)
                || (_stableCandidate != null
                    && candidate != null
                    && _stableCandidate != candidate
                    && Time.time - _hoverStartTime >= _detectionDelay))
            {
                _pointedCandidate = null;
                _stableCandidate = candidate;
                _currentTarget.Set(_immediateTarget);
            }
            else if (_stableCandidate != null)
            {
                RefreshTarget(_stableCandidate, ref _currentTarget);
            }
            return _stableCandidate;
        }

        protected DistanceHandGrabInteractable ComputeBestHandGrabTarget(ref HandGrabTarget handGrabTarget, bool wideSearch)
        {
            DistanceHandGrabInteractable closestInteractable = null;
            float bestScore = float.NegativeInfinity;
            float bestFingerScore = float.NegativeInfinity;

            IEnumerable<DistanceHandGrabInteractable> interactables = DistanceHandGrabInteractable.Registry.List(this);

            foreach (DistanceHandGrabInteractable interactable in interactables)
            {
                if (!Grab.HandGrab.CouldSelect(this, interactable, out GrabTypeFlags availableGrabTypes))
                {
                    continue;
                }

                float fingerScore = 1.0f;
                if (!Grab.HandGrab.ComputeShouldSelect(this, interactable, out GrabTypeFlags selectingGrabTypes))
                {
                    fingerScore = Grab.HandGrab.ComputeHandGrabScore(this, interactable, out selectingGrabTypes);
                }

                if (fingerScore < bestFingerScore)
                {
                    continue;
                }

                if (selectingGrabTypes == GrabTypeFlags.None)
                {
                    if (_hoverOnZeroStrength)
                    {
                        selectingGrabTypes = availableGrabTypes;
                    }
                    else
                    {
                        continue;
                    }
                }

                if (!_detector.ComputeIsPointing(interactable.Colliders, !wideSearch, out float score, out Vector3 hitPoint)
                    || score < bestScore)
                {
                    continue;
                }

                Pose grabPose = GetGrabAnchorPose(interactable, selectingGrabTypes,
                    out HandGrabTarget.GrabAnchor anchorMode);
                Pose worldPose = new Pose(hitPoint, grabPose.rotation);
                bool poseFound = interactable.CalculateBestPose(worldPose, Hand.Scale,
                    Hand.Handedness,
                    ref _cachedResult);

                if (!poseFound)
                {
                    continue;
                }

                bestScore = score;
                Pose offset = GetGrabAnchorOffset(anchorMode, grabPose);
                _originalHitPoint = hitPoint;
                _cachedResult.SnapPose = PoseUtils.Multiply(_cachedResult.SnapPose, offset);
                handGrabTarget.Set(interactable.RelativeTo, interactable.HandAlignment, anchorMode, _cachedResult);
                closestInteractable = interactable;
            }

            return closestInteractable;
        }

        private void RefreshTarget(DistanceHandGrabInteractable interactable, ref HandGrabTarget handGrabTarget)
        {
            Pose grabPose;
            if (interactable.UsesHandPose())
            {
                grabPose = _wristPose;
            }
            else if (handGrabTarget.Anchor == HandGrabTarget.GrabAnchor.Pinch)
            {
                grabPose = _pinchPose;
            }
            else
            {
                grabPose = _gripPose;
            }

            Pose worldPose = new Pose(_originalHitPoint, grabPose.rotation);
            interactable.CalculateBestPose(worldPose, Hand.Scale,
                   Hand.Handedness,
                   ref _cachedResult);
            Pose offset = GetGrabAnchorOffset(handGrabTarget.Anchor, grabPose);
            _cachedResult.SnapPose = PoseUtils.Multiply(_cachedResult.SnapPose, offset);
            handGrabTarget.Set(interactable.RelativeTo, handGrabTarget.HandAlignment, handGrabTarget.Anchor, _cachedResult);

        }

        #region Inject
        public void InjectAllDistanceHandGrabInteractor(HandGrabAPI handGrabApi,
            DistantPointDetectorFrustums frustums,
            Transform grabOrigin,
            IHand hand, GrabTypeFlags supportedGrabTypes)
        {
            InjectHandGrabApi(handGrabApi);
            InjectDetectionFrustums(frustums);
            InjectGrabOrigin(grabOrigin);
            InjectHand(hand);
            InjectSupportedGrabTypes(supportedGrabTypes);
        }

        public void InjectHandGrabApi(HandGrabAPI handGrabApi)
        {
            _handGrabApi = handGrabApi;
        }

        public void InjectDetectionFrustums(DistantPointDetectorFrustums frustums)
        {
            _detectionFrustums = frustums;
        }

        public void InjectGrabOrigin(Transform grabOrigin)
        {
            _grabOrigin = grabOrigin;
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as MonoBehaviour;
            Hand = hand;
        }

        public void InjectSupportedGrabTypes(GrabTypeFlags supportedGrabTypes)
        {
            _supportedGrabTypes = supportedGrabTypes;
        }

        public void InjectOptionalGripPoint(Transform gripPoint)
        {
            _gripPoint = gripPoint;
        }

        public void InjectOptionalPinchPoint(Transform pinchPoint)
        {
            _pinchPoint = pinchPoint;
        }

        public void InjectOptionalVelocityCalculator(IVelocityCalculator velocityCalculator)
        {
            _velocityCalculator = velocityCalculator as MonoBehaviour;
            VelocityCalculator = velocityCalculator;
        }
        #endregion
    }
}
