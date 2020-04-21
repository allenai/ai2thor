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
using System.Collections;
using System.Collections.Generic;

using HoudiniEngineUnity;

/// <summary>
/// Example to show how a HDA's parameters can be modified via script and recooked.
/// </summary>
public class HEU_ScriptParameterExample : MonoBehaviour
{
	// Instance the Evergreen HDA in the scene, and set its gameobject to here
	public GameObject _evergreenGameObject;

	// Reference to the actual HEU_HoduiniAsset
	private HEU_HoudiniAsset _evergreenAsset;

	public float _updateRate = 0.1f;

	public float _scale = 20f;



	public void Start()
	{
		// Grab the HEU_HoduiniAsset
		_evergreenAsset = _evergreenGameObject.GetComponent<HEU_HoudiniAssetRoot>() != null ? _evergreenGameObject.GetComponent<HEU_HoudiniAssetRoot>()._houdiniAsset : null;

		// Always get the latest parms after each cook
		List<HEU_ParameterData> parms = _evergreenAsset.Parameters.GetParameters();

		foreach(HEU_ParameterData parmData in parms)
		{
			Debug.Log(parmData._labelName);
			
			if(parmData._parmInfo.type == HAPI_ParmType.HAPI_PARMTYPE_BUTTON)
			{
				// Display a button: parmData._intValues[0];

			}
			else if(parmData._parmInfo.type == HAPI_ParmType.HAPI_PARMTYPE_FLOAT)
			{
				// Display a float: parmData._floatValues[0];

				// You can set a float this way
				HEU_ParameterUtility.SetFloat(_evergreenAsset, parmData._name, 1f);

				// Or this way (the index is 0, unless its for array of floats)
				parmData._floatValues[0] = 1;
			}
		}
		// Make sure to cook after changing
		_evergreenAsset.RequestCook(true, false, true, true);

		// Start a repeating updater
		InvokeRepeating("UpdateGravity", _updateRate, _updateRate);
	}


	private void UpdateGravity()
	{
		if (_evergreenAsset != null)
		{
			float g = (1.0f + Mathf.Sin(Time.realtimeSinceStartup)) * _scale;

			// Use helper to set float parameter with name
			HEU_ParameterUtility.SetFloat(_evergreenAsset, "gravity", g);

			// Use helper to set random color
			HEU_ParameterUtility.SetColor(_evergreenAsset, "branch_vtx_color_color", Random.ColorHSV());

			// Cook synchronously to guarantee geometry generated in this update.
			_evergreenAsset.RequestCook(true, false, true, true);
		}
	}
	
}
