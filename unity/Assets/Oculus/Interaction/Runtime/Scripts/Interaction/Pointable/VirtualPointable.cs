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
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// This Virtual Pointable can broadcasts grab events that can be toggled
    /// from within the Unity inspector using the Grab Flag
    /// </summary>
    public class VirtualPointable : MonoBehaviour, IPointable
    {
        [SerializeField]
        private bool _grabFlag;

        public event Action<PointerEvent> WhenPointerEventRaised = delegate { };

        private UniqueIdentifier _id;
        private bool _currentlyGrabbing;

        protected virtual void Awake()
        {
            _id = UniqueIdentifier.Generate();
        }

        protected virtual void Update()
        {
            if (_currentlyGrabbing != _grabFlag)
            {
                _currentlyGrabbing = _grabFlag;
                if (_currentlyGrabbing)
                {
                    WhenPointerEventRaised(new PointerEvent(_id.ID, PointerEventType.Hover,
                        transform.GetPose()));
                    WhenPointerEventRaised(new PointerEvent(_id.ID, PointerEventType.Select,
                        transform.GetPose()));
                }
                else
                {
                    WhenPointerEventRaised(new PointerEvent(_id.ID, PointerEventType.Unselect,
                        transform.GetPose()));
                    WhenPointerEventRaised(new PointerEvent(_id.ID, PointerEventType.Unhover,
                        transform.GetPose()));
                }
                return;
            }

            if (_currentlyGrabbing)
            {
                WhenPointerEventRaised(new PointerEvent(_id.ID, PointerEventType.Move,
                    transform.GetPose()));
            }
        }

        public void SetGrabFlag(bool grabFlag)
        {
            _grabFlag = grabFlag;
        }

        protected virtual void OnDestroy()
        {
            UniqueIdentifier.Release(_id);
        }

    }
}
