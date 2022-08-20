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

namespace Oculus.Interaction
{
    /// <summary>
    /// TouchHandGrabInteractable provides a hand-specific grab interactable that
    /// owns a set of colliders that associated TouchHandGrabInteractors can then use
    /// for determining selection and release.
    /// </summary>
    public class TouchHandGrabInteractable : PointerInteractable<TouchHandGrabInteractor, TouchHandGrabInteractable>
    {
        [SerializeField]
        private Collider _boundsCollider;

        [SerializeField]
        private List<Collider> _colliders;

        private ColliderGroup _colliderGroup;
        public ColliderGroup ColliderGroup => _colliderGroup;

        protected override void Start()
        {
            base.Start();
            Assert.IsNotNull(_boundsCollider);
            Assert.IsTrue(_colliders.Count > 0);
            _colliderGroup = new ColliderGroup(_colliders, _boundsCollider);
        }

        #region Inject

        public void InjectAllTouchHandGrabInteractable(Collider boundsCollider, List<Collider> colliders)
        {
            InjectBoundsCollider(boundsCollider);
            InjectColliders(colliders);
        }

        private void InjectBoundsCollider(Collider boundsCollider)
        {
            _boundsCollider = boundsCollider;
        }

        public void InjectColliders(List<Collider> colliders)
        {
            _colliders = colliders;
        }

        #endregion
    }
}
