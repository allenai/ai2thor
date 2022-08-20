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

using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    public class CylinderProximityField : MonoBehaviour,
        IProximityField, ICurvedPlane
    {
        [SerializeField]
        private Cylinder _cylinder;

        [SerializeField]
        private float _rotation = 0f;

        [SerializeField, Range(0f, 360f)]
        private float _arcDegrees = 360;

        [SerializeField]
        private float _bottom = -1f;

        [SerializeField]
        private float _top = 1f;

        [Tooltip("Providing an ICurvedPlane here will " +
            "override all other local properties")]
        [SerializeField, Optional, Interface(typeof(ICurvedPlane))]
        private MonoBehaviour _curvedPlane;

        private ICurvedPlane CurvedPlane;

        public Cylinder Cylinder => _cylinder;

        public float ArcDegrees
        {
            get => _arcDegrees;
            set => _arcDegrees = value;
        }
        public float Rotation
        {
            get => _rotation;
            set => _rotation = value;
        }
        public float Bottom
        {
            get => _bottom;
            set => _bottom = value;
        }
        public float Top
        {
            get => _top;
            set => _top = value;
        }

        protected virtual void Awake()
        {
            CurvedPlane = _curvedPlane != null ?
                          _curvedPlane as ICurvedPlane :
                          this;
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(CurvedPlane);
            Assert.IsNotNull(CurvedPlane.Cylinder);
        }

        public Vector3 ComputeClosestPoint(Vector3 point)
        {
            return ComputeClosestPoint(CurvedPlane, point);
        }

        private static Vector3 ComputeClosestPoint(ICurvedPlane curvedPlane, Vector3 point)
        {
            Vector3 localPoint = curvedPlane.Cylinder.transform.InverseTransformPoint(point);

            if (curvedPlane.Top > curvedPlane.Bottom)
            {
                localPoint.y = Mathf.Clamp(localPoint.y, curvedPlane.Bottom, curvedPlane.Top);
            }

            if (curvedPlane.ArcDegrees < 360)
            {
                float angle = Mathf.Atan2(localPoint.x, localPoint.z) * Mathf.Rad2Deg % 360;
                float rotation = curvedPlane.Rotation % 360;

                if (angle > rotation + 180)
                {
                    angle -= 360;
                }
                else if (angle < rotation - 180)
                {
                    angle += 360;
                }

                angle = Mathf.Clamp(angle, rotation - curvedPlane.ArcDegrees / 2f,
                                           rotation + curvedPlane.ArcDegrees / 2f);

                localPoint.x = Mathf.Sin(angle * Mathf.Deg2Rad) * curvedPlane.Cylinder.Radius;
                localPoint.z = Mathf.Cos(angle * Mathf.Deg2Rad) * curvedPlane.Cylinder.Radius;
            }
            else
            {
                Vector3 nearestPointOnCenterAxis = new Vector3(0f, localPoint.y, 0f);
                float distanceFromCenterAxis = Vector3.Distance(localPoint,
                                                                nearestPointOnCenterAxis);
                localPoint = Vector3.MoveTowards(localPoint,
                                                 nearestPointOnCenterAxis,
                                                 distanceFromCenterAxis - curvedPlane.Cylinder.Radius);
            }

            return curvedPlane.Cylinder.transform.TransformPoint(localPoint);
        }

        #region Inject

        public void InjectAllCylinderProximityField(Cylinder cylinder)
        {
            InjectCylinder(cylinder);
        }

        public void InjectCylinder(Cylinder cylinder)
        {
            _cylinder = cylinder;
        }

        public void InjectOptionalCurvedPlane(ICurvedPlane curvedPlane)
        {
            _curvedPlane = curvedPlane as MonoBehaviour;
            CurvedPlane = curvedPlane;
        }

        #endregion
    }
}
