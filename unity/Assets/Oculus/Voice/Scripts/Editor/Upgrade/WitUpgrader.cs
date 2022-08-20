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

using Facebook.WitAi;
using UnityEditor;
using UnityEngine;

namespace Oculus.Voice.Upgrade
{
    [CustomEditor(typeof(Wit), false)]
    public class WitUpgrader : Editor
    {
        class Styles
        {
            public static GUIContent upgrade = new GUIContent("Upgrade to App Voice Experience",
                "This will replace your Wit object with a comparable component from the Voice SDK.");
        }

        public override void OnInspectorGUI()
        {
            var wit = (Wit) target;
            if (!wit.GetComponent<AppVoiceExperience>())
            {
                base.OnInspectorGUI();

                if (!Application.isPlaying && GUILayout.Button(Styles.upgrade))
                {
                    var voiceService = wit.gameObject.AddComponent<AppVoiceExperience>();
                    voiceService.events = wit.events;
                    voiceService.RuntimeConfiguration = wit.RuntimeConfiguration;
                    var voiceServiceSerializedObject = new SerializedObject(voiceService);
                    voiceServiceSerializedObject.ApplyModifiedProperties();
                    DestroyImmediate(wit);
                }
            }
        }
    }
}
