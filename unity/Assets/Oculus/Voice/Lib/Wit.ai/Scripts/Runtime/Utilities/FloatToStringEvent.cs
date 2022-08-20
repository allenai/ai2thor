/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;
using UnityEngine.Events;

namespace Facebook.WitAi.Utilities
{
    public class FloatToStringEvent : MonoBehaviour
    {
        [SerializeField] private string format;
        [SerializeField] private StringEvent onFloatToString = new StringEvent();

        public void ConvertFloatToString(float value)
        {
            if (string.IsNullOrEmpty(format))
            {
                onFloatToString?.Invoke(value.ToString());
            }
            else
            {
                onFloatToString?.Invoke(value.ToString(format));
            }
        }
    }

    [Serializable]
    public class StringEvent : UnityEvent<string> {}
}
