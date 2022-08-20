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

using Oculus.Voice.Core.Bindings.Interfaces;
using UnityEngine;

namespace Oculus.Voice.Core.Bindings.Android
{
    public class AndroidServiceConnection : IConnection
    {
        private AndroidJavaObject mAssistantServiceConnection;

        private string serviceFragmentClass;
        private string serviceGetter;

        public bool IsConnected => null != mAssistantServiceConnection;

        public AndroidJavaObject AssistantServiceConnection => mAssistantServiceConnection;

        /// <summary>
        /// Creates a connection manager of the given type
        /// </summary>
        /// <param name="serviceFragmentClassName">The fully qualified class name of the service fragment that will manage this connection</param>
        /// <param name="serviceGetterMethodName">The name of the method that will return an instance of the service</param>
        /// TODO: We should make the getBlahService simply getService() within each fragment implementation.
        public AndroidServiceConnection(string serviceFragmentClassName, string serviceGetterMethodName)
        {
            serviceFragmentClass = serviceFragmentClassName;
            serviceGetter = serviceGetterMethodName;
        }

        public void Connect()
        {
            if (null == mAssistantServiceConnection)
            {
                AndroidJNIHelper.debug = true;

                AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject activity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

                using (AndroidJavaClass assistantBackgroundFragment = new AndroidJavaClass(serviceFragmentClass))
                {
                    mAssistantServiceConnection =
                        assistantBackgroundFragment.CallStatic<AndroidJavaObject>("createAndAttach", activity);
                }
            }
        }

        public void Disconnect()
        {
            mAssistantServiceConnection.Call("detach");
        }

        public AndroidJavaObject GetService()
        {
            return mAssistantServiceConnection.Call<AndroidJavaObject>(serviceGetter);
        }
    }
}
