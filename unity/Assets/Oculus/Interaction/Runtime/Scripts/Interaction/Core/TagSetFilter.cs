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
    /// Compares string tags to filter GameObjects.
    /// </summary>
    public class TagSetFilter : MonoBehaviour, IGameObjectFilter
    {
        /// An GameObject must meet all required tags
        [SerializeField, Optional]
        private string[] _requireTags;

        /// A GameObject must not meet any exclude tags
        [SerializeField, Optional]
        [FormerlySerializedAs("_avoidTags")]
        private string[] _excludeTags;

        private HashSet<string> _requireTagSet;
        private HashSet<string> _excludeTagSet;

        protected virtual void Start()
        {
            _requireTagSet = new HashSet<string>();
            _excludeTagSet = new HashSet<string>();

            foreach (string requireTag in _requireTags)
            {
                _requireTagSet.Add(requireTag);
            }

            foreach (string excludeTag in _excludeTags)
            {
                _excludeTagSet.Add(excludeTag);
            }
        }

        public bool Filter(GameObject gameObject)
        {
            TagSet tagSet = gameObject.GetComponent<TagSet>();
            if (tagSet == null && _requireTagSet.Count > 0)
            {
                return false;
            }

            foreach (string tag in _requireTagSet)
            {
                if (!tagSet.ContainsTag(tag))
                {
                    return false;
                }
            }

            if (tagSet == null)
            {
                return true;
            }

            foreach (string tag in _excludeTagSet)
            {
                if (tagSet.ContainsTag(tag))
                {
                    return false;
                }
            }

            return true;
        }

        #region Inject

        public void InjectOptionalRequireTags(string[] requireTags)
        {
            _requireTags = requireTags;
        }

        public void InjectOptionalExcludeTags(string[] excludeTags)
        {
            _excludeTags = excludeTags;
        }

        #endregion
    }
}
