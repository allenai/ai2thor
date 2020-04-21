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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace HoudiniEngineUnity
{
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Typedefs (copy these from HEU_Common.cs)
	using HAPI_NodeId = System.Int32;
	using HAPI_PartId = System.Int32;
	using HAPI_StringHandle = System.Int32;



	/// <summary>
	/// General utility functions for the Houdini Engine plugin.
	/// </summary>
	public class HEU_GeneralUtility
	{

		// ARRAYS GET -------------------------------------------------------------------------------------------------

		public delegate bool GetArray1ArgDel<T>(int arg1, [Out] T[] data, int start, int length);
		public delegate bool GetArray2ArgDel<ARG2, T>(int arg1, ARG2 arg2, [Out] T[] data, int start, int length);
		public delegate bool GetArray3ArgDel<ARG3, ARG2, T>(int arg1, ARG2 arg2, ARG3 arg3, [Out] T[] data, int start, int length);

		public static bool GetArray1Arg<T>(int arg1, GetArray1ArgDel<T> func, [Out] T[] data, int start, int count)
		{
			return GetArray(arg1, 0, 0, func, null, null, data, start, count, 1);
		}

		public static bool GetArray2Arg<ARG2, T>(int arg1, ARG2 arg2, GetArray2ArgDel<ARG2, T> func, [Out] T[] data, int start, int count)
		{
			return GetArray(arg1, arg2, 0, null, func, null, data, start, count, 1);
		}

		public static bool GetArray3Arg<ARG3, ARG2, T>(int arg1, ARG2 arg2, ARG3 arg3, GetArray3ArgDel<ARG3, ARG2, T> func, [Out] T[] data, int start, int count)
		{
			return GetArray(arg1, arg2, arg3, null, null, func, data, start, count, 1);
		}

		private static bool GetArray<ARG3, ARG2, T>(
			int arg1, ARG2 arg2, ARG3 arg3,
			GetArray1ArgDel<T> func1,
			GetArray2ArgDel<ARG2, T> func2,
			GetArray3ArgDel<ARG3, ARG2, T> func3,
			[Out] T[] data, int start, int count, int tupleSize)
		{
			int maxArraySize = HEU_Defines.HAPI_MAX_PAGE_SIZE / (Marshal.SizeOf(typeof(T)) * tupleSize);
			int localCount = count;
			int currentIndex = start;

			bool bResult = true;
			while (localCount > 0)
			{
				int length = 0;
				if (localCount > maxArraySize)
				{
					length = maxArraySize;
					localCount -= maxArraySize;
				}
				else
				{
					length = localCount;
					localCount = 0;
				}

				T[] localArray = new T[length * tupleSize];

				if (func1 != null)
				{
					bResult = func1(arg1, localArray, currentIndex, length);
				}
				else if (func2 != null)
				{
					bResult = func2(arg1, arg2, localArray, currentIndex, length);
				}
				else if (func3 != null)
				{
					bResult = func3(arg1, arg2, arg3, localArray, currentIndex, length);
				}
				else
				{
					HEU_HAPIUtility.LogError("No valid delegates given to GetArray< T >!");
					return false;
				}

				if (!bResult)
				{
					break;
				}

				// Copy from temporary array
				for (int i = currentIndex; i < (currentIndex + length); ++i)
				{
					for (int j = 0; j < tupleSize; ++j)
					{
						data[i * tupleSize + j] = localArray[(i - currentIndex) * tupleSize + j];
					}
				}

				currentIndex += length;
			}

			return bResult;
		}

		// ARRAYS SET -------------------------------------------------------------------------------------------------

		public static bool SetArray1Arg<T>(int arg1, GetArray1ArgDel<T> func, [Out] T[] data, int start, int count)
		{
			return SetArray(arg1, 0, func, null, data, start, count, 1);
		}

		public static bool SetArray2Arg<ARG2, T>(int arg1, ARG2 arg2, GetArray2ArgDel<ARG2, T> func, [Out] T[] data, int start, int count)
		{
			return SetArray(arg1, arg2, null, func, data, start, count, 1);
		}

		public static bool SetArray<ARG2, T>(
			int arg1, ARG2 arg2,
			GetArray1ArgDel<T> func1,
			GetArray2ArgDel<ARG2, T> func2,
			[Out] T[] data, int start, int count, int tupleSize)
		{
			int maxArraySize = HEU_Defines.HAPI_MAX_PAGE_SIZE / (Marshal.SizeOf(typeof(T)) * tupleSize);

			int localCount = count;
			int currentIndex = start;

			bool bResult = true;
			while (localCount > 0)
			{
				int length = 0;
				if (localCount > maxArraySize)
				{
					length = maxArraySize;
					localCount -= maxArraySize;
				}
				else
				{
					length = localCount;
					localCount = 0;
				}

				T[] localArray = new T[length * tupleSize];

				// Copy from main array to temporary
				for (int i = currentIndex; i < (currentIndex + length); ++i)
				{
					for (int j = 0; j < tupleSize; ++j)
					{
						localArray[(i - currentIndex) * tupleSize + j] = data[i * tupleSize + j];
					}
				}

				if (func1 != null)
				{
					bResult = func1(arg1, localArray, currentIndex, length);
				}
				else if (func2 != null)
				{
					bResult = func2(arg1, arg2, localArray, currentIndex, length);
				}
				else
				{
					HEU_HAPIUtility.LogError("No valid delegates given to SetArray<T>!");
					return false;
				}

				if (!bResult)
				{
					break;
				}

				currentIndex += length;
			}

			return bResult;
		}


		// ARRAYS GENERAL ---------------------------------------------------------------------------------------------

		/// <summary>
		/// Returns true if the element values in the two arrays match, and lengths match, and neither is null.
		/// </summary>
		/// <typeparam name="T">Type of array elements</typeparam>
		/// <param name="array1">First array</param>
		/// <param name="array2">Second array</param>
		/// <returns>True if array elements match</returns>
		public static bool DoArrayElementsMatch<T>(T[] array1, T[] array2)
		{
			if(ReferenceEquals(array1, array2))
			{
				return true;
			}

			if(array1 == null || array2 == null)
			{
				return false;
			}

			if(array1.Length != array2.Length)
			{
				return false;
			}

			EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
			for(int i = 0; i < array1.Length; ++i)
			{
				if(!equalityComparer.Equals(array1[i], array2[i]))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Returns true if the element values in the two arrays match, and lengths match, and neither is null.
		/// </summary>
		/// <typeparam name="T">Type of array elements</typeparam>
		/// <param name="array1">First array</param>
		/// <param name="startOffset1">Offset into first array to start checking</param>
		/// <param name="array2">Second array</param>
		/// <param name="startOffset2">Offset into second array to start checking</param>
		/// <param name="length">Number of elements to check</param>
		/// <returns>True if array elements match</returns>
		public static bool DoArrayElementsMatch<T>(T[] array1, int startOffset1, T[] array2, int startOffset2, int length)
		{
			if (array1 == null || array2 == null)
			{
				return false;
			}

			int lastIndex1 = startOffset1 + length;
			int lastIndex2 = startOffset2 + length;

			EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
			for (int index1 = startOffset1, index2 = startOffset2; (index1 < lastIndex1) && (index2 < lastIndex2); ++index1, ++index2)
			{
				if (!equalityComparer.Equals(array1[index1], array2[index2]))
				{
					return false;
				}
			}
			return true;
		}


		// ATTRIBUTES -------------------------------------------------------------------------------------------------

		public delegate bool GetAttributeArrayInputFunc<T>(HAPI_NodeId geoID, HAPI_PartId partID, string name, ref HAPI_AttributeInfo info, [Out] T[] items, int start, int end);

		public static bool GetAttributeArray<T>(HAPI_NodeId geoID, HAPI_PartId partID, string name, ref HAPI_AttributeInfo info, T[] items, GetAttributeArrayInputFunc<T> getFunc, int count)
		{
			int maxArraySize = HEU_Defines.HAPI_MAX_PAGE_SIZE / (Marshal.SizeOf(typeof(T)) * info.tupleSize);

			int localCount = count;
			int currentIndex = 0;

			bool bResult = false;

			while (localCount > 0)
			{
				int length = 0;
				if (localCount > maxArraySize)
				{
					length = maxArraySize;
					localCount -= maxArraySize;
				}
				else
				{
					length = localCount;
					localCount = 0;
				}

				T[] localArray = new T[length * info.tupleSize];
				bResult = getFunc(geoID, partID, name, ref info, localArray, currentIndex, length);
				if (!bResult)
				{
					break;
				}

				// Copy data from temporary array
				for (int i = currentIndex; i < currentIndex + length; ++i)
				{
					for (int j = 0; j < info.tupleSize; ++j)
					{
						items[i * info.tupleSize + j] = localArray[(i - currentIndex) * info.tupleSize + j];
					}
				}

				currentIndex += length;
			}

			return bResult;
		}

		public static bool GetAttribute<T>(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, string name, ref HAPI_AttributeInfo info, ref T[] data, GetAttributeArrayInputFunc<T> getFunc)
		{
			int originalTupleSize = info.tupleSize;
			bool bResult = false;
			for (HAPI_AttributeOwner type = 0; type < HAPI_AttributeOwner.HAPI_ATTROWNER_MAX; ++type)
			{
				bResult = session.GetAttributeInfo(geoID, partID, name, type, ref info);
				if (bResult && info.exists)
				{
					break;
				}
			}

			if (!bResult || !info.exists)
			{
				return false;
			}

			if (originalTupleSize > 0)
			{
				info.tupleSize = originalTupleSize;
			}

			data = new T[info.count * info.tupleSize];
			return GetAttributeArray(geoID, partID, name, ref info, data, getFunc, info.count);
		}

		public static void GetAttributeStringDataHelper(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, string name, ref HAPI_AttributeInfo info, ref HAPI_StringHandle[] data)
		{
			int originalTupleSize = info.tupleSize;
			bool bResult = false;
			for (HAPI_AttributeOwner type = 0; type < HAPI_AttributeOwner.HAPI_ATTROWNER_MAX; ++type)
			{
				bResult = session.GetAttributeInfo(geoID, partID, name, type, ref info);
				if (bResult && info.exists)
				{
					break;
				}
			}

			if (!bResult || !info.exists)
			{
				return;
			}

			if (originalTupleSize > 0)
			{
				info.tupleSize = originalTupleSize;
			}

			data = new HAPI_StringHandle[info.count * info.tupleSize];
			bResult = session.GetAttributeStringData(geoID, partID, name, ref info, data, 0, info.count);
			if(!bResult)
			{
				Debug.LogErrorFormat("Failed to get string IDs for attribute {0}", name);
			}
		}

		public static string[] GetAttributeStringData(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, string name, ref HAPI_AttributeInfo attrInfo)
		{
			int[] stringHandles = new int[0];
			if (GetAttribute(session, geoID, partID, name, ref attrInfo, ref stringHandles, session.GetAttributeStringData))
			{
				return HEU_SessionManager.GetStringValuesFromStringIndices(stringHandles);
			}
			return null;
		}

		public delegate bool SetAttributeArrayFunc<T>(HAPI_NodeId geoID, HAPI_PartId partID, string attrName, ref HAPI_AttributeInfo attrInfo, T[] items, int start, int end);

		public static bool SetAttributeArray<T>(HAPI_NodeId geoID, HAPI_PartId partID, string attrName, ref HAPI_AttributeInfo attrInfo, T[] items, SetAttributeArrayFunc<T> setFunc, int count)
		{
			bool bResult = false;

			int maxArraySize = 0;
			if(typeof(T) == typeof(string))
			{
				int maxStringLength = 1;
				foreach(T s in items)
				{
					string str = (string)(object)s;
					if(str.Length > maxStringLength)
					{
						maxStringLength = str.Length;
					}
				}
				maxArraySize = HEU_Defines.HAPI_MAX_PAGE_SIZE / (maxStringLength * Marshal.SizeOf(typeof(char)) * attrInfo.tupleSize);
			}
			else
			{
				maxArraySize = HEU_Defines.HAPI_MAX_PAGE_SIZE / (Marshal.SizeOf(typeof(T)) * attrInfo.tupleSize);
			}

			int localCount = count;
			int currentIndex = 0;

			while(localCount > 0)
			{
				int length = 0;
				if(localCount > maxArraySize)
				{
					length = maxArraySize;
					localCount -= maxArraySize;
				}
				else
				{
					length = localCount;
					localCount = 0;
				}

				T[] localArray = new T[length * attrInfo.tupleSize];

				// Copy subset to temp array
				for(int i = currentIndex; i < currentIndex + length; ++i)
				{
					for(int j = 0; j < attrInfo.tupleSize; ++j)
					{
						localArray[(i - currentIndex) * attrInfo.tupleSize + j] = items[i * attrInfo.tupleSize + j];
					}
				}

				bResult = setFunc(geoID, partID, attrName, ref attrInfo, localArray, currentIndex, length);
				if (!bResult)
				{
					break;
				}

				currentIndex += length;
			}

			return bResult;
		}

		public static bool SetAttribute<T>(HAPI_NodeId geoID, HAPI_PartId partID, string attrName, ref HAPI_AttributeInfo attrInfo, T[] items, SetAttributeArrayFunc<T> setFunc)
		{
			return SetAttributeArray(geoID, partID, attrName, ref attrInfo, items, setFunc, attrInfo.count);
		}

		public static bool CheckAttributeExists(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, string attribName, HAPI_AttributeOwner attribOwner)
		{
			HAPI_AttributeInfo attribInfo = new HAPI_AttributeInfo();
			if(session.GetAttributeInfo(geoID, partID, attribName, attribOwner, ref attribInfo))
			{
				return attribInfo.exists;
			}
			return false;
		}

		public static bool GetAttributeInfo(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, string attribName, ref HAPI_AttributeInfo attribInfo)
		{
			bool bResult = false;
			for (HAPI_AttributeOwner type = 0; type < HAPI_AttributeOwner.HAPI_ATTROWNER_MAX; ++type)
			{
				bResult = session.GetAttributeInfo(geoID, partID, attribName, type, ref attribInfo);
				if (!bResult)
				{
					attribInfo.exists = false;
					return false;
				}
				else if(attribInfo.exists)
				{
					break;
				}
			}
			return true;
		}

		/// <summary>
		/// Returns true if given part in geometry has the given attribute with name.
		/// </summary>
		/// <param name="session">Houdini Engine session</param>
		/// <param name="geoID">Geometry object ID</param>
		/// <param name="partID">Part ID</param>
		/// <param name="attribName">Name of the attribute</param>
		/// <returns>True if attribute exists</returns>
		public static bool HasValidInstanceAttribute(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, string attribName)
		{
			HAPI_AttributeInfo instanceAttrInfo = new HAPI_AttributeInfo();
			GetAttributeInfo(session, geoID, partID, attribName, ref instanceAttrInfo);
			return (instanceAttrInfo.exists && instanceAttrInfo.count > 0);
		}

		/// <summary>
		/// Add or Update HEU_OutputAttributesStore component on the given gameobject for the specified part. 
		/// </summary>
		/// <param name="session">Houdini Engine session</param>
		/// <param name="geoID">Geometry object ID</param>
		/// <param name="partID">Part ID</param>
		/// <param name="go">GameObject that will contain the HEU_OutputAttributesStore component</param>
		public static void UpdateGeneratedAttributeStore(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, GameObject go)
		{
			HAPI_AttributeInfo storeAttrInfo = new HAPI_AttributeInfo();
			GetAttributeInfo(session, geoID, partID, HEU_Defines.HENGINE_STORE_ATTR, ref storeAttrInfo);
			if (!storeAttrInfo.exists)
			{
				DestroyComponent<HEU_OutputAttributesStore>(go);
				return;
			}

			string[] storeAttrs = GetAttributeStringData(session, geoID, partID, HEU_Defines.HENGINE_STORE_ATTR, ref storeAttrInfo);
			if (storeAttrs == null)
			{
				DestroyComponent<HEU_OutputAttributesStore>(go);
				return;
			}

			HEU_OutputAttributesStore attrsStore = GetOrCreateComponent<HEU_OutputAttributesStore>(go);
			attrsStore.Clear();

			string[] attrNames = storeAttrs[0].Split(',');
			for(int a = 0; a < attrNames.Length; ++a)
			{
				HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
				if (GetAttributeInfo(session, geoID, partID, attrNames[a], ref attrInfo) && attrInfo.exists)
				{
					HEU_OutputAttribute outAttr = CreateOutputAttribute(session, geoID, partID, attrNames[a], ref attrInfo);
					if (outAttr != null)
					{
						attrsStore.SetAttribute(outAttr);
					}
				}
			}
		}

		/// <summary>
		/// Helper to create HEU_OutputAttribute given the name and attribute info.
		/// </summary>
		/// <param name="attrName">Name of attribute</param>
		/// <param name="attrInfo">Attribute info</param>
		/// <returns>Created HEU_OutputAttribute</returns>
		public static HEU_OutputAttribute CreateOutputAttributeHelper(string attrName, ref HAPI_AttributeInfo attrInfo)
		{
			HEU_OutputAttribute outputAttr = new HEU_OutputAttribute();
			outputAttr._name = attrName;
			outputAttr._class = attrInfo.owner;
			outputAttr._type = attrInfo.storage;
			return outputAttr;
		}

		/// <summary>
		/// Create the HEU_OutputAttribute for the specified attribute on the given part.
		/// </summary>
		/// <param name="session">Houdini Engine session</param>
		/// <param name="geoID">Geometry object ID</param>
		/// <param name="partID">Part ID</param>
		/// <param name="attrName">Name of the attribute</param>
		/// <param name="attrInfo">Attribute info</param>
		/// <returns>The generated HEU_OutputAttribute if successful else null</returns>
		public static HEU_OutputAttribute CreateOutputAttribute(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, string attrName, ref HAPI_AttributeInfo attrInfo)
		{
			HEU_OutputAttribute outputAttr = null;
			if (attrInfo.storage == HAPI_StorageType.HAPI_STORAGETYPE_FLOAT)
			{
				float[] attrValues = new float[0];
				if (GetAttribute(session, geoID, partID, attrName, ref attrInfo, ref attrValues, session.GetAttributeFloatData))
				{
					outputAttr = CreateOutputAttributeHelper(attrName, ref attrInfo);
					outputAttr._floatValues = attrValues;
				}
			}
			else if (attrInfo.storage == HAPI_StorageType.HAPI_STORAGETYPE_INT)
			{
				int[] attrValues = new int[0];
				if (GetAttribute(session, geoID, partID, attrName, ref attrInfo, ref attrValues, session.GetAttributeIntData))
				{
					outputAttr = CreateOutputAttributeHelper(attrName, ref attrInfo);
					outputAttr._intValues = attrValues;
				}
			}
			else if (attrInfo.storage == HAPI_StorageType.HAPI_STORAGETYPE_STRING)
			{
				string[] attrValues = GetAttributeStringData(session, geoID, partID, attrName, ref attrInfo);
				if (attrValues != null)
				{
					outputAttr = CreateOutputAttributeHelper(attrName, ref attrInfo);
					outputAttr._stringValues = attrValues;
				}
			}
			else
			{
				Debug.LogWarningFormat("Unsupported storage type {0} for storing attribute!", attrInfo.storage);
			}

			return outputAttr;
		}

		/// <summary>
		/// Copy the world transform values from src to dest.
		/// </summary>
		/// <param name="src"></param>
		/// <param name="dest"></param>
		public static void CopyWorldTransformValues(Transform src, Transform dest)
		{
			dest.localScale = src.localScale;
			dest.rotation = src.rotation;
			dest.position = src.position;
		}

		/// <summary>
		/// Multiply the src transform to target transform.
		/// </summary>
		/// <param name="src"></param>
		/// <param name="target"></param>
		public static void ApplyTransformTo(Transform src, Transform target)
		{
			Matrix4x4 mat = target.localToWorldMatrix * src.localToWorldMatrix;

			HEU_HAPIUtility.ApplyMatrixToLocalTransform(ref mat, target);
		}

		/// <summary>
		/// Copy the local transform values from src to dest.
		/// </summary>
		/// <param name="src"></param>
		/// <param name="dest"></param>
		public static void CopyLocalTransformValues(Transform src, Transform dest)
		{
			dest.localScale = src.localScale;
			dest.localRotation = src.localRotation;
			dest.localPosition = src.localPosition;
		}

		/// <summary>
		/// Returns list of child gameobjects of parentGO.
		/// </summary>
		/// <param name="parentGO"></param>
		/// <returns>List of child gameobjects of parentGO</returns>
		public static List<GameObject> GetChildGameObjects(GameObject parentGO)
		{
			Transform parentTransform = parentGO.transform;
			List<GameObject> children = new List<GameObject>();

			foreach (Transform child in parentTransform)
			{
				children.Add(child.gameObject);
			}
			return children;
		}

		/// <summary>
		/// Returns list of child gameobjects of parentGO with name containing the given patter.
		/// Or if bExcluse is true, then the inverse of above list.
		/// </summary>
		/// <param name="parentGO">The parent gameobject to get children from.</param>
		/// <param name="pattern">The pattern to search for in the game.</param>
		/// <param name="bExclude">If true, returns list of children with names not containing the pattern.</param>
		/// <returns></returns>
		public static List<GameObject> GetChildGameObjectsWithNamePattern(GameObject parentGO, string pattern, bool bExclude)
		{
			Transform parentTransform = parentGO.transform;
			List<GameObject> children = new List<GameObject>();

			foreach (Transform child in parentTransform)
			{
				string childName = child.name;

				if (System.Text.RegularExpressions.Regex.IsMatch(childName, pattern) != bExclude)
				{
					children.Add(child.gameObject);
				}
			}
			return children;
		}

		/// <summary>
		/// Returns list of child gameobjects of parentGO that are instances.
		/// </summary>
		/// <param name="parentGO">The parent gameobject to get children from.</param>
		/// <returns></returns>
		public static List<GameObject> GetInstanceChildObjects(GameObject parentGO)
		{
			return GetChildGameObjectsWithNamePattern(parentGO, HEU_Defines.HEU_INSTANCE_PATTERN, false);
		}

		/// <summary>
		/// Returns list of child gameobjects of parentGO that are not instances.
		/// </summary>
		/// <param name="parentGO">The parent gameobject to get children from.</param>
		/// <returns></returns>
		public static List<GameObject> GetNonInstanceChildObjects(GameObject parentGO)
		{
			return GetChildGameObjectsWithNamePattern(parentGO, HEU_Defines.HEU_INSTANCE_PATTERN, true);
		}

		/// <summary>
		/// Returns the gameobject with the name from given list.
		/// </summary>
		/// <param name="goList">List to search</param>
		/// <param name="name">Name to match</param>
		/// <returns>Found gameobject or null if not found</returns>
		public static GameObject GetGameObjectByName(List<GameObject> goList, string name)
		{
			foreach (GameObject go in goList)
			{
				if(go.name.Equals(name))
				{
					return go;
				}
			}
			return null;
		}

		/// <summary>
		/// Find and return gameobject with name in project (not in scene).
		/// </summary>
		/// <param name="name">Name of gameobject to search for</param>
		/// <returns>Found gameobject or null</returns>
		public static GameObject GetGameObjectByNameInProjectOnly(string name)
		{
			GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
			foreach(GameObject go in objects)
			{
				if(IsGameObjectInProject(go) && go.name.Equals(name))
				{
					return go;
				}
			}

			return null;
		}

		/// <summary>
		/// Returns true if given gameobjet is in Project only, and not in scene.
		/// </summary>
		/// <param name="go"></param>
		/// <returns></returns>
		public static bool IsGameObjectInProject(GameObject go)
		{
			return HEU_AssetDatabase.ContainsAsset(go);
		}

		/// <summary>
		/// Find and return gameobject with name in scene (not in project).
		/// </summary>
		/// <param name="name">Name of gameobject to search for</param>
		/// <returns>Found gameobject or null</returns>
		public static GameObject GetGameObjectByNameInScene(string name)
		{
#if UNITY_EDITOR
			int numScenes = UnityEditor.SceneManagement.EditorSceneManager.sceneCount;
			for(int i = 0; i < numScenes; ++i)
			{
				var scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i);
				if(scene.isLoaded)
				{
					GameObject[] gameObjects = scene.GetRootGameObjects();
					int numObjects = gameObjects.Length;
					for(int j = 0; j < numObjects; ++j)
					{
						Transform[] childTransforms = gameObjects[j].GetComponentsInChildren<Transform>(true);
						foreach(Transform t in childTransforms)
						{
							if (t.gameObject.name.Equals(name))
							{
								return t.gameObject;
							}
						}
					}
				}
			}
#else
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
#endif
			return null;
		}

		public static HEU_HoudiniAssetRoot GetHDAByGameObjectNameInScene(string name)
		{
#if UNITY_EDITOR
			int numScenes = UnityEditor.SceneManagement.EditorSceneManager.sceneCount;
			for (int i = 0; i < numScenes; ++i)
			{
				var scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i);
				if (scene.isLoaded)
				{
					GameObject[] gameObjects = scene.GetRootGameObjects();
					int numObjects = gameObjects.Length;
					for (int j = 0; j < numObjects; ++j)
					{
						HEU_HoudiniAssetRoot[] assetRoots = gameObjects[j].GetComponentsInChildren<HEU_HoudiniAssetRoot>();
						foreach (HEU_HoudiniAssetRoot ar in assetRoots)
						{
							if (ar.gameObject.name.Equals(name))
							{
								return ar;
							}
						}
					}
				}
			}
#else
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
#endif
			return null;
		}

		/// <summary>
		/// Gets existing or creates new component for given gameObject.
		/// </summary>
		/// <typeparam name="T">Component to retrieve (and/or create)</typeparam>
		/// <param name="gameObject">GameObject to retrieve (and/or create) from</param>
		/// <returns>Component part of the gameObject</returns>
		public static T GetOrCreateComponent<T>(GameObject gameObject) where T : Component
		{
			T component = gameObject.GetComponent<T>();
			if (component == null)
			{
				component = gameObject.AddComponent<T>();
			}
			return component;
		}

		/// <summary>
		/// Destroy any potential generated components on given gameObject
		/// </summary>
		/// <param name="gameObject"></param>
		public static void DestroyGeneratedComponents(GameObject gameObject)
		{
			DestroyComponent<MeshFilter>(gameObject);
			DestroyComponent<MeshRenderer>(gameObject);
			DestroyComponent<Collider>(gameObject);
			DestroyComponent<HEU_OutputAttributesStore>(gameObject);

#if !HEU_TERRAIN_COLLIDER_DISABLED
			DestroyComponent<TerrainCollider>(gameObject);
#endif

			DestroyComponent<Terrain>(gameObject);
			DestroyComponent<LODGroup>(gameObject);
		}

		public static void DestroyGeneratedMeshComponents(GameObject gameObject)
		{
			DestroyComponent<MeshFilter>(gameObject);
			DestroyComponent<MeshRenderer>(gameObject);
			DestroyComponent<Collider>(gameObject);
			DestroyComponent<LODGroup>(gameObject);
		}
		
		/// <summary>
		/// Destroy any terrain components and data on given gameObject.
		/// </summary>
		/// <param name="gameObject"></param>
		public static void DestroyTerrainComponents(GameObject gameObject)
		{
			if(gameObject == null)
			{
				return;
			}

			Terrain terrain = gameObject.GetComponent<Terrain>();
			if(terrain != null)
			{
				if (terrain.terrainData != null)
				{
					HEU_AssetDatabase.DeleteAsset(terrain.terrainData);
					HEU_GeneralUtility.DestroyImmediate(terrain.terrainData, true);
					terrain.terrainData = null;
				}

				DestroyImmediate(terrain);
			}

#if !HEU_TERRAIN_COLLIDER_DISABLED
			DestroyComponent<TerrainCollider>(gameObject);
#endif
		}

		/// <summary>
		/// Destroys component T if found on gameObject.
		/// </summary>
		/// <typeparam name="T">Component type</typeparam>
		/// <param name="gameObject">GameObject to search component on</param>
		public static void DestroyComponent<T>(GameObject gameObject) where T : Component
		{
			if (gameObject != null)
			{
				T component = gameObject.GetComponent<T>();
				if (component != null)
				{
					DestroyImmediate(component);
				}
			}
		}

		/// <summary>
		/// Destroys obj immediately and permanently.
		/// </summary>
		/// <param name="obj">The object to destroy</param>
		/// <param name="bAllowDestroyingAssets">Force destroy asset</param>
		public static void DestroyImmediate(UnityEngine.Object obj, bool bAllowDestroyingAssets = false, bool bRegisterUndo = false)
		{
#if UNITY_EDITOR
			if (bRegisterUndo)
			{
				Undo.DestroyObjectImmediate(obj);
			}
			else
			{
				UnityEngine.Object.DestroyImmediate(obj, bAllowDestroyingAssets);
			}
#else
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
#endif
		}

		public static void DestroyBakedGameObjects(List<GameObject> gameObjectsToDestroy)
		{
			DestroyBakedGameObjectsWithEndName(gameObjectsToDestroy, null);
		}

		public static void DestroyBakedGameObjectsWithEndName(List<GameObject> gameObjectsToDestroy, string endName)
		{
			int numLeft = gameObjectsToDestroy.Count;
			for (int i = 0; i < numLeft; ++i)
			{
				GameObject deleteGO = gameObjectsToDestroy[i];
				if (string.IsNullOrEmpty(endName) || deleteGO.name.EndsWith(endName))
				{
					gameObjectsToDestroy[i] = null;
					DestroyGeneratedMeshMaterialsLODGroups(deleteGO, true);
					DestroyImmediate(deleteGO);
				}
			}
		}

		public static void DestroyLODGroup(GameObject targetGO, bool bDontDeletePersistantResources)
		{
			LODGroup lodGroup = targetGO.GetComponent<LODGroup>();
			if (lodGroup != null)
			{
				List<GameObject> childrenGO = GetChildGameObjects(targetGO);
				if (childrenGO != null)
				{
					for (int i = 0; i < childrenGO.Count; ++i)
					{
						if (childrenGO[i].gameObject != targetGO)
						{
							DestroyGeneratedMeshMaterialsLODGroups(childrenGO[i], bDontDeletePersistantResources);
							HEU_GeneralUtility.DestroyImmediate(childrenGO[i]);
						}
					}
				}

				DestroyImmediate(lodGroup);
			}
		}

		/// <summary>
		/// Destroy existing components on targetGO which were generated by our bake process.
		/// Persistent resources like meshes, materials, textures will be deleted if bDontDeletePersistentResources is true.
		/// Fills in targetAssetPath with targetGO's asset cache path.
		/// </summary>
		/// <param name="targetGO">The gameobject to destroy components of</param>
		/// <param name="bDontDeletePersistantResources">Whether to delete persistant data</param>
		/// <param name="targetAssetPath">targetGO's asset cache path, if used</param>
		public static void DestroyGeneratedMeshMaterialsLODGroups(GameObject targetGO, bool bDontDeletePersistantResources)
		{
			if(targetGO == null)
			{
				return;
			}

			// Removed the MeshCollider deletion here in favour of moving it into HEU_GeneratedOutputData.DestroyAllGeneratedColliders

			// Delete the target mesh filter's mesh
			MeshFilter targetMeshFilter = targetGO.GetComponent<MeshFilter>();
			if (targetMeshFilter != null)
			{
				Mesh targetMesh = targetMeshFilter.sharedMesh;
				if (targetMesh != null)
				{
					if (!bDontDeletePersistantResources || !HEU_EditorUtility.IsPersistant(targetMesh))
					{
						DestroyImmediate(targetMesh, true);
					}

					targetMesh = null;
					targetMeshFilter.sharedMesh = null;
				}
			}

			// Delete existing materials and textures
			MeshRenderer targetMeshRenderer = targetGO.GetComponent<MeshRenderer>();
			if (targetMeshRenderer != null && !bDontDeletePersistantResources)
			{
				Material[] targetMaterials = targetMeshRenderer.sharedMaterials;

				if (targetMaterials != null)
				{
					for (int i = 0; i < targetMaterials.Length; ++i)
					{
						Material material = targetMaterials[i];
						if (material == null)
						{
							continue;
						}

						DestroyGeneratedMaterial(material);
						targetMaterials[i] = null;
					}

					targetMeshRenderer.sharedMaterials = targetMaterials;
				}
			}

			// If has LOD group, delete children as well. Presumably the children are the LOD meshes.
			DestroyLODGroup(targetGO, bDontDeletePersistantResources);
		}

		public static void DestroyGeneratedMaterial(Material material)
		{
			// Diffuse texture
			if (material.HasProperty("_MainTex"))
			{
				Texture srcDiffuseTexture = material.mainTexture;
				if (srcDiffuseTexture != null)
				{
					HEU_AssetDatabase.DeleteAssetIfInBakedFolder(srcDiffuseTexture);
				}
			}

			// Normal map
			if (material.HasProperty(HEU_Defines.UNITY_SHADER_BUMP_MAP))
			{
				Texture srcNormalMap = material.GetTexture(HEU_Defines.UNITY_SHADER_BUMP_MAP);
				if (srcNormalMap != null)
				{
					HEU_AssetDatabase.DeleteAssetIfInBakedFolder(srcNormalMap);
				}
			}

			// Material
			HEU_AssetDatabase.DeleteAssetIfInBakedFolder(material);
		}

		public static void DestroyMeshCollider(MeshCollider meshCollider, bool bDontDeletePersistantResources)
		{
			Mesh targetColliderMesh = meshCollider != null ? meshCollider.sharedMesh : null;
			if (targetColliderMesh != null)
			{
				if (!bDontDeletePersistantResources || !HEU_EditorUtility.IsPersistant(targetColliderMesh))
				{
					// Need to call DestroyImmediate with bAllowDestroyingAssets to force deleting the asset file
					DestroyImmediate(targetColliderMesh, bAllowDestroyingAssets: true);
				}

				targetColliderMesh = null;
				meshCollider.sharedMesh = null;
			}
		}

		/// <summary>
		/// Set the given gameobject's render visibility.
		/// </summary>
		/// <param name="gameObject">Gameobject to set visibility on</param>
		/// <param name="bVisible">Visibility state</param>
		public static void SetGameObjectRenderVisiblity(GameObject gameObject, bool bVisible)
		{
			if (gameObject != null)
			{
				MeshRenderer partMeshRenderer = gameObject.GetComponent<MeshRenderer>();
				if (partMeshRenderer != null)
				{
					partMeshRenderer.enabled = bVisible;
				}
			}
		}

		/// <summary>
		/// Set the given gameobject childrens' render visibility.
		/// </summary>
		/// <param name="gameObject">The gameobject's children to set the visiblity on</param>
		/// <param name="bVisible">Visibility state</param>
		public static void SetGameObjectChildrenRenderVisibility(GameObject gameObject, bool bVisible)
		{
			if (gameObject != null)
			{
				MeshRenderer[] childRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();
				if (childRenderers != null)
				{
					foreach (MeshRenderer renderer in childRenderers)
					{
						renderer.enabled = bVisible;
					}
				}
			}
		}

		/// <summary>
		/// Set the gameobject's collider state
		/// </summary>
		/// <param name="gameObject">The gameobject's collider will be set to bEnabled</param>
		/// <param name="bEnabled">Collider enabled state</param>
		public static void SetGameObjectColliderState(GameObject gameObject, bool bEnabled)
		{
			if (gameObject != null)
			{
				MeshCollider partMeshCollider = gameObject.GetComponent<MeshCollider>();
				if (partMeshCollider != null)
				{
					partMeshCollider.enabled = bEnabled;
				}
			}
		}

		/// <summary>
		/// Set the given gameobject childrens' collision state.
		/// </summary>
		/// <param name="gameObject">The gameobject's children to set the collder state</param>
		/// <param name="bVisible">Collider enabled state</param>
		public static void SetGameObjectChildrenColliderState(GameObject gameObject, bool bVisible)
		{
			if (gameObject != null)
			{
				Collider[] childColliders = gameObject.GetComponentsInChildren<Collider>();
				if (childColliders != null)
				{
					foreach (Collider collider in childColliders)
					{
						collider.enabled = bVisible;
					}
				}
			}
		}

		public static string ColorToString(Color c)
		{
			return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1},{2},{3}", c[0], c[1], c[2], c[3]);
		}

		public static Color StringToColor(string colorString)
		{
			Color c = new Color();
			string[] strList = colorString.Split(',');
			int count = Mathf.Min(4, strList.Length);
			for (int i = 0; i < count; ++i)
			{
				float f = 1f;
				if(float.TryParse(strList[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f))
				{
					c[i] = f;
				}
			}
			return c;
		}

		public static bool DoesUnityTagExist(string tagName)
		{
			// TODO: Implement this
			return true;// string.IsNullOrEmpty(tagName);
		}

		public static void SetLayer(GameObject rootGO, int layer, bool bIncludeChildren)
		{
			rootGO.layer = layer;
			if(bIncludeChildren)
			{
				foreach(Transform trans in rootGO.transform.GetComponentsInChildren<Transform>(true))
				{
					trans.gameObject.layer = layer;
				}
			}
		}

		public static void SetTag(GameObject rootGO, string tag, bool bIncludeChildren)
		{
			rootGO.tag = tag;
			if (bIncludeChildren)
			{
				foreach (Transform trans in rootGO.transform.GetComponentsInChildren<Transform>(true))
				{
					trans.gameObject.tag = tag;
				}
			}
		}

		public static bool IsMouseWithinSceneView(Camera camera, Vector2 mousePosition)
		{
			return (mousePosition.x < camera.pixelWidth && mousePosition.y < camera.pixelHeight
						&& mousePosition.x > 0 && mousePosition.y > 0);
		}

		public static bool IsMouseOverRect(Camera camera, Vector2 mousePosition, ref Rect rect)
		{
			mousePosition.y = camera.pixelHeight - mousePosition.y;
			return rect.Contains(mousePosition);
		}

		/// <summary>
		/// Returns the type from name.
		/// </summary>
		/// <param name="typeName">String name of type</param>
		/// <returns>Valid type or null if not found in loaded assemblies.</returns>
		public static System.Type GetSystemTypeByName(string typeName)
		{
#if UNITY_EDITOR
			System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
			foreach (System.Reflection.Assembly assembly in assemblies)
			{
				System.Type[] types = assembly.GetTypes();
				foreach (System.Type type in types)
				{
					if(type.Name.Equals(typeName))
					{
						return type;
					}
				}
			}
#else
			Debug.LogWarning(HEU_Defines.HEU_USERMSG_NONEDITOR_NOT_SUPPORTED);
#endif
			return null;
		}

		/// <summary>
		/// Assign the Unity tag to the GameObject if found on the part as attribute.
		/// </summary>
		public static void AssignUnityTag(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, GameObject gameObject)
		{
			HAPI_AttributeInfo tagAttrInfo = new HAPI_AttributeInfo();
			int[] tagAttr = new int[0];
			HEU_GeneralUtility.GetAttribute(session, geoID, partID, HEU_PluginSettings.UnityTagAttributeName, ref tagAttrInfo, ref tagAttr, session.GetAttributeStringData);
			if (tagAttrInfo.exists)
			{
				string tag = HEU_SessionManager.GetString(tagAttr[0]);
				if (tag.Length > 0)
				{
					try
					{
						SetTag(gameObject, tag, true);
					}
					catch (Exception ex)
					{
						Debug.LogWarning("Tag exception: " + ex.ToString());
						Debug.LogWarningFormat("Unity tag '{0}' does not exist for current project. Add the tag in order to use it!", tag);
					}
				}
			}
		}

		/// <summary>
		/// Assign Unity layer to the GameObject if found on the part as attribute.
		/// </summary>
		/// <param name="session"></param>
		/// <param name="geoID"></param>
		/// <param name="partID"></param>
		/// <param name="gameObject"></param>
		public static void AssignUnityLayer(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, GameObject gameObject)
		{
			HAPI_AttributeInfo layerAttrInfo = new HAPI_AttributeInfo();
			int[] layerAttr = new int[0];
			HEU_GeneralUtility.GetAttribute(session, geoID, partID, HEU_PluginSettings.UnityLayerAttributeName, ref layerAttrInfo, ref layerAttr, session.GetAttributeStringData);
			if (layerAttrInfo.exists)
			{
				string layerStr = HEU_SessionManager.GetString(layerAttr[0]);
				if (layerStr.Length > 0)
				{
					int layer = LayerMask.NameToLayer(layerStr);
					if (layer < 0)
					{
						Debug.LogWarningFormat("Unity layer '{0}' does not exist for current project. Add the layer in order to use it!", layerStr);
					}
					else
					{
						HEU_GeneralUtility.SetLayer(gameObject, layer, true);
					}
				}
			}
		}

		/// <summary>
		/// If part has static attribute, then set gameobject as static or not
		/// </summary>
		public static void MakeStaticIfHasAttribute(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, GameObject gameObject)
		{
			HAPI_AttributeInfo staticAttrInfo = new HAPI_AttributeInfo();
			int[] staticAttr = new int[0];
			HEU_GeneralUtility.GetAttribute(session, geoID, partID, HEU_PluginSettings.UnityStaticAttributeName, ref staticAttrInfo, ref staticAttr, session.GetAttributeIntData);
			if (staticAttrInfo.exists && staticAttr.Length > 0)
			{
				HEU_EditorUtility.SetStatic(gameObject, (staticAttr[0] == 1));
			}
		}

		/// <summary>
		/// Returns the Unity script attribute value, if found, on the specified geo's part.
		/// The attribute must be of string type, and owned by detail.
		/// </summary>
		/// <param name="session">Session that the asset resides in</param>
		/// <param name="geoID">The geo node's ID</param>
		/// <param name="partID">The part's ID</param>
		/// <returns>The name of the Unity script, or null if not found</returns>
		public static string GetUnityScriptAttributeValue(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID)
		{
			HAPI_AttributeInfo scriptAttributeInfo = new HAPI_AttributeInfo();
			int[] scriptAttr = new int[0];
			string scriptString = null;

			HEU_GeneralUtility.GetAttribute(session, geoID, partID, HEU_PluginSettings.UnityScriptAttributeName, ref scriptAttributeInfo, ref scriptAttr, session.GetAttributeStringData);
			if (scriptAttributeInfo.exists)
			{
				if (scriptAttr.Length > 0)
				{
					scriptString = HEU_SessionManager.GetString(scriptAttr[0]);
				}
			}

			return scriptString;
		}

		/// <summary>
		/// Returns the single string value from Attribute with given name and owner type, or null if failed.
		/// </summary>
		/// <param name="session">Houdini Engine session to query</param>
		/// <param name="geoID">The geometry ID in Houdini</param>
		/// <param name="partID">The part ID in Houdini</param>
		/// <param name="attrName">Name of the attribute to query</param>
		/// <param name="attrOwner">Owner type of the attribute</param>
		/// <returns>Valid string if successful, otherwise returns null</returns>
		public static string GetAttributeStringValueSingle(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, string attrName, HAPI_AttributeOwner attrOwner)
		{
			if (string.IsNullOrEmpty(attrName))
			{
				return null;
			}

			HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
			int[] stringHandle = new int[0];
			HEU_GeneralUtility.GetAttribute(session, geoID, partID, attrName, ref attrInfo, ref stringHandle, session.GetAttributeStringData);
			if (attrInfo.exists)
			{
				if (attrInfo.owner != attrOwner)
				{
					Debug.LogWarningFormat("Expected {0} attribute owner for attribute {1} but got {2}!", attrOwner, attrName, attrInfo.owner);
				}
				else if (stringHandle.Length > 0)
				{
					return HEU_SessionManager.GetString(stringHandle[0]);
				}
			}
			return null;
		}

		/// <summary>
		/// Helper to get a single float value for the Attribute with given name, or 0 if none found.
		/// Returns true if successful, otherwise false.
		/// </summary>
		/// <param name="session">Houdini Engine session to query</param>
		/// <param name="geoID">The geometry ID in Houdini</param>
		/// <param name="partID">The part ID in Houdini</param>
		/// <param name="attrName">Name of the attribute to query</param>
		/// <param name="value">Float attribute value</param>
		/// <returns>True if successfully found and acquired value, otherwise false</returns>
		public static bool GetAttributeFloatSingle(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, 
			string attrName, out float value)
		{
			value = 0;
			bool bResult = false;

			if (!string.IsNullOrEmpty(attrName))
			{
				HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
				float[] values = new float[0];
				HEU_GeneralUtility.GetAttribute(session, geoID, partID, attrName, ref attrInfo,
					ref values, session.GetAttributeFloatData);
				if (attrInfo.exists && values.Length > 0)
				{
					value = values[0];
					bResult = true;
				}
			}
			return bResult;
		}

		/// <summary>
		/// Helper to get a single int value for the Attribute with given name, or 0 if none found.
		/// Returns true if successful, otherwise false.
		/// </summary>
		/// <param name="session">Houdini Engine session to query</param>
		/// <param name="geoID">The geometry ID in Houdini</param>
		/// <param name="partID">The part ID in Houdini</param>
		/// <param name="attrName">Name of the attribute to query</param>
		/// <param name="value">Int attribute value</param>
		/// <returns>True if successfully found and acquired value, otherwise false</returns>
		public static bool GetAttributeIntSingle(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID,
			string attrName, out int value)
		{
			value = 0;
			bool bResult = false;

			if (!string.IsNullOrEmpty(attrName))
			{
				HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
				int[] values = new int[0];
				HEU_GeneralUtility.GetAttribute(session, geoID, partID, attrName, ref attrInfo,
					ref values, session.GetAttributeIntData);
				if (attrInfo.exists && values.Length > 0)
				{
					value = values[0];
					bResult = true;
				}
			}
			return bResult;
		}

		/// <summary>
		/// Helper to get a single color value for the Attribute with given name, or 0 if none found.
		/// Returns true if successful, otherwise false.
		/// </summary>
		/// <param name="session">Houdini Engine session to query</param>
		/// <param name="geoID">The geometry ID in Houdini</param>
		/// <param name="partID">The part ID in Houdini</param>
		/// <param name="attrName">Name of the attribute to query</param>
		/// <param name="value">Color attribute value</param>
		/// <returns>True if successfully found and acquired value, otherwise false</returns>
		public static bool GetAttributeColorSingle(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID,
			string attrName, ref Color value)
		{
			bool bResult = false;

			if (!string.IsNullOrEmpty(attrName))
			{
				HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
				float[] values = new float[0];
				HEU_GeneralUtility.GetAttribute(session, geoID, partID, attrName, ref attrInfo,
					ref values, session.GetAttributeFloatData);
				if (attrInfo.exists && values.Length >= 3)
				{
					value = new Color();
					value.r = Mathf.Clamp01(values[0]);
					value.g = Mathf.Clamp01(values[1]);
					value.b = Mathf.Clamp01(values[2]);

					if (values.Length >= 4)
					{
						value.a = Mathf.Clamp01(values[3]);
					}

					bResult = true;
				}
			}
			return bResult;
		}

		/// <summary>
		/// Returns true if specified geometry and part has the given atttribute name.
		/// </summary>
		/// <param name="session">Houdini session to check</param>
		/// <param name="geoID">Geometry object ID</param>
		/// <param name="partID">Part ID</param>
		/// <param name="attrName">The name of the attribute to check</param>
		/// <param name="attrOwner">The owner type for the attribute</param>
		/// <returns></returns>
		public static bool HasAttribute(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, string attrName, HAPI_AttributeOwner attrOwner)
		{
			if (string.IsNullOrEmpty(attrName))
			{
				return false;
			}

			HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
			bool bResult = session.GetAttributeInfo(geoID, partID, attrName, attrOwner, ref attrInfo);
			return (bResult && attrInfo.exists);
		}

		/// <summary>
		/// Attach scripts in Unity to the asset root gameobject, and optionally
		/// invoke a function with an optional argument.
		/// </summary>
		/// <param name="scriptSet">A string with format: scriptname:function:msg[;scriptname:function:msg]</param>
		public static void AttachScriptWithInvokeFunction(string scriptSet, GameObject gameObject)
		{
			// Scripts will be attached to the given gameObject.
			// Multiple scripts can be attached, where each script string is delimted by semicolon.
			// Then if set, the function will be invoked on the script passing in the message.
			string expectedFormat = "scriptname:function:argument[;scriptname:function:argument]";

			string[] scriptLists = scriptSet.Split(';');
			foreach(string scriptToAttach in scriptLists)
			{
				int scriptColon = scriptToAttach.IndexOf(":");
				string scriptTypeName = scriptColon > 0 ? scriptToAttach.Substring(0, scriptColon).Trim() : scriptToAttach;
				System.Type scriptType = HEU_GeneralUtility.GetSystemTypeByName(scriptTypeName);
				if (scriptType == null)
				{
					Debug.LogFormat("Script with name {0} not found! Unable to attach script from attribute: {1}. Expected format: {2}", scriptTypeName, scriptToAttach, expectedFormat);
					return;
				}

				Component component = null;
				try
				{
					component = gameObject.GetComponent(scriptType);
					if (component == null)
					{
						Debug.LogFormat("Attaching script {0} to gameobject", scriptType);
						component = gameObject.AddComponent(scriptType);
						if (component == null)
						{
							Debug.LogFormat("Unable to attach script component with type '{0}' from script attribute: {1}", scriptType.ToString(), scriptToAttach);
							return;
						}
					}
				}
				catch(System.ArgumentException ex)
				{
					Debug.LogWarningFormat("Specified unity_script '{0}' does not derive from MonoBehaviour. Unable to attach script.\n{1}", scriptTypeName, ex.ToString());
					return;
				}

				if (scriptColon + 1 >= scriptToAttach.Length)
				{
					// No function
					return;
				}

				int functionNameLength = 0;
				int functionColon = scriptToAttach.IndexOf(":", scriptColon + 1);
				functionNameLength = (functionColon > 0) ? functionColon - (scriptColon + 1) : scriptToAttach.Length - (scriptColon + 1);

				if (functionNameLength > 0)
				{
					string scriptFunction = scriptToAttach.Substring(scriptColon + 1, functionNameLength).Trim();
					if (functionColon + 1 < scriptToAttach.Length)
					{
						// Get argument
						string scriptArgument = scriptToAttach.Substring(functionColon + 1).Trim();
						//Debug.LogFormat("Invoking script function {0} with argument {1}", scriptFunction, scriptArgument);
						component.SendMessage(scriptFunction, scriptArgument, SendMessageOptions.DontRequireReceiver);
					}
					else
					{
						// No argument
						//Debug.LogFormat("Invoking script function {0}", scriptFunction);
						component.SendMessage(scriptFunction, SendMessageOptions.DontRequireReceiver);
					}
				}
			}
		}

		public static bool IsInCameraView(Camera camera, Vector3 point)
		{
			Vector3 viewportPos = camera.WorldToViewportPoint(point);
			return (viewportPos.z > 0) && (new Rect(0, 0, 1, 1).Contains(viewportPos));
		}

		public static List<HEU_Handle> FindOrGenerateHandles(HEU_SessionBase session, ref HAPI_AssetInfo assetInfo, HAPI_NodeId assetID, string assetName, HEU_Parameters parameters, List<HEU_Handle> currentHandles)
		{
			List<HEU_Handle> newHandles = new List<HEU_Handle>();

			if (assetInfo.handleCount <= 0)
			{
				return newHandles;
			}

			HAPI_HandleInfo[] handleInfos = new HAPI_HandleInfo[assetInfo.handleCount];
			HEU_GeneralUtility.GetArray1Arg(assetID, session.GetHandleInfo, handleInfos, 0, assetInfo.handleCount);

			for (int i = 0; i < handleInfos.Length; ++i)
			{
				if (handleInfos[i].bindingsCount <= 0)
				{
					continue;
				}

				string handleName = HEU_SessionManager.GetString(handleInfos[i].nameSH, session);

				HEU_Handle.HEU_HandleType handleType = HEU_Handle.HEU_HandleType.UNSUPPORTED;
				string handleTypeString = HEU_SessionManager.GetString(handleInfos[i].typeNameSH, session);
				if (handleTypeString.Equals(HEU_Defines.HAPI_HANDLE_TRANSFORM))
				{
					handleType = HEU_Handle.HEU_HandleType.XFORM;
				}
				else
				{
					// Commented out warning as it gets annoying, especially with "Curve" handles
					//Debug.LogWarningFormat("Asset {0} has unsupported Handle type {0} for handle {1}", assetName, handleName, handleTypeString);
					continue;
				}

				HEU_Handle newHandle = null;
				foreach (HEU_Handle curHandle in currentHandles)
				{
					if (curHandle.HandleName.Equals(handleName))
					{
						newHandle = curHandle;
						break;
					}
				}

				if (newHandle == null)
				{
					newHandle = ScriptableObject.CreateInstance<HEU_Handle>();
				}

				bool bSuccess = newHandle.SetupHandle(session, assetID, i, handleName, handleType, ref handleInfos[i], parameters);
				if (bSuccess)
				{
					newHandles.Add(newHandle);
					//Debug.LogFormat("Found handle {0} of type {1}", handleName, handleTypeString);
				}
			}

			return newHandles;
		}

		/// <summary>
		/// Copy components from srcGo to destGO, ignoring those already on destGO.
		/// </summary>
		/// <param name="srcGO">Components to source from</param>
		/// <param name="destGO">Components to copy to</param>
		public static void CopyComponents(GameObject srcGO, GameObject destGO)
		{
#if UNITY_EDITOR
			Component[] srcComponents = srcGO.GetComponents<Component>();
			Component[] destComponents = destGO.GetComponents<Component>();

			bool bSkip = false;
			foreach (Component srcComp in srcComponents)
			{
				bSkip = false;
				System.Type srcType = srcComp.GetType();
				foreach (Component destComp in destComponents)
				{
					if (srcType == destComp.GetType())
					{
						bSkip = true;
						break;
					}
				}

				if (!bSkip)
				{
					UnityEditorInternal.ComponentUtility.CopyComponent(srcComp);
					UnityEditorInternal.ComponentUtility.PasteComponentAsNew(destGO);
				}
			}
#endif
		}

		/// <summary>
		/// Loads image file and return as Texture.
		/// Presumes image file is PNG or JPG format (i.e. supported by Texture2D.LoadImage).
		/// </summary>
		/// <param name="filePath">Path to image file</param>
		/// <returns>Loaded texture or null if failed</returns>
		public static Texture LoadTextureFromFile(string filePath)
		{
			Texture2D newTexture = null;

			if (HEU_Platform.DoesFileExist(filePath))
			{
				try
				{
					byte[] imageData = System.IO.File.ReadAllBytes(filePath);
					newTexture = new Texture2D(2, 2);
					newTexture.LoadImage(imageData);
					newTexture.Apply();
					return newTexture;
				}
				catch (Exception ex)
				{
					Debug.LogErrorFormat("Loading image at {0} triggered exception: {1}", filePath, ex);
				}
			}
			return newTexture;
		}

		/// <summary>
		/// Returns a new texture with given size, and filled in with given single color.
		/// </summary>
		public static Texture2D MakeTexture(int width, int height, Color color)
		{
			if (width <= 0 || height <= 0)
			{
				return null;
			}

			Color[] pixels = new Color[width * height];
			for(int i = 0; i < pixels.Length; ++i)
			{
				pixels[i] = color;
			}

			Texture2D texture = new Texture2D(width, height);
			texture.SetPixels(pixels);
			texture.Apply(false, true);
			return texture;
		}

		/// <summary>
		/// Replace the first occurence of searchStr in srcStr with replaceStr.
		/// </summary>
		/// <param name="srcStr"></param>
		/// <param name="searchStr"></param>
		/// <param name="replaceStr"></param>
		/// <returns></returns>
		public static string ReplaceFirstOccurrence(string srcStr, string searchStr, string replaceStr)
		{
			int index = srcStr.IndexOf(searchStr);
			if (index < 0)
			{
				return srcStr;
			}
			return srcStr.Substring(0, index) + replaceStr + srcStr.Substring(index + searchStr.Length);
		}

		/// <summary>
		/// Sets given childTransform parented to the given parentTransform, with clean (identity) transform matrix.
		/// </summary>
		public static void SetParentWithCleanTransform(Transform parentTransform, Transform childTransform)
		{
			childTransform.parent = parentTransform;
			childTransform.localPosition = Vector3.zero;
			childTransform.localRotation = Quaternion.identity;
			childTransform.localScale = Vector3.one;
		}

		/// <summary>
		/// Copy src HAPI_Transfrom to dest HAPI_Transform.
		/// </summary>
		public static void CopyHAPITransform(ref HAPI_Transform src, ref HAPI_Transform dest)
		{
			src.position.CopyToWithResize<float>(ref dest.position);
			src.rotationQuaternion.CopyToWithResize<float>(ref dest.rotationQuaternion);
			src.scale.CopyToWithResize<float>(ref dest.scale);
			src.shear.CopyToWithResize<float>(ref dest.shear);

			dest.rstOrder = src.rstOrder;
		}

		/// <summary>
		/// Get the assigned material via string attribute from the given part.
		/// </summary>
		public static string GetMaterialAttributeValueFromPart(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID)
		{
			string materialName = null;
			HAPI_AttributeInfo unityMaterialAttrInfo = new HAPI_AttributeInfo();
			HAPI_StringHandle[] unityMaterialAttrName = new HAPI_StringHandle[0];
			HEU_GeneralUtility.GetAttribute(session, geoID, partID, HEU_PluginSettings.UnityMaterialAttribName,
				ref unityMaterialAttrInfo, ref unityMaterialAttrName, session.GetAttributeStringData);

			if (unityMaterialAttrInfo.exists && unityMaterialAttrName.Length > 0)
			{
				materialName = HEU_SessionManager.GetString(unityMaterialAttrName[0], session);
				if (string.IsNullOrEmpty(materialName))
				{
					// Warn user of empty string, but add it anyway to our map so we don't keep trying to parse it
					Debug.LogWarningFormat("Found empty material attribute value for terrain heightfield part.");
				}
			}

			return materialName;
		}

		/// <summary>
		/// Replace the targetGO's Collider component's mesh with mesh from
		/// sourceColliderGO's mesh.
		/// If targetGO has a MeshCollider, its mesh will be replaced but the component kept.
		/// If targetGO has any other collider, it will be destroyed and new MeshCollider added.
		/// If targetGO has no other collider, a new MeshCollider will be added.
		/// </summary>
		/// <param name="targetGO">The gameobject to replace the collider mesh for.</param>
		/// <param name="sourceColliderGO">The gameobject containing MeshFilter with mesh to use.</param>
		public static void ReplaceColliderMeshFromMeshFilter(GameObject targetGO, GameObject sourceColliderGO)
		{
			MeshFilter srcMeshFilter = sourceColliderGO.GetComponent<MeshFilter>();
			if (srcMeshFilter != null)
			{
				// Either replace existing MeshCollider's mesh, or remove other colliders, 
				// and add new MeshCollider with source mesh.
				MeshCollider meshCollider = targetGO.GetComponent<MeshCollider>();
				if (meshCollider == null)
				{
					HEU_GeneralUtility.DestroyComponent<Collider>(targetGO);
					meshCollider = targetGO.AddComponent<MeshCollider>();
				}

				meshCollider.sharedMesh = srcMeshFilter.sharedMesh;
			}
		}

		/// <summary>
		/// Replace the targetGO's Collider component's mesh with mesh from
		/// sourceColliderGO's MeshCollider mesh.
		/// If targetGO has a MeshCollider, its mesh will be replaced but the component kept.
		/// If targetGO has any other collider, it will be destroyed and new MeshCollider added.
		/// If targetGO has no other collider, a new MeshCollider will be added.
		/// </summary>
		/// <param name="targetGO">The gameobject to replace the collider mesh for.</param>
		/// <param name="sourceColliderGO">The gameobject containing MeshCollider with mesh to use.</param>
		public static void ReplaceColliderMeshFromMeshCollider(GameObject targetGO, GameObject sourceColliderGO)
		{
			MeshCollider srcMeshCollider = sourceColliderGO.GetComponent<MeshCollider>();
			if (srcMeshCollider != null)
			{
				// Either replace existing MeshCollider's mesh, or remove other colliders, 
				// and add new MeshCollider with source mesh.
				MeshCollider meshCollider = targetGO.GetComponent<MeshCollider>();
				if (meshCollider == null)
				{
					HEU_GeneralUtility.DestroyComponent<Collider>(targetGO);
					meshCollider = targetGO.AddComponent<MeshCollider>();
				}

				meshCollider.sharedMesh = srcMeshCollider.sharedMesh;
			}
		}
	}


	public static class ArrayExtensions
	{
		/// <summary>
		/// Set the given array with the given value for every element.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="array"></param>
		/// <param name="defaultValue"></param>
		public static void Init<T>(this T[] array, T defaultValue)
		{
			if (array != null)
			{
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = defaultValue;
				}
			}
		}

		/// <summary>
		/// Set the given list with the given value for every element.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="array"></param>
		/// <param name="defaultValue"></param>
		public static void Init<T>(this List<T> array, T defaultValue)
		{
			if(array != null)
			{
				for (int i = 0; i < array.Count; i++)
				{
					array[i] = defaultValue;
				}
			}
		}

		public static void CopyToWithResize<T>(this T[] srcArray, ref T[] destArray)
		{
			if (srcArray == null)
			{
				destArray = null;
			}
			else
			{
				if (destArray == null || destArray.Length != srcArray.Length)
				{
					destArray = new T[srcArray.Length];
				}

				Array.Copy(srcArray, destArray, srcArray.Length);
			}
		}
	}

	public class ReverseCompare : IComparer
	{
		public int Compare(object x, object y)
		{
			return (new CaseInsensitiveComparer().Compare(y, x));
		}
	}

}   // HoudiniEngineUnity