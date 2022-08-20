/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Facebook.WitAi.Lib;
using UnityEngine;

namespace Facebook.WitAi.Data
{
    public abstract class WitValue : ScriptableObject
    {
        [SerializeField] public string path;
        private WitResponseReference reference;

        public WitResponseReference Reference
        {
            get
            {
                if (null == reference)
                {
                    reference = WitResultUtilities.GetWitResponseReference(path);
                }

                return reference;
            }
        }

        public abstract object GetValue(WitResponseNode response);

        public abstract bool Equals(WitResponseNode response, object value);

        public string ToString(WitResponseNode response)
        {
            return Reference.GetStringValue(response);
        }
    }
}
