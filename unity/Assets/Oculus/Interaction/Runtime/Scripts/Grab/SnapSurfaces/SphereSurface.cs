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
using UnityEngine.Assertions;
using System;

namespace Oculus.Interaction.HandGrab.SnapSurfaces
{
    [Serializable]
    public class SphereSurfaceData : ICloneable
    {
        public object Clone()
        {
            SphereSurfaceData clone = new SphereSurfaceData();
            clone.centre = this.centre;
            return clone;
        }

        public SphereSurfaceData Mirror()
        {
            SphereSurfaceData mirror = Clone() as SphereSurfaceData;
            return mirror;
        }

        public Vector3 centre;
    }

    /// <summary>
    /// Specifies an entire sphere around an object in which the grip point is valid.
    ///
    /// One of the main advantages of spheres is that the rotation of the hand pose does
    /// not really matters, as it will always fit the surface correctly.
    /// </summary>
    [Serializable]
    public class SphereSurface : MonoBehaviour, ISnapSurface
    {

        [SerializeField]
        protected SphereSurfaceData _data = new SphereSurfaceData();

        /// <summary>
        /// Getter for the data-only version of this surface. Used so it can be stored when created
        /// at Play-Mode.
        /// </summary>
        public SphereSurfaceData Data
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
            }
        }

        [SerializeField]
        private Transform _relativeTo;

        [SerializeField]
        private Transform _gripPoint;

        /// <summary>
        /// Transform to which the surface refers to.
        /// </summary>
        public Transform RelativeTo
        {
            get => _relativeTo;
            set => _relativeTo = value;
        }
        /// <summary>
        /// Valid point at which the hand can snap, typically the SnapPoint position itself.
        /// </summary>
        public Transform GripPoint
        {
            get => _gripPoint;
            set => _gripPoint = value;
        }

        /// <summary>
        /// The center of the sphere in world coordinates.
        /// </summary>
        public Vector3 Centre
        {
            get
            {
                if (RelativeTo != null)
                {
                    return RelativeTo.TransformPoint(_data.centre);
                }
                else
                {
                    return _data.centre;
                }
            }
            set
            {
                if (RelativeTo != null)
                {
                    _data.centre = RelativeTo.InverseTransformPoint(value);
                }
                else
                {
                    _data.centre = value;
                }
            }
        }

        /// <summary>
        /// The radius of the sphere, this is automatically calculated as the distance between
        /// the center and the original grip pose.
        /// </summary>
        public float Radius
        {
            get
            {
                if (this.GripPoint == null)
                {
                    return 0f;
                }
                return Vector3.Distance(Centre, this.GripPoint.position);
            }
        }

        /// <summary>
        /// The direction of the sphere, measured from the center to the original grip position.
        /// </summary>
        public Vector3 Direction
        {
            get
            {
                return (this.GripPoint.position - Centre).normalized;
            }
        }

        /// <summary>
        /// The rotation of the sphere from the recorded grip position.
        /// </summary>
        public Quaternion Rotation
        {
            get
            {
                return Quaternion.LookRotation(Direction, this.GripPoint.forward);
            }
        }

        #region editor events
        private void Reset()
        {
            _gripPoint = this.transform;
            if (this.TryGetComponent(out HandGrabPose grabPose))
            {
                _relativeTo = grabPose.RelativeTo;
            }
        }
        #endregion


        protected virtual void Start()
        {
            Assert.IsNotNull(_relativeTo);
            Assert.IsNotNull(_gripPoint);
            Assert.IsNotNull(_data);
        }

        public Pose MirrorPose(in Pose pose)
        {
            Vector3 normal = Quaternion.Inverse(RelativeTo.rotation) * Direction;
            Vector3 tangent = Vector3.Cross(normal, Vector3.up);
            return pose.MirrorPoseRotation(normal, tangent);
        }

        public bool CalculateBestPoseAtSurface(Ray targetRay, in Pose recordedPose, out Pose bestPose)
        {
            Vector3 projection = Vector3.Project(Centre - targetRay.origin, targetRay.direction);
            Vector3 nearestCentre = targetRay.origin + projection;
            float distanceToSurface = Mathf.Max(Vector3.Distance(Centre, nearestCentre) - Radius);
            if (distanceToSurface < Radius)
            {
                float adjustedDistance = Mathf.Sqrt(Radius * Radius - distanceToSurface * distanceToSurface);
                nearestCentre -= targetRay.direction * adjustedDistance;
            }


            Vector3 surfacePoint = NearestPointInSurface(nearestCentre);
            Pose desiredPose = new Pose(surfacePoint, recordedPose.rotation);
            bestPose = MinimalTranslationPoseAtSurface(desiredPose, recordedPose);
            return true;
        }

        public float CalculateBestPoseAtSurface(in Pose targetPose, in Pose reference, out Pose bestPose, in PoseMeasureParameters scoringModifier)
        {
            return SnapSurfaceHelper.CalculateBestPoseAtSurface(targetPose, reference, out bestPose,
                scoringModifier, MinimalTranslationPoseAtSurface, MinimalRotationPoseAtSurface);
        }

        public ISnapSurface CreateMirroredSurface(GameObject gameObject)
        {
            SphereSurface surface = gameObject.AddComponent<SphereSurface>();
            surface.Data = _data.Mirror();
            return surface;
        }

        public ISnapSurface CreateDuplicatedSurface(GameObject gameObject)
        {
            SphereSurface surface = gameObject.AddComponent<SphereSurface>();
            surface.Data = _data;
            return surface;
        }

        protected Vector3 NearestPointInSurface(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - Centre).normalized;
            return Centre + direction * Radius;
        }

        protected Pose MinimalRotationPoseAtSurface(in Pose userPose, in Pose snapPose)
        {
            Quaternion rotCorrection = userPose.rotation * Quaternion.Inverse(snapPose.rotation);
            Vector3 correctedDir = rotCorrection * Direction;
            Vector3 surfacePoint = NearestPointInSurface(Centre + correctedDir * Radius);
            Quaternion surfaceRotation = RotationAtPoint(surfacePoint, snapPose.rotation, userPose.rotation);
            return new Pose(surfacePoint, surfaceRotation);
        }

        protected Pose MinimalTranslationPoseAtSurface(in Pose userPose, in Pose snapPose)
        {
            Vector3 desiredPos = userPose.position;
            Quaternion baseRot = snapPose.rotation;
            Vector3 surfacePoint = NearestPointInSurface(desiredPos);
            Quaternion surfaceRotation = RotationAtPoint(surfacePoint, baseRot, userPose.rotation);
            return new Pose(surfacePoint, surfaceRotation);
        }

        protected Quaternion RotationAtPoint(Vector3 surfacePoint, Quaternion baseRot, Quaternion desiredRotation)
        {
            Vector3 desiredDirection = (surfacePoint - Centre).normalized;
            Quaternion targetRotation = Quaternion.FromToRotation(Direction, desiredDirection) * baseRot;
            Vector3 targetProjected = Vector3.ProjectOnPlane(targetRotation * Vector3.forward, desiredDirection).normalized;
            Vector3 desiredProjected = Vector3.ProjectOnPlane(desiredRotation * Vector3.forward, desiredDirection).normalized;
            Quaternion rotCorrection = Quaternion.FromToRotation(targetProjected, desiredProjected);
            return rotCorrection * targetRotation;
        }

        #region Inject

        public void InjectAllSphereSurface(SphereSurfaceData data,
            Transform relativeTo, Transform gripPoint)
        {
            InjectData(data);
            InjectRelativeTo(relativeTo);
            InjectGripPoint(gripPoint);
        }

        public void InjectData(SphereSurfaceData data)
        {
            _data = data;
        }

        public void InjectRelativeTo(Transform relativeTo)
        {
            _relativeTo = relativeTo;
        }
        public void InjectGripPoint(Transform gripPoint)
        {
            _gripPoint = gripPoint;
        }

        #endregion
    }
}
