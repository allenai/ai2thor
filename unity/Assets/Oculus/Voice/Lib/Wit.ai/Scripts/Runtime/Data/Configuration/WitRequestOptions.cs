/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Facebook.WitAi.Interfaces;

namespace Facebook.WitAi.Configuration
{
    public class WitRequestOptions
    {
        /// <summary>
        /// An interface that provides a list of entities that should be used for nlu resolution.
        /// </summary>
        public IDynamicEntitiesProvider dynamicEntities;

        /// <summary>
        /// The maximum number of intent matches to return
        /// </summary>
        public int nBestIntents = -1;

        /// <summary>
        /// The tag for snapshot
        /// </summary>
        public string tag;

        /// <summary>
        /// Callback for completion
        /// </summary>
        public Action<WitRequest> onResponse;
    }
}
