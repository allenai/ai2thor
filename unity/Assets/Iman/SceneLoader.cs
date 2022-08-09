using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private int _persistentSceneIndex = 0;
    [SerializeField] private XRManager _xrManager;
    [SerializeField] private GameObject _xrPrefab;
    [SerializeField] private float _waitingTime = 2.5f;

    private bool _isSwitchingScene = false;

    private void Awake() {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += SetActiveScene;
    }

    private void OnDestroy() {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += SetActiveScene;
    }

    private IEnumerator UnloadCurrentScene() {
        AsyncOperation unloadOperation = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        while (!unloadOperation.isDone)
            yield return null;
    }

    private IEnumerator LoadNewScene(string name) {
        GC.Collect();

        AsyncOperation loadOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
        loadOperation.allowSceneActivation = false;

        while (loadOperation.progress < 0.90f) {
            yield return null;
        }


        loadOperation.allowSceneActivation = true;
    }

    // Main sequence for when chaning scene
    private IEnumerator SwitchSceneCoroutine(string name) {
        _isSwitchingScene = true;

        // Fade out
        yield return ScreenFader.Instance.StartFadeOut();

        // Unload current scene
        yield return StartCoroutine(UnloadCurrentScene());

        // Load new scene
        yield return StartCoroutine(LoadNewScene(name));

        yield return new WaitForSeconds(_waitingTime);

        // Wait for user to hit indicator
        //yield return StartCoroutine(CheckInidcator());

        // Fade in
        yield return ScreenFader.Instance.StartFadeIn();

        _isSwitchingScene = false;
    }

    private void SetActiveScene(Scene scene, LoadSceneMode mode) {
        if (scene.buildIndex != _persistentSceneIndex) {
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);

            if (_xrManager != null) {
                DestroyImmediate(_xrManager.gameObject);
                _xrManager = GameObject.Instantiate(_xrPrefab).GetComponent<XRManager>();
                _xrManager.transform.SetParent(this.transform);
                _xrManager.transform.SetParent(null);
                ScreenFader.Instance.Alpha = 1;
            } else {
                _xrManager = GameObject.Instantiate(_xrPrefab).GetComponent<XRManager>();
                _xrManager.transform.SetParent(this.transform);
                _xrManager.transform.SetParent(null);
            }

        }
    }

    public void SwitchScene(string name) {
        if (!_isSwitchingScene) {
            StartCoroutine(SwitchSceneCoroutine(name));
        }
    }
}
