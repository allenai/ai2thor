/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEditor;

namespace Facebook.WitAi.Lib
{
    [CustomEditor(typeof(Mic))]
    public class MicEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var mic = (Mic) target;

            int index = EditorGUILayout.Popup("Input", mic.CurrentDeviceIndex, mic.Devices.ToArray());
            if (index != mic.CurrentDeviceIndex)
            {
                mic.ChangeDevice(index);
            }
        }
    }
}
