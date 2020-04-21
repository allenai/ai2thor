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

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace HoudiniEngineUnity
{
	/// <summary>
	/// Manages storage for Houdini Engine plugin data.
	/// </summary>
	public class HEU_PluginStorage
	{
		// Internally this is using JSON as the format, and stored in a file at project root.

		// Note: Unity's JSON support is streamlined for predefined objects (JsonUtility).
		// Unstructured data is not supported, but is a necessary part of this plugin.
		// To support unstructured data, the workaround used here is to store the data into a 
		// dictionary in memory, then write out as 2 ordered lists (keys, values) on to disk.
		// The lists are added to a temporary object then written out using JsonUtility.

		private enum DataType
		{
			BOOL,
			INT,
			LONG,
			FLOAT,
			STRING
		}

		// Dictionary for unstructured data.
		private Dictionary<string, StoreData> _dataMap = new Dictionary<string, StoreData>();
		// Class for unstructured data.
		[System.Serializable]
		private class StoreData
		{
			public DataType _type;
			public string _valueStr;
		}

#pragma warning disable 0649
		// Temporary class to enable us to write out arrays using JsonUtility.
		[System.Serializable]
		private class StoreDataArray<T>
		{
			public T[] _array;
		}
#pragma warning restore 0649

		private Dictionary<string, string> _envPathMap;

		public Dictionary<string, string> GetEnvironmentPathMap() { return _envPathMap; }

		// Whether plugin setting need to be saved out to file.
		private bool _requiresSave;
		public bool RequiresSave { get { return _requiresSave; } }

		private static HEU_PluginStorage _instance;

		public static HEU_PluginStorage Instance
		{
			get
			{
				if(_instance == null)
				{
					InstantiateAndLoad();
				}
				return _instance;
			}
		}

		/// <summary>
		/// Create new instance if none found.
		/// Loads plugin data from file.
		/// </summary>
		public static void InstantiateAndLoad()
		{
			if (_instance == null)
			{
				_instance = new HEU_PluginStorage();
				_instance.LoadPluginData();

				// Set the current culture based on user plugin setting. By default (fresh install)
				// this sets to InvariantCulture to fix decimal parsing issues on certain locales (like es-ES).
				// Note that this sets for the entire project as long as the plugin is in use.
				// Users can turn this off from Plugin Settings.
				SetCurrentCulture(HEU_PluginSettings.SetCurrentThreadToInvariantCulture);

				HEU_SessionManager.LoadAllSessionData();

				_instance.LoadAssetEnvironmentPaths();
			}
		}

		/// <summary>
		/// Sets the System.Threading.Thread.CurrentThread.CurrentCulture to System.Globalization.CultureInfo.InvariantCulture
		/// if useInvariant is true. This fixes issues with locale-specific parsing issues such as commas for dots in decimals.
		/// </summary>
		/// <param name="useInvariant">True to use InvariatCulture. False will reset to DefaultThreadCurrentCulture.</param>
		public static void SetCurrentCulture(bool useInvariant)
		{
#if NET_4_6
			if(useInvariant)
			{
				// DefaultThreadCurrentCulture could be null in which case save the CurrentCulture into it
				if (System.Globalization.CultureInfo.DefaultThreadCurrentCulture == null)
				{
					System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
				}
				
				System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			}
			else if(System.Globalization.CultureInfo.DefaultThreadCurrentCulture != null)
			{
				// Only reset to default if DefaultThreadCurrentCulture is not null otherwise we get error
				System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.DefaultThreadCurrentCulture;
			}
#endif
		}

		/// <summary>
		/// Retrieve the array from given JSON string.
		/// </summary>
		/// <typeparam name="T">Type of array</typeparam>
		/// <param name="jsonArray">String containing JSON array.</param>
		/// <returns>Array of objects of type T.</returns>
		private T[] GetJSONArray<T>(string jsonArray)
		{
			// Parse out array string into array class, then just grab the array.
			StoreDataArray<T> dataArray = JsonUtility.FromJson<StoreDataArray<T>>(jsonArray);
			return dataArray._array;
		}

		public void Set(string key, bool value)
		{
			StoreData data = new StoreData();
			data._type = DataType.BOOL;
			data._valueStr = System.Convert.ToString(value);

			_dataMap[key] = data;
			MarkDirtyForSave();
		}

		public void Set(string key, int value)
		{
			StoreData data = new StoreData();
			data._type = DataType.INT;
			data._valueStr = System.Convert.ToString(value);

			_dataMap[key] = data;
			MarkDirtyForSave();
		}

		public void Set(string key, long value)
		{
			StoreData data = new StoreData();
			data._type = DataType.LONG;
			data._valueStr = System.Convert.ToString(value);

			_dataMap[key] = data;
			MarkDirtyForSave();
		}

		public void Set(string key, float value)
		{
			StoreData data = new StoreData();
			data._type = DataType.FLOAT;
			data._valueStr = System.Convert.ToString(value);

			_dataMap[key] = data;
			MarkDirtyForSave();
		}
		
		public void Set(string key, string value)
		{
			StoreData data = new StoreData();
			data._type = DataType.STRING;
			data._valueStr = value;

			_dataMap[key] = data;
			MarkDirtyForSave();
		}

		public void Set(string key, List<string> values, char delimiter = ';')
		{
			StringBuilder sb = new StringBuilder();
			int numStrings = values.Count;
			for(int i = 0; i < numStrings; ++i)
			{
				sb.AppendFormat("{0}{1}", values[i], delimiter);
			}

			Set(key, sb.ToString());
		}

		public bool Get(string key, out bool value, bool defaultValue)
		{
			if(_dataMap.ContainsKey(key))
			{
				StoreData data = _dataMap[key];
				if(data._type == DataType.BOOL)
				{
					value = System.Convert.ToBoolean(data._valueStr);
					return true;
				}
			}
			value = defaultValue;
			return false;
		}

		public bool Get(string key, out int value, int defaultValue)
		{
			if (_dataMap.ContainsKey(key))
			{
				StoreData data = _dataMap[key];
				if (data._type == DataType.INT)
				{
					value = System.Convert.ToInt32(data._valueStr);
					return true;
				}
			}
			value = defaultValue;
			return false;
		}

		public bool Get(string key, out long value, long defaultValue)
		{
			if (_dataMap.ContainsKey(key))
			{
				StoreData data = _dataMap[key];
				if (data._type == DataType.LONG)
				{
					value = System.Convert.ToInt64(data._valueStr);
					return true;
				}
			}
			value = defaultValue;
			return false;
		}

		public bool Get(string key, out float value, float defaultValue)
		{
			if (_dataMap.ContainsKey(key))
			{
				StoreData data = _dataMap[key];
				if (data._type == DataType.FLOAT)
				{
					value = System.Convert.ToSingle(data._valueStr, System.Globalization.CultureInfo.InvariantCulture);
					return true;
				}
			}
			value = defaultValue;
			return false;
		}

		public bool Get(string key, out string value, string defaultValue)
		{
			if (_dataMap.ContainsKey(key))
			{
				StoreData data = _dataMap[key];
				if (data._type == DataType.STRING)
				{
					value = data._valueStr;
					return true;
				}
			}
			value = defaultValue;
			return false;
		}

		public bool Get(string key, out List<string> values, char delimiter = ';')
		{
			values = new List<string>();

			string combinedStr = "";
			bool bResult = Get(key, out combinedStr, combinedStr);
			if (bResult && !string.IsNullOrEmpty(combinedStr))
			{
				string[] split = combinedStr.Split(delimiter);
				if (split != null)
				{
					int numStrings = split.Length;
					for (int i = 0; i < numStrings; ++i)
					{
						if (!string.IsNullOrEmpty(split[i]))
						{
							values.Add(split[i]);
						}
					}
				}
				else
				{
					bResult = false;
				}
			}

			return bResult;
		}

		/// <summary>
		/// Set flag so that the plugin data will be saved out
		/// at end of update.
		/// </summary>
		private void MarkDirtyForSave()
		{
			if (!_requiresSave)
			{
#if UNITY_EDITOR
				_requiresSave = true;
				UnityEditor.EditorApplication.delayCall += SaveIfRequired;
#endif
			}
		}

		/// <summary>
		/// Saves plugin data if there are outstanding changes.
		/// </summary>
		public static void SaveIfRequired()
		{
			if(_instance != null && _instance.RequiresSave)
			{
				_instance.SavePluginData();
			}
		}

		public const string PluginSettingsLine1 = "Houdini Engine for Unity Plugin Settings";
		public const string PluginSettingsLine2 = "Version=";
		public const string PluginSettingsVersion = "1.0";

		// Path to the plugin settings ini file. Placed in the project root (i.e. Assets/../)
		public static string SettingsFilePath()
		{
			return Path.Combine(Application.dataPath, ".." + Path.DirectorySeparatorChar + HEU_Defines.PLUGIN_SETTINGS_FILE);
		}

		/// <summary>
		/// Save plugin data to disk.
		/// </summary>
		public bool SavePluginData()
		{
			try
			{
				string settingsFilePath = SettingsFilePath();
				using (StreamWriter writer = new StreamWriter(settingsFilePath, false))
				{
					writer.WriteLine("Houdini Engine for Unity Plugin Settings");
					writer.WriteLine("Version=" + PluginSettingsVersion);

					foreach (KeyValuePair<string, StoreData> kvpair in _dataMap)
					{
						// key(type)=value
						writer.WriteLine("{0}({1})={2}", kvpair.Key, kvpair.Value._type, kvpair.Value._valueStr);
					}
				}

#if UNITY_EDITOR
				// Remove old keys from EditorPrefs as its the deprecated method of saving the plugin settings
				if (EditorPrefs.HasKey(HEU_Defines.PLUGIN_STORE_KEYS))
				{
					EditorPrefs.DeleteKey(HEU_Defines.PLUGIN_STORE_KEYS);
				}
				if (EditorPrefs.HasKey(HEU_Defines.PLUGIN_STORE_DATA))
				{
					EditorPrefs.DeleteKey(HEU_Defines.PLUGIN_STORE_DATA);
				}
#endif

				_requiresSave = false;
			}
			catch (System.Exception ex)
			{
				Debug.LogErrorFormat("Exception when trying to save settings file: {0}", ex.ToString());
				return false;
			}

			return true;
		}

		/// <summary>
		/// Load the saved plugin settings from disk.
		/// </summary>
		/// <returns>True if successfully loaded.</returns>
		public bool LoadPluginData()
		{
			// First check if settings pref file exists
			string settingsFilePath = SettingsFilePath();
			if (!HEU_Platform.DoesFileExist(settingsFilePath))
			{
				// Try reading from EditorPrefs to see if this is still using the old method
				return ReadFromEditorPrefs();
			}

			// Open file and read each line to create the settings entry
			using (StreamReader reader = new StreamReader(settingsFilePath))
			{
				// Must match first line
				string line = reader.ReadLine();
				if (string.IsNullOrEmpty(line) || !line.Equals(PluginSettingsLine1))
				{
					Debug.LogWarningFormat("Unable to load Plugin settings file. {0} should have line 1: {1}", settingsFilePath, PluginSettingsLine1);
					return false;
				}

				// Must match 2nd line
				line = reader.ReadLine();
				if (string.IsNullOrEmpty(line) || !line.StartsWith(PluginSettingsLine2))
				{
					Debug.LogWarningFormat("Unable to load Plugin settings file. {0} should start line 2 with: {1}", settingsFilePath, PluginSettingsLine2);
					return false;
				}

				Dictionary<string, StoreData> storeMap = new Dictionary<string, StoreData>();
				string keyStr;
				string typeStr;
				string valueStr;
				DataType dataType;
				// "key(type)=value"
				System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"^(\w+)\((\w+)\)=(.*)");
				while ((line = reader.ReadLine()) != null)
				{
					System.Text.RegularExpressions.Match match = regex.Match(line);
					if (match.Success && match.Groups.Count >= 4)
					{
						keyStr = match.Groups[1].Value;
						typeStr = match.Groups[2].Value;
						valueStr = match.Groups[3].Value;
						
						if (!string.IsNullOrEmpty(keyStr) && !string.IsNullOrEmpty(typeStr) 
							&& !string.IsNullOrEmpty(valueStr))
						{
							try
							{
								dataType = (DataType)System.Enum.Parse(typeof(DataType), typeStr);

								StoreData store = new StoreData();
								store._type = dataType;
								store._valueStr = valueStr;
								storeMap.Add(keyStr, store);
							}
							catch( System.Exception ex)
							{
								Debug.LogErrorFormat("Invalid data type found in settings: {0}. Exception: {1}", typeStr, ex.ToString());
							}
						}
					}
				}

				_dataMap = storeMap;
			}

			return true;
		}

		private bool ReadFromEditorPrefs()
		{
#if UNITY_EDITOR
			if (UnityEditor.EditorPrefs.HasKey(HEU_Defines.PLUGIN_STORE_KEYS) && UnityEditor.EditorPrefs.HasKey(HEU_Defines.PLUGIN_STORE_DATA))
			{
				// Grab JSON strings from EditorPrefs, then use temporary array class to grab the JSON array.
				// Finally add into dictionary.

				string keyJson = UnityEditor.EditorPrefs.GetString(HEU_Defines.PLUGIN_STORE_KEYS);
				string dataJson = UnityEditor.EditorPrefs.GetString(HEU_Defines.PLUGIN_STORE_DATA);

				//Debug.Log("Load:: Keys: " + keyJson);
				//Debug.Log("Load:: Data: " + dataJson);

				string[] keyList = GetJSONArray<string>(keyJson);
				StoreData[] dataList = GetJSONArray<StoreData>(dataJson);

				_dataMap = new Dictionary<string, StoreData>();
				int numKeys = keyList.Length;
				int numData = dataList.Length;
				if (numKeys == numData)
				{
					for (int i = 0; i < numKeys; ++i)
					{
						_dataMap.Add(keyList[i], dataList[i]);
						//Debug.Log(string.Format("{0} : {1}", keyList[i], dataList[i]._valueStr));
					}
				}

				// Remove from EditorPrefs. This is the deprecated method of saving the plugin settings.
				if (EditorPrefs.HasKey(HEU_Defines.PLUGIN_STORE_KEYS))
				{
					EditorPrefs.DeleteKey(HEU_Defines.PLUGIN_STORE_KEYS);
				}
				if (EditorPrefs.HasKey(HEU_Defines.PLUGIN_STORE_DATA))
				{
					EditorPrefs.DeleteKey(HEU_Defines.PLUGIN_STORE_DATA);
				}

				return true;
			}
#endif
			return false;
		}

		/// <summary>
		/// Removes all plugin data from persistent storage.
		/// </summary>
		public static void ClearPluginData()
		{
			if (_instance != null)
			{
				_instance._dataMap = new Dictionary<string, StoreData>();
				_instance.SavePluginData();
			}
		}

		/// <summary>
		/// Load setings data from saved file.
		/// </summary>
		public static void LoadFromSavedFile()
		{
			if (_instance != null)
			{
				_instance.LoadPluginData();
			}
		}


		// Session file path. Stored at project root (i.e. Assets/../)
		public static string SessionFilePath()
		{
			return Path.Combine(Application.dataPath, ".." + Path.DirectorySeparatorChar + HEU_Defines.PLUGIN_SESSION_FILE);
		}

		/// <summary>
		/// Save given list of sessions (HEU_SessionData) into storage for retrieval later.
		/// A way to persist current session information through code refresh/compiles.
		/// </summary>
		/// <param name="allSessions"></param>
		public static void SaveAllSessionData(List<HEU_SessionBase> allSessions)
		{
#if UNITY_EDITOR && HOUDINIENGINEUNITY_ENABLED
			// Formulate the JSON string for existing sessions.
			StringBuilder sb = new StringBuilder();
			foreach(HEU_SessionBase session in allSessions)
			{
				if (session.GetSessionData() != null)
				{
					sb.AppendFormat("{0};", JsonUtility.ToJson(session.GetSessionData()));
				}
			}
			HEU_Platform.WriteAllText(SessionFilePath(), sb.ToString());
#endif
		}

		/// <summary>
		/// Returns list of session data retrieved from storage.
		/// </summary>
		/// <returns>List of HEU_SessionData stored on disk.</returns>
		public static List<HEU_SessionData> LoadAllSessionData()
		{
			// Retrieve saved JSON string from storage, and parse it to create the session datas.
			List<HEU_SessionData> sessions = new List<HEU_SessionData>();
#if UNITY_EDITOR && HOUDINIENGINEUNITY_ENABLED
			string jsonStr = HEU_Platform.ReadAllText(SessionFilePath());
			if (!string.IsNullOrEmpty(jsonStr))
			{
				string[] jsonSplit = jsonStr.Split(';');
				foreach(string entry in jsonSplit)
				{
					if (!string.IsNullOrEmpty(entry))
					{
						HEU_SessionData sessionData = JsonUtility.FromJson<HEU_SessionData>(entry);
						if(sessionData != null)
						{
							sessions.Add(sessionData);
						}
					}
				}
			}
#endif
			return sessions;
		}

		/// <summary>
		/// Build up the asset environment paths set in unity_houdini.env.
		/// The paths must have key prefixes start with HEU_Defines.HEU_ENVPATH_PREFIX.
		/// When assets are loaded, these mappings are used to find real paths.
		/// </summary>
		public void LoadAssetEnvironmentPaths()
		{
			string envPath = HEU_Platform.GetHoudiniEngineEnvironmentFilePathFull();
			if (!string.IsNullOrEmpty(envPath) && HEU_Platform.DoesFileExist(envPath))
			{
				_envPathMap = new Dictionary<string, string>();
				char[] delimiter = new char[] { '=' };
				char[] trimEnd = new char[] { '\\', '/' };

				using (var file = new StreamReader(envPath))
				{
					string line;
					while((line = file.ReadLine()) != null)
					{
						if (line.StartsWith(HEU_Defines.HEU_ENVPATH_PREFIX))
						{
							string[] split = line.Split(delimiter, 2, System.StringSplitOptions.RemoveEmptyEntries);
							if(split != null && split.Length == 2)
							{
								string value = split[1].Replace("\\", "/").TrimEnd(trimEnd);
								_envPathMap.Add(split[0], value);
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Convert the given real path to its environment mapped version, if there is a mapping for it.
		/// For example, if inPath is C:\temp\hdas\trees\tree1.hda and environment map is
		/// HEU_ENVPATH_HDAS=C:\temp\hdas\ then the result will be $HEU_ENVPATH_HDAS/trees\tree1.hda
		/// </summary>
		/// <param name="inPath">The real path to convert</param>
		/// <returns>The mapped path if a valid environment mapping is found for it, otherwise inPath is returned.</returns>
		public string ConvertRealPathToEnvKeyedPath(string inPath)
		{
			if (string.IsNullOrEmpty(inPath) || inPath.StartsWith(HEU_Defines.HEU_ENVPATH_KEY))
			{
				return inPath;
			}

			if (_envPathMap == null)
			{
				LoadAssetEnvironmentPaths();
			}

			if(_envPathMap != null)
			{
				foreach (KeyValuePair<string, string> pair in _envPathMap)
				{
					if (inPath.StartsWith(pair.Value))
					{
						inPath = inPath.Replace(pair.Value, HEU_Defines.HEU_ENVPATH_KEY + pair.Key);
						break;
					}
				}
			}

			return inPath;
		}

		/// <summary>
		/// Convert environment mapped path to real path, if there is a mapping for it.
		/// For example, if inPath is $HEU_ENVPATH_HDAS/trees/tree1.hda and mapping is
		/// HEU_ENVPATH_HDAS=C:\temp\hdas\ then the result will be C:\temp\hdas\trees/tree1.
		/// </summary>
		/// <param name="inPath"></param>
		/// <returns></returns>
		public string ConvertEnvKeyedPathToReal(string inPath)
		{
			if(string.IsNullOrEmpty(inPath))
			{
				return inPath;
			}

			if(inPath.StartsWith(HEU_Defines.HEU_PATH_KEY_HFS, System.StringComparison.InvariantCulture))
			{
				return HEU_HAPIUtility.GetRealPathFromHFSPath(inPath);
			}

			if (_envPathMap == null)
			{
				LoadAssetEnvironmentPaths();
			}

			if (_envPathMap != null && inPath.StartsWith(HEU_Defines.HEU_ENVPATH_KEY))
			{
				foreach (KeyValuePair<string, string> pair in _envPathMap)
				{
					string key = HEU_Defines.HEU_ENVPATH_KEY + pair.Key;
					if (inPath.StartsWith(key, System.StringComparison.InvariantCulture))
					{
						inPath = inPath.Replace(key, pair.Value);
						break;
					}
				}
			}

			return inPath;
		}
	}

}   // HoudiniEngineUnity