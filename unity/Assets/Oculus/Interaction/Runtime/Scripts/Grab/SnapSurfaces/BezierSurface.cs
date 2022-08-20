/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction.HandGrab.SnapSurfaces
{
    [Serializable]
    public class BezierSurface : MonoBehaviour, ISnapSurface
    {
        [SerializeField]
        private List<BezierControlPoint> _controlPoints = new List<BezierControlPoint>();

        public List<BezierControlPoint> ControlPoints => _controlPoints;

        private const float MAX_PLANE_DOT = 0.95f;
        private const float MAX_RAY_DISTANCE = 2f;

        public float CalculateBestPoseAtSurface(in Pose targetPose, in Pose reference, out Pose bestPose,
            in PoseMeasureParameters scoringModifier)
        {
            Pose relativePose = this.transform.GetPose();
            Pose testPose = Pose.identity;
            Pose smallestRotationPose = Pose.identity;
            bestPose = targetPose;
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < _controlPoints.Count; i++)
            {
                BezierControlPoint currentControlPoint = _controlPoints[i];
                BezierControlPoint nextControlPoint = _controlPoints[(i + 1) % _controlPoints.Count];

                if (!currentControlPoint.disconnected
                    && nextControlPoint.disconnected)
                {
                    continue;
                }

                float score;
                if ((currentControlPoint.disconnected && nextControlPoint.disconnected)
                    || _controlPoints.Count == 1)
                {
                    Pose worldPose = currentControlPoint.WorldSpacePose(relativePose);
                    testPose.CopyFrom(worldPose);
                    score = PoseUtils.Similarity(targetPose, testPose, scoringModifier);
                }
                else
                {
                    Pose start = currentControlPoint.WorldSpacePose(relativePose);
                    Pose end = nextControlPoint.WorldSpacePose(relativePose);
                    Vector3 tangent = start.position + relativePose.rotation * currentControlPoint.tangentPoint;

                    NearestPointInTriangle(targetPose.position, start.position, tangent, end.position, out float positionT);
                    float rotationT = ProgressForRotation(targetPose.rotation, start.rotation, end.rotation);

                    score = SnapSurfaceHelper.CalculateBestPoseAtSurface(targetPose, reference, out testPose, scoringModifier,
                        (in Pose target, in Pose original) =>
                        {
                            Pose result;
                            result.position = EvaluateBezier(start.position, tangent, end.position, positionT);
                            result.rotation = Quaternion.Slerp(start.rotation, end.rotation, positionT);
                            return result;

                        },
                        (in Pose target, in Pose original) =>
                        {
                            Pose result;
                            result.position = EvaluateBezier(start.position, tangent, end.position, rotationT);
                            result.rotation = Quaternion.Slerp(start.rotation, end.rotation, rotationT);
                            return result;
                        });
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPose.CopyFrom(testPose);
                }
            }
            return bestScore;
        }

        public bool CalculateBestPoseAtSurface(Ray targetRay, in Pose reference, out Pose bestPose)
        {
            Pose relativePose = this.transform.GetPose();
            Pose testPose = Pose.identity;
            Pose targetPose = Pose.identity;
            bestPose = reference;
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < _controlPoints.Count; i++)
            {
                BezierControlPoint currentControlPoint = _controlPoints[i];
                BezierControlPoint nextControlPoint = _controlPoints[(i + 1) % _controlPoints.Count];

                if (!currentControlPoint.disconnected
                    && nextControlPoint.disconnected)
                {
                    continue;
                }

                if ((currentControlPoint.disconnected && nextControlPoint.disconnected)
                    || _controlPoints.Count == 1)
                {
                    Pose worldPose = currentControlPoint.WorldSpacePose(relativePose);
                    Plane plane = new Plane(-targetRay.direction, worldPose.position);
                    if (!plane.Raycast(targetRay, out float enter))
                    {
                        continue;
                    }
                    targetPose.position = targetRay.GetPoint(enter);
                    testPose.CopyFrom(worldPose);
                }
                else
                {
                    Pose start = currentControlPoint.WorldSpacePose(relativePose);
                    Pose end = nextControlPoint.WorldSpacePose(relativePose);
                    Vector3 tangent = start.position + relativePose.rotation * currentControlPoint.tangentPoint;
                    Plane plane = GenerateRaycastPlane(start.position, tangent, end.position, -targetRay.direction);
                    if (!plane.Raycast(targetRay, out float enter))
                    {
                        continue;
                    }
                    targetPose.position = targetRay.GetPoint(enter);
                    NearestPointInTriangle(targetPose.position, start.position, tangent, end.position, out float t);
                    testPose.position = EvaluateBezier(start.position, tangent, end.position, t);
                    testPose.rotation = Quaternion.Slerp(start.rotation, end.rotation, t);
                }

                float score = PoseUtils.PositionalSimilarity(targetPose.position, testPose.position, MAX_RAY_DISTANCE);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPose.CopyFrom(testPose);
                }
            }
            return bestScore != float.NegativeInfinity;
        }

        private Plane GenerateRaycastPlane(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 fallbackDir)
        {
            Vector3 line0 = (p1 - p0).normalized;
            Vector3 line1 = (p2 - p0).normalized;

            Plane plane;
            if (Mathf.Abs(Vector3.Dot(line0, line1)) > MAX_PLANE_DOT)
            {
                plane = new Plane(fallbackDir, (p0 + p2 + p1) / 3f);
            }
            else
            {
                plane = new Plane(p0, p1, p2);
            }
            return plane;
        }

        private float ProgressForRotation(Quaternion targetRotation, Quaternion from, Quaternion to)
        {
            Vector3 targetForward = targetRotation * Vector3.forward;
            Vector3 fromForward = from * Vector3.forward;
            Vector3 toForward = to * Vector3.forward;
            Vector3 axis = Vector3.Cross(fromForward, toForward).normalized;

            float angleFrom = Vector3.SignedAngle(targetForward, fromForward, axis);
            float angleTo = Vector3.SignedAngle(targetForward, toForward, axis);

            if (angleFrom < 0 && angleTo < 0)
            {
                return 1f;
            }
            if (angleFrom > 0 && angleTo > 0)
            {
                return 0f;
            }
            return Mathf.Abs(angleFrom) / Vector3.Angle(fromForward, toForward);
        }

        private Vector3 NearestPointInTriangle(Vector3 point, Vector3 p0, Vector3 p1, Vector3 p2, out float t)
        {
            Vector3 centroid = (p0 + p1 + p2) / 3f;

            Vector3 pointInMedian0 = NearestPointToSegment(point, p0, centroid, out float t0);
            Vector3 pointInMedian1 = NearestPointToSegment(point, centroid, p2, out float t1);

            float median0 = Vector3.Distance(p0, centroid);
            float median2 = Vector3.Distance(p2, centroid);
            float alpha = median2 / (median0 + median2);

            float distance0 = (pointInMedian0 - point).sqrMagnitude;
            float distance1 = (pointInMedian1 - point).sqrMagnitude;
            if (distance0 < distance1)
            {
                t = t0 * alpha;
                return pointInMedian0;
            }
            else
            {
                t = alpha + t1 * (1f - alpha);
                return pointInMedian1;
            }
        }

        private Vector3 NearestPointToSegment(Vector3 point, Vector3 start, Vector3 end, out float progress)
        {
            Vector3 segment = end - start;
            Vector3 projection = Vector3.Project(point - start, segment.normalized);
            Vector3 pointInSegment;
            if (Vector3.Dot(segment, projection) <= 0)
            {
                pointInSegment = start;
                progress = 0;
            }
            else if (projection.sqrMagnitude >= segment.sqrMagnitude)
            {
                pointInSegment = end;
                progress = 1;
            }
            else
            {
                pointInSegment = start + projection;
                progress = projection.magnitude / segment.magnitude;
            }

            return pointInSegment;
        }

        public ISnapSurface CreateDuplicatedSurface(GameObject gameObject)
        {
            BezierSurface surface = gameObject.AddComponent<BezierSurface>();
            surface._controlPoints = new List<BezierControlPoint>(_controlPoints);
            return surface;
        }

        public ISnapSurface CreateMirroredSurface(GameObject gameObject)
        {
            BezierSurface surface = gameObject.AddComponent<BezierSurface>();
            surface._controlPoints = new List<BezierControlPoint>(_controlPoints);
            return surface;
        }

        public Pose MirrorPose(in Pose gripPose)
        {
            return gripPose;
        }

        public static Vector3 EvaluateBezier(Vector3 start, Vector3 middle, Vector3 end, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return (oneMinusT * oneMinusT * start)
                + (2f * oneMinusT * t * middle)
                + (t * t * end);
        }
    }

    [Serializable]
    public struct BezierControlPoint
    {
        public Pose pose;
        public Vector3 tangentPoint;
        public bool disconnected;

        public Pose WorldSpacePose(in Pose relativePose)
        {
            return PoseUtils.Multiply(relativePose, pose);
        }

        public static readonly BezierControlPoint DEFAULT = new BezierControlPoint()
        {
            pose = Pose.identity,
            tangentPoint = new Vector3(0.1f, 0f, 0.1f)
        };
    }
}
