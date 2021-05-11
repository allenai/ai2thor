using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablesUtil : MonoBehaviour
{
    public static AddressablesUtil Instance { get; private set; }

    private List<AsyncOperationHandle<GameObject>> gameObjects { get; } = new List<AsyncOperationHandle<GameObject>>();
    private List<AsyncOperationHandle> objects { get; } = new List<AsyncOperationHandle>();


    private void Awake()
    {
        Instance = this;
    }

    public T InstantiateAddressable<T>(string path) where T : Object
    {
        AsyncOperationHandle<T> objectOperation = Addressables.LoadAssetAsync<T>(path);
        T objectAsset = objectOperation.WaitForCompletion();
        T objectInstance = Instantiate(objectAsset);

        objects.Add(objectOperation);

        return objectInstance;
    }

    public GameObject InstantiateAddressableGameObject(string path)
    {
        AsyncOperationHandle<GameObject> objectOperation = Addressables.LoadAssetAsync<GameObject>(path);
        GameObject objectAsset = objectOperation.WaitForCompletion();
        GameObject objectInstance = Instantiate(objectAsset);

        gameObjects.Add(objectOperation);

        return objectInstance;
    }

    public void ReleaseAddressables()
    {
        foreach (var go in gameObjects)
        {
            Addressables.Release(go);
        }
        gameObjects.Clear();

        foreach (var obj in objects)
        {
            Addressables.Release(obj);
        }
        objects.Clear();
    }
}