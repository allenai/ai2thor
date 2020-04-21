/*
* Copyright (c) <2018> Side Effects Software Inc.
* All rights reserved.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions are met:
*
* 1. Redistributions of source code must retain the above copyright notice,
*    this list of conditions and the following disclaimer.
*
* 2. The name of Side Effects Software may not be used to endorse or
*    promote products derived from this software without specific prior
*    written permission.
*
* THIS SOFTWARE IS PROVIDED BY SIDE EFFECTS SOFTWARE "AS IS" AND ANY EXPRESS
* OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
* OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.  IN
* NO EVENT SHALL SIDE EFFECTS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
* INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
* LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
* OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
* LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
* NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
* EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// Test class for Houdini Engine Asset events (build, cook, bake)
/// </summary>
public class HEU_AssetEventReceiverTest : MonoBehaviour
{

	public void ReloadCallback(HoudiniEngineUnity.HEU_HoudiniAsset asset, bool success, List<GameObject> outputList)
	{
		Debug.LogFormat("Reloaded! Asset={0}, Success={1}, Outputs={2}", asset.AssetName, success, outputList.Count);
	}

	public void CookedCallback(HoudiniEngineUnity.HEU_HoudiniAsset asset, bool success, List<GameObject> outputList)
	{
		Debug.LogFormat("Cooked! Asset={0}, Success={1}, Outputs={2}", asset.AssetName, success, outputList.Count);
	}

	public void BakedCallback(HoudiniEngineUnity.HEU_HoudiniAsset asset, bool success, List<GameObject> outputList)
	{
		Debug.LogFormat("Baked! Asset={0}, Success={1}, Outputs={2}", asset.AssetName, success, outputList.Count);
	}
}
