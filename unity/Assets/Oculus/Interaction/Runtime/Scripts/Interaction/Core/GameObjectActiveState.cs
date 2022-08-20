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
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    public class GameObjectActiveState : MonoBehaviour, IActiveState
    {
        [SerializeField]
        private GameObject _sourceGameObject;

        [SerializeField]
        private bool _sourceActiveSelf;

        public bool SourceActiveSelf
        {
            get
            {
                return _sourceActiveSelf;
            }
            set
            {
                _sourceActiveSelf = value;
            }
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(_sourceGameObject);
        }

        public bool Active => _sourceActiveSelf
            ? _sourceGameObject.activeSelf
            : _sourceGameObject.activeInHierarchy;

        #region Inject

        public void InjectAllGameObjectActiveState(GameObject sourceGameObject)
        {
            InjectSourceGameObject(sourceGameObject);
        }

        public void InjectSourceGameObject(GameObject sourceGameObject)
        {
            _sourceGameObject = sourceGameObject;
        }

        #endregion
    }
}
