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
using Oculus.Interaction.Surfaces;
using System;

namespace Oculus.Interaction
{
    public class PokeInteractable : PointerInteractable<PokeInteractor, PokeInteractable>
    {
        [SerializeField, Interface(typeof(IProximityField))]
        private MonoBehaviour _proximityField;
        public IProximityField ProximityField;

        [SerializeField, Interface(typeof(IPointableSurface))]
        private MonoBehaviour _surface;
        public IPointableSurface Surface;

        [SerializeField]
        private float _maxDistance = 0.1f;
        public float MaxDistance => _maxDistance;

        [SerializeField]
        private float _enterHoverDistance = 0f;

        [SerializeField]
        private float _releaseDistance = 0.25f;
        public float ReleaseDistance => _releaseDistance;

        public float EnterHoverDistance => _enterHoverDistance;

        [SerializeField]
        private float _horizontalDragThreshold = 0.0f;
        public float HorizontalDragThreshold => _horizontalDragThreshold;

        [SerializeField]
        private float _verticalDragThreshold = 0.0f;
        public float VerticalDragThreshold => _verticalDragThreshold;

        [SerializeField, Optional]
        private Collider _volumeMask = null;
        public Collider VolumeMask { get => _volumeMask; }

        [Serializable]
        public class PositionPinningConfig
        {
            public bool Enabled;
            public float MaxPinDistance;
        }

        [SerializeField]
        private PositionPinningConfig _positionPinning;

        public PositionPinningConfig PositionPinning
        {
            get
            {
                return _positionPinning;
            }

            set
            {
                _positionPinning = value;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            ProximityField = _proximityField as IProximityField;
            Surface = _surface as IPointableSurface;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            Assert.IsNotNull(ProximityField);
            Assert.IsNotNull(Surface);
            if (_enterHoverDistance > 0f)
            {
                _enterHoverDistance = Mathf.Min(_enterHoverDistance, _maxDistance);
            }
            this.EndStart(ref _started);
        }

        public Vector3 ComputeClosestPoint(Vector3 point)
        {
            return ProximityField.ComputeClosestPoint(point);
        }

        public Vector3 ClosestSurfacePoint(Vector3 point)
        {
            Surface.ClosestSurfacePoint(point, out SurfaceHit hit);
            return hit.Point;
        }

        public Vector3 ClosestSurfaceNormal(Vector3 point)
        {
            Surface.ClosestSurfacePoint(point, out SurfaceHit hit);
            return hit.Normal;
        }

        #region Inject

        public void InjectAllPokeInteractable(IPointableSurface surface,
                                              IProximityField proximityField)
        {
            InjectSurface(surface);
            InjectProximityField(proximityField);
        }

        public void InjectSurface(IPointableSurface surface)
        {
            _surface = surface as MonoBehaviour;
            Surface = surface;
        }

        public void InjectProximityField(IProximityField proximityField)
        {
            _proximityField = proximityField as MonoBehaviour;
            ProximityField = proximityField;
        }

        public void InjectOptionalMaxDistance(float maxDistance)
        {
            _maxDistance = maxDistance;
        }

        public void InjectOptionalReleaseDistance(float releaseDistance)
        {
            _releaseDistance = releaseDistance;
        }

        public void InjectOptionalHorizontalDragThreshold(float horizontalDragThreshold)
        {
            _horizontalDragThreshold = horizontalDragThreshold;
        }

        public void InjectOptionalVerticalDragThreshold(float verticalDragThreshold)
        {
            _verticalDragThreshold = verticalDragThreshold;
        }

        public void InjectOptionalEnterHoverDistance(float enterHoverDistance)
        {
            _enterHoverDistance = enterHoverDistance;
        }

        public void InjectOptionalVolumeMask(Collider volumeMask)
        {
            _volumeMask = volumeMask;
        }

        #endregion
    }
}
