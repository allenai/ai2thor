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
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Oculus.Interaction
{
    /// <summary>
    /// The CollisionsInteractableRegistry maintains a collision map for any Rigidbody-Interactables
    /// pair that utilizes Unity Colliders for overlap checks
    /// </summary>
    public class CollisionInteractionRegistry<TInteractor, TInteractable> :
                             InteractableRegistry<TInteractor, TInteractable>
                             where TInteractor : Interactor<TInteractor, TInteractable>, IRigidbodyRef
                             where TInteractable : Interactable<TInteractor, TInteractable>, IRigidbodyRef
    {
        private Dictionary<Rigidbody, HashSet<TInteractable>> _rigidbodyCollisionMap;
        private Dictionary<TInteractable, InteractableTriggerBroadcaster> _broadcasters;

        public CollisionInteractionRegistry() : base()
        {
            _rigidbodyCollisionMap = new Dictionary<Rigidbody, HashSet<TInteractable>>();
            _broadcasters = new Dictionary<TInteractable, InteractableTriggerBroadcaster>();
        }

        public override void Register(TInteractable interactable)
        {
            base.Register(interactable);

            GameObject triggerGameObject = interactable.Rigidbody.gameObject;
            InteractableTriggerBroadcaster broadcaster;
            if (!_broadcasters.TryGetValue(interactable, out broadcaster))
            {
                broadcaster = triggerGameObject.AddComponent<InteractableTriggerBroadcaster>();
                broadcaster.InjectAllInteractableTriggerBroadcaster(interactable);
                _broadcasters.Add(interactable, broadcaster);
                broadcaster.OnTriggerEntered += MarkCollision;
                broadcaster.OnTriggerExited += UnmarkCollision;
            }
        }

        public override void Unregister(TInteractable interactable)
        {
            base.Unregister(interactable);

            InteractableTriggerBroadcaster broadcaster;
            if (_broadcasters.TryGetValue(interactable, out broadcaster))
            {
                broadcaster.enabled = false;
                broadcaster.OnTriggerEntered -= MarkCollision;
                broadcaster.OnTriggerExited -= UnmarkCollision;
                _broadcasters.Remove(interactable);
                Object.Destroy(broadcaster);
            }
        }

        private void MarkCollision(IInteractable interactable, Rigidbody rigidbody)
        {
            TInteractable typedInteractable = interactable as TInteractable;
            if (!_rigidbodyCollisionMap.ContainsKey(rigidbody))
            {
                _rigidbodyCollisionMap.Add(rigidbody, new HashSet<TInteractable>());
            }

            HashSet<TInteractable> interactables = _rigidbodyCollisionMap[rigidbody];
            interactables.Add(typedInteractable);
        }

        private void UnmarkCollision(IInteractable interactable, Rigidbody rigidbody)
        {
            TInteractable typedInteractable = interactable as TInteractable;
            HashSet<TInteractable> interactables = _rigidbodyCollisionMap[rigidbody];
            interactables.Remove(typedInteractable);

            if (interactables.Count == 0)
            {
                _rigidbodyCollisionMap.Remove(rigidbody);
            }
        }

        public override IEnumerable<TInteractable> List(TInteractor interactor)
        {
            HashSet<TInteractable> colliding;
            if (_rigidbodyCollisionMap.TryGetValue(interactor.Rigidbody, out colliding))
            {
                return PruneInteractables(colliding, interactor);
            }
            return _empty;
        }

        private  static readonly List<TInteractable> _empty = new List<TInteractable>();
    }
}
