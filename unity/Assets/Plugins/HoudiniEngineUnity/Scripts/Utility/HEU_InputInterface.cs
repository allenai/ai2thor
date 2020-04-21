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

namespace HoudiniEngineUnity
{
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Typedefs (copy these from HEU_Common.cs)
	using HAPI_NodeId = System.Int32;

	/// <summary>
	/// Base interface class containing functionality for uploading input data into Houdini.
	/// Derived classes should register with the HEU_InputUtility after scripts are reloaded.
	/// For example, check out HEU_InputInterfaceMesh.cs or HEU_InputInterfaceTerrain.cs.
	/// </summary>
	public abstract class HEU_InputInterface
	{
		public const int DEFAULT_PRIORITY = 100;

		protected int _priority;
		public int Priority { get { return _priority; } }

		public HEU_InputInterface(int priority)
		{
			_priority = priority;
		}

		/// <summary>
		/// Register this interface class with HEU_InputUtility
		/// so that it will be used on input gameobjects
		/// </summary>
		public void RegisterInterface()
		{
			HEU_InputUtility.RegisterInputInterface(this);
		}

#if UNITY_EDITOR
		// USE THE FOLLOWING COMMENTED OUT CODE TO AUTOMATICALLY REGISTER
		// DERIVED INPUT INTERFACE CLASSES ON SCRIPT RELOAD.

		/// <summary>
		/// Registers this input inteface for Unity meshes on
		/// the callback after scripts are reloaded in Unity.
		/// </summary>
		//[UnityEditor.Callbacks.DidReloadScripts]
		//private static void OnScriptsReloaded()
		//{
		//	HEU_InputInterfaceChild inputInterface = new HEU_InputInterfaceChild();
		//	HEU_InputUtility.RegisterInputInterface(inputInterface);
		//}
#endif

		/// <summary>
		/// Return true if this interface supports uploading the given inputObject's data.
		/// Should check the components on the inputObject and children.
		/// </summary>
		/// <param name="inputObject">The gameobject whose components will be checked</param>
		/// <returns>True if this interface supports uploading this input object's data</returns>
		public abstract bool IsThisInputObjectSupported(GameObject inputObject);

		/// <summary>
		/// Create the input node and upload data based on given inputObject.
		/// </summary>
		/// <param name="session">Session to create the node in</param>
		/// <param name="connectNodeID">The node to connect the input node to. Usually the SOP/merge node.</param>
		/// <param name="inputObject">The gameobject containing the components with data for upload</param>
		/// <param name="inputNodeID">The newly created input node's ID</param>
		/// <returns>Returns true if sucessfully created the input node and uploaded data.</returns>
		public abstract bool CreateInputNodeWithDataUpload(HEU_SessionBase session, HAPI_NodeId connectNodeID, GameObject inputObject, out HAPI_NodeId inputNodeID);
	}

}   // HoudiniEngineUnity