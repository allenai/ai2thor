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
    public class DotGridProperties : MonoBehaviour
    {
        [SerializeField]
        private MaterialPropertyBlockEditor _materialPropertyBlockEditor;

        [SerializeField]
        private int _columns;

        [SerializeField]
        private int _rows;

        [SerializeField]
        private float _radius;

        [SerializeField]
        private Color _color;

        public int Columns
        {
            get
            {
                return _columns;
            }
            set
            {
                _columns = value;
            }
        }

        public int Rows
        {
            get
            {
                return _rows;
            }
            set
            {
                _rows = value;
            }
        }

        public float Radius
        {
            get
            {
                return _radius;
            }
            set
            {
                _radius = value;
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

        private bool _change = false;
        private readonly int _colorShaderID = Shader.PropertyToID("_Color");
        private readonly int _dimensionsShaderID = Shader.PropertyToID("_Dimensions");

        protected virtual void Start()
        {
            Assert.IsNotNull(_materialPropertyBlockEditor);
            _change = true;
        }

        protected virtual void Update()
        {
            if (!_change || _materialPropertyBlockEditor == null)
            {
                return;
            }

            MaterialPropertyBlock block = _materialPropertyBlockEditor.MaterialPropertyBlock;
            block.SetColor(_colorShaderID, _color);
            block.SetVector(_dimensionsShaderID, new Vector4((float)_columns, (float)_rows, _radius, 0));
            _materialPropertyBlockEditor.UpdateMaterialPropertyBlock();

            _change = false;
        }

        protected virtual void OnValidate()
        {
            _change = true;
        }

        #region Inject

        public void InjectAllDotGridProperties(MaterialPropertyBlockEditor editor)
        {
            InjectMaterialPropertyBlockEditor(editor);
        }

        public void InjectMaterialPropertyBlockEditor(MaterialPropertyBlockEditor editor)
        {
            _materialPropertyBlockEditor = editor;
        }

        #endregion
    }
}
