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

    public bool IsAddressableObject(GameObject go)
    {
        return addressableGameObjects.Contains(go);
    }

    private void Awake()
    {
        Instance = this;
    }

    public T InstantiateAddressable<T>(string path) where T : Object
    {
        AsyncOperationHandle<T> objectOperation = Addressables.LoadAssetAsync<T>(path);
        T objectAsset = objectOperation.WaitForCompletion();

        StartCoroutine(WaitForAssetRelease(objectAsset));
        return objectAsset;
    }

    private IEnumerator WaitForAssetRelease<T>(T asset)
    {
        yield return null;
        Addressables.Release(asset);
    }

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

    public void ReleaseAddressableGameObjects()
    {
        foreach (var go in addressableGameObjects)
        {
            Addressables.ReleaseInstance(go);
        }
        addressableGameObjects.Clear();
    }
}