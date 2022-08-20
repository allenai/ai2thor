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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    /// <summary>
    /// Generate a mapping of joints to spheres from a HandPrefabDataSource
    /// that has a set of transforms representing sphere positions and radii.
    /// </summary>
    public class HandSphereMap : MonoBehaviour, IHandSphereMap
    {
        [SerializeField]
        public FromHandPrefabDataSource _handPrefabDataSource;
        private List<List<HandSphere>> _sourceSphereMap;

        protected virtual void Awake()
        {
            _sourceSphereMap = new List<List<HandSphere>>();
            for (int i = 0; i < (int)HandJointId.HandEnd; i++)
            {
                _sourceSphereMap.Add(new List<HandSphere>());
            }
        }

        protected virtual void Start()
        {
            float handednessMult = _handPrefabDataSource.Handedness == Handedness.Left ? -1 : 1f;

            for (int i = 0; i < (int)HandJointId.HandEnd; i++)
            {
                List<HandSphere> spheres = _sourceSphereMap[i];
                HandJointId joint = (HandJointId)i;
                Transform assocTransform = _handPrefabDataSource.GetTransformFor(joint);
                foreach (Transform t in assocTransform)
                {
                    if (t.name != "sphere")
                    {
                        continue;
                    }

                    spheres.Add(new HandSphere(t.localPosition * handednessMult, t.lossyScale.x / 2.0f, joint));
                    Destroy(t.gameObject);
                }
            }
        }

        public void GetSpheres(Handedness handedness, HandJointId joint, Pose pose, float scale,
            List<HandSphere> spheres)
        {
            int idx = (int)joint;
            for (int j = 0; j < _sourceSphereMap[idx].Count; j++)
            {
                HandSphere sphere = _sourceSphereMap[idx][j];
                Vector3 spherePosition = (handedness == Handedness.Left ? -1 : 1) * sphere.Position;
                HandSphere target = new HandSphere(
                    pose.rotation * spherePosition * scale + pose.position,
                    sphere.Radius*scale,
                    sphere.Joint);
                spheres.Add(target);
            }
        }
    }
}
