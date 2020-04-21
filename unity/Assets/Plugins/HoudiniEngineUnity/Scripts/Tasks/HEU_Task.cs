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
	/// Base class for all Houdini Engine Unity tasks.
	/// </summary>
	public abstract class HEU_Task
	{
		public enum TaskStatus
		{
			NONE,
			PENDING_START,
			STARTED,
			REQUIRE_UPDATE,
			PENDING_COMPLETE,
			COMPLETED,
			UNUSED
		}
		public TaskStatus _status;

		public enum TaskResult
		{
			NONE,
			SUCCESS,
			FAILED,
			KILLED
		}
		public TaskResult _result;

		private System.Guid _guid;
		public System.Guid TaskGuid { get { return _guid; } }

		public HEU_Task()
		{
			_guid = System.Guid.NewGuid();
		}

		public delegate void TaskCallback(HEU_Task task);

		public TaskCallback _taskCompletedDelegate;

		public abstract void DoTask();

		public virtual void UpdateTask() { }

		public abstract void KillTask();

		public abstract void CompleteTask(TaskResult result);
	}


	/// <summary>
	/// Asset-specific class for Houdini Engine Unity tasks.
	/// </summary>
	public class HEU_AssetTask : HEU_Task
	{
		public enum BuildType
		{
			NONE,
			LOAD,
			COOK,
			RELOAD
		}
		public BuildType _buildType;

		public HEU_HoudiniAsset _asset;

		public string _assetPath;

		public Vector3 _position = Vector3.zero;

		public bool _buildResult;

		public long _forceSessionID = HEU_SessionData.INVALID_SESSION_ID;

		public HEU_SessionBase GetTaskSession()
		{
			if(_forceSessionID == HEU_SessionData.INVALID_SESSION_ID)
			{
				return HEU_SessionManager.GetOrCreateDefaultSession();
			}
			else
			{
				return HEU_SessionManager.GetSessionWithID(_forceSessionID);
			}
		}


		public override void DoTask()
		{
			if(_buildType == BuildType.LOAD)
			{
				if (string.IsNullOrEmpty(_assetPath))
				{
					// Bad path so fail		
					HEU_TaskManager.CompleteTask(this, TaskResult.FAILED);
				}
				else
				{
					// File-based HDA
					GameObject newGO = HEU_HAPIUtility.InstantiateHDA(_assetPath, _position, GetTaskSession(), true);
					if(newGO != null && newGO.GetComponent<HEU_HoudiniAssetRoot>() != null)
					{
						// Add to post-load callback
						_asset = newGO.GetComponent<HEU_HoudiniAssetRoot>()._houdiniAsset;
						_asset._reloadEvent.AddListener(CookCompletedCallback);
					}
					else
					{
						HEU_TaskManager.CompleteTask(this, TaskResult.FAILED);
					}
				}
			}
			else if(_buildType == BuildType.COOK)
			{
				_asset._cookedEvent.RemoveListener(CookCompletedCallback);
				_asset._cookedEvent.AddListener(CookCompletedCallback);
				_asset.RequestCook(true, true, false, true);
			}
			else if(_buildType == BuildType.RELOAD)
			{
				_asset._reloadEvent.RemoveListener(CookCompletedCallback);
				_asset._reloadEvent.AddListener(CookCompletedCallback);
				_asset.RequestReload(true);
			}
		}

		public override void KillTask()
		{
			if(_asset != null)
			{
				_asset._reloadEvent.RemoveListener(CookCompletedCallback);
				_asset._cookedEvent.RemoveListener(CookCompletedCallback);
			}
		}

		public override void CompleteTask(TaskResult result)
		{
			if (_asset != null)
			{
				_asset._reloadEvent.RemoveListener(CookCompletedCallback);
				_asset._cookedEvent.RemoveListener(CookCompletedCallback);
			}
		}

		private void CookCompletedCallback(HEU_HoudiniAsset asset, bool bSuccess, List<GameObject> outputs)
		{
			if (_status == HEU_Task.TaskStatus.STARTED)
			{
				HEU_TaskManager.CompleteTask(this, bSuccess ? TaskResult.SUCCESS : TaskResult.FAILED);
			}
		}
	}

}   // HoudiniEngineUnity