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
using Oculus.Voice.Core.Bindings.Android;
using UnityEngine;

namespace Oculus.Voice.Bindings.Android
{
    public class VoiceSDKBinding : BaseServiceBinding
    {
        public VoiceSDKBinding(AndroidJavaObject sdkInstance) : base(sdkInstance)
        {
        }

        public bool Active => binding.Call<bool>("isActive");
        public bool IsRequestActive => binding.Call<bool>("isRequestActive");
        public bool MicActive => binding.Call<bool>("isMicActive");
        public bool PlatformSupportsWit => binding.Call<bool>("isSupported");

        public void Activate(string text)
        {
            binding.Call("activate", text, "");
        }

        public void Activate(string text, WitRequestOptions options)
        {
            binding.Call("activate", text, JsonUtility.ToJson(options));
        }

        public void Activate()
        {
            binding.Call("activate");
        }

        public void Activate(WitRequestOptions options)
        {
            binding.Call("activate", JsonUtility.ToJson(options));
        }

        public void ActivateImmediately()
        {
            binding.Call("activateImmediately");
        }

        public void ActivateImmediately(WitRequestOptions options)
        {
            binding.Call("activateImmediately", JsonUtility.ToJson(options));
        }

        public void Deactivate()
        {
            binding.Call("deactivate");
        }

        public void DeactivateAndAbortRequest()
        {
            binding.Call("deactivateAndAbortRequest");
        }

        public void SetRuntimeConfiguration(WitRuntimeConfiguration configuration)
        {
            binding.Call("setRuntimeConfig", new VoiceSDKConfigBinding(configuration).ToJavaObject());
        }

        public void SetListener(VoiceSDKListenerBinding listener)
        {
            binding.Call("setListener", listener);
        }

        public void Connect()
        {
            binding.Call<bool>("connect");
        }
    }
}
