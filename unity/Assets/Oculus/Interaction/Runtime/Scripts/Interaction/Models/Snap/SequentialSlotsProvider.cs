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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    /// <summary>
    /// This ISnapSlotsProvider uses a ordered list of individual Slots and will
    /// push the elements back or forth to make room for the new element.
    /// </summary>
    public class SequentialSlotsProvider : MonoBehaviour, ISnapPoseProvider
    {
        [SerializeField]
        private List<Transform> _slots;

        private int[] _slotInteractors;

        protected bool _started;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            Assert.IsTrue(_slots != null && _slots.Count > 0);
            _slotInteractors = new int[_slots.Count];

            this.EndStart(ref _started);
        }

        public void TrackInteractor(SnapInteractor interactor)
        {
            int desiredIndex = FindBestSlotIndex(interactor.SnapPose.position);
            if (TryOccupySlot(desiredIndex))
            {
                _slotInteractors[desiredIndex] = interactor.Identifier;
            }
        }

        public void UntrackInteractor(SnapInteractor interactor)
        {
            if (TryFindIndexForInteractor(interactor, out int index))
            {
                _slotInteractors[index] = 0;
            }
        }

        public void SnapInteractor(SnapInteractor interactor)
        {
        }

        public void UnsnapInteractor(SnapInteractor interactor)
        {
        }

        public void UpdateTrackedInteractor(SnapInteractor interactor)
        {
            int desiredIndex = FindBestSlotIndex(interactor.SnapPose.position);
            if (TryFindIndexForInteractor(interactor, out int index))
            {
                if (desiredIndex != index)
                {
                    _slotInteractors[index] = 0;
                    if (TryOccupySlot(desiredIndex))
                    {
                        _slotInteractors[desiredIndex] = interactor.Identifier;
                    }
                }
            }
            else if (TryOccupySlot(desiredIndex))
            {
                _slotInteractors[desiredIndex] = interactor.Identifier;
            }
        }

        private bool TryFindIndexForInteractor(SnapInteractor interactor, out int index)
        {
            //FindIndex is not ideal, but this single line simplifies this sample SlotsProvider a lot.
            index = Array.FindIndex(_slotInteractors, i => i == interactor.Identifier);
            return index >= 0;
        }

        public bool PoseForInteractor(SnapInteractor interactor, out Pose pose)
        {
            if (TryFindIndexForInteractor(interactor, out int index))
            {
                pose = _slots[index].GetPose();
                return true;
            }
            pose = Pose.identity;
            return false;
        }

        private bool TryOccupySlot(int index)
        {
            if (IsSlotFree(index))
            {
                return true;
            }

            int freeSlot = FindBestSlotIndex(_slots[index].position, true);
            if (freeSlot < 0)
            {
                return false;
            }

            PushSlots(index, freeSlot);
            return true;
        }

        private bool IsSlotFree(int index)
        {
            return _slotInteractors[index] == 0;
        }

        private int FindBestSlotIndex(in Vector3 target, bool freeOnly = false)
        {
            int bestIndex = -1;
            float minDistance = float.PositiveInfinity;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (freeOnly && !IsSlotFree(i))
                {
                    continue;
                }

                float distance = (target - _slots[i].position).sqrMagnitude;
                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestIndex = i;
                }

            }
            return bestIndex;
        }

        private void PushSlots(int index, int freeSlot)
        {
            bool forwardDirection = index > freeSlot;
            for (int i = freeSlot; i != index; i = Next(i))
            {
                int nextIndex = Next(i);
                SwapSlot(i, nextIndex);
            }

            int Next(int value)
            {
                return value + (forwardDirection ? 1 : -1);
            }
        }

        private void SwapSlot(int index, int freeSlot)
        {
            (_slotInteractors[index], _slotInteractors[freeSlot]) = (_slotInteractors[freeSlot], _slotInteractors[index]);
        }

        #region Inject
        public void InjectAllSequentialSlotsProvider(List<Transform> slots)
        {
            InjectSlots(slots);
        }

        public void InjectSlots(List<Transform> slots)
        {
            _slots = slots;
        }
        #endregion
    }
}
