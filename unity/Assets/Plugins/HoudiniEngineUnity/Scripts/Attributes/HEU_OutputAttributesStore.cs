using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoudiniEngineUnity
{
	/// <summary>
	/// Contains Houdini attributes data (HEU_OutpuAttribute) for generated gameobjects.
	/// Query the attributes by name.
	/// </summary>
	public class HEU_OutputAttributesStore : MonoBehaviour
	{
		private Dictionary<string, HEU_OutputAttribute> _attributes = new Dictionary<string, HEU_OutputAttribute>();

		/// <summary>
		/// Add the given attribute to the internal map by name.
		/// </summary>
		/// <param name="attribute">Attribute data to store</param>
		public void SetAttribute(HEU_OutputAttribute attribute)
		{
			if (string.IsNullOrEmpty(attribute._name))
			{
				Debug.LogWarningFormat("Unable to store attribute with empty name!", attribute._name);
				return;
			}
			_attributes.Add(attribute._name, attribute);
		}

		/// <summary>
		/// Returns the attribute specified by name, or null if not found.
		/// </summary>
		/// <param name="name">Name of attribute</param>
		public HEU_OutputAttribute GetAttribute(string name)
		{
			HEU_OutputAttribute attr = null;
			_attributes.TryGetValue(name, out attr);
			return attr;
		}

		/// <summary>
		/// Clear the store so nothing exists.
		/// </summary>
		public void Clear()
		{
			_attributes.Clear();
		}
	}

}