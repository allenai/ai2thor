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

namespace Oculus.Interaction
{
    [ExecuteAlways]
    public class RoundedBoxProperties : MonoBehaviour
    {
        [SerializeField]
        private MaterialPropertyBlockEditor _editor;

        [SerializeField]
        private float _width = 1.0f;

        [SerializeField]
        private float _height = 1.0f;

        [SerializeField]
        private Color _color = Color.white;

        [SerializeField]
        private Color _borderColor = Color.black;

        [SerializeField]
        private float _radiusTopLeft;

        [SerializeField]
        private float _radiusTopRight;

        [SerializeField]
        private float _radiusBottomLeft;

        [SerializeField]
        private float _radiusBottomRight;

        [SerializeField]
        private float _borderInnerRadius;

        [SerializeField]
        private float _borderOuterRadius;

        #region Properties

        public float Width
        {
            get
            {
                return _width;
            }
            set
            {
                _width = value;
            }
        }

        public float Height
        {
            get
            {
                return _height;
            }
            set
            {
                _height = value;
            }
        }

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

        public Color BorderColor
        {
            get
            {
                return _borderColor;
            }
            set
            {
                _borderColor = value;
            }
        }

        public float RadiusTopLeft
        {
            get
            {
                return _radiusTopLeft;
            }
            set
            {
                _radiusTopLeft = value;
            }
        }

        public float RadiusTopRight
        {
            get
            {
                return _radiusTopRight;
            }
            set
            {
                _radiusTopRight = value;
            }
        }

        public float RadiusBottomLeft
        {
            get
            {
                return _radiusBottomLeft;
            }
            set
            {
                _radiusBottomLeft = value;
            }
        }

        public float RadiusBottomRight
        {
            get
            {
                return _radiusBottomRight;
            }
            set
            {
                _radiusBottomRight = value;
            }
        }

        public float BorderInnerRadius
        {
            get
            {
                return _borderInnerRadius;
            }
            set
            {
                _borderInnerRadius = value;
            }
        }

        public float BorderOuterRadius
        {
            get
            {
                return _borderOuterRadius;
            }
            set
            {
                _borderOuterRadius = value;
            }
        }

        #endregion

        private readonly int _colorShaderID = Shader.PropertyToID("_Color");
        private readonly int _borderColorShaderID = Shader.PropertyToID("_BorderColor");
        private readonly int _radiiShaderID = Shader.PropertyToID("_Radii");
        private readonly int _dimensionsShaderID = Shader.PropertyToID("_Dimensions");

        protected virtual void Awake()
        {
            UpdateSize();
            UpdateMaterialPropertyBlock();
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(_editor);
            UpdateSize();
            UpdateMaterialPropertyBlock();
        }

        private void UpdateSize()
        {
            transform.localScale = new Vector3(_width + _borderOuterRadius * 2,
                                               _height + _borderOuterRadius * 2,
                                               1.0f);
        }

        private void UpdateMaterialPropertyBlock()
        {
            if (_editor == null)
            {
                _editor = GetComponent<MaterialPropertyBlockEditor>();
                if (_editor == null)
                {
                    return;
                }
            }

            MaterialPropertyBlock block = _editor.MaterialPropertyBlock;

            block.SetColor(_colorShaderID, _color);
            block.SetColor(_borderColorShaderID, _borderColor);
            block.SetVector(_radiiShaderID,
                                             new Vector4(
                                                _radiusTopRight,
                                                _radiusBottomRight,
                                                _radiusTopLeft,
                                                _radiusBottomLeft
                                             ));
            block.SetVector(_dimensionsShaderID,
                                             new Vector4(
                                                transform.localScale.x,
                                                transform.localScale.y,
                                                _borderInnerRadius,
                                                _borderOuterRadius
                                             ));

            _editor.UpdateMaterialPropertyBlock();
        }

        protected virtual void OnValidate()
        {
            UpdateSize();
            UpdateMaterialPropertyBlock();
        }
    }
}
