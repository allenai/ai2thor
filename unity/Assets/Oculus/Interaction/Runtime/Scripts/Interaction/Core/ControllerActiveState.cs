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
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    public class ControllerActiveState : MonoBehaviour, IActiveState
    {
        [SerializeField, Interface(typeof(IController))]
        MonoBehaviour _controller;

        private IController Controller;

        public bool Active => Controller.IsConnected;

        protected virtual void Awake()
        {
            Controller = _controller as IController;
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(Controller);
        }

        #region Inject

        public void InjectAllControllerActiveState(IController controller)
        {
            InjectController(controller);
        }

        public void InjectController(IController controller)
        {
            _controller = controller as MonoBehaviour;
            Controller = controller;
        }

        #endregion
    }
}
