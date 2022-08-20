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

namespace Oculus.Interaction.Throw
{
    /// <summary>
    /// Provides pose information for a controller.
    /// </summary>
    public class ControllerPoseInputDevice : MonoBehaviour, IPoseInputDevice
    {
        [SerializeField, Interface(typeof(IController))]
        private MonoBehaviour _controller;
        public IController Controller { get; private set; }

        public bool IsInputValid =>
            Controller.IsConnected &&
            Controller.IsPoseValid;

        public bool IsHighConfidence => IsInputValid;

        public bool GetRootPose(out Pose pose)
        {
            pose = Pose.identity;
            if (!IsInputValid)
            {
                return false;
            }

            if (!Controller.TryGetPose(out pose))
            {
                return false;
            }

            return true;
        }

        protected virtual void Awake()
        {
            Controller = _controller as IController;
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(_controller);
        }

        public (Vector3, Vector3) GetExternalVelocities()
        {
            return (Vector3.zero, Vector3.zero);
        }

        #region Inject

        public void InjectAllControllerPoseInputDevice(
            IController controller)
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
