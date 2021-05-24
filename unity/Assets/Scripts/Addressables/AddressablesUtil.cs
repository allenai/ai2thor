using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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

        StartCoroutine(WaitForAssetRelease(objectAsset));
        return objectAsset;
    }

    /// <summary>
    /// Waits for asset creation and reference to be made before clearing intial resource in memory
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="asset"></param>
    /// <returns></returns>
    private IEnumerator WaitForAssetRelease<T>(T asset)
    {
        yield return null;
        // We are releasing this way primarily for materials which on assignment create a new instance
        Addressables.Release(asset);
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

        var op = Addressables.InstantiateAsync(path);
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
}