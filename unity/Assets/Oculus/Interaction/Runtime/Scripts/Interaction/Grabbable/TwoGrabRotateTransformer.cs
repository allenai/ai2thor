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
    /// A Transformer that rotates the target about an axis, given two grab points.
    /// Updates apply relative rotational changes, relative to the angle change between the two
    /// grab points each frame.
    /// The axis is defined by a pivot transform: a world position and up vector.
    /// </summary>
    public class TwoGrabRotateTransformer : MonoBehaviour, ITransformer
    {
        public enum Axis
        {
            Right = 0,
            Up = 1,
            Forward = 2
        }

        [SerializeField, Optional]
        private Transform _pivotTransform = null;

        private Transform PivotTransform =>
            _pivotTransform != null ? _pivotTransform : _grabbable.Transform;

        [SerializeField]
        private Axis _rotationAxis = Axis.Up;

        [Serializable]
        public class TwoGrabRotateConstraints
        {
            public FloatConstraint MinAngle;
            public FloatConstraint MaxAngle;
        }

        [SerializeField]
        private TwoGrabRotateConstraints _constraints;

        private float _relativeAngle = 0.0f;
        private float _constrainedRelativeAngle = 0.0f;

        private IGrabbable _grabbable;

        // vector from the hand at the first grab point to the hand on the second grab point,
        // projected onto the plane of the rotation.
        private Vector3 _previousHandsVectorOnPlane;

        public void Initialize(IGrabbable grabbable)
        {
            _grabbable = grabbable;
        }

        public void BeginTransform()
        {
            Vector3 rotationAxis = CalculateRotationAxisInWorldSpace();
            _previousHandsVectorOnPlane = CalculateHandsVectorOnPlane(rotationAxis);
            _relativeAngle = _constrainedRelativeAngle;
        }

        public void UpdateTransform()
        {
            Vector3 rotationAxis = CalculateRotationAxisInWorldSpace();
            Vector3 handsVector = CalculateHandsVectorOnPlane(rotationAxis);
            float angleDelta =
                Vector3.SignedAngle(_previousHandsVectorOnPlane, handsVector, rotationAxis);

            float previousAngle = _constrainedRelativeAngle;
            _relativeAngle += angleDelta;
            _constrainedRelativeAngle = _relativeAngle;
            if (_constraints.MinAngle.Constrain)
            {
                _constrainedRelativeAngle =
                    Mathf.Max(_constrainedRelativeAngle, _constraints.MinAngle.Value);
            }

            if (_constraints.MaxAngle.Constrain)
            {
                _constrainedRelativeAngle =
                    Mathf.Min(_constrainedRelativeAngle, _constraints.MaxAngle.Value);
            }

            angleDelta = _constrainedRelativeAngle - previousAngle;

            // Apply this angle rotation about the axis to our transform
            _grabbable.Transform.RotateAround(PivotTransform.position, rotationAxis, angleDelta);

            _previousHandsVectorOnPlane = handsVector;
        }

        public void EndTransform() { }

        private Vector3 CalculateRotationAxisInWorldSpace()
        {
            Vector3 worldAxis = Vector3.zero;
            worldAxis[(int)_rotationAxis] = 1f;
            return PivotTransform.TransformDirection(worldAxis);
        }

        private Vector3 CalculateHandsVectorOnPlane(Vector3 planeNormal)
        {
            Vector3[] grabPointsOnPlane =
            {
                Vector3.ProjectOnPlane(_grabbable.GrabPoints[0].position, planeNormal),
                Vector3.ProjectOnPlane(_grabbable.GrabPoints[1].position, planeNormal),
            };

            return grabPointsOnPlane[1] - grabPointsOnPlane[0];
        }

        #region Inject

        public void InjectOptionalPivotTransform(Transform pivotTransform)
        {
            _pivotTransform = pivotTransform;
        }

        public void InjectOptionalRotationAxis(Axis rotationAxis)
        {
            _rotationAxis = rotationAxis;
        }

        public void InjectOptionalConstraints(TwoGrabRotateConstraints constraints)
        {
            _constraints = constraints;
        }

        #endregion
    }
}
