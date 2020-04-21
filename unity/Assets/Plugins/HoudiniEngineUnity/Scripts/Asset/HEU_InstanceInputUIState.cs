/*
* Copyright (c) <2019> Side Effects Software Inc.
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoudiniEngineUnity
{
	/// <summary>
	/// Object to store instance input UI state so that we can check if UI changed
	/// and apply modifications for just this object instead of for the entire asset.
	/// Used by HEU_InstanceInputUI.
	/// </summary>
	[System.Serializable]
	public class HEU_InstanceInputUIState : ScriptableObject
	{
		// Whether to show all instance inputs to expanded form
		public bool _showInstanceInputs = true;

		// For pagination, the number of inputs to show per page
		public int _numInputsToShowUI = 5;

		// The current page to show
		public int _inputsPageIndexUI = 0;

		public void CopyTo(HEU_InstanceInputUIState dest)
		{
			dest._showInstanceInputs = _showInstanceInputs;
			dest._numInputsToShowUI = _numInputsToShowUI;
			dest._inputsPageIndexUI = _inputsPageIndexUI;
		}
	}

}   // HoudiniEngineUnity