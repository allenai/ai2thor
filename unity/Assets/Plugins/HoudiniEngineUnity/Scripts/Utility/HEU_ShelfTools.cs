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
using System.IO;


namespace HoudiniEngineUnity
{
	/// <summary>
	/// Container of a tool's metadata
	/// </summary>
	[System.Serializable]
	public class HEU_ShelfToolData
	{
		public string _name = "";

		public enum ToolType
		{
			GENERATOR,
			OPERATOR_SINGLE,
			OPERATOR_MULTI,
			BATCH
		}

		public ToolType _toolType;

		public string _toolTip = "";

		public string _iconPath = "";

		public string _assetPath = "";

		public string _helpURL = "";

		public string[] _targets;

		public string _jsonPath = "";
	}

	[System.Serializable]
	public class HEU_Shelf
	{
		public string _shelfName;

		public string _shelfPath;

		public bool _defaultShelf;

		public List<HEU_ShelfToolData> _tools = new List<HEU_ShelfToolData>();
	}

	public class HEU_ShelfTools
	{
		[SerializeField]
		private static List<HEU_Shelf> _shelves = new List<HEU_Shelf>();

		[SerializeField]
		private static bool _shelvesLoaded = false;

		[SerializeField]
		private static int _currentSelectedShelf;

		// Target values to check for compatibility with this plugin
		public const string TARGET_ALL = "all";
		public const string TARGET_UNITY = "unity";


		public static bool AreShelvesLoaded()
		{
			return _shelvesLoaded;
		}

		public static void SetReloadShelves()
		{
			_shelvesLoaded = false;
		}

		public static void ClearShelves()
		{
			_shelves.Clear();
		}

		public static int GetNumShelves()
		{
			return _shelves.Count;
		}

		public static int GetCurrentShelfIndex()
		{
			return _currentSelectedShelf;
		}

		public static void SetCurrentShelf(int index)
		{
			_currentSelectedShelf = index;
		}

		public static HEU_Shelf GetShelf(int index)
		{
			return (index >= 0 && index < _shelves.Count) ? _shelves[index] : null;
		}

		public static HEU_Shelf GetShelf(string shelfName)
		{
			foreach (HEU_Shelf shelf in _shelves)
			{
				if (shelf._shelfName.Equals(shelfName))
				{
					return shelf;
				}
			}
			return null;
		}

		public static string GetShelfStorageEntry(string shelfName, string shelfPath)
		{
			return string.Format("{0}={1}", shelfName, shelfPath);
		}

		public static void GetSplitShelfEntry(string shelfEntry, out string shelfName, out string shelfPath)
		{
			shelfName = "";
			shelfPath = "";

			int index = shelfEntry.IndexOf("=");
			if(index > 0 && index < shelfEntry.Length)
			{
				shelfName = shelfEntry.Substring(0, index);
				shelfPath = shelfEntry.Substring(index + 1);
			}
		}

		public static void LoadShelves()
		{
			bool bSaveShelf = false;

			_shelves.Clear();

			// Always add the default shelf
			HEU_Shelf defaultShelf = AddShelf(HEU_Defines.HEU_HENGINE_SHIPPED_SHELF, HEU_Defines.HEU_HENGINE_TOOLS_SHIPPED_FOLDER);
			defaultShelf._defaultShelf = true;

			List<string> shelfEntries = HEU_PluginSettings.HEngineToolsShelves;
			if (shelfEntries == null || shelfEntries.Count == 0)
			{
				shelfEntries = new List<string>();
			}

			// Convert shelf path + name to actual shelf objects
			int numShelves = shelfEntries.Count;
			for(int i = 0; i < numShelves; i++)
			{
				string shelfName = "";
				string shelfPath = "";

				GetSplitShelfEntry(shelfEntries[i], out shelfName, out shelfPath);

				// Ignore default shelf because we added it already
				if(shelfPath.Equals(HEU_Defines.HEU_HENGINE_TOOLS_SHIPPED_FOLDER))
				{
					continue;
				}

				if(!string.IsNullOrEmpty(shelfName) && !string.IsNullOrEmpty(shelfPath))
				{
					HEU_Shelf newShelf = new HEU_Shelf();
					newShelf._shelfName = shelfName;
					newShelf._shelfPath = shelfPath;

					_shelves.Add(newShelf);
				}
				else
				{
					Debug.LogWarningFormat("Found invalid shelf with entry: {0}", shelfEntries[i]);
					shelfEntries.RemoveAt(i);
					i--;
					bSaveShelf = true;
				}
			}

			foreach(HEU_Shelf shelf in _shelves)
			{
				string realShelfPath = HEU_HAPIUtility.GetRealPathFromHFSPath(shelf._shelfPath);

				if (!HEU_Platform.DoesPathExist(realShelfPath))
				{
					Debug.LogWarningFormat("Shelf path does not exist: {0}", realShelfPath);
				}
				else
				{
					bool bShelfLoaded = LoadToolsFromDirectory(realShelfPath, out shelf._tools);
					if (!bShelfLoaded)
					{
						Debug.LogWarningFormat("Failed to load shelf {0} at path {1}", shelf._shelfName, realShelfPath);
					}
				}
			}

			_currentSelectedShelf = HEU_PluginSettings.HEngineShelfSelectedIndex;
			if (_currentSelectedShelf < 0 || _currentSelectedShelf >= _shelves.Count)
			{
				_currentSelectedShelf = 0;
				HEU_PluginSettings.HEngineShelfSelectedIndex = _currentSelectedShelf;
			}

			if (bSaveShelf)
			{
				SaveShelf();
			}

			_shelvesLoaded = true;
		}

		public static bool LoadToolsFromDirectory(string folderPath, out List<HEU_ShelfToolData> tools)
		{
			tools = new List<HEU_ShelfToolData>();

			string[] filePaths = HEU_Platform.GetFilesInFolder(folderPath, "*.json", true);
			bool bResult = false;
			try
			{
				if (filePaths != null)
				{
					foreach (string fileName in filePaths)
					{
						HEU_ShelfToolData tool = LoadToolFromJsonFile(fileName);
						if(tool != null)
						{
							tools.Add(tool);
						}
					}

					bResult = true;
				}
			}
			catch(System.Exception ex)
			{
				Debug.LogErrorFormat("Parsing JSON files in directory caused exception: {0}", ex);
				return false;
			}

			return bResult;
		}

		public static HEU_ShelfToolData LoadToolFromJsonFile(string jsonFilePath)
		{
			string json = null;
			try
			{
				StreamReader fileReader = new StreamReader(jsonFilePath);
				json = fileReader.ReadToEnd();
				fileReader.Close();
			}
			catch(System.Exception ex)
			{
				Debug.LogErrorFormat("Exception while reading {0}: {1}", jsonFilePath, ex);
				return null;
			}

			HEU_ShelfToolData tool = LoadToolFromJsonString(json, jsonFilePath);
			return tool;
		}

		public static HEU_ShelfToolData LoadToolFromJsonString(string json, string jsonFilePath)
		{
			//Debug.Log("Loading json: " + jsonFilePath);

			// Get environment variable for tool path
			string envValue = HEU_Platform.GetEnvironmentValue(HEU_Defines.HEU_PATH_KEY_TOOL);
			string envKey = string.Format("<{0}>", HEU_Defines.HEU_PATH_KEY_TOOL);

			HEU_ShelfToolData toolData = null;

			if (!string.IsNullOrEmpty(json))
			{
				try
				{
					JSONNode jsonShelfNode = JSON.Parse(json);
					if (jsonShelfNode != null)
					{
						bool isObject = jsonShelfNode.IsObject;
						bool isArray = jsonShelfNode.IsArray;

						toolData = new HEU_ShelfToolData();

						toolData._name = jsonShelfNode["name"];

						toolData._toolType = (HEU_ShelfToolData.ToolType)System.Enum.Parse(typeof(HEU_ShelfToolData.ToolType), jsonShelfNode["toolType"]);

						toolData._toolTip = jsonShelfNode["toolTip"];

						toolData._iconPath = jsonShelfNode["iconPath"];

						toolData._assetPath = jsonShelfNode["assetPath"];

						toolData._helpURL = jsonShelfNode["helpURL"];

						JSONArray targetArray = jsonShelfNode["target"].AsArray;
						if(targetArray != null)
						{
							int targetCount = targetArray.Count;
							toolData._targets = new string[targetCount];
							for(int j = 0; j < targetCount; ++j)
							{
								toolData._targets[j] = targetArray[j];
							}
						}
	}
				}
				catch (System.Exception ex)
				{
					Debug.LogErrorFormat("Exception when trying to parse shelf json file at path: {0}. Exception: {1}", jsonFilePath, ex.ToString());
					return null;
				}

				toolData._jsonPath = jsonFilePath;

				if (toolData != null && !string.IsNullOrEmpty(toolData._name))
				{
					// Make sure this tool targets Unity (must have "all" or "unity" set in target field)
					bool bCompatiple = false;
					if(toolData._targets != null)
					{
						int numTargets = toolData._targets.Length;
						for(int i = 0; i < numTargets; ++i)
						{
							if (toolData._targets[i].Equals(TARGET_ALL) || toolData._targets[i].Equals(TARGET_UNITY))
							{
								bCompatiple = true;
								break;
							}
						}
					}

					if (bCompatiple)
					{
						if (!string.IsNullOrEmpty(toolData._assetPath))
						{
							toolData._assetPath = toolData._assetPath.Replace(HEU_Defines.HEU_PATH_KEY_PROJECT + "/", "");
							if (toolData._assetPath.Contains(envKey))
							{
								if (string.IsNullOrEmpty(envValue))
								{
									Debug.LogErrorFormat("Environment value {0} used but not set in environment.", HEU_Defines.HEU_PATH_KEY_TOOL);
								}
								else
								{
									toolData._assetPath = toolData._assetPath.Replace(envKey, envValue);
								}
							}
						}
						else
						{
							toolData._assetPath = GetToolAssetPath(toolData, toolData._assetPath);
						}

						string realPath = HEU_PluginStorage.Instance.ConvertEnvKeyedPathToReal(toolData._assetPath);
						if (!HEU_Platform.DoesFileExist(realPath))
						{
							Debug.LogErrorFormat("Houdini Engine shelf tool at {0} does not exist!", realPath);
							return null;
						}

						if (!string.IsNullOrEmpty(toolData._iconPath))
						{
							toolData._iconPath = toolData._iconPath.Replace(HEU_Defines.HEU_PATH_KEY_PROJECT + "/", "");
							if (toolData._iconPath.Contains(envKey))
							{
								if (string.IsNullOrEmpty(envValue))
								{
									Debug.LogErrorFormat("Environment value {0} used but not set in environment.", HEU_Defines.HEU_PATH_KEY_TOOL);
								}
								else
								{
									toolData._iconPath = toolData._iconPath.Replace(envKey, envValue);
								}
							}
						}
						else
						{
							toolData._iconPath = GetToolIconPath(toolData, toolData._iconPath);
						}

						return toolData;
					}
				}
			}

			return null;
		}

		public static HEU_Shelf AddShelf(string shelfName, string shelfPath)
		{
			HEU_Shelf newShelf = new HEU_Shelf();
			newShelf._shelfName = shelfName;
			newShelf._shelfPath = shelfPath;

			_shelves.Add(newShelf);
			return newShelf;
		}

		public static void RemoveShelf(int shelfIndex)
		{
			if(shelfIndex >= 0 && shelfIndex < _shelves.Count)
			{
				if(!_shelves[shelfIndex]._defaultShelf)
				{
					_shelves.RemoveAt(shelfIndex);

					if(_currentSelectedShelf == shelfIndex)
					{
						_currentSelectedShelf = Mathf.Max(0, _currentSelectedShelf - 1);
					}
				}
			}
		}

		public static void SaveShelf()
		{
			List<string> shelfEntries = new List<string>();

			foreach(HEU_Shelf shelf in _shelves)
			{
				// Don't save default shelf since we always keep it around.
				if (!shelf._defaultShelf)
				{
					shelfEntries.Add(GetShelfStorageEntry(shelf._shelfName, shelf._shelfPath));
				}
			}

			HEU_PluginSettings.HEngineToolsShelves = shelfEntries;

			HEU_PluginSettings.HEngineShelfSelectedIndex = _currentSelectedShelf;
		}


		public static void ExecuteTool(int toolSlot)
		{
			if(_currentSelectedShelf < 0 && _currentSelectedShelf >= _shelves.Count)
			{
				Debug.LogWarning("Invalid shelf selected. Unable to apply tool.");
				return;
			}

			if(toolSlot < 0 || toolSlot >= _shelves[_currentSelectedShelf]._tools.Count)
			{
				Debug.LogWarning("Invalid tool selected. Unable to apply tool.");
				return;
			}

			HEU_ShelfToolData toolData = _shelves[_currentSelectedShelf]._tools[toolSlot];

			GameObject[] selectedObjects = HEU_EditorUtility.GetSelectedObjects();

			if (toolData._toolType == HEU_ShelfToolData.ToolType.GENERATOR)
			{
				Matrix4x4 targetMatrix = HEU_EditorUtility.GetSelectedObjectsMeanTransform();
				Vector3 position = HEU_HAPIUtility.GetPosition(ref targetMatrix);
				Quaternion rotation = HEU_HAPIUtility.GetQuaternion(ref targetMatrix);
				Vector3 scale = HEU_HAPIUtility.GetScale(ref targetMatrix);
				scale = Vector3.one;

				ExecuteToolGenerator(toolData._name, toolData._assetPath, position, rotation, scale);
			}
			else if(selectedObjects.Length == 0)
			{
				ExecuteToolNoInput(toolData._name, toolData._assetPath);
			}
			else if(toolData._toolType == HEU_ShelfToolData.ToolType.OPERATOR_SINGLE)
			{
				ExecuteToolOperatorSingle(toolData._name, toolData._assetPath, selectedObjects);
			}
			else if (toolData._toolType == HEU_ShelfToolData.ToolType.OPERATOR_MULTI)
			{
				ExecuteToolOperatorMultiple(toolData._name, toolData._assetPath, selectedObjects);
			}
			else if (toolData._toolType == HEU_ShelfToolData.ToolType.BATCH)
			{
				ExecuteToolBatch(toolData._name, toolData._assetPath, selectedObjects);
			}
		}

		public static void ExecuteToolGenerator(string toolName, string toolPath, Vector3 targetPosition, Quaternion targetRotation, Vector3 targetScale)
		{
			GameObject go = HEU_HAPIUtility.InstantiateHDA(toolPath, targetPosition, HEU_SessionManager.GetOrCreateDefaultSession(), true);
			if (go != null)
			{
				go.transform.rotation = targetRotation;
				go.transform.localScale = targetScale;

				HEU_EditorUtility.SelectObject(go);
			}
			else
			{
				Debug.LogWarningFormat("Failed to instantiate tool: {0}", toolName);
			}
		}

		public static bool IsValidInput(GameObject gameObject)
		{
			if(gameObject != null)
			{
				MeshFilter meshfilter = gameObject.GetComponent<MeshFilter>();
				return (meshfilter != null && meshfilter.sharedMesh != null);
			}
			return false;
		}

		public static void ExecuteToolNoInput(string toolName, string toolPath)
		{
			GameObject go = HEU_HAPIUtility.InstantiateHDA(toolPath, Vector3.zero, HEU_SessionManager.GetOrCreateDefaultSession(), false);
			if (go == null)
			{
				Debug.LogWarningFormat("Failed to instantiate tool: {0}", toolName);
			}
			else
			{
				HEU_EditorUtility.SelectObject(go);
			}
		}

		public static void ExecuteToolOperatorSingle(string toolName, string toolPath, GameObject[] inputObjects)
		{
			// Single operator means single asset input. If multiple inputs are provided, create tool for each input.

			List<GameObject> outputObjectsToSelect = new List<GameObject>();

			int numInputs = inputObjects.Length;
			for (int i = 0; i < numInputs; ++i)
			{
				if(!IsValidInput(inputObjects[i]))
				{
					continue;
				}
				GameObject inputObject = inputObjects[i];

				GameObject go = HEU_HAPIUtility.InstantiateHDA(toolPath, Vector3.zero, HEU_SessionManager.GetOrCreateDefaultSession(), false);
				if (go != null)
				{
					HEU_HoudiniAssetRoot assetRoot = go.GetComponent<HEU_HoudiniAssetRoot>();
					if (assetRoot != null)
					{
						HEU_HoudiniAsset asset = assetRoot._houdiniAsset;
						HEU_SessionBase session = asset.GetAssetSession(true);

						List<HEU_InputNode> inputNodes = asset.GetInputNodes();
						if (inputNodes == null || inputNodes.Count == 0)
						{
							Debug.LogErrorFormat("Unable to assign input geometry due to no asset inputs on selected tool.");
						}
						else
						{
							HEU_InputNode inputNode = inputNodes[0];

							inputNode.ResetInputNode(session);

							inputNode.ChangeInputType(session, HEU_InputNode.InputObjectType.UNITY_MESH);

							HEU_InputObjectInfo inputInfo = inputNode.AddInputEntryAtEnd(inputObject);
							inputInfo._useTransformOffset = false;
							inputNode.KeepWorldTransform = true;
							inputNode.PackGeometryBeforeMerging = false;

							inputNode.RequiresUpload = true;

							asset.RequestCook(true, true, true, true);

							outputObjectsToSelect.Add(assetRoot.gameObject);
						}
					}
				}
				else
				{
					Debug.LogWarningFormat("Failed to instantiate tool: {0}", toolName);
				}
			}

			if (outputObjectsToSelect.Count > 0)
			{
				HEU_EditorUtility.SelectObjects(outputObjectsToSelect.ToArray());
			}
		}

		public static void ExecuteToolOperatorMultiple(string toolName, string toolPath, GameObject[] inputObjects)
		{
			GameObject outputObjectToSelect = null;

			GameObject go = HEU_HAPIUtility.InstantiateHDA(toolPath, Vector3.zero, HEU_SessionManager.GetOrCreateDefaultSession(), false);
			if(go == null)
			{
				Debug.LogWarningFormat("Failed to instantiate tool: {0}", toolName);
				return;
			}

			HEU_HoudiniAssetRoot assetRoot = go.GetComponent<HEU_HoudiniAssetRoot>();
			if (assetRoot != null)
			{
				HEU_HoudiniAsset asset = assetRoot._houdiniAsset;
				HEU_SessionBase session = asset.GetAssetSession(true);

				int numInputs = inputObjects.Length;

				List<HEU_InputNode> inputNodes = asset.GetInputNodes();
				if (inputNodes == null || inputNodes.Count == 0)
				{
					Debug.LogErrorFormat("Unable to assign input geometry due to no asset inputs on selected tool.");
				}
				else
				{
					// User could have selected any number of inputs objects, and asset could have any number of inputs.
					// So use minimum of either to set input object into asset input.
					int minInputCount = Mathf.Min(inputNodes.Count, numInputs);
					for (int i = 0; i < minInputCount; ++i)
					{
						if (!IsValidInput(inputObjects[i]))
						{
							continue;
						}
						GameObject inputObject = inputObjects[i];

						HEU_InputNode inputNode = inputNodes[i];
						inputNode.ResetInputNode(session);

						inputNode.ChangeInputType(session, HEU_InputNode.InputObjectType.UNITY_MESH);

						HEU_InputObjectInfo inputInfo = inputNode.AddInputEntryAtEnd(inputObject);
						inputInfo._useTransformOffset = false;
						inputNode.KeepWorldTransform = true;
						inputNode.PackGeometryBeforeMerging = false;

						inputNode.RequiresUpload = true;
					}

					asset.RequestCook(true, true, true, true);

					outputObjectToSelect = assetRoot.gameObject;
				}
			}

			if (outputObjectToSelect != null)
			{
				HEU_EditorUtility.SelectObject(outputObjectToSelect);
			}
		}

		public static void ExecuteToolBatch(string toolName, string toolPath, GameObject[] batchObjects)
		{
			// This is same as the single path. The batch setting should be removed as its unnecessary.
			ExecuteToolOperatorSingle(toolName, toolPath, batchObjects);
		}

		public static string GetToolResourcePath(HEU_ShelfToolData tool, string inPath, string ext)
		{
			if(string.IsNullOrEmpty(inPath) || inPath.Equals("."))
			{
				// Use same path as where json file was
				//inPath = shelf._shelfPath + HEU_Platform.DirectorySeparatorStr + 

				if (!string.IsNullOrEmpty(tool._jsonPath))
				{
					inPath = tool._jsonPath.Replace(".json", "." + ext);
				}
			}

			return inPath;
		}

		public static string GetToolIconPath(HEU_ShelfToolData tool, string inPath)
		{
			if (string.IsNullOrEmpty(inPath) || inPath.Equals("."))
			{
				// Use same path as where json file was
				if (!string.IsNullOrEmpty(tool._jsonPath))
				{
					inPath = tool._jsonPath.Replace(".json", ".png");
				}
			}

			// Replace the HFS path with <HFS>
			if (!string.IsNullOrEmpty(inPath))
			{
				string hpath = HEU_Platform.GetHoudiniEnginePath();
				if (inPath.StartsWith(hpath))
				{
					inPath = inPath.Replace(hpath, HEU_Defines.HEU_PATH_KEY_HFS);
				}
			}

			return inPath;
		}

		public static string GetToolAssetPath(HEU_ShelfToolData tool, string inPath)
		{
			if (string.IsNullOrEmpty(inPath) || inPath.Equals("."))
			{
				// Use same path as where json file was
				if (!string.IsNullOrEmpty(tool._jsonPath))
				{
					string filePath = tool._jsonPath.Replace(".json", "");
					inPath = HEU_HAPIUtility.FindHoudiniAssetFileInPathWithExt(filePath);
				}
			}

			// Replace the HFS path with <HFS>
			if (!string.IsNullOrEmpty(inPath))
			{
				string hpath = HEU_Platform.GetHoudiniEnginePath();
				if(inPath.StartsWith(hpath))
				{
					inPath = inPath.Replace(hpath, HEU_Defines.HEU_PATH_KEY_HFS);
				}
			}

			return inPath;
		}
	}

}   // HoudiniEngineUnity