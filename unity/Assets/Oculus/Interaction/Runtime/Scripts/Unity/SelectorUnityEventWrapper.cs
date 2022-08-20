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
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    public class SelectorUnityEventWrapper : MonoBehaviour
    {
        [SerializeField, Interface(typeof(ISelector))]
        private MonoBehaviour _selector;
        private ISelector Selector;

        [SerializeField]
        private UnityEvent _whenSelected;

        [SerializeField]
        private UnityEvent _whenUnselected;

        public UnityEvent WhenSelected => _whenSelected;
        public UnityEvent WhenUnselected => _whenUnselected;

        protected bool _started = false;

        protected virtual void Awake()
        {
            Selector = _selector as ISelector;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(Selector);
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Selector.WhenSelected += HandleSelected;
                Selector.WhenUnselected += HandleUnselected;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Selector.WhenSelected -= HandleSelected;
                Selector.WhenUnselected -= HandleUnselected;
            }
        }

        private void HandleSelected()
        {
            _whenSelected.Invoke();
        }

        private void HandleUnselected()
        {
            _whenUnselected.Invoke();
        }

        #region Inject

        public void InjectAllSelectorUnityEventWrapper(ISelector selector)
        {
            InjectSelector(selector);
        }

        public void InjectSelector(ISelector selector)
        {
            _selector = selector as MonoBehaviour;
            Selector = selector;
        }

        #endregion
    }
}
