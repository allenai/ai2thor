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
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace HoudiniEngineUnity
{
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Typedefs (copy these from HEU_Common.cs)
	using HAPI_Int64 = System.Int64;
	using HAPI_StringHandle = System.Int32;
	using HAPI_ErrorCodeBits = System.Int32;
	using HAPI_AssetLibraryId = System.Int32;
	using HAPI_NodeId = System.Int32;
	using HAPI_NodeTypeBits = System.Int32;
	using HAPI_NodeFlagsBits = System.Int32;
	using HAPI_ParmId = System.Int32;
	using HAPI_PartId = System.Int32;
	using HAPI_PDG_WorkitemId = System.Int32;
	using HAPI_PDG_GraphContextId = System.Int32;


	public class HEU_SessionHAPI : HEU_SessionBase
	{

#if HOUDINIENGINEUNITY_ENABLED

		// ASSET REGISTRATION -----------------------------------------------------------------------------------------------

		private Dictionary<HAPI_NodeId, HEU_HoudiniAsset> _idToGameObjectMap = new Dictionary<HAPI_NodeId, HEU_HoudiniAsset>();

		public override HEU_HoudiniAsset GetRegisteredAssetFromID(HAPI_NodeId id)
		{
			HEU_HoudiniAsset foundObject = null;
			_idToGameObjectMap.TryGetValue(id, out foundObject);
			return foundObject;
		}

		public override bool IsAssetRegistered(HEU_HoudiniAsset asset)
		{
			HEU_HoudiniAsset registeredAsset = GetRegisteredAssetFromID(asset.AssetID);
			return (registeredAsset != null && registeredAsset == asset);
		}

		public override void RegisterAsset(HEU_HoudiniAsset asset)
		{
			if (asset.AssetID != HEU_Defines.HEU_INVALID_NODE_ID && !_idToGameObjectMap.ContainsKey(asset.AssetID))
			{
				_idToGameObjectMap.Add(asset.AssetID, asset);
			}
		}

		public override void UnregisterAsset(HAPI_NodeId id)
		{
			if (id != HEU_Defines.HEU_INVALID_NODE_ID && _idToGameObjectMap.ContainsKey(id))
			{
				_idToGameObjectMap.Remove(id);
			}
		}

		public override void ReregisterOnAwake(HEU_HoudiniAsset asset)
		{
			if (asset.AssetID != HEU_Defines.HEU_INVALID_NODE_ID)
			{
				if(_idToGameObjectMap.ContainsKey(asset.AssetID) && _idToGameObjectMap[asset.AssetID] == null)
				{
					_idToGameObjectMap[asset.AssetID] = asset;
				}
				else
				{
					asset.InvalidateAsset();
				}
			}
		}

		// SESSION ----------------------------------------------------------------------------------------------------

		/// <summary>
		/// Create new session data if specified.
		/// </summary>
		/// <param name="bOverwriteExisting">True if overwrite existing session data. Note it does not close existing.</param>
		/// /// <param name="bIsDefaultSession">Is this the current default session?</param>
		/// <returns>True if created new session. False if session already exists.</returns>
		protected override bool CreateSessionData(bool bOverwriteExisting, bool bIsDefaultSession)
		{
			// Make sure we are only overwriting existing session data if caller asked
			if (_sessionData != null && !bOverwriteExisting)
			{
				return false;
			}

			_sessionData = new HEU_SessionData();
			_sessionData.SessionClassType = this.GetType();
			_sessionData.IsDefaultSession = bIsDefaultSession;
			return true;
		}

		/// <summary>
		/// Create in-process Houdini Engine session.
		/// </summary>
		/// <returns>True if session creation succeeded.</returns>
		public override bool CreateInProcessSession(bool isDefaultSession)
		{
			CheckAndCloseExistingSession();

			try
			{
				if (!CreateSessionData(true, isDefaultSession))
				{
					return false;
				}

				// Start at failed since this is several steps. Once connected, we can set it as such.
				ConnectedState = SessionConnectionState.FAILED_TO_CONNECT;

				HAPI_Result result = HEU_HAPIImports.HAPI_CreateInProcessSession(out _sessionData._HAPISession);
				if (result != HAPI_Result.HAPI_RESULT_SUCCESS)
				{
					SetSessionErrorMsg(string.Format("Unable to start in-process session.\n Make sure {0} exists.", HEU_Platform.LibPath), true);
					return false;
				}

				Debug.LogFormat("Houdini Engine: Created In-Process session with ID {0}.", _sessionData.SessionID);

				// Make sure API version matches with plugin version
				if (!CheckVersionMatch())
				{
					return false;
				}

				return InitializeSession(_sessionData);
			}
			catch (System.Exception ex)
			{
				if (ex is System.DllNotFoundException || ex is System.EntryPointNotFoundException)
				{
					SetSessionErrorMsg(string.Format("Creating Houdini Engine session resulted in exception: {0}", ex.ToString()), true);
				}
				else
				{
					throw;
				}
			}
			return false;
		}

		/// <summary>
		/// Create and connect socket session for Houdini Engine.
		/// </summary>
		/// <param name="hostName">Network name of the host.</param>
		/// <param name="serverPort">Network port of the host.</param>
		/// <param name="autoClose"></param>
		/// <param name="timeout"></param>
		/// <returns>True if successfully created session.</returns>
		public override bool CreateThriftSocketSession(bool bIsDefaultSession, string hostName, int serverPort, bool autoClose, float timeout, bool bLogError)
		{
			try
			{
				return InternalConnectThriftSocketSession(true, hostName, serverPort, autoClose, timeout, bIsDefaultSession);
			}
			catch (System.Exception ex)
			{
				if (ex is System.DllNotFoundException || ex is System.EntryPointNotFoundException)
				{
					SetSessionErrorMsg(string.Format("Unable to create session due to Houdini Engine libraries not found!"), true);
				}
				else
				{
					throw;
				}
			}
			return false;
		}

		/// <summary>
		/// Connect socket session for Houdini Engine.
		/// Creates session if specified.
		/// </summary>
		/// <param name="bCreateSession">Create the session before connecting.</param>
		/// <param name="hostName">Network name of the host.</param>
		/// <param name="serverPort">Network port of the host.</param>
		/// <param name="autoClose"></param>
		/// <param name="timeout"></param>
		/// <returns>True if successfully connected session.</returns>
		private bool InternalConnectThriftSocketSession(bool bCreateSession, string hostName, int serverPort, bool autoClose, float timeout, bool bIsDefaultSession)
		{
			CheckAndCloseExistingSession();
			if (!CreateSessionData(true, bIsDefaultSession))
			{
				return false;
			}

			int processID = 0;
			HAPI_Result result;

			// Start at failed since this is several steps. Once connected, we can set it as such.
			ConnectedState = SessionConnectionState.FAILED_TO_CONNECT;

			string sessionConnectionErrorMsg = string.Format("\nHost name: {0}"
				+ "\nPort: {1}"
				+ "\nCheck Session information in Plugin Settings."
				+ "\nCheck that {2} exists.",
				hostName, serverPort, HEU_Platform.LibPath);

			if (bCreateSession)
			{
				// First create the socket server
				HAPI_ThriftServerOptions serverOptions = new HAPI_ThriftServerOptions();
				serverOptions.autoClose = autoClose;
				serverOptions.timeoutMs = timeout;

				result = HEU_HAPIImports.HAPI_StartThriftSocketServer(ref serverOptions, serverPort, out processID);
				if (result != HAPI_Result.HAPI_RESULT_SUCCESS)
				{
					SetSessionErrorMsg(string.Format("Unable to start socket server.\nError Code: {1}\n{0}", result, sessionConnectionErrorMsg), true);
					return false;
				}
			}

			// Then create the session
			_sessionData._HAPISession.type = HAPI_SessionType.HAPI_SESSION_THRIFT;
			result = HEU_HAPIImports.HAPI_CreateThriftSocketSession(out _sessionData._HAPISession, hostName, serverPort);
			if (result != HAPI_Result.HAPI_RESULT_SUCCESS)
			{
				string debugMsg = "";
				if (!bCreateSession)
				{
					debugMsg = "\n\nMake sure you started Houdini Engine Debugger in Houdini (Windows -> Houdini Engine Debugger)";
				}

				SetSessionErrorMsg(string.Format("Unable to create socket session.\nError Code: {0}\n{1}{2}", result, sessionConnectionErrorMsg, debugMsg), true);
				return false;
			}

			Debug.LogFormat("Houdini Engine: Created Socket session with ID {0}.", _sessionData.SessionID);

			// Make sure API version matches with plugin version
			if (!CheckVersionMatch())
			{
				return false;
			}

			return InitializeSession(_sessionData);
		}

		/// <summary>
		/// Create and connect pipe session for Houdini Engine.
		/// </summary>
		/// <param name="pipeName"></param>
		/// <param name="autoClose"></param>
		/// <param name="timeout"></param>
		/// <returns>True if successfully created session.</returns>
		public override bool CreateThriftPipeSession(bool bIsDefaultSession, string pipeName, bool autoClose, float timeout, bool bLogError)
		{
			try
			{
				return InternalCreateThriftPipeSession(true, pipeName, autoClose, timeout, bIsDefaultSession);
			}
			catch (System.Exception ex)
			{
				if (ex is System.DllNotFoundException || ex is System.EntryPointNotFoundException)
				{
					SetSessionErrorMsg(string.Format("Unable to create session due to Houdini Engine libraries not found!"), bLogError);
				}
				else
				{
					throw;
				}
			}
			return false;
		}

		/// <summary>
		/// Connect to pipe session for Houdini Engine.
		/// Create session first if specified.
		/// </summary>
		/// <param name="bCreateSession">Create the session if specified.</param>
		/// <param name="pipeName">Name of the pipe.</param>
		/// <param name="autoClose"></param>
		/// <param name="timeout"></param>
		/// <returns>True if successfully created session.</returns>
		private bool InternalCreateThriftPipeSession(bool bCreateSession, string pipeName, bool autoClose, float timeout, bool bIsDefaultSession)
		{
			CheckAndCloseExistingSession();
			if (!CreateSessionData(true, bIsDefaultSession))
			{
				return false;
			}

			int processID = 0;
			HAPI_Result result;

			_sessionData.PipeName = pipeName;

			// Start at failed since this is several steps. Once connected, we can set it as such.
			ConnectedState = SessionConnectionState.FAILED_TO_CONNECT;

			string sessionConnectionErrorMsg = string.Format("\nPipe name: {0}."
				+ "\nCheck Session information in Plugin Settings."
				+ "\nCheck that {1} exists.",
				pipeName, HEU_Platform.LibPath);

			if (bCreateSession)
			{
				// First create the pipe server
				HAPI_ThriftServerOptions serverOptions = new HAPI_ThriftServerOptions();
				serverOptions.autoClose = autoClose;
				serverOptions.timeoutMs = timeout;

				result = HEU_HAPIImports.HAPI_StartThriftNamedPipeServer(ref serverOptions, pipeName, out processID);
				if (result != HAPI_Result.HAPI_RESULT_SUCCESS)
				{
					SetSessionErrorMsg(string.Format("Unable to start RPC server.\nError Code: {0}\n{1}", result, sessionConnectionErrorMsg));
					return false;
				}
			}

			_sessionData.ProcessID = processID;

			// Then create the pipe session
			_sessionData._HAPISession.type = HAPI_SessionType.HAPI_SESSION_THRIFT;
			result = HEU_HAPIImports.HAPI_CreateThriftNamedPipeSession(out _sessionData._HAPISession, pipeName);
			if (result != HAPI_Result.HAPI_RESULT_SUCCESS)
			{
				string debugMsg = "";
				if (!bCreateSession)
				{
					debugMsg = "\n\nMake sure you started Houdini Engine Debugger in Houdini (Windows -> Houdini Engine Debugger)";
				}

				SetSessionErrorMsg(string.Format("Unable to create RPC pipe session.\nError Code: {0}\n{1}{2}", result, sessionConnectionErrorMsg, debugMsg));
				return false;
			}

			Debug.LogFormat("Houdini Engine: Created Pipe session with ID {0}.", _sessionData.SessionID);

			// Make sure API version matches with plugin version
			if (!CheckVersionMatch())
			{
				return false;
			}

			return InitializeSession(_sessionData);
		}

		public override bool CreateCustomSession(bool bIsDefaultSession)
		{
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// Connect to (debug) Houdini session over socket.
		/// </summary>
		/// <param name="hostName"></param>
		/// <param name="serverPort"></param>
		/// <param name="autoClose"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		public override bool ConnectThriftSocketSession(bool bIsDefaultSession, string hostName, int serverPort, bool autoClose, float timeout)
		{
			return InternalConnectThriftSocketSession(false, hostName, serverPort, autoClose, timeout, bIsDefaultSession);
		}

		/// <summary>
		/// Connect to (debug) Houdini session over pipe.
		/// </summary>
		/// <param name="pipeName"></param>
		/// <param name="autoClose"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		public override bool ConnectThriftPipeSession(bool bIsDefaultSession, string pipeName, bool autoClose, float timeout)
		{
			return InternalCreateThriftPipeSession(false, pipeName, autoClose, timeout, bIsDefaultSession);
		}

		/// <summary>
		/// Close the existing session.
		/// </summary>
		/// <returns>True if successfully closed session.</returns>
		public override bool CloseSession()
		{
			if (_sessionData != null)
			{
				// Always unregister so that we don't leave behind persistent zombie session data
				HEU_SessionManager.UnregisterSession(_sessionData.SessionID);

				try
				{
					if (IsSessionValid())
					{
						HAPI_Result result = HEU_HAPIImports.HAPI_Cleanup(ref _sessionData._HAPISession);
						if (result != HAPI_Result.HAPI_RESULT_SUCCESS)
						{
							HandleStatusResult(result, "Clean Up Session", false, true);
						}

						result = HEU_HAPIImports.HAPI_CloseSession(ref _sessionData._HAPISession);
						if (result != HAPI_Result.HAPI_RESULT_SUCCESS)
						{
							// Probably not possible to query more info about the session error as it might be in an invalid state.
							// Just clear our own session, and flag the user that there was an error on closing
							SetSessionErrorMsg(string.Format("Closing session resulted in error (result code: {0})", result));
						}
					}
				}
				catch (System.Exception ex)
				{
					if (ex is System.DllNotFoundException || ex is System.EntryPointNotFoundException)
					{
						SetSessionErrorMsg(string.Format("Unable to close session due to Houdini Engine libraries not found!"), true);
					}
					else
					{
						throw;
					}
				}

				// Always clear session info 
				ClearSessionInfo();
				return true;
			}
			else
			{
				SetSessionErrorMsg(HEU_Defines.NO_EXISTING_SESSION);
			}
			return false;
		}

		/// <summary>
		/// Closes session if one exists and is valid. Returns true even if session is invalid.
		/// Trying to close invalid session might throw error so this bypasses it.
		/// </summary>
		/// <returns>Only returns false if closing existing session failed.</returns>
		protected override bool CheckAndCloseExistingSession()
		{
			if (_sessionData != null && IsSessionValid())
			{
				// Because we already checked that the session exists, this should only return false if it can't be closed.
				return CloseSession();
			}
			return true;
		}

		/// <summary>
		/// Clears the session info locally and on disk.
		/// </summary>
		protected override void ClearSessionInfo()
		{
			if (_sessionData != null)
			{
				_sessionData.SessionID = -1;
				_sessionData.ProcessID = -1;
				_sessionData.PipeName = "";
				_sessionData = null;
			}
		}

		/// <summary>
		/// Return the session info.
		/// </summary>
		/// <returns>The session information as a formatted string.</returns>
		public override string GetSessionInfo()
		{
			if (_sessionData != null)
			{
				StringBuilder sb = new StringBuilder();

				sb.AppendFormat("Session ID: {0}\nSession Type: {1}", _sessionData.SessionID, _sessionData.SessionType);

				if (_sessionData.ProcessID > 0)
				{
					sb.AppendFormat("\nProcess ID: {0}", _sessionData.ProcessID);
				}

				return sb.ToString();
			}
			return HEU_Defines.NO_EXISTING_SESSION;
		}

		/// <summary>
		/// Checks that the Houdini Engine session is valid.
		/// The bCheckHAPI flag checks via HAPI for actual session inside Houdini.
		/// </summary>
		/// <returns>True if this session is valid.</returns>
		public override bool IsSessionValid()
		{
			if (_sessionData != null && ConnectedState != SessionConnectionState.FAILED_TO_CONNECT)
			{
				try
				{
					HAPI_Result result = HEU_HAPIImports.HAPI_IsSessionValid(ref _sessionData._HAPISession);
					return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
				}
				// In most cases, this call fails due to HAPI libraries not found, so catch and handle gracefully
				catch (System.DllNotFoundException ex)
				{
					SetSessionErrorMsg(ex.ToString(), true);
				}
			}
			return false;
		}

		/// <summary>
		/// Close current (if valid) and open a new session.
		/// </summary>
		/// <returns>True if created a new session.</returns>
		public override bool RestartSession()
		{
			HAPI_SessionType sessionType = HAPI_SessionType.HAPI_SESSION_THRIFT;
			int processID = -1;
			if (IsSessionValid())
			{
				sessionType = _sessionData.SessionType;
				processID = _sessionData.ProcessID;

				CheckAndCloseExistingSession();
			}

			if (sessionType == HAPI_SessionType.HAPI_SESSION_THRIFT && processID > 0)
			{
				return CreateThriftSocketSession(true, HEU_PluginSettings.Session_Localhost, HEU_PluginSettings.Session_Port, HEU_PluginSettings.Session_AutoClose, HEU_PluginSettings.Session_Timeout, true);
			}
			else if (sessionType == HAPI_SessionType.HAPI_SESSION_INPROCESS)
			{
				return CreateInProcessSession(true);
			}

			// Default session. On Linux use socket due to issues with pipe.
#if UNITY_STANDALONE_LINUX
			return CreateThriftSocketSession(true, HEU_PluginSettings.Session_Localhost, HEU_PluginSettings.Session_Port, HEU_PluginSettings.Session_AutoClose, HEU_PluginSettings.Session_Timeout, true);
#else
			return CreateThriftPipeSession(true, HEU_PluginSettings.Session_PipeName, HEU_PluginSettings.Session_AutoClose, HEU_PluginSettings.Session_Timeout, true);
#endif
		}

		/// <summary>
		/// Check that the Unity plugin's Houdini Engine version matches with the linked Houdini Engine API version.
		/// </summary>
		/// <returns>True if the versions match.</returns>
		public override bool CheckVersionMatch()
		{
			int heuMajor = GetEnvInt(HAPI_EnvIntType.HAPI_ENVINT_VERSION_HOUDINI_ENGINE_MAJOR);
			int heuMinor = GetEnvInt(HAPI_EnvIntType.HAPI_ENVINT_VERSION_HOUDINI_ENGINE_MINOR);
			int heuAPI = GetEnvInt(HAPI_EnvIntType.HAPI_ENVINT_VERSION_HOUDINI_ENGINE_API);
			
			if (heuMajor != HEU_HoudiniVersion.HOUDINI_ENGINE_MAJOR || heuMinor != HEU_HoudiniVersion.HOUDINI_ENGINE_MINOR || heuAPI != HEU_HoudiniVersion.HOUDINI_ENGINE_API)
			{
				SetSessionErrorMsg(string.Format("This plugin's Houdini Engine API version does not match the found API version:" +
					"\n  Built: {0}.{1}.{2}." +
					"\n  Found: {3}.{4}.{5}." +
					"\n\nThe API version must match in order to use the plugin." +
					"\nEither update the plugin package, or change Houdini install version in Plugin Settings.\n" +
					"\nRestarting Unity is required if you have already done the above.",
									HEU_HoudiniVersion.HOUDINI_ENGINE_MAJOR,
									HEU_HoudiniVersion.HOUDINI_ENGINE_MINOR,
									HEU_HoudiniVersion.HOUDINI_ENGINE_API,
									heuMajor,
									heuMinor,
									heuAPI));
				return false;
			}

			return true;
		}

		/// <summary>
		/// Initialize the HAPI session. Session must have already been created.
		/// </summary>
		/// <param name="sessionData">The Houdini Engine session to initliaze</param>
		/// <returns>True if session was successfully initialized.</returns>
		private bool InitializeSession(HEU_SessionData sessionData)
		{
			HAPI_CookOptions cookOptions = new HAPI_CookOptions();
			GetCookOptions(ref cookOptions);

			string HDASearchPath = HEU_Platform.GetAllFoldersInPath(HEU_Defines.HEU_ENGINE_ASSETS + "/HDAs");
			string DSOSearchPath = HEU_Platform.GetAllFoldersInPath(HEU_Defines.HEU_ENGINE_ASSETS + "/DSOs");
			string environmentFilePath = HEU_Platform.GetHoudiniEngineEnvironmentFilePathFull();

			HAPI_Result result = HEU_HAPIImports.HAPI_Initialize(ref sessionData._HAPISession, ref cookOptions, true, -1, environmentFilePath, HDASearchPath, DSOSearchPath, DSOSearchPath, DSOSearchPath);
			if (result != HAPI_Result.HAPI_RESULT_ALREADY_INITIALIZED && result != HAPI_Result.HAPI_RESULT_SUCCESS)
			{
				HandleStatusResult(result, "Session Initialize failed.", true, true);
				sessionData.IsInitialized = false;
				return false;
			}

			SetServerEnvString(HEU_Defines.HAPI_ENV_CLIENT_NAME, "unity");

			sessionData.IsInitialized = true;
			ConnectedState = SessionConnectionState.CONNECTED;

			HEU_SessionManager.RegisterSession(_sessionData.SessionID, this);

			HEU_PluginSettings.LastHoudiniVersion = HEU_HoudiniVersion.HOUDINI_VERSION_STRING;

			return true;
		}

		/// <summary>
		/// Handle result received from a HAPI call. If there was an error in the result, it tries to find out more information.
		/// Nothing done for a successful result.
		/// </summary>
		/// <param name="result"></param>
		/// <param name="prependMsg"></param>
		/// <param name="bThrowError"></param>
		/// <param name="bLogError"></param>
		/// <returns></returns>
		public override bool HandleStatusResult(HAPI_Result result, string prependMsg, bool bThrowError, bool bLogError)
		{
			if (result != HAPI_Result.HAPI_RESULT_SUCCESS)
			{
				//int resultCode;
				//HEU_HAPIWrapper.HAPI_GetStatus(ref sessionData._HAPISession, HAPI_StatusType.HAPI_STATUS_CALL_RESULT, out resultCode);

				string statusMessage = GetStatusString(HAPI_StatusType.HAPI_STATUS_CALL_RESULT, HAPI_StatusVerbosity.HAPI_STATUSVERBOSITY_WARNINGS);
				string errorMsg = string.Format("{0} : {1}\nIf session is invalid, try restarting Unity.", prependMsg, statusMessage);
				SetSessionErrorMsg(errorMsg, bLogError);

				if (ThrowErrorOverride && bThrowError)
				{
					throw new HEU_HoudiniEngineError(errorMsg);
				}

				return false;
			}
			else
			{
				return true;
			}
		}

		/// <summary>
		/// Returns default cook options for HAPI.
		/// </summary>
		/// <returns>Default cook options</returns>
		private void GetCookOptions(ref HAPI_CookOptions cookOptions)
		{
			// In keeping consistency with other plugins, we don't support splitting by groups or attributes.
			// Though allowing it now behind an option.
			cookOptions.splitGeosByGroup = HEU_PluginSettings.CookOptionSplitGeosByGroup;
			cookOptions.splitGeosByAttribute = false;
			cookOptions.splitAttrSH = 0;
			cookOptions.splitPointsByVertexAttributes = false;

			cookOptions.cookTemplatedGeos = HEU_PluginSettings.CookTemplatedGeos;
			cookOptions.maxVerticesPerPrimitive = HEU_PluginSettings.MaxVerticesPerPrimitive;
			cookOptions.refineCurveToLinear = HEU_Defines.HAPI_CURVE_REFINE_TO_LINEAR;
			cookOptions.curveRefineLOD = HEU_Defines.HAPI_CURVE_LOD;
			cookOptions.packedPrimInstancingMode = HAPI_PackedPrimInstancingMode.HAPI_PACKEDPRIM_INSTANCING_MODE_FLAT;

			cookOptions.handleBoxPartTypes = HEU_PluginSettings.SupportHoudiniBoxType;
			cookOptions.handleSpherePartTypes = HEU_PluginSettings.SupportHoudiniSphereType;
		}

		// ENVIRONMENT ------------------------------------------------------------------------------------------------

		/// <summary>
		/// Set environment variable for the server process as a string.
		/// </summary>
		/// <param name="name">Name of variable.</param>
		/// <param name="value">String value.</param>
		public override void SetServerEnvString(string name, string value)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_SetServerEnvString(ref _sessionData._HAPISession, name, value);
			HandleStatusResult(result, "Set Server Environment", true, true);
		}

		public override bool GetServerEnvString(string name, out string value)
		{
			value = null;
			HAPI_StringHandle stringHandle;
			HAPI_Result result = HEU_HAPIImports.HAPI_GetServerEnvString(ref _sessionData._HAPISession, name, out stringHandle);
			if (result == HAPI_Result.HAPI_RESULT_SUCCESS)
			{
				int bufferLength = 0;
				result = HEU_HAPIImports.HAPI_GetStringBufLength(ref _sessionData._HAPISession, stringHandle, out bufferLength);
				if (result == HAPI_Result.HAPI_RESULT_SUCCESS)
				{
					if (bufferLength <= 0)
					{
						value = "";
					}
					else
					{
						StringBuilder strBuilder = new StringBuilder(bufferLength);
						result = HEU_HAPIImports.HAPI_GetString(ref _sessionData._HAPISession, stringHandle, strBuilder, bufferLength);
						if (result == HAPI_Result.HAPI_RESULT_SUCCESS)
						{
							value = strBuilder.ToString();
						}
					}
				}
			}
			HandleStatusResult(result, "Get Server Environment String", true, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetServerEnvVarCount(out int env_count)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetServerEnvVarCount(ref _sessionData._HAPISession, out env_count);
			HandleStatusResult(result, "Get Server Environment Var Count", true, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Gives back the status code for a specific status type
		/// </summary>
		/// <param name="statusType">Status type to query</param>
		/// <param name="statusCode">Result status code</param>
		/// <returns>True if successfully queried status</returns>
		public override bool GetStatus(HAPI_StatusType statusType, out HAPI_State statusCode)
		{
			int status = 0;
			HAPI_Result result = HEU_HAPIImports.HAPI_GetStatus(ref _sessionData._HAPISession, statusType, out status);
			if (result != HAPI_Result.HAPI_RESULT_SUCCESS)
			{
				SetSessionErrorMsg(string.Format("HAPI_GetStatus failed!"));
			}

			statusCode = (HAPI_State)status;
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Return the status string from HAPI session.
		/// </summary>
		/// <param name="statusType"></param>
		/// <param name="verbosity"></param>
		/// <returns>True if successfully queried status string</returns>
		public override string GetStatusString(HAPI_StatusType statusType, HAPI_StatusVerbosity verbosity)
		{
			int bufferLength = 0;
			HAPI_Result result = HEU_HAPIImports.HAPI_GetStatusStringBufLength(ref _sessionData._HAPISession, statusType, verbosity, out bufferLength);
			if (result != HAPI_Result.HAPI_RESULT_SUCCESS)
			{
				return "Failed to get status string. Likely the session is invalid.";
			}

			if (bufferLength <= 0)
			{
				return "";
			}

			StringBuilder strBuilder = new StringBuilder(bufferLength);
			result = HEU_HAPIImports.HAPI_GetStatusString(ref _sessionData._HAPISession, statusType, strBuilder, bufferLength);
			if (result != HAPI_Result.HAPI_RESULT_SUCCESS)
			{
				return "Failed to get status string. Likely the session is invalid.";
			}

			return strBuilder.ToString();
		}

		/// <summary>
		/// Returns environment value in Houdini Engine.
		/// </summary>
		/// <param name="intType">Type of environment variable</param>
		/// <returns>Value of environment variable</returns>
		public override int GetEnvInt(HAPI_EnvIntType intType)
		{
			int value;
			HAPI_Result result = HEU_HAPIImports.HAPI_GetEnvInt(intType, out value);
			HandleStatusResult(result, "Getting Environment Value", false, true);

			return value;
		}

		/// <summary>
		/// Return the session environment variable.
		/// </summary>
		/// <param name="intType">Type of environment variable.</param>
		/// <returns>Value of environment variable</returns>
		public override int GetSessionEnvInt(HAPI_SessionEnvIntType intType, bool bLogError)
		{
			int value;
			HAPI_Result result = HEU_HAPIImports.HAPI_GetSessionEnvInt(ref _sessionData._HAPISession, intType, out value);
			HandleStatusResult(result, "Getting Session Environment Value", false, bLogError);

			return value;
		}

		/// <summary>
		/// Get the string value for the associated string handle.
		/// </summary>
		/// <param name="stringHandle">Handle to look up.</param>
		/// <param name="stringBuilder">Container for return value.</param>
		/// <param name="bufferLength">Length of return value</param>
		/// <returns>True if it has successfully populated the string value.</returns>
		public override bool GetString(HAPI_StringHandle stringHandle, StringBuilder stringBuilder, int bufferLength)
		{
			Debug.AssertFormat(stringBuilder.Capacity >= bufferLength, "StringBuilder must be atleast of size {0}.", bufferLength);
			HAPI_Result result = HEU_HAPIImports.HAPI_GetString(ref _sessionData._HAPISession, stringHandle, stringBuilder, bufferLength);
			HandleStatusResult(result, "Getting String Value", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Returns the length of the string value for the given handle.
		/// </summary>
		/// <param name="stringHandle">Handle of the string to query</param>
		/// <returns>Buffer of the length of the queried string</returns>
		public override int GetStringBufferLength(HAPI_StringHandle stringHandle)
		{
			int bufferLength = 0;
			HAPI_Result result = HEU_HAPIImports.HAPI_GetStringBufLength(ref _sessionData._HAPISession, stringHandle, out bufferLength);
			HandleStatusResult(result, "Getting String Buffer Length", false, true);
			return bufferLength;
		}

		/// <summary>
		/// Checks for and returns specific errors on node.
		/// </summary>
		/// <param name="nodeID">Node to check</param>
		/// <param name="errorsToCheck">Specific errors to check for</param>
		/// <returns>Errors found on node</returns>
		public override HAPI_ErrorCodeBits CheckForSpecificErrors(HAPI_NodeId nodeID, HAPI_ErrorCodeBits errorsToCheck)
		{
			HAPI_ErrorCodeBits errorsFound;
			HAPI_Result result = HEU_HAPIImports.HAPI_CheckForSpecificErrors(ref _sessionData._HAPISession, nodeID, errorsToCheck, out errorsFound);
			HandleStatusResult(result, "Check For Specific Errors", false, true);
			return errorsFound;
		}

		// ASSETS -----------------------------------------------------------------------------------------------------

		/// <summary>
		/// Load given asset file in Houdini Engine.
		/// Note that this might show a dialog if there is an existing matching asset definition.
		/// </summary>
		/// <param name="assetPath">Path to the asset</param>
		/// <param name="bAllowOverwrite">Whether to overwrite an existing matching asset definition</param>
		/// <param name="libraryID">ID of the asset in the library</param>
		/// <returns>True if successfully loaded the asset</returns>
		public override bool LoadAssetLibraryFromFile(string assetPath, bool bAllowOverwrite, out HAPI_StringHandle libraryID)
		{
			// Make the asset path a full path as otherwise debug sessions will not load the asset properly in Houdini.
			if (HEU_AssetDatabase.IsPathRelativeToAssets(assetPath))
			{
				assetPath = HEU_AssetDatabase.GetAssetFullPath(assetPath);
			}

			libraryID = 0;
			HAPI_Result result = HEU_HAPIImports.HAPI_LoadAssetLibraryFromFile(ref _sessionData._HAPISession, assetPath, bAllowOverwrite, out libraryID);
			if (result == HAPI_Result.HAPI_RESULT_ASSET_DEF_ALREADY_LOADED)
			{
				if (HEU_EditorUtility.DisplayDialog("Houdini Asset Definition Overwrite",
									"The asset file being loaded (" + assetPath + ") contains asset defintions " +
									"that have already been loaded from another asset library file.\n" +
									"Would you like to overwrite them?", "Yes", "No"))
				{
					result = HEU_HAPIImports.HAPI_LoadAssetLibraryFromFile(ref _sessionData._HAPISession, assetPath, true, out libraryID);
				}
			}
			HandleStatusResult(result, "Loading Asset Library From File", false, true);

			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Creates a node inside a node network.
		/// </summary>
		/// <param name="parentNodeID">Parent node network's node ID or -1 if at top level</param>
		/// /// <param name="operatorName">Name of the node operator type</param>
		/// <param name="nodeLabel">Label for newly created node</param>
		/// <param name="bCookOnCreation">Whether should cook on creation or not</param>
		/// <param name="newNodeID">New node's ID</param>
		/// <returns>True if successfully created a new node</returns>
		public override bool CreateNode(HAPI_StringHandle parentNodeID, string operatorName, string nodeLabel, bool bCookOnCreation, out HAPI_NodeId newNodeID)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_CreateNode(ref _sessionData._HAPISession, parentNodeID, operatorName, nodeLabel, bCookOnCreation, out newNodeID);
			HandleStatusResult(result, "Create Node", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}


		/// <summary>
		/// Delete specified Houdini Engine node.
		/// </summary>
		/// <param name="nodeID">Node to delete</param>
		public override void DeleteNode(HAPI_NodeId nodeID)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_DeleteNode(ref _sessionData._HAPISession, nodeID);
			HandleStatusResult(result, "Delete Node", false, true);
		}

		/// <summary>
		/// Cook the given node. This may trigger cooks on other nodes if connected.
		/// </summary>
		/// <param name="nodeID">ID of the node to cook</param>
		/// <param name="bCookTemplatedGeos">Whether to recursively cook all templated geos or not</param>
		/// <param name="bSplitGeosByGroup">Whether to split the geometry by groups. Not recommended to use, but allowing in specific situations.</param>
		/// <returns>True if successfully cooked the node</returns>
		public override bool CookNode(HAPI_NodeId nodeID, bool bCookTemplatedGeos, bool bSplitGeosByGroup)
		{
			HAPI_CookOptions cookOptions = new HAPI_CookOptions();
			GetCookOptions(ref cookOptions);
			cookOptions.cookTemplatedGeos = bCookTemplatedGeos;
			cookOptions.splitGeosByGroup |= bSplitGeosByGroup;
			//float cookTime = Time.realtimeSinceStartup;
			HAPI_Result result = HEU_HAPIImports.HAPI_CookNode(ref _sessionData._HAPISession, nodeID, ref cookOptions);
			//Debug.Log("Cook time: " + (Time.realtimeSinceStartup - cookTime));
			HandleStatusResult(result, "Cooking Node", false, true);

			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Rename an existing node.
		/// </summary>
		/// <param name="nodeID">ID of the node to rename</param>
		/// <param name="newName">New name</param>
		/// <returns>True if successful</returns>
		public override bool RenameNode(HAPI_NodeId nodeID, string newName)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_RenameNode(ref _sessionData._HAPISession, nodeID, newName);
			HandleStatusResult(result, "Rename Node", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Connect two nodes together
		/// </summary>
		/// <param name="nodeID">Node whom's input to connect to</param>
		/// <param name="inputIndex">The input index should be between 0 and nodeIDToConnect's input count</param>
		/// <param name="nodeIDToConnect">The ndoe to connect to nodeID's input</param>
		/// <param name="outputIndex">The output index should be between 0 and nodeIDToConnect's output count</param>
		/// <returns>True if successful</returns>
		public override bool ConnectNodeInput(HAPI_NodeId nodeID, int inputIndex, HAPI_NodeId nodeIDToConnect, int outputIndex = 0)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_ConnectNodeInput(ref _sessionData._HAPISession, nodeID, inputIndex, nodeIDToConnect, outputIndex);
			HandleStatusResult(result, "Connect Node Input", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Disconnect a node input
		/// </summary>
		/// <param name="nodeID">The node whom's input to disconnect</param>
		/// <param name="inputIndex">The input index should be between 0 and the node's input count</param>
		/// <param name="bLogError">Whether to log error</param>
		/// <returns>True if successful</returns>
		public override bool DisconnectNodeInput(HAPI_NodeId nodeID, int inputIndex, bool bLogError)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_DisconnectNodeInput(ref _sessionData._HAPISession, nodeID, inputIndex);
			HandleStatusResult(result, "Disconnect Node Input", false, bLogError);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Query which node is connected to another node's input.
		/// </summary>
		/// <param name="nodeID">The node to query</param>
		/// <param name="inputIndex">The input index should be between 0 and the node's input count</param>
		/// <param name="connectedNodeID">The node ID of the connected node to this input. -1 if no connection.</param>
		/// <param name="bLogError">True if error should be logged</param>
		/// <returns>True if successfully queried the node</returns>
		public override bool QueryNodeInput(HAPI_NodeId nodeID, int inputIndex, out HAPI_NodeId connectedNodeID, bool bLogError)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_QueryNodeInput(ref _sessionData._HAPISession, nodeID, inputIndex, out connectedNodeID);
			HandleStatusResult(result, "Query Node Input", false, bLogError);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Get the name of the given node's input. This will return a string handle for the name
		/// which will persisst until the next call to this function.
		/// </summary>
		/// <param name="nodeID">Node's ID</param>
		/// <param name="inputIndex">Index of the input</param>
		/// <param name="nodeNameIndex">Input name string handle</param>
		/// <returns>True if successfully queried the node</returns>
		public override bool GetNodeInputName(HAPI_NodeId nodeID, int inputIndex, out HAPI_StringHandle nodeNameIndex)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetNodeInputName(ref _sessionData._HAPISession, nodeID, inputIndex, out nodeNameIndex);
			HandleStatusResult(result, "Get Node Input Name", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Returns the number of assets contained in an asset library.
		/// Requires LoadAssetLibraryFromFile to be invokved before calling this.
		/// </summary>
		/// <param name="libraryID">ID of the asset in lbirary</param>
		/// <param name="assetCount">Number of assets contained in this asset library</param>
		/// <returns>True if successfully queried the asset count</returns>
		public override bool GetAvailableAssetCount(HAPI_AssetLibraryId libraryID, out int assetCount)
		{
			assetCount = 0;

			HAPI_Result result = HEU_HAPIImports.HAPI_GetAvailableAssetCount(ref _sessionData._HAPISession, libraryID, out assetCount);
			HandleStatusResult(result, "Get Asset Count", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Returns the names of the assets contained in given asset library.
		/// </summary>
		/// <param name="libraryID">ID of the asset in the library</param>
		/// <param name="assetNames">Array to fill with names. Assumes array is initialized at least size of assetCount</param>
		/// <param name="assetCount">Should be same or less than returned by GetAvailableAssetCount</param>
		/// <returns>True if query was successful</returns>
		public override bool GetAvailableAssets(HAPI_AssetLibraryId libraryID, ref HAPI_StringHandle[] assetNames, int assetCount)
		{
			Debug.Assert((assetNames != null && assetNames.Length >= assetCount), "Houdini Engine: Asset name array is not valid!");

			HAPI_Result result = HEU_HAPIImports.HAPI_GetAvailableAssets(ref _sessionData._HAPISession, libraryID, assetNames, assetCount);
			HandleStatusResult(result, "Get Available Assets", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Returns the asset info for the given node
		/// </summary>
		/// <param name="nodeID">The node to retrieve the asset info for</param>
		/// <param name="assetInfo">The asset info structure to populate</param>
		/// <returns>True if successfully queried the asset info</returns>
		public override bool GetAssetInfo(HAPI_NodeId nodeID, ref HAPI_AssetInfo assetInfo)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetAssetInfo(ref _sessionData._HAPISession, nodeID, ref assetInfo);
			HandleStatusResult(result, "Getting Asset Info", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Returns the node info for the given node.
		/// </summary>
		/// <param name="nodeID">The node to retrieve the node info for</param>
		/// <param name="nodeInfo">The node info structure to populate</param>
		/// <param name="bLogError">True to log any error</param>
		/// <returns>True if successfully queried the node info</returns>
		public override bool GetNodeInfo(HAPI_NodeId nodeID, ref HAPI_NodeInfo nodeInfo, bool bLogError = true)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetNodeInfo(ref _sessionData._HAPISession, nodeID, out nodeInfo);
			HandleStatusResult(result, "Getting Node Info", false, bLogError);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Get the node absolute path or relative path in the Houdini node network.
		/// </summary>
		/// <param name="nodeID">The ID of the node to query</param>
		/// <param name="relativeNodeID">The relative node. Set to -1 to get absolute.</param>
		/// <param name="path">The returned path string</param>
		/// <returns>True if successfully queried the node path</returns>
		public override bool GetNodePath(HAPI_NodeId nodeID, HAPI_NodeId relativeNodeID, out string path)
		{
			path = null;

			HAPI_StringHandle pathStringHandle;
			HAPI_Result result = HEU_HAPIImports.HAPI_GetNodePath(ref _sessionData._HAPISession, nodeID, relativeNodeID, out pathStringHandle);
			HandleStatusResult(result, "Getting Node Path", false, true);
			if (result == HAPI_Result.HAPI_RESULT_SUCCESS)
			{
				path = HEU_SessionManager.GetString(pathStringHandle);
			}
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Returns true if this node exists in the Houdini session.
		/// Allows host application to check if needed to repopulate in Houdini.
		/// </summary>
		/// <param name="nodeID">The node ID</param>
		/// <param name="uniqueNodeID">The unique node ID</param>
		/// <returns>True if the node exists</returns>
		public override bool IsNodeValid(HAPI_NodeId nodeID, int uniqueNodeID)
		{
			bool bValid = false;
			// Note that HAPI_IsNodeValid will always return HAPI_RESULT_SUCCESS
			HEU_HAPIImports.HAPI_IsNodeValid(ref _sessionData._HAPISession, nodeID, uniqueNodeID, ref bValid);
			return bValid;
		}

		/// <summary>
		/// Compose a list of child nodes based on given filters.
		/// </summary>
		/// <param name="parentNodeID">Parent node ID</param>
		/// <param name="nodeTypeFilter">Node type by which to filter the children</param>
		/// <param name="nodeFlagFilter">Node flags by which to filter the children</param>
		/// <param name="bRecursive">Whether or not to compose the list recursively</param>
		/// <param name="count">Number of child nodes composed</param>
		/// <returns>True if successfully composed the child node list</returns>
		public override bool ComposeChildNodeList(HAPI_NodeId parentNodeID, HAPI_NodeTypeBits nodeTypeFilter, HAPI_NodeFlagsBits nodeFlagFilter, bool bRecursive, ref int count)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_ComposeChildNodeList(ref _sessionData._HAPISession, parentNodeID, nodeTypeFilter, nodeFlagFilter, bRecursive, out count);
			HandleStatusResult(result, "Composing Child Node List", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Get the composed list of child node IDs after calling ComposeChildNodeList.
		/// </summary>
		/// <param name="parentNodeID">Parent node ID</param>
		/// <param name="childNodeIDs">Array to store the child node IDs. If null, will create array of size count. If non-null, size must at least be count.</param>
		/// <param name="count">Number of children in the composed list. Must match the count returned by ComposeChildNodeList</param>
		/// <returns>True if successfully retrieved the child node list</returns>
		public override bool GetComposedChildNodeList(HAPI_NodeId parentNodeID, HAPI_NodeId[] childNodeIDs, int count)
		{
			Debug.Assert(childNodeIDs != null && childNodeIDs.Length == count, "Child node IDs array not set to correct size!");
			HAPI_Result result = HEU_HAPIImports.HAPI_GetComposedChildNodeList(ref _sessionData._HAPISession, parentNodeID, childNodeIDs, count);
			HandleStatusResult(result, "Getting Child Node List", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		// HIP FILES --------------------------------------------------------------------------------------------------

		/// <summary>
		/// Load a HIP file into current (or new if none existing) Houdini session.
		/// </summary>
		/// <param name="fileName">HIP file path to load</param>
		/// <param name="bCookOnLoad">True if want to cook on loading instead of manually cook each node</param>
		/// <returns>True if successfull</returns>
		public override bool LoadHIPFile(string fileName, bool bCookOnLoad)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_LoadHIPFile(ref _sessionData._HAPISession, fileName, bCookOnLoad);
			HandleStatusResult(result, "Loading HIP file", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Save current Houdini session into a HIP file.
		/// </summary>
		/// <param name="fileName">HIP file path to save to</param>
		/// <param name="bLockNodes">True if all SOP nodes should be locked to maintain state, instead of relying on the re-cook</param>
		/// <returns>True if successfull</returns>
		public override bool SaveHIPFile(string fileName, bool bLockNodes)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_SaveHIPFile(ref _sessionData._HAPISession, fileName, bLockNodes);
			HandleStatusResult(result, "Saving HIP file", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		// OBJECTS ----------------------------------------------------------------------------------------------------

		/// <summary>
		/// Get the object info on an OBJ node.
		/// </summary>
		/// <param name="nodeID">The node ID</param>
		/// <param name="objectInfo">Object info to populate</param>
		/// <returns>True if successfully queried object info</returns>
		public override bool GetObjectInfo(HAPI_NodeId nodeID, ref HAPI_ObjectInfo objectInfo)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetObjectInfo(ref _sessionData._HAPISession, nodeID, out objectInfo);
			HandleStatusResult(result, "Getting Object Info", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Get the transform of an OBJ node.
		/// </summary>
		/// <param name="nodeID">The object node ID</param>
		/// <param name="relativeToNodeID">The object node ID of the object to which the returned transform will be relative to. -1 if want object's local transform</param>
		/// <param name="rstOrder">The transform order</param>
		/// <param name="hapiTransform">Transform info to populate</param>
		/// <returns>True if successfully queried transform info</returns>
		public override bool GetObjectTransform(HAPI_NodeId nodeID, HAPI_NodeId relativeToNodeID, HAPI_RSTOrder rstOrder, ref HAPI_Transform hapiTransform)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetObjectTransform(ref _sessionData._HAPISession, nodeID, relativeToNodeID, rstOrder, out hapiTransform);
			HandleStatusResult(result, "Getting Object's Transform Info", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Set the transform of an OBJ node.
		/// </summary>
		/// <param name="nodeID">The object node ID</param>
		/// <param name="hapiTransform">The transform to set</param>
		/// <returns>True if successfully set the transform</returns>
		public override bool SetObjectTransform(HAPI_NodeId nodeID, ref HAPI_TransformEuler hapiTransform)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_SetObjectTransform(ref _sessionData._HAPISession, nodeID, ref hapiTransform);
			HandleStatusResult(result, "Setting Object's Transform Info", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Compose a list of child object nodes given a parent node ID.
		/// </summary>
		/// <param name="nodeID">The parent node ID</param>
		/// <param name="objectCount">The number of object nodes currently under the parent</param>
		/// <returns>True if successfully composed the list</returns>
		public override bool ComposeObjectList(HAPI_NodeId nodeID, out int objectCount)
		{
			objectCount = 0;

			HAPI_NodeInfo nodeInfo = new HAPI_NodeInfo();
			bool bResult = GetNodeInfo(nodeID, ref nodeInfo);
			if (bResult)
			{
				int objectNodeID = nodeID;
				if (nodeInfo.type == HAPI_NodeType.HAPI_NODETYPE_SOP)
				{
					objectNodeID = nodeInfo.parentId;
				}

				HAPI_Result result = HEU_HAPIImports.HAPI_ComposeObjectList(ref _sessionData._HAPISession, objectNodeID, "", out objectCount);
				HandleStatusResult(result, "Composing Object List", false, true);
				return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
			}
			return false;
		}

		/// <summary>
		/// Fill an array of HAPI_ObjectInfo list.
		/// </summary>
		/// <param name="nodeID">The parent node ID</param>
		/// <param name="objectInfos">Array to fill. Should at least be size of length</param>
		/// <param name="start">At least 0 and at most object count returned by ComposeObjectList</param>
		/// <param name="length">Object count returned by ComposeObjectList. Should be at least 0 and at most object count - start</param>
		/// <returns>True if successfully queuried the object list</returns>
		public override bool GetComposedObjectList(HAPI_NodeId nodeID, [Out] HAPI_ObjectInfo[] objectInfos, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetComposedObjectList(ref _sessionData._HAPISession, nodeID, objectInfos, start, length);
			HandleStatusResult(result, "Getting Composed Object List", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Fill in array of HAPI_Transform list.
		/// </summary>
		/// <param name="nodeID">The parent node ID</param>
		/// <param name="rstOrder">Transform order</param>
		/// <param name="transforms">Array to fill. Should at least be size of length</param>
		/// <param name="start">At least 0 and at most object count returned by ComposeObjectList</param>
		/// <param name="length">Object count returned by ComposeObjectList. Should be at least 0 and at most object count - start</param>
		/// <returns>True if successfully queuried the transform list</returns>
		public override bool GetComposedObjectTransforms(HAPI_NodeId nodeID, HAPI_RSTOrder rstOrder, [Out] HAPI_Transform[] transforms, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetComposedObjectTransforms(ref _sessionData._HAPISession, nodeID, rstOrder, transforms, start, length);
			HandleStatusResult(result, "Getting Composed Object List", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}


		// GEOMETRY GETTERS -------------------------------------------------------------------------------------------

		/// <summary>
		/// Get the display geo (SOP) node inside an Object node. If there are multiple display SOP nodes, only
		/// the first one is returned.
		/// </summary>
		/// <param name="nodeID">Object node ID</param>
		/// <param name="geoInfo">Geo info to populate</param>
		/// <returns>True if successfully queried the geo info</returns>
		public override bool GetDisplayGeoInfo(HAPI_NodeId nodeID, ref HAPI_GeoInfo geoInfo, bool bLogError)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetDisplayGeoInfo(ref _sessionData._HAPISession, nodeID, out geoInfo);
			HandleStatusResult(result, "Getting Display Geo Info", false, bLogError);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Get the geoemtry info on a SOP node.
		/// </summary>
		/// <param name="nodeID">The SOP node ID</param>
		/// <param name="geoInfo">Geo info to populate</param>
		/// <returns>True if successfully queried the geo info</returns>
		public override bool GetGeoInfo(HAPI_NodeId nodeID, ref HAPI_GeoInfo geoInfo)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetGeoInfo(ref _sessionData._HAPISession, nodeID, out geoInfo);
			HandleStatusResult(result, "Getting Geo Info", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Get the part info on a SOP node.
		/// </summary>
		/// <param name="nodeID">The SOP node ID</param>
		/// <param name="partID">The part ID</param>
		/// <param name="geoInfo">Part info to populate</param>
		/// <returns>True if successfully queried the part info</returns>
		public override bool GetPartInfo(HAPI_NodeId nodeID, HAPI_PartId partID, ref HAPI_PartInfo partInfo)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetPartInfo(ref _sessionData._HAPISession, nodeID, partID, out partInfo);
			HandleStatusResult(result, "Getting Part Info", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Get the main geometry info struct.
		/// </summary>
		/// <param name="nodeID">The SOP node ID</param>
		/// <param name="partID">The part ID</param>
		/// <param name="name">Attribute name</param>
		/// <param name="owner">Attribute owner</param>
		/// <param name="attributeInfo">Info to populate</param>
		/// <returns>True if successfully queried the attribute info</returns>
		public override bool GetAttributeInfo(HAPI_NodeId nodeID, HAPI_PartId partID, string name, HAPI_AttributeOwner owner, ref HAPI_AttributeInfo attributeInfo)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetAttributeInfo(ref _sessionData._HAPISession, nodeID, partID, name, owner, ref attributeInfo);
			HandleStatusResult(result, "Getting Attribute Info", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Get the attribute names of all attributes having owner of the given part.
		/// </summary>
		/// <param name="nodeID">The SOP node ID</param>
		/// <param name="partID">The part ID</param>
		/// <param name="owner">Attributes must have this owner type</param>
		/// <param name="attributeNames">Result array of name strings. Must be atleast count size.</param>
		/// <param name="count">Expected number of attributes. Should be from HAPI_PartInfo.attributeCounts[owner].</param>
		/// <returns>True if successfully retrieved the names</returns>
		public override bool GetAttributeNames(HAPI_NodeId nodeID, HAPI_PartId partID, HAPI_AttributeOwner owner, ref string[] attributeNames, int count)
		{
			HAPI_StringHandle[] attributeStringHandles = new HAPI_StringHandle[count];
			HAPI_Result result = HEU_HAPIImports.HAPI_GetAttributeNames(ref _sessionData._HAPISession, nodeID, partID, owner, attributeStringHandles, count);
			if (result == HAPI_Result.HAPI_RESULT_SUCCESS)
			{
				Debug.AssertFormat(attributeNames != null && attributeNames.Length >= count, "attributeNames must be atleast {0} size!", count);
				for (int i = 0; i < attributeStringHandles.Length; ++i)
				{
					attributeNames[i] = HEU_SessionManager.GetString(attributeStringHandles[i], this);
				}
			}
			HandleStatusResult(result, "Getting Attribute Names", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Get the attribute string data.
		/// </summary>
		/// <param name="nodeID">The node ID</param>
		/// <param name="partID">The part ID</param>
		/// <param name="name">Attribute name</param>
		/// <param name="attributeInfo">Atttribute info</param>
		/// <param name="dataArray">Array to populate. Must be at least the size of length * HAPI_AttributeInfo::tupleSize</param>
		/// <param name="start">First index of range. Must be at least 0 and at most HAPI_AttributeInfo::count - 1</param>
		/// <param name="length">Must be at least 0 and at most HAPI_AttributeInfo::count - start</param>
		/// <returns>True if successfully queried the atttribute string data</returns>
		public override bool GetAttributeStringData(HAPI_NodeId nodeID, HAPI_PartId partID, string name, ref HAPI_AttributeInfo attributeInfo, [Out] HAPI_StringHandle[] dataArray, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetAttributeStringData(ref _sessionData._HAPISession, nodeID, partID, name, ref attributeInfo, dataArray, start, length);
			HandleStatusResult(result, "Getting Attribute String Data", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Get the attribute float data.
		/// </summary>
		/// <param name="nodeID">The node ID</param>
		/// <param name="partID">The part ID</param>
		/// <param name="name">Attribut ename</param>
		/// <param name="attributeInfo">Should be same struct returned by HAPI_GetAttributeInfo</param>
		/// <param name="data">A float array at least the size of length * HAPI_AttributeInfo::tupleSize</param>
		/// <param name="start">First index of range. Must be at least 0 and at most HAPI_AttributeInfo::count - 1</param>
		/// <param name="length">Must be at least 0 and at most HAPI_AttributeInfo::count - start.</param>
		/// <returns>True if successfully queried the atttribute float data</returns>
		public override bool GetAttributeFloatData(HAPI_NodeId nodeID, HAPI_PartId partID, string name, ref HAPI_AttributeInfo attributeInfo, [Out] float[] data, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetAttributeFloatData(ref _sessionData._HAPISession, nodeID, partID, name, ref attributeInfo, -1, data, start, length);
			HandleStatusResult(result, "Getting Attribute Float Data", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Get the attribute int data.
		/// </summary>
		/// <param name="nodeID">The node ID</param>
		/// <param name="partID">The part ID</param>
		/// <param name="name">Attribut ename</param>
		/// <param name="attributeInfo">Should be same struct returned by HAPI_GetAttributeInfo</param>
		/// <param name="data">An int array at least the size of length * HAPI_AttributeInfo::tupleSize</param>
		/// <param name="start">First index of range. Must be at least 0 and at most HAPI_AttributeInfo::count - 1</param>
		/// <param name="length">Must be at least 0 and at most HAPI_AttributeInfo::count - start.</param>
		/// <returns>True if successfully queried the atttribute int data</returns>
		public override bool GetAttributeIntData(HAPI_NodeId nodeID, HAPI_PartId partID, string name, ref HAPI_AttributeInfo attributeInfo, [Out] int[] data, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetAttributeIntData(ref _sessionData._HAPISession, nodeID, partID, name, ref attributeInfo, -1, data, start, length);
			HandleStatusResult(result, "Getting Attribute Int Data", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Gets the group names for given group type.
		/// </summary>
		/// <param name="nodeID">The node ID</param>
		/// <param name="groupType">The group type</param>
		/// <param name="names">Array to populate. Must at least be of size count</param>
		/// <param name="count">Should be less than or equal to size of names</param>
		/// <returns>True if successfully queried the group names</returns>
		public override bool GetGroupNames(HAPI_NodeId nodeID, HAPI_GroupType groupType, ref HAPI_StringHandle[] names, int count)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetGroupNames(ref _sessionData._HAPISession, nodeID, groupType, names, count);
			HandleStatusResult(result, "Getting Group Names", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Get group membership
		/// </summary>
		/// <param name="nodeID"></param>
		/// <param name="partID"></param>
		/// <param name="groupType"></param>
		/// <param name="groupName"></param>
		/// <param name="membershipArrayAllEqual">Quick way to determine if all items are in the given group or not</param>
		/// <param name="membershipArray">Array of ints that represents membership of group</param>
		/// <param name="start">Start offset into the membership array</param>
		/// <param name="length">Should be less than or equal to the size of membership</param>
		/// <returns>True if successfully queried the group membership</returns>
		public override bool GetGroupMembership(HAPI_NodeId nodeID, HAPI_PartId partID, HAPI_GroupType groupType, string groupName, ref bool membershipArrayAllEqual, [Out] int[] membershipArray, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetGroupMembership(ref _sessionData._HAPISession, nodeID, partID, groupType, groupName, ref membershipArrayAllEqual, membershipArray, start, length);
			HandleStatusResult(result, "Getting Group Membership", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetGroupCountOnPackedInstancePart(HAPI_NodeId nodeID, HAPI_PartId partID, out int pointGroupCount, out int primitiveGroupCount)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetGroupCountOnPackedInstancePart(ref _sessionData._HAPISession, nodeID, partID, out pointGroupCount, out primitiveGroupCount);
			HandleStatusResult(result, "Getting Group Count on Packed Instance Part", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetGroupNamesOnPackedInstancePart(HAPI_NodeId nodeID, HAPI_PartId partID, HAPI_GroupType groupType, ref HAPI_StringHandle[] groupNamesArray, int groupCount)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetGroupNamesOnPackedInstancePart(ref _sessionData._HAPISession, nodeID, partID, groupType, groupNamesArray, groupCount);
			HandleStatusResult(result, "Getting Group Names on Packed Instance Part", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetGroupMembershipOnPackedInstancePart(HAPI_NodeId nodeID, HAPI_PartId partID, HAPI_GroupType groupType, string groupName, ref bool membershipArrayAllEqual, [Out] int[] membershipArray, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetGroupMembershipOnPackedInstancePart(ref _sessionData._HAPISession, nodeID, partID, groupType, groupName, ref membershipArrayAllEqual, membershipArray, start, length);
			HandleStatusResult(result, "Getting Group Membership on Packed Instance Part", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetInstancedPartIds(HAPI_NodeId nodeID, HAPI_PartId partID, [Out] HAPI_PartId[] instancedPartsArray, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetInstancedPartIds(ref _sessionData._HAPISession, nodeID, partID, instancedPartsArray, start, length);
			HandleStatusResult(result, "Getting Instance Part Ids", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetInstancerPartTransforms(HAPI_NodeId nodeID, HAPI_PartId partID, HAPI_RSTOrder rstOrder, [Out] HAPI_Transform[] transformsArray, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetInstancerPartTransforms(ref _sessionData._HAPISession, nodeID, partID, rstOrder, transformsArray, start, length);
			HandleStatusResult(result, "Getting Instance Part Transforms", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetInstanceTransformsOnPart(HAPI_NodeId nodeID, HAPI_PartId partID, HAPI_RSTOrder rstOrder, [Out] HAPI_Transform[] transformsArray, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetInstanceTransformsOnPart(ref _sessionData._HAPISession, nodeID, partID, rstOrder, transformsArray, start, length);
			HandleStatusResult(result, "Getting Instance Transforms On Part", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetInstancedObjectIds(HAPI_NodeId nodeID, [Out] HAPI_NodeId[] instanced_node_id_array, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetInstancedObjectIds(ref _sessionData._HAPISession, nodeID, instanced_node_id_array, start, length);
			HandleStatusResult(result, "Getting Instanced Object Ids", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Get the array of faces where the nth integer in the array is the number of vertices
		/// the nth face has.
		/// </summary>
		/// <param name="nodeID">The node ID</param>
		/// <param name="partID">The part ID</param>
		/// <param name="faceCounts">An integer array at least the size of length</param>
		/// <param name="start">First index of range. Must be at least 0 and at most HAPI_PartInfo::faceCount - 1</param>
		/// <param name="length">Must be at least 0 and at most HAPI_PartInfo::faceCount - start</param>
		/// <returns>True if successfully queried the face counts</returns>
		public override bool GetFaceCounts(HAPI_NodeId nodeID, HAPI_PartId partID, [Out] int[] faceCounts, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetFaceCounts(ref _sessionData._HAPISession, nodeID, partID, faceCounts, start, length);
			HandleStatusResult(result, "Getting Face Counts", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Get the array containing the vertex-point associations where the ith element
		/// in the array is the point index that the ith vertex associates with.
		/// </summary>
		/// <param name="nodeID">The node ID</param>
		/// <param name="partID">The part ID</param>
		/// <param name="vertexList">An integer array at least the size of length</param>
		/// <param name="start">First index of range. Must be at least 0 and at most HAPI_PartInfo::vertexCount - 1</param>
		/// <param name="length">Must be at least 0 and at most HAPI_PartInfo::vertexCount - start</param>
		/// <returns>True if successfully queried the vertex list</returns>
		public override bool GetVertexList(HAPI_NodeId nodeID, HAPI_PartId partID, [Out] int[] vertexList, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetVertexList(ref _sessionData._HAPISession, nodeID, partID, vertexList, start, length);
			HandleStatusResult(result, "Getting Vertex List", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Get the box info on a geo part.
		/// </summary>
		/// <param name="nodeID">The geo node ID</param>
		/// <param name="partID">The part ID</param>
		/// <param name="boxInfo">The info to fill</param>
		/// <returns>True if successfully queried the box info</returns>
		public override bool GetBoxInfo(HAPI_NodeId nodeID, HAPI_PartId partID, ref HAPI_BoxInfo boxInfo)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetBoxInfo(ref _sessionData._HAPISession, nodeID, partID, ref boxInfo);
			HandleStatusResult(result, "Getting Box Info", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Get the sphere info on a geo part.
		/// </summary>
		/// <param name="nodeID">The geo node ID</param>
		/// <param name="partID">The part ID</param>
		/// <param name="sphereInfo">The info to fill</param>
		/// <returns>True if successfully queried the sphere info</returns>
		public override bool GetSphereInfo(HAPI_NodeId nodeID, HAPI_PartId partID, ref HAPI_SphereInfo sphereInfo)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetSphereInfo(ref _sessionData._HAPISession, nodeID, partID, ref sphereInfo);
			HandleStatusResult(result, "Getting Sphere Info", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetCurveInfo(HAPI_NodeId nodeID, HAPI_PartId partID, ref HAPI_CurveInfo curveInfo)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetCurveInfo(ref _sessionData._HAPISession, nodeID, partID, ref curveInfo);
			HandleStatusResult(result, "Getting Curve Info", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetCurveCounts(HAPI_NodeId nodeID, HAPI_PartId partID, [Out] int[] counts, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetCurveCounts(ref _sessionData._HAPISession, nodeID, partID, counts, start, length);
			HandleStatusResult(result, "Getting Curve Counts", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		// GEOMETRY SETTERS -------------------------------------------------------------------------------------------

		public override bool SetPartInfo(HAPI_NodeId nodeID, HAPI_PartId partID, ref HAPI_PartInfo partInfo)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_SetPartInfo(ref _sessionData._HAPISession, nodeID, partID, ref partInfo);
			HandleStatusResult(result, "Setting Part Info", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool SetFaceCount(HAPI_NodeId nodeID, HAPI_PartId partID, int[] faceCounts, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_SetFaceCounts(ref _sessionData._HAPISession, nodeID, partID, faceCounts, start, length);
			HandleStatusResult(result, "Setting Face Count", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool SetVertexList(HAPI_NodeId nodeID, HAPI_PartId partID, int[] vertexList, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_SetVertexList(ref _sessionData._HAPISession, nodeID, partID, vertexList, start, length);
			HandleStatusResult(result, "Setting Vertex List", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool SetAttributeIntData(HAPI_NodeId nodeID, HAPI_PartId partID, string name, ref HAPI_AttributeInfo attrInfo,
			int[] data, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_SetAttributeIntData(ref _sessionData._HAPISession, nodeID, partID, name, ref attrInfo, data, start, length);
			HandleStatusResult(result, "Setting Attribute Int Data", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool SetAttributeFloatData(HAPI_NodeId nodeID, HAPI_PartId partID, string name, ref HAPI_AttributeInfo attrInfo,
			float[] data, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_SetAttributeFloatData(ref _sessionData._HAPISession, nodeID, partID, name, ref attrInfo, data, start, length);
			HandleStatusResult(result, "Setting Attribute Float Data", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool SetAttributeStringData(HAPI_NodeId nodeID, HAPI_PartId partID, string name, ref HAPI_AttributeInfo attrInfo,
			string[] data, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_SetAttributeStringData(ref _sessionData._HAPISession, nodeID, partID, name, ref attrInfo, data, start, length);
			HandleStatusResult(result, "Setting Attribute String Data", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool AddAttribute(HAPI_NodeId nodeID, HAPI_PartId partID, string name, ref HAPI_AttributeInfo attrInfo)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_AddAttribute(ref _sessionData._HAPISession, nodeID, partID, name, ref attrInfo);
			HandleStatusResult(result, "Adding Attribute Info", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool AddGroup(HAPI_NodeId nodeID, HAPI_PartId partID, HAPI_GroupType groupType, string groupName)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_AddGroup(ref _sessionData._HAPISession, nodeID, partID, groupType, groupName);
			HandleStatusResult(result, "Adding Group", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool DeleteGroup(HAPI_NodeId nodeID, HAPI_PartId partID, HAPI_GroupType groupType, string groupName)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_DeleteGroup(ref _sessionData._HAPISession, nodeID, partID, groupType, groupName);
			HandleStatusResult(result, "Deleting Group", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool SetGroupMembership(HAPI_NodeId nodeID, HAPI_PartId partID, HAPI_GroupType groupType, string groupName, [Out] int[] membershipArray, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_SetGroupMembership(ref _sessionData._HAPISession, nodeID, partID, groupType, groupName, membershipArray, start, length);
			HandleStatusResult(result, "Setting Group Membership", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool CommitGeo(HAPI_NodeId nodeID)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_CommitGeo(ref _sessionData._HAPISession, nodeID);
			HandleStatusResult(result, "Committing Geo", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool RevertGeo(HAPI_NodeId nodeID)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_RevertGeo(ref _sessionData._HAPISession, nodeID);
			HandleStatusResult(result, "Revertting Geo", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		// MATERIALS --------------------------------------------------------------------------------------------------

		/// <summary>
		/// Get the Material on the specified Part
		/// </summary>
		/// <param name="nodeID">The node ID</param>
		/// <param name="partID">The part ID</param>
		/// <param name="materialInfo">A valid material info to populate</param>
		/// <returns>True if successfully queried the material info</returns>
		public override bool GetMaterialOnPart(HAPI_NodeId nodeID, HAPI_PartId partID, ref HAPI_MaterialInfo materialInfo)
		{
			bool areAllSame = false;
			HAPI_NodeId[] materialIDs = new HAPI_NodeId[1];
			HAPI_Result result = HEU_HAPIImports.HAPI_GetMaterialNodeIdsOnFaces(ref _sessionData._HAPISession, nodeID, partID, ref areAllSame, materialIDs, 0, 1);
			if(result == HAPI_Result.HAPI_RESULT_SUCCESS)
			{
				result = HEU_HAPIImports.HAPI_GetMaterialInfo(ref _sessionData._HAPISession, materialIDs[0], out materialInfo);
			}

			HandleStatusResult(result, "Getting Material On Part", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Get the material node IDs on faces for specified part.
		/// </summary>
		/// <param name="nodeID">The node ID</param>
		/// <param name="partID">The part to query</param>
		/// <param name="bSingleFaceMaterial">Whether same material on all faces</param>
		/// <param name="materialNodeIDs">The array to fill in with material node IDs. Must be at least size of faceCount</param>
		/// <param name="faceCount">Number of material IDs to query</param>
		/// <returns>True if successfully queried the materials</returns>
		public override bool GetMaterialNodeIDsOnFaces(HAPI_NodeId nodeID, HAPI_PartId partID, ref bool bSingleFaceMaterial, [Out] HAPI_NodeId[] materialNodeIDs, int faceCount)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetMaterialNodeIdsOnFaces(ref _sessionData._HAPISession, nodeID, partID, ref bSingleFaceMaterial, materialNodeIDs, 0, faceCount);
			HandleStatusResult(result, "Getting Material Node IDs On Faces", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Get the material info for the given material node ID.
		/// </summary>
		/// <param name="materialNodeID">The material node ID</param>
		/// <param name="materialInfo">Material info to populate</param>
		/// <returns>True if successfully returned the material info</returns>
		public override bool GetMaterialInfo(HAPI_NodeId materialNodeID, ref HAPI_MaterialInfo materialInfo, bool bLogError)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetMaterialInfo(ref _sessionData._HAPISession, materialNodeID, out materialInfo);
			HandleStatusResult(result, "Getting Material Info", false, bLogError);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetImageInfo(HAPI_NodeId materialNodeID, ref HAPI_ImageInfo imageInfo)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetImageInfo(ref _sessionData._HAPISession, materialNodeID, out imageInfo);
			HandleStatusResult(result, "Getting Image Info", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool SetImageInfo(HAPI_NodeId materialNodeID, ref HAPI_ImageInfo imageInfo)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_SetImageInfo(ref _sessionData._HAPISession, materialNodeID, ref imageInfo);
			HandleStatusResult(result, "Setting Image Info", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool RenderTextureToImage(HAPI_NodeId materialNodeID, HAPI_ParmId parmID, bool bLogError = true)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_RenderTextureToImage(ref _sessionData._HAPISession, materialNodeID, parmID);
			HandleStatusResult(result, "Rendering Texture To Image", false, bLogError);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool RenderCOPToImage(HAPI_NodeId copNodeID)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_RenderCOPToImage(ref _sessionData._HAPISession, copNodeID);
			HandleStatusResult(result, "Rendering COP To Image", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool ExtractImageToMemory(HAPI_NodeId nodeID, string fileFormat, string imagePlanes, out byte[] buffer)
		{
			int bufferSize = 0;
			HAPI_Result result = HEU_HAPIImports.HAPI_ExtractImageToMemory(ref _sessionData._HAPISession, nodeID, fileFormat, imagePlanes, out bufferSize);
			if(result == HAPI_Result.HAPI_RESULT_SUCCESS)
			{
				buffer = new byte[bufferSize];
				result = HEU_HAPIImports.HAPI_GetImageMemoryBuffer(ref _sessionData._HAPISession, nodeID, buffer, bufferSize);
			}
			else
			{
				buffer = new byte[0];
			}

			HandleStatusResult(result, "Extracting Image Memory to Buffer", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetImagePlanes(HAPI_NodeId nodeID, [Out] HAPI_StringHandle[] imagePlanes, int numImagePlanes)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetImagePlanes(ref _sessionData._HAPISession, nodeID, imagePlanes, numImagePlanes);
			HandleStatusResult(result, "Getting Image Planes", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Extract image to given folder path, returning the path to the written image file.
		/// </summary>
		/// <param name="nodeID">Material node ID</param>
		/// <param name="fileFormat">The image's file format</param>
		/// <param name="imagePlanes">Image planes, see docs</param>
		/// <param name="destinationFolderPath">Path to folder where image needs to be written out to</param>
		/// <param name="destinationFilePath">Path to the written image file</param>
		/// <returns>Returns valid path to written image file, or null if failed</returns>
		public override bool ExtractImageToFile(HAPI_NodeId nodeID, string fileFormat, string imagePlanes, string destinationFolderPath, out string destinationFilePath)
		{
			int destinationFilePathSH = 0;
			HAPI_Result result = HEU_HAPIImports.HAPI_ExtractImageToFile(ref _sessionData._HAPISession, nodeID, fileFormat, imagePlanes, destinationFolderPath, null, out destinationFilePathSH);
			if(result == HAPI_Result.HAPI_RESULT_SUCCESS)
			{
				destinationFilePath = HEU_SessionManager.GetString(destinationFilePathSH);
			}
			else
			{
				destinationFilePath = null;
			}

			HandleStatusResult(result, "Extracting Image to File", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}


		// PARAMS -----------------------------------------------------------------------------------------------------

		/// <summary>
		/// Fill an array of HAPI_ParmInfo structs with parameter information from asset node.
		/// </summary>
		/// <param name="nodeID">The asset node ID</param>
		/// <param name="parmInfos">Array to fill. Must be at least size of length.</param>
		/// <param name="start">First index of range. Must be at least 0 and at most HAPI_NodeInfo::parmCount - 1</param>
		/// <param name="length">Must be at least 0 and at most HAPI_NodeInfo::parmCount - start</param>
		/// <returns>True if successfully retrieved the HAPI_ParmInfos</returns>
		public override bool GetParams(HAPI_NodeId nodeID, [Out] HAPI_ParmInfo[] parmInfos, int start, int length)
		{
			Debug.Assert(parmInfos != null && parmInfos.Length >= length, "Invalid HAPI_ParmInfo array passed in to retrieve parameters!");
			HAPI_Result result = HEU_HAPIImports.HAPI_GetParameters(ref _sessionData._HAPISession, nodeID, parmInfos, start, length);
			HandleStatusResult(result, "Getting Parameters", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetParmTagName(HAPI_NodeId nodeID, HAPI_ParmId parmID, int tagIndex, out HAPI_StringHandle tagName)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetParmTagName(ref _sessionData._HAPISession, nodeID, parmID, tagIndex, out tagName);
			HandleStatusResult(result, "Getting Parm Tag Name", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetParmTagValue(HAPI_NodeId nodeID, HAPI_ParmId parmID, string tagName, out HAPI_StringHandle tagValue)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetParmTagValue(ref _sessionData._HAPISession, nodeID, parmID, tagName, out tagValue);
			HandleStatusResult(result, "Getting Parm Tag Value", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool ParmHasTag(HAPI_NodeId nodeID, HAPI_ParmId parmID, string tagName, ref bool hasTag)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_ParmHasTag(ref _sessionData._HAPISession, nodeID, parmID, tagName, ref hasTag);
			HandleStatusResult(result, "Parm Has Tag", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetParamIntValues(HAPI_NodeId nodeID, [Out] int[] values, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetParmIntValues(ref _sessionData._HAPISession, nodeID, values, start, length);
			HandleStatusResult(result, "Getting Param Int Values", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetParamIntValue(HAPI_NodeId nodeID, string parmName, int index, out int value)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetParmIntValue(ref _sessionData._HAPISession, nodeID, parmName, index, out value);
			HandleStatusResult(result, "Getting Param Int Value", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetParamFloatValues(HAPI_NodeId nodeID, [Out] float[] values, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetParmFloatValues(ref _sessionData._HAPISession, nodeID, values, start, length);
			HandleStatusResult(result, "Getting Param Float Values", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetParamFloatValue(HAPI_NodeId nodeID, string parmName, int index, out float value)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetParmFloatValue(ref _sessionData._HAPISession, nodeID, parmName, index, out value);
			HandleStatusResult(result, "Getting Param Float Value", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetParamStringValues(HAPI_NodeId nodeID, [Out] HAPI_StringHandle[] values, int start, int length)
		{
			const bool bEvaluate = true;
			HAPI_Result result = HEU_HAPIImports.HAPI_GetParmStringValues(ref _sessionData._HAPISession, nodeID, bEvaluate, values, start, length);
			HandleStatusResult(result, "Getting Param String Values", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetParamStringValue(HAPI_NodeId nodeID, string parmName, int index, out HAPI_StringHandle value)
		{
			const bool bEvaluate = true;
			HAPI_Result result = HEU_HAPIImports.HAPI_GetParmStringValue(ref _sessionData._HAPISession, nodeID, parmName, index, bEvaluate, out value);
			HandleStatusResult(result, "Getting Param String Value", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetParamNodeValue(HAPI_NodeId nodeID, string paramName, out int nodeValue)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetParmNodeValue(ref _sessionData._HAPISession, nodeID, paramName, out nodeValue);
			HandleStatusResult(result, "Getting Param Node Value", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetParamChoiceValues(HAPI_NodeId nodeID, [Out] HAPI_ParmChoiceInfo[] values, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetParmChoiceLists(ref _sessionData._HAPISession, nodeID, values, start, length);
			HandleStatusResult(result, "Getting Param Choice Values", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool SetParamIntValues(HAPI_NodeId nodeID, ref int[] values, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_SetParmIntValues(ref _sessionData._HAPISession, nodeID, values, start, length);
			HandleStatusResult(result, "Setting Param Int Values", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool SetParamIntValue(HAPI_NodeId nodeID, string paramName, int index, int value)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_SetParmIntValue(ref _sessionData._HAPISession, nodeID, paramName, index, value);
			HandleStatusResult(result, "Setting Param Int Value", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool SetParamFloatValues(HAPI_NodeId nodeID, ref float[] values, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_SetParmFloatValues(ref _sessionData._HAPISession, nodeID, values, start, length);
			HandleStatusResult(result, "Setting Param Float Values", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool SetParamFloatValue(HAPI_NodeId nodeID, string paramName, int index, float value)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_SetParmFloatValue(ref _sessionData._HAPISession, nodeID, paramName, index, value);
			HandleStatusResult(result, "Setting Param Float Value", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool SetParamStringValue(HAPI_NodeId nodeID, string strValue, HAPI_ParmId parmID, int index)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_SetParmStringValue(ref _sessionData._HAPISession, nodeID, strValue, parmID, index);
			HandleStatusResult(result, "Setting Param String Value", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool SetParamStringValue(HAPI_NodeId nodeID, string parmName, string parmValue, int index)
		{
			HAPI_NodeId parmID = HEU_Defines.HAPI_INVALID_PARM_ID;
			if (GetParmIDFromName(nodeID, parmName, out parmID))
			{
				return SetParamStringValue(nodeID, parmValue, parmID, index);
			}
			return false;
		}

		public override bool SetParamNodeValue(HAPI_NodeId nodeID, string paramName, HAPI_NodeId nodeValue)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_SetParmNodeValue(ref _sessionData._HAPISession, nodeID, paramName, nodeValue);
			HandleStatusResult(result, "Setting Param Node Value", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool InsertMultiparmInstance(HAPI_NodeId nodeID, HAPI_ParmId parmID, int instancePosition)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_InsertMultiparmInstance(ref _sessionData._HAPISession, nodeID, parmID, instancePosition);
			HandleStatusResult(result, "Inserting Multiparm Instance", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool RemoveMultiParmInstance(HAPI_NodeId nodeID, HAPI_ParmId parmID, int instancePosition)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_RemoveMultiparmInstance(ref _sessionData._HAPISession, nodeID, parmID, instancePosition);
			HandleStatusResult(result, "Removing MultiParm Instance", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetParmWithTag(HAPI_NodeId nodeID, string tagName, ref HAPI_ParmId parmID)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetParmWithTag(ref _sessionData._HAPISession, nodeID, tagName, ref parmID);
			HandleStatusResult(result, "Getting Parm With Tag", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool RevertParmToDefault(HAPI_NodeId nodeID, string parm_name, int index)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_RevertParmToDefault(ref _sessionData._HAPISession, nodeID, parm_name, index);
			HandleStatusResult(result, "Reverting Parm To Default", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool RevertParmToDefaults(HAPI_NodeId nodeID, string parm_name)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_RevertParmToDefaults(ref _sessionData._HAPISession, nodeID, parm_name);
			HandleStatusResult(result, "Reverting Parms To Defaults", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetParmIDFromName(HAPI_NodeId nodeID, string parmName, out HAPI_ParmId parmID)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetParmIdFromName(ref _sessionData._HAPISession, nodeID, parmName, out parmID);
			HandleStatusResult(result, "Getting Parm ID from Name", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetParmStringValue(HAPI_NodeId nodeID, string parmName, int index, bool evaluate, out HAPI_StringHandle value)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetParmStringValue(ref _sessionData._HAPISession, nodeID, parmName, index, evaluate, out value);
			HandleStatusResult(result, "Getting String Value For Parm", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		// INPUT NODES ------------------------------------------------------------------------------------------------

		public override bool CreateInputNode(out HAPI_NodeId nodeID, string name)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_CreateInputNode(ref _sessionData._HAPISession, out nodeID, name);
			HandleStatusResult(result, "Creating Input Node", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool CreateHeightfieldInputNode(HAPI_NodeId parentNodeID, string name, int xSize, int ySize, float voxelSize,
			out HAPI_NodeId heightfieldNodeID, out HAPI_NodeId heightNodeID, out HAPI_NodeId maskNodeID, out HAPI_NodeId mergeNodeID)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_CreateHeightfieldInputNode(ref _sessionData._HAPISession, parentNodeID, name, xSize, ySize, voxelSize,
				out heightfieldNodeID, out heightNodeID, out maskNodeID, out mergeNodeID);
			HandleStatusResult(result, "Creating Heightfield Input Node", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool CreateHeightfieldInputVolumeNode(HAPI_NodeId parentNodeID, out HAPI_NodeId newNodeID, string name, int xSize, int ySize, float voxelSize)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_CreateHeightfieldInputVolumeNode(ref _sessionData._HAPISession, parentNodeID, out newNodeID, name, xSize, ySize, voxelSize);
			HandleStatusResult(result, "Creating Heightfield Input Volume Node", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		// PRESETS ----------------------------------------------------------------------------------------------------

		/// <summary>
		/// Returns a preset blob of the current state of all parameter values.
		/// </summary>
		/// <param name="nodeID">The asset node ID</param>
		/// <param name="presetData">The acquired preset data (or empty if failed)</param>
		/// <returns>True if successfully acquired the preset data</returns>
		public override bool GetPreset(HAPI_NodeId nodeID, out byte[] presetData)
		{
			int bufferLength = 0;
			HAPI_Result result = HEU_HAPIImports.HAPI_GetPresetBufLength(ref _sessionData._HAPISession, nodeID, HAPI_PresetType.HAPI_PRESETTYPE_BINARY, null, ref bufferLength);
			HandleStatusResult(result, "Getting Preset Buffer Length", false, true);

			// If above query fails, then bufferLength is 0 and we'll return an empty array
			presetData = new byte[bufferLength];

			if (result == HAPI_Result.HAPI_RESULT_SUCCESS)
			{
				result = HEU_HAPIImports.HAPI_GetPreset(ref _sessionData._HAPISession, nodeID, presetData, bufferLength);
				HandleStatusResult(result, "Getting Preset", false, true);
			}
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		/// <summary>
		/// Sets an asset's preset data.
		/// </summary>
		/// <param name="nodeID">The asset node ID</param>
		/// <param name="presetData">The preset data buffer to set</param>
		/// <returns>True if successfully set the preset</returns>
		public override bool SetPreset(HAPI_NodeId nodeID, byte[] presetData)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_SetPreset(ref _sessionData._HAPISession, nodeID, HAPI_PresetType.HAPI_PRESETTYPE_BINARY, null, presetData, presetData.Length);
			HandleStatusResult(result, "Setting Preset", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}


		// VOLUMES -----------------------------------------------------------------------------------------------------

		public override bool GetVolumeInfo(HAPI_NodeId nodeID, HAPI_PartId partID, ref HAPI_VolumeInfo volumeInfo)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetVolumeInfo(ref _sessionData._HAPISession, nodeID, partID, ref volumeInfo);
			HandleStatusResult(result, "Getting Volume Info", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetHeightFieldData(HAPI_NodeId nodeID, HAPI_PartId partID, float[] valuesArray, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetHeightFieldData(ref _sessionData._HAPISession, nodeID, partID, valuesArray, start, length);
			HandleStatusResult(result, "Getting HeightField Data", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool SetVolumeInfo(HAPI_NodeId nodeID, HAPI_PartId partID, ref HAPI_VolumeInfo volumeInfo)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_SetVolumeInfo(ref _sessionData._HAPISession, nodeID, partID, ref volumeInfo);
			HandleStatusResult(result, "Setting VolumeInfo Data", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool SetVolumeTileFloatData(HAPI_NodeId nodeID, HAPI_PartId partID, ref HAPI_VolumeTileInfo tileInfo, float[] valuesArray, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_SetVolumeTileFloatData(ref _sessionData._HAPISession, nodeID, partID, ref tileInfo, valuesArray, length);
			HandleStatusResult(result, "Setting VolumeInfo Data", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetVolumeBounds(HAPI_NodeId nodeID, HAPI_PartId partID, out float x_min, out float y_min, out float z_min,
			out float x_max, out float y_max, out float z_max, out float x_center, out float y_center, out float z_center)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetVolumeBounds(ref _sessionData._HAPISession, nodeID, partID, out x_min, out y_min, out z_min,
				out x_max, out y_max, out z_max, out x_center, out y_center, out z_center);
			HandleStatusResult(result, "Getting Volume Bounds", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool SetHeightFieldData(HAPI_NodeId nodeID, HAPI_PartId partID, string name, float[] valuesArray, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_SetHeightFieldData(ref _sessionData._HAPISession, nodeID, partID, name, valuesArray, start, length);
			HandleStatusResult(result, "Setting VolumeInfo Data", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		// CACHING ----------------------------------------------------------------------------------------------------

		public override bool GetActiveCacheCount(out int activeCacheCount)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetActiveCacheCount(ref _sessionData._HAPISession, out activeCacheCount);
			HandleStatusResult(result, "Getting Active Cache Count", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetActiveCacheNames([Out] HAPI_StringHandle[] cacheNamesArray, int activeCacheCount)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetActiveCacheNames(ref _sessionData._HAPISession, cacheNamesArray, activeCacheCount);
			HandleStatusResult(result, "Getting Active Cache Names", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetCacheProperty(string cacheName, HAPI_CacheProperty cacheProperty, out int propertyValue)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetCacheProperty(ref _sessionData._HAPISession, cacheName, cacheProperty, out propertyValue);
			HandleStatusResult(result, "Getting Cache Property", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool SetCacheProperty(string cacheName, HAPI_CacheProperty cacheProperty, int propertyValue)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_SetCacheProperty(ref _sessionData._HAPISession, cacheName, cacheProperty, propertyValue);
			HandleStatusResult(result, "Setting Cache Property", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool SaveGeoToFile(HAPI_NodeId nodeID, string fileName)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_SaveGeoToFile(ref _sessionData._HAPISession, nodeID, fileName);
			HandleStatusResult(result, "Saving Geo File", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool LoadGeoFromFile(HAPI_NodeId nodeID, string file_name)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_LoadGeoFromFile(ref _sessionData._HAPISession, nodeID, file_name);
			HandleStatusResult(result, "Loading Geo From File", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetGeoSize(HAPI_NodeId nodeID, string format, out int size)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetGeoSize(ref _sessionData._HAPISession, nodeID, format, out size);
			HandleStatusResult(result, "Getting Geo Size", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		// HANDLES ----------------------------------------------------------------------------------------------------

		public override bool GetHandleInfo(HAPI_NodeId nodeID, [Out] HAPI_HandleInfo[] handleInfos, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetHandleInfo(ref _sessionData._HAPISession, nodeID, handleInfos, start, length);
			HandleStatusResult(result, "Getting Handle Info", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool GetHandleBindingInfo(HAPI_NodeId nodeID, int handleIndex, [Out] HAPI_HandleBindingInfo[] handleBindingInfos, int start, int length)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_GetHandleBindingInfo(ref _sessionData._HAPISession, nodeID, handleIndex, handleBindingInfos, start, length);
			HandleStatusResult(result, "Getting Handle Binding Info", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

		public override bool ConvertTransform(ref HAPI_TransformEuler inTransform, HAPI_RSTOrder RSTOrder, HAPI_XYZOrder ROTOrder, out HAPI_TransformEuler outTransform)
		{
			HAPI_Result result = HEU_HAPIImports.HAPI_ConvertTransform(ref _sessionData._HAPISession, ref inTransform, RSTOrder, ROTOrder, out outTransform);
			HandleStatusResult(result, "Converting Transform", false, true);
			return (result == HAPI_Result.HAPI_RESULT_SUCCESS);
		}

#endif // HOUDINIENGINEUNITY_ENABLED

	}

}   // HoudiniEngineUnity
