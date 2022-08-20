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
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.Throw
{
    /// <summary>
    /// Velocity calculator that depends only on an IPoseInputDevice,
    /// which means its input agnostic.
    /// </summary>
    public class StandardVelocityCalculator : MonoBehaviour, IVelocityCalculator
    {
        [Serializable]
        public class BufferingParams
        {
            public float BufferLengthSeconds = 0.4f;
            public float SampleFrequency = 90.0f;

            public void Validate()
            {
                Assert.IsTrue(BufferLengthSeconds > 0.0f);
                Assert.IsTrue(SampleFrequency > 0.0f);
            }
        }

        private struct SamplePoseData
        {
            public readonly Pose TransformPose;
            public readonly Vector3 LinearVelocity;
            public readonly Vector3 AngularVelocity;
            public readonly float Time;

            public SamplePoseData(Pose transformPose,
              Vector3 linearVelocity, Vector3 angularVelocity, float time)
            {
                TransformPose = transformPose;
                LinearVelocity = linearVelocity;
                AngularVelocity = angularVelocity;
                Time = time;
            }
        }

        [SerializeField, Interface(typeof(IPoseInputDevice))]
        private MonoBehaviour _throwInputDevice;
        public IPoseInputDevice ThrowInputDevice { get; private set; }

        [SerializeField]
        [Tooltip("The reference position is the center of mass of the hand or controller." +
            " Use this offset this in case the computed center of mass is not entirely correct.")]
        private Vector3 _referenceOffset = Vector3.zero;

        [SerializeField, Tooltip("Related to buffering velocities; used for final " +
            "velocity calculation.")]
        private BufferingParams _bufferingParams;

        [SerializeField, Tooltip("Influence of latest velocities upon release.")]
        [Range(0.0f, 1.0f)]
        private float _instantVelocityInfluence = 1.0f;
        [SerializeField]
        [Range(0.0f, 1.0f), Tooltip("Influence of derived velocities trend upon release.")]
        private float _trendVelocityInfluence = 1.0f;
        [SerializeField]
        [Range(0.0f, 1.0f), Tooltip("Influence of tangential velcities upon release, which" +
            " can be affected by rotational motion.")]
        private float _tangentialVelocityInfluence = 1.0f;
        [SerializeField]
        [Range(0.0f, 1.0f), Tooltip("Influence of external velocities upon release. For hands, " +
            "this can include fingers.")]
        private float _externalVelocityInfluence = 0.0f;

        [SerializeField, Tooltip("Time of anticipated release. Hand tracking " +
            "might experience greater latency compared to controllers.")]
        private float _stepBackTime = 0.08f;

        [SerializeField, Tooltip("Trend velocity uses a window of velocities, " +
            "assuming not too many of those velocities are zero. If they exceed a max percentage " +
            "then a last resort method is used.")]
        private float _maxPercentZeroSamplesTrendVeloc = 0.5f;

        [Header("Sampling filtering.")]
        [SerializeField]
        private OneEuroFilterPropertyBlock _filterProps = OneEuroFilterPropertyBlock.Default;

        public float UpdateFrequency => _updateFrequency;
        private float _updateFrequency = -1.0f;
        private float _updateLatency = -1.0f;
        private float _lastUpdateTime = -1.0f;

        private IOneEuroFilter<Vector3> _linearVelocityFilter;

        public Vector3 ReferenceOffset
        {
            get
            {
                return _referenceOffset;
            }
            set
            {
                _referenceOffset = value;
            }
        }

        public float InstantVelocityInfluence
        {
            get
            {
                return _instantVelocityInfluence;
            }
            set
            {
                _instantVelocityInfluence = value;
            }
        }

        public float TrendVelocityInfluence
        {
            get
            {
                return _trendVelocityInfluence;
            }
            set
            {
                _trendVelocityInfluence = value;
            }
        }

        public float TangentialVelocityInfluence
        {
            get
            {
                return _tangentialVelocityInfluence;
            }
            set
            {
                _tangentialVelocityInfluence = value;
            }
        }

        public float ExternalVelocityInfluence
        {
            get
            {
                return _externalVelocityInfluence;
            }
            set
            {
                _externalVelocityInfluence = value;
            }
        }

        public float StepBackTime
        {
            get
            {
                return _stepBackTime;
            }
            set
            {
                _stepBackTime = value;
            }
        }

        public float MaxPercentZeroSamplesTrendVeloc
        {
            get
            {
                return _maxPercentZeroSamplesTrendVeloc;
            }
            set
            {
                _maxPercentZeroSamplesTrendVeloc = value;
            }
        }

        public Vector3 AddedInstantLinearVelocity { get; private set; }
        public Vector3 AddedTrendLinearVelocity { get; private set; }
        public Vector3 AddedTangentialLinearVelocity { get; private set; }

        /// <summary>
        /// Tangential velocity information, updated upon release.
        /// </summary>
        public Vector3 AxisOfRotation { get; private set; }
        public Vector3 CenterOfMassToObject { get; private set; }
        public Vector3 TangentialDirection { get; private set; }
        public Vector3 AxisOfRotationOrigin { get; private set; }

        List<ReleaseVelocityInformation> _currentThrowVelocities = new List<ReleaseVelocityInformation>();

        public event Action<List<ReleaseVelocityInformation>> WhenThrowVelocitiesChanged = delegate { };

        public event Action<ReleaseVelocityInformation> WhenNewSampleAvailable = delegate { };

        private Vector3 _linearVelocity = Vector3.zero;
        private Vector3 _angularVelocity = Vector3.zero;

        private Vector3? _previousReferencePosition;
        private Quaternion? _previousReferenceRotation;
        private float _accumulatedDelta;

        private List<SamplePoseData> _bufferedPoses = new List<SamplePoseData>();
        private int _lastWritePos = -1;
        private int _bufferSize = -1;

        private List<SamplePoseData> _windowWithMovement = new List<SamplePoseData>();
        private List<SamplePoseData> _tempWindow = new List<SamplePoseData>();

        private const float _TREND_DOT_THRESHOLD = 0.6f;

        protected virtual void Awake()
        {
            ThrowInputDevice = _throwInputDevice as IPoseInputDevice;
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(_bufferingParams);
            _bufferingParams.Validate();

            _bufferSize = Mathf.CeilToInt(_bufferingParams.BufferLengthSeconds
                * _bufferingParams.SampleFrequency);
            _bufferedPoses.Capacity = _bufferSize;

            _linearVelocityFilter = OneEuroFilter.CreateVector3();

            Assert.IsNotNull(ThrowInputDevice);
        }

        public ReleaseVelocityInformation CalculateThrowVelocity(Transform objectThrown)
        {
            Vector3 linearVelocity = Vector3.zero,
                angularVelocity = Vector3.zero;

            IncludeInstantVelocities(ref linearVelocity, ref angularVelocity);

            IncludeTrendVelocities(ref linearVelocity, ref angularVelocity);

            IncludeTangentialInfluence(ref linearVelocity, objectThrown.position);

            IncludeExternalVelocities(ref linearVelocity, ref angularVelocity);

            _currentThrowVelocities.Clear();
            // queue items in order from lastWritePos to earliest sample
            int numPoses = _bufferedPoses.Count;
            for (int readPos = _lastWritePos, itemsRead = 0;
                itemsRead < numPoses; readPos--, itemsRead++)
            {
                if (readPos < 0)
                {
                    readPos = numPoses - 1;
                }
                var item = _bufferedPoses[readPos];
                ReleaseVelocityInformation newSample = new ReleaseVelocityInformation(
                   item.LinearVelocity,
                   item.AngularVelocity,
                   item.TransformPose.position,
                   false);
                _currentThrowVelocities.Add(newSample);
            }
            ReleaseVelocityInformation newVelocity = new ReleaseVelocityInformation(linearVelocity,
                angularVelocity,
                _previousReferencePosition.HasValue ? _previousReferencePosition.Value : Vector3.zero,
                true);
            _currentThrowVelocities.Add(newVelocity);
            WhenThrowVelocitiesChanged(_currentThrowVelocities);

            _bufferedPoses.Clear();
            _lastWritePos = -1;

            _linearVelocityFilter.Reset();

            return newVelocity;
        }

        private void IncludeInstantVelocities(ref Vector3 linearVelocity,
            ref Vector3 angularVelocity)
        {
            Vector3 instantLinearVelocity = Vector3.zero,
                instantAngularVelocity = Vector3.zero;
            IncludeEstimatedReleaseVelocities(ref instantLinearVelocity,
                ref instantAngularVelocity);

            AddedInstantLinearVelocity = instantLinearVelocity * _instantVelocityInfluence;
            linearVelocity += AddedInstantLinearVelocity;
            angularVelocity += instantAngularVelocity * _instantVelocityInfluence;
        }

        private void IncludeEstimatedReleaseVelocities(ref Vector3 linearVelocity,
            ref Vector3 angularVelocity)
        {
            linearVelocity = _linearVelocity;
            angularVelocity = _angularVelocity;

            if (_stepBackTime < Mathf.Epsilon)
            {
                return;
            }

            int beforeIndex, afterIndex;
            float lookupTime = Time.time - _stepBackTime;
            (beforeIndex, afterIndex) = FindPoseIndicesAdjacentToTime(lookupTime);

            if (beforeIndex < 0 || afterIndex < 0)
            {
                return;
            }

            var previousPoseData = _bufferedPoses[beforeIndex];
            var nextPoseData = _bufferedPoses[afterIndex];
            float previousTime = previousPoseData.Time;
            float nextTime = nextPoseData.Time;
            float t = (lookupTime - previousTime) / (nextTime - previousTime);

            Vector3 lerpedVelocity = Vector3.Lerp(previousPoseData.LinearVelocity,
                nextPoseData.LinearVelocity, t);

            Quaternion previousAngularVelocityQuat =
                VelocityCalculatorUtilMethods.AngularVelocityToQuat(previousPoseData.AngularVelocity);
            Quaternion nextAngularVelocityQuat =
                VelocityCalculatorUtilMethods.AngularVelocityToQuat(nextPoseData.AngularVelocity);
            Quaternion lerpedAngularVelocQuat = Quaternion.Slerp(previousAngularVelocityQuat,
                nextAngularVelocityQuat, t);
            Vector3 lerpedAngularVelocity = VelocityCalculatorUtilMethods.QuatToAngularVeloc(
                lerpedAngularVelocQuat);

            linearVelocity = lerpedVelocity;
            angularVelocity = lerpedAngularVelocity;
        }

        private void IncludeTrendVelocities(ref Vector3 linearVelocity,
            ref Vector3 angularVelocity)
        {
            Vector3 trendLinearVelocity, trendAngularVelocity;
            (trendLinearVelocity, trendAngularVelocity) = ComputeTrendVelocities();
            AddedTrendLinearVelocity = trendLinearVelocity * _trendVelocityInfluence;
            linearVelocity += AddedTrendLinearVelocity;
            angularVelocity += trendAngularVelocity * _trendVelocityInfluence;
        }

        private void IncludeTangentialInfluence(ref Vector3 linearVelocity, Vector3 interactablePosition)
        {
            var addedTangentialLinearVelocity = CalculateTangentialVector(interactablePosition);
            AddedTangentialLinearVelocity =
                addedTangentialLinearVelocity * _tangentialVelocityInfluence;
            linearVelocity += AddedTangentialLinearVelocity;
        }

        private void IncludeExternalVelocities(ref Vector3 linearVelocity, ref Vector3 angularVelocity)
        {
            Vector3 extraLinearVelocity, extraAngularVelocity;
            (extraLinearVelocity, extraAngularVelocity) = ThrowInputDevice.GetExternalVelocities();
            float addedExternalSpeed = extraLinearVelocity.magnitude * _externalVelocityInfluence;
            linearVelocity += linearVelocity.normalized * addedExternalSpeed;
            float addedExternalAngularSpeed = extraAngularVelocity.magnitude * _externalVelocityInfluence;
            angularVelocity += angularVelocity.normalized * addedExternalAngularSpeed;
        }

        private (int, int) FindPoseIndicesAdjacentToTime(float time)
        {
            if (_lastWritePos < 0)
            {
                return (-1, -1);
            }
            int beforeIndex = -1, afterIndex = -1;

            int numPoses = _bufferedPoses.Count;
            for (int readPos = _lastWritePos, itemsRead = 0;
                itemsRead < numPoses; readPos--, itemsRead++)
            {
                if (readPos < 0)
                {
                    readPos = numPoses - 1;
                }
                int prevReadPos = readPos - 1;
                if (prevReadPos < 0)
                {
                    prevReadPos = numPoses - 1;
                }
                var currPose = _bufferedPoses[readPos];
                var prevPose = _bufferedPoses[prevReadPos];
                if (currPose.Time > time && prevPose.Time < time)
                {
                    beforeIndex = prevReadPos;
                    afterIndex = readPos;
                }
            }

            return (beforeIndex, afterIndex);
        }

        private (Vector3, Vector3) ComputeTrendVelocities()
        {
            Vector3 trendLinearVelocity = Vector3.zero;
            Vector3 trendAngularVelocity = Vector3.zero;
            if (_bufferedPoses.Count == 0)
            {
                return (Vector3.zero, Vector3.zero);
            }

            if (BufferedVelocitiesValid())
            {
                FindLargestWindowWithMovement();
                int numItemsWithMovement = _windowWithMovement.Count;
                if (numItemsWithMovement == 0)
                {
                    return (Vector3.zero, Vector3.zero);
                }
                foreach (var item in _windowWithMovement)
                {
                    trendLinearVelocity += item.LinearVelocity;
                    trendAngularVelocity += item.AngularVelocity;
                }
                trendLinearVelocity /= numItemsWithMovement;
                trendAngularVelocity /= numItemsWithMovement;
            }
            else
            {
                (trendLinearVelocity, trendAngularVelocity) =
                    FindMostRecentBufferedSampleWithMovement();
            }

            return (trendLinearVelocity, trendAngularVelocity);
        }

        /// <summary>
        /// Do we have enough buffered velocities to derive some sort of trend?
        /// If not, return false. This can happen when a user performs a very fast
        /// overhand or underhand throw that results in mostly zero velocities.
        /// </summary>
        /// <returns></returns>
        private bool BufferedVelocitiesValid()
        {
            int numZeroVectors = 0;

            foreach(var item in _bufferedPoses)
            {
                var velocityVector = item.LinearVelocity;
                if (velocityVector.sqrMagnitude < Mathf.Epsilon)
                {
                    numZeroVectors++;
                }
            }

            int numTotalVectors = _bufferedPoses.Count;
            float percentZero = (float)numZeroVectors / numTotalVectors;
            bool bufferedVelocitiesValid = percentZero > _maxPercentZeroSamplesTrendVeloc ?
                false : true;

            return bufferedVelocitiesValid;
        }

        private void FindLargestWindowWithMovement()
        {
            int numPoses = _bufferedPoses.Count;
            bool processingMovementWindow = false;
            _windowWithMovement.Clear();
            _tempWindow.Clear();
            Vector3 initialVector = Vector3.zero;

            // start backwards from last written sample
            for (int readPos = _lastWritePos, itemsRead = 0;
                itemsRead < numPoses; readPos--, itemsRead++)
            {
                if (readPos < 0)
                {
                    readPos = numPoses - 1;
                }

                var item = _bufferedPoses[readPos];
                bool currentItemHasMovement = item.LinearVelocity.sqrMagnitude > 0.0f;
                if (currentItemHasMovement)
                {
                    if (!processingMovementWindow)
                    {
                        processingMovementWindow = true;
                        _tempWindow.Clear();
                        initialVector = item.LinearVelocity;
                    }

                    // include vectors that are roughly the same direction as initial velocity
                    if (Vector3.Dot(initialVector.normalized, item.LinearVelocity.normalized)
                        > _TREND_DOT_THRESHOLD)
                    {
                        _tempWindow.Add(item);
                    }
                }
                // end of window when we hit something with no speed
                else if (!currentItemHasMovement && processingMovementWindow)
                {
                    processingMovementWindow = false;
                    if (_tempWindow.Count > _windowWithMovement.Count)
                    {
                        TransferToDestBuffer(_tempWindow, _windowWithMovement);
                    }
                }
            }

            // in case window continues till end of buffer
            if (processingMovementWindow)
            {
                if (_tempWindow.Count > _windowWithMovement.Count)
                {
                    TransferToDestBuffer(_tempWindow, _windowWithMovement);
                }
            }
        }

        private (Vector3, Vector3) FindMostRecentBufferedSampleWithMovement()
        {
            int numPoses = _bufferedPoses.Count;
            Vector3 linearVelocity = Vector3.zero;
            Vector3 angularVelocity = Vector3.zero;

            for (int readPos = _lastWritePos, itemsRead = 0;
                itemsRead < numPoses; readPos--, itemsRead++)
            {
                if (readPos < 0)
                {
                    readPos = numPoses - 1;
                }

                var item = _bufferedPoses[readPos];
                var itemLinearVelocity = item.LinearVelocity;
                var itemAngularVelocity = item.AngularVelocity;
                if (itemLinearVelocity.sqrMagnitude > Mathf.Epsilon &&
                    itemAngularVelocity.sqrMagnitude > Mathf.Epsilon)
                {
                    linearVelocity = itemLinearVelocity;
                    angularVelocity = itemAngularVelocity;
                    break;
                }
            }

            return (linearVelocity, angularVelocity);
        }

        private void TransferToDestBuffer(List<SamplePoseData> source,
            List<SamplePoseData> dest)
        {
            dest.Clear();
            foreach (var sourceItem in source)
            {
                dest.Add(sourceItem);
            }
        }

        private Vector3 CalculateTangentialVector(Vector3 objectPosition)
        {
            if (_previousReferencePosition == null)
            {
                return Vector3.zero;
            }

            float angularVelocityMag = _angularVelocity.magnitude;
            if (angularVelocityMag < Mathf.Epsilon)
            {
                return Vector3.zero;
            }

            Vector3 centerOfMassToObject = objectPosition - _previousReferencePosition.Value;
            float radius = centerOfMassToObject.magnitude;
            if (radius < Mathf.Epsilon)
            {
                return Vector3.zero;
            }

            Vector3 centerOfMassToObjectNorm = centerOfMassToObject.normalized;
            Vector3 axisOfRotation = _angularVelocity.normalized;
            Vector3 tangentialDirection = Vector3.Cross(axisOfRotation, centerOfMassToObjectNorm);
            // https://byjus.com/tangential-velocity-formula/
            // https://www.toppr.com/guides/physics-formulas/tangential-velocity-formula/
            AxisOfRotation = axisOfRotation;
            TangentialDirection = tangentialDirection;
            CenterOfMassToObject = centerOfMassToObjectNorm * radius;
            AxisOfRotationOrigin = objectPosition;
            return tangentialDirection * radius * angularVelocityMag;
        }

        public IReadOnlyList<ReleaseVelocityInformation> LastThrowVelocities()
        {
            return _currentThrowVelocities;
        }

        public void SetUpdateFrequency(float frequency)
        {
            if (frequency < Mathf.Epsilon)
            {
                Debug.LogError($"Provided frequency ${frequency} must be " +
                    $"greater than or equal to zero.");
                return;
            }
            _updateFrequency = frequency;
            _updateLatency = 1.0f / _updateFrequency;
        }

        protected virtual void LateUpdate()
        {
            if (_updateLatency > 0.0f && _lastUpdateTime > 0.0f &&
                   (Time.time - _lastUpdateTime) < _updateLatency)
            {
                return;
            }

            Pose referencePose;
            if (!ThrowInputDevice.IsInputValid || !ThrowInputDevice.IsHighConfidence ||
                !ThrowInputDevice.GetRootPose(out referencePose))
            {
                return;
            }

            _lastUpdateTime = Time.time;
            referencePose = new Pose(
                _referenceOffset + referencePose.position,
                referencePose.rotation);
            CalculateLatestVelocitiesAndUpdateBuffer(Time.deltaTime, referencePose);
        }

        private void CalculateLatestVelocitiesAndUpdateBuffer(float delta, Pose referencePose)
        {
            _accumulatedDelta += delta;

            UpdateLatestVelocitiesAndPoseValues(referencePose, _accumulatedDelta);
            _accumulatedDelta = 0.0f;
            int nextWritePos = (_lastWritePos < 0) ?
                0 :
                (_lastWritePos + 1) % _bufferSize;
            var newPose = new SamplePoseData(referencePose, _linearVelocity,
                _angularVelocity, Time.time);
            if (_bufferedPoses.Count <= nextWritePos)
            {
                _bufferedPoses.Add(newPose);
            }
            else
            {
                _bufferedPoses[nextWritePos] = newPose;
            }
            _lastWritePos = nextWritePos;
        }

        private void UpdateLatestVelocitiesAndPoseValues(Pose referencePose, float delta)
        {
            (_linearVelocity, _angularVelocity) = GetLatestLinearAndAngularVelocities(
                referencePose, delta);
             _linearVelocity = _linearVelocityFilter.Step(_linearVelocity);

            var newReleaseVelocInfo = new ReleaseVelocityInformation(_linearVelocity, _angularVelocity,
                referencePose.position);
            WhenNewSampleAvailable(newReleaseVelocInfo);

            _previousReferencePosition = referencePose.position;
            _previousReferenceRotation = referencePose.rotation;
        }

        private (Vector3, Vector3) GetLatestLinearAndAngularVelocities(Pose referencePose,
            float delta)
        {
            // Don't compute any values if they would result in NaN.
            if (!_previousReferencePosition.HasValue || delta < Mathf.Epsilon)
            {
                return (Vector3.zero, Vector3.zero);
            }
            Vector3 newLinearVelocity = (referencePose.position -
                _previousReferencePosition.Value) / delta;
            var newAngularVelocity = VelocityCalculatorUtilMethods.ToAngularVelocity(
                _previousReferenceRotation.Value,
                referencePose.rotation, delta);

            return (newLinearVelocity, newAngularVelocity);
        }

        #region Inject

        public void InjectAllStandardVelocityCalculator(
            IPoseInputDevice poseInputDevice,
            BufferingParams bufferingParams)
        {
            InjectPoseInputDevice(poseInputDevice);
            InjectBufferingParams(bufferingParams);
        }

        public void InjectPoseInputDevice(IPoseInputDevice poseInputDevice)
        {
            _throwInputDevice = poseInputDevice as MonoBehaviour;
            ThrowInputDevice = poseInputDevice;
        }

        public void InjectBufferingParams(BufferingParams bufferingParams)
        {
            _bufferingParams = bufferingParams;
        }

        #endregion
    }
}
