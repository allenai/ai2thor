/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Facebook.WitAi.Configuration;
using UnityEngine;

namespace Oculus.Voice.Bindings.Android
{
    public class VoiceSDKConfigBinding
    {
        private WitRuntimeConfiguration configuration;

        public VoiceSDKConfigBinding(WitRuntimeConfiguration config)
        {
            configuration = config;
        }

        public AndroidJavaObject ToJavaObject()
        {
            AndroidJavaObject witConfig =
                new AndroidJavaObject("com.oculus.assistant.api.voicesdk.immersivevoicecommands.WitConfiguration");
            witConfig.Set("clientAccessToken", configuration.witConfiguration.clientAccessToken);

            AndroidJavaObject witRuntimeConfig = new AndroidJavaObject("com.oculus.assistant.api.voicesdk.immersivevoicecommands.WitRuntimeConfiguration");
            witRuntimeConfig.Set("witConfiguration", witConfig);

            witRuntimeConfig.Set("minKeepAliveVolume", configuration.minKeepAliveVolume);
            witRuntimeConfig.Set("minKeepAliveTimeInSeconds",
                configuration.minKeepAliveTimeInSeconds);
            witRuntimeConfig.Set("minTranscriptionKeepAliveTimeInSeconds",
                configuration.minTranscriptionKeepAliveTimeInSeconds);
            witRuntimeConfig.Set("maxRecordingTime",
                configuration.maxRecordingTime);
            witRuntimeConfig.Set("soundWakeThreshold",
                configuration.soundWakeThreshold);
            witRuntimeConfig.Set("sampleLengthInMs",
                configuration.sampleLengthInMs);
            witRuntimeConfig.Set("micBufferLengthInSeconds",
                configuration.micBufferLengthInSeconds);
            witRuntimeConfig.Set("sendAudioToWit",
                configuration.sendAudioToWit);
            witRuntimeConfig.Set("preferredActivationOffset",
                configuration.preferredActivationOffset);

            return witRuntimeConfig;
        }
    }
}
