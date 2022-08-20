/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using Facebook.WitAi.Data.Entities;

namespace Facebook.WitAi.Interfaces
{
    public interface IDynamicEntitiesProvider
    {
        WitDynamicEntities GetDynamicEntities();
    }
}
