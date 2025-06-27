using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Priority_Queue;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Thor.Procedural {
    using PrefabAsset = AssetHandle<GameObject>;
    using MaterialAsset = AssetHandle<Material>;
    public interface IAsset<T> where T : class {
        void OnDelete();
        T Get();
    }

    // public class PrefabAsset : IAsset<GameObject> {
    //     public GameObject prefab;
    //     public bool isAddressable;
    //     private AsyncOperationHandle<GameObject>? handle;

    //     public PrefabAsset(GameObject prefab, bool isAddressable, AsyncOperationHandle<GameObject>? handle = null) {
    //         this.prefab = prefab;
    //         this.isAddressable = isAddressable;
    //         this.handle = handle;
    //     }

    //     public void OnDelete() {
    //         prefab.transform.parent = null;
    //         prefab.SetActive(false);
    //         if (isAddressable && handle.HasValue) {
    //             Addressables.Release(handle.Value);
    //         }
    //         else {
    //             UnityEngine.Object.Destroy(prefab);
    //         }
    //     }

    //     public GameObject Get() {
    //        return prefab;
    //     }
    // }
    public class AssetHandle<T> : IAsset<T> where T : UnityEngine.Object {
        private T asset;
        private AsyncOperationHandle<T>? handle;

        public AssetHandle(T asset, AsyncOperationHandle<T>? handle = null) {
            this.asset = asset;
            this.handle = handle;
        }

        public void OnDelete() {
            if (handle.HasValue) {
                Addressables.Release(handle.Value);
            } else {
                GameObject.Destroy(asset);
            }
        }

        public T Get() => asset;
    }

    

    [ExecuteInEditMode]
    [Serializable]
    public class AssetMap<T, U> where U : class, IAsset<T> where T : class {
        protected Dictionary<string, U> assetMap;

        public AssetMap(Dictionary<string, U> assetMap) {
            this.assetMap = assetMap;
        }

        public virtual T getAsset(string name) {
            return assetMap[name]?.Get();
        }

        public virtual U getAssetWrapper(string name) {
            return assetMap[name];
        }

        public virtual bool ContainsKey(string key) {
            return assetMap.ContainsKey(key);
        }

        public virtual int Count() {
            return assetMap.Count;
        }

        public virtual IEnumerable<string> Keys() {
            return assetMap.Keys;
        }

        public virtual IEnumerable<T> Values() {
            return assetMap.Values.Select(x => x?.Get());
        }

        public virtual void Clear() {
            assetMap.Clear();
        }
    }

    public class ProceduralAssetDatabase : MonoBehaviour {
        public static ProceduralAssetDatabase Instance { get; private set; }

        [SerializeField]
        public List<Material> materials;
        // TODO: move to not use this list
        [SerializeField]
        public List<GameObject> prefabs;

        [SerializeField]
        public int totalMats;

        [SerializeField]
        public ProceduralLRUCacheAssetMap<GameObject, PrefabAsset> assetMap;

        [SerializeField]
        public ProceduralLRUCacheAssetMap<Material, MaterialAsset> materialMap;

        public bool dontDestroyOnLoad = true;

        /// Build database based on materials and prefabs
        public void BuildAssetMap() {
            this.assetMap = new ProceduralLRUCacheAssetMap<GameObject, PrefabAsset>(
                prefabs.GroupBy(p => p.name).ToDictionary(p => p.Key, p => new PrefabAsset(asset: p.First()))
            );

            this.materialMap = new ProceduralLRUCacheAssetMap<Material, MaterialAsset>(
                materials.GroupBy(m => m.name).ToDictionary(p => p.Key, p => new MaterialAsset(asset: p.First()))
            );
        }

        public void Awake() {
            if (Instance != null) {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Debug.Log("*************** Awake called on ProceduralAssetDatabase");
            BuildAssetMap();
            if (dontDestroyOnLoad) {
                DontDestroyOnLoad(gameObject);
            } else {
                // Reset it back to enable caching for next time object is created
                dontDestroyOnLoad = true;
            }
        }

        public void addAsset(GameObject asset, bool procedural = false, AsyncOperationHandle<GameObject>? handle = null) {
            // prefabs.Add(asset);
            assetMap.addAsset(asset.name, new PrefabAsset(asset: asset, handle: handle), procedural: procedural);
        }

        public void addMaterial(Material material, bool procedural = false, AsyncOperationHandle<Material>? handle = null) {
            // prefabs.Add(asset);
            materialMap.addAsset(material.name, new MaterialAsset(asset: material, handle: handle), procedural: procedural);
            // assetMap.addAsset(asset.name, asset, procedural);
            totalMats = materialMap.Count();
        }

        public void addAssets(IEnumerable<GameObject> assets, bool procedural = false, AsyncOperationHandle<GameObject>? handle = null) {
            foreach (var asset in assets) {
                assetMap.addAsset(asset.name, new PrefabAsset(asset: asset, handle: handle), procedural);
            }
        }

        public bool ContainsAssetKey(string key) {
            return assetMap.ContainsKey(key);
        }

        public bool ContainsMaterialKey(string key) {
            // Debug.Log($"======== ContainsMaterialKey {materialMap == null} ");
            return materialMap.ContainsKey(key);
        }

        public void touchProceduralLRUCache(IEnumerable<string> ids) {
            this.assetMap.touch(ids);
        }

        public void removeLRUItems(int limit) {
            this.assetMap.removeLRU(limit: limit);
        }

        public IEnumerator removeLRUItemsAsync(int limit) {
            yield return this.assetMap.removeLRUAsync(limit: limit);
        }

        public IEnumerable<GameObject> GetPrefabs() {
            return this.assetMap.Values();
        }

        public AssetMap<GameObject, PrefabAsset> GetPrefabMap() {
            return this.assetMap;
        }

        public AssetMap<Material, MaterialAsset> GetMaterialMap() {
            return this.materialMap;
        }
    }



    public class ProceduralLRUCacheAssetMap<T, U> : AssetMap<T, U> 
        where T : class
        where U : class, IAsset<T>
    {
        public SimplePriorityQueue<string, int> proceduralAssetQueue { get; private set; }
        public int priorityMinValue { get; private set; }
        public int priorityMaxValue { get; private set; }
        private int originalPriorityMinValue;
        private int originalPriorityMaxValue;

        public ProceduralLRUCacheAssetMap(int priorityMinValue = 0, int priorityMaxValue = 1)
            : this(new Dictionary<string, U>(), priorityMinValue, priorityMaxValue) { }

        public ProceduralLRUCacheAssetMap(
            Dictionary<string, U> assetMap,
            int rankingMinValue = 0,
            int rankingMaxValue = 1
        )
            : base(assetMap) {
            this.priorityMinValue = this.originalPriorityMinValue = rankingMinValue;
            this.priorityMaxValue = this.originalPriorityMaxValue = rankingMaxValue;
            proceduralAssetQueue = new SimplePriorityQueue<string, int>();
        }

        // TODO: If we want an in unity LRU, not driven by python hooks call use here
        // python hooks is desired because asset-dynamic-creation is driven by hooks
        // so there can be times if we internally drive the LRU cache that we may delete
        // assets and not have the hook to create them
        public void addAsset(string id, U asset, bool procedural = true) {
            if (procedural) {
                proceduralAssetQueue.Enqueue(id, this.priorityMaxValue);
            }
            this.assetMap.Add(id, asset);
        }

        public void touch(IEnumerable<string> ids) {
            this.advanceExpiration();
            this.use(ids);
        }

        public void touch(string id) {
            this.advanceExpiration();
            this.use(id);
        }

        public IEnumerator removeLRUAsync(int limit, bool deleteWithHighestPriority = true) {
            int assetCountBeforeRemove = proceduralAssetQueue.Count;

            if (assetCountBeforeRemove == 0) {
                yield break;
            }

            int dequeueCount = removeLRUItems(limit, deleteWithHighestPriority);

            AsyncOperation asyncOp = null;
            if (dequeueCount > 0) {
                // WARNING: Async operation, should be ok for deleting assets if using the same creation-deletion hook
                // cache should be all driven within one system, currently python driven
                var heapSizeBeforeUnload = System.GC.GetTotalMemory(false);
                // System.Diagnostics.Process proc = System.Diagnostics.Process.GetCurrentProcess();
                // proc.Refresh();
                Debug.Log($"Asset count was '{assetCountBeforeRemove}' and limit '{limit}'. Deleted '{dequeueCount}' GameObjects and removed them from cache. Total assets in cache now '{proceduralAssetQueue.Count}'.");
                // Debug.Log($"Process Used Memory(WorkingSet64) {proc.WorkingSet64} Bytes. GarbageCollector available Heap estimate '{heapSizeBeforeUnload}' Bytes.");
                yield return Resources.UnloadUnusedAssets();
                Debug.Log("Asyncop callback called calling GC");
                GC.Collect();
                // proc.Refresh();
                var heapSizeAfterUnload = System.GC.GetTotalMemory(false);
                Debug.Log($"GarbageCollector available Heap Before Unload '{heapSizeBeforeUnload/1e6}' MB. After Garbage Collection {heapSizeAfterUnload/1e6} MB. GarbageCollector available Heap difference {(heapSizeBeforeUnload-heapSizeAfterUnload)/1e6} MB.");
            }

        }

        private int removeLRUItems(int limit, bool deleteWithHighestPriority = true) {
            if (proceduralAssetQueue.Count == 0) {
                return 0;
            }
            var current = proceduralAssetQueue.First;
            var toDequeuePrio = proceduralAssetQueue.GetPriority(current);
            int dequeueCount = 0;

            // Do not delete items with the highest priority if !deleteWithHighestPriority
            while (
                proceduralAssetQueue.Count > limit
                && (deleteWithHighestPriority || toDequeuePrio < this.priorityMaxValue)
            ) {
                var removed = proceduralAssetQueue.Dequeue();

                var asset = this.getAssetWrapper(removed);
                asset.OnDelete();
                this.assetMap.Remove(removed);
                
                //                Debug.Log($"Removing {removed}");
                dequeueCount++;
                if (proceduralAssetQueue.Count == 0) {
                    break;
                }
                current = proceduralAssetQueue.First;
                toDequeuePrio = proceduralAssetQueue.GetPriority(current);
            }
            return dequeueCount;
        }

        public AsyncOperation removeLRU(int limit, bool deleteWithHighestPriority = true) {
           
            int assetCountBeforeRemove = proceduralAssetQueue.Count;

            if (assetCountBeforeRemove == 0) {
                return null;
            }

            int dequeueCount = removeLRUItems(limit, deleteWithHighestPriority);
            
            AsyncOperation asyncOp = null;
            if (dequeueCount > 0) {
                // WARNING: Async operation, should be ok for deleting assets if using the same creation-deletion hook
                // cache should be all driven within one system, currently python driven
                var heapSizeBeforeUnload = System.GC.GetTotalMemory(false);
                // System.Diagnostics.Process proc = System.Diagnostics.Process.GetCurrentProcess();
                // proc.Refresh();
                Debug.Log($"Asset count was '{assetCountBeforeRemove}' and limit '{limit}'. Deleted '{dequeueCount}' GameObjects and removed them from cache. Total assets in cache now '{proceduralAssetQueue.Count}'.");
                // Debug.Log($"Process Used Memory(WorkingSet64) {proc.WorkingSet64} Bytes. GarbageCollector available Heap estimate '{heapSizeBeforeUnload}' Bytes.");
                asyncOp = Resources.UnloadUnusedAssets();
                asyncOp.completed += (op) => {
                    Debug.Log("Asyncop callback called calling GC");
                    GC.Collect();
                };
                

                // #if !UNITY_EDITOR && !UNITY_WEBGL
                float timeout = 2.0f;
                float startTime = Time.realtimeSinceStartup;
                while (!asyncOp.isDone && Time.realtimeSinceStartup - startTime < timeout) {
                    // waiting
                    continue;
                }
                GC.Collect();
                // proc.Refresh();
                var heapSizeAfterUnload = System.GC.GetTotalMemory(false);
                Debug.Log($"GarbageCollector available Heap Before Unload '{heapSizeBeforeUnload/1e6}' MB. After Garbage Collection {heapSizeAfterUnload/1e6} MB. GarbageCollector available Heap difference {(heapSizeBeforeUnload-heapSizeAfterUnload)/1e6} MB.");
                // Debug.Log($"Process Used Memory(WorkingSet64) {proc.WorkingSet64}");
                // proc.Dispose();
                // #endif
            }
            return asyncOp;
        }

        protected void use(IEnumerable<string> ids) {
            foreach (var id in ids) {
                this.use(id);
            }
        }

        protected void use(string name) {
            if (proceduralAssetQueue.Contains(name)) {
                var currentPriority = proceduralAssetQueue.GetPriority(name);
                if (currentPriority < priorityMaxValue) {
                    proceduralAssetQueue.UpdatePriority(name, priorityMaxValue);
                }
            }
        }

        // Amortized O(n)
        protected void advanceExpiration() {
            if (this.priorityMaxValue + 1 != int.MaxValue) {
                this.priorityMinValue++;
                this.priorityMaxValue++;
            } else {
                foreach (var item in proceduralAssetQueue) {
                    var currentPriority = proceduralAssetQueue.GetPriority(item);
                    var distance = currentPriority - this.priorityMinValue;
                    proceduralAssetQueue.UpdatePriority(
                        item,
                        this.originalPriorityMinValue + distance
                    );
                }
                this.priorityMinValue = this.originalPriorityMinValue;
                this.priorityMaxValue = this.originalPriorityMaxValue;
            }
        }

        // O(n) every time
        // public void advanceExpiration() {
        //     foreach (var item in proceduralAssetQueue) {
        //         var currentPriority = proceduralAssetQueue.GetPriority(item);
        //         if (currentPriority < this.rankingMaxValue) {
        //             proceduralAssetQueue.UpdatePriority(item, currentPriority + 1);
        //         }
        //     }
        // }
    }
}
