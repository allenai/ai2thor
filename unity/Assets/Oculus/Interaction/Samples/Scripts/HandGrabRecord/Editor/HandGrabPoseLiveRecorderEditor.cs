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

using Oculus.Interaction.HandGrab.Editor;
using UnityEditor;

namespace Oculus.Interaction.HandGrab.Recorder.Editor
{
    [CustomEditor(typeof(HandGrabPoseLiveRecorder))]
    public class HandGrabPoseLiveRecorderEditor : UnityEditor.Editor
    {
        private SerializedProperty _ghostProviderProperty;

        private void Awake()
        {
            _ghostProviderProperty = serializedObject.FindProperty("_ghostProvider");
            AssignMissingGhostProvider();
        }

        private void AssignMissingGhostProvider()
        {
            if (_ghostProviderProperty.objectReferenceValue != null)
            {
                return;
            }

            if (HandGhostProviderUtils.TryGetDefaultProvider(out Visuals.HandGhostProvider provider))
            {
                _ghostProviderProperty.objectReferenceValue = provider;
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
