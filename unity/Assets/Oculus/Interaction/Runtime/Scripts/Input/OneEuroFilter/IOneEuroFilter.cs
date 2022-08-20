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

namespace Oculus.Interaction.Input
{
    public interface IOneEuroFilter<TData>
    {
        /// <summary>
        /// The last value returned by <see cref="Step(TData, float)"/>
        /// </summary>
        TData Value { get; }

        /// <summary>
        /// Update the parameters of the filter
        /// </summary>
        /// <param name="propertyBlock">The property block containing the parameters to se</param>
        void SetProperties(in OneEuroFilterPropertyBlock properties);

        /// <summary>
        /// Update the filter with a new noisy value to be smoothed.
        /// This is a destructive operation that should be run once per frame, as
        /// calling this updates the previous frame data.
        /// </summary>
        /// <param name="rawValue">The noisy value to be filtered</param>
        /// <param name="deltaTime">The time between steps, use to derive filter frequency.
        /// Omitting this value will fallback to <see cref="OneEuroFilter._DEFAULT_FREQUENCY_HZ"/></param>
        /// <returns>The filtered value, equivalent to <see cref="Value"/></returns>
        TData Step(TData rawValue, float deltaTime = 1f / OneEuroFilter._DEFAULT_FREQUENCY_HZ);

        /// <summary>
        /// Clear previous values and reset the filter
        /// </summary>
        void Reset();
    }
}
