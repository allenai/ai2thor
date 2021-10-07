using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

/// <summary>
/// Runtime helper for interfacing with addressables content. Manages memory for assets loaded/unloaded from runtime.
/// </summary>
public class AddressablesUtil : MonoBehaviour
{
    public static AddressablesUtil Instance { get; private set; }
    private List<GameObject> addressableGameObjects = new List<GameObject>();

    private static string CACHING_ARG = "CACHEADDRESSABLES";
    private bool cachingAddressablesBuild = false;

    /// <summary>
    /// Check if gameobject is managed through addressables
    /// </summary>
    /// <param name="go"></param>
    /// <returns></returns>
    public bool IsAddressableObject(GameObject go)
    {
        return addressableGameObjects.Contains(go);
    }

    private void Awake()
    {
        Instance = this;
        Addressables.InitializeAsync();

        cachingAddressablesBuild = MCSUtil.HasArg(CACHING_ARG);
        if (cachingAddressablesBuild)
        {
            ClearAllAddressablesCache(() => CacheAllAddressables());         
        }
    }

    /// <summary>
    /// Creates an addressables object (Materials, Text Assets etc)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public T InstantiateAddressable<T>(string path) where T : UnityEngine.Object
    {
        AsyncOperationHandle<T> objectOperation = Addressables.LoadAssetAsync<T>(path);
        T objectAsset = objectOperation.WaitForCompletion();

        return objectAsset;
    }

    /// <summary>
    /// Instantiates a gameobject through addressables and manages memory reference automatically 
    /// to be cleared when gameobject has been destroyed
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public GameObject InstantiateAddressableGameObject(string path) {
        GameObject objectInstance;

        if (IsAssetAddressable(path)) {
            objectInstance = Addressables.InstantiateAsync(path).WaitForCompletion();
            addressableGameObjects.Add(objectInstance);
            
            GameObject objectAsset = Addressables.LoadAssetAsync<GameObject>(path).WaitForCompletion();
            Addressables.Release(objectAsset);
        }
        else {
            var prefab = Resources.Load<GameObject>(path);
            objectInstance = GameObject.Instantiate(prefab);
        }

        return objectInstance;
    }

    bool IsAssetAddressable(string path) {
        return Addressables.LoadResourceLocationsAsync(path).WaitForCompletion().Count > 0;
    }

    /// <summary>
    /// Releases all addressables gameobject instances and clears assetreference from memory
    /// </summary>
    public void ReleaseAddressableGameObjects()
    {
        foreach (var go in addressableGameObjects)
        {
            Addressables.ReleaseInstance(go);
        }
        addressableGameObjects.Clear();
    }

    /// <summary>
    /// Clears all downloaded addressables cached on local machine
    /// </summary>
    public void ClearAllAddressablesCache(Action completedAction = null)
    {
        Debug.Log("MCS: Clearing Addressables...");
        StartCoroutine(ClearAllCoroutine(completedAction));
    }

    private IEnumerator ClearAllCoroutine(Action completedAction = null)
    {
        yield return Addressables.InitializeAsync();
        var handle = ClearAllAddressablesCacheHandle();
        yield return handle;
        Caching.ClearCache();

        completedAction?.Invoke();
    }

    private AsyncOperationHandle<bool> ClearAllAddressablesCacheHandle()
    {
        var locs = GetAllAddressablesLocations();
        return Addressables.ClearDependencyCacheAsync(locs, true);
    }

    /// <summary>
    /// Downloads and caches all addressable bundles on local machine
    /// </summary>
    public void CacheAllAddressables(Action completedAction = null)
    {
        Debug.Log("MCS: Caching Addressables...");
        StartCoroutine(DownloadAllCoroutine(completedAction));
    }

    private IEnumerator DownloadAllCoroutine(Action completedAction = null)
    {
        yield return Addressables.InitializeAsync();
        IList<IAssetBundleResource> result = CacheAllAddressablesFromBundleResource();
        yield return result;

        if (cachingAddressablesBuild)
        {
            Debug.Log("MCS: Addressables Cached!");
            MCSUtil.CloseApplication();
        }

        completedAction?.Invoke();
    }

    private IList<IAssetBundleResource> CacheAllAddressablesFromBundleResource()
    {
        List<IResourceLocation> locs = GetAllAddressablesLocations();
        foreach (IResourceLocation loc in locs) {
            Debug.Log("MCS: Will download Addressable Asset Bundle Resource Location " + loc.InternalId);
            // Try loading addressables from one Resource Location at a time, and only return if it works (doesn't error).
            List<IResourceLocation> oneLoc = new List<IResourceLocation>() { loc };
            try {
                AsyncOperationHandle<IList<IAssetBundleResource>> handle = Addressables.LoadAssetsAsync<IAssetBundleResource>(oneLoc, null, true);
                IList<IAssetBundleResource> result = handle.WaitForCompletion();
                if(handle.Status == AsyncOperationStatus.Succeeded) {
                    Debug.Log("MCS: Return from " + loc.InternalId);
                    return result;
                }
            }
            // Please note: sometimes the handle will fail but catch its own exception (ignoring this catch block).
            // Use the properties on the handle itself, like Status and OperationException, to investigate.
            catch (Exception e) {
                Debug.Log("MCS: Addressable Asset Bundle Resource Location " + loc.InternalId + " failed to load");
            }
        }
        throw new Exception("MCS: Each Addressable Asset Bundle Resource Location failed to load!");
    }

    private List<IResourceLocation> GetAllAddressablesLocations()
    {
        List<IResourceLocation> locations = new List<IResourceLocation>();
        HashSet<int> addedLocations = new HashSet<int>();
        foreach (IResourceLocator locator in Addressables.ResourceLocators)
        {
            var map = locator as ResourceLocationMap;
            if (map == null)
                continue;
            foreach (KeyValuePair<object, IList<IResourceLocation>> mapLocation in map.Locations)
            {
                foreach (IResourceLocation location in mapLocation.Value)
                {
                    int hash = location.Hash(location.ResourceType);
                    if (addedLocations.Contains(hash))
                        continue;
                    if (typeof(IAssetBundleResource).IsAssignableFrom(location.ResourceType))
                    {
                        var dls = Addressables.GetDownloadSizeAsync(location);
                        dls.WaitForCompletion();
                        
                        locations.Add(location);
                        addedLocations.Add(hash);
                    }
                }
            }
        }
        return locations;
    }
}