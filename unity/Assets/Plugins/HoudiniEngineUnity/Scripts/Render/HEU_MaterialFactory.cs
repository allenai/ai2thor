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
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Typedefs (copy these from HEU_Common.cs)
	using HAPI_NodeId = System.Int32;
	using HAPI_PartId = System.Int32;
	using HAPI_ParmId = System.Int32;
	using HAPI_StringHandle = System.Int32;


	/// <summary>
	/// Manages materials used by Houdini Engine assets.
	/// </summary>
	public static class HEU_MaterialFactory
	{
		
		public static Shader FindShader(string shaderName)
		{
#if UNITY_EDITOR
			return Shader.Find(shaderName);
#else
			// TODO RUNTIME: Shader.Find is not available in non-Editor mode, so need to figure out a replacement in runtime.
			Debug.LogWarning("Houdini Engine is unable to load shaders in non-Editor mode!");
			return null;
#endif
		}

		public static Shader FindPluginShader(string shaderName)
		{
#if UNITY_EDITOR
			return FindShader(shaderName);
#else
			// TODO RUNTIME: Shader.Find is not available in non-Editor mode, so need to figure out a replacement in runtime.
			Debug.LogWarning("Houdini Engine is unable to load shaders in non-Editor mode!");
			return null;
#endif
		}

		public static Material GetNewMaterialWithShader(string assetCacheFolderPath, string shaderName, string materialName = "", bool bWriteToFile = true)
		{
			Material material = null;
			Shader shader = FindShader(shaderName);
			if (shader != null)
			{
				material = new Material(shader);
				if (materialName == null || materialName.Length == 0)
				{
					material.name = shaderName;
				}
				else
				{
					material.name = materialName;
				}

				if (bWriteToFile && !string.IsNullOrEmpty(assetCacheFolderPath))
				{
					string materialFileName = materialName + HEU_Defines.HEU_EXT_MAT;
					HEU_AssetDatabase.CreateObjectInAssetCacheFolder(material, assetCacheFolderPath, HEU_Defines.HEU_FOLDER_MATERIALS, materialFileName, typeof(Material));
				}
			}
			else
			{
				Debug.LogWarningFormat("Shader with name {0} not found!", shaderName);
			}
			return material;
		}

		public static Material CreateNewHoudiniStandardMaterial(string assetCacheFolderPath, string materialName, bool bWriteToFile)
		{
			return GetNewMaterialWithShader(assetCacheFolderPath, HEU_PluginSettings.DefaultStandardShader, materialName, bWriteToFile);
		}

		public static void WriteMaterialToAssetCache(Material material, string assetCacheFolderPath, string materialName)
		{
			string materialFileName = materialName + HEU_Defines.HEU_EXT_MAT;
			//Debug.LogFormat("Writing material {0} out to {1}", materialFileName, assetCacheFolderPath);
			HEU_AssetDatabase.CreateObjectInAssetCacheFolder(material, assetCacheFolderPath, HEU_Defines.HEU_FOLDER_MATERIALS, materialFileName, typeof(Material));
		}

		public static bool DoesMaterialExistInAssetCache(Material material)
		{
			return !string.IsNullOrEmpty(HEU_AssetDatabase.GetAssetPath(material));
		}

		public static void DestroyNonAssetMaterial(Material material, bool bRegisterUndo)
		{
			// If the material is not part of the asset database then delete it
			if (material != null && !HEU_AssetDatabase.ContainsAsset(material))
			{
				//Debug.LogFormat("Destroying non-asset material {0}", material.name);
				HEU_GeneralUtility.DestroyImmediate(material, false, bRegisterUndo: bRegisterUndo);
			}
		}

		public static void DeleteAssetMaterial(Material material)
		{
			HEU_AssetDatabase.DeleteAsset(material);
		}

		public static Texture2D RenderAndExtractImageToTexture(HEU_SessionBase session, HAPI_MaterialInfo materialInfo, HAPI_ParmId textureParmID, string textureName, string assetCacheFolderPath)
		{
			//Debug.LogFormat("Rendering texture {0} with name {1} for material {2} at path {3}", textureParmID, textureName, materialInfo.nodeId, assetCacheFolderPath);

			Texture2D texture = null;

			// First we get Houdini to render the texture to an image buffer, then query the buffer over HAPI
			// Next we convert to PNG, and write out to file in our Assets directory
			// The reason for querying as a buffer is to workaround a bug with ExtractHoudiniImageToTextureFile 
			// Note: intentionly ignoring any errors as sometimes there aren't any textures
			if (session.RenderTextureToImage(materialInfo.nodeId, textureParmID, false))
			{
				texture = HEU_MaterialFactory.ExtractHoudiniImageToTextureRaw(session, materialInfo, "C A");
				if(texture != null)
				{
					texture.name = textureName;

					// Get the Textures folder in the assetCacheFolderPath. Make sure it exists.
					assetCacheFolderPath = HEU_AssetDatabase.AppendTexturesPathToAssetFolder(assetCacheFolderPath);
					HEU_AssetDatabase.CreatePathWithFolders(assetCacheFolderPath);

					// We are defaulting to PNG here if no extension already set. This forces it to use PNG format below.
					if (!textureName.EndsWith(".png") && !textureName.EndsWith(".jpg"))
					{
						textureName = textureName + ".png";
					}

					string textureFileName = HEU_Platform.BuildPath(assetCacheFolderPath, string.Format("{0}", textureName));

					byte[] encodedBytes;
					if(textureName.EndsWith(".jpg"))
					{
						encodedBytes = texture.EncodeToJPG();
					}
					else // Use PNG otherwise
					{
						encodedBytes = texture.EncodeToPNG();
					}
					HEU_Platform.WriteBytes(textureFileName, encodedBytes);

					// Re-import for project to recognize the new texture file
					HEU_AssetDatabase.ImportAsset(textureFileName, HEU_AssetDatabase.HEU_ImportAssetOptions.Default);

					// Load the new texture file
					texture = HEU_AssetDatabase.LoadAssetAtPath(textureFileName, typeof(Texture2D)) as Texture2D;
				}
				
				//texture = HEU_MaterialFactory.ExtractHoudiniImageToTextureFile(session, materialInfo, "C A", assetCacheFolderPath);
			}
			return texture;
		}

		private static Texture2D ExtractHoudiniImageToTexturePNGJPEG(HEU_SessionBase session, HAPI_MaterialInfo materialInfo, string imagePlanes)
		{
			Texture2D textureResult = null;

			HAPI_ImageInfo imageInfo = new HAPI_ImageInfo();
			if (!session.GetImageInfo(materialInfo.nodeId, ref imageInfo))
			{
				return textureResult;
			}

			// This will return null if the current imageInfo file format is supported by Unity, otherwise
			// returns a Unity supported file format.
			string desiredFileFormatName = HEU_MaterialData.GetSupportedFileFormat(session, ref imageInfo);

			imageInfo.gamma = HEU_PluginSettings.ImageGamma;
			session.SetImageInfo(materialInfo.nodeId, ref imageInfo);

			// Download the image into memory buffer
			byte[] imageData = null;
			if (!session.ExtractImageToMemory(materialInfo.nodeId, desiredFileFormatName, imagePlanes, out imageData))
			{
				return textureResult;
			}

			// Upload to Unity
			textureResult = new Texture2D(1, 1);
			textureResult.LoadImage(imageData);

			return textureResult;
		}

		private static Texture2D ExtractHoudiniImageToTextureRaw(HEU_SessionBase session, HAPI_MaterialInfo materialInfo, string imagePlanes)
		{
			Texture2D textureResult = null;

			HAPI_ImageInfo imageInfo = new HAPI_ImageInfo();
			if (!session.GetImageInfo(materialInfo.nodeId, ref imageInfo))
			{
				return textureResult;
			}

			imageInfo.dataFormat = HAPI_ImageDataFormat.HAPI_IMAGE_DATA_INT8;
			imageInfo.interleaved = true;
			imageInfo.packing = HAPI_ImagePacking.HAPI_IMAGE_PACKING_RGBA;
			imageInfo.gamma = HEU_PluginSettings.ImageGamma;

			session.SetImageInfo(materialInfo.nodeId, ref imageInfo);

			// Extract image to buffer
			byte[] imageData = null;
			if (!session.ExtractImageToMemory(materialInfo.nodeId, HEU_Defines.HAPI_RAW_FORMAT_NAME, imagePlanes, out imageData))
			{
				return textureResult;
			}

			int colorDataSize = imageInfo.xRes * imageInfo.yRes;
			if (colorDataSize * 4 != imageData.Length)
			{
				Debug.LogErrorFormat("Extracted image size does not match expected image info size."
					+ " Try using non-raw format for texture extraction.");
				return textureResult;
			}

			Color32[] colorData = new Color32[colorDataSize];
			for (int i = 0; i < colorDataSize; ++i)
			{
				colorData[i].r = imageData[i * 4 + 0];
				colorData[i].g = imageData[i * 4 + 1];
				colorData[i].b = imageData[i * 4 + 2];
				colorData[i].a = imageData[i * 4 + 3];
			}

			textureResult = new Texture2D(imageInfo.xRes, imageInfo.yRes, TextureFormat.ARGB32, false);
			textureResult.SetPixels32(colorData);
			textureResult.Apply();

			return textureResult;
		}

		public static Texture2D ExtractHoudiniImageToTextureFile(HEU_SessionBase session, HAPI_MaterialInfo materialInfo, string imagePlanes, string assetCacheFolderPath)
		{
			Texture2D textureResult = null;

			// Get the Textures folder in the assetCacheFolderPath. Make sure it exists.
			assetCacheFolderPath = HEU_AssetDatabase.AppendTexturesPathToAssetFolder(assetCacheFolderPath);
			HEU_AssetDatabase.CreatePathWithFolders(assetCacheFolderPath);

			// Need to pass in full path to Houdini to write out the file
			assetCacheFolderPath = HEU_AssetDatabase.GetAssetFullPath(assetCacheFolderPath);
			if (assetCacheFolderPath == null)
			{
				return textureResult;
			}

			HAPI_ImageInfo imageInfo = new HAPI_ImageInfo();
			if (!session.GetImageInfo(materialInfo.nodeId, ref imageInfo))
			{
				return textureResult;
			}

			// This will return null if the current imageInfo file format is supported by Unity, otherwise
			// returns a Unity supported file format.
			string desiredFileFormatName = HEU_MaterialData.GetSupportedFileFormat(session, ref imageInfo);

			// Extract image to file
			string writtenFilePath = null;
			if (!session.ExtractImageToFile(materialInfo.nodeId, desiredFileFormatName, imagePlanes, assetCacheFolderPath, out writtenFilePath))
			{
				return textureResult;
			}
			

			HEU_AssetDatabase.SaveAndRefreshDatabase();

			// Convert full path back to relative in order to work with AssetDatabase
			string assetRelativePath = HEU_AssetDatabase.GetAssetRelativePath(writtenFilePath);

			// Re-import to refresh the project
			HEU_AssetDatabase.ImportAsset(assetRelativePath, HEU_AssetDatabase.HEU_ImportAssetOptions.Default);

			textureResult = HEU_AssetDatabase.LoadAssetAtPath(assetRelativePath, typeof(Texture2D)) as Texture2D;
			//Debug.LogFormat("Loaded texture to file {0} with format {1}", writtenFilePath, textureResult != null ? textureResult.format.ToString() : "none");

			return textureResult;
		}

		public static Material LoadUnityMaterial(string materialPath)
		{
			if(materialPath.StartsWith(HEU_Defines.DEFAULT_UNITY_BUILTIN_RESOURCES))
			{
				return HEU_AssetDatabase.LoadUnityAssetFromUniqueAssetPath<Material>(materialPath);
			}
			else if (materialPath.StartsWith("Packages/"))
			{
				return HEU_AssetDatabase.LoadAssetAtPath(materialPath, typeof(Material)) as Material;
			}

			string relativePath = materialPath;
			if (relativePath.StartsWith(Application.dataPath))
			{
				// If absolute path, change to relative path
				relativePath = HEU_AssetDatabase.GetAssetRelativePath(materialPath);
			}

			Material material = null;

			string mainPath = null;
			string subPath = null;
			HEU_AssetDatabase.GetSubAssetPathFromPath(relativePath, out mainPath, out subPath);
			if(subPath != null)
			{
				// This is a subasset. Need to find it by first loading the main asset.
				Object subObject = HEU_AssetDatabase.LoadSubAssetAtPath(mainPath, subPath);
				if(subObject != null)
				{
					material = subObject as Material;
				}
			}
			else
			{
				// Try loading from Resources first
				material = Resources.Load<Material>(relativePath) as Material;
				if (material == null)
				{
					// If not in Resources, try loading from project
					HEU_AssetDatabase.ImportAsset(relativePath, HEU_AssetDatabase.HEU_ImportAssetOptions.Default);
					material = HEU_AssetDatabase.LoadAssetAtPath(relativePath, typeof(Material)) as Material;
				}
			}

			return material;
		}

		public static Material LoadSubstanceMaterialWithName(string materialPath, string substanceName)
		{
			Material material = LoadUnityMaterial(materialPath);
#if UNITY_2017_4_OR_NEWER || UNITY_2018_1_OR_NEWER
			Debug.LogErrorFormat("Houdini Engine for Unity does not support the new Substance plugin as of yet!");
#elif UNITY_EDITOR
			if (material != null)
			{
				string assetPath = HEU_AssetDatabase.GetAssetPath(material);
				
				SubstanceImporter substanceImporter = AssetImporter.GetAtPath(assetPath) as SubstanceImporter;

				ProceduralMaterial[] proceduralMaterials = substanceImporter.GetMaterials();
				for(int i = 0; i < proceduralMaterials.Length; ++i)
				{
					if(proceduralMaterials[i].name.Equals(substanceName))
					{
						material = proceduralMaterials[i];
						break;
					}
				}
			}
#endif

			if(material != null)
			{
				Debug.LogFormat("Loaded Substance material with name {0} from path {1}.", substanceName, materialPath);
			}
			else
			{
				Debug.LogWarningFormat("Failed to load Substance material with name {0} from path {1}.", substanceName, materialPath);
			}
			
			return material;
		}

		public static Material LoadSubstanceMaterialWithIndex(string materialPath, int substanceMaterialIndex)
		{
			Material material = LoadUnityMaterial(materialPath);
#if UNITY_2017_4_OR_NEWER || UNITY_2018_1_OR_NEWER
			Debug.LogErrorFormat("Houdini Engine for Unity does not support the new Substance plugin as of yet!");
#elif UNITY_EDITOR
			if (material != null)
			{
				string assetPath = HEU_AssetDatabase.GetAssetPath(material);
				SubstanceImporter substanceImporter = AssetImporter.GetAtPath(assetPath) as SubstanceImporter;

				if(substanceMaterialIndex >= 0 && substanceMaterialIndex < substanceImporter.GetMaterialCount())
				{
					material = substanceImporter.GetMaterials()[substanceMaterialIndex];
				}
			}
#endif
			if (material != null)
			{
				Debug.LogFormat("Loaded Substance material with index {0} from path {1}.", substanceMaterialIndex, materialPath);
			}
			else
			{
				Debug.LogWarningFormat("Failed to load Substance material with index {0} from path {1}.", substanceMaterialIndex, materialPath);
			}

			return material;
		}

		public static int GetUnitySubstanceMaterialKey(string unityMaterialPath, string substanceName, int substanceIndex)
		{
			System.Text.StringBuilder strBuilder = new System.Text.StringBuilder();
			strBuilder.Append(unityMaterialPath);

			if (!string.IsNullOrEmpty(substanceName))
			{
				strBuilder.AppendFormat("-{0}", substanceName);
			}

			if (substanceIndex >= 0)
			{
				strBuilder.AppendFormat("-{0}", substanceIndex);
			}
			return HEU_MaterialFactory.MaterialNameToKey(strBuilder.ToString());
		}

		public static int MaterialNameToKey(string materialName)
		{
			return materialName.GetHashCode();
		}

		public static void EnableGPUInstancing(Material material)
		{
#if UNITY_5_6_OR_NEWER
			material.enableInstancing = true;
#endif
		}

		public static bool MaterialHasGPUInstancingEnabled(Material material)
		{
#if UNITY_5_6_OR_NEWER
			return material.enableInstancing;
#else
			return true;
#endif
		}

		public static Material CopyMaterial(Material material)
		{
			return new Material(material);
		}

		public static Material _defaultStandardMaterial;

		public static Material GetDefaultStandardMaterial()
		{
			if (_defaultStandardMaterial == null)
			{
				_defaultStandardMaterial = HEU_AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
			}
			return _defaultStandardMaterial;
		}

		/// <summary>
		/// Loads texture at path which should be under a Resources/ folder.
		/// </summary>
		/// <param name="path">Path to texture, must be relative to a Resources/ folder.</param>
		/// <returns>Loaded texture or null if failed.</returns>
		public static Texture2D LoadTexture(string path)
		{
			if(HEU_AssetDatabase.IsPathRelativeToAssets(path) || System.IO.Path.IsPathRooted(path))
			{
				HEU_AssetDatabase.ImportAsset(path, HEU_AssetDatabase.HEU_ImportAssetOptions.Default);
				return HEU_AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)) as Texture2D;
			}
			else
			{
				// Try as relative path to Resources
				return Resources.Load<Texture2D>(path);
			}
		}

		public static Texture2D WhiteTexture()
		{
			return Texture2D.whiteTexture;
		}

		public static HEU_MaterialData CreateUnitySubstanceMaterialData(int materialKey, string materialPath, string substanceName, int substanceIndex, List<HEU_MaterialData> materialCache,
			string assetCacheFolderPath)
		{
			// Let's make sure we can find the Unity or Substance material first
			Material material = null;

			HEU_MaterialData.Source sourceType = HEU_MaterialData.Source.UNITY;
			if (!string.IsNullOrEmpty(substanceName))
			{
				sourceType = HEU_MaterialData.Source.SUBSTANCE;
				material = HEU_MaterialFactory.LoadSubstanceMaterialWithName(materialPath, substanceName);
			}
			else if (substanceIndex >= 0)
			{
				sourceType = HEU_MaterialData.Source.SUBSTANCE;
				material = HEU_MaterialFactory.LoadSubstanceMaterialWithIndex(materialPath, substanceIndex);
			}
			else if (!string.IsNullOrEmpty(materialPath))
			{
				material = HEU_MaterialFactory.LoadUnityMaterial(materialPath);
			}

			if (material != null)
			{
				HEU_MaterialData materialData = ScriptableObject.CreateInstance<HEU_MaterialData>();
				materialData._materialSource = sourceType;
				materialData._materialKey = materialKey;
				materialData._material = material;

				materialCache.Add(materialData);
				return materialData;
			}
			else
			{
				// We can't find the material in Unity, so notify user and use a default one which allows to at least get the geometry in.
				if (string.IsNullOrEmpty(materialPath))
				{
					Debug.LogWarningFormat("Empty material name found. Using default material.");
				}
				else
				{
					Debug.LogErrorFormat("Unable to find {0} material {1}. Using a default material instead. Please check material exists in project and reload asset!", sourceType, materialPath);
				}

				// The materialKey helps uniquely identify this material for further look ups. But we also need to get a valid file name
				// to create the default material, so strip out just the file name.
				string strippedFileName = HEU_Platform.GetFileName(materialPath);
				return CreateMaterialInCache(materialKey, strippedFileName, HEU_MaterialData.Source.UNITY, false, materialCache, assetCacheFolderPath);
			}
		}

		public static HEU_MaterialData CreateMaterialInCache(int materialKey, string materialName, HEU_MaterialData.Source sourceType, bool bWriteToFile, List<HEU_MaterialData> materialCache,
			string assetCacheFolderPath)
		{
			HEU_MaterialData materialData = ScriptableObject.CreateInstance<HEU_MaterialData>();
			materialData._materialSource = sourceType;
			materialData._materialKey = materialKey;

			materialData._material = HEU_MaterialFactory.CreateNewHoudiniStandardMaterial(assetCacheFolderPath, materialName, bWriteToFile);
			if (materialData._material != null)
			{
				materialData._material.name = materialName;
			}

			materialCache.Add(materialData);
			return materialData;
		}

		public static HEU_MaterialData GetOrCreateDefaultMaterialInCache(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, bool bWriteToFile, List<HEU_MaterialData> materialCache,
			string assetCacheFolderPath)
		{
			string materialName = GenerateDefaultMaterialName(geoID, partID);
			int materialKey = HEU_MaterialFactory.MaterialNameToKey(materialName);
			HEU_MaterialData materialData = GetMaterialDataFromCache(materialKey, materialCache);
			if (materialData == null)
			{
				materialData = CreateMaterialInCache(materialKey, materialName, HEU_MaterialData.Source.DEFAULT, bWriteToFile, materialCache, assetCacheFolderPath);
			}
			return materialData;
		}

		public static HEU_MaterialData GetMaterialDataFromCache(int materialKey, List<HEU_MaterialData> materialCache)
		{
			foreach (HEU_MaterialData materialData in materialCache)
			{
				if (materialData._materialKey == materialKey)
				{
					return materialData;
				}
			}
			return null;
		}

		public static string GenerateDefaultMaterialName(HAPI_NodeId geoID, HAPI_PartId partID)
		{
			return string.Format("{0}_{1}_{2}", HEU_Defines.DEFAULT_MATERIAL, geoID, partID);
		}

		public static HEU_MaterialData CreateHoudiniMaterialData(HEU_SessionBase session, HAPI_NodeId assetID, HAPI_NodeId materialID, HAPI_NodeId geoID, HAPI_PartId partID, List<HEU_MaterialData> materialCache, string assetCacheFolderPath)
		{
			string materialName = "";

			if (materialID == HEU_Defines.HEU_INVALID_NODE_ID)
			{
				return GetOrCreateDefaultMaterialInCache(session, geoID, partID, false, materialCache, assetCacheFolderPath);
			}
			else
			{
				materialName = HEU_SessionManager.GetUniqueMaterialShopName(assetID, materialID);
			}

			HEU_MaterialData materialData = ScriptableObject.CreateInstance<HEU_MaterialData>();
			materialData._materialSource = HEU_MaterialData.Source.HOUDINI;
			materialData._materialKey = materialID;

			materialData._material = HEU_MaterialFactory.CreateNewHoudiniStandardMaterial(assetCacheFolderPath, materialName, true);
			materialData._material.name = materialName;

			//Debug.LogFormat("New Material ID: {0} - {1}", materialID, materialName);

			if (materialID != HEU_Defines.HEU_INVALID_NODE_ID)
			{
				// Get material info from Houdini to populate the Unity material values

				HAPI_MaterialInfo materialInfo = new HAPI_MaterialInfo();
				if (session.GetMaterialInfo(materialID, ref materialInfo))
				{
					if (materialInfo.exists)
					{
						materialData.UpdateMaterialFromHoudini(materialInfo, assetCacheFolderPath);
					}
				}
			}

			//Debug.LogFormat("Created new material with id={0} and name={1}", materialID, materialName);

			materialCache.Add(materialData);
			return materialData;
		}

		public static Dictionary<int, HEU_MaterialData> GetMaterialDataMapFromCache(List<HEU_MaterialData> materialCache)
		{
			Dictionary<int, HEU_MaterialData> materialMap = new Dictionary<int, HEU_MaterialData>();
			if (materialCache != null)
			{
				foreach (HEU_MaterialData materialData in materialCache)
				{
					if (materialData._materialKey == HEU_Defines.HEU_INVALID_MATERIAL)
					{
						Debug.LogWarningFormat("Invalid material key found! Recommend to reload HDA!");
					}
					else
					{
						materialMap.Add(materialData._materialKey, materialData);
					}
				}
			}
			return materialMap;
		}
	}

}   // HoudiniEngineUnity