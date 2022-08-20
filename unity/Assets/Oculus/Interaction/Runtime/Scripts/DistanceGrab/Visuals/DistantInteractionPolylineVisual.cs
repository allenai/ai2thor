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
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.DistanceReticles
{
    public class DistantInteractionPolylineVisual : DistantInteractionLineVisual
    {
        [SerializeField]
        private Color _color = Color.white;

        public Color Color
        {
            get
            {
                return _color;
            }
            set
            {
                _color = value;
            }
        }

        [SerializeField]
        private float _lineWidth = 0.02f;
        public float LineWidth
        {
            get
            {
                return _lineWidth;
            }
            set
            {
                _lineWidth = value;
            }
        }

        private List<Vector4> _linePointsVec4;

        [SerializeField]
        private Material _lineMaterial;

        private PolylineRenderer _polylineRenderer;

        protected override void Start()
        {
            base.Start();
            Assert.IsNotNull(_lineMaterial);
            _polylineRenderer = new PolylineRenderer(_lineMaterial);
            _linePointsVec4 = new List<Vector4>(new Vector4[NumLinePoints]);
        }

        private void OnDestroy()
        {
            _polylineRenderer.Cleanup();
        }

        protected override void RenderLine(List<Vector3> linePoints)
        {
            _linePointsVec4 = linePoints.Select(p => new Vector4(p.x, p.y, p.z, _lineWidth)).ToList();
            _polylineRenderer.SetLines(_linePointsVec4, _color);
            _polylineRenderer.RenderLines();
        }

        #region Inject

        public void InjectAllDistantInteractionPolylineVisual(IDistanceInteractor interactor,
            Color color, Material material)
        {
            InjectDistanceInteractor(interactor);
            InjectLineColor(color);
            InjectLineMaterial(material);
        }

        public void InjectLineColor(Color color)
        {
            _color = color;
        }

        public void InjectLineMaterial(Material material)
        {
            _lineMaterial = material;
        }

        #endregion
    }
}
