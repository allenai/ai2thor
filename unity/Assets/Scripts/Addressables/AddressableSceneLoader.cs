using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.UI;

/// <summary>
/// Scene loader for loading and unloading addressable scenes. 
/// This script is not destroyed on load, and the menu canvas can be re-enabled for future extensions of menu options.
/// </summary>
public class AddressableSceneLoader : MonoBehaviour
{
    public GameObject loadingAddressablesUI;
    public GameObject loadingLogo;
    public GameObject loadingText;
    public AssetReference scene;
    public string defaultSceneFile;
    private AsyncOperationHandle<SceneInstance> asyncOperationHandle;
    private Text text;
    public AsyncOperationHandle loadingAddressable;

    private void Awake() 
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start() 
    {
        text = loadingText.GetComponent<Text>();
        LoadScene();
    }

    /// <summary>
    /// Load the scene assigned to the addressable "scene" AssetReference.
    /// </summary>
    public void LoadScene()
    {
        loadingAddressablesUI.SetActive(true);

        AsyncOperationHandle<SceneInstance> loadingScene = Addressables.LoadSceneAsync(scene, UnityEngine.SceneManagement.LoadSceneMode.Single);
        loadingAddressable = loadingScene;
        loadingScene.Completed += SceneLoadComplete;
        loadingText.SetActive(true);
        loadingLogo.SetActive(true);
        text.text = "Loading MCS scene...";
        text.color = new Color(0f, 1f, 0f);
    }

    /// <summary>
    /// Called from LoadScene when the scene load operation has completed.
    /// </summary>
    /// <param name="operationHandle"></param>
    private void SceneLoadComplete(AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> operationHandle)
    {
        if(operationHandle.Status == AsyncOperationStatus.Succeeded)
        {
            loadingAddressablesUI.SetActive(false);

            text.text = "Scene load successful.";
            text.color = new Color(0f, 1f, 0f);
            loadingText.SetActive(false);
            loadingLogo.SetActive(false);

            asyncOperationHandle = operationHandle;

            MCSMain MCS = FindObjectOfType<MCSMain>();
            MCSController MCSController = FindObjectOfType<MCSController>();

            MCSController.asyncOperationHandle = operationHandle;
            MCS.asyncOperationHandle = operationHandle;
            MCS.defaultSceneFile = defaultSceneFile;

            Debug.Log("Load scene succeeded.");
        }
        else
        {
            text.text = "Error loading scene.";
            text.color = new Color(1f, 0f, 0f);
            Debug.Log("Could not load scene.");
        }
    }

    /// <summary>
    /// Unloads the scene assigned to the "scene" AssetReference.
    /// </summary>
    private void UnloadScene()
    {
        Addressables.UnloadSceneAsync(asyncOperationHandle, true).Completed += op => 
        {
            if(op.Status == AsyncOperationStatus.Succeeded)
            {
                text.text = "Unloaded scene.";
                text.color = new Color(0f, 1f, 0f);
                Debug.Log("Successfully unloaded scene.");
            }
            else
            {
                text.text = "Error unloading scene.";
                text.color = new Color(1f, 0f, 0f);
                Debug.Log("Could not unload scene.");
            }
        };
    }

}
