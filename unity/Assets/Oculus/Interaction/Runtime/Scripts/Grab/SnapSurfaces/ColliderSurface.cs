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

namespace Oculus.Interaction.HandGrab.SnapSurfaces
{
    public class ColliderSurface : MonoBehaviour, ISnapSurface
    {
        [SerializeField]
        private Collider _collider;

        protected virtual void Start()
        {
            Assert.IsNotNull(_collider);
        }

        private Vector3 NearestPointInSurface(Vector3 targetPosition)
        {
            if (_collider.bounds.Contains(targetPosition))
            {
                targetPosition = _collider.ClosestPointOnBounds(targetPosition);
            }
            return _collider.ClosestPoint(targetPosition);
        }

        public float CalculateBestPoseAtSurface(in Pose targetPose, in Pose snapPose, out Pose bestPose, in PoseMeasureParameters scoringModifier)
        {
            Vector3 surfacePoint = NearestPointInSurface(targetPose.position);

            float bestScore = 1f;
            if (scoringModifier.MaxDistance > 0)
            {
                bestScore = PoseUtils.PositionalSimilarity(surfacePoint, targetPose.position, scoringModifier.MaxDistance);
            }

            bestPose = new Pose(surfacePoint, targetPose.rotation);
            return bestScore;
        }

        public bool CalculateBestPoseAtSurface(Ray targetRay, in Pose recordedPose, out Pose bestPose)
        {
            if (_collider.Raycast(targetRay, out RaycastHit hit, Mathf.Infinity))
            {
                bestPose.position = hit.point;
                bestPose.rotation = recordedPose.rotation;
                return true;
            }
            bestPose = Pose.identity;
            return false;
        }


        public Pose MirrorPose(in Pose gripPose)
        {
            return gripPose;
        }

        public ISnapSurface CreateMirroredSurface(GameObject gameObject)
        {
            return CreateDuplicatedSurface(gameObject);
        }

        public ISnapSurface CreateDuplicatedSurface(GameObject gameObject)
        {
            ColliderSurface colliderSurface = gameObject.AddComponent<ColliderSurface>();
            colliderSurface.InjectAllColliderSurface(_collider);
            return colliderSurface;
        }

        #region inject

        public void InjectCollider(Collider collider)
        {
            _collider = collider;
        }

        public void InjectAllColliderSurface(Collider collider)
        {
            InjectCollider(collider);
        }

        #endregion

    }
}
