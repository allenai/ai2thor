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

namespace Oculus.Interaction
{
    /// <summary>
    /// Acts as a forwarder of Trigger events for Rigidbody overlaps. Used in conjunction with
    /// CollisionInteractionRegistry.
    ///
    /// Note: If Physics.autoSimulation is false, ForceGlobalUpdateTrigger should be called
    /// after every call to Physics.Simulate
    /// </summary>
    public class InteractableTriggerBroadcaster : MonoBehaviour
    {
        public Action<IInteractable, Rigidbody> OnTriggerEntered = delegate { };
        public Action<IInteractable, Rigidbody> OnTriggerExited = delegate { };

        private IInteractable _interactable;
        private Dictionary<Rigidbody, bool> _rigidbodyTriggers;
        private List<Rigidbody> _rigidbodies;

        private static HashSet<InteractableTriggerBroadcaster> _broadcasters =
            new HashSet<InteractableTriggerBroadcaster>();

        protected bool _started = false;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            _rigidbodyTriggers = new Dictionary<Rigidbody, bool>();
            _rigidbodies = new List<Rigidbody>();
            this.EndStart(ref _started);
        }

        protected virtual void OnTriggerStay(Collider collider)
        {
            if (!_started)
            {
                return;
            }

            Rigidbody rigidbody = collider.attachedRigidbody;
            if (rigidbody == null)
            {
                return;
            }

            if (!_rigidbodyTriggers.ContainsKey(rigidbody))
            {
                OnTriggerEntered(_interactable, rigidbody);
                _rigidbodyTriggers.Add(rigidbody, true);
            }
            else
            {
                _rigidbodyTriggers[rigidbody] = true;
            }
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _broadcasters.Add(this);
            }
        }

        protected virtual void FixedUpdate()
        {
            if (!Physics.autoSimulation)
            {
                return;
            }
            UpdateTriggers();
        }

        private void UpdateTriggers()
        {
            _rigidbodies.Clear();
            _rigidbodies.AddRange(_rigidbodyTriggers.Keys);
            foreach (Rigidbody rigidbody in _rigidbodies)
            {
                if (_rigidbodyTriggers[rigidbody] == false)
                {
                    _rigidbodyTriggers.Remove(rigidbody);
                    OnTriggerExited(_interactable, rigidbody);
                }
                else
                {
                    _rigidbodyTriggers[rigidbody] = false;
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                // Clean up any remaining active triggers
                foreach (Rigidbody rigidbody in _rigidbodyTriggers.Keys)
                {
                    OnTriggerExited(_interactable, rigidbody);
                }
                _broadcasters.Remove(this);
                _rigidbodies.Clear();
            }
        }

        public static void ForceGlobalUpdateTriggers()
        {
            foreach (InteractableTriggerBroadcaster broadcaster in _broadcasters)
            {
                broadcaster.UpdateTriggers();
            }
        }

        #region Inject
        public void InjectAllInteractableTriggerBroadcaster(IInteractable interactable)
        {
            InjectInteractable(interactable);
        }

        public void InjectInteractable(IInteractable interactable)
        {
            _interactable = interactable;
        }
        #endregion
    }
}
