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
using UnityEngine.Assertions;

namespace Oculus.Interaction.Input
{
    public partial class OneEuroFilter
    {
        /// <summary>
        /// Implementation of <see cref="IOneEuroFilter{TData}"/> that acts on 
        /// data types with multiple <see cref="float"/> components, such as 
        /// <see cref="UnityEngine.Vector3"/>
        /// </summary>
        /// <typeparam name="TData">The multi-component datatype to filter</typeparam>
        private class OneEuroFilterMulti<TData> : IOneEuroFilter<TData>
        {
            public TData Value { get; private set; }

            private readonly Func<float[], TData> _arrayToType;
            private readonly Func<TData, int, float> _getValAtIndex;
            private readonly IOneEuroFilter<float>[] _filters;
            private readonly float[] _componentValues;

            public OneEuroFilterMulti(int numComponents,
                                      Func<float[], TData> arrayToType,
                                      Func<TData, int, float> getValAtIndex)
            {
                Assert.IsNotNull(arrayToType);
                Assert.IsNotNull(getValAtIndex);
                Assert.IsTrue(numComponents > 0);

                _filters = new OneEuroFilter[numComponents];
                _componentValues = new float[numComponents];
                _arrayToType = arrayToType;
                _getValAtIndex = getValAtIndex;

                for (int i = 0; i < _filters.Length; ++i)
                {
                    _filters[i] = new OneEuroFilter();
                }
            }

            public void SetProperties(in OneEuroFilterPropertyBlock properties)
            {
                foreach (var filter in _filters)
                {
                    filter.SetProperties(properties);
                }
            }

            public TData Step(TData newValue, float deltaTime)
            {
                for (int i = 0; i < _filters.Length; ++i)
                {
                    float componentValue = _getValAtIndex(newValue, i);
                    _componentValues[i] = _filters[i].Step(componentValue, deltaTime);
                }

                Value = _arrayToType(_componentValues);
                return Value;
            }

            public void Reset()
            {
                foreach (var filter in _filters)
                {
                    filter.Reset();
                }
            }
        }
    }
}
