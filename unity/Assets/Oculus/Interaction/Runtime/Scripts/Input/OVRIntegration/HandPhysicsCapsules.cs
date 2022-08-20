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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.Input
{
    public class HandPhysicsCapsules : MonoBehaviour
    {
        [SerializeField] private HandVisual _handVisual;

        private GameObject _capsulesGO;
        private List<BoneCapsule> _capsules;
        public IList<BoneCapsule> Capsules { get; private set; }
        private OVRPlugin.Skeleton2 _skeleton;
        private bool _capsulesAreActive;
        protected bool _started;

        protected virtual void Awake()
        {
            Assert.IsNotNull(_handVisual);
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            _skeleton = _handVisual.Hand.Handedness == Handedness.Left
                ? OVRSkeletonData.LeftSkeleton
                : OVRSkeletonData.RightSkeleton;
            _capsulesGO = new GameObject("Capsules");
            _capsulesGO.transform.SetParent(transform, false);
            _capsulesGO.transform.localPosition = Vector3.zero;
            _capsulesGO.transform.localRotation = Quaternion.identity;

            _capsules = new List<BoneCapsule>(new BoneCapsule[_skeleton.NumBoneCapsules]);
            Capsules = _capsules.AsReadOnly();

            for (int i = 0; i < _capsules.Count; ++i)
            {
                Transform boneTransform = _handVisual.Joints[_skeleton.BoneCapsules[i].BoneIndex];
                BoneCapsule capsule = new BoneCapsule();
                _capsules[i] = capsule;

                capsule.BoneIndex = _skeleton.BoneCapsules[i].BoneIndex;

                capsule.CapsuleRigidbody = new GameObject((boneTransform.name).ToString() + "_CapsuleRigidbody")
                    .AddComponent<Rigidbody>();
                capsule.CapsuleRigidbody.mass = 1.0f;
                capsule.CapsuleRigidbody.isKinematic = true;
                capsule.CapsuleRigidbody.useGravity = false;
                capsule.CapsuleRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

                GameObject rbGO = capsule.CapsuleRigidbody.gameObject;
                rbGO.transform.SetParent(_capsulesGO.transform, false);
                rbGO.transform.position = boneTransform.position;
                rbGO.transform.rotation = boneTransform.rotation;
                rbGO.SetActive(false);

                capsule.CapsuleCollider = new GameObject((boneTransform.name).ToString() + "_CapsuleCollider")
                    .AddComponent<CapsuleCollider>();
                capsule.CapsuleCollider.isTrigger = false;

                var p0 = _skeleton.BoneCapsules[i].StartPoint.FromFlippedXVector3f();
                var p1 = _skeleton.BoneCapsules[i].EndPoint.FromFlippedXVector3f();
                var delta = p1 - p0;
                var mag = delta.magnitude;
                var rot = Quaternion.FromToRotation(Vector3.right, delta);
                capsule.CapsuleCollider.radius = _skeleton.BoneCapsules[i].Radius;
                capsule.CapsuleCollider.height = mag + _skeleton.BoneCapsules[i].Radius * 2.0f;
                capsule.CapsuleCollider.direction = 0;
                capsule.CapsuleCollider.center = Vector3.right * mag * 0.5f;

                GameObject ccGO = capsule.CapsuleCollider.gameObject;
                ccGO.transform.SetParent(rbGO.transform, false);
                ccGO.transform.localPosition = p0;
                ccGO.transform.localRotation = rot;
            }
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _handVisual.WhenHandVisualUpdated += HandleHandVisualUpdated;
            }
        }
        protected virtual void OnDisable()
        {
            if (_started)
            {
                _handVisual.WhenHandVisualUpdated -= HandleHandVisualUpdated;

                if (_capsules != null)
                {
                    for (int i = 0; i < _capsules.Count; ++i)
                    {
                        var capsuleGO = _capsules[i].CapsuleRigidbody.gameObject;
                        capsuleGO.SetActive(false);
                    }
                    _capsulesAreActive = false;
                }
            }
        }

        protected virtual void FixedUpdate()
        {
            if (_capsulesAreActive && !_handVisual.IsVisible)
            {
                for (int i = 0; i < _capsules.Count; ++i)
                {
                    var capsuleGO = _capsules[i].CapsuleRigidbody.gameObject;
                    capsuleGO.SetActive(false);
                }
                _capsulesAreActive = false;
            }
        }

        private void HandleHandVisualUpdated()
        {
            _capsulesAreActive = _handVisual.IsVisible;

            for (int i = 0; i < _capsules.Count; ++i)
            {
                BoneCapsule capsule = _capsules[i];
                var capsuleGO = capsule.CapsuleRigidbody.gameObject;

                if (_capsulesAreActive)
                {
                    Transform boneTransform = _handVisual.Joints[(int)capsule.BoneIndex];

                    if (capsuleGO.activeSelf)
                    {
                        capsule.CapsuleRigidbody.MovePosition(boneTransform.position);
                        capsule.CapsuleRigidbody.MoveRotation(boneTransform.rotation);
                    }
                    else
                    {
                        capsuleGO.SetActive(true);
                        capsule.CapsuleRigidbody.position = boneTransform.position;
                        capsule.CapsuleRigidbody.rotation = boneTransform.rotation;
                    }
                }
                else
                {
                    if (capsuleGO.activeSelf)
                    {
                        capsuleGO.SetActive(false);
                    }
                }
            }
        }

        #region Inject

        public void InjectAllOVRHandPhysicsCapsules(HandVisual hand)
        {
            InjectHandSkeleton(hand);
}

        public void InjectHandSkeleton(HandVisual hand)
        {
            _handVisual = hand;
        }

        #endregion
    }

    public class BoneCapsule
    {
        public short BoneIndex { get; set; }
        public Rigidbody CapsuleRigidbody { get; set; }
        public CapsuleCollider CapsuleCollider { get; set; }
    }
}
