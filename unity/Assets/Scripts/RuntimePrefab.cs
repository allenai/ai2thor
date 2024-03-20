using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;
using UnityEngine;


#if UNITY_EDITOR
using EasyButtons.Editor;
using UnityEditor.SceneManagement;
#endif
using EasyButtons;

[ExecuteInEditMode]
public class RuntimePrefab : MonoBehaviour {

    // Textures for runtime objects are stored on disk
    // so that they can easily be used across builds,
    // and do not make the build size massive. Here,
    // we store a local reference to the texture.
    public string albedoTexturePath;

    public string metallicSmoothnessTexturePath;

    public string normalTexturePath;

    public string emissionTexturePath;

    // we cache the material so that if multiple of the same
    // runtime prefabs are spawned in, they don't each need to load
    // the texture again, since they can share it.
    public Material sharedMaterial;

    Texture2D SwapChannelsRGBAtoRRRB(Texture2D originalTexture) {
        Color[] pixels = originalTexture.GetPixels();
        for (int i = 0; i < pixels.Length; i++) {
            Color temp = pixels[i];
            pixels[i] = new Color(temp.r, temp.r, temp.r, temp.b); // Swap R and B
        }

        Texture2D newTexture = new Texture2D(originalTexture.width, originalTexture.height);
        newTexture.SetPixels(pixels);
        newTexture.Apply();
        return newTexture;
    }

    private void reloadtextures() {
         GameObject mesh = transform.Find("mesh").gameObject;
        // load the texture from disk
        if (!string.IsNullOrEmpty(albedoTexturePath)) {
            if (sharedMaterial.mainTexture == null) {
                Debug.Log("adding texture!!!");
                byte[] imageBytes = File.ReadAllBytes(albedoTexturePath);
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(imageBytes);
                sharedMaterial.mainTexture = tex;
            }
        }

        if (!string.IsNullOrEmpty(metallicSmoothnessTexturePath)) {
            Debug.Log("metalic "+ metallicSmoothnessTexturePath);
            sharedMaterial.EnableKeyword("_METALLICGLOSSMAP");
            byte[] imageBytes = File.ReadAllBytes(metallicSmoothnessTexturePath);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(imageBytes);
            if (metallicSmoothnessTexturePath.ToLower().EndsWith(".jpg")) {
                tex = SwapChannelsRGBAtoRRRB(tex);
            }
            sharedMaterial.SetTexture("_MetallicGlossMap", tex);
        }

        if (!string.IsNullOrEmpty(normalTexturePath)) {
            sharedMaterial.EnableKeyword("_NORMALMAP");
            byte[] imageBytes = File.ReadAllBytes(normalTexturePath);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(imageBytes);
            sharedMaterial.SetTexture("_BumpMap", tex);
        }

        if (!string.IsNullOrEmpty(emissionTexturePath)) {
            sharedMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            sharedMaterial.EnableKeyword("_EMISSION");
            byte[] imageBytes = File.ReadAllBytes(emissionTexturePath);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(imageBytes);
            sharedMaterial.SetTexture("_EmissionMap", tex);
            sharedMaterial.SetColor("_EmissionColor", Color.white);
        }
    }

    public void Awake() {

       reloadtextures();
    }

    #if UNITY_EDITOR
    [Button(Expanded = true)]
    public void RealoadTextures() { 
        reloadtextures();
    }

    #endif


}