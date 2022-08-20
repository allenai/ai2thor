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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.DistanceReticles
{
    public class DistantInteractionLineRendererVisual : DistantInteractionLineVisual
    {
        [SerializeField]
        private LineRenderer _lineRenderer;

        protected override void Start()
        {
            base.Start();
            Assert.IsNotNull(_lineRenderer);
            _lineRenderer.positionCount = NumLinePoints;
        }

        protected override void InteractableSet(MonoBehaviour interactable)
        {
            base.InteractableSet(interactable);
            _lineRenderer.enabled = true;
        }

        protected override void InteractableUnset()
        {
            base.InteractableUnset();
            _lineRenderer.enabled = false;
        }

        protected override void RenderLine(List<Vector3> linePoints)
        {
            _lineRenderer.SetPositions(linePoints.ToArray());
        }

        #region Inject

        public void InjectAllDistantInteractionLineRendererVisual(IDistanceInteractor interactor,
            LineRenderer lineRenderer)
        {
            InjectDistanceInteractor(interactor);
            InjectLineRenderer(lineRenderer);
        }

        public void InjectLineRenderer(LineRenderer lineRenderer)
        {
            _lineRenderer = lineRenderer;
        }

        #endregion
    }
}
