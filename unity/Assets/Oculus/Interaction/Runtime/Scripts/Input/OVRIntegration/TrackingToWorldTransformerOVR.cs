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
using UnityEngine.Assertions;

namespace Oculus.Interaction.Input
{
    public class TrackingToWorldTransformerOVR : MonoBehaviour, ITrackingToWorldTransformer
    {
        [SerializeField, Interface(typeof(IOVRCameraRigRef))]
        private MonoBehaviour _cameraRigRef;
        public IOVRCameraRigRef CameraRigRef { get; private set; }

        public Transform Transform => CameraRigRef.CameraRig.transform;

        /// <summary>
        /// Converts a tracking space pose to a world space pose (Applies any transform applied to the OVRCameraRig)
        /// </summary>
        public Pose ToWorldPose(Pose pose)
        {
            Transform trackingToWorldSpace = Transform;

            pose.position = trackingToWorldSpace.TransformPoint(pose.position);
            pose.rotation = trackingToWorldSpace.rotation * pose.rotation;
            return pose;
        }

        /// <summary>
        /// Converts a world space pose to a tracking space pose (Removes any transform applied to the OVRCameraRig)
        /// </summary>
        public Pose ToTrackingPose(in Pose worldPose)
        {
            Transform trackingToWorldSpace = Transform;

            Vector3 position = trackingToWorldSpace.InverseTransformPoint(worldPose.position);
            Quaternion rotation = Quaternion.Inverse(trackingToWorldSpace.rotation) * worldPose.rotation;

            return new Pose(position, rotation);
        }

        public Quaternion WorldToTrackingWristJointFixup => FromOVRHandDataSource.WristFixupRotation;

        protected virtual void Awake()
        {
            CameraRigRef = _cameraRigRef as IOVRCameraRigRef;
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(CameraRigRef);
        }

        #region Inject

        public void InjectAllTrackingToWorldTransformerOVR(IOVRCameraRigRef cameraRigRef)
        {
            InjectCameraRigRef(cameraRigRef);
        }

        public void InjectCameraRigRef(IOVRCameraRigRef cameraRigRef)
        {
            _cameraRigRef = cameraRigRef as MonoBehaviour;
            CameraRigRef = cameraRigRef;
        }

        #endregion
    }
}
