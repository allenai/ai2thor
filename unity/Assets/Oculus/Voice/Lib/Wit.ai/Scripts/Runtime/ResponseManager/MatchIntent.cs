/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;

namespace Facebook.WitAi
{
    /// <summary>
    /// Triggers a method to be executed if it matches a voice command's intent
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class MatchIntent : Attribute
    {
        public string Intent { get; private set; }
        public float MinConfidence { get; private set; }
        public float MaxConfidence { get; private set; }

        /// <summary>
        /// Triggers a method to be executed if it matches a voice command's intent
        /// </summary>
        /// <param name="intent">The name of the intent to match</param>
        /// <param name="minConfidence">The minimum confidence value (0-1) needed to match</param>
        /// <param name="maxConfidence">The maximum confidence value(0-1) needed to match</param>
        public MatchIntent(string intent, float minConfidence = .9f, float maxConfidence = 1f)
        {
            Intent = intent;
            MinConfidence = minConfidence;
            MaxConfidence = maxConfidence;
        }
    }
}
