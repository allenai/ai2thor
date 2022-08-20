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
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction
{
    public class TouchShadowHand
    {
        private ShadowHand _shadowHand;
        public ShadowHand ShadowHand => _shadowHand;
        private IHandSphereMap _handSphereMap;
        private Handedness _handedness;
        private List<HandSphere> _spheres;

        public int TotalIterations
        {
            get
            {
                return _totalIterations;
            }
            set
            {
                _totalIterations = _totalIterations > 0 ? _totalIterations : 1;
            }
        }

        public int PushoutIterations
        {
            get
            {
                return _pushoutIterations;
            }
            set
            {
                _pushoutIterations = _pushoutIterations > 0 ? _pushoutIterations : 1;
            }
        }

        private int _totalIterations = 10;
        private int _pushoutIterations = 10;
        public int Iterations;

        public TouchShadowHand(IHandSphereMap map, Handedness handedness, int iterations = 10)
        {
            _shadowHand = new ShadowHand();
            _spheres = new List<HandSphere>();
            _handSphereMap = map;
            _handedness = handedness;
            PushoutIterations = TotalIterations = Iterations = iterations;
        }

        public void SetShadowRootFromHand(ShadowHand hand)
        {
            Pose pose = hand.GetRoot();
            _shadowHand.SetRoot(pose);
            _shadowHand.SetRootScale(hand.GetRootScale());
        }

        public void SetShadowRootFromHands(ShadowHand from, ShadowHand to, float t)
        {
            Pose rootFrom = from.GetRoot();
            Pose rootTo = to.GetRoot();
            rootFrom.Lerp(rootTo, t);
            _shadowHand.SetRoot(rootFrom);
            _shadowHand.SetRootScale(from.GetRootScale());
        }

        public void SetShadowFingerFrom(int fingerIdx, ShadowHand from)
        {
            HandJointId[] fingerJoints = FingersMetadata.FINGER_TO_JOINTS[fingerIdx];
            for (int i = 0; i < fingerJoints.Length; i++)
            {
                HandJointId fingerJoint = fingerJoints[i];
                Pose fromPose = from.GetLocalPose(fingerJoint);
                _shadowHand.SetLocalPose(fingerJoint, fromPose);
            }
        }

        private void SetShadowFingerFromLerp(int fingerIdx, ShadowHand from, ShadowHand to, float t)
        {
            HandJointId[] fingerJoints = FingersMetadata.FINGER_TO_JOINTS[fingerIdx];
            for (int i = 0; i < fingerJoints.Length; i++)
            {
                HandJointId fingerJoint = fingerJoints[i];
                Pose fromPose = from.GetLocalPose(fingerJoint);
                Pose toPose = to.GetLocalPose(fingerJoint);
                fromPose.Lerp(toPose, t);
                _shadowHand.SetLocalPose(fingerJoint, fromPose);
            }
        }

        private void SetShadowFingerFromLerps(int fingerIdx, ShadowHand from, ShadowHand to, float[] t)
        {
            HandJointId[] fingerJoints = FingersMetadata.FINGER_TO_JOINTS[fingerIdx];
            for (int i = 0; i < fingerJoints.Length; i++)
            {
                HandJointId fingerJoint = fingerJoints[i];
                Pose fromPose = from.GetLocalPose(fingerJoint);
                Pose toPose = to.GetLocalPose(fingerJoint);
                fromPose.Lerp(toPose, t[i]);
                _shadowHand.SetLocalPose(fingerJoint, fromPose);
            }
        }

        private void SetShadowFromLerpHands(ShadowHand from, ShadowHand to, float t)
        {
            Pose rootFrom = from.GetRoot();
            Pose rootTo = to.GetRoot();
            rootFrom.Lerp(rootTo, t);
            _shadowHand.SetRoot(rootFrom);
            _shadowHand.SetRootScale(from.GetRootScale());

            for (int i = 0; i < (int)HandJointId.HandEnd; i++)
            {
                Pose fromPose = from.GetLocalPose((HandJointId)i);
                Pose toPose = to.GetLocalPose((HandJointId)i);
                fromPose.Lerp(toPose, t);
                _shadowHand.SetLocalPose((HandJointId)i, fromPose);
            }
        }

        private void LoadSpheresForFingerFromShadow(int fingerIdx, int jointIdx = 0)
        {
            HandJointId[] fingerJoints = FingersMetadata.FINGER_TO_JOINTS[fingerIdx];
            _spheres.Clear();
            for (int i = jointIdx; i < fingerJoints.Length; i++)
            {
                HandJointId joint = fingerJoints[i];
                _handSphereMap.GetSpheres(_handedness,
                    joint,
                    _shadowHand.GetWorldPose(joint), _shadowHand.GetRootScale(), _spheres);
            }
        }

        private void LoadSpheresForHandFromShadow()
        {
            _spheres.Clear();
            for (int i = 0; i < (int)HandJointId.HandEnd; i++)
            {
                HandJointId joint = (HandJointId)i;
                _handSphereMap.GetSpheres(_handedness,
                    joint,
                    _shadowHand.GetWorldPose(joint), _shadowHand.GetRootScale(), _spheres);
            }
        }

        private List<int> _sphereHit = new List<int>();

        private bool CheckSphereCollision(ColliderGroup colliderGroup,
            Vector3 offset,
            List<int> sphereHit = null,
            List<int> sphereIndices = null)
        {
            bool hit = false;
            if (sphereHit != null)
            {
                sphereHit.Clear();
            }

            for (int i = 0; i < (sphereIndices == null ? _spheres.Count : sphereIndices.Count); i++)
            {
                int index = sphereIndices == null ? i : sphereIndices[i];
                HandSphere s = _spheres[index];
                if (!Collisions.IsSphereWithinCollider(s.Position - offset, s.Radius,
                    colliderGroup.Bounds))
                {
                    continue;
                }

                for (int j = 0; j < colliderGroup.Colliders.Count; j++)
                {
                    if (Collisions.IsSphereWithinCollider(s.Position - offset, s.Radius,
                        colliderGroup.Colliders[j]))
                    {
                        hit = true;
                        if (sphereHit == null)
                        {
                            return true;
                        }
                        else
                        {
                            sphereHit.Add(index);
                        }

                        break;
                    }

                }
            }

            return hit;
        }

        public bool CheckFingerTouch(int fingerIdx, int jointIdx, ColliderGroup colliderGroup, Vector3 offset, List<int> sphereHit = null)
        {
            LoadSpheresForFingerFromShadow(fingerIdx, jointIdx);
            return CheckSphereCollision(colliderGroup, offset, sphereHit);
        }

        public void CheckTouchFingers(ShadowHand hand, ColliderGroup colliderGroup, GrabTouchInfo result)
        {
            _shadowHand.Copy(hand);
            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                LoadSpheresForFingerFromShadow(i);
                _sphereHit.Clear();
                if (CheckFingerTouch(i, 0, colliderGroup, Vector3.zero, _sphereHit))
                {
                    result.grabbingFingers[i] = true;
                }
            }
        }

        public bool GrabReleaseFinger(int fingerIdx,
            ShadowHand fromHand, ShadowHand toHand,
            ColliderGroup colliderGroup,
            Vector3 offset)
        {

            // Setup from and to finger values
            float tinc = 1.0f / TotalIterations;

            float t = 0;
            while (true)
            {
                t = Mathf.Clamp01(t);
                SetShadowFingerFromLerp(fingerIdx, fromHand, toHand, t);
                LoadSpheresForFingerFromShadow(fingerIdx);
                if (!CheckFingerTouch(fingerIdx, 0, colliderGroup, offset, null))
                {
                    return true;
                }

                if (t == 1)
                {
                    return false;
                }

                t += tinc;
            }
        }


        public bool GrabConformFinger(int fingerIdx,
            ShadowHand fromHand, ShadowHand toHand,
            ColliderGroup colliderGroup,
            Vector3 offset)
        {

            // Setup from and to finger values
            float tinc = 1.0f / TotalIterations;

            float[] t = new float[FingersMetadata.FINGER_TO_JOINT_INDEX.Length];
            bool[] locked = new bool[FingersMetadata.FINGER_TO_JOINT_INDEX.Length];

            bool touching = false;
            bool done = false;
            bool unlocked = false;

            int maxLockedJoint = 0;
            while (true)
            {
                SetShadowFingerFromLerps(fingerIdx, fromHand, toHand, t);
                LoadSpheresForFingerFromShadow(fingerIdx);
                _sphereHit.Clear();
                if (CheckFingerTouch(fingerIdx, maxLockedJoint + 1, colliderGroup, offset, _sphereHit))
                {
                    for (int j = 0; j < _sphereHit.Count; j++)
                    {
                        HandSphere sphereData = _spheres[_sphereHit[j]];
                        HandJointId joint = sphereData.Joint;
                        int idx = FingersMetadata.JOINT_TO_FINGER_INDEX[(int)joint];
                        for (int k = idx; k >= 0; k--)
                        {
                            if (locked[k])
                            {
                                continue;
                            }

                            locked[k] = true;
                            touching = true;
                            if (maxLockedJoint < idx)
                            {
                                maxLockedJoint = idx;
                            }
                        }
                    }
                }

                for (int j = 0; j < t.Length; j++)
                {
                    if (locked[j])
                    {
                        continue;
                    }

                    unlocked = true;
                    t[j] += tinc;
                    if (t[j] > 1)
                    {
                        t[j] = 1;
                        done = true;
                    }
                }

                if (!unlocked || done)
                {
                    SetShadowFingerFromLerps(fingerIdx, fromHand, toHand, t);
                    break;
                }
            }

            return touching;
        }

        public void GrabConformFingers(ShadowHand fromHand, ShadowHand toHand,
            ColliderGroup colliderGroup,
            Vector3 offset)
        {
            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                GrabConformFinger(i, fromHand, toHand, colliderGroup, offset);
            }
        }

        public bool PushoutFinger(int fingerIdx, ShadowHand from, ShadowHand to,
            ColliderGroup colliderGroup, Vector3 offset)
        {
            float tinc = 1.0f / TotalIterations;

            float t = 0;
            while (true)
            {
                if (t > 1)
                {
                    t = Mathf.Clamp01(t);
                }

                SetShadowFingerFromLerp(fingerIdx, from, to, t);
                LoadSpheresForFingerFromShadow(fingerIdx);
                if (!CheckFingerTouch(fingerIdx, 0, colliderGroup, offset, null))
                {
                    return true;
                }

                if (t == 1)
                {
                    return false;
                }

                t += tinc;
            }
        }

        public class GrabTouchInfo
        {
            public Vector3 offset;
            public bool grabbing = false;
            public bool[] grabbingFingers = new bool[5];
            public float grabT = 0;
        }

        public void GrabTouchStep(ShadowHand from, ShadowHand to,
            ColliderGroup colliderGroup,
            int iteration,
            Vector3 colliderOffset, bool pushout,
            GrabTouchInfo result)
        {
            if (iteration > TotalIterations)
            {
                return;
            }

            float tInc = 1.0f / TotalIterations;
            float t = Mathf.Clamp01(iteration * tInc);

            result.offset = colliderOffset;

            Pose fromWristRoot = from.GetRoot();
            Pose toWristRoot = to.GetRoot();
            Vector3 deltaWristPosition = (toWristRoot.position - fromWristRoot.position) * tInc;

            SetShadowFromLerpHands(from, to, t);
            LoadSpheresForHandFromShadow();
            _sphereHit.Clear();

            for (int i = 0; i < 5; i++)
            {
                result.grabbingFingers[i] = false;
            }

            if (CheckSphereCollision(colliderGroup, result.offset, _sphereHit))
            {
                // Check which fingers are touching
                // A grab is any finger + thumb or palm
                bool finger = false;
                bool thumb = false;
                bool palm = false;
                for (int i = 0; i < _sphereHit.Count; i++)
                {
                    HandSphere sphereData = _spheres[_sphereHit[i]];
                    HandJointId joint = sphereData.Joint;

                    int fingerIndex = (int)FingersMetadata.JOINT_TO_FINGER[(int)joint];
                    if (fingerIndex >= 0)
                    {
                        result.grabbingFingers[fingerIndex] = true;
                    }

                    if (fingerIndex > 0)
                    {
                        finger = true;
                    }
                    else if (fingerIndex == 0)
                    {
                        thumb = true;
                    }
                    else
                    {
                        palm = true;
                    }
                }

                if (finger && (thumb || palm))
                {
                    result.grabbing = true;
                    result.grabT = t;
                    return;
                }

                if (!pushout)
                {
                    return;
                }

                Vector3 avgDir = new Vector3();
                SetShadowFromLerpHands(from, to, Mathf.Clamp01(t + tInc));
                LoadSpheresForHandFromShadow();
                for (int i = 0; i < _spheres.Count; i++)
                {
                    avgDir += _spheres[i].Position / _spheres.Count;
                }

                SetShadowFromLerpHands(from, to, Mathf.Clamp01(t - tInc));
                LoadSpheresForHandFromShadow();
                for (int i = 0; i < _spheres.Count; i++)
                {
                    avgDir -= _spheres[i].Position / _spheres.Count;
                }

                float avgRadius = 0;
                for (int i = 0; i < _sphereHit.Count; i++)
                {
                    avgRadius += _spheres[_sphereHit[i]].Radius / _sphereHit.Count;
                }

                avgDir -= deltaWristPosition;

                Vector3 pushoutInc = avgRadius * avgDir.normalized;
                SetShadowFromLerpHands(from, to, t);
                LoadSpheresForHandFromShadow();
                bool noTouch = false;
                for (int j = 0; j < PushoutIterations; j++)
                {
                    result.offset += pushoutInc;
                    if (!CheckSphereCollision(colliderGroup, result.offset,
                        null,
                        _sphereHit))
                    {
                        noTouch = true;
                        break;
                    }
                }

                if (!noTouch)
                {
                    result.offset = Vector3.zero;
                    result.grabbing = false;
                    SetShadowFromLerpHands(from, to, 1);
                }
            }
        }

        public void GrabTouch(ShadowHand fromHand, ShadowHand toHand, ColliderGroup colliderGroup,
            bool pushout, GrabTouchInfo result)
        {
            result.grabbing = false;
            result.offset = Vector3.zero;
            for (int i = 0; i <= Iterations; i++)
            {
                GrabTouchStep(fromHand, toHand, colliderGroup, i, result.offset, pushout, result);
                if (result.grabbing)
                {
                    break;
                }
            }
        }

        public void GetJointsFromShadow(HandJointId[] jointIds, Pose[] outJoints, bool local)
        {
            for (int i = 0; i < jointIds.Length; i++)
            {
                outJoints[i] = local?
                    _shadowHand.GetLocalPose(jointIds[i]):
                    _shadowHand.GetWorldPose(jointIds[i]);
            }
        }
    }
}
