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
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    /// <summary>
    /// A Transformer that rotates the target about an axis.
    /// Updates apply relative rotational changes of a GrabPoint about an axis.
    /// The axis is defined by a pivot transform: a world position and up vector.
    /// </summary>
    public class OneGrabRotateTransformer : MonoBehaviour, ITransformer
    {
        public enum Axis
        {
            Right = 0,
            Up = 1,
            Forward = 2
        }

        [SerializeField, Optional]
        private Transform _pivotTransform = null;

        [SerializeField]
        private Axis _rotationAxis = Axis.Up;

        [Serializable]
        public class OneGrabRotateConstraints
        {
            public FloatConstraint MinAngle;
            public FloatConstraint MaxAngle;
        }

        [SerializeField]
        private OneGrabRotateConstraints _constraints;

        public OneGrabRotateConstraints Constraints
        {
            get
            {
                return _constraints;
            }

            set
            {
                _constraints = value;
            }
        }

        private float _relativeAngle = 0.0f;
        private float _constrainedRelativeAngle = 0.0f;

        private IGrabbable _grabbable;

        private Pose _previousGrabPose;

        public void Initialize(IGrabbable grabbable)
        {
            _grabbable = grabbable;
        }

        public void BeginTransform()
        {
            _relativeAngle = _constrainedRelativeAngle;
            var grabPoint = _grabbable.GrabPoints[0];
            _previousGrabPose = grabPoint;
        }

        public void UpdateTransform()
        {
            var grabPoint = _grabbable.GrabPoints[0];
            var targetTransform = _grabbable.Transform;

            Transform pivot = _pivotTransform != null ? _pivotTransform : targetTransform;
            Vector3 worldAxis = Vector3.zero;
            worldAxis[(int)_rotationAxis] = 1f;
            Vector3 rotationAxis = pivot.TransformDirection(worldAxis);

            // Project our positional offsets onto a plane with normal equal to the rotation axis
            Vector3 initialOffset = _previousGrabPose.position - pivot.position;
            Vector3 initialVector = Vector3.ProjectOnPlane(initialOffset, rotationAxis);

            Vector3 targetOffset = grabPoint.position - pivot.position;
            Vector3 targetVector = Vector3.ProjectOnPlane(targetOffset, rotationAxis);

            // Shortest angle between two planar vectors is the angle about the axis
            // Because we know the vectors are planar, we derive the sign ourselves
            float angleDelta = Vector3.Angle(initialVector, targetVector);
            angleDelta *= Vector3.Dot(Vector3.Cross(initialVector, targetVector), rotationAxis) > 0.0f ? 1.0f : -1.0f;

            float previousAngle = _constrainedRelativeAngle;

            _relativeAngle += angleDelta;
            _constrainedRelativeAngle = _relativeAngle;
            if (_constraints.MinAngle.Constrain)
            {
                _constrainedRelativeAngle = Mathf.Max(_constrainedRelativeAngle, _constraints.MinAngle.Value);
            }

            if (_constraints.MaxAngle.Constrain)
            {
                _constrainedRelativeAngle = Mathf.Min(_constrainedRelativeAngle, _constraints.MaxAngle.Value);
            }

            angleDelta = _constrainedRelativeAngle - previousAngle;

            // Apply this angle rotation about the axis to our transform
            targetTransform.RotateAround(pivot.position, rotationAxis, angleDelta);

            _previousGrabPose = grabPoint;
        }

        public void EndTransform() { }

        #region Inject

        public void InjectOptionalPivotTransform(Transform pivotTransform)
        {
            _pivotTransform = pivotTransform;
        }

        public void InjectOptionalRotationAxis(Axis rotationAxis)
        {
            _rotationAxis = rotationAxis;
        }

        public void InjectOptionalConstraints(OneGrabRotateConstraints constraints)
        {
            _constraints = constraints;
        }

        #endregion
    }
}
