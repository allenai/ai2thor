using UnityEngine;
using System.Collections.Generic;

namespace Thor.Procedural {
    public class ProceduralAssetDatabase : MonoBehaviour {
        public static ProceduralAssetDatabase Instance { get; private set; }

        [SerializeField] public List<Material> materials;
        [SerializeField] public List<GameObject> prefabs;
        [SerializeField] public int totalMats;

        void Awake() {
            if (Instance != null) {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}