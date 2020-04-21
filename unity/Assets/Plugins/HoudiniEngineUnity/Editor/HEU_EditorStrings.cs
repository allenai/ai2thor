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
	/// Commonly-used strings all in one place.
	/// </summary>
	public static class HEU_EditorStrings
	{

		// Menus
		public const string INPROCESS_SESSION = "In-Process Session";
		public const string RPC_PIPE_SESSION = "Pipe Session";
		public const string RPC_SOCKET_SESSION = "Socket Session";
		public const string GET_SESSION_INFO = "Session Info";
		public const string CLOSE_DEFAULT_SESSION = "Close Default Session";
		public const string CLOSE_ALL_SESSIONS = "Close All Sessions";
		public const string RESTART_SESSION = "Restart Session";
		public const string RECONNECT_TO_SESSION = "Reconnect To Session";

		public const string LOAD_SESSION_FROM_HIP = "Load Session From HIP";
		public const string SAVE_SESSION_TO_HIP = "Save Houdini Scene (.hip)";

		public const string OPEN_SCENE_IN_HOUDINI = "Open Scene In Houdini";

		public const string REVERT_SETTINGS = "Revert To Default";
		public const string RELOAD_SETTINGS = "Reload From Saved File";

		public const string HELP_DOCUMENTATION = "Online Documentation";
		public const string HELP_DOCUMENTATION_URL = "http://www.sidefx.com/docs/unity/";
		public const string HELP_FORUM = "Online Forum";
		public const string HELP_FORUM_URL = "http://www.sidefx.com/forum/50/";
	}

}   // HoudiniEngineUnity
