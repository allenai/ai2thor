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

using System;
using System.IO;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HoudiniEngineUnity
{

	/// <summary>
	/// Base class for platform-specific functionaltiy.
	/// </summary>
#if UNITY_EDITOR && HOUDINIENGINEUNITY_ENABLED
	[InitializeOnLoad ]
#endif
	public class HEU_Platform 
	{
#pragma warning disable 0414
		private static string _lastErrorMsg;
#pragma warning restore 0414

		private static string _libPath = null;

		public static string LibPath
		{
			get { return _libPath; }
		}

		private static bool _pathSet = false;

		public static bool IsPathSet
		{
			get { return _pathSet;  }
		}


		static HEU_Platform()
		{
			// This gets set whenever Unity initializes or there is a code refresh.
			SetHoudiniEnginePath();
		}
		
		/// <summary>
		/// Returns the path to the Houdini Engine plugin installation.
		/// </summary>
		/// <returns>Path to the Houdini Engine plugin installation.</returns>
		public static string GetHoudiniEnginePath()
		{
			// Use plugin setting path unless its not set
			string HAPIPath = GetSavedHoudiniPath();
			if (!string.IsNullOrEmpty(HAPIPath))
			{
				return HAPIPath;
			}

			return GetHoudiniEngineDefaultPath();
		}

		/// <summary>
		/// Returns the default installation path of Houdini that this plugin was built to use.
		/// </summary>
		public static string GetHoudiniEngineDefaultPath()
		{
			string HAPIPath = null;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

			// Look up in environment variable
			HAPIPath = System.Environment.GetEnvironmentVariable(HEU_Defines.HAPI_PATH, System.EnvironmentVariableTarget.Machine);
			if (HAPIPath == null || HAPIPath.Length == 0)
			{
				HAPIPath = System.Environment.GetEnvironmentVariable(HEU_Defines.HAPI_PATH, System.EnvironmentVariableTarget.User);
			}
			if (HAPIPath == null || HAPIPath.Length == 0)
			{
				HAPIPath = System.Environment.GetEnvironmentVariable(HEU_Defines.HAPI_PATH, System.EnvironmentVariableTarget.Process);
			}

			if (HAPIPath == null || HAPIPath.Length == 0)
			{
				// HAPI_PATH not set. Look in registry.

				string[] houdiniAppNames = { "Houdini Engine", "Houdini" };

				foreach (string appName in houdiniAppNames)
				{
					try
					{
						HAPIPath = HEU_PlatformWin.GetApplicationPath(appName);
						break;
					}
					catch (HEU_HoudiniEngineError error)
					{
						_lastErrorMsg = error.ToString();
					}
				}

				//Debug.Log("HAPI Path: " + HAPIPath);
			}

#elif (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX)
			HAPIPath = HEU_HoudiniVersion.HOUDINI_INSTALL_PATH;
#else
			_lastErrorMsg = "Unable to find Houdini installation because this is an unsupported platform!";
#endif

			return HAPIPath;
		}

		/// <summary>
		/// Return the saved Houdini install path.
		/// Checks if the plugin has been updated, and if so, asks
		/// user whether they want to switch to new version.
		/// If user switches, then this returns null to allow installed version
		/// to be used.
		/// </summary>
		/// <returns>The saved Houdini install path or null if it doesn't 
		/// exist or user wants to use installed version</returns>
		public static string GetSavedHoudiniPath()
		{
			string HAPIPath = HEU_PluginSettings.HoudiniInstallPath;
			if (!string.IsNullOrEmpty(HAPIPath))
			{
				// First check if the last stored installed Houdini version matches current installed version
				string lastHoudiniVersion = HEU_PluginSettings.LastHoudiniVersion;
				if (!string.IsNullOrEmpty(lastHoudiniVersion))
				{
					if (!lastHoudiniVersion.Equals(HEU_HoudiniVersion.HOUDINI_VERSION_STRING))
					{
						// Mismatch means different version of the plugin has been installed.
						// Ask user if they want to update their HAPIPath.
						// Confirmation means to clear out the saved HAPI path and use
						// the default one specified by the plugin.
						string title = "Updated Houdini Engine Plugin Detected";
						string msg = string.Format("You have overriden the plugin's default Houdini version with your own, but the plugin has been updated.\n" +
							"Would you like to use the updated plugin's default Houdini version?.");
						if (HEU_EditorUtility.DisplayDialog(title, msg, "Yes", "No"))
						{
							HEU_PluginSettings.HoudiniInstallPath = "";
							HAPIPath = null;
						}

						// Always update LastHoudiniVersion so this doesn't keep asking
						HEU_PluginSettings.LastHoudiniVersion = HEU_HoudiniVersion.HOUDINI_VERSION_STRING;
					}
				}
			}
			return HAPIPath;
		}

		/// <summary>
		/// Find the Houdini Engine libraries, and add the Houdini Engine path to the system path.
		/// </summary>
		public static void SetHoudiniEnginePath()
		{
#if HOUDINIENGINEUNITY_ENABLED
			if (_pathSet)
			{
				return;
			}

			// Get path to Houdini Engine
			string appPath = GetHoudiniEnginePath();
			string binPath = appPath + HEU_HoudiniVersion.HAPI_BIN_PATH;

			_pathSet = false;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
			bool bFoundLib = false;

			// Add path to system path if not already in there
			string systemPath = System.Environment.GetEnvironmentVariable("PATH", System.EnvironmentVariableTarget.Machine);
			if (systemPath != "" && !systemPath.Contains(binPath))
			{
				if (systemPath.Length == 0)
				{
					systemPath = binPath;
				}
				else
				{
					systemPath = binPath + ";" + systemPath;
				}

				System.Environment.SetEnvironmentVariable("PATH", systemPath, System.EnvironmentVariableTarget.Process);
			}

			// Look for the HAPI library DLL using system path
			foreach (string path in systemPath.Split(';'))
			{
				if (!System.IO.Directory.Exists(path))
				{
					continue;
				}

				string libPath = string.Format("{0}/{1}.dll", path, HEU_HoudiniVersion.HAPI_LIBRARY);
				if (DoesFileExist(libPath))
				{
					_libPath = libPath.Replace("\\", "/");
					bFoundLib = true;
					//Debug.Log("Houdini Engine DLL found at: " + LibPath);
					break;
				}
			}

			if (!bFoundLib)
			{
				if (appPath != "")
				{
					_lastErrorMsg = string.Format("Could not find {0} in PATH or at {1}.", HEU_HoudiniVersion.HAPI_LIBRARY, appPath);
				}
				else
				{
					_lastErrorMsg = string.Format("Could not find {0} in PATH.", HEU_HoudiniVersion.HAPI_LIBRARY);
				}
				return;
			}

			_pathSet = true;

#elif (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX)
			if(!System.IO.Directory.Exists(appPath))
			{
				_lastErrorMsg = string.Format("Could not find Houdini Engine library at {0}", appPath);
				Debug.LogError(_lastErrorMsg);
				return;
			}

			_libPath = appPath + HEU_HoudiniVersion.HAPI_LIBRARY_PATH;

			// Set HARS bin path to environment path so that we can start Thrift server
			string systemPath = System.Environment.GetEnvironmentVariable("PATH", System.EnvironmentVariableTarget.Process);
			if (string.IsNullOrEmpty(systemPath) || !systemPath.Contains(binPath))
			{
				if (string.IsNullOrEmpty(systemPath))
				{
					systemPath = binPath;
				}
				else
				{
					systemPath = binPath + ":" + systemPath;
				}
			}
			System.Environment.SetEnvironmentVariable("PATH", systemPath, System.EnvironmentVariableTarget.Process);

			_pathSet = true;
#endif

#endif
		}

		/// <summary>
		/// Return all folders (their full paths) in given path as semicolon delimited string.
		/// </summary>
		/// <param name="path">Path to parse.</param>
		/// <returns>Paths of all folders under given path.</returns>
		public static string GetAllFoldersInPath(string path)
		{
			if (!Directory.Exists(path))
			{
				return "";
			}

			// Using StringBuilder as its much more memory efficient than regular strings for concatenation.
			StringBuilder pathBuilder = new StringBuilder();
			GetAllFoldersInPathHelper(path, pathBuilder);
			return pathBuilder.ToString();
		}

		/// <summary>
		/// Helper that uses StringBuilder to build up the paths of all folders in given path.
		/// </summary>
		/// <param name="inPath">Path to parse.</param>
		/// <param name="pathBuilder">StringBuilder to add results to.</param>
		private static void GetAllFoldersInPathHelper(string inPath, StringBuilder pathBuilder)
		{
			if (Directory.Exists(inPath))
			{
				pathBuilder.Append(inPath);

				DirectoryInfo dirInfo = new DirectoryInfo(inPath);
				foreach (DirectoryInfo childDir in dirInfo.GetDirectories())
				{
					pathBuilder.Append(";");
					pathBuilder.Append(GetAllFoldersInPath(childDir.FullName));
				}
			}
		}

		/// <summary>
		/// Returns all files (with their paths) in a given folder, with or without pattern, either recursively or just the first.
		/// </summary>
		/// <param name="folderPath">Path to folder</param>
		/// <param name="searchPattern">File name pattern to search for</param>
		/// <param name="bRecursive">Search all directories or just the top</param>
		/// <returns>Array of file paths found or null if error</returns>
		public static string[] GetFilesInFolder(string folderPath, string searchPattern, bool bRecursive)
		{
			try
			{
				return Directory.GetFiles(folderPath, searchPattern, bRecursive ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);
			}
			catch(Exception ex)
			{
				Debug.LogErrorFormat("Getting files in directory {0} threw exception: {1}", folderPath, ex);
				return null;
			}
		}

		public static string GetFileName(string path)
		{
			return Path.GetFileName(path);
		}

		public static string GetFileNameWithoutExtension(string path)
		{
			return Path.GetFileNameWithoutExtension(path);
		}

		/// <summary>
		/// Removes file name and returns the path containing just the folders.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static string GetFolderPath(string path, bool bRemoveDirectorySeparatorAtEnd = false)
		{
			string resultPath = path;

			string fileName = Path.GetFileName(path);
			if (!string.IsNullOrEmpty(fileName))
			{
				resultPath = path.Replace(fileName, "");
			}

			if (bRemoveDirectorySeparatorAtEnd)
			{
				resultPath = resultPath.TrimEnd('\\', '/');
			}

			return resultPath;
		}

		/// <summary>
		/// Returns path separator character.
		/// </summary>
		public static char DirectorySeparator
		{
			// Instead of returning Path.DirectorySeparator, we'll use /
			// since all our platforms support it and to keep it consistent.
			// This way any saved paths in the project will work on all platforms.
			get { return '/'; }
		}

		/// <summary>
		/// Returns path separator string.
		/// </summary>
		public static string DirectorySeparatorStr
		{
			// Instead of returning Path.DirectorySeparator, we'll use /
			// since all our platforms support it and to keep it consistent.
			// This way any saved paths in the project will work on all platforms.
			get { return "/"; }
		}

		/// <summary>
		/// Given a list of folders, builds a platform-compatible
		/// path, using a separator in between the arguments.
		/// Assumes folder arguments are given in order from left to right.
		/// eg. folder1/folder2/args[0]/args[1]/...
		/// </summary>
		/// <param name="str1"></param>
		/// <param name="str2"></param>
		/// <param name="args"></param>
		/// <returns>Returns platform-compatible path of given folders</returns>
		public static string BuildPath(string folder1, string folder2, params object[] args)
		{
			char separator = DirectorySeparator;

			StringBuilder sb = new StringBuilder();
			sb.Append(folder1);
			sb.Append(separator);
			sb.Append(folder2);

			for (int i = 0; i < args.Length; ++i)
			{
				sb.Append(separator);
				sb.Append(args[i]);
			}

			return sb.ToString();
		}

		/// <summary>
		/// Returns the valid relative path if the given path is under the project Assets/ directory.
		/// If not, it returns the given path as is, but warns the user that the path is outside the project
		/// and therefore not safe to use if the project changes location on another workstation (eg. source control).
		/// </summary>
		/// <param name="inPath">Path to validate.</param>
		/// <returns>Relative path to project Assets/ folder, or just returns the path as is if outside the project.</returns>
		public static string GetValidRelativePath(string inPath)
		{
			// If the selected path is outside the project directory, warn user
			// If inside project, make sure its relative (for version control and multi-user projects)
			if (!HEU_AssetDatabase.IsPathRelativeToAssets(inPath))
			{
				string relativePath = HEU_AssetDatabase.GetAssetRelativePath(inPath);
				if (string.IsNullOrEmpty(relativePath))
				{
					string message = string.Format("Path: {0} is outside the project!\n This is not recommended if using version control or for multi-user projects.",
						inPath);
					Debug.LogWarning(message);
				}
				else
				{
					inPath = relativePath;
				}
			}
			return inPath;
		}

		/// <summary>
		/// Removes and returns the last directory separator character from given string.
		/// </summary>
		/// <param name="inPath">Path to parse</param>
		/// <returns>Returns the last directory separator character from given string</returns>
		public static string TrimLastDirectorySeparator(string inPath)
		{
			return inPath.TrimEnd(new char[] { DirectorySeparator });
		}

		public static bool DoesPathExist(string inPath)
		{
			return File.Exists(inPath) || Directory.Exists(inPath);
		}

		public static bool DoesFileExist(string inPath)
		{
			return File.Exists(inPath);
		}

		public static bool DoesDirectoryExist(string inPath)
		{
			return Directory.Exists(inPath);
		}

		public static bool CreateDirectory(string inPath)
		{
			DirectoryInfo dirInfo = Directory.CreateDirectory(inPath);
			if (dirInfo != null)
			{
				return dirInfo.Exists;
			}
			return false;
		}

		public static string GetFullPath(string inPath)
		{
			return Path.GetFullPath(inPath);
		}

		public static bool IsPathRooted(string inPath)
		{
			return Path.IsPathRooted(inPath);
		}

		public static void WriteBytes(string path, byte[] bytes)
		{
			File.WriteAllBytes(path, bytes);
		}

		public static bool WriteAllText(string path, string text)
		{
			try
			{
				File.WriteAllText(path, text);
				return true;
			}
			catch (System.Exception ex)
			{
				Debug.LogErrorFormat("Unable to save session to file: {0}. Exception: {1}", text, ex.ToString());
			}
			return false;
		}

		public static string ReadAllText(string path)
		{
			try
			{
				if (File.Exists(path))
				{
					return File.ReadAllText(path);
				}
			}
			catch (System.Exception ex)
			{
				Debug.LogErrorFormat("Unable to load from file: {0}. Exception: {1}", path, ex.ToString());
			}
			return "";
		}

		/// <summary>
		/// Returns environment value of given key, if found.
		/// </summary>
		/// <param name="key">Key to get the environment value for</param>
		/// <returns>Environment value as string, or empty if none found</returns>
		public static string GetEnvironmentValue(string key)
		{
			string value = System.Environment.GetEnvironmentVariable(key, System.EnvironmentVariableTarget.Machine);
			if (string.IsNullOrEmpty(value))
			{
				value = System.Environment.GetEnvironmentVariable(key, System.EnvironmentVariableTarget.User);
			}

			if (string.IsNullOrEmpty(value))
			{
				value = System.Environment.GetEnvironmentVariable(key, System.EnvironmentVariableTarget.Process);
			}

			return value;
		}

		public static string GetHoudiniEngineEnvironmentFilePathFull()
		{
			string envPath = HEU_PluginSettings.HoudiniEngineEnvFilePath;

			if (!HEU_Platform.IsPathRooted(envPath))
			{
				envPath = HEU_AssetDatabase.GetAssetFullPath(envPath);
			}

			return HEU_Platform.DoesFileExist(envPath) ? envPath : "";
		}
	}

}   // HoudiniEngineUnity