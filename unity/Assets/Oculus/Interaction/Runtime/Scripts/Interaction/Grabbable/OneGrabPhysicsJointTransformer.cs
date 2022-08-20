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
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    /// <summary>
    /// A Transformer that moves the target using Physics joints
    /// Updates an internal kinematic rigidbody attached with a joint
    /// to the grabbable. It also supports adding custom joints for grabbing
    /// to improve stability on some constrained grabs.
    /// </summary>
    public class OneGrabPhysicsJointTransformer : MonoBehaviour, ITransformer
    {
        /// <summary>
        /// Optional. The custom joint to use when grabbing the grabbable.
        /// Set this field to a Joint in a disabled GameObject, it will be duplicated every time
        /// a new Grabber grabs this object, and the anchoring will be adjusted automatically.
        /// If not set a FixedJoint will be used.
        /// If the grabbable is already attached to something with a joint, using preprocessing here is not recommended.
        /// </summary>
        [SerializeField, Optional]
        [Tooltip("Specify a custom joint to use when grabbing; should be disabled.")]
        private ConfigurableJoint _customJoint;

        /// <summary>
        /// Indicates if the grabbing rigidbody should be kinematic or not.
        /// Non-kinematic grabs can be interesting for simulating weight or
        /// more robust interactions.
        /// </summary>
        [SerializeField]
        [Tooltip("Indicates if the grabbing rigidbody should be kinematic or not.")]
        private bool _isKinematicGrab = true;

        public bool IsKinematicGrab
        {
            get
            {
                return _isKinematicGrab;
            }
            set
            {
                _isKinematicGrab = value;
            }
        }

        private Joint _joint;
        private Rigidbody _grabbingRigidbody;
        private static List<Rigidbody> _cachedGrabbingRigidbodies = new List<Rigidbody>();

        private IGrabbable _grabbable;

        private Vector3 _targetPosition;
        private Quaternion _targetRotation;

        /// <summary>
        /// Ensure the custom joint is set to a disabled GameObject, as it will be used
        /// just to read the values and duplicate them onto a new dynamically generated joint.
        /// </summary>
        private void OnValidate()
        {
            if (_customJoint != null)
            {
                if (_customJoint.gameObject == this.gameObject)
                {
                    Debug.LogWarning($"The OptionalCustomJoint must be placed in a disabled child GameObject. Moving it.", this.gameObject);
                    GameObject holder = CreateJointHolder();
                    _customJoint = CloneJoint(_customJoint, holder);
                }
                else
                {
                    _customJoint.gameObject.SetActive(false);
                }
            }
        }

        public void Initialize(IGrabbable grabbable)
        {
            _grabbable = grabbable;
        }

        public void BeginTransform()
        {
            Vector3 grabPointPosition = _grabbable.GrabPoints[0].position;
            Quaternion grabPointRotation = _grabbable.GrabPoints[0].rotation;

            _grabbingRigidbody = GetGrabRigidbody();
            _grabbingRigidbody.transform.SetPositionAndRotation(grabPointPosition, grabPointRotation);
            _joint = AddJoint(_grabbingRigidbody);
        }

        public void UpdateTransform()
        {
            Pose grabPoint = _grabbable.GrabPoints[0];
            _targetPosition = grabPoint.position;
            _targetRotation = grabPoint.rotation;

            if (_isKinematicGrab)
            {
                _grabbingRigidbody.transform.SetPositionAndRotation(_targetPosition, _targetRotation);
            }
        }

        private void FixedUpdate()
        {
            if (!_isKinematicGrab && _grabbingRigidbody != null)
            {
                _grabbingRigidbody.MovePosition(_targetPosition);
                _grabbingRigidbody.MoveRotation(_targetRotation);
            }
        }

        public void EndTransform()
        {
            RemoveCurrentJoint();
            RemoveCurrentGrabRigidbody();
        }

        /// <summary>
        /// Attaches the grabbable to a rigidbody using a joint.
        /// It will use the desired joint configuration set in _customJoint or
        /// the default one.
        /// <paramref name="rigidbody">The rigidbody to be attached to this grabbable using a joint</paramref>
        /// </summary>
        /// <returns>The generated joint</returns>
        private Joint AddJoint(Rigidbody rigidbody)
        {
            RemoveCurrentJoint();

            Joint joint;
            if (_customJoint != null)
            {
                joint = CloneJoint(_customJoint, this.gameObject);
            }
            else
            {
                joint = CreateDefaultJoint();
            }

            joint.connectedBody = rigidbody;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = joint.transform.InverseTransformPoint(rigidbody.transform.position);
            joint.connectedAnchor = Vector3.zero;
            return joint;
        }

        /// <summary>
        /// Destroys the current joint.
        /// Ideally it should just disable it, but Unity
        /// does not allow it.
        /// </summary>
        private void RemoveCurrentJoint()
        {
            if (_joint != null)
            {
                Destroy(_joint);
            }
        }

        /// <summary>
        /// Generates (or retrieves from a cache) a rigidbody to be used
        /// for connecting to grabbable via joints.
        /// </summary>
        /// <returns>The generated rigidbody.</returns>
        private Rigidbody GetGrabRigidbody()
        {
            Rigidbody rigidbody = _cachedGrabbingRigidbodies.Find(rb => !rb.gameObject.activeSelf);
            if (rigidbody == null)
            {
                rigidbody = CreateRigidBody();
                _cachedGrabbingRigidbodies.Add(rigidbody);
            }
            rigidbody.gameObject.SetActive(true);
            rigidbody.isKinematic = _isKinematicGrab;
            return rigidbody;
        }

        /// <summary>
        /// Disables the currently grabbing rigidbody so it
        /// can be recycled for future grabs.
        /// </summary>
        private void RemoveCurrentGrabRigidbody()
        {
            if (_grabbingRigidbody != null)
            {
                _grabbingRigidbody.gameObject.SetActive(false);
                _grabbingRigidbody.isKinematic = true;
                _grabbingRigidbody = null;
            }
        }

        /// <summary>
        /// Creates a new proxy RigidBody to anchor the grabbables.
        /// </summary>
        /// <returns>A new kinematic Rigidbody</returns>
        private Rigidbody CreateRigidBody()
        {
            GameObject go = new GameObject();
            go.name = "Proxy RigidBody";
            go.SetActive(false);
            go.transform.SetParent(null);
            Rigidbody body = go.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = false;
            return body;
        }

        /// <summary>
        /// When no custom joint is specified, this joint is used.
        /// An unbreakable Fixed joint without Preprocessing.
        /// </summary>
        /// <returns>The generated FixedJoint</returns>
        private Joint CreateDefaultJoint()
        {
            Joint joint = this.gameObject.AddComponent<FixedJoint>();
            joint.breakForce = Mathf.Infinity;
            joint.enablePreprocessing = false;
            return joint;
        }

        /// <summary>
        /// Creates a disabled GameObject for holding the data of the desired custom joint.
        /// </summary>
        /// <returns>A children GameObject for holding joint data.</returns>
        protected GameObject CreateJointHolder()
        {
            GameObject savedJointHolder = new GameObject();
            savedJointHolder.name = "Saved Joint";
            savedJointHolder.SetActive(false);
            savedJointHolder.transform.SetParent(this.transform);
            Rigidbody body = savedJointHolder.AddComponent<Rigidbody>();
            body.isKinematic = true;
            return savedJointHolder;
        }

        /// <summary>
        /// Copy all the properties of a ConfigurableJoint onto a new one.
        /// </summary>
        /// <param name="joint">The ConfigurableJoint to be copied.</param>
        /// <param name="destination">The GameObject that will contain the new ConfigurableJoint.</param>
        /// <returns>The created Joint.</returns>
        private static ConfigurableJoint CloneJoint(ConfigurableJoint joint, GameObject destination)
        {
            ConfigurableJoint clonedJoint = destination.gameObject.AddComponent<ConfigurableJoint>();

            //From Joint
            clonedJoint.connectedBody = joint.connectedBody;
            clonedJoint.axis = joint.axis;
            clonedJoint.anchor = joint.anchor;
            clonedJoint.connectedAnchor = joint.connectedAnchor;
            clonedJoint.autoConfigureConnectedAnchor = joint.autoConfigureConnectedAnchor;
            clonedJoint.breakForce = joint.breakForce;
            clonedJoint.breakTorque = joint.breakTorque;
            clonedJoint.enableCollision = joint.enableCollision;
            clonedJoint.enablePreprocessing = joint.enablePreprocessing;
            clonedJoint.massScale = joint.massScale;
            clonedJoint.connectedMassScale = joint.connectedMassScale;
            ///From ConfigurableJoint
            clonedJoint.projectionAngle = joint.projectionAngle;
            clonedJoint.projectionDistance = joint.projectionDistance;
            clonedJoint.projectionMode = joint.projectionMode;
            clonedJoint.slerpDrive = joint.slerpDrive;
            clonedJoint.angularYZDrive = joint.angularYZDrive;
            clonedJoint.angularXDrive = joint.angularXDrive;
            clonedJoint.rotationDriveMode = joint.rotationDriveMode;
            clonedJoint.targetAngularVelocity = joint.targetAngularVelocity;
            clonedJoint.targetRotation = joint.targetRotation;
            clonedJoint.zDrive = joint.zDrive;
            clonedJoint.yDrive = joint.yDrive;
            clonedJoint.xDrive = joint.xDrive;
            clonedJoint.targetVelocity = joint.targetVelocity;
            clonedJoint.targetPosition = joint.targetPosition;
            clonedJoint.angularZLimit = joint.angularZLimit;
            clonedJoint.angularYLimit = joint.angularYLimit;
            clonedJoint.highAngularXLimit = joint.highAngularXLimit;
            clonedJoint.lowAngularXLimit = joint.lowAngularXLimit;
            clonedJoint.linearLimit = joint.linearLimit;
            clonedJoint.angularYZLimitSpring = joint.angularYZLimitSpring;
            clonedJoint.angularXLimitSpring = joint.angularXLimitSpring;
            clonedJoint.linearLimitSpring = joint.linearLimitSpring;
            clonedJoint.angularZMotion = joint.angularZMotion;
            clonedJoint.angularYMotion = joint.angularYMotion;
            clonedJoint.angularXMotion = joint.angularXMotion;
            clonedJoint.zMotion = joint.zMotion;
            clonedJoint.yMotion = joint.yMotion;
            clonedJoint.xMotion = joint.xMotion;
            clonedJoint.secondaryAxis = joint.secondaryAxis;
            clonedJoint.configuredInWorldSpace = joint.configuredInWorldSpace;
            clonedJoint.swapBodies = joint.swapBodies;

            return clonedJoint;
        }

        #region Inject

        public void InjectOptionalCustomJoint(ConfigurableJoint customJoint)
        {
            _customJoint = customJoint;
        }

        #endregion
    }
}
