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

namespace Oculus.Interaction
{
    /// <summary>
    /// The SnapInteractable, specifies a volume in space in which
    /// SnapInteractors can snap to. How the slots are organised is configured via a custom
    /// IDropZoneSlotsProvider. If none is provided the SnapInteractable has one single slot at its own transform.
    /// </summary>
    public class SnapInteractable : Interactable<SnapInteractor, SnapInteractable>,
        IRigidbodyRef
    {
        /// <summary>
        /// The movement provider specifies how the objects will align to this SnapInteractable.
        /// When none is provided a continuous translation over time will be applied.
        /// </summary>
        [SerializeField, Optional, Interface(typeof(IMovementProvider))]
        private MonoBehaviour _movementProvider;
        private IMovementProvider MovementProvider { get; set; }

        /// <summary>
        /// By default SnapInteractables contain just one slot at their own pose.
        /// But with the SlotsProvider one can assign multiple slots and indicate
        /// which one the items should snap to or even move items from one slot to another.
        /// Useful for implementing inventory systems or boards with multiple slots.
        /// </summary>
        [SerializeField, Optional, Interface(typeof(ISnapPoseProvider))]
        [UnityEngine.Serialization.FormerlySerializedAs("_slotsProvider")]
        private MonoBehaviour _snapPosesProvider;
        private ISnapPoseProvider SnapPosesProvider { get; set; }

        [SerializeField]
        private Rigidbody _rigidbody;
        public Rigidbody Rigidbody => _rigidbody;

        private Collider[] _colliders;
        public Collider[] Colliders => _colliders;

        private bool _started;

        private static CollisionInteractionRegistry<SnapInteractor, SnapInteractable> _registry = null;

        #region Editor events
        private void Reset()
        {
            _rigidbody = this.GetComponentInParent<Rigidbody>();
        }
        #endregion

        protected override void Awake()
        {
            base.Awake();
            MovementProvider = _movementProvider as IMovementProvider;
            SnapPosesProvider = _snapPosesProvider as ISnapPoseProvider;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            Assert.IsNotNull(Rigidbody, "The rigidbody is missing");
            _colliders = Rigidbody.GetComponentsInChildren<Collider>();
            if (_registry == null)
            {
                _registry = new CollisionInteractionRegistry<SnapInteractor, SnapInteractable>();
                SetRegistry(_registry);
            }
            if (MovementProvider == null)
            {
                FollowTargetProvider movementProvider = this.gameObject.AddComponent<FollowTargetProvider>();
                InjectOptionalMovementProvider(movementProvider);
            }
            this.EndStart(ref _started);
        }

        protected override void InteractorAdded(SnapInteractor interactor)
        {
            base.InteractorAdded(interactor);
            if (SnapPosesProvider != null)
            {
                SnapPosesProvider.TrackInteractor(interactor);
            }
        }

        protected override void InteractorRemoved(SnapInteractor interactor)
        {
            base.InteractorRemoved(interactor);
            if (SnapPosesProvider != null)
            {
                SnapPosesProvider.UntrackInteractor(interactor);
            }
        }

        protected override void SelectingInteractorAdded(SnapInteractor interactor)
        {
            base.SelectingInteractorAdded(interactor);
            if (SnapPosesProvider != null)
            {
                SnapPosesProvider.SnapInteractor(interactor);
            }
        }

        protected override void SelectingInteractorRemoved(SnapInteractor interactor)
        {
            base.SelectingInteractorRemoved(interactor);
            if (SnapPosesProvider != null)
            {
                SnapPosesProvider.UnsnapInteractor(interactor);
            }
        }

        public void InteractorHoverUpdated(SnapInteractor interactor)
        {
            if (SnapPosesProvider != null)
            {
                SnapPosesProvider.UpdateTrackedInteractor(interactor);
            }
        }

        public bool PoseForInteractor(SnapInteractor interactor, out Pose slot)
        {
            if (SnapPosesProvider != null)
            {
                return SnapPosesProvider.PoseForInteractor(interactor, out slot);
            }

            slot = this.transform.GetPose();
            return true;
        }

        public IMovement GenerateMovement(in Pose from, SnapInteractor interactor)
        {
            if (PoseForInteractor(interactor, out Pose to))
            {
                IMovement movement = MovementProvider.CreateMovement();
                movement.StopAndSetPose(from);
                movement.MoveTo(to);
                return movement;
            }
            return null;
        }

        #region Inject
        public void InjectAllSnapInteractable(Rigidbody rigidbody)
        {
            InjectRigidbody(rigidbody);
        }

        public void InjectRigidbody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        public void InjectOptionalMovementProvider(IMovementProvider provider)
        {
            _movementProvider = provider as MonoBehaviour;
            MovementProvider = provider;
        }

        public void InjectOptionalSnapPosesProvider(ISnapPoseProvider snapPosesProvider)
        {
            _snapPosesProvider = snapPosesProvider as MonoBehaviour;
            SnapPosesProvider = snapPosesProvider;
        }

        #endregion
    }
}
