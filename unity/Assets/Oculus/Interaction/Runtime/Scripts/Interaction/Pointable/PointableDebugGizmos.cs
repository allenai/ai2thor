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
    public class PointableDebugGizmos : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IPointable))]
        private MonoBehaviour _pointable;

        [SerializeField]
        private float _radius = 0.01f;

        [SerializeField]
        private Color _hoverColor = Color.blue;

        [SerializeField]
        private Color _selectColor = Color.green;

        [SerializeField]
        private bool _drawAxes = true;

        #region Properties

        public float Radius {
            get
            {
                return _radius;
            }
            set
            {
                _radius = value;
            }
        }

        public Color HoverColor
        {
            get
            {
                return _hoverColor;
            }
            set
            {
                _hoverColor = value;
            }
        }

        public Color SelectColor
        {
            get
            {
                return _selectColor;
            }
            set
            {
                _selectColor = value;
            }
        }

        public bool DrawAxes
        {
            get
            {
                return _drawAxes;
            }
            set
            {
                _drawAxes = value;
            }
        }

        #endregion Properties

        class PointData
        {
            public Pose Pose { get; set; }
            public bool Selecting { get; set; }
        }

        private Dictionary<int, PointData> _points;

        private IPointable Pointable;

        protected bool _started = false;

        protected virtual void Awake()
        {
            Pointable = _pointable as IPointable;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(Pointable);
            _points = new Dictionary<int, PointData>();
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
                    _points.Add(evt.Identifier,
                        new PointData() {Pose = evt.Pose, Selecting = false});
                    break;
                case PointerEventType.Select:
                    _points[evt.Identifier].Selecting = true;
                    break;
                case PointerEventType.Move:
                    _points[evt.Identifier].Pose = evt.Pose;
                    break;
                case PointerEventType.Unselect:
                    if (_points.ContainsKey(evt.Identifier))
                    {
                        _points[evt.Identifier].Selecting = false;
                    }
                    break;
                case PointerEventType.Unhover:
                case PointerEventType.Cancel:
                    _points.Remove(evt.Identifier);
                    break;
            }
        }

        protected virtual void LateUpdate()
        {
            foreach (PointData pointData in _points.Values)
            {
                DebugGizmos.LineWidth = _radius;
                DebugGizmos.Color = pointData.Selecting ? _selectColor : _hoverColor;
                DebugGizmos.DrawPoint(pointData.Pose.position);
                if (_drawAxes)
                {
                    DebugGizmos.LineWidth = _radius / 2f;
                    DebugGizmos.DrawAxis(pointData.Pose.position, pointData.Pose.rotation, _radius * 2);
                }
            }
        }

        #region Inject

        public void InjectAllPointableDebugGizmos(IPointable pointable)
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
