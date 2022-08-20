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
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    /// <summary>
    /// This componenet makes it possible to connect IPointables in the
    /// inspector to Unity Events that are broadcast on IPointable events.
    /// </summary>
    public class PointableUnityEventWrapper : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IPointable))]
        private MonoBehaviour _pointable;
        private IPointable Pointable;

        private HashSet<int> _pointers;

        [SerializeField]
        private UnityEvent _whenRelease;

        [SerializeField]
        private UnityEvent _whenHover;
        [SerializeField]
        private UnityEvent _whenUnhover;
        [SerializeField]
        private UnityEvent _whenSelect;
        [SerializeField]
        private UnityEvent _whenUnselect;
        [SerializeField]
        private UnityEvent _whenMove;
        [SerializeField]
        private UnityEvent _whenCancel;

        public UnityEvent WhenRelease => _whenRelease;

        public UnityEvent WhenHover => _whenHover;
        public UnityEvent WhenUnhover => _whenUnhover;
        public UnityEvent WhenSelect => _whenSelect;
        public UnityEvent WhenUnselect => _whenUnselect;
        public UnityEvent WhenMove => _whenMove;
        public UnityEvent WhenCancel => _whenCancel;

        protected bool _started = false;

        protected virtual void Awake()
        {
            Pointable = _pointable as IPointable;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(Pointable);
            _pointers = new HashSet<int>();
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Pointable.WhenPointerEventRaised += HandlePointerEventRaised;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Pointable.WhenPointerEventRaised -= HandlePointerEventRaised;
            }
        }

        private void HandlePointerEventRaised(PointerEvent evt)
        {
            switch (evt.Type)
            {
                case PointerEventType.Hover:
                    _whenHover.Invoke();
                    _pointers.Add(evt.Identifier);
                    break;
                case PointerEventType.Unhover:
                    _whenUnhover.Invoke();
                    _pointers.Remove(evt.Identifier);
                    break;
                case PointerEventType.Select:
                    _whenSelect.Invoke();
                    break;
                case PointerEventType.Unselect:
                    if (_pointers.Contains(evt.Identifier))
                    {
                        _whenRelease.Invoke();
                    }
                    _whenUnselect.Invoke();
                    break;
                case PointerEventType.Move:
                    _whenMove.Invoke();
                    break;
                case PointerEventType.Cancel:
                    _whenCancel.Invoke();
                    _pointers.Remove(evt.Identifier);
                    break;
            }
        }

        #region Inject

        public void InjectAllPointableUnityEventWrapper(IPointable pointable)
        {
            InjectPointable(pointable);
        }

        public void InjectPointable(IPointable pointable)
        {
            _pointable = pointable as MonoBehaviour;
            Pointable = pointable;
        }

        #endregion
    }
}
