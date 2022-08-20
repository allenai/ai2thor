/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;

namespace Facebook.WitAi.Utilities
{
    public class DictionaryList<T, U>
    {
        private Dictionary<T, List<U>> dictionary = new Dictionary<T, List<U>>();

        public void Add(T key, U value)
        {
            if (!TryGetValue(key, out var values))
            {
                dictionary[key] = values;
            }
            values.Add(value);
        }

        public void RemoveAt(T key, int index)
        {
            if (TryGetValue(key, out var values)) values.RemoveAt(index);
        }

        public void Remove(T key, U value)
        {
            if (TryGetValue(key, out var values)) values.Remove(value);
        }

        #region Getters
        public List<U> this[T key]
        {
            get
            {
                List<U> values;
                if (!TryGetValue(key, out values))
                {
                    values = new List<U>();
                    dictionary[key] = values;
                }
                return values;
            }
            set => dictionary[key] = value;
        }

        public bool TryGetValue(T key, out List<U> values)
        {
            if (!dictionary.TryGetValue(key, out values))
            {
                values = new List<U>();
                return false;
            }

            return true;
        }
        #endregion
    }
}
