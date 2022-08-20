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
using TMPro;
using UnityEngine;

namespace Oculus.Interaction.Samples
{
    public class PoseUseSample : MonoBehaviour
    {
        [SerializeField] private ActiveStateSelector[] _poses;
        [SerializeField] private Material[] _onSelectIcons;
        [SerializeField] private GameObject _poseActiveVisualPrefab;

        private GameObject[] _poseActiveVisuals;

        private void Start()
        {
            _poseActiveVisuals = new GameObject[_poses.Length];
            for (int i = 0; i < _poses.Length; i++)
            {
                _poseActiveVisuals[i] = Instantiate(_poseActiveVisualPrefab);
                _poseActiveVisuals[i].GetComponentInChildren<TextMeshPro>().text = _poses[i].name;
                _poseActiveVisuals[i].GetComponentInChildren<ParticleSystemRenderer>().material = _onSelectIcons[i];
                _poseActiveVisuals[i].SetActive(false);

                int poseNumber = i;
                _poses[i].WhenSelected += () => ShowVisuals(poseNumber);
                _poses[i].WhenUnselected += () => HideVisuals(poseNumber);
            }
        }
        private void ShowVisuals(int poseNumber)
        {
            var centerEyePos = FindObjectOfType<OVRCameraRig>().centerEyeAnchor.position;
            Vector3 spawnSpot = centerEyePos + FindObjectOfType<OVRCameraRig>().centerEyeAnchor.forward;

            _poseActiveVisuals[poseNumber].transform.position = spawnSpot;
            _poseActiveVisuals[poseNumber].transform.LookAt(2 * _poseActiveVisuals[poseNumber].transform.position - centerEyePos);

            var hands = _poses[poseNumber].GetComponents<HandRef>();
            Vector3 visualsPos = Vector3.zero;
            foreach (var hand in hands)
            {
                hand.GetRootPose(out Pose wristPose);
                Vector3 forward = hand.Handedness == Handedness.Left ? wristPose.right : -wristPose.right;
                visualsPos += wristPose.position + forward * .15f + Vector3.up * .02f;
            }
            _poseActiveVisuals[poseNumber].transform.position = visualsPos / hands.Length;
            _poseActiveVisuals[poseNumber].gameObject.SetActive(true);
        }

        private void HideVisuals(int poseNumber)
        {
            _poseActiveVisuals[poseNumber].gameObject.SetActive(false);
        }
    }
}
