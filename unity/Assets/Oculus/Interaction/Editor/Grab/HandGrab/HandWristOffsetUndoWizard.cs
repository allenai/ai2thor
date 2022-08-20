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

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.HandGrab.Editor
{
    public class HandWristOffsetUndoWizard : ScriptableWizard
    {
        [SerializeField]
        private HandWristOffset _wristOffset;

        [SerializeField]
        private HandGrabPose _grabPose;

        [MenuItem("Oculus/Interaction/HandWristOffset Undo Wizard")]
        private static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<HandWristOffsetUndoWizard>("HandWristOffset Undo Wizard", "Close", "Undo Offset");
        }

        private void OnWizardCreate()
        {

        }

        private void OnWizardOtherButton()
        {
            List<HandGrabPose> children = new List<HandGrabPose>(_grabPose.GetComponentsInChildren<HandGrabPose>());
            children.Remove(_grabPose);
            foreach (HandGrabPose childPoint in children)
            {
                if (childPoint == _grabPose)
                {
                    continue;
                }

                childPoint.transform.SetParent(_grabPose.transform.parent, true);
                UndoOffset(childPoint);
            }

            UndoOffset(_grabPose);

            foreach (HandGrabPose childPoint in children)
            {
                childPoint.transform.SetParent(_grabPose.transform, true);
            }
        }

        private void UndoOffset(HandGrabPose grabPose)
        {
            Pose offset = Pose.identity;
            _wristOffset.GetOffset(ref offset, grabPose.HandPose.Handedness, grabPose.transform.localScale.x);
            offset.Invert();

            Undo.RecordObject(grabPose.transform, "Transform Changed");
            Pose pose = grabPose.transform.GetPose(Space.Self);
            pose.Premultiply(offset);
            grabPose.transform.SetPose(pose, Space.Self);
            EditorUtility.SetDirty(grabPose.transform);
        }

    }
}
