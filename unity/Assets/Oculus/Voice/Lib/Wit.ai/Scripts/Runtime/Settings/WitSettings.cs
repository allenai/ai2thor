/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

#if UNITY_EDITOR
using System;

namespace Facebook.WitAi
{
    // Wit Settings
    [Serializable]
    public struct WitSettings
    {
        public WitConfigSettings[] configSettings;
    }
    // Wit Config Settings
    [Serializable]
    public struct WitConfigSettings
    {
        public string appID;
        public string serverToken;
    }
}
#endif
