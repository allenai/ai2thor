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

using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;


namespace HoudiniEngineUnity
{
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Typedefs (copy these from HEU_Common.cs)
	using HAPI_StringHandle = System.Int32;
	using HAPI_NodeId = System.Int32;
	using HAPI_PDG_WorkitemId = System.Int32;
	using HAPI_PDG_GraphContextId = System.Int32;


	/// <summary>
	/// Session wrapper for HAPI PDG calls.
	/// </summary>
	public static class HEU_SessionPDG
	{
#if HOUDINIENGINEUNITY_ENABLED

		// SESSION ----------------------------------------------------------------------------------------------------

		public static bool GetPDGGraphContexts(this HEU_SessionBase session, out int num_contexts, [Out] HAPI_StringHandle[] context_names_array, [Out] HAPI_PDG_GraphContextId[] context_id_array, int count, bool bLogError)
		{
			HAPI_Result result = HEU_HAPIImportsPDG.HAPI_GetPDGGraphContexts(ref session.GetSessionData()._HAPISession, out num_contexts, context_names_array, context_id_array, count);
			session.HandleStatusResult(result, "Getting PDG Graph Contexts", false, bLogError);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public static bool CookPDG(this HEU_SessionBase session, HAPI_NodeId cook_node_id, int generate_only, int blocking)
		{
			HAPI_Result result = HEU_HAPIImportsPDG.HAPI_CookPDG(ref session.GetSessionData()._HAPISession, cook_node_id, generate_only, blocking);
			session.HandleStatusResult(result, "Cooking PDG", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public static bool GetPDGEvents(this HEU_SessionBase session, HAPI_PDG_GraphContextId graph_context_id, [Out] HAPI_PDG_EventInfo[] event_array, int length, out int event_count, out int remaining_events)
		{
			HAPI_Result result = HEU_HAPIImportsPDG.HAPI_GetPDGEvents(ref session.GetSessionData()._HAPISession, graph_context_id, event_array, length, out event_count, out remaining_events);
			session.HandleStatusResult(result, "Getting PDG Events", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public static bool GetPDGState(this HEU_SessionBase session, HAPI_PDG_GraphContextId graph_context_id, out int pdg_state)
		{
			HAPI_Result result = HEU_HAPIImportsPDG.HAPI_GetPDGState(ref session.GetSessionData()._HAPISession, graph_context_id, out pdg_state);
			session.HandleStatusResult(result, "Getting PDG State", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public static bool CreateWorkitem(this HEU_SessionBase session, HAPI_NodeId node_id, out HAPI_PDG_WorkitemId workitem_id, string name, int index)
		{
			HAPI_Result result = HEU_HAPIImportsPDG.HAPI_CreateWorkitem(ref session.GetSessionData()._HAPISession, node_id, out workitem_id, name, index);
			session.HandleStatusResult(result, "Creating Workitem", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public static bool GetWorkItemInfo(this HEU_SessionBase session, HAPI_PDG_GraphContextId graph_context_id, HAPI_PDG_WorkitemId workitem_id, ref HAPI_PDG_WorkitemInfo workitem_info)
		{
			HAPI_Result result = HEU_HAPIImportsPDG.HAPI_GetWorkitemInfo(ref session.GetSessionData()._HAPISession, graph_context_id, workitem_id, ref workitem_info);
			session.HandleStatusResult(result, "Getting WorkItem", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public static bool SetWorkitemIntData(this HEU_SessionBase session, HAPI_NodeId node_id, HAPI_PDG_WorkitemId workitem_id, string data_name, int[] values_array, int length)
		{
			HAPI_Result result = HEU_HAPIImportsPDG.HAPI_SetWorkitemIntData(ref session.GetSessionData()._HAPISession, node_id, workitem_id, data_name, values_array, length);
			session.HandleStatusResult(result, "Setting Workitem Int Data", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public static bool SetWorkitemFloatData(this HEU_SessionBase session, HAPI_NodeId node_id, HAPI_PDG_WorkitemId workitem_id, string data_name, float[] values_array, int length)
		{
			HAPI_Result result = HEU_HAPIImportsPDG.HAPI_SetWorkitemFloatData(ref session.GetSessionData()._HAPISession, node_id, workitem_id, data_name, values_array, length);
			session.HandleStatusResult(result, "Setting Workitem Float Data", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public static bool SetWorkitemStringData(this HEU_SessionBase session, HAPI_NodeId node_id, HAPI_PDG_WorkitemId workitem_id, string data_name, int data_index, string value)
		{
			HAPI_Result result = HEU_HAPIImportsPDG.HAPI_SetWorkitemStringData(ref session.GetSessionData()._HAPISession, node_id, workitem_id, data_name, data_index, value);
			session.HandleStatusResult(result, "Setting Workitem String Data", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public static bool CommitWorkitems(this HEU_SessionBase session, HAPI_NodeId node_id)
		{
			HAPI_Result result = HEU_HAPIImportsPDG.HAPI_CommitWorkitems(ref session.GetSessionData()._HAPISession, node_id);
			session.HandleStatusResult(result, "Committing Workitems", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public static bool GetNumWorkItems(this HEU_SessionBase session, HAPI_NodeId node_id, out int num)
		{
			HAPI_Result result = HEU_HAPIImportsPDG.HAPI_GetNumWorkitems(ref session.GetSessionData()._HAPISession, node_id, out num);
			session.HandleStatusResult(result, "Getting Number of Workitems", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public static bool GetWorkitems(this HEU_SessionBase session, HAPI_NodeId node_id, [Out] HAPI_PDG_WorkitemId[] workitem_ids, int length)
		{
			HAPI_Result result = HEU_HAPIImportsPDG.HAPI_GetWorkitems(ref session.GetSessionData()._HAPISession, node_id, workitem_ids, length);
			session.HandleStatusResult(result, "Getting Workitems", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public static bool GetWorkitemsDataLength(this HEU_SessionBase session, HAPI_NodeId node_id, HAPI_PDG_WorkitemId workitem_id, string data_name, out int length)
		{
			HAPI_Result result = HEU_HAPIImportsPDG.HAPI_GetWorkitemDataLength(ref session.GetSessionData()._HAPISession, node_id, workitem_id, data_name, out length);
			session.HandleStatusResult(result, "Getting Workitem Data Length", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public static bool GetWorkitemIntData(this HEU_SessionBase session, HAPI_NodeId node_id, HAPI_PDG_WorkitemId workitem_id, string data_name, [Out] int[] values_array, int length)
		{
			HAPI_Result result = HEU_HAPIImportsPDG.HAPI_GetWorkitemIntData(ref session.GetSessionData()._HAPISession, node_id, workitem_id, data_name, values_array, length);
			session.HandleStatusResult(result, "Getting Workitem Int Data", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public static bool GetWorkitemFloatData(this HEU_SessionBase session, HAPI_NodeId node_id, HAPI_PDG_WorkitemId workitem_id, string data_name, [Out] float[] values_array, int length)
		{
			HAPI_Result result = HEU_HAPIImportsPDG.HAPI_GetWorkitemFloatData(ref session.GetSessionData()._HAPISession, node_id, workitem_id, data_name, values_array, length);
			session.HandleStatusResult(result, "Getting Workitem Float Data", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public static bool GetWorkitemStringData(this HEU_SessionBase session, HAPI_NodeId node_id, HAPI_PDG_WorkitemId workitem_id, string data_name, StringBuilder values, int length)
		{
			Debug.AssertFormat(values.Capacity >= length, "StringBuilder must be atleast of size {0}.", length);
			HAPI_Result result = HEU_HAPIImportsPDG.HAPI_GetWorkitemStringData(ref session.GetSessionData()._HAPISession, node_id, workitem_id, data_name, values, length);
			session.HandleStatusResult(result, "Getting Workitem String Data", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public static bool GetWorkitemResultInfo(this HEU_SessionBase session, HAPI_NodeId node_id, HAPI_PDG_WorkitemId workitem_id, [Out] HAPI_PDG_WorkitemResultInfo[] resultinfo_array, int resultinfo_count)
		{
			HAPI_Result result = HEU_HAPIImportsPDG.HAPI_GetWorkitemResultInfo(ref session.GetSessionData()._HAPISession, node_id, workitem_id, resultinfo_array, resultinfo_count);
			session.HandleStatusResult(result, "Getting Workitem Result Info", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public static bool DirtyPDGNode(this HEU_SessionBase session, HAPI_NodeId node_id, bool clean_results)
		{
			HAPI_Result result = HEU_HAPIImportsPDG.HAPI_DirtyPDGNode(ref session.GetSessionData()._HAPISession, node_id, clean_results);
			session.HandleStatusResult(result, "Dirtying PDG Node", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public static bool PausePDGCook(this HEU_SessionBase session, HAPI_PDG_GraphContextId graph_context_id)
		{
			HAPI_Result result = HEU_HAPIImportsPDG.HAPI_PausePDGCook(ref session.GetSessionData()._HAPISession, graph_context_id);
			session.HandleStatusResult(result, "Pausing PDG Cook", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public static bool CancelPDGCook(this HEU_SessionBase session, HAPI_PDG_GraphContextId graph_context_id)
		{
			HAPI_Result result = HEU_HAPIImportsPDG.HAPI_CancelPDGCook(ref session.GetSessionData()._HAPISession, graph_context_id);
			session.HandleStatusResult(result, "Cancel PDG Cook", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

#endif
	}

}   // HoudiniEngineUnity
