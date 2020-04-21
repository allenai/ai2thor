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
	/// General manager for threads created via the plugin for asynchronous work.
	/// Works with HEU_ThreadTasks.
	/// </summary>
	public class HEU_ThreadManager
	{
#pragma warning disable 0649
		private static HEU_ThreadManager _instance;
#pragma warning restore 0649

		public static HEU_ThreadManager Instance
		{
			get
			{
				if (_instance == null)
				{
					CreateInstance();
				}
				return _instance;
			}
		} 

		/* TODO: save to remove, unless issues when reloading scene or code compile
		[InitializeOnLoadMethod]
		[UnityEditor.Callbacks.DidReloadScripts]
		private static void OnScriptsReloaded()
		{
			CreateInstance();
		}
		*/

		private static void CreateInstance()
		{
#if UNITY_EDITOR && HOUDINIENGINEUNITY_ENABLED
			if (_instance == null)
			{
				_instance = new HEU_ThreadManager();
				_instance.Register();
			}
#endif
		}

		~HEU_ThreadManager()
		{
			Unregister();
		}

		public void Register()
		{
#if UNITY_EDITOR && HOUDINIENGINEUNITY_ENABLED
			EditorApplication.update += Update;
#endif
		}

		public void Unregister()
		{
#if UNITY_EDITOR && HOUDINIENGINEUNITY_ENABLED
			EditorApplication.update -= Update;
#endif
		}

		public void Update()
		{
			foreach(HEU_ThreadedTask task in _pendingAdd)
			{
				_tasks.Add(task);
				//Debug.Log("Adding task: " + task.TaskName);
			}
			_pendingAdd.Clear();

			foreach (HEU_ThreadedTask task in _tasks)
			{
				// Checks for complete, then does OnComplete
				task.Update();

				if (!task.IsActive)
				{
					// Removes from this list
					task.Reset();
				}
			}

			foreach (HEU_ThreadedTask task in _pendingRemove)
			{
				_tasks.Remove(task);
				//Debug.Log("Removing task: " + task.TaskName);
			}
			_pendingRemove.Clear();
		}

		public void AddTask(HEU_ThreadedTask task)
		{
			if (!_tasks.Contains(task) && !_pendingAdd.Contains(task))
			{
				_pendingAdd.Add(task);
			}
		}

		public void RemoveTask(HEU_ThreadedTask task)
		{
			if (_tasks.Contains(task) && !_pendingRemove.Contains(task))
			{
				//Debug.Log("Remove task requested: " + task.TaskName);
				_pendingRemove.Add(task);
			}
		}
		


		// List of current tasks (pool)
		private List<HEU_ThreadedTask> _tasks = new List<HEU_ThreadedTask>();

		// List of tasks to add
		private List<HEU_ThreadedTask> _pendingAdd = new List<HEU_ThreadedTask>();

		// List of tasks to remove
		private List<HEU_ThreadedTask> _pendingRemove = new List<HEU_ThreadedTask>();
	}


}   // HoudiniEngineUnity