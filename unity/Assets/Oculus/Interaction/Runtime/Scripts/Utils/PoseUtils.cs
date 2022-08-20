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
    /// <summary>
    /// Tools for working with Unity Poses
    /// </summary>
    public static class PoseUtils
    {
        /// <summary>
        /// Assigns a Pose to a given transform.
        /// </summary>
        /// <param name="transform"> The transform to which apply the pose.</param>
        /// <param name="pose">The desired pose.</param>
        /// <param name="space">If the pose should be applied to the local position/rotation or world position/rotation.</param>
        public static void SetPose(this Transform transform, in Pose pose, Space space = Space.World)
        {
            if (space == Space.World)
            {
                transform.SetPositionAndRotation(pose.position, pose.rotation);
            }
            else
            {
                transform.localRotation = pose.rotation;
                transform.localPosition = pose.position;
            }
        }

        /// <summary>
        /// Extract the position/rotation of a given transform.
        /// </summary>
        /// <param name="transform">The transform from which to extract the pose.</param>
        /// <param name="space">If the desired position/rotation is the world or local one.</param>
        /// <returns>A Pose containing the position/rotation of the transform.</returns>
        public static Pose GetPose(this Transform transform, Space space = Space.World)
        {
            if (space == Space.World)
            {
                return new Pose(transform.position, transform.rotation);
            }
            else
            {
                return new Pose(transform.localPosition, transform.localRotation);
            }
        }

        /// <summary>
        /// Compose two poses, applying the first to the second one.
        /// </summary>
        /// <param name="a">First pose to compose.</param>
        /// <param name="b">Pose to compose over the first one.</param>
        /// <param name="result">A Pose with the two operands applied.</param>
        public static void Multiply(in Pose a, in Pose b, ref Pose result)
        {
            result.position = a.position + a.rotation * b.position;
            result.rotation = a.rotation * b.rotation;
        }

        public static Pose Multiply(in Pose a, in Pose b)
        {
            Pose result = new Pose();
            Multiply(a, b, ref result);
            return result;
        }

        /// <summary>
        /// Compose two poses, applying the provided one on top of the caller.
        /// </summary>
        /// <param name="a">Pose to compose upon.</param>
        /// <param name="b">Pose to compose over the first one.</param>
        public static void Premultiply(this ref Pose a, in Pose b)
        {
            Multiply(a, b, ref a);
        }

        /// <summary>
        /// Compose two poses, applying the caller on top of the provided pose.
        /// </summary>
        /// <param name="a">Pose to compose upon.</param>
        /// <param name="b">Pose to compose over the first one.</param>
        public static void Postmultiply(this ref Pose a, in Pose b)
        {
            Multiply(b, a, ref a);
        }

        /// <summary>
        /// Moves the calling pose towards a target one using interpolation
        /// </summary>
        /// <param name="from">Original pose to interpolate from</param>
        /// <param name="to">Target pose for the interpolation.</param>
        /// <param name="t">Interpolation factor, normalized but will not be clamped.</param>
        public static void Lerp(this ref Pose from, in Pose to, float t)
        {
            Lerp(from, to, t, ref from);
        }

        /// <summary>
        /// Interpolation between two poses.
        /// </summary>
        /// <param name="from">From pose.</param>
        /// <param name="to">To pose.</param>
        /// <param name="t">Interpolation factor,  normalized but will not be clamped.</param>
        /// <param name="result">A Pose between a and b</param>
        public static void Lerp(in Pose from, in Pose to, float t, ref Pose result)
        {
            result.position = Vector3.LerpUnclamped(from.position, to.position, t);
            result.rotation = Quaternion.SlerpUnclamped(from.rotation, to.rotation, t);
        }

        public static void Inverse(in Pose a, ref Pose result)
        {
            result.rotation = Quaternion.Inverse(a.rotation);
            result.position = result.rotation * -a.position;
        }

        public static void Invert(this ref Pose a)
        {
            Inverse(a, ref a);
        }

        public static void CopyFrom(this ref Pose to, in Pose from)
        {
            to.position = from.position;
            to.rotation = from.rotation;
        }

        /// <summary>
        /// Get the position/rotation difference between two transforms.
        /// </summary>
        /// <param name="from">The base transform.</param>
        /// <param name="to">The target transform.</param>
        /// <returns>A Pose indicating the position/rotation change</returns>
        public static Pose Delta(this Transform from, Transform to)
        {
            return Delta(from.position, from.rotation, to.position, to.rotation);
        }

        /// <summary>
        /// Get the position/rotation difference between a transform and a pose.
        /// </summary>
        /// <param name="from">The base transform.</param>
        /// <param name="to">The target pose.</param>
        /// <returns>A Pose indicating the delta.</returns>
        public static Pose Delta(this Transform from, in Pose to)
        {
            return Delta(from.position, from.rotation, to.position, to.rotation);
        }

        public static void Delta(this Transform from, in Pose to, ref Pose result)
        {
            Delta(from.position, from.rotation, to.position, to.rotation, ref result);
        }

        /// <summary>
        /// Get the position/rotation difference between two poses.
        /// </summary>
        /// <param name="from">The base pose.</param>
        /// <param name="to">The target pose.</param>
        /// <returns>A Pose indicating the delta.</returns>
        public static Pose Delta(in Pose from, in Pose to)
        {
            return Delta(from.position, from.rotation, to.position, to.rotation);
        }

        /// <summary>
        /// Get the position/rotation difference between two poses, indicated with separated positions and rotations.
        /// </summary>
        /// <param name="fromPosition">The base position.</param>
        /// <param name="fromRotation">The base rotation.</param>
        /// <param name="toPosition">The target position.</param>
        /// <param name="toRotation">The target rotation.</param>
        /// <returns>A Pose indicating the delta.</returns>
        private static Pose Delta(Vector3 fromPosition, Quaternion fromRotation, Vector3 toPosition, Quaternion toRotation)
        {
            Pose result = new Pose();
            Delta(fromPosition, fromRotation, toPosition, toRotation, ref result);
            return result;
        }

        private static void Delta(Vector3 fromPosition, Quaternion fromRotation, Vector3 toPosition, Quaternion toRotation, ref Pose result)
        {
            Quaternion inverseFromRot = Quaternion.Inverse(fromRotation);
            result.position = inverseFromRot * (toPosition - fromPosition);
            result.rotation = inverseFromRot * toRotation;
        }

        /// <summary>
        /// Get the world position/rotation of a relative position.
        /// </summary>
        /// <param name="reference">The transform in which the offset is local.</param>
        /// <param name="offset">The offset from the reference.</param>
        /// <returns>A Pose in world units.</returns>
        public static Pose GlobalPose(this Transform reference, in Pose offset)
        {
            return new Pose(
                reference.position + reference.rotation * offset.position,
                reference.rotation * offset.rotation);
        }

        /// <summary>
        /// Indicates how similar two poses are.
        /// </summary>
        /// <param name="from">First pose to compare.</param>
        /// <param name="to">Second pose to compare.</param>
        /// <param name="maxDistance">The max distance in which the poses can be similar.</param>
        /// <returns>0 indicates no similitude, 1 for equal poses</returns>
        public static float Similarity(in Pose from, in Pose to, HandGrab.PoseMeasureParameters scoringModifier)
        {
            float rotationDifference = RotationalSimilarity(from.rotation, to.rotation);
            float positionDifference = PositionalSimilarity(from.position, to.position, scoringModifier.MaxDistance);
            return positionDifference * (1f - scoringModifier.PositionRotationWeight)
                + rotationDifference * (scoringModifier.PositionRotationWeight);
        }

        /// <summary>
        /// Get how similar two positions are.
        /// It uses a maximum value to normalize the output
        /// </summary>
        /// <param name="from">The first position.</param>
        /// <param name="to">The second position.</param>
        /// <param name="maxDistance">The Maximum distance used to normalise the output</param>
        /// <returns>0 when the input positions are further than maxDistance, 1 for equal positions.</returns>
        public static float PositionalSimilarity(in Vector3 from, in Vector3 to, float maxDistance)
        {
            float distance = Vector3.Distance(from, to);
            if (distance == 0)
            {
                return 1f;
            }
            return 1f - Mathf.Clamp01(distance / maxDistance);
        }

        /// <summary>
        /// Get how similar two rotations are.
        /// Since the Quaternion.Dot is bugged in unity. We compare the
        /// dot products of the forward and up vectors of the rotations.
        /// </summary>
        /// <param name="from">The first rotation.</param>
        /// <param name="to">The second rotation.</param>
        /// <returns>0 for opposite rotations, 1 for equal rotations.</returns>
        public static float RotationalSimilarity(in Quaternion from, in Quaternion to)
        {
            float forwardDifference = Vector3.Dot(from * Vector3.forward, to * Vector3.forward) * 0.5f + 0.5f;
            float upDifference = Vector3.Dot(from * Vector3.up, to * Vector3.up) * 0.5f + 0.5f;
            return forwardDifference * upDifference;
        }

        /// <summary>
        /// Rotate a pose around an axis.
        /// </summary>
        /// <param name="pose">The pose to mirror.</param>
        /// <param name="normal">The direction of the mirror.</param>
        /// <param name="tangent">The tangent of the mirror.</param>
        /// <returns>A mirrored pose.</returns>
        public static Pose MirrorPoseRotation(this in Pose pose, Vector3 normal, Vector3 tangent)
        {
            Pose mirrorPose = pose;
            Vector3 forward = pose.rotation * -Vector3.forward;
            Vector3 projectedForward = Vector3.ProjectOnPlane(forward, normal);
            float angleForward = Vector3.SignedAngle(projectedForward, tangent, normal);
            Vector3 mirrorForward = Quaternion.AngleAxis(2 * angleForward, normal) * forward;

            Vector3 up = pose.rotation * -Vector3.up;
            Vector3 projectedUp = Vector3.ProjectOnPlane(up, normal);
            float angleUp = Vector3.SignedAngle(projectedUp, tangent, normal);
            Vector3 mirrorUp = Quaternion.AngleAxis(2 * angleUp, normal) * up;

            mirrorPose.rotation = Quaternion.LookRotation(mirrorForward, mirrorUp);
            return mirrorPose;
        }
    }
}
