using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;
using UnityEngine;

public class RuntimePrefab : MonoBehaviour {

    // Textures for runtime objects are stored on disk
    // so that they can easily be used across builds,
    // and do not make the build size massive. Here,
    // we store a local reference to the texture.
    public string localTexturePath;

    // we cache the material so that if multiple of the same
    // runtime prefabs are spawned in, they don't each need to load
    // the texture again, since they can share it.
    public Material sharedMaterial;

    public void Awake() {
        // load the texture from disk
        if (localTexturePath != null) {
            GameObject mesh = transform.Find("mesh").gameObject;
            if (sharedMaterial.mainTexture == null) {
                Debug.Log("adding texture!!!");
                byte[] imageBytes = File.ReadAllBytes(localTexturePath);
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(imageBytes);
                sharedMaterial.mainTexture = tex;
            }
        }
    }
}