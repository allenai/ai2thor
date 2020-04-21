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
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HoudiniEngineUnity
{
	/// <summary>
	/// Manages the asset database to store persistent assets such as
	/// materials, textures, asset data, etc.
	/// Wraps around Unity AssetDatabase.
	/// Only available in Editor. Probably not needed at runtime as
	/// data probably does not need to persist past session.
	/// </summary>
	public static class HEU_AssetDatabase
	{
		public static string GetAssetCachePath()
		{
#if UNITY_EDITOR
			string rootPath = HEU_Platform.BuildPath("Assets", HEU_PluginSettings.AssetCachePath);
			if (!AssetDatabase.IsValidFolder(rootPath))
			{
				CreatePathWithFolders(rootPath);
			}

			return rootPath;
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return ""; 
#endif
		}

		/// <summary>
		/// Given full path this returns the path relative to the Assets/ folder.
		/// </summary>
		/// <param name="inFullPath">Full path to parse</param>
		/// <returns>Relative path to Assets/ folder, or null if invalid input path</returns>
		public static string GetAssetRelativePath(string inFullPath)
		{
			string replaceOld = Application.dataPath + HEU_Platform.DirectorySeparatorStr;
			string replaceNew = "Assets" + HEU_Platform.DirectorySeparatorStr;
			if (inFullPath.StartsWith(replaceOld))
			{
				return inFullPath.Replace(replaceOld, replaceNew);
			}
			else
			{
				//Debug.LogWarningFormat("Given full path {0} does not start with {1}. Unable to create relative path!", inFullPath, replaceOld);
				return null;
			}
		}

		public static string GetAssetPath(Object asset)
		{
#if UNITY_EDITOR
			return AssetDatabase.GetAssetPath(asset);
#else
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return null;
#endif
		}

		/// <summary>
		/// Returns the path to the given asset, with subasset tagging if it is
		/// a subasset. Unity doesn't have a way to query subasset paths directly
		/// nor load them directly. Instead have to load the main asset first then
		/// traverse through all assets to find the subasset.
		/// </summary>
		/// <param name="asset">Asset to get path for</param>
		/// <returns>Path of given asset</returns>
		public static string GetAssetPathWithSubAssetSupport(Object asset)
		{
#if UNITY_EDITOR
			string assetPath = AssetDatabase.GetAssetPath(asset);

			bool isSubAsset = HEU_AssetDatabase.IsSubAsset(asset);
			if (isSubAsset)
			{
				Object[] subAssets = HEU_AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
				int numSubAssets = subAssets.Length;
				for(int i = 0; i < numSubAssets; ++i)
				{
					if(subAssets[i] == asset)
					{
						assetPath = string.Format("{0}{1}/{2}", HEU_Defines.HEU_SUBASSET, assetPath, subAssets[i].name);
						break;
					}
				}
			}

			return assetPath;
#else
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return null;
#endif
		}

		/// <summary>
		/// Given the path to an asset, it returns the proper paths to load it.
		/// If its not a subasset, it just returns the given path.
		/// If its a subasset, it returns the main path, as well as well as the subasset name.
		/// </summary>
		/// <param name="fullPath">Path of asset to parse</param>
		/// <param name="mainPath">Path to main asset</param>
		/// <param name="subPath">Name of subasset if its a subasset, otherwise null for main asset</param>
		public static void GetSubAssetPathFromPath(string fullPath, out string mainPath, out string subPath)
		{
			mainPath = fullPath;
			subPath = null;

			if (fullPath.StartsWith(HEU_Defines.HEU_SUBASSET))
			{
				// This is a subasset: SUBASSET::main_asset_path/subasset_name
				string strippedPath = fullPath.Replace(HEU_Defines.HEU_SUBASSET, "");
				int lastSlash = strippedPath.LastIndexOf("/");
				if (lastSlash > 2)
				{
					mainPath = strippedPath.Substring(0, lastSlash);
					subPath = strippedPath.Substring(lastSlash + 1);
				}
			}
		}

		private static string GetAssetRelativePathStart()
		{
			return "Assets" + HEU_Platform.DirectorySeparatorStr;
		}

		/// <summary>
		/// Given relative path to an asset (with Assets/ in the path), this returns the full path to it.
		/// </summary>
		/// <param name="inAssetRelativePath">Relative path to parse</param>
		/// <returns>Returns full path to asset, or null if invalid input path</returns>
		public static string GetAssetFullPath(string inAssetRelativePath)
		{
			string replaceOld = GetAssetRelativePathStart();
			string replaceNew = Application.dataPath + HEU_Platform.DirectorySeparatorStr;
			//Debug.LogFormat("Replacing relative {0} with full {1}", inAssetRelativePath, inAssetRelativePath.Replace(replaceOld, replaceNew));
			if (IsPathRelativeToAssets(inAssetRelativePath))
			{
				return HEU_GeneralUtility.ReplaceFirstOccurrence(inAssetRelativePath, replaceOld, replaceNew);
			}
			else
			{
				Debug.LogWarningFormat("Given relative path {0} does not start with {1}. Unable to create full path!", inAssetRelativePath, replaceOld);
				return null;
			}
		}

		/// <summary>
		/// Returns true if given path starts relative to Assets/
		/// </summary>
		/// <param name="inPath">Path to check</param>
		/// <returns>True if given path starts relative to Assets/</returns>
		public static bool IsPathRelativeToAssets(string inPath)
		{
			string assetRelativeStart = GetAssetRelativePathStart();
			return inPath.StartsWith(assetRelativeStart);
		}

		public static string GetAssetRootPath(Object asset)
		{
#if UNITY_EDITOR
			string assetPath = GetAssetPath(asset);
			if(!string.IsNullOrEmpty(assetPath))
			{
				// We'll strip the path until we're under AssetCache/Baked/assetName or AssetCache/Working/assetName

				string assetTypePath = GetAssetBakedPath();
				if(!assetPath.StartsWith(assetTypePath))
				{
					assetTypePath = GetAssetWorkingPath();
					if(!assetPath.StartsWith(assetTypePath))
					{
						return null;
					}
				}

				string removedBakedPath = assetPath.Replace(assetTypePath + HEU_Platform.DirectorySeparator, "");
				string[] splits = removedBakedPath.Split(HEU_Platform.DirectorySeparator);
				if (!string.IsNullOrEmpty(splits[0]))
				{
					string rootPath = HEU_Platform.BuildPath(assetTypePath, splits[0]);
					Debug.AssertFormat(AssetDatabase.IsValidFolder(rootPath), "Calculated root path {0} is invalid for asset at {1}.", rootPath, assetPath);
					return rootPath;
				}
			}
			return null;
#else
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return null; 
#endif
		}

		/// <summary>
		/// Returns a unique path for the given path.
		/// </summary>
		/// <param name="path">The input path to find unique path for</param>
		/// <returns>A unique path for the given path.</returns>
		public static string GetUniqueAssetPath(string path)
		{
#if UNITY_EDITOR
			return AssetDatabase.GenerateUniqueAssetPath(path);
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return null;
#endif
		}

		public static string GetAssetOrScenePath(Object inputObject)
		{
#if UNITY_EDITOR
			return AssetDatabase.GetAssetOrScenePath(inputObject);
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return null;
#endif
		}

		public static bool IsPathInAssetCache(string path)
		{
			string assetDBPath = null;
			if (path.StartsWith(Application.dataPath))
			{
				assetDBPath = GetAssetRelativePath(path);
			}
			else
			{
				assetDBPath = GetAssetCachePath();
			}
			return path.StartsWith(assetDBPath);
		}

		/// <summary>
		/// Returns true if the given path is inside the Baked/ subfolder
		/// of the plugin's asset cache
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static bool IsPathInAssetCacheBakedFolder(string path)
		{
#if UNITY_EDITOR
			if (path.StartsWith(Application.dataPath))
			{
				path = GetAssetRelativePath(path);
			}
			string bakedPath = GetAssetBakedPath();
			return path.StartsWith(bakedPath);
#else
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return false;
#endif
		}

		/// <summary>
		/// Returns true if the given path is inside the Working/ subfolder
		/// of the plugin's asset cache
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static bool IsPathInAssetCacheWorkingFolder(string path)
		{
#if UNITY_EDITOR
			if (path.StartsWith(Application.dataPath))
			{
				path = GetAssetRelativePath(path);
			}
			string workingPath = GetAssetWorkingPath();
			return path.StartsWith(workingPath);
#else
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return false;
#endif
		}

		/// <summary>
		/// Returns true if the given asset is stored in the Baked/ subfolder
		/// of the plugin's asset cache.
		/// </summary>
		/// <param name="asset"></param>
		/// <returns></returns>
		public static bool IsAssetInAssetCacheBakedFolder(Object asset)
		{
#if UNITY_EDITOR
			string assetPath = GetAssetPath(asset);
			return HEU_AssetDatabase.IsPathInAssetCacheBakedFolder(assetPath);
#else
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return false;
#endif
		}

		public static bool IsAssetInAssetCacheWorkingFolder(Object asset)
		{
#if UNITY_EDITOR
			string assetPath = GetAssetPath(asset);
			return HEU_AssetDatabase.IsPathInAssetCacheWorkingFolder(assetPath);
#else
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return false;
#endif
		}

		/// <summary>
		/// Create a unique asset cache folder for the given asset path.
		/// The given asset path should be the HDA's path in the project.
		/// </summary>
		/// <param name="suggestedAssetPath">A suggested path to try. Will use default if empty or null./param>
		/// <returns>Unique asset cache folder for given asset path</returns>
		public static string CreateAssetCacheFolder(string suggestedAssetPath)
		{
#if UNITY_EDITOR
			// We create a unique folder inside our plugin's asset database cache folder.

			string assetDBPath = GetAssetCachePath();
			string assetWorkingPath = HEU_Platform.BuildPath(assetDBPath, HEU_Defines.HEU_WORKING_PATH);
			if(!AssetDatabase.IsValidFolder(assetWorkingPath))
			{
				AssetDatabase.CreateFolder(assetDBPath, HEU_Defines.HEU_WORKING_PATH);
			}

			string fileName = HEU_Platform.GetFileNameWithoutExtension(suggestedAssetPath);
			if (string.IsNullOrEmpty(fileName))
			{
				fileName = "AssetCache";
				Debug.LogWarningFormat("Unable to get file name from {0}. Using default value: {1}.", suggestedAssetPath, fileName);
			}

			string fullPath = HEU_Platform.BuildPath(assetWorkingPath, fileName);

			// Gives us the unique folder path, which we then separate out to create this folder
			fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);

			CreatePathWithFolders(fullPath);
			if (!AssetDatabase.IsValidFolder(fullPath))
			{
				Debug.LogErrorFormat("Unable to create a valid asset cache folder: {0}! Check directory permission or that enough space is available!", fullPath);
				fullPath = null;
			}

			return fullPath;
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return null;
#endif
		}

		/// <summary>
		/// Delete the asset cache folder path.
		/// </summary>
		/// <param name="assetCacheFolderPath"></param>
		public static void DeleteAssetCacheFolder(string assetCacheFolderPath)
		{
#if UNITY_EDITOR
			if (!string.IsNullOrEmpty(assetCacheFolderPath))
			{
				AssetDatabase.DeleteAsset(assetCacheFolderPath);
			}
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
#endif
		}

		/// <summary>
		/// Delete the asset object.
		/// </summary>
		/// <param name="asset">The asset object to delete</param>
		public static void DeleteAsset(Object asset)
		{
#if UNITY_EDITOR
			string assetPath = AssetDatabase.GetAssetPath(asset);
			if(!string.IsNullOrEmpty(assetPath))
			{
				AssetDatabase.DeleteAsset(assetPath);
			}
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
#endif
		}

		/// <summary>
		/// Delete the asset object.
		/// </summary>
		/// <param name="asset">The asset object to delete</param>
		public static void DeleteAssetAtPath(string path)
		{
#if UNITY_EDITOR
			if (!string.IsNullOrEmpty(path))
			{
				AssetDatabase.DeleteAsset(path);
			}
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
#endif
		}

		public static void DeleteAssetIfInBakedFolder(Object asset)
		{
#if UNITY_EDITOR
			string assetPath = GetAssetPath(asset);
			if (HEU_AssetDatabase.IsPathInAssetCacheBakedFolder(assetPath))
			{
				AssetDatabase.DeleteAsset(assetPath);
			}
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
#endif
		}

		/// <summary>
		/// Returns true if the material resides in the asset database.
		/// </summary>
		/// <param name="assetObject">The material object to check</param>
		/// <returns>True if the material resides in the asset database</returns>
		public static bool ContainsAsset(Object assetObject)
		{
#if UNITY_EDITOR
			return AssetDatabase.Contains(assetObject);
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return false;
#endif
		}

		public static bool CopyAsset(string path, string newPath)
		{
#if UNITY_EDITOR
			return AssetDatabase.CopyAsset(path, newPath);
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return false;
#endif
		}

		/// <summary>
		/// Loads a copy of the srcAsset, or if copy is not found, creates the copy and loads it.
		/// The copy is expected to be located at newAssetFolderPath/relativePath.
		/// If relativePath is null or empty, uses the srcAsset type to acquire the subfolder if the type requires it.
		/// </summary>
		/// <param name="srcAsset">Source asset whose copy will be loaded (and created if no copy exists).</param>
		/// <param name="copyAssetFolder">Asset's root folder to look for the copy or create in</param>
		/// <param name="relativePath">If not null or empty, the relative path to append to the newAssetFolderPath. 
		/// Otherwise uses type of asset to subfolder name.</param>
		/// <param name="type">Type of asset</param>
		/// <returns>Returns loaded copy if exists or created, otherwise null</returns>
		public static Object CopyAndLoadAssetWithRelativePath(Object srcAsset, string copyAssetFolder, string relativePath, System.Type type, bool bOverwriteExisting)
		{
#if UNITY_EDITOR
			string srcAssetPath = GetAssetPath(srcAsset);
			if (!string.IsNullOrEmpty(srcAssetPath) && IsPathInAssetCache(srcAssetPath))
			{
				string copyAssetFullPath = copyAssetFolder;

				if (!string.IsNullOrEmpty(relativePath))
				{
					copyAssetFullPath = HEU_Platform.BuildPath(copyAssetFullPath, relativePath);
				}
				else
				{
					if (type == typeof(Material))
					{
						copyAssetFullPath = AppendMaterialsPathToAssetFolder(copyAssetFolder);
					}
					else if (type == typeof(Texture))
					{
						copyAssetFullPath = AppendTexturesPathToAssetFolder(copyAssetFolder);
					}
					else if (type == typeof(Mesh))
					{
						copyAssetFullPath = AppendMeshesPathToAssetFolder(copyAssetFolder);
					}
					else if (type == typeof(TerrainData)
#if UNITY_2018_3_OR_NEWER
						|| (type == typeof(TerrainLayer))
#else
						|| (type == typeof(SplatPrototype))
#endif
					)
					{
						copyAssetFullPath = AppendTerrainPathToAssetFolder(copyAssetFolder);
					}
				}

				CreatePathWithFolders(copyAssetFullPath);

				string fileName = HEU_Platform.GetFileName(srcAssetPath);
				string newAssetPath = HEU_Platform.BuildPath(copyAssetFullPath, fileName);

				if ((!bOverwriteExisting && HEU_Platform.DoesFileExist(newAssetPath)) || CopyAsset(srcAssetPath, newAssetPath))
				{
					// Refresh database as otherwise we won't be able to load it in the next line.
					SaveAndRefreshDatabase();

					return LoadAssetAtPath(newAssetPath, type);
				}
				else
				{
					Debug.LogErrorFormat("Failed to copy and load asset from {0} to {1}!", srcAssetPath, newAssetPath);
				}
			}
			return null;
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return null;
#endif
		}

		/// <summary>
		/// Loads a copy of the srcAsset at copyPath, which must reside in the asset cache. Creates a copy if not found.
		/// This does nothing if srcAsset resides outside the asset cache.
		/// </summary>
		/// <param name="srcAsset">The source asset object</param>
		/// <param name="copyPath">The full path to the copy</param>
		/// <param name="type">The type of source asset</param>
		/// <param name="bOverwriteExisting">Whether to overwrite existing copy if found</param>
		/// <returns>Returns loaded copy if exists or created, otherwise null</returns>
		public static Object CopyAndLoadAssetFromAssetCachePath(Object srcAsset, string copyPath, System.Type type, bool bOverwriteExisting)
		{
#if UNITY_EDITOR
			string srcAssetPath = GetAssetPath(srcAsset);
			if (!string.IsNullOrEmpty(srcAssetPath) && IsPathInAssetCache(srcAssetPath))
			{
				return CopyAndLoadAssetAtAnyPath(srcAsset, copyPath, type, bOverwriteExisting);
			}
			return null;
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return null;
#endif
		}

		/// <summary>
		/// Loads a copy of the srcAsset at copyPath. Creates a copy if not found.
		/// </summary>
		/// <param name="srcAsset">The source asset object</param>
		/// <param name="copyPath">The full path to the copy</param>
		/// <param name="type">The type of source asset</param>
		/// <param name="bOverwriteExisting">Whether to overwrite existing copy if found</param>
		/// <returns>Returns loaded copy if exists or created, otherwise null</returns>
		public static Object CopyAndLoadAssetAtAnyPath(Object srcAsset, string copyPath, System.Type type, bool bOverwriteExisting)
		{
#if UNITY_EDITOR
			string srcAssetPath = GetAssetPath(srcAsset);
			if (!string.IsNullOrEmpty(srcAssetPath))
			{
				CreatePathWithFolders(copyPath);

				string fileName = HEU_Platform.GetFileName(srcAssetPath);
				string fullCopyPath = HEU_Platform.BuildPath(copyPath, fileName);

				if ((!bOverwriteExisting && HEU_Platform.DoesFileExist(fullCopyPath)) || CopyAsset(srcAssetPath, fullCopyPath))
				{
					// Refresh database as otherwise we won't be able to load it in the next line.
					SaveAndRefreshDatabase();

					return LoadAssetAtPath(fullCopyPath, type);
				}
				else
				{
					Debug.LogErrorFormat("Failed to copy and load asset from {0} to {1}!", srcAssetPath, fullCopyPath);
				}
			}
			return null;
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return null;
#endif
		}

		/// <summary>
		/// Copy the file of the given srcAsset into the given targetPath, which must be absolute.
		/// If targetPath doesn't have a file name, the srcAsset's file name will be used.
		/// </summary>
		/// <param name="srcAsset">Source asset to copy</param>
		/// <param name="targetPath">Absolute path of destination</param>
		/// <param name="type">Type of the asset</param>
		/// <returns></returns>
		public static Object CopyAndLoadAssetAtGivenPath(Object srcAsset, string targetPath, System.Type type)
		{
#if UNITY_EDITOR
			string srcAssetPath = GetAssetPath(srcAsset);
			if (!string.IsNullOrEmpty(srcAssetPath))
			{
				string targetFolderPath = HEU_Platform.GetFolderPath(targetPath);
				CreatePathWithFolders(targetFolderPath);

				string targetFileName = HEU_Platform.GetFileName(targetPath);
				if (string.IsNullOrEmpty(targetFileName))
				{
					Debug.LogErrorFormat("Copying asset failed. Destination path must end with a file name: {0}!", targetPath);
					return null;
				}

				if (CopyAsset(srcAssetPath, targetPath))
				{
					// Refresh database as otherwise we won't be able to load it in the next line.
					SaveAndRefreshDatabase();

					return LoadAssetAtPath(targetPath, type);
				}
				else
				{
					Debug.LogErrorFormat("Failed to copy and load asset from {0} to {1}!", srcAssetPath, targetPath);
				}
			}
			return null;
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return null;
#endif
		}

		/// <summary>
		/// Creates a unique copy of the srcAsset at copyPath, and loads it.
		/// If another asset is at copyPath, it creates another (unique) file name.
		/// </summary>
		/// <param name="srcAsset">The source asset object</param>
		/// <param name="copyPath">The full path to the copy</param>
		/// <param name="type">The type of source asset</param>
		/// <returns>Returns loaded copy if exists or created, otherwise null</returns>
		public static Object CopyUniqueAndLoadAssetAtAnyPath(Object srcAsset, string copyPath, System.Type type)
		{
#if UNITY_EDITOR
			string srcAssetPath = GetAssetPath(srcAsset);
			if (!string.IsNullOrEmpty(srcAssetPath))
			{
				CreatePathWithFolders(copyPath);

				string fileName = HEU_Platform.GetFileName(srcAssetPath);
				string fullCopyPath = HEU_Platform.BuildPath(copyPath, fileName);

				if (HEU_Platform.DoesFileExist(fullCopyPath))
				{
					fullCopyPath = GetUniqueAssetPath(fullCopyPath);
					if (HEU_Platform.DoesFileExist(fullCopyPath))
					{
						Debug.LogErrorFormat("Failed to get unique path to make copy for {0} at {1}!", srcAssetPath, fullCopyPath);
						return null;
					}
				}

				if (CopyAsset(srcAssetPath, fullCopyPath))
				{
					// Refresh database as otherwise we won't be able to load it in the next line.
					SaveAndRefreshDatabase();

					return LoadAssetAtPath(fullCopyPath, type);
				}
				else
				{
					Debug.LogErrorFormat("Failed to copy and load asset from {0} to {1}!", srcAssetPath, fullCopyPath);
				}
			}
			return null;
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return null;
#endif
		}

		/// <summary>
		/// Create the given object inside the asset cache folder path, with relative folder path.
		/// Depending on type, it might store in a subfolder for organizational purposes.
		/// </summary>
		/// <param name="objectToCreate">The object to create inside the asset cache</param>
		/// <param name="assetCacheRoot">The target path in the asset cache</param>
		/// <param name="relativeFolderPath">If not null or empty, the relative path to append to the assetCacheRoot. 
		/// Otherwise uses type of asset to get subfolder name.</param>
		/// <param name="assetFileName">The asset's file name</param>
		/// <param name="type">The type of asset</param>
		public static void CreateObjectInAssetCacheFolder(Object objectToCreate, string assetCacheRoot, string relativeFolderPath, string assetFileName, System.Type type)
		{
#if UNITY_EDITOR
			Debug.Assert(!string.IsNullOrEmpty(assetCacheRoot), "Must give valid assetCacheFolderPath to create object at");

			string subFolderPath = assetCacheRoot;

			if (!string.IsNullOrEmpty(relativeFolderPath))
			{
				subFolderPath = HEU_Platform.BuildPath(subFolderPath, relativeFolderPath);
			}
			else
			{
				if (type == typeof(Mesh))
				{
					subFolderPath = AppendMeshesPathToAssetFolder(assetCacheRoot);
				}
				else if (type == typeof(Material))
				{
					subFolderPath = AppendMaterialsPathToAssetFolder(assetCacheRoot);
				}
				else if (type == typeof(TerrainData)
#if UNITY_2018_3_OR_NEWER
				|| (type == typeof(TerrainLayer))
#else
				|| (type == typeof(SplatPrototype))
#endif
				)
				{
					subFolderPath = AppendTerrainPathToAssetFolder(assetCacheRoot);
				}
			}

			// Make sure subfolders exist
			HEU_AssetDatabase.CreatePathWithFolders(subFolderPath);

			// Add file name
			subFolderPath = HEU_Platform.BuildPath(subFolderPath, assetFileName);

			AssetDatabase.CreateAsset(objectToCreate, subFolderPath);

			// Commented out AssetDatabase.Refresh() below because its slow and seems to be unnecessary.
			// Leaving it commented in case need to revisit due to problems with asset creation.
			//RefreshAssetDatabase();
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
#endif
		}

		public static void CreateAsset(Object asset, string path)
		{
#if UNITY_EDITOR
			AssetDatabase.CreateAsset(asset, path);
#else
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
#endif
		}

		public static void CreateAddObjectInAssetCacheFolder(string assetName, string assetObjectFileName, UnityEngine.Object objectToAdd, string relativeFolderPath, ref string exportRootPath, ref UnityEngine.Object assetDBObject)
		{
#if UNITY_EDITOR
			if (string.IsNullOrEmpty(exportRootPath))
			{
				exportRootPath = HEU_AssetDatabase.CreateUniqueBakePath(assetName);
			}

			if (assetDBObject == null)
			{
				HEU_AssetDatabase.CreateObjectInAssetCacheFolder(objectToAdd, exportRootPath, relativeFolderPath, assetObjectFileName, objectToAdd.GetType());
				assetDBObject = objectToAdd;
			}
			else
			{
				HEU_AssetDatabase.AddObjectToAsset(objectToAdd, assetDBObject);
			}
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
#endif
		}

		public static void AddObjectToAsset(UnityEngine.Object objectToAdd, UnityEngine.Object assetObject)
		{
#if UNITY_EDITOR
			AssetDatabase.AddObjectToAsset(objectToAdd, assetObject);
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
#endif
		}

		/// <summary>
		/// Saves all assets to disk, and refreshes for loading.
		/// </summary>
		public static void SaveAndRefreshDatabase()
		{
#if UNITY_EDITOR
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
#endif
		}

		/// <summary>
		/// Save the Unity asset database.
		/// </summary>
		public static void SaveAssetDatabase()
		{
#if UNITY_EDITOR
			AssetDatabase.SaveAssets();
#endif
		}

		/// <summary>
		/// Refresh the Unity asset database.
		/// Wrapping this as its slow so would be good to track usage.
		/// </summary>
		public static void RefreshAssetDatabase()
		{
#if UNITY_EDITOR
			AssetDatabase.Refresh();
#endif
		}

		/// <summary>
		/// Load the asset at the given path, and return the object.
		/// </summary>
		/// <param name="assetPath">The asset's path</param>
		/// <param name="type">The expected type of asset</param>
		/// <returns>The loaded object</returns>
		public static Object LoadAssetAtPath(string assetPath, System.Type type)
		{
#if UNITY_EDITOR
			return AssetDatabase.LoadAssetAtPath(assetPath, type);
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return null;
#endif
		}

		/// <summary>
		/// Loads and returns the subasset at the given main path, with subasset name.
		/// </summary>
		/// <param name="mainPath">The path to the container</param>
		/// <param name="subAssetPath">The name of the subasset within the container</param>
		/// <returns>The subasset object found or null if not</returns>
		public static Object LoadSubAssetAtPath(string mainPath, string subAssetPath)
		{
			Object[] subObjects = HEU_AssetDatabase.LoadAllAssetRepresentationsAtPath(mainPath);
			if (subObjects != null)
			{
				int numSubObjects = subObjects.Length;
				for (int i = 0; i < numSubObjects; ++i)
				{
					if (subObjects[i].name.Equals(subAssetPath))
					{
						return subObjects[i];
					}
				}
			}
			return null;
		}

		public static Object[] LoadAllAssetsAtPath(string assetPath)
		{
#if UNITY_EDITOR
			return AssetDatabase.LoadAllAssetsAtPath(assetPath);
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return null;
#endif
		}

		public static Object[] LoadAllAssetRepresentationsAtPath(string assetPath)
		{
#if UNITY_EDITOR
			return AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
#else
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return null;
#endif
		}

		// Stand-in for Unity's Import Options
		public enum HEU_ImportAssetOptions
		{
			// Default import options.
			Default = 0,

			// User initiated asset import.
			ForceUpdate = 1,

			// Import all assets synchronously.
			ForceSynchronousImport = 8,

			// When a folder is imported, import all its contents as well.
			ImportRecursive = 256,

			// Force a full reimport but don't download the assets from the cache server.
			DontDownloadFromCacheServer = 8192,

			// Forces asset import as uncompressed for edition facilities.
			ForceUncompressedImport = 16384
		}

		/// <summary>
		/// Import the asset at the given path.
		/// </summary>
		/// <param name="assetPath"></param>
		/// <param name="options"></param>
		public static void ImportAsset(string assetPath, HEU_ImportAssetOptions heuOptions)
		{
#if UNITY_EDITOR
			ImportAssetOptions unityOptions = ImportAssetOptions.Default;
			switch(heuOptions)
			{
				case HEU_ImportAssetOptions.Default:						unityOptions = ImportAssetOptions.Default; break;
				case HEU_ImportAssetOptions.ForceUpdate:					unityOptions = ImportAssetOptions.ForceUpdate; break;
				case HEU_ImportAssetOptions.ForceSynchronousImport:			unityOptions = ImportAssetOptions.ForceSynchronousImport; break;
				case HEU_ImportAssetOptions.ImportRecursive:				unityOptions = ImportAssetOptions.ImportRecursive; break;
				case HEU_ImportAssetOptions.DontDownloadFromCacheServer:	unityOptions = ImportAssetOptions.DontDownloadFromCacheServer; break;
				case HEU_ImportAssetOptions.ForceUncompressedImport:		unityOptions = ImportAssetOptions.ForceUncompressedImport; break;
				default: Debug.LogWarningFormat("Unsupported import options: {0}", heuOptions); break;
			}

			AssetDatabase.ImportAsset(assetPath, unityOptions);
#else
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
#endif
		}

		public static string GetAssetWorkingPath()
		{
#if UNITY_EDITOR
			string dbRoot = GetAssetCachePath();
			string workingPath = HEU_Platform.BuildPath(dbRoot, HEU_Defines.HEU_WORKING_PATH);

			if (!AssetDatabase.IsValidFolder(workingPath))
			{
				AssetDatabase.CreateFolder(dbRoot, HEU_Defines.HEU_WORKING_PATH);
			}

			return workingPath;
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return null;
#endif
		}

		public static string GetAssetBakedPath()
		{
#if UNITY_EDITOR
			string dbRoot = GetAssetCachePath();
			string bakedPath = HEU_Platform.BuildPath(dbRoot, HEU_Defines.HEU_BAKED_PATH);

			if (!AssetDatabase.IsValidFolder(bakedPath))
			{
				AssetDatabase.CreateFolder(dbRoot, HEU_Defines.HEU_BAKED_PATH);
			}

			return bakedPath;
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return null;
#endif
		}

		public static string GetAssetBakedPathWithAssetName(string assetName)
		{
#if UNITY_EDITOR
			return HEU_Platform.BuildPath(GetAssetBakedPath(), assetName);
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return null;
#endif
		}

		public static string CreateUniqueBakePath(string assetName)
		{
#if UNITY_EDITOR
			string assetBakedPath = GetAssetBakedPathWithAssetName(assetName);
			assetBakedPath = AssetDatabase.GenerateUniqueAssetPath(assetBakedPath);

			if (!HEU_Platform.DoesPathExist(assetBakedPath))
			{
				CreatePathWithFolders(assetBakedPath);
			}

			return assetBakedPath;
#else
			// TODO RUNTIME: AssetDatabase is not supported at runtime. Do we need to support this for runtime?
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return null;
#endif
		}

		/// <summary>
		/// Creates all folders in the given path if they don't exist.
		/// </summary>
		/// <param name="inPath">The path to create folders for</param>
		public static void CreatePathWithFolders(string inPath)
		{
#if UNITY_EDITOR
			string pathBuild = "";
			string[] folders = inPath.Split(HEU_Platform.DirectorySeparator);
			foreach(string folder in folders)
			{
				if(string.IsNullOrEmpty(folder))
				{
					break;
				}

				string nextPath = "";
				if (string.IsNullOrEmpty(pathBuild))
				{
					nextPath = folder;
				}
				else
				{
					nextPath = HEU_Platform.BuildPath(pathBuild, folder);
				}

				if (!AssetDatabase.IsValidFolder(nextPath))
				{
					//Debug.LogFormat("{0}: Creating folder: {1}/{2}", HEU_Defines.HEU_NAME, pathBuild, folder);
					AssetDatabase.CreateFolder(pathBuild, folder);
				}

				pathBuild = nextPath;
			}
#else
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
#endif
		}

		public static string AppendMeshesPathToAssetFolder(string inAssetCacheFolder)
		{
			return HEU_Platform.BuildPath(inAssetCacheFolder, HEU_Defines.HEU_FOLDER_MESHES);
		}

		public static string AppendTexturesPathToAssetFolder(string inAssetCacheFolder)
		{
			return HEU_Platform.BuildPath(inAssetCacheFolder, HEU_Defines.HEU_FOLDER_TEXTURES);
		}

		public static string AppendMaterialsPathToAssetFolder(string inAssetCacheFolder)
		{
			return HEU_Platform.BuildPath(inAssetCacheFolder, HEU_Defines.HEU_FOLDER_MATERIALS);
		}

		public static string AppendTerrainPathToAssetFolder(string inAssetCacheFolder)
		{
			return HEU_Platform.BuildPath(inAssetCacheFolder, HEU_Defines.HEU_FOLDER_TERRAIN);
		}

		public static string[] GetAssetSubFolders()
		{
			return new string[] 
			{
				HEU_Defines.HEU_FOLDER_MESHES,
				HEU_Defines.HEU_FOLDER_TEXTURES,
				HEU_Defines.HEU_FOLDER_MATERIALS,
				HEU_Defines.HEU_FOLDER_TERRAIN
			};
		}

		public static string AppendPrefabPath(string inAssetCacheFolder, string assetName)
		{
			string prefabPath = HEU_Platform.BuildPath(inAssetCacheFolder, assetName);
			return prefabPath + ".prefab";
		}

		public static string AppendMeshesAssetFileName(string assetName)
		{
			return assetName + "_meshes.asset";
		}

		public static bool IsSubAsset(Object obj)
		{
#if UNITY_EDITOR
			return (obj != null) ? AssetDatabase.IsSubAsset(obj) : false;
#else
			return false;
#endif
		}

		public static string[] GetAssetPathsFromAssetBundle(string assetBundleFileName)
		{
#if UNITY_EDITOR
			return AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleFileName);
#else
			return null;
#endif
		}

		/// <summary>
		/// Returns true if this gameobject has been saved in a scene.
		/// </summary>
		/// <returns>True if gameobject has been saved in a scene.</returns>
		public static bool IsAssetSavedInScene(GameObject go)
		{
#if UNITY_EDITOR
			string scenePath = GetAssetOrScenePath(go);
			return !string.IsNullOrEmpty(scenePath);
#else
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
			return false;
#endif
		}

		public static void PrintDependencies(GameObject targetGO)
		{
#if UNITY_EDITOR
			Debug.Log("Print Dependcies: target: " + targetGO.name);
			UnityEngine.Object[] depends = HEU_EditorUtility.CollectDependencies(targetGO);
			foreach (UnityEngine.Object obj in depends)
			{
				Debug.LogFormat("Dependent: name={0}, type={1}, path={2}, persist={3}, native={4}", obj.name, obj.GetType().ToString(), AssetDatabase.GetAssetOrScenePath(obj), EditorUtility.IsPersistent(obj),
					AssetDatabase.IsNativeAsset(obj));
			}
#endif
		}

		public static string GetUniqueAssetPathForUnityAsset(UnityEngine.Object obj)
		{
			string assetPath = GetAssetPath(obj);
			if(!string.IsNullOrEmpty(obj.name))
			{
				assetPath += "::name::" + obj.name;
			}
			else
			{
				assetPath += "::id::" + obj.GetInstanceID();
			}
			return assetPath;
		}

		public static T LoadUnityAssetFromUniqueAssetPath<T>(string assetPath) where T : UnityEngine.Object
		{
			// Expecting assetPath to be of format: assetPath::name::assetname OR assetPath::id::assetid
			// See GetUniqueAssetPathForUnityAsset()
			if (assetPath.Contains("::"))
			{
				string[] splits = assetPath.Split(new string[] { "::" }, System.StringSplitOptions.RemoveEmptyEntries);
				assetPath = splits[0];
				if(splits.Length > 2)
				{
					bool nameType = splits[1].Equals("name");
					string assetName = splits[2];
					int assetID = 0;

					if (!nameType)
					{
						// This is using ID type, so get the ID
						if(!int.TryParse(splits[2], out assetID))
						{
							return null;
						}
					}

					System.Type t = typeof(T);
					Object[] objects = LoadAllAssetsAtPath(assetPath);
					foreach(Object obj in objects)
					{
						if(obj.GetType() == t)
						{
							if (nameType)
							{
								if (obj.name.Equals(assetName))
								{
									return obj as T;
								}
							}
							else if(obj.GetInstanceID() == assetID)
							{
								return obj as T;
							}
						}
					}
				}
			}
			return null;
		}

		public static T GetBuiltinExtraResource<T>(string resourceName) where T : Object
		{
#if UNITY_EDITOR
			return AssetDatabase.GetBuiltinExtraResource<T>(resourceName);
#else
			return null;
#endif
		}
	}

}   // HoudiniEngineUnity