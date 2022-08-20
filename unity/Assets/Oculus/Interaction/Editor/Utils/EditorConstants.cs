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

namespace Oculus.Interaction.Editor
{
    public class EditorConstants
    {
        public static readonly Color PRIMARY_COLOR = new Color(0f, 1f, 1f, 0.5f);
        public static readonly Color PRIMARY_COLOR_DISABLED = new Color(0f, 1f, 1f, 0.1f);

        public static readonly Color SECONDARY_COLOR = new Color(0.5f, 0.3f, 1f, 0.5f);
        public static readonly Color SECONDARY_COLOR_DISABLED = new Color(0.5f, 0.3f, 1f, 0.1f);

        public static readonly float LINE_THICKNESS = 2f;

        public static readonly float ROW_HEIGHT = 20f;
    }
}
