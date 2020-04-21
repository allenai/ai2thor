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

#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
using System;
using System.Runtime.InteropServices;
using System.Text;
#endif


namespace HoudiniEngineUnity
{
	/// <summary>
	/// Windows-specific platform functionality.
	/// </summary>
	public static class HEU_PlatformWin
	{
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)

		public enum RegSAM
		{
			QueryValue = 0x0001,
			SetValue = 0x0002,
			CreateSubKey = 0x0004,
			EnumerateSubKeys = 0x0008,
			Notify = 0x0010,
			CreateLink = 0x0020,
			WOW64_32Key = 0x0200,
			WOW64_64Key = 0x0100,
			WOW64_Res = 0x0300,
			Read = 0x00020019,
			Write = 0x00020006,
			Execute = 0x00020019,
			AllAccess = 0x000f003f
		}

		public static UIntPtr HKEY_LOCAL_MACHINE = new UIntPtr(0x80000002u);
		public static UIntPtr HKEY_CURRENT_USER = new UIntPtr(0x80000001u);

		[DllImport("Advapi32.dll")]
		static extern uint RegOpenKeyEx(
			UIntPtr hKey,
			string lpSubKey,
			uint ulOptions,
			int samDesired,
			out int phkResult);

		[DllImport("advapi32.dll", EntryPoint = "RegQueryValueEx")]
		public static extern int RegQueryValueEx(
			int hKey,
			string lpValueName,
			int lpReserved,
			ref uint lpType,
			System.Text.StringBuilder lpData,
			ref uint lpcbData);

		[DllImport("Advapi32.dll")]
		static extern uint RegCloseKey(int hKey);

		/// <summary>
		/// Returns the value of the specified registry key.
		/// </summary>
		/// <param name="rootKey">Handle to registry key.</param>
		/// <param name="keyName">Name of the registry subkey to be queried.</param>
		/// <param name="is32or64Key">Specify 32 or 64 bit value to query</param>
		/// <param name="inPropertyName">Name of the registry value.</param>
		/// <returns>Value of specified registry key.</returns>
		public static string GetRegistryKeyValue(UIntPtr rootKey, string keyName, RegSAM is32or64Key, string inPropertyName)
		{
			int phkResult = 0;

			try
			{
				uint lResult = RegOpenKeyEx(rootKey, keyName, 0, (int)RegSAM.QueryValue | (int)is32or64Key, out phkResult);
				if(lResult != 0)
				{
					return null;
				}

				uint lpType = 0;
				uint lpcData = 1024;
				StringBuilder valueBuffer = new StringBuilder(1024);
				RegQueryValueEx(phkResult, inPropertyName, 0, ref lpType, valueBuffer, ref lpcData);
				return valueBuffer.ToString();
			}
			finally
			{
				if(phkResult != 0)
				{
					RegCloseKey(phkResult);
				}
			}
		}

		/// <summary>
		/// Returns the value of the specified registry key (32-bit app).
		/// </summary>
		/// <param name="rootKey">Handle to registry key.</param>
		/// <param name="keyName">Name of the registry subkey to be queried.</param>
		/// <param name="inPropertyName">Name of the registry value.</param>
		/// <returns>Value of specified registry key.</returns>
		public static string GetRegistryKeyvalue_x86(UIntPtr rootKey, string keyName, string inPropertyName)
		{
			return GetRegistryKeyValue(rootKey, keyName, RegSAM.WOW64_32Key, inPropertyName);
		}

		/// <summary>
		/// Returns the value of the specified registry key (64-bit app).
		/// </summary>
		/// <param name="rootKey">Handle to registry key.</param>
		/// <param name="keyName">Name of the registry subkey to be queried.</param>
		/// <param name="inPropertyName">Name of the registry value.</param>
		/// <returns>Value of specified registry key.</returns>
		public static string GetRegistryKeyvalue_x64(UIntPtr rootKey, string keyName, string inPropertyName)
		{
			return GetRegistryKeyValue(rootKey, keyName, RegSAM.WOW64_64Key, inPropertyName);
		}

		/// <summary>
		/// Returns the application path of the specified application, as found in the system registry.
		/// </summary>
		/// <param name="appName">Name of the application as set in the registry.</param>
		/// <returns>Returns application path of the specified applicatoin.</returns>
		public static string GetApplicationPath(string appName)
		{
			// For Windows, we look at the registry entries made by the Houdini installer. We look for the
			// "active version" key which gives us the most recently installed Houdini version. Using the
			// active version we find the registry made by that particular installer and find the install
			// path.

			string correctVersion =
				HEU_HoudiniVersion.HOUDINI_MAJOR + "." +
				HEU_HoudiniVersion.HOUDINI_MINOR + "." +
				HEU_HoudiniVersion.HOUDINI_BUILD;

			// Note the extra 0 for the "minor-minor" version that's needed here.
			string correctVersionKey =
				HEU_HoudiniVersion.HOUDINI_MAJOR + "." +
				HEU_HoudiniVersion.HOUDINI_MINOR + "." +
				"0" + "." +
				HEU_HoudiniVersion.HOUDINI_BUILD;

			// HOUDINI_PATCH is a const variable, hence the 'unreachable code' warning.
			// However, it's a const variable in HoudiniVersion.cs which is a generated
			// file and could have the value be non-zero for certain builds, like
			// stub builds.
#pragma warning disable 162
			if (HEU_HoudiniVersion.HOUDINI_PATCH != 0)
				correctVersionKey += "." + HEU_HoudiniVersion.HOUDINI_PATCH;
#pragma warning restore 162

			string appPath = HEU_PlatformWin.GetRegistryKeyvalue_x64(HEU_PlatformWin.HKEY_LOCAL_MACHINE, HEU_Defines.SIDEFX_SOFTWARE_REGISTRY + appName, correctVersionKey);
			if (appPath == null || appPath.Length == 0)
			{
				// Try 32-bit entry (for Steam builds)
				appPath = HEU_PlatformWin.GetRegistryKeyvalue_x86(HEU_PlatformWin.HKEY_LOCAL_MACHINE, HEU_Defines.SIDEFX_SOFTWARE_REGISTRY + appName, correctVersionKey);
				if (appPath == null || appPath.Length == 0)
				{
					throw new HEU_HoudiniEngineError(string.Format("Expected version {0} of {1} not found in the system registry!", correctVersion, appName));
				}
			}

			if (appPath.EndsWith("\\") || appPath.EndsWith("/"))
			{
				appPath = appPath.Remove(appPath.Length - 1);
			}

			return appPath;
		}

#endif
	}

}   // HoudiniEngineUnity
