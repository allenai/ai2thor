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
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    /// <summary>
    /// Modifies the `active` state of a list of GameObjects, as well as the `enabled` state of a
    /// list of components, from the `Active` field of the given IActiveState.
    /// The component will only activate/enable dependants that were active/enabled during Start()
    /// lifecycle phase.
    /// </summary>
    /// These need to be updated in batch or else we could get inconsistent behaviour when
    /// multiple of these are in a scene.
    [DefaultExecutionOrder(1)]
    public class ActiveStateTracker : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IActiveState))]
        private MonoBehaviour _activeState;

        private IActiveState ActiveState;

        [Header("Active state dependents")]
        [SerializeField]
        private bool _includeChildrenAsDependents = false;

        [SerializeField, Optional]
        [Tooltip("Sets the `active` field on whole GameObjects")]
        private List<GameObject> _gameObjects;

        [SerializeField, Optional]
        [Tooltip("Sets the `enabled` field on individual components")]
        private List<MonoBehaviour> _monoBehaviours;

        protected virtual void Awake()
        {
            ActiveState = _activeState as IActiveState;
        }

        private bool _active = false;

        protected virtual void Start()
        {
            Assert.IsNotNull(ActiveState);

            if (_includeChildrenAsDependents)
            {
                for(int i = 0; i < transform.childCount; i ++)
                {
                    _gameObjects.Add(transform.GetChild(i).gameObject);
                }
            }

            SetDependentsActive(false);
        }

        protected virtual void Update()
        {
            if (_active == ActiveState.Active) return;

            _active = ActiveState.Active;
            SetDependentsActive(ActiveState.Active);
        }

        private void SetDependentsActive(bool active)
        {
            for (int i = 0; i < _gameObjects.Count; ++i)
            {
                _gameObjects[i].SetActive(active);
            }

            for (int i = 0; i < _monoBehaviours.Count; ++i)
            {
                _monoBehaviours[i].enabled = active;
            }
        }

        #region Inject

        public void InjectAllActiveStateTracker(IActiveState activeState)
        {
            InjectActiveState(activeState);
        }

        public void InjectActiveState(IActiveState activeState)
        {
            _activeState = activeState as MonoBehaviour;
            ActiveState = activeState;
        }

        public void InjectOptionalIncludeChildrenAsDependents(bool includeChildrenAsDependents)
        {
            _includeChildrenAsDependents = includeChildrenAsDependents;
        }

        public void InjectOptionalGameObjects(List<GameObject> gameObjects)
        {
            _gameObjects = gameObjects;
        }

        public void InjectOptionalMonoBehaviours(List<MonoBehaviour> monoBehaviours)
        {
            _monoBehaviours = monoBehaviours;
        }
        #endregion
    }
}
