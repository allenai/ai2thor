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

namespace Oculus.Interaction
{
    public class ConicalFrustum : MonoBehaviour
    {
        [SerializeField]
        [Min(0f)]
        private float _minLength = 0f;

        [SerializeField]
        [Min(0f)]
        private float _maxLength = 5f;

        [SerializeField]
        [Min(0f)]
        private float _radiusStart = 0.03f;

        [SerializeField]
        [Range(0f, 60f)]
        private float _apertureDegrees = 20f;

        public Pose Pose => this.transform.GetPose();

        #region public properties
        public float MinLength
        {
            get
            {
                return _minLength;
            }
            set
            {
                _minLength = value;
            }
        }

        public float MaxLength
        {
            get
            {
                return _maxLength;
            }
            set
            {
                _maxLength = value;
            }
        }

        public float RadiusStart
        {
            get
            {
                return _radiusStart;
            }
            set
            {
                _radiusStart = value;
            }
        }

        public float ApertureDegrees
        {
            get
            {
                return _apertureDegrees;
            }
            set
            {
                _apertureDegrees = value;
            }
        }

        public Vector3 StartPoint
        {
            get
            {
                return this.transform.position + Direction * MinLength;
            }
        }

        public Vector3 EndPoint
        {
            get
            {
                return this.transform.position + Direction * MaxLength;
            }
        }

        public Vector3 Direction
        {
            get
            {
                return this.transform.forward;
            }
        }
        #endregion

        public bool IsPointInConeFrustum(Vector3 point)
        {
            Vector3 coneOriginToPoint = point - this.transform.position;
            Vector3 pointProjection = Vector3.Project(coneOriginToPoint, Direction);
            if(Vector3.Dot(pointProjection, Direction) < 0)
            {
                return false;
            }
            float pointLength = pointProjection.magnitude;

            if (pointLength < _minLength
                || pointLength > _maxLength)
            {
                return false;
            }

            float pointRadius = Vector3.Distance(Pose.position + pointProjection, point);
            return pointRadius <= ConeFrustumRadiusAtLength(pointLength);
        }

        public float ConeFrustumRadiusAtLength(float length)
        {
            float angleRadius = Mathf.Asin(_apertureDegrees * Mathf.Deg2Rad);
            float radiusEnd = angleRadius * _maxLength;

            float lengthRatio = length / _maxLength;
            float radiusAtLength = Mathf.Lerp(_radiusStart, radiusEnd, lengthRatio);
            return radiusAtLength;
        }

        public bool HitsCollider(Collider collider, out float score, out Vector3 point)
        {
            Vector3 centerPosition = collider.bounds.center;
            Vector3 projectedCenter = Pose.position
                + Vector3.Project(centerPosition - Pose.position, Pose.forward);
            point = collider.ClosestPointOnBounds(projectedCenter);

            if (!IsPointInConeFrustum(point))
            {
                score = 0f;
                return false;
            }

            Vector3 originToInteractable = point - Pose.position;
            float angleToInteractable = Vector3.Angle(originToInteractable.normalized, Pose.forward);
            score = 1f - Mathf.Clamp01(angleToInteractable / _apertureDegrees);
            return true;
        }

        public Vector3 NearestColliderHit(Collider collider, out float score)
        {
            Vector3 centerPosition = collider.bounds.center;
            Vector3 projectedCenter = Pose.position
                + Vector3.Project(centerPosition - Pose.position, Pose.forward);
            Vector3 point = collider.ClosestPointOnBounds(projectedCenter);

            Vector3 originToInteractable = point - Pose.position;
            float vectorAngle = Vector3.Angle(originToInteractable.normalized, Pose.forward);
            score = 1f - Mathf.Clamp01(vectorAngle / _apertureDegrees);

            return point;
        }
    }
}
