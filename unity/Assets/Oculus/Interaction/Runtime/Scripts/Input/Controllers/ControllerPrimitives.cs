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

namespace Oculus.Interaction.Input
{
    // Enum containing all values of Unity.XR.CommonUsage.
    [Flags]
    public enum ControllerButtonUsage
    {
        None = 0,
        PrimaryButton = 1 << 0,
        PrimaryTouch = 1 << 1,
        SecondaryButton = 1 << 2,
        SecondaryTouch = 1 << 3,
        GripButton = 1 << 4,
        TriggerButton = 1 << 5,
        MenuButton = 1 << 6,
        Primary2DAxisClick = 1 << 7,
        Primary2DAxisTouch = 1 << 8,
        Thumbrest = 1 << 9,
    }
}
