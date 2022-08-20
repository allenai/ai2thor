/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using UnityEngine;

namespace Oculus.Interaction.Throw
{
    /// <summary>
    /// Based on modified LeapMotion's PhysicsUtility class. See associated license.
    /// </summary>
    public class VelocityCalculatorUtilMethods
    {
        public static Vector3 ToLinearVelocity(Vector3 startPosition, Vector3 destinationPosition,
           float deltaTime)
        {
            return Mathf.Abs(deltaTime) > Mathf.Epsilon ?
              (destinationPosition - startPosition) / deltaTime :
              Vector3.zero;
        }

        public static Vector3 ToAngularVelocity(Quaternion startQuaternion,
          Quaternion destinationQuaternion, float deltaTime)
        {
            if (startQuaternion.Equals(destinationQuaternion) || deltaTime == 0f)
            {
                return Vector3.zero;
            }
            return DeltaRotationToAngularVelocity(
              destinationQuaternion * Quaternion.Inverse(startQuaternion),
              deltaTime);
        }

        public static Quaternion AngularVelocityToQuat(Vector3 angularVelocity)
        {
            float speed = angularVelocity.magnitude;
            return Quaternion.AngleAxis(speed, angularVelocity.normalized);
        }

        public static (float, Vector3) QuatToAngleAxis(Quaternion inputQuat)
        {
            Vector3 axis;
            float angle;

            inputQuat.ToAngleAxis(out angle, out axis);

            if (float.IsInfinity(axis.x))
            {
                axis = Vector3.zero;
                angle = 0;
            }

            if (angle > 180)
            {
                angle -= 360.0f;
            }

            return (angle, axis);
        }

        public static Vector3 QuatToAngularVeloc(Quaternion inputQuat)
        {
            float angle;
            Vector3 axis = Vector3.zero;
            (angle, axis) =
              VelocityCalculatorUtilMethods.QuatToAngleAxis(inputQuat);
            return axis * angle;
        }

        public static Vector3 DeltaRotationToAngularVelocity(Quaternion deltaRotation,
          float deltaTime)
        {
            Vector3 deltaAxis;
            float deltaAngle;
            (deltaAngle, deltaAxis) = QuatToAngleAxis(deltaRotation);

            return Mathf.Abs(deltaTime) > Mathf.Epsilon ?
              deltaAxis * deltaAngle * Mathf.Deg2Rad / deltaTime :
              Vector3.zero;
        }

        public static (Vector3, Vector3) GetVelocityAndAngularVelocity(TransformSample startSample,
          TransformSample endSample, float duration)
        {
            return (ToLinearVelocity(startSample.Position,
              endSample.Position, duration),
              ToAngularVelocity(startSample.Rotation,
              endSample.Rotation, duration));
        }
    }
}
