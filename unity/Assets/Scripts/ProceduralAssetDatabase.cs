using UnityEngine;
using System.Collections.Generic;

namespace Thor.Procedural {
    public class ProceduralAssetDatabase : MonoBehaviour {
        [SerializeField] public List<Material> materials;
        [SerializeField] public List<GameObject> prefabs;
        [SerializeField] public int totalMats;

        // public AssetMap<Material> materialDb {
        //     get;

        // }
    }
}