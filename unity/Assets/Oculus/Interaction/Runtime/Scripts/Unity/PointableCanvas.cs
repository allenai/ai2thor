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
using UnityEngine.UI;

namespace Oculus.Interaction
{
    /// <summary>
    /// PointerCanvas allows any IPointable to forward its
    /// events onto an associated Canvas via the IPointableCanvas interface
    /// Requires a PointableCanvasModule present in the scene.
    /// </summary>
    public class PointableCanvas : PointableElement, IPointableCanvas
    {
        [SerializeField]
        private Canvas _canvas;
        public Canvas Canvas => _canvas;

        private bool _registered = false;

        protected override void Start()
        {
            base.Start();
            Assert.IsNotNull(Canvas);
            Assert.IsNotNull(Canvas.GetComponent<GraphicRaycaster>(),
        "PointableCanvas requires that the Canvas object has an attached GraphicRaycaster.");
        }

        private void Register()
        {
            PointableCanvasModule.RegisterPointableCanvas(this);
            _registered = true;
        }

        private void Unregister()
        {
            if (!_registered) return;
            PointableCanvasModule.UnregisterPointableCanvas(this);
            _registered = false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_started)
            {
                Register();
            }
        }

        protected override void OnDisable()
        {
            if (_started)
            {
                Unregister();
            }
            base.OnDisable();
        }

        #region Inject

        public void InjectAllPointableCanvas(Canvas canvas)
        {
            InjectCanvas(canvas);
        }

        public void InjectCanvas(Canvas canvas)
        {
            _canvas = canvas;
        }

        #endregion
    }
}
