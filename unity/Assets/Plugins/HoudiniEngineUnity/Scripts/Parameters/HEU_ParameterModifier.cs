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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoudiniEngineUnity
{
	/// <summary>
	/// Helper that contains a request for parameter modification.
	/// Currently used to modifier multiparms after the UI has drawn.
	/// </summary>
	[System.Serializable]
	public class HEU_ParameterModifier
	{
		public int _parameterIndex;

		// Modifier action
		public enum ModifierAction
		{
			MULTIPARM_INSERT,	// Insert _modifierValue number of params from _instanceIndex onwards
			MULTIPARM_REMOVE,   // Remove _modifierValue number of params from _instanceIndex onwards
			MULTIPARM_CLEAR,	// Clear all instances
			SET_FLOAT,			// Set float value for parameter
			SET_INT				// Set int value for parameter
		}
		public ModifierAction _action;

		// Instance index of the parameter instance (for multiparm)
		public int _instanceIndex;

		// General value for the action (eg. number of new instances for INSERT, number of instances to REMOVE)
		public int _modifierValue;

		public float _floatValue;
		public int _intValue;

		public static HEU_ParameterModifier GetNewModifier(ModifierAction action, int parameterIndex, int instanceIndex, int modifierValue)
		{
			HEU_ParameterModifier newModifier = new HEU_ParameterModifier();
			newModifier._action = action;
			newModifier._parameterIndex = parameterIndex;
			newModifier._instanceIndex = instanceIndex;
			newModifier._modifierValue = modifierValue;

			return newModifier;
		}
	}

}   // HoudiniEngineUnity