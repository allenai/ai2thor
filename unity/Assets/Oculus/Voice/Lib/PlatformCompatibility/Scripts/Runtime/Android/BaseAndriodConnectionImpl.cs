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

using System;
using UnityEngine;

namespace Oculus.Voice.Core.Bindings.Android
{
    public class BaseAndroidConnectionImpl<T> where T : BaseServiceBinding
    {
        private string fragmentClassName;
        protected T service;
        protected readonly AndroidServiceConnection serviceConnection;

        public bool IsConnected => serviceConnection.IsConnected;

        public BaseAndroidConnectionImpl(string className)
        {
            fragmentClassName = className;
            serviceConnection = new AndroidServiceConnection(className, "getService");
        }

        #region Service Connection

        public virtual void Connect()
        {
            serviceConnection.Connect();
            var serviceInstance = serviceConnection.GetService();
            if (null == serviceInstance)
            {
                throw new Exception("Unable to get service connection from " + fragmentClassName);
            }

            service = (T) Activator.CreateInstance(typeof(T), serviceInstance);
        }

        public virtual void Disconnect()
        {
            service.Shutdown();
            serviceConnection.Disconnect();
            service = null;
        }

        #endregion
    }

    public class BaseServiceBinding
    {
        protected AndroidJavaObject binding;

        protected BaseServiceBinding(AndroidJavaObject sdkInstance)
        {
            binding = sdkInstance;
        }

        public void Shutdown()
        {
            binding.Call("shutdown");
        }
    }
}
