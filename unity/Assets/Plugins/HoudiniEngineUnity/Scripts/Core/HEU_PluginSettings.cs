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
	/// Container for plugin global settings.
	/// </summary>
	public static class HEU_PluginSettings
	{
		public static string HoudiniEngineEnvFilePath
		{
			get
			{
				string path = "Assets/unity_houdini.env";
				HEU_PluginStorage.Instance.Get("HEU_EnvFilePathRel", out path, path);
				return path;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HEU_EnvFilePathRel", value);

				// Reload the environment
				HEU_PluginStorage.Instance.LoadAssetEnvironmentPaths();
			}
		}

		public static bool CookingEnabled
		{
			get
			{
				bool bValue = true;
				HEU_PluginStorage.Instance.Get("HAPI_EnableCooking", out bValue, bValue);
				return bValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_EnableCooking", value);
			}
		}

		public static bool CookingTriggersDownstreamCooks
		{
			get
			{
				bool bValue = false;
				HEU_PluginStorage.Instance.Get("HAPI_CookingTriggersDownCooks", out bValue, bValue);
				return bValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_CookingTriggersDownCooks", value);
			}
		}

		public static bool CookTemplatedGeos
		{
			get
			{
				bool bValue = true;
				HEU_PluginStorage.Instance.Get("HAPI_CookTemplatedGeos", out bValue, bValue);
				return bValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_CookTemplatedGeos", value);
			}
		}

		public static bool PushUnityTransformToHoudini
		{
			get
			{
				bool bValue = true;
				HEU_PluginStorage.Instance.Get("HAPI_PushUnityTransformToHoudini", out bValue, bValue);
				return bValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_PushUnityTransformToHoudini", value);
			}
		}

		public static bool TransformChangeTriggersCooks
		{
			get
			{
				bool bValue = true;
				HEU_PluginStorage.Instance.Get("HAPI_TransformChangeTriggersCooks", out bValue, bValue);
				return bValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_TransformChangeTriggersCooks", value);
			}
		}

		public static string CollisionGroupName
		{
			get
			{
				string sValue = HEU_Defines.DEFAULT_COLLISION_GEO;
				HEU_PluginStorage.Instance.Get("HAPI_CollisionGroupName", out sValue, sValue);
				return sValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_CollisionGroupName", value);
			}
		}

		public static string RenderedCollisionGroupName
		{
			get
			{
				string sValue = HEU_Defines.DEFAULT_RENDERED_COLLISION_GEO;
				HEU_PluginStorage.Instance.Get("HAPI_RenderedCollisionGroupName", out sValue, sValue);
				return sValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_RenderedCollisionGroupName", value);
			}
		}

		public static string UnityMaterialAttribName
		{
			get
			{
				string sValue = HEU_Defines.DEFAULT_UNITY_MATERIAL_ATTR;
				HEU_PluginStorage.Instance.Get("HAPI_UnityMaterialAttribName", out sValue, sValue);
				return sValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_UnityMaterialAttribName", value);
			}
		}

		public static string UnitySubMaterialAttribName
		{
			get
			{
				string sValue = HEU_Defines.DEFAULT_UNITY_SUBMATERIAL_NAME_ATTR;
				HEU_PluginStorage.Instance.Get("HAPI_UnitySubMaterialNameAttribName", out sValue, sValue);
				return sValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_UnitySubMaterialNameAttribName", value);
			}
		}
		public static string UnitySubMaterialIndexAttribName
		{
			get
			{
				string sValue = HEU_Defines.DEFAULT_UNITY_SUBMATERIAL_INDEX_ATTR;
				HEU_PluginStorage.Instance.Get("HAPI_UnitySubMaterialIndexAttribName", out sValue, sValue);
				return sValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_UnitySubMaterialIndexAttribName", value);
			}
		}


		public static string UnityTagAttributeName
		{
			get
			{
				string sValue = HEU_Defines.DEFAULT_UNITY_TAG_ATTR;
				HEU_PluginStorage.Instance.Get("HAPI_UnityTagAttribName", out sValue, sValue);
				return sValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_UnityTagAttribName", value);
			}
		}

		public static string UnityStaticAttributeName
		{
			get
			{
				string sValue = HEU_Defines.DEFAULT_UNITY_STATIC_ATTR;
				HEU_PluginStorage.Instance.Get("HAPI_UnityStaticAttribName", out sValue, sValue);
				return sValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_UnityStaticAttribName", value);
			}
		}

		public static string UnityScriptAttributeName
		{
			get
			{
				string sValue = HEU_Defines.DEFAULT_UNITY_SCRIPT_ATTR;
				HEU_PluginStorage.Instance.Get("HAPI_UnityScriptAttribName", out sValue, sValue);
				return sValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_UnityScriptAttribName", value);
			}
		}

		public static string UnityLayerAttributeName
		{
			get
			{
				string sValue = HEU_Defines.DEFAULT_UNITY_LAYER_ATTR;
				HEU_PluginStorage.Instance.Get("HAPI_UnityLayerAttribName", out sValue, sValue);
				return sValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_UnityLayerAttribName", value);
			}
		}

		public static float ImageGamma
		{
			get
			{
				float gamma = 2.2f;
				HEU_PluginStorage.Instance.Get("HAPI_Gamma", out gamma, gamma);
				return gamma;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_Gamma", value);
			}
		}

		public static float NormalGenerationThresholdAngle
		{
			get
			{
				float angle = 80f;
				HEU_PluginStorage.Instance.Get("HAPI_NormalGenerationThresholdAngle", out angle, angle);
				return angle;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_NormalGenerationThresholdAngle", value);
			}
		}

		public static string LastLoadHDAPath
		{
			get
			{
				string lastPath = "";
				HEU_PluginStorage.Instance.Get("HAPI_LastLoadHDAPath", out lastPath, lastPath);
				return lastPath;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_LastLoadHDAPath", value);
			}
		}

		public static string LastLoadHIPPath
		{
			get
			{
				string lastPath = "";
				HEU_PluginStorage.Instance.Get("HAPI_LastLoadHIPPath", out lastPath, lastPath);
				return lastPath;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_LastLoadHIPPath", value);
			}
		}

		public static string InstanceAttr
		{
			get
			{
				string attrValue = HEU_Defines.HAPI_ATTRIB_INSTANCE;
				HEU_PluginStorage.Instance.Get("HAPI_InstanceAttr", out attrValue, attrValue);
				return attrValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_InstanceAttr", value);
			}
		}

		public static string UnityInstanceAttr
		{
			get
			{
				string attrValue = HEU_Defines.DEFAULT_UNITY_INSTANCE_ATTR;
				HEU_PluginStorage.Instance.Get("HAPI_UnityInstanceAttr", out attrValue, attrValue);
				return attrValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_UnityInstanceAttr", value);
			}
		}

		public static string UnityInputMeshAttr
		{
			get
			{
				string attrValue = HEU_Defines.DEFAULT_UNITY_INPUT_MESH_ATTR;
				HEU_PluginStorage.Instance.Get("HAPI_UnityInputMeshAttr", out attrValue, attrValue);
				return attrValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_UnityInputMeshAttr", value);
			}
		}

		public static Color LineColor
		{
			get
			{
				Color storedColor = new Color(0f, 1f, 0f, 1f);
				string storedColorStr = HEU_GeneralUtility.ColorToString(storedColor);
				HEU_PluginStorage.Instance.Get("HAPI_LineColor", out storedColorStr, storedColorStr);
				return HEU_GeneralUtility.StringToColor(storedColorStr);
			}
			set
			{
				string storeColorStr = HEU_GeneralUtility.ColorToString(value);
				HEU_PluginStorage.Instance.Set("HAPI_LineColor", storeColorStr);
			}
		}

		public static string EditorOnly_Tag
		{
			get
			{
				string attrValue = HEU_Defines.UNITY_EDITORONLY_TAG;
				HEU_PluginStorage.Instance.Get("HAPI_EditorOnlyTag", out attrValue, attrValue);
				return attrValue;
			}
			set
			{
				if(HEU_GeneralUtility.DoesUnityTagExist(value))
				{
					HEU_PluginStorage.Instance.Set("HAPI_EditorOnlyTag", value);
				}
				else
				{
					string msg = string.Format("Tag '{0}' does not exist in the Editor. Add it before setting it as the tag.", value);
					HEU_EditorUtility.DisplayErrorDialog("Tag Does Not Exist", msg, "OK");
				}
			}
		}

		public static string HDAData_Name
		{
			get
			{
				string attrValue = HEU_Defines.UNITY_HDADATA_NAME;
				HEU_PluginStorage.Instance.Get("HAPI_HDADataName", out attrValue, attrValue);
				return attrValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_HDADataName", value);
			}
		}

		public static string Session_PipeName
		{
			get
			{
				string attrValue = HEU_Defines.HEU_SESSION_PIPENAME;
				HEU_PluginStorage.Instance.Get("HAPI_SessionPipeName", out attrValue, attrValue);
				return attrValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_SessionPipeName", value);
			}
		}

		public static string Session_Localhost
		{
			get
			{
				string attrValue = HEU_Defines.HEU_SESSION_LOCALHOST;
				HEU_PluginStorage.Instance.Get("HAPI_SessionLocalhost", out attrValue, attrValue);
				return attrValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_SessionLocalhost", value);
			}
		}

		public static int Session_Port
		{
			get
			{
				int attrValue = HEU_Defines.HEU_SESSION_PORT;
				HEU_PluginStorage.Instance.Get("HAPI_SessionPort", out attrValue, attrValue);
				return attrValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_SessionPort", value);
			}
		}

		public static float Session_Timeout
		{
			get
			{
				float attrValue = HEU_Defines.HEU_SESSION_TIMEOUT;
				HEU_PluginStorage.Instance.Get("HAPI_SessionTimeout", out attrValue, attrValue);
				return attrValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_SessionTimeout", value);
			}
		}

		public static bool Session_AutoClose
		{
			get
			{
				bool attrValue = HEU_Defines.HEU_SESSION_AUTOCLOSE;
				HEU_PluginStorage.Instance.Get("HAPI_SessionAutoclose", out attrValue, attrValue);
				return attrValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_SessionAutoclose", value);
			}
		}

		public static bool Curves_ShowInSceneView
		{
			get
			{
				bool attrValue = true;
				HEU_PluginStorage.Instance.Get("HAPI_CurvesShowInSceneView", out attrValue, attrValue);
				return attrValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_CurvesShowInSceneView", value);
			}
		}

		public static string AssetCachePath
		{
			get
			{
				string path = HEU_Defines.HEU_ASSET_CACHE_PATH;
				HEU_PluginStorage.Instance.Get("HAPI_AssetCachePath", out path, path);
				return path;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_AssetCachePath", value);
			}
		}

		public static bool UseFullPathNamesForOutput
		{
			get
			{
				bool bValue = true;
				HEU_PluginStorage.Instance.Get("HAPI_UseFullPathNamesForOutput", out bValue, bValue);
				return bValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_UseFullPathNamesForOutput", value);
			}
		}

		public static List<string> HEngineToolsShelves
		{
			get
			{
				List<string> paths = null;
				HEU_PluginStorage.Instance.Get("HAPI_HEngineToolsShelves", out paths);
				return paths;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_HEngineToolsShelves", value);
			}
		}

		public static int HEngineShelfSelectedIndex
		{
			get
			{
				int selectedIndex = 0;
				HEU_PluginStorage.Instance.Get("HAPI_HEngineShelfSelectedIndex", out selectedIndex, selectedIndex);
				return selectedIndex;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_HEngineShelfSelectedIndex", value);
			}
		}

		public static string DefaultTerrainMaterial
		{
			get
			{
				string path = "";
				HEU_PluginStorage.Instance.Get("HAPI_DefaultTerrainMaterial", out path, path);
				return path;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_DefaultTerrainMaterial", value);
			}
		}

		public static string TerrainSplatTextureDefault
		{
			get
			{
				string path = HEU_Defines.HEU_TERRAIN_SPLAT_DEFAULT;
				HEU_PluginStorage.Instance.Get("HAPI_TerrainSplatTextureDefault", out path, path);
				return path;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_TerrainSplatTextureDefault", value);
			}
		}

		public static string DefaultStandardShader
		{
			get
			{
				string path = HEU_Defines.DEFAULT_STANDARD_SHADER;
				HEU_PluginStorage.Instance.Get("HAPI_DefaultStandardShader", out path, path);

				// To keep backwards compatiblity, add in "Houdini/" prefix if not found for shipped shaders
				if (path.Equals(HEU_Defines.DEFAULT_STANDARD_SHADER))
				{
					path = HEU_Defines.HOUDINI_SHADER_PREFIX + path;
				}

				return path;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_DefaultStandardShader", value);
			}
		}

		public static string DefaultVertexColorShader
		{
			get
			{
				string path = HEU_Defines.DEFAULT_VERTEXCOLOR_SHADER;
				HEU_PluginStorage.Instance.Get("HAPI_DefaultVertexColorShader", out path, path);

				// To keep backwards compatiblity, add in "Houdini/" prefix if not found for shipped shaders
				if (path.Equals(HEU_Defines.DEFAULT_VERTEXCOLOR_SHADER))
				{
					path = HEU_Defines.HOUDINI_SHADER_PREFIX + path;
				}

				return path;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_DefaultVertexColorShader", value);
			}
		}

		public static string DefaultTransparentShader
		{
			get
			{
				string path = HEU_Defines.DEFAULT_TRANSPARENT_SHADER;
				HEU_PluginStorage.Instance.Get("HAPI_DefaultTransparentShader", out path, path);

				// To keep backwards compatiblity, add in "Houdini/" prefix if not found for shipped shaders
				if (path.Equals(HEU_Defines.DEFAULT_TRANSPARENT_SHADER))
				{
					path = HEU_Defines.HOUDINI_SHADER_PREFIX + path;
				}

				return path;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_DefaultTransparentShader", value);
			}
		}

		public static string DefaultCurveShader
		{
			get
			{
				string path = HEU_Defines.DEFAULT_CURVE_SHADER;
				HEU_PluginStorage.Instance.Get("HAPI_DefaultCurveShader", out path, path);

				// To keep backwards compatiblity, add in "Houdini/" prefix if not found for shipped shaders
				if (path.Equals(HEU_Defines.DEFAULT_CURVE_SHADER))
				{
					path = HEU_Defines.HOUDINI_SHADER_PREFIX + path;
				}

				return path;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_DefaultCurveShader", value);
			}
		}

		public static bool SupportHoudiniBoxType
		{
			get
			{
				bool bValue = false;
				HEU_PluginStorage.Instance.Get("HAPI_SupportBoxType", out bValue, bValue);
				return bValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_SupportBoxType", value);
			}
		}

		public static bool SupportHoudiniSphereType
		{
			get
			{
				bool bValue = false;
				HEU_PluginStorage.Instance.Get("HAPI_SupportSphereType", out bValue, bValue);
				return bValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_SupportSphereType", value);
			}
		}

		public static bool SetCurrentThreadToInvariantCulture
		{
			get
			{
				bool bValue = true;
				HEU_PluginStorage.Instance.Get("HAPI_SetCurrentThreadToInvariantCulture", out bValue, bValue);
				return bValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_SetCurrentThreadToInvariantCulture", value);
				HEU_PluginStorage.SetCurrentCulture(value);
			}
		}

		public static string HoudiniDebugLaunchPath
		{
			get
			{
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
				string path = HEU_Platform.GetHoudiniEnginePath();
#else
				string path = HEU_Platform.GetHoudiniEnginePath() + HEU_HoudiniVersion.HAPI_BIN_PATH + HEU_Platform.DirectorySeparator + "houdini";
#endif
				HEU_PluginStorage.Instance.Get("HEU_HoudiniDebugLaunchPath", out path, path);
				return path;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HEU_HoudiniDebugLaunchPath", value);
			}
		}

		public static string LastExportPath
		{
			get
			{
				string path = "";
				HEU_PluginStorage.Instance.Get("HAPI_LastExportPath", out path, path);
				return path;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_LastExportPath", value);
			}
		}

		public static int InputSelectionFilterLocation
		{
			get
			{
				int selection = 1;
				HEU_PluginStorage.Instance.Get("HAPI_InputFilterLocation", out selection, selection);
				return selection;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_InputFilterLocation", value);
			}
		}

		public static int InputSelectionFilterState
		{
			get
			{
				int selection = 1;
				HEU_PluginStorage.Instance.Get("HAPI_InputFilterState", out selection, selection);
				return selection;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_InputFilterState", value);
			}
		}

		public static bool InputSelectionFilterRoots
		{
			get
			{
				bool selection = false;
				HEU_PluginStorage.Instance.Get("HAPI_InputFilterRoots", out selection, selection);
				return selection;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_InputFilterRoots", value);
			}
		}

		public static string InputSelectionFilterName
		{
			get
			{
				string selection = "";
				HEU_PluginStorage.Instance.Get("HAPI_InputFilterName", out selection, selection);
				return selection;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_InputFilterName", value);
			}
		}

		public static bool CookOptionSplitGeosByGroup
		{
			get
			{
				bool selection = false;
				HEU_PluginStorage.Instance.Get("HAPI_CookOptionSplitGeosByGroup", out selection, selection);
				return selection;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_CookOptionSplitGeosByGroup", value);
			}
		}

		public static int MaxVerticesPerPrimitive
		{
			get
			{
				int maxValue = HEU_Defines.HAPI_MAX_VERTICES_PER_FACE;
				HEU_PluginStorage.Instance.Get("HAPI_MaxVerticesPerPrimitive", out maxValue, maxValue);
				return maxValue;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_MaxVerticesPerPrimitive", value);
			}
		}

		public static string HoudiniInstallPath
		{
			get
			{
				string path = "";
				HEU_PluginStorage.Instance.Get("HAPI_HoudiniInstallPath", out path, path);
				return path;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_HoudiniInstallPath", value);
			}
		}

		public static string LastHoudiniVersion
		{
			get
			{
				string version = "";
				HEU_PluginStorage.Instance.Get("HAPI_LastHoudiniVersion", out version, version);
				return version;
			}
			set
			{
				HEU_PluginStorage.Instance.Set("HAPI_LastHoudiniVersion", value);
			}
		}
	}

}   // HoudiniEngineUnity