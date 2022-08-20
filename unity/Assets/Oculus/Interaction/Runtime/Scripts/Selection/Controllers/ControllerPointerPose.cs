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
    public class ControllerPointerPose : MonoBehaviour, IActiveState
    {
        [SerializeField, Interface(typeof(IController))]
        private MonoBehaviour _controller;
        public IController Controller { get; private set; }

        [SerializeField]
        private Vector3 _offset;

        protected bool _started = false;

        public bool Active { get; private set; }

        protected virtual void Awake()
        {
            Controller = _controller as IController;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(Controller);
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Controller.ControllerUpdated += HandleControllerUpdated;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Controller.ControllerUpdated -= HandleControllerUpdated;
            }
        }

        private void HandleControllerUpdated()
        {
            IController controller = Controller;
            if (controller.TryGetPointerPose(out Pose pose))
            {
                pose.position += pose.rotation * _offset;
                transform.SetPose(pose);
                Active = true;
            }
            else
            {
                Active = false;
            }
        }

        #region Inject

        public void InjectController(IController controller)
        {
            _controller = controller as MonoBehaviour;
            Controller = controller;
        }

        public void InjectOffset(Vector3 offset)
        {
            _offset = offset;
        }

        public void InjectAllHandPointerPose(IController controller,
            Vector3 offset)
        {
            InjectController(controller);
            InjectOffset(offset);
        }

        #endregion
    }
}
