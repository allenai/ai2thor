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

namespace Oculus.Interaction
{
    public class Grabbable : PointableElement, IGrabbable
    {
        [SerializeField, Interface(typeof(ITransformer)), Optional]
        private MonoBehaviour _oneGrabTransformer = null;

        [SerializeField, Interface(typeof(ITransformer)), Optional]
        private MonoBehaviour _twoGrabTransformer = null;

        [SerializeField]
        private int _maxGrabPoints = -1;

        public int MaxGrabPoints
        {
            get
            {
                return _maxGrabPoints;
            }
            set
            {
                _maxGrabPoints = value;
            }
        }

        public Transform Transform => transform;
        public List<Pose> GrabPoints => _selectingPoints;

        private ITransformer _activeTransformer = null;
        private ITransformer OneGrabTransformer;
        private ITransformer TwoGrabTransformer;

        protected override void Awake()
        {
            base.Awake();
            OneGrabTransformer = _oneGrabTransformer as ITransformer;
            TwoGrabTransformer = _twoGrabTransformer as ITransformer;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());

            if (OneGrabTransformer != null)
            {
                Assert.IsNotNull(OneGrabTransformer);
                OneGrabTransformer.Initialize(this);
            }

            if (TwoGrabTransformer != null)
            {
                Assert.IsNotNull(TwoGrabTransformer);
                TwoGrabTransformer.Initialize(this);
            }

            // Create a default if no transformers assigned
            if (OneGrabTransformer == null &&
                TwoGrabTransformer == null)
            {
                OneGrabFreeTransformer defaultTransformer = gameObject.AddComponent<OneGrabFreeTransformer>();
                _oneGrabTransformer = defaultTransformer;
                OneGrabTransformer = defaultTransformer;
                OneGrabTransformer.Initialize(this);
            }

            this.EndStart(ref _started);
        }

        public override void ProcessPointerEvent(PointerEvent evt)
        {
            switch (evt.Type)
            {
                case PointerEventType.Select:
                    EndTransform();
                    break;
                case PointerEventType.Unselect:
                    EndTransform();
                    break;
                case PointerEventType.Cancel:
                    EndTransform();
                    break;
            }

            base.ProcessPointerEvent(evt);

            switch (evt.Type)
            {
                case PointerEventType.Select:
                    BeginTransform();
                    break;
                case PointerEventType.Unselect:
                    BeginTransform();
                    break;
                case PointerEventType.Move:
                    UpdateTransform();
                    break;
            }
        }

        // Whenever we change the number of grab points, we save the
        // current transform data
        private void BeginTransform()
        {
            // End the transform on any existing transformer before we
            // begin the new one
            EndTransform();

            int useGrabPoints = _selectingPoints.Count;
            if (_maxGrabPoints != -1)
            {
                useGrabPoints = Mathf.Min(useGrabPoints, _maxGrabPoints);
            }

            switch (useGrabPoints)
            {
                case 1:
                    _activeTransformer = OneGrabTransformer;
                    break;
                case 2:
                    _activeTransformer = TwoGrabTransformer;
                    break;
                default:
                    _activeTransformer = null;
                    break;
            }

            if (_activeTransformer == null)
            {
                return;
            }

            _activeTransformer.BeginTransform();
        }

        private void UpdateTransform()
        {
            if (_activeTransformer == null)
            {
                return;
            }

            _activeTransformer.UpdateTransform();
        }

        private void EndTransform()
        {
            if (_activeTransformer == null)
            {
                return;
            }
            _activeTransformer.EndTransform();
            _activeTransformer = null;
        }

        #region Inject

        public void InjectOptionalOneGrabTransformer(ITransformer transformer)
        {
            _oneGrabTransformer = transformer as MonoBehaviour;
            OneGrabTransformer = transformer;
        }

        public void InjectOptionalTwoGrabTransformer(ITransformer transformer)
        {
            _twoGrabTransformer = transformer as MonoBehaviour;
            TwoGrabTransformer = transformer;
        }

        #endregion
    }
}
