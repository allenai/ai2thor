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
using Facebook.WitAi;
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Data;
using Facebook.WitAi.Data.Configuration;
using UnityEditor;
using UnityEngine;

namespace Oculus.Voice.Data
{
    [Serializable]
    public class VoiceSDKDataCreation
    {
        [MenuItem("Assets/Create/Voice SDK/Add App Voice Experience to Scene")]
        public static void AddVoiceCommandServiceToScene()
        {
            var witGo = new GameObject();
            witGo.name = "App Voice Experience";
            var wit = witGo.AddComponent<AppVoiceExperience>();
            wit.RuntimeConfiguration = new WitRuntimeConfiguration
            {
                witConfiguration = WitDataCreation.FindDefaultWitConfig()
            };
        }

        [MenuItem("Assets/Create/Voice SDK/Values/String Value")]
        public static void WitStringValue()
        {
            WitDataCreation.CreateStringValue("");
        }

        [MenuItem("Assets/Create/Voice SDK/Values/Float Value")]
        public static void WitFloatValue()
        {
            WitDataCreation.CreateFloatValue("");
        }

        [MenuItem("Assets/Create/Voice SDK/Values/Int Value")]
        public static void WitIntValue()
        {
            WitDataCreation.CreateIntValue("");
        }

        [MenuItem("Assets/Create/Voice SDK/Configuration")]
        public static void CreateWitConfiguration()
        {
            WitConfigurationUtility.CreateConfiguration(WitAuthUtility.ServerToken);
        }
    }
}
