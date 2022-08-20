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
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    public abstract class PointerInteractable<TInteractor, TInteractable> : Interactable<TInteractor, TInteractable>,
        IPointable
        where TInteractor : Interactor<TInteractor, TInteractable>
        where TInteractable : PointerInteractable<TInteractor, TInteractable>
    {
        [SerializeField, Interface(typeof(IPointableElement)), Optional]
        private MonoBehaviour _pointableElement;

        public IPointableElement PointableElement { get; private set; }

        public event Action<PointerEvent> WhenPointerEventRaised = delegate { };

        protected bool _started = false;

        public void PublishPointerEvent(PointerEvent evt)
        {
            WhenPointerEventRaised(evt);
        }

        protected override void Awake()
        {
            base.Awake();
            if (_pointableElement != null)
            {
                PointableElement = _pointableElement as IPointableElement;
            }
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            if (_pointableElement != null)
            {
                Assert.IsNotNull(PointableElement);
            }
            this.EndStart(ref _started);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_started)
            {
                if (PointableElement != null)
                {
                    WhenPointerEventRaised += PointableElement.ProcessPointerEvent;
                }
            }
        }

        protected override void OnDisable()
        {
            if (_started)
            {
                if (PointableElement != null)
                {
                    WhenPointerEventRaised -= PointableElement.ProcessPointerEvent;
                }
            }
            base.OnDisable();
        }

        #region Inject

        public void InjectOptionalPointableElement(IPointableElement pointableElement)
        {
            PointableElement = pointableElement;
            _pointableElement = pointableElement as MonoBehaviour;
        }

        #endregion
    }
}
