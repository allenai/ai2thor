
namespace HoudiniEngineUnity
{
	/// <summary>
	/// Container for a Houdini attribute.
	/// </summary>
	[System.Serializable]
	public class HEU_OutputAttribute
	{
		// Name of attribute
		public string _name;

		// Class ownership
		public HAPI_AttributeOwner _class;

		// Storage type
		public HAPI_StorageType _type;

		// Arrays of values, based on class.
		public int[] _intValues;
		public float[] _floatValues;
		public string[] _stringValues;
	}

}
