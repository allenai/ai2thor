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

#if (UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_LINUX)
#define HOUDINIENGINEUNITY_ENABLED
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HoudiniEngineUnity
{
	/// <summary>
	/// This updates HEU_HoudiniAsset nodes that are added to its internal list.
	/// This is to workaround Unity's editor update limitations.
	/// </summary>
#if UNITY_EDITOR && HOUDINIENGINEUNITY_ENABLED
	[InitializeOnLoad]
#endif
	public class HEU_AssetUpdater
	{
#if UNITY_EDITOR && HOUDINIENGINEUNITY_ENABLED
		private static List<HEU_HoudiniAsset> _allHoudiniAssets = new List<HEU_HoudiniAsset>();
#endif

		static HEU_AssetUpdater()
		{
#if UNITY_EDITOR && HOUDINIENGINEUNITY_ENABLED
			EditorApplication.update += Update;
#endif
		}

		static void Update()
		{
#if UNITY_EDITOR && HOUDINIENGINEUNITY_ENABLED
			for(int i = 0; i < _allHoudiniAssets.Count; ++i)
			{
				if(_allHoudiniAssets[i] != null)
				{
					_allHoudiniAssets[i].AssetUpdate();
				}
				else
				{
					_allHoudiniAssets.RemoveAt(i);
					i--;
				}
			}

			// PostAssetUpdate progresses the asset's state after cooking and building
			// in order to update the UI.
			foreach (HEU_HoudiniAsset asset in _allHoudiniAssets)
			{
				if (asset != null)
				{
					asset.PostAssetUpdate();
				}
			}
#endif
		}

		public static void AddAssetForUpdate(HEU_HoudiniAsset asset)
		{
#if UNITY_EDITOR && HOUDINIENGINEUNITY_ENABLED
			if (!_allHoudiniAssets.Contains(asset))
			{
				_allHoudiniAssets.Add(asset);
			}
#endif
		}

		public static void RemoveAsset(HEU_HoudiniAsset asset)
		{
#if UNITY_EDITOR && HOUDINIENGINEUNITY_ENABLED
			// Setting the asset reference to null and removing
			// later in Update in case of removing while iterating the list
			int index = _allHoudiniAssets.IndexOf(asset);
			if (index >= 0)
			{
				_allHoudiniAssets[index] = null;
			}
#endif
		}
	}

}   // HoudiniEngineUnity