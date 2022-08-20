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

namespace Oculus.Interaction.HandGrab
{
    [System.Serializable]
    public struct DistantPointDetectorFrustums
    {
        /// <summary>
        /// The main frustum, items inside this frustum will be selected.
        /// </summary>
        [SerializeField]
        private ConicalFrustum _selectionFrustum;
        /// <summary>
        /// Selected items within this optional frustum will keep being selected.
        /// It should be wider than the selection frustum.
        /// </summary>
        [SerializeField, Optional]
        private ConicalFrustum _deselectionFrustum;
        /// <summary>
        /// When provided, items need to be also inside this frustum as well as the
        /// selection one to be selected.
        /// e.g. Selection frustum coming out from the hand, and aid frustum from the
        /// head so items need to be pointed at with the gaze and hand.
        /// </summary>
        [SerializeField, Optional]
        private ConicalFrustum _aidFrustum;
        /// <summary>
        /// Blends the scores between the Selection and Aid frustums.
        /// At 0 items centered in the selection frustum are given preference, at one
        /// items centered at the aid frustum are given preference.
        /// </summary>
        [SerializeField]
        [Range(0f, 1f)]
        private float _aidBlending;

        public ConicalFrustum SelectionFrustum => _selectionFrustum;
        public ConicalFrustum DeselectionFrustum => _deselectionFrustum;
        public ConicalFrustum AidFrustum => _aidFrustum;
        public float AidBlending => _aidBlending;

        public DistantPointDetectorFrustums(ConicalFrustum selection,
            ConicalFrustum deselection, ConicalFrustum aid, float blend)
        {
            _selectionFrustum = selection;
            _deselectionFrustum = deselection;
            _aidFrustum = aid;
            _aidBlending = blend;
        }
    }

    /// <summary>
    /// This class contains the logic for finding the best candidate among a series of colliders.
    /// It uses up to three conical frustums from DistantPointDetectorFrustums for selection and deselection.
    /// </summary>
    public class DistantPointDetector
    {
        private DistantPointDetectorFrustums _frustums;

        public DistantPointDetector(DistantPointDetectorFrustums frustums)
        {
            _frustums = frustums;
        }

        public bool ComputeIsPointing(Collider[] colliders, bool isSelecting, out float bestScore, out Vector3 bestHitPoint)
        {
            ConicalFrustum _searchFrustrum = (isSelecting || _frustums.DeselectionFrustum == null) ?
                _frustums.SelectionFrustum : _frustums.DeselectionFrustum;
            bestHitPoint = Vector3.zero;
            bestScore = float.NegativeInfinity;
            bool anyHit = false;

            foreach (Collider collider in colliders)
            {
                float score = 0f;
                if (!_searchFrustrum.HitsCollider(collider, out score, out Vector3 hitPoint))
                {
                    continue;
                }

                if (_frustums.AidFrustum != null)
                {
                    if (!_frustums.AidFrustum.HitsCollider(collider, out float headScore, out Vector3 headPosition))
                    {
                        continue;
                    }
                    score = score * (1f - _frustums.AidBlending) + headScore * _frustums.AidBlending;
                }

                if (score > bestScore)
                {
                    bestHitPoint = hitPoint;
                    bestScore = score;
                    anyHit = true;
                }
            }
            return anyHit;
        }

        public bool IsPointingWithoutAid(Collider[] colliders)
        {
            if (_frustums.AidFrustum == null)
            {
                return false;
            }
            return !IsPointingAtColliders(colliders, _frustums.AidFrustum)
                && IsWithinDeselectionRange(colliders);
        }

        public bool IsWithinDeselectionRange(Collider[] colliders)
        {
            return IsPointingAtColliders(colliders, _frustums.DeselectionFrustum)
                || IsPointingAtColliders(colliders, _frustums.SelectionFrustum);
        }

        private bool IsPointingAtColliders(Collider[] colliders, ConicalFrustum frustum)
        {
            if (frustum == null)
            {
                return false;
            }

            foreach (Collider collider in colliders)
            {
                if (frustum.HitsCollider(collider, out float score, out Vector3 point))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
