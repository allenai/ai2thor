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
	/// General task manager for Houdini Engine Unity plugin.
	/// </summary>
#if UNITY_EDITOR && HOUDINIENGINEUNITY_ENABLED
	[InitializeOnLoad]
#endif
	public class HEU_TaskManager
	{
		/// <summary>
		/// Register for the update callback.
		/// Called when scripts are initially loaded.
		/// </summary>
		static HEU_TaskManager()
		{
#if UNITY_EDITOR && HOUDINIENGINEUNITY_ENABLED
			EditorApplication.update += Update;
#endif
		}

		private static List<HEU_Task> _tasks = new List<HEU_Task>();

		private static List<HEU_Task> _pendingAdd = new List<HEU_Task>();
		private static List<HEU_Task> _pendingRemove = new List<HEU_Task>();

		/// <summary>
		/// Process tasks and update their state.
		/// </summary>
		static void Update()
		{
#if HOUDINIENGINEUNITY_ENABLED && HOUDINIENGINEUNITY_ENABLED
			// Deferred removal of tasks.
			if (_pendingRemove.Count > 0)
			{
				foreach (HEU_Task task in _pendingRemove)
				{
					_tasks.Remove(task);
				}
				_pendingRemove.Clear();
			}

			// Deferred addition of tasks.
			if (_pendingAdd.Count > 0)
			{
				foreach (HEU_Task task in _pendingAdd)
				{
					_tasks.Add(task);
				}
				_pendingAdd.Clear();
			}

			if (_tasks.Count > 0)
			{
				// Start of any waiting tasks
				foreach (HEU_Task task in _tasks)
				{
					switch(task._status)
					{
						case HEU_Task.TaskStatus.PENDING_START:
						{
							ExecuteTask(task);
							break;
						}
						case HEU_Task.TaskStatus.REQUIRE_UPDATE:
						{
							task.UpdateTask();
							break;
						}
						case HEU_Task.TaskStatus.PENDING_COMPLETE:
						{
							InternalCompleteTask(task);
							break;
						}
					}
				}
			}
#endif
		}

		public static HEU_Task GetTask(System.Guid taskGuid)
		{
			foreach(HEU_Task task in _tasks)
			{
				if(task.TaskGuid == taskGuid)
				{
					return task;
				}
			}
			return null;
		}

		public static void AddTask(HEU_Task task)
		{
			if (!_tasks.Contains(task) && !_pendingAdd.Contains(task))
			{
				task._status = HEU_Task.TaskStatus.PENDING_START;
				_pendingAdd.Add(task);
			}
		}

		public static void KillTask(HEU_Task task, bool bRemove)
		{
			if(_tasks.Contains(task))
			{
				if(task._status == HEU_Task.TaskStatus.STARTED)
				{
					task.KillTask();
				}

				task._status = HEU_Task.TaskStatus.COMPLETED;
				task._result = HEU_Task.TaskResult.KILLED;
				
				// Note that the complete callback is not invoked if killed
				// because presumably it is most likely the invoker that killed it

				if (bRemove)
				{
					RemoveTask(task);
				}
			}
		}

		public static void KillTask(System.Guid taskGuid, bool bRemove)
		{
			HEU_Task task = GetTask(taskGuid);
			if(task != null)
			{
				KillTask(task, bRemove);
			}
		}

		public static void RemoveTask(HEU_Task task)
		{
			if(_tasks.Contains(task) && !_pendingRemove.Contains(task))
			{
				_pendingRemove.Add(task);
			}
		}

		public static void ExecuteTask(HEU_Task task)
		{
			if (task._status == HEU_Task.TaskStatus.PENDING_START)
			{
				task._status = HEU_Task.TaskStatus.STARTED;
				task.DoTask();
			}
		}

		public static void CompleteTask(HEU_Task task, HEU_Task.TaskResult result)
		{
			if(task._status == HEU_Task.TaskStatus.STARTED)
			{
				// Marking this as pending complete allows to defer
				// the actual 'completion work' resulting in cleaner
				// task management, where tasks aren't adding and removing
				// in the same tick.
				task._status = HEU_Task.TaskStatus.PENDING_COMPLETE;
				task._result = result;
			}
		}

		private static void InternalCompleteTask(HEU_Task task)
		{
			if (task._status == HEU_Task.TaskStatus.PENDING_COMPLETE)
			{
				task._status = HEU_Task.TaskStatus.COMPLETED;

				// Do callbacks
				task.CompleteTask(task._result);
				task._taskCompletedDelegate(task);
			}
		}
	}

}   // HoudiniEngineUnity