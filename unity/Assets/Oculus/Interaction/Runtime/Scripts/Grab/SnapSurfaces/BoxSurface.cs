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
    public class BoxSurfaceData : ICloneable
    {
        public object Clone()
        {
            BoxSurfaceData clone = new BoxSurfaceData();
            clone.widthOffset = this.widthOffset;
            clone.snapOffset = this.snapOffset;
            clone.size = this.size;
            clone.eulerAngles = this.eulerAngles;
            return clone;
        }

        public BoxSurfaceData Mirror()
        {
            BoxSurfaceData mirror = Clone() as BoxSurfaceData;
            mirror.snapOffset = new Vector4(
                -mirror.snapOffset.y, -mirror.snapOffset.x,
                -mirror.snapOffset.w, -mirror.snapOffset.z);

            return mirror;
        }


        [Range(0f, 1f)]
        public float widthOffset = 0.5f;
        public Vector4 snapOffset;
        public Vector3 size = new Vector3(0.1f, 0f, 0.1f);
        public Vector3 eulerAngles;
    }

    /// <summary>
    /// This SnapSurface defines a Rectangle around which the grip point is valid.
    /// Since the grip point might be offset from the fingers, a valid range for each opposite
    /// side of the rectangle can be set so the grabbing fingers are constrained to the object bounds.
    /// </summary>
    [Serializable]
    public class BoxSurface : MonoBehaviour, ISnapSurface
    {
        [SerializeField]
        protected BoxSurfaceData _data = new BoxSurfaceData();

        /// <summary>
        /// Getter for the data-only version of this surface. Used so it can be stored when created
        /// at Play-Mode.
        /// </summary>
        public BoxSurfaceData Data
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
        /// The lateral displacement of the grip point in the main side.
        /// </summary>
        public float WidthOffset
        {
            get
            {
                return _data.widthOffset;
            }
            set
            {
                _data.widthOffset = value;
            }
        }

        /// <summary>
        /// The range at which the sides are constrained.
        /// X,Y for the back and forward sides range.
        /// Z,W for the left and right sides range.
        /// </summary>
        public Vector4 SnapOffset
        {
            get
            {
                return _data.snapOffset;
            }
            set
            {
                _data.snapOffset = value;
            }
        }

        /// <summary>
        /// The size of the rectangle. Y is ignored.
        /// </summary>
        public Vector3 Size
        {
            get
            {
                return _data.size;
            }
            set
            {
                _data.size = value;
            }
        }

        /// <summary>
        /// The rotation of the rectangle from the Grip point
        /// </summary>
        public Quaternion Rotation
        {
            get
            {
                return this.RelativeTo.rotation * Quaternion.Euler(_data.eulerAngles);
            }
            set
            {
                _data.eulerAngles = (Quaternion.Inverse(this.RelativeTo.rotation) * value).eulerAngles;
            }
        }

        /// <summary>
        /// The forward direction of the rectangle (based on its rotation)
        /// </summary>
        public Vector3 Direction
        {
            get
            {
                return Rotation * Vector3.forward;
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
            Vector3 normal = Quaternion.Inverse(this.RelativeTo.rotation) * Direction;
            Vector3 tangent = Quaternion.Inverse(this.RelativeTo.rotation) * (Rotation * Vector3.up);
            return pose.MirrorPoseRotation(normal, tangent);
        }

        public ISnapSurface CreateMirroredSurface(GameObject gameObject)
        {
            BoxSurface surface = gameObject.AddComponent<BoxSurface>();
            surface.Data = _data.Mirror();
            return surface;
        }

        public ISnapSurface CreateDuplicatedSurface(GameObject gameObject)
        {
            BoxSurface surface = gameObject.AddComponent<BoxSurface>();
            surface.Data = _data;
            return surface;
        }

        public float CalculateBestPoseAtSurface(in Pose targetPose, in Pose reference, out Pose bestPose, in PoseMeasureParameters scoringModifier)
        {
            return SnapSurfaceHelper.CalculateBestPoseAtSurface(targetPose, reference, out bestPose,
                scoringModifier, MinimalTranslationPoseAtSurface, MinimalRotationPoseAtSurface);
        }

        private void CalculateCorners(out Vector3 bottomLeft, out Vector3 bottomRight, out Vector3 topLeft, out Vector3 topRight)
        {
            Vector3 rightRot = Rotation * Vector3.right;
            bottomLeft = GripPoint.position - rightRot * _data.size.x * (1f - _data.widthOffset);
            bottomRight = GripPoint.position + rightRot * _data.size.x * (_data.widthOffset);
            Vector3 forwardOffset = Rotation * Vector3.forward * _data.size.z;
            topLeft = bottomLeft + forwardOffset;
            topRight = bottomRight + forwardOffset;
        }

        private Vector3 ProjectOnSegment(Vector3 point, (Vector3, Vector3) segment)
        {
            Vector3 line = segment.Item2 - segment.Item1;
            Vector3 projection = Vector3.Project(point - segment.Item1, line);
            if (Vector3.Dot(projection, line) < 0f)
            {
                projection = segment.Item1;
            }
            else if (projection.magnitude > line.magnitude)
            {
                projection = segment.Item2;
            }
            else
            {
                projection += segment.Item1;
            }
            return projection;
        }

        public bool CalculateBestPoseAtSurface(Ray targetRay, in Pose recordedPose, out Pose bestPose)
        {
            Plane plane = new Plane(Rotation * Vector3.up, this.transform.position);
            plane.Raycast(targetRay, out float rayDistance);
            Vector3 proximalPoint = targetRay.origin + targetRay.direction * rayDistance;

            Vector3 surfacePoint = NearestPointInSurface(proximalPoint);
            Pose desiredPose = new Pose(surfacePoint, recordedPose.rotation);
            bestPose = MinimalTranslationPoseAtSurface(desiredPose, recordedPose);
            return true;
        }

        protected Vector3 NearestPointInSurface(Vector3 targetPosition)
        {
            NearestPointAndAngleInSurface(targetPosition, out Vector3 surfacePoint, out float angle);
            return surfacePoint;
        }

        private void NearestPointAndAngleInSurface(Vector3 targetPosition, out Vector3 surfacePoint, out float angle)
        {
            Vector3 rightDir = Rotation * Vector3.right;
            Vector3 forwardDir = Rotation * Vector3.forward;
            Vector3 bottomLeft, bottomRight, topLeft, topRight;
            CalculateCorners(out bottomLeft, out bottomRight, out topLeft, out topRight);

            Vector3 bottomP = ProjectOnSegment(targetPosition, (bottomLeft + rightDir * SnapOffset.y, bottomRight + rightDir * SnapOffset.x));
            Vector3 topP = ProjectOnSegment(targetPosition, (topLeft - rightDir * SnapOffset.x, topRight - rightDir * SnapOffset.y));
            Vector3 leftP = ProjectOnSegment(targetPosition, (bottomLeft - forwardDir * SnapOffset.z, topLeft - forwardDir * SnapOffset.w));
            Vector3 rightP = ProjectOnSegment(targetPosition, (bottomRight + forwardDir * SnapOffset.w, topRight + forwardDir * SnapOffset.z));

            float bottomDistance = Vector3.Distance(bottomP, targetPosition);
            float topDistance = Vector3.Distance(topP, targetPosition);
            float leftDistance = Vector3.Distance(leftP, targetPosition);
            float rightDistance = Vector3.Distance(rightP, targetPosition);

            float minDistance = Mathf.Min(bottomDistance, Mathf.Min(topDistance, Mathf.Min(leftDistance, rightDistance)));
            if (bottomDistance == minDistance)
            {
                surfacePoint = bottomP;
                angle = 0f;
                return;
            }
            if (topDistance == minDistance)
            {
                surfacePoint = topP;
                angle = 180f;
                return;
            }
            if (leftDistance == minDistance)
            {
                surfacePoint = leftP;
                angle = 90f;
                return;
            }
            surfacePoint = rightP;
            angle = -90f;
        }

        protected Pose MinimalRotationPoseAtSurface(in Pose userPose, in Pose snapPose)
        {
            Vector3 desiredPos = userPose.position;
            Quaternion baseRot = snapPose.rotation;
            Quaternion desiredRot = userPose.rotation;
            Vector3 up = Rotation * Vector3.up;

            Quaternion bottomRot = baseRot;
            Quaternion topRot = Quaternion.AngleAxis(180f, up) * baseRot;
            Quaternion leftRot = Quaternion.AngleAxis(90f, up) * baseRot;
            Quaternion rightRot = Quaternion.AngleAxis(-90f, up) * baseRot;

            float bottomDot = PoseUtils.RotationalSimilarity(bottomRot, desiredRot);
            float topDot = PoseUtils.RotationalSimilarity(topRot, desiredRot);
            float leftDot = PoseUtils.RotationalSimilarity(leftRot, desiredRot);
            float rightDot = PoseUtils.RotationalSimilarity(rightRot, desiredRot);

            Vector3 rightDir = Rotation * Vector3.right;
            Vector3 forwardDir = Rotation * Vector3.forward;
            Vector3 bottomLeft, bottomRight, topLeft, topRight;
            CalculateCorners(out bottomLeft, out bottomRight, out topLeft, out topRight);

            float maxDot = Mathf.Max(bottomDot, Mathf.Max(topDot, Mathf.Max(leftDot, rightDot)));
            if (bottomDot == maxDot)
            {
                Vector3 projBottom = ProjectOnSegment(desiredPos, (bottomLeft + rightDir * SnapOffset.y, bottomRight + rightDir * SnapOffset.x));
                return new Pose(projBottom, bottomRot);
            }
            if (topDot == maxDot)
            {
                Vector3 projTop = ProjectOnSegment(desiredPos, (topLeft - rightDir * SnapOffset.x, topRight - rightDir * SnapOffset.y));
                return new Pose(projTop, topRot);
            }
            if (leftDot == maxDot)
            {
                Vector3 projLeft = ProjectOnSegment(desiredPos, (bottomLeft - forwardDir * SnapOffset.z, topLeft - forwardDir * SnapOffset.w));
                return new Pose(projLeft, leftRot);
            }
            Vector3 projRight = ProjectOnSegment(desiredPos, (bottomRight + forwardDir * SnapOffset.w, topRight + forwardDir * SnapOffset.z));
            return new Pose(projRight, rightRot);
        }

        protected Pose MinimalTranslationPoseAtSurface(in Pose userPose, in Pose snapPose)
        {
            Vector3 desiredPos = userPose.position;
            Quaternion baseRot = snapPose.rotation;
            Vector3 surfacePoint;
            float surfaceAngle;
            NearestPointAndAngleInSurface(desiredPos, out surfacePoint, out surfaceAngle);
            Quaternion surfaceRotation = RotateUp(baseRot, surfaceAngle);
            return new Pose(surfacePoint, surfaceRotation);
        }

        protected Quaternion RotateUp(Quaternion baseRot, float angle)
        {
            Quaternion offset = Quaternion.AngleAxis(angle, Rotation * Vector3.up);
            return offset * baseRot;
        }

        #region Inject

        public void InjectAllBoxSurface(BoxSurfaceData data,
            Transform relativeTo, Transform gripPoint)
        {
            InjectData(data);
            InjectRelativeTo(relativeTo);
            InjectGripPoint(gripPoint);
        }

        public void InjectData(BoxSurfaceData data)
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
