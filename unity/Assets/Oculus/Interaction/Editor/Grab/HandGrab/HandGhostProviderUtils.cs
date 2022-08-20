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

using UnityEditor;
using UnityEngine;
using Oculus.Interaction.HandGrab.Visuals;

namespace Oculus.Interaction.HandGrab.Editor
{
    public class HandGhostProviderUtils
    {
        public static bool TryGetDefaultProvider(out HandGhostProvider provider)
        {
            provider = null;
            HandGhostProvider[] providers = Resources.FindObjectsOfTypeAll<HandGhostProvider>();
            if (providers != null && providers.Length > 0)
            {
                provider = providers[0];
                return true;
            }

            string[] assets = AssetDatabase.FindAssets($"t:{nameof(HandGhostProvider)}");
            if (assets != null && assets.Length > 0)
            {
                string pathPath = AssetDatabase.GUIDToAssetPath(assets[0]);
                provider = AssetDatabase.LoadAssetAtPath<HandGhostProvider>(pathPath);
            }


            return provider != null;
        }
    }
}
