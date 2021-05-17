using System.Collections.Generic;
using UnityEngine;

namespace Thor.Procedural
{
	[ExecuteInEditMode]
	public class AssetMap<T>
	{
		private Dictionary<string, T> assetMap;
		public AssetMap(Dictionary<string, T> assetMap) {
			this.assetMap = assetMap;
		}

		public T getAsset(string name) {
			return assetMap[name];
		}
	}

	[ExecuteInEditMode]
	public class RoomCreatorFactory
	{
		public RoomCreatorFactory(AssetMap<Material> materials, AssetMap<GameObject> prefabs) {

		}
		public static GameObject CreateProceduralRoomFromArray() {

			return null;
		}
	}

}