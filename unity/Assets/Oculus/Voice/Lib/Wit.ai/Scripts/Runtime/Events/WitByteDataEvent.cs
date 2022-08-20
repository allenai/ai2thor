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
    [Serializable]
    public class WitByteDataEvent : UnityEvent<byte[], int, int> { }

    public interface IWitByteDataReadyHandler
    {
        void OnWitDataReady(byte[] data, int offset, int length);
    }

    public interface IWitByteDataSentHandler
    {
        void OnWitDataSent(byte[] data, int offset, int length);
    }
}
