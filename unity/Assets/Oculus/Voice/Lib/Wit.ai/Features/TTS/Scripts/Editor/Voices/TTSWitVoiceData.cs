/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Facebook.WitAi.Utilities;
using Facebook.WitAi.TTS.Data;
using Facebook.WitAi.Data.Configuration;
using Facebook.WitAi.Lib;
using NUnit.Framework;

namespace Facebook.WitAi.TTS.Editor.Voices
{
    public struct TTSWitVoiceData
    {
        public string name;
        public string locale;
        public string gender;
        public string[] styles;
    }
}
