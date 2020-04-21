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
	/// Base class that wraps threaded tasks.
	/// Derive from this for custom threaded tasks.
	/// </summary>
	public class HEU_ThreadedTask
	{
		/// <summary>
		/// Start the work in a new thread, with priority and background set.
		/// </summary>
		public virtual void Start()
		{
			Debug.Assert(!StopRequested, "StopRequested is true. Task didn't get reset!");
			Debug.Assert(!IsComplete, "IsComplete is true. Task didn't get reset!");

			if (_thread == null)
			{
				// Activate the task first ensuring it won't be stopped until complete or stopped
				IsActive = true;
				IsComplete = false;

				HEU_ThreadManager.Instance.AddTask(this);

				_thread = new System.Threading.Thread(Run);
				_thread.Priority = Priority;
				_thread.IsBackground = IsBackground;
				_thread.Start();
			}
			else
			{
				Debug.LogError("Thread already running!");
			}
		}

		/// <summary>
		/// Request the thread to stop.
		/// </summary>
		public virtual void Stop()
		{
			if (!IsComplete && _thread != null && _thread.IsAlive)
			{
				StopRequested = true;
			}
		}

		/// <summary>
		/// Abort the thread immediately.
		/// </summary>
		public virtual void Abort()
		{
			if (_thread != null)
			{
				_thread.Abort();
				Reset();
			}
		}

		/// <summary>
		/// Reset this wrapper's state.
		/// Aborts running thread.
		/// </summary>
		public virtual void Reset()
		{
			if (_thread != null)
			{
				if (_thread.IsAlive)
				{
					_thread.Abort();
				}
				_thread = null;
			}

			IsComplete = false;
			IsActive = false;
			StopRequested = false;

			CleanUp();

			HEU_ThreadManager.Instance.RemoveTask(this);
		}

		/// <summary>
		/// Check if thread has finished or stopped,
		/// and does callbacks for either state.
		/// </summary>
		public virtual void Update()
		{
			if (!IsActive)
			{
				return;
			}

			if (IsComplete)
			{
				IsActive = false;

				if (StopRequested)
				{
					OnStopped();
				}
				else
				{
					OnComplete();
				}
			}
		}

		/// <summary>
		/// Do the actual work. Derived classes should override this.
		/// Not thread-safe.
		/// </summary>
		protected virtual void DoWork()
		{

		}

		/// <summary>
		/// Callback when task is completed.
		/// </summary>
		protected virtual void OnComplete()
		{

		}

		/// <summary>
		/// Callback when task is stopped.
		/// </summary>
		protected virtual void OnStopped()
		{

		}

		/// <summary>
		/// Clean up this thread wrapper.
		/// </summary>
		protected virtual void CleanUp()
		{

		}

		/// <summary>
		/// Internal thread function to execute the work.
		/// </summary>
		private void Run()
		{
			DoWork();

			IsComplete = true;
		}


		//	DATA ------------------------------------------------------------------------------------------------------

		// Whether the work is completed
		private bool _isComplete = false;

		// Whether the thread is still running
		private bool _isActive = false;

		// Whether stop has been qurested
		private bool _stopRequested = false;

		// Thread lock handle
		private object _lockHandle = new object();

		// Actual thread object
		private System.Threading.Thread _thread;

		// Thread priority
		private System.Threading.ThreadPriority _priority = System.Threading.ThreadPriority.Lowest;

		// Whether thread is running in the background
		private bool _isBackground = true;

		// Name of this wrapper instance or task name
		protected string _name;
		public string TaskName { get { return _name; } }

		/// <summary>
		/// Get or set task is complete
		/// </summary>
		public bool IsComplete
		{
			get
			{
				bool bTempComplete = false;
				lock (_lockHandle)
				{
					bTempComplete = _isComplete;
				}
				return bTempComplete;
			}

			set
			{
				lock (_lockHandle)
				{
					_isComplete = value;
				}
			}
		}

		/// <summary>
		/// Get or set work is still executing (thread running)
		/// </summary>
		public bool IsActive
		{
			get
			{
				bool bTempActive = false;
				lock (_lockHandle)
				{
					bTempActive = _isActive;
				}
				return bTempActive;
			}

			set
			{
				lock (_lockHandle)
				{
					_isActive = value;
				}
			}
		}

		/// <summary>
		/// Get or set request for thread to stop
		/// </summary>
		public bool StopRequested
		{
			get
			{
				bool bTempStop = false;
				lock (_lockHandle)
				{
					bTempStop = _stopRequested;
				}
				return bTempStop;
			}

			set
			{
				lock (_lockHandle)
				{
					_stopRequested = value;
				}
			}
		}

		/// <summary>
		/// Get or set thread priority
		/// </summary>
		public System.Threading.ThreadPriority Priority
		{
			get
			{
				return _priority;
			}
			set
			{
				_priority = value;
			}
		}

		/// <summary>
		/// Get or set thread should run in background or not
		/// </summary>
		public bool IsBackground
		{
			get
			{
				return _isBackground;
			}
			set
			{
				_isBackground = value;
			}
		}
	}


}   // HoudiniEngineUnity