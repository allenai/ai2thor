using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

/// <summary>
/// 
/// </summary>
public class AddressablesUtil : MonoBehaviour
{
    public static AddressablesUtil Instance { get; private set; }
    private List<GameObject> addressableGameObjects = new List<GameObject>();

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
    }

    /// <summary>
    /// Creates an addressables object (Materials, Text Assets etc)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public T InstantiateAddressable<T>(string path) where T : Object
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
    public GameObject InstantiateAddressableGameObject(string path)
    {
        AsyncOperationHandle<GameObject> objectOperation = Addressables.LoadAssetAsync<GameObject>(path);
        GameObject objectAsset = objectOperation.WaitForCompletion();

        AsyncOperationHandle<GameObject> op = Addressables.InstantiateAsync(path);
        GameObject objectInstance = op.WaitForCompletion();
        addressableGameObjects.Add(objectInstance);

        Addressables.Release(objectAsset);

        return objectInstance;
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
    public void ClearAllAddressablesCache()
    {
        StartCoroutine(ClearAllCoroutine());
    }

    private IEnumerator ClearAllCoroutine()
    {
        yield return Addressables.InitializeAsync();
        var handle = ClearAllAddressablesCacheHandle();
        yield return handle;
    }

    private AsyncOperationHandle<bool> ClearAllAddressablesCacheHandle()
    {
        var locs = GetAllAddressablesLocations();
        return Addressables.ClearDependencyCacheAsync(locs, true);
    }

    /// <summary>
    /// Downloads and caches all addressable bundles on local machine
    /// </summary>
    public void CacheAllAddressables()
    {
        StartCoroutine(DownloadAllCoroutine());
    }

    private IEnumerator DownloadAllCoroutine()
    {
        yield return Addressables.InitializeAsync();
        var handle = CacheAllAddressablesHandle();
        yield return handle;
        Caching.ClearCache();
    }

    private AsyncOperationHandle<IList<IAssetBundleResource>> CacheAllAddressablesHandle()
    {
        var locs = GetAllAddressablesLocations();
        foreach (var location in locs)
            Debug.Log("Will download : " + location.InternalId);
        return Addressables.LoadAssetsAsync<IAssetBundleResource>(locs, null, true);
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

                        // this does not seem to be returning different results
                        //if (dls.Result > 0)
                        {
                            locations.Add(location);
                            addedLocations.Add(hash);
                        }
                    }
                }
            }
        }
        return locations;
    }
}