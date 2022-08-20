/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Facebook.WitAi.Lib;

namespace Facebook.WitAi.Data
{
    public class WitStringValue : WitValue
    {
        public override object GetValue(WitResponseNode response)
        {
            return GetStringValue(response);
        }

        public override bool Equals(WitResponseNode response, object value)
        {
            if (value is string sValue)
            {
                return GetStringValue(response) == sValue;
            }

            return "" + value == GetStringValue(response);
        }

        public string GetStringValue(WitResponseNode response)
        {
            return Reference.GetStringValue(response);
        }
    }
}
