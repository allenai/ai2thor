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

using System.Text;
using System.Runtime.InteropServices;

namespace HoudiniEngineUnity
{
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Typedefs (copy these from HEU_Common.cs)
	using HAPI_StringHandle = System.Int32;
	using HAPI_NodeId = System.Int32;
	using HAPI_PDG_WorkitemId = System.Int32;
	using HAPI_PDG_GraphContextId = System.Int32;


	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Imports

	/// <summary>
	/// Import HAPI PDG functions.
	/// </summary>
	public static class HEU_HAPIImportsPDG
	{
#if HOUDINIENGINEUNITY_ENABLED

		[DllImport(HEU_HoudiniVersion.HAPI_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
		public static extern HAPI_Result
		HAPI_GetPDGGraphContexts(
			ref HAPI_Session session,
			out int num_contexts,
			[Out] HAPI_StringHandle[] context_names_array,
			[Out] HAPI_PDG_GraphContextId[] context_id_array,
			int count);

		[DllImport(HEU_HoudiniVersion.HAPI_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
		public static extern HAPI_Result
		HAPI_CookPDG(
			ref HAPI_Session session,
			HAPI_NodeId cook_node_id,
			int generate_only,
			int blocking);

		[DllImport(HEU_HoudiniVersion.HAPI_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
		public static extern HAPI_Result
		HAPI_GetPDGEvents(
			ref HAPI_Session session,
			HAPI_PDG_GraphContextId graph_context_id,
			[Out] HAPI_PDG_EventInfo[] event_array,
			int length,
			out int event_count,
			out int remaining_events);

		[DllImport(HEU_HoudiniVersion.HAPI_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
		public static extern HAPI_Result
		HAPI_GetPDGState(
			ref HAPI_Session session,
			HAPI_PDG_GraphContextId graph_context_id,
			out int pdg_state);

		[DllImport(HEU_HoudiniVersion.HAPI_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
		public static extern HAPI_Result
		HAPI_CreateWorkitem(
			ref HAPI_Session session,
			HAPI_NodeId node_id,
			out HAPI_PDG_WorkitemId workitem_id,
			string name,
			int index);

		[DllImport(HEU_HoudiniVersion.HAPI_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
		public static extern HAPI_Result
		HAPI_GetWorkitemInfo(
			ref HAPI_Session session,
			HAPI_PDG_GraphContextId graph_context_id,
			HAPI_PDG_WorkitemId workitem_id,
			ref HAPI_PDG_WorkitemInfo workitem_info);

		[DllImport(HEU_HoudiniVersion.HAPI_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
		public static extern HAPI_Result
		HAPI_SetWorkitemIntData(
			ref HAPI_Session session,
			HAPI_NodeId node_id,
			HAPI_PDG_WorkitemId workitem_id,
			string data_name,
			int[] values_array,
			int length);

		[DllImport(HEU_HoudiniVersion.HAPI_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
		public static extern HAPI_Result
		HAPI_SetWorkitemFloatData(
			ref HAPI_Session session,
			HAPI_NodeId node_id,
			HAPI_PDG_WorkitemId workitem_id,
			string data_name,
			float[] values_array,
			int length);

		[DllImport(HEU_HoudiniVersion.HAPI_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
		public static extern HAPI_Result
		HAPI_SetWorkitemStringData(
			ref HAPI_Session session,
			HAPI_NodeId node_id,
			HAPI_PDG_WorkitemId workitem_id,
			string data_name,
			int data_index,
			string value);

		[DllImport(HEU_HoudiniVersion.HAPI_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
		public static extern HAPI_Result
		HAPI_CommitWorkitems(
			ref HAPI_Session session,
			HAPI_NodeId node_id);

		[DllImport(HEU_HoudiniVersion.HAPI_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
		public static extern HAPI_Result
		HAPI_GetNumWorkitems(
			ref HAPI_Session session,
			HAPI_NodeId node_id,
			out int num);

		[DllImport(HEU_HoudiniVersion.HAPI_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
		public static extern HAPI_Result
		HAPI_GetWorkitems(
			ref HAPI_Session session,
			HAPI_NodeId node_id,
			[Out] HAPI_PDG_WorkitemId[] workitem_ids,
			int length);

		[DllImport(HEU_HoudiniVersion.HAPI_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
		public static extern HAPI_Result
		HAPI_GetWorkitemDataLength(
			ref HAPI_Session session,
			HAPI_NodeId node_id,
			HAPI_PDG_WorkitemId workitem_id,
			string data_name,
			out int length);

		[DllImport(HEU_HoudiniVersion.HAPI_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
		public static extern HAPI_Result
		HAPI_GetWorkitemIntData(
			ref HAPI_Session session,
			HAPI_NodeId node_id,
			HAPI_PDG_WorkitemId workitem_id,
			string data_name,
			[Out] int[] data_array,
			int length);

		[DllImport(HEU_HoudiniVersion.HAPI_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
		public static extern HAPI_Result
		HAPI_GetWorkitemFloatData(
			ref HAPI_Session session,
			HAPI_NodeId node_id,
			HAPI_PDG_WorkitemId workitem_id,
			string data_name,
			[Out] float[] data_array,
			int length);

		[DllImport(HEU_HoudiniVersion.HAPI_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
		public static extern HAPI_Result
		HAPI_GetWorkitemStringData(
			ref HAPI_Session session,
			HAPI_NodeId node_id,
			HAPI_PDG_WorkitemId workitem_id,
			string data_name,
			StringBuilder data_array,
			int length);

		[DllImport(HEU_HoudiniVersion.HAPI_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
		public static extern HAPI_Result
		HAPI_GetWorkitemResultInfo(
			ref HAPI_Session session,
			HAPI_NodeId node_id,
			HAPI_PDG_WorkitemId workitem_id,
			[Out] HAPI_PDG_WorkitemResultInfo[] resultinfo_array,
			int resultinfo_count);


		[DllImport(HEU_HoudiniVersion.HAPI_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
		public static extern HAPI_Result
		HAPI_DirtyPDGNode(
			ref HAPI_Session session,
			HAPI_NodeId node_id,
			[MarshalAs(UnmanagedType.U1)] bool clean_results);

		[DllImport(HEU_HoudiniVersion.HAPI_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
		public static extern HAPI_Result
		HAPI_PausePDGCook(
			ref HAPI_Session session,
			HAPI_PDG_GraphContextId graph_context_id);

		[DllImport(HEU_HoudiniVersion.HAPI_LIBRARY, CallingConvention = CallingConvention.Cdecl)]
		public static extern HAPI_Result
		HAPI_CancelPDGCook(
			ref HAPI_Session session,
			HAPI_PDG_GraphContextId graph_context_id);
#endif
	}


}   // HoudiniEngineUnity