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

using Oculus.Interaction.Input;
using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    public class ControllerSelector : MonoBehaviour, ISelector
    {
        public enum ControllerSelectorLogicOperator
        {
            Any = 0,
            All = 1
        }

        [SerializeField, Interface(typeof(IController))]
        private MonoBehaviour _controller;

        [SerializeField]
        private ControllerButtonUsage _controllerButtonUsage;

        [SerializeField]
        private ControllerSelectorLogicOperator _requireButtonUsages =
            ControllerSelectorLogicOperator.Any;

        #region Properties
        public ControllerButtonUsage ControllerButtonUsage
        {
            get
            {
                return _controllerButtonUsage;
            }
            set
            {
                _controllerButtonUsage = value;
            }
        }

        public ControllerSelectorLogicOperator RequireButtonUsages
        {
            get
            {
                return _requireButtonUsages;
            }
            set
            {
                _requireButtonUsages = value;
            }
        }
        #endregion

        public IController Controller { get; private set; }

        public event Action WhenSelected = delegate { };
        public event Action WhenUnselected = delegate { };

        private bool _selected;

        protected virtual void Awake()
        {
            Controller = _controller as IController;
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(Controller);
        }

        protected virtual void Update()
        {
            bool selected = _requireButtonUsages == ControllerSelectorLogicOperator.All
                ? Controller.IsButtonUsageAllActive(_controllerButtonUsage)
                : Controller.IsButtonUsageAnyActive(_controllerButtonUsage);

            if (selected)
            {
                if (_selected) return;
                _selected = true;
                WhenSelected();
            }
            else
            {
                if (!_selected) return;
                _selected = false;
                WhenUnselected();
            }
        }

        #region Inject

        public void InjectAllControllerSelector(IController controller)
        {
            InjectController(controller);
        }

        public void InjectController(IController controller)
        {
            _controller  = controller as MonoBehaviour;;
            Controller = controller;
        }

        #endregion
    }
}
