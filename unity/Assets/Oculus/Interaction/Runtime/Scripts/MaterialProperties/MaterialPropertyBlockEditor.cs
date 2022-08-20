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

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction
{
    [Serializable]
    public struct MaterialPropertyVector
    {
        public string name;
        public Vector4 value;
    }

    [Serializable]
    public struct MaterialPropertyColor
    {
        public string name;
        public Color value;
    }

    [Serializable]
    public struct MaterialPropertyFloat
    {
        public string name;
        public float value;
    }

    [ExecuteAlways]
    public class MaterialPropertyBlockEditor : MonoBehaviour
    {
        [SerializeField]
        private List<Renderer> _renderers;

        [SerializeField]
        private List<MaterialPropertyVector> _vectorProperties;

        [SerializeField]
        private List<MaterialPropertyColor> _colorProperties;

        [SerializeField]
        private List<MaterialPropertyFloat> _floatProperties;

        [SerializeField]
        private bool _updateEveryFrame = true;

        public List<Renderer> Renderers
        {
            get
            {
                return _renderers;
            }
            set
            {
                _renderers = value;
            }
        }

        public List<MaterialPropertyVector> VectorProperties
        {
            get
            {
                return _vectorProperties;
            }
            set
            {
                _vectorProperties = value;
            }
        }

        public List<MaterialPropertyColor> ColorProperties
        {
            get
            {
                return _colorProperties;
            }
            set
            {
                _colorProperties = value;
            }
        }

        public List<MaterialPropertyFloat> FloatProperties
        {
            get
            {
                return _floatProperties;
            }
            set
            {
                _floatProperties = value;
            }
        }

        public MaterialPropertyBlock MaterialPropertyBlock
        {
            get
            {
                if (_materialPropertyBlock == null)
                {
                    _materialPropertyBlock = new MaterialPropertyBlock();
                }

                return _materialPropertyBlock;
            }
        }

        private MaterialPropertyBlock _materialPropertyBlock = null;

        protected virtual void Awake()
        {
            if (_renderers == null)
            {
                Renderer renderer = GetComponent<Renderer>();
                if (renderer != null)
                {
                    _renderers = new List<Renderer>()
                    {
                        renderer
                    };
                }
            }
            UpdateMaterialPropertyBlock();
        }

        public void UpdateMaterialPropertyBlock()
        {
            var materialPropertyBlock = MaterialPropertyBlock;

            if (_vectorProperties != null)
            {
                foreach (var property in _vectorProperties)
                {
                    _materialPropertyBlock.SetVector(property.name, property.value);
                }
            }

            if (_colorProperties != null)
            {
                foreach (var property in _colorProperties)
                {
                    _materialPropertyBlock.SetColor(property.name, property.value);
                }
            }

            if (_floatProperties != null)
            {
                foreach (var property in _floatProperties)
                {
                    _materialPropertyBlock.SetFloat(property.name, property.value);
                }
            }

            if (_renderers != null)
            {
                foreach (Renderer renderer in _renderers)
                {
                    renderer.SetPropertyBlock(materialPropertyBlock);
                }
            }
        }

        protected virtual void Update()
        {
            if (_updateEveryFrame)
            {
                UpdateMaterialPropertyBlock();
            }
        }
    }
}
