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
using UnityEngine.Events;

namespace Oculus.Interaction.Samples
{
    public class RespawnOnDrop : MonoBehaviour
    {
        [SerializeField]
        private float _yThresholdForRespawn;

        [SerializeField]
        private UnityEvent _whenRespawned = new UnityEvent();

        public UnityEvent WhenRespawned => _whenRespawned;

        // cached starting transform
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Vector3 _initialScale;

        private TwoGrabFreeTransformer[] _freeTransformers;
        private Rigidbody _rigidBody;

        protected virtual void OnEnable()
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _initialScale = transform.localScale;
            _freeTransformers = GetComponents<TwoGrabFreeTransformer>();
            _rigidBody = GetComponent<Rigidbody>();
        }

        protected virtual void Update()
        {
            if (transform.position.y < _yThresholdForRespawn)
            {
                transform.position = _initialPosition;
                transform.rotation = _initialRotation;
                transform.localScale = _initialScale;

                if (_rigidBody)
                {
                    _rigidBody.velocity = Vector3.zero;
                    _rigidBody.angularVelocity = Vector3.zero;
                }

                foreach (var freeTransformer in _freeTransformers)
                {
                    freeTransformer.MarkAsBaseScale();
                }

                _whenRespawned.Invoke();
            }
        }
    }
}
