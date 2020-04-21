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
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Typedefs (copy these from HEU_Common.cs)
	using HAPI_NodeId = System.Int32;

	/// <summary>
	/// Represents an instanced object along with its list of instances.
	/// </summary>
	public class HEU_ObjectInstanceInfo : ScriptableObject
	{
		// Instanced game objects. User can override these. Randomly assigned if more than 1.
		public List<HEU_InstancedInput> _instancedInputs = new List<HEU_InstancedInput>();

		// The part using this instanced object
		public HEU_PartData _partTarget;

		// If first element in _instancedGameObjects is a Houdini Engine object node, then this would be its node ID
		public HAPI_NodeId _instancedObjectNodeID = HEU_Defines.HEU_INVALID_NODE_ID;

		// Path in Unity to the instanced object (could be empty or null if not a Unity instanced object)
		public string _instancedObjectPath;

		// Instances using the source instanced object
		public List<GameObject> _instances = new List<GameObject>();
	}

	/// <summary>
	/// Container for an instanced object's input gameobject, and offsets.
	/// </summary>
	[System.Serializable]
	public class HEU_InstancedInput
	{
		public GameObject _instancedGameObject;
		public Vector3 _rotationOffset = Vector3.zero;
		public Vector3 _scaleOffset = Vector3.one;
	}

}   // HoudiniEngineUnity