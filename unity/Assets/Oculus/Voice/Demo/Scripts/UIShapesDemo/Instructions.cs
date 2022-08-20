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
using Facebook.WitAi.Data.Configuration;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Oculus.Voice.Demo.UIShapesDemo
{
    [ExecuteAlways]
    public class Instructions : MonoBehaviour
    {
        internal enum Step
        {
            SetupWit = 0,
            MissingServerToken,
            MissingClientToken,
            AddConfig,
            AddVoiceExperiences,
            SetConfig,
            Ready
        }

        static readonly string[] steps = new string[]
        {
            "Create an application at https://wit.ai. You can import the \"shapes_demo - Wit.ai Config.zip\" in the Demo/Data directory to create it for you.\n\nConnect to the Wit.ai app by clicking Oculus>Voice SDK>Settings and copy the Server Access Token from the Wit.ai app's settings page.Next, create a new Wit configuration by clicking Create.",
            "Copy the Server Access Token from the Wit.ai app's settings page and paste it in field found in Oculus/Voice SDK/Settings.",
            "Wit configuration is missing a Client Access Token. Open the Wit configuration, expand Application Configuration, and click Refresh or paste a Client Access Token from your Wit.ai app settings page.",
            "Create a Wit configuration by clicking Assets/Create/Voice SDK/Configuration.",
            "The scene is missing the App Voice Experience component. Add it by clicking Assets/Create/Voice SDK/Add App Voice Experience to Scene.",
            "The App Voice Experience GameObject is missing its Wit configuration. Set the configuration to begin trying voice commands.",
            ""
        };

        [SerializeField] private Text instructionText;

        private Step currentStep = Step.Ready;
        internal Step CurrentStep => currentStep;
        internal string CurrentStepText => steps[(int) currentStep];

        private void OnValidate()
        {
            UpdateStep();
        }

        private void OnEnable()
        {
            UpdateStep();
        }

        private void Update()
        {
            UpdateStep();
        }

        private void UpdateStep()
        {
#if UNITY_EDITOR
            var appVoiceExperience = FindObjectOfType<AppVoiceExperience>();
            string[] guids = AssetDatabase.FindAssets("t:WitConfiguration");
            if (guids.Length == 0)
            {
                currentStep = Step.SetupWit;
            }
            else if (!appVoiceExperience)
            {
                currentStep = Step.AddVoiceExperiences;
            }
            else if (!appVoiceExperience.RuntimeConfiguration.witConfiguration)
            {
                currentStep = Step.SetConfig;
                appVoiceExperience.RuntimeConfiguration.witConfiguration =
                    AssetDatabase.LoadAssetAtPath<WitConfiguration>(
                        AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            else if (!WitAuthUtility.IsServerTokenValid())
            {
                currentStep = Step.MissingServerToken;
            }
            else if (string.IsNullOrEmpty(appVoiceExperience.RuntimeConfiguration?.witConfiguration
                .clientAccessToken))
            {
                currentStep = Step.MissingClientToken;
            }
            else
            {
                currentStep = Step.Ready;
            }


            instructionText.text = steps[(int) currentStep];
#endif
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Instructions))]
    public class InstructionManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var instructions = (Instructions) target;

            if (instructions.CurrentStep == Instructions.Step.Ready)
            {
                GUILayout.Label(
                    "Everything is ready. Press play to test activation via the Activate button.");
            }
            else
            {
                GUILayout.TextArea(instructions.CurrentStepText);
                GUILayout.Space(32);

                switch (instructions.CurrentStep)
                {
                    case Instructions.Step.SetupWit:
                        SetupWitResources();
                        break;
                    case Instructions.Step.MissingServerToken:
                        MissingServerTokenResources();
                        break;
                    case Instructions.Step.MissingClientToken:
                        MissingClientTokenResources();
                        break;
                }
            }
        }

        private void MissingClientTokenResources()
        {
            GUILayout.Label("Resources", EditorStyles.boldLabel);

            if (GUILayout.Button("Select Wit Config"))
            {
                Selection.activeObject = (FindObjectOfType<AppVoiceExperience>()
                    .RuntimeConfiguration.witConfiguration);
            }

            if (GUILayout.Button("Open Wit.ai"))
            {
                Application.OpenURL("https://wit.ai/apps");
            }
        }

        private void MissingServerTokenResources()
        {
            GUILayout.Label("Resources", EditorStyles.boldLabel);

            if (GUILayout.Button("Open Wit.ai"))
            {
                Application.OpenURL("https://wit.ai/apps");
            }
        }

        private void SetupWitResources()
        {
            GUILayout.Label("Resources", EditorStyles.boldLabel);

            if (GUILayout.Button("Open Wit.ai"))
            {
                Application.OpenURL("https://wit.ai/apps");
            }

            GUILayout.Label("Wit.ai Sample Application File");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Open In Explorer"))
            {
                EditorUtility.RevealInFinder("Assets/Oculus/Voice/Demo/Data/");
            }

            if (GUILayout.Button("Copy Path"))
            {
                GUIUtility.systemCopyBuffer = Application.dataPath + "/Oculus/Voice/Demo/Data";
            }

            GUILayout.EndHorizontal();
        }
    }
#endif
}
