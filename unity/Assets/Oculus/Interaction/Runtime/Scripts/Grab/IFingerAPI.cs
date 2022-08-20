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

namespace Oculus.Interaction
{
    /// <summary>
    /// This interface is used for tracking the grabbing strength of the fingers.
    /// Since said strength can be measured with different methods: pinching, curl,
    /// trigger presses, etc. Multiple implementations are needed.
    /// </summary>
    public interface IFingerAPI
    {
        bool GetFingerIsGrabbing(HandFinger finger);
        bool GetFingerIsGrabbingChanged(HandFinger finger, bool targetPinchState);
        float GetFingerGrabScore(HandFinger finger);
        Vector3 GetCenterOffset();
        void Update(IHand hand);
    }
}
