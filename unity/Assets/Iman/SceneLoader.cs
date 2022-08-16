using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private int _persistentSceneIndex = 0;
    [SerializeField] private XRManager _xrManager;
    [SerializeField] private GameObject _xrPrefab;
    [SerializeField] private float _waitingTime = 2.5f;
    [SerializeField] private Canvas _sceneMenu;
    [SerializeField] private GameObject _sceneButtonPrefab;
    [SerializeField] private Transform _sceneButtonContainer;

    private bool _isSwitchingScene = false;

    private void Awake() {
        if (UnityEngine.SceneManagement.SceneManager.sceneCount == 1 && UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 0) {
            UnityEngine.SceneManagement.SceneManager.LoadScene(1, LoadSceneMode.Additive);
        }
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += SetActiveScene;

        // Loop through all thescens and create a scene switch button
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++) {
            if (i != _persistentSceneIndex) {
                string name = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
                Button sceneButton = GameObject.Instantiate(_sceneButtonPrefab, _sceneButtonContainer).GetComponent<Button>();
                sceneButton.onClick.AddListener(() => { SwitchScene(name); });
                sceneButton.GetComponentInChildren<TMP_Text>().text = name;
            }

        }
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

            _sceneMenu.worldCamera = _xrManager.GetComponent<XROrigin>().Camera;

        }
    }

    public void SwitchScene(string name) {
        if (!_isSwitchingScene) {
            StartCoroutine(SwitchSceneCoroutine(name));
        }
    }
}
