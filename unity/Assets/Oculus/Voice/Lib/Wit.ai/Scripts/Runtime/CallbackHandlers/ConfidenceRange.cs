/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine.Events;

namespace Facebook.WitAi.CallbackHandlers
{
    [Serializable]
    public class ConfidenceRange
    {
        public float minConfidence;
        public float maxConfidence;
        public UnityEvent onWithinConfidenceRange = new UnityEvent();
        public UnityEvent onOutsideConfidenceRange = new UnityEvent();
    }
}
