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

using System.Collections.Generic;
using UnityEngine;

namespace HoudiniEngineUnity
{
	/// <summary>
	/// Represents generated output for an HDA part.
	/// Contains a HEU_GeneratedOutputData for single GameObject output, as well as a list of children outputs for LOD groups.
	/// </summary>
	[System.Serializable]
	public class HEU_GeneratedOutput
	{
		// The output data for single GameObject (non-groups)
		public HEU_GeneratedOutputData _outputData = new HEU_GeneratedOutputData();

		// List of child output datas (for LOD groups)
		public List<HEU_GeneratedOutputData> _childOutputs = new List<HEU_GeneratedOutputData>();


		/// <summary>
		/// Remove material overrides on given output, replacing
		/// with the generated materials.
		/// </summary>
		public static void ResetMaterialOverrides(HEU_GeneratedOutput output)
		{
			if (HasLODGroup(output))
			{
				foreach(HEU_GeneratedOutputData child in output._childOutputs)
				{
					ResetMaterialOverrides(child);
				}
			}
			else
			{
				ResetMaterialOverrides(output._outputData);
			}
		}

		/// <summary>
		/// Remove material overrides on given output data, replacing
		/// with the generated materials.
		/// </summary>
		public static void ResetMaterialOverrides(HEU_GeneratedOutputData outputData)
		{
			if(outputData._gameObject == null)
			{
				return;
			}

			MeshRenderer meshRenderer = outputData._gameObject.GetComponent<MeshRenderer>();
			meshRenderer.sharedMaterials = outputData._renderMaterials;
		}


		/// <summary>
		/// Returns list of materials that were generated for inGameObject in the output data.
		/// Checks children as well.
		/// </summary>
		/// <param name="output">The output data to check for matching inGameObject</param>
		/// <param name="inGameObject">The inGameObject to find in the output</param>
		/// <returns>List of generated materials or null if not found</returns>
		public static Material[] GetGeneratedMaterialsForGameObject(HEU_GeneratedOutput output, GameObject inGameObject)
		{
			if(output._outputData._gameObject == inGameObject)
			{
				return output._outputData._renderMaterials;
			}
			else
			{
				foreach(HEU_GeneratedOutputData outputData in output._childOutputs)
				{
					if(outputData._gameObject == inGameObject)
					{
						return outputData._renderMaterials;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Returns true if output has LOD groups.
		/// </summary>
		public static bool HasLODGroup(HEU_GeneratedOutput output)
		{
			return output._childOutputs.Count > 0;
		}

		/// <summary>
		/// Returns true if output is using checkMaterial. Checks children outputs as well.
		/// </summary>
		/// <param name="checkMaterial">Material to check</param>
		/// <param name="output">Output to check</param>
		/// <returns>True if output is using checkMaterial</returns>
		public static bool IsOutputUsingMaterial(Material checkMaterial, HEU_GeneratedOutput output)
		{
			bool bValue = IsOutputDataUsingMaterial(checkMaterial, output._outputData);
			if (!bValue)
			{
				foreach (HEU_GeneratedOutputData child in output._childOutputs)
				{
					if(IsOutputDataUsingMaterial(checkMaterial, child))
					{
						bValue = true;
						break;
					}
				}
			}
			return bValue;
		}

		/// <summary>
		/// Returns true if checkMaterial is being used by outputData.
		/// </summary>
		/// <param name="checkMaterial">Material to check</param>
		/// <param name="outputData">Output  datato check</param>
		/// <returns>True if output is using checkMaterial</returns>
		public static bool IsOutputDataUsingMaterial(Material checkMaterial, HEU_GeneratedOutputData outputData)
		{
			if(outputData._renderMaterials != null)
			{
				foreach(Material mat in outputData._renderMaterials)
				{
					if(mat == checkMaterial)
					{
						return true;
					}
				}
			}

			if (outputData._gameObject != null)
			{
				MeshRenderer meshRenderer = outputData._gameObject.GetComponent<MeshRenderer>();
				if (meshRenderer != null)
				{
					Material[] inUseMaterials = meshRenderer.sharedMaterials;
					foreach (Material material in inUseMaterials)
					{
						if (checkMaterial == material)
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		public static void ClearGeneratedMaterialReferences(HEU_GeneratedOutputData generatedOutputData)
		{
			generatedOutputData._renderMaterials = null;
		}

		/// <summary>
		/// Destroys the Collider components that were generated and stored in outputData.
		/// Specially handles MeshColliders for the contained mesh.
		/// </summary>
		/// <param name="outputData">Contains the generated list of Colliders</param>
		public static void DestroyAllGeneratedColliders(HEU_GeneratedOutputData outputData)
		{
			if (outputData._colliders != null)
			{
				int numExisting = outputData._colliders.Count;
				for (int i = 0; i < numExisting; ++i)
				{
					if (outputData._colliders[i] != null)
					{
						if (outputData._colliders[i].GetType() == typeof(MeshCollider))
						{
							HEU_GeneralUtility.DestroyMeshCollider(outputData._colliders[i] as MeshCollider, true);
						}
						else
						{
							HEU_GeneralUtility.DestroyImmediate(outputData._colliders[i], true);
						}
					}
				}
				outputData._colliders.Clear();
			}
		}

		public static void DestroyGeneratedOutput(HEU_GeneratedOutput generatedOutput)
		{
			int numChildren = generatedOutput._childOutputs != null ? generatedOutput._childOutputs.Count : 0;
			for (int i = 0; i < numChildren; ++i)
			{
				if (generatedOutput._childOutputs[i] != null && generatedOutput._childOutputs[i]._gameObject != null)
				{
					HEU_GeneralUtility.DestroyImmediate(generatedOutput._childOutputs[i]._gameObject);
				}
			}
			generatedOutput._childOutputs.Clear();

			DestroyAllGeneratedColliders(generatedOutput._outputData);
			HEU_GeneralUtility.DestroyImmediate(generatedOutput._outputData._gameObject);
			generatedOutput._outputData._gameObject = null;
			generatedOutput._outputData._renderMaterials = null;
		}

		public static void DestroyGeneratedOutputChildren(HEU_GeneratedOutput generatedOutput)
		{
			// LOD Group component
			HEU_GeneralUtility.DestroyComponent<LODGroup>(generatedOutput._outputData._gameObject);

			int numChildren = generatedOutput._childOutputs != null ? generatedOutput._childOutputs.Count : 0;
			for (int i = 0; i < numChildren; ++i)
			{
				if (generatedOutput._childOutputs[i] != null && generatedOutput._childOutputs[i]._gameObject != null)
				{
					DestroyGeneratedOutputData(generatedOutput._childOutputs[i], true);
				}
			}
			generatedOutput._childOutputs.Clear();
		}

		public static void DestroyGeneratedOutputData(HEU_GeneratedOutputData generatedOutputData, bool bDontDeletePersistantResources)
		{
			ClearGeneratedMaterialReferences(generatedOutputData);

			// Colliders
			HEU_GeneratedOutput.DestroyAllGeneratedColliders(generatedOutputData);

			// Components
			HEU_GeneralUtility.DestroyGeneratedMeshMaterialsLODGroups(generatedOutputData._gameObject, bDontDeletePersistantResources);

			// Gameobject
			HEU_GeneralUtility.DestroyImmediate(generatedOutputData._gameObject);
		}

		public static void ClearMaterialsNoLongerUsed(Material[] materialsToCheck, Material[] materialsInUse)
		{
			if (materialsToCheck == null)
			{
				return;
			}

			bool bClear = true;
			int numMaterialsToCheck = materialsToCheck.Length;
			for (int i = 0; i < numMaterialsToCheck; ++i)
			{
				bClear = true;
				if (materialsInUse != null)
				{
					foreach (Material safeMat in materialsInUse)
					{
						if (safeMat == materialsToCheck[i])
						{
							bClear = false;
							break;
						}
					}
				}

				if (bClear)
				{
					materialsToCheck[i] = null;
				}
			}
		}

		/// <summary>
		/// Copy material overrides from sourceOutputData to destOutputData, skipping over materials that weren't overridden.
		/// </summary>
		/// <param name="sourceOutputData">Source output data to get the materials from</param>
		/// <param name="destOutputData">Destination output data to assign materials to</param>
		public static void CopyMaterialOverrides(HEU_GeneratedOutputData sourceOutputData, HEU_GeneratedOutputData destOutputData)
		{
			MeshRenderer srcMeshRenderer = sourceOutputData._gameObject != null ? sourceOutputData._gameObject.GetComponent<MeshRenderer>() : null;
			MeshRenderer destMeshRenderer = destOutputData._gameObject != null ? destOutputData._gameObject.GetComponent<MeshRenderer>() : null;
			if (srcMeshRenderer != null && destMeshRenderer != null)
			{
				Material[] srcAssignedMaterials = srcMeshRenderer.sharedMaterials;
				int numSrcAssignedMaterials = srcAssignedMaterials.Length;

				Material[] destAssignedMaterials = destMeshRenderer.sharedMaterials;
				int numDestAssignedMaterials = destAssignedMaterials.Length;

				for (int j = 0; j < numSrcAssignedMaterials; ++j)
				{
					if ((j < sourceOutputData._renderMaterials.Length) && (j < numDestAssignedMaterials))
					{
						if (srcAssignedMaterials[j] != sourceOutputData._renderMaterials[j])
						{
							// Material has been overriden on the source, so assign same material to destination
							destAssignedMaterials[j] = srcAssignedMaterials[j];
						}
					}
				}

				destMeshRenderer.sharedMaterials = destAssignedMaterials;
			}
		}
	}


	/// <summary>
	/// Holds references for the HDA part output data.
	/// </summary>
	[System.Serializable]
	public class HEU_GeneratedOutputData
	{
		// The generated GameObject which will have generated components and children
		public GameObject _gameObject;

		// The materials used by _gameObject as dictated by the cook process.
		// These could be generated materials, but also existing in Unity (specified by HDA).
		// These will not be material overrides set by user in Unity. This allows to 
		// check which materials have been overriden.
		public Material[] _renderMaterials;

		// Keep track of colliders generated by this asset, so we can remove
		// just those on regeneration
		public List<Collider> _colliders = new List<Collider>();
	}

}   // HoudiniEngineUnity