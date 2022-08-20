/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine.Events;

namespace Facebook.WitAi.Events
{
    /// <summary>
    /// An error event with two parameters.
    ///
    /// Param 1: error - the type of error that occurred
    /// Param 2: message - A human readable message describing the error
    /// </summary>
    [Serializable]
    public class WitErrorEvent : UnityEvent<string, string>
    {
    }
}
