using UnityEngine;

// Bring in Houdini Engine Unity API
using HoudiniEngineUnity;

[ExecuteInEditMode]
public class HEU_ExampleInstanceCustomAttribute : MonoBehaviour
{
	/// <summary>
	/// Example to show how to use the HEU_OutputAttributeStore component to query
	/// attribute data and set it on instances.
	/// This should be used with heu_instance_cubes_with_custom_attr.hda.
	/// This function is called after HDA is cooked.
	/// </summary>
	void InstancerCallback()
	{
		// Acquire the attribute storage component (HEU_OutputAttributesStore).
		// HEU_OutputAttributesStore contains a dictionary of attribute names to attribute data (HEU_OutputAttribute).
		// HEU_OutputAttributesStore is added to the generated gameobject when an attribute with name 
		// "hengine_attr_store" is created at the detail level.
		HEU_OutputAttributesStore attrStore = gameObject.GetComponent<HEU_OutputAttributesStore>();
		if (attrStore == null)
		{
			Debug.LogWarning("No HEU_OutputAttributesStore component found!");
			return;
		}

		// Query for the health attribute (HEU_OutputAttribute).
		// HEU_OutputAttribute contains the attribute info such as name, class, storage, and array of data.
		// Use the name to get HEU_OutputAttribute.
		// Can use HEU_OutputAttribute._type to figure out what the actual data type is.
		// Note that data is stored in array. The size of the array corresponds to the data type.
		// For instances, the size of the array is the point cound.
		HEU_OutputAttribute healthAttr = attrStore.GetAttribute("health");
		if (healthAttr != null)
		{
			Debug.LogFormat("Found health attribute with data for {0} instances.", healthAttr._intValues.Length);

			for(int i = 0; i < healthAttr._intValues.Length; ++i)
			{
				Debug.LogFormat("{0} = {1}", i, healthAttr._intValues[i]);
			}
		}

		// Query for the stringdata attribute
		HEU_OutputAttribute stringAttr = attrStore.GetAttribute("stringdata");
		if (stringAttr != null)
		{
			Debug.LogFormat("Found stringdata attribute with data for {0} instances.", stringAttr._stringValues.Length);

			for (int i = 0; i < stringAttr._stringValues.Length; ++i)
			{
				Debug.LogFormat("{0} = {1}", i, stringAttr._stringValues[i]);
			}
		}

		// Example of how to map the attribute array values to instances
		// Get the generated instances as children of this gameobject.
		// Note that this will include the current parent as first element (so its number of children + 1 size)
		Transform[] childTrans = transform.GetComponentsInChildren<Transform>();
		int numChildren = childTrans.Length;
		// Starting at 1 to skip parent transform
		for (int i = 1; i < numChildren; ++i)
		{
			Debug.LogFormat("Instance {0}: name = {1}", i, childTrans[i].name);

			// Can use the name to match up indices
			string instanceName = "Instance" + i;
			if (childTrans[i].name.EndsWith(instanceName))
			{
				// Now apply health as scale value
				Vector3 scale = childTrans[i].localScale;

				// Health index is -1 due to child indices off by 1 because of parent
				scale.y = healthAttr._intValues[i - 1];

				childTrans[i].localScale = scale;
			}
		}
	}
}
