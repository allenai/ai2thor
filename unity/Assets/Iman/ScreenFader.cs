
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenFader : MonoBehaviour {
    [SerializeField] private float _speed = 1.0f;
    [SerializeField] private float _alpha = 0.0f;
    [SerializeField] private Color _color = Color.black;
    [SerializeField] private Material _fadeMaterial = null;

    public static ScreenFader Instance { get; private set; }

    public float Alpha {
        get { return _alpha; }
        set { _alpha = value; }
    }

    private void Awake() {
        // If there is an instance, and it's not me, delete myself.
        if (Instance != null && Instance != this) {
            Destroy(Instance);
            Instance = this;
        } else {
            Instance = this;
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        _fadeMaterial.SetFloat("_Alpha", _alpha);
        _fadeMaterial.SetColor("_FadeColor", _color);
        Graphics.Blit(source, destination, _fadeMaterial);
    }

    public Coroutine StartFadeOut() {
        StopAllCoroutines();
        return StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut() {
        while (_alpha < 1.0f) {
            _alpha += _speed * Time.deltaTime;
            yield return null;
        }
        _alpha = 1.0f;
    }

    public Coroutine StartFadeIn() {
        StopAllCoroutines();
        return StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn() {
        while (_alpha > 0.0f) {
            _alpha -= _speed * Time.deltaTime;
            yield return null;
        }
        _alpha = 0.0f;
    }
}
