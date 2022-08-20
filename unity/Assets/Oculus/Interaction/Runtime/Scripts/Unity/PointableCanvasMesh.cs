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
using Oculus.Interaction.UnityCanvas;

namespace Oculus.Interaction
{
    public class PointableCanvasMesh : PointableElement
    {
        [SerializeField]
        private CanvasRenderTextureMesh _canvasRenderTextureMesh;

        protected override void Start()
        {
            base.Start();
            Assert.IsNotNull(_canvasRenderTextureMesh);
        }

        public override void ProcessPointerEvent(PointerEvent evt)
        {
            Vector3 transformPosition =
                _canvasRenderTextureMesh.ImposterToCanvasTransformPoint(evt.Pose.position);
            Pose transformedPose = new Pose(transformPosition, evt.Pose.rotation);
            base.ProcessPointerEvent(new PointerEvent(evt.Identifier, evt.Type, transformedPose));
        }

        #region Inject

        public void InjectAllCanvasMeshPointable(CanvasRenderTextureMesh canvasRenderTextureMesh)
        {
            InjectCanvasRenderTextureMesh(canvasRenderTextureMesh);
        }

        public void InjectCanvasRenderTextureMesh(CanvasRenderTextureMesh canvasRenderTextureMesh)
        {
            _canvasRenderTextureMesh = canvasRenderTextureMesh;
        }

        #endregion
    }
}
