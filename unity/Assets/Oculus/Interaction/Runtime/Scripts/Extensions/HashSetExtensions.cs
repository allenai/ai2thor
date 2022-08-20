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
using System.Collections.Generic;

namespace Oculus.Interaction
{
    /// <summary>
    /// Non-allocating HashSet extension methods mirroring MS implementation
    /// https://referencesource.microsoft.com/#system.core/system/Collections/Generic/HashSet.cs
    /// </summary>
    public static class HashSetExtensions
    {
        /// <summary>
        /// Take the union of this HashSet with other. Modifies this set.
        /// </summary>
        /// <param name="other">HashSet with items to add</param>
        public static void UnionWithNonAlloc<T>(this HashSet<T> hashSetToModify, HashSet<T> other)
        {
            if (hashSetToModify == null)
            {
                throw new ArgumentNullException(nameof(hashSetToModify));
            }
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            foreach (T item in other)
            {
                hashSetToModify.Add(item);
            }
        }

        /// <summary>
        /// Take the union of this HashSet with other. Modifies this set.
        /// </summary>
        /// <param name="other">IList with items to add</param>
        public static void UnionWithNonAlloc<T>(this HashSet<T> hashSetToModify, IList<T> other)
        {
            if (hashSetToModify == null)
            {
                throw new ArgumentNullException(nameof(hashSetToModify));
            }
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            for (int i = 0; i < other.Count; ++i)
            {
                hashSetToModify.Add(other[i]);
            }
        }

        /// <summary>
        /// Remove items in other from this set. Modifies this set
        /// </summary>
        /// <param name="other">HashSet with items to remove</param>
        public static void ExceptWithNonAlloc<T>(this HashSet<T> hashSetToModify, HashSet<T> other)
        {
            if (hashSetToModify == null)
            {
                throw new ArgumentNullException(nameof(hashSetToModify));
            }
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (hashSetToModify.Count == 0)
            {
                return;
            }
            if (other == hashSetToModify)
            {
                hashSetToModify.Clear();
                return;
            }
            foreach (T element in other)
            {
                hashSetToModify.Remove(element);
            }
        }

        /// <summary>
        /// Remove items in other from this set. Modifies this set
        /// </summary>
        /// <param name="other">IList with items to remove</param>
        public static void ExceptWithNonAlloc<T>(this HashSet<T> hashSetToModify, IList<T> other)
        {
            if (hashSetToModify == null)
            {
                throw new ArgumentNullException(nameof(hashSetToModify));
            }
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (hashSetToModify.Count == 0)
            {
                return;
            }
            for (int i = 0; i < other.Count; ++i)
            {
                hashSetToModify.Remove(other[i]);
            }
        }

        /// <summary>
        /// Checks if this set overlaps other (i.e. they share at least one item)
        /// </summary>
        /// <param name="other">HashSet to check overlap against</param>
        /// <returns>true if these have at least one common element; false if disjoint</returns>
        public static bool OverlapsNonAlloc<T>(this HashSet<T> hashSetToCheck, HashSet<T> other)
        {
            if (hashSetToCheck == null)
            {
                throw new ArgumentNullException(nameof(hashSetToCheck));
            }
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (hashSetToCheck.Count == 0)
            {
                return false;
            }
            foreach (T element in other)
            {
                if (hashSetToCheck.Contains(element))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if this set overlaps other (i.e. they share at least one item)
        /// </summary>
        /// <param name="other">IList to check overlap against</param>
        /// <returns>true if these have at least one common element; false if disjoint</returns>
        public static bool OverlapsNonAlloc<T>(this HashSet<T> hashSetToCheck, IList<T> other)
        {
            if (hashSetToCheck == null)
            {
                throw new ArgumentNullException(nameof(hashSetToCheck));
            }
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (hashSetToCheck.Count == 0)
            {
                return false;
            }
            for (int i = 0; i < other.Count; ++i)
            {
                if (hashSetToCheck.Contains(other[i]))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
