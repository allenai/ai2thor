using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private InputActionReference _leftMenuHoldReference = null;
    [SerializeField] private int _persistentSceneIndex = 0;
    [SerializeField] private XRManager _xrManager;
    [SerializeField] private GameObject _xrPrefab;
    [SerializeField] private float _waitingTime = 2.5f;
    [SerializeField] private Canvas _sceneSwitchMenu;
    [SerializeField] private GameObject _sceneButtonPrefab;
    [SerializeField] private Transform _sceneButtonContainer;
    [Range(0.25f, 1)] [SerializeField] private float _fadeDuration = 0.25f;
    [Range(0, 10)] [SerializeField] private int _sceneSwitchMenuDistance = 2;
    [Range(0, 180)] [SerializeField] private float _rotDelta = 45;
    [Range(0, 10)] [SerializeField] private float _posDelta = 5;
    [Range(0, 1)] [SerializeField] private float _smoothTime = 0.3f;

    private bool _isSwitchingScene = false;
    private XROrigin _xrOrigin;
    private CanvasGroup _canvasGroup;

    private void Awake() {
        if (UnityEngine.SceneManagement.SceneManager.sceneCount == 1 && UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 0) {
            UnityEngine.SceneManagement.SceneManager.LoadScene(1, LoadSceneMode.Additive);
        }
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += SetActiveScene;

        // Loop through all the scenes and create a scene switch button
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++) {
            if (i != _persistentSceneIndex) {
                string name = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
                Button sceneButton = GameObject.Instantiate(_sceneButtonPrefab, _sceneButtonContainer).GetComponent<Button>();
                sceneButton.onClick.AddListener(() => { SwitchScene(name); });
                sceneButton.GetComponentInChildren<TMP_Text>().text = name;
            }
        }

        _leftMenuHoldReference.action.performed += (InputAction.CallbackContext context) => { ToggleSceneSwitchMenu(); };

        _canvasGroup = _sceneSwitchMenu.GetComponent<CanvasGroup>();
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
                ScreenFader.Instance.StartFadeOut();
            } else {
                _xrManager = GameObject.Instantiate(_xrPrefab).GetComponent<XRManager>();
                _xrManager.transform.SetParent(this.transform);
                _xrManager.transform.SetParent(null);
            }

            GameObject[] agents =  GameObject.FindGameObjectsWithTag("Player");
            if (agents.Length > 0) {
                Vector3 pos = agents[0].transform.position;
                pos.y = _xrManager.transform.position.y;
                _xrManager.transform.position = pos;
            }

            _xrOrigin = _xrManager.GetComponent<XROrigin>();
            _sceneSwitchMenu.worldCamera = _xrOrigin.Camera;
        }
    }

    public void SwitchScene(string name) {
        if (!_isSwitchingScene) {
            ToggleSceneSwitchMenu();
            StartCoroutine(SwitchSceneCoroutine(name));
        }
    }

    private IEnumerator SwitchSceneMenuCoroutine() {
        Vector3 lastCameraPos = _xrOrigin.Camera.transform.position;
        Vector3 lastCameraRot = _xrOrigin.Camera.transform.eulerAngles;

        Vector3 cameraPos = _xrOrigin.Camera.transform.TransformPoint(0, 0, _sceneSwitchMenuDistance);
        _sceneSwitchMenu.transform.position = new Vector3(cameraPos.x, _xrOrigin.Camera.transform.position.y, cameraPos.z);
        _sceneSwitchMenu.transform.LookAt(_xrOrigin.Camera.transform.position);

        while (true) {
            // If position has changed
            if (Vector3.Distance(lastCameraPos, _xrOrigin.Camera.transform.position) > _posDelta) {
                cameraPos = _xrOrigin.Camera.transform.TransformPoint(0, 0, _sceneSwitchMenuDistance);
                _sceneSwitchMenu.transform.position = new Vector3(cameraPos.x, _xrOrigin.Camera.transform.position.y, cameraPos.z);
                lastCameraPos = _xrOrigin.Camera.transform.position;
            }

            // If rotation has changed
            if (Mathf.Abs(_xrOrigin.Camera.transform.eulerAngles.y - lastCameraRot.y) > _rotDelta) {
                StopCoroutine("MoveSwitchSceneMenu");
                StartCoroutine("MoveSwitchSceneMenu");
                lastCameraRot = _xrOrigin.Camera.transform.eulerAngles;
            }
            yield return null;
        }
    }

    private IEnumerator MoveSwitchSceneMenu() {
        Vector3 velocity = Vector3.zero;
        Vector3 cameraPos = _xrOrigin.Camera.transform.TransformPoint(0, 0, _sceneSwitchMenuDistance);
        Vector3 pos = new Vector3(cameraPos.x, _xrOrigin.Camera.transform.position.y, cameraPos.z); ;

        while (_sceneSwitchMenu.transform.position != pos) {
            _sceneSwitchMenu.transform.position = Vector3.SmoothDamp(_sceneSwitchMenu.transform.position, pos, ref velocity, _smoothTime);
            _sceneSwitchMenu.transform.LookAt(_xrOrigin.Camera.transform.position);

            yield return null;
        }
    }

    private void ToggleSceneSwitchMenu() {
        if (!_sceneSwitchMenu.gameObject.activeSelf) {
            StartCoroutine("FadeInMenu");
            StartCoroutine("SwitchSceneMenuCoroutine");
        } else {
            StartCoroutine("FadeOutMenu");
            StopCoroutine("SwitchSceneMenuCoroutine");
        }
    }

    private IEnumerator FadeInMenu() {
        _sceneSwitchMenu.gameObject.SetActive(true);
        float elapsedTime = 0f;

        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;

        while (elapsedTime < _fadeDuration) {
            elapsedTime += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Clamp(elapsedTime / _fadeDuration, 0, 1);
            yield return null;
        }
    }

    private IEnumerator FadeOutMenu() {
        float elapsedTime = 0f;

        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        while (elapsedTime < _fadeDuration) {
            elapsedTime += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Clamp(1 - (elapsedTime / _fadeDuration), 0, 1);

            yield return null;
        }
        _sceneSwitchMenu.gameObject.SetActive(false);
    }
}
