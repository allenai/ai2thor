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

using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.DistanceReticles
{
    public class ReticleMeshDrawer : InteractorReticle<ReticleDataMesh>
    {
        [SerializeField]
        private DistanceHandGrabInteractor _distanceInteractor;
        protected override IInteractorView Interactor => _distanceInteractor;

        [SerializeField]
        private MeshFilter _filter;

        [SerializeField]
        private MeshRenderer _renderer;

        [SerializeField]
        private PoseTravelData _travelData = PoseTravelData.FAST;
        public PoseTravelData TravelData
        {
            get
            {
                return _travelData;
            }
            set
            {
                _travelData = value;
            }
        }

        private Tween _tween;

        protected virtual void Reset()
        {
            _filter = this.GetComponent<MeshFilter>();
            _renderer = this.GetComponent<MeshRenderer>();
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            Assert.IsNotNull(_distanceInteractor);
            Assert.IsNotNull(_filter);
            Assert.IsNotNull(_renderer);
            this.EndStart(ref _started);
        }

        protected override void Draw(ReticleDataMesh dataMesh)
        {
            _filter.sharedMesh = dataMesh.Filter.sharedMesh;
            _filter.transform.localScale = dataMesh.Filter.transform.lossyScale;
            _renderer.enabled = true;

            Pose destination = DestinationPose(dataMesh, _distanceInteractor.transform.GetPose());
            _tween = _travelData.CreateTween(dataMesh.Target.GetPose(), destination);
        }

        protected override void Hide()
        {
            _tween = null;
            _renderer.enabled = false;
        }

        protected override void Align(ReticleDataMesh data)
        {
            HandGrabTarget grabTarget = _distanceInteractor.HandGrabTarget;

            if (grabTarget != null && _distanceInteractor.HasInteractable)
            {
                Pose pose = DestinationPose(data, grabTarget.WorldGrabPose);
                _tween.UpdateTarget(pose);
            }

            _tween.Tick();
            _filter.transform.SetPose(_tween.Pose);
        }

        private Pose DestinationPose(IReticleData data, Pose worldSnapPose)
        {
            Pose targetOffset = PoseUtils.Delta(worldSnapPose, data.Target.GetPose());
            _distanceInteractor.Hand.GetRootPose(out Pose pose);
            pose.Premultiply(_distanceInteractor.WristToGrabPoseOffset);
            pose.Premultiply(targetOffset);

            return pose;
        }

        #region Inject
        public void InjectAllReticleMeshDrawer(DistanceHandGrabInteractor interactor,
            MeshFilter filter, MeshRenderer renderer)
        {
            InjectDistanceInteractor(interactor);
            InjectFilter(filter);
            InjectRenderer(renderer);
        }

        public void InjectDistanceInteractor(DistanceHandGrabInteractor interactor)
        {
            _distanceInteractor = interactor;
        }

        public void InjectFilter(MeshFilter filter)
        {
            _filter = filter;
        }

        public void InjectRenderer(MeshRenderer renderer)
        {
            _renderer = renderer;
        }
        #endregion
    }
}
