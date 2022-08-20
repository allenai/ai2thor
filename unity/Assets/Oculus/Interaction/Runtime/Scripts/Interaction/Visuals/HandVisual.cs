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

namespace Oculus.Interaction
{
    public class HandVisual : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;
        public IHand Hand;

        [SerializeField]
        private SkinnedMeshRenderer _skinnedMeshRenderer;

        [SerializeField]
        private bool _updateRootPose = true;

        [SerializeField]
        private bool _updateRootScale = true;

        [SerializeField, Optional]
        private Transform _root = null;

        [SerializeField, Optional]
        private MaterialPropertyBlockEditor _handMaterialPropertyBlockEditor;

        [HideInInspector]
        [SerializeField]
        private List<Transform> _jointTransforms = new List<Transform>();
        public event Action WhenHandVisualUpdated = delegate { };

        public bool IsVisible => _skinnedMeshRenderer != null && _skinnedMeshRenderer.enabled;

        private int _wristScalePropertyId;

        public List<Transform> Joints => _jointTransforms;

        public bool ForceOffVisibility { get; set; }

        private bool _started = false;

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
            if (_root == null && _jointTransforms.Count > 0 && _jointTransforms[0] != null)
            {
                _root = _jointTransforms[0].parent;
            }
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(Hand);
            Assert.IsNotNull(_skinnedMeshRenderer);
            if (_handMaterialPropertyBlockEditor != null)
            {
                _wristScalePropertyId = Shader.PropertyToID("_WristScale");
            }

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated += UpdateSkeleton;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started && _hand != null)
            {
                Hand.WhenHandUpdated -= UpdateSkeleton;
            }
        }

        public void UpdateSkeleton()
        {
            if (!Hand.IsTrackedDataValid)
            {
                if (IsVisible || ForceOffVisibility)
                {
                    _skinnedMeshRenderer.enabled = false;
                }
                WhenHandVisualUpdated.Invoke();
                return;
            }

            if (!IsVisible && !ForceOffVisibility)
            {
                _skinnedMeshRenderer.enabled = true;
            }
            else if(IsVisible && ForceOffVisibility)
            {
                _skinnedMeshRenderer.enabled = false;
            }

            if (_updateRootPose)
            {
                if (_root != null && Hand.GetRootPose(out Pose handRootPose))
                {
                    _root.localPosition = handRootPose.position;
                    _root.localRotation = handRootPose.rotation;
                }
            }

            if (_updateRootScale)
            {
                if (_root != null)
                {
                    _root.localScale = new Vector3(Hand.Scale, Hand.Scale, Hand.Scale);
                }
            }

            if (!Hand.GetJointPosesLocal(out ReadOnlyHandJointPoses localJoints))
            {
                return;
            }
            for (var i = 0; i < Constants.NUM_HAND_JOINTS; ++i)
            {
                if (_jointTransforms[i] == null)
                {
                    continue;
                }
                _jointTransforms[i].SetPose(localJoints[i], Space.Self);
            }

            if (_handMaterialPropertyBlockEditor != null)
            {
                _handMaterialPropertyBlockEditor.MaterialPropertyBlock.SetFloat(_wristScalePropertyId, Hand.Scale);
                _handMaterialPropertyBlockEditor.UpdateMaterialPropertyBlock();
            }
            WhenHandVisualUpdated.Invoke();
        }

        public Transform GetTransformByHandJointId(HandJointId handJointId)
        {
            return _jointTransforms[(int)handJointId];
        }

        #region Inject

        public void InjectAllHandSkeletonVisual(IHand hand, SkinnedMeshRenderer skinnedMeshRenderer)
        {
            InjectHand(hand);
            InjectSkinnedMeshRenderer(skinnedMeshRenderer);
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as MonoBehaviour;
            Hand = hand;
        }

        public void InjectSkinnedMeshRenderer(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            _skinnedMeshRenderer = skinnedMeshRenderer;
        }

        public void InjectOptionalUpdateRootPose(bool updateRootPose)
        {
            _updateRootPose = updateRootPose;
        }

        public void InjectOptionalUpdateRootScale(bool updateRootScale)
        {
            _updateRootScale = updateRootScale;
        }

        public void InjectOptionalRoot(Transform root)
        {
            _root = root;
        }

        public void InjectOptionalMaterialPropertyBlockEditor(MaterialPropertyBlockEditor editor)
        {
            _handMaterialPropertyBlockEditor = editor;
        }
        #endregion
    }
}
