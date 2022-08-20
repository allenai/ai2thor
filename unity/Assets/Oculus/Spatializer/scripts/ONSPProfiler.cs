/************************************************************************************
Filename    :   ONSPProfiler.cs
Content     :   Use this to attach to the Oculus Audio Profiler tool
Copyright   :   Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Licensed under the Oculus SDK Version 3.5 (the "License"); 
you may not use the Oculus SDK except in compliance with the License, 
which is provided at the time of installation or download, or which 
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developer.oculus.com/licenses/sdk-3.5/

Unless required by applicable law or agreed to in writing, the Oculus SDK 
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
************************************************************************************/
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;


public class ONSPProfiler : MonoBehaviour
{
    public bool profilerEnabled = false;
    const int DEFAULT_PORT = 2121;
    public int port = DEFAULT_PORT;

    void Start()
    {
        Application.runInBackground = true;
    }

    void Update()
    {
        if (port < 0 || port > 65535)
        {
            port = DEFAULT_PORT;
        }
        ONSP_SetProfilerPort(port);
        ONSP_SetProfilerEnabled(profilerEnabled);
    }

	// Import functions
    public const string strONSPS = "AudioPluginOculusSpatializer";
	
    [DllImport(strONSPS)]
    private static extern int ONSP_SetProfilerEnabled(bool enabled);
    [DllImport(strONSPS)]
    private static extern int ONSP_SetProfilerPort(int port);
}
