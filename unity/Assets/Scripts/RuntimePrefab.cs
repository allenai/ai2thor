using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EasyButtons;
using UnityEngine;
using Thor.Procedural.Data;
using Thor.Procedural;


#if UNITY_EDITOR
using EasyButtons.Editor;
using UnityEditor.SceneManagement;
#endif

namespace Thor.Procedural {

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
    
    public ProceduralTextures rawTextures = null;

    public string materialName;

    public Transform meshRendererObject = null;

    // Storing the textures as paths, and loading them on object awake,
    // In the case that rawTextures is provided textures are stored
    // as base64 strings in this component for the prefab, and decoded on awake
    // TODO: maybe store as Textures when rawTextures is not null
    public Material sharedMaterial;

    private static Texture2D SwapChannelsRGBAtoRRRB(Texture2D originalTexture) {
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

    public static Texture2D LoadTextureFromBase64(string base64String)
    {
        if (!string.IsNullOrEmpty(base64String)) {
            // Strip off the header if present (e.g., "data:image/jpeg;base64,")
            var commaIndex = base64String.IndexOf(',');
            if (commaIndex != -1)
            {
                base64String = base64String.Substring(commaIndex + 1);
            }

            byte[] imageData = System.Convert.FromBase64String(base64String);
            Texture2D tex = new Texture2D(2, 2); // Temporary size; will resize on LoadImage
            tex.LoadImage(imageData);
            return tex;
        }
        return null;
    }

    public static Texture2D LoadTextureFromFile(string filePath) {
        if (!string.IsNullOrEmpty(filePath)) {
            byte[] imageBytes = File.ReadAllBytes(filePath);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(imageBytes);
            return tex;
        }
        return null;
    }

    private static void setAlbedoProps(Texture2D tex, Material sharedMaterial) {
        sharedMaterial.mainTexture = tex;
    }

     private static void setMetallicProps(Texture2D tex, Material sharedMaterial, bool swapRGBAtoRRRB = false) {
        if (tex != null) {
            sharedMaterial.EnableKeyword("_METALLICGLOSSMAP");
            if (swapRGBAtoRRRB) {
                tex = SwapChannelsRGBAtoRRRB(tex);
            }
            sharedMaterial.SetTexture("_MetallicGlossMap", tex);
        }
        else {
            sharedMaterial.SetFloat("_Metallic", 0f);
            sharedMaterial.SetFloat("_Glossiness", 0f);
        }
    }

    private static void setNormalProps(Texture2D tex, Material sharedMaterial) {
            sharedMaterial.EnableKeyword("_NORMALMAP");
            sharedMaterial.SetTexture("_BumpMap", tex);
    }

    private static void setEmissionProps(Texture2D tex, Material sharedMaterial) {
            sharedMaterial.globalIlluminationFlags =
                MaterialGlobalIlluminationFlags.RealtimeEmissive;
            sharedMaterial.EnableKeyword("_EMISSION");
            sharedMaterial.SetTexture("_EmissionMap", tex);
            sharedMaterial.SetColor("_EmissionColor", Color.white);
    }

      // Acts as a constructor
    public void SetProperties(
        Material newMaterial = null,
        string albedoTexturePath = null,
        string metallicSmoothnessTexturePath = null,
        string normalTexturePath = null,
        string emissionTexturePath = null,
        ProceduralTextures rawTextures = null
    ) {
        // shoulw happen only when fist creating this component
            this.sharedMaterial = newMaterial;
            this.albedoTexturePath = albedoTexturePath;
            this.metallicSmoothnessTexturePath = metallicSmoothnessTexturePath;
            this.normalTexturePath = normalTexturePath;
            this.emissionTexturePath = emissionTexturePath;
            this.rawTextures = rawTextures;
            // this.materialName = newMaterial.name;

            Debug.Log($"albedoTexturePath { albedoTexturePath} m {metallicSmoothnessTexturePath} n {normalTexturePath} e {emissionTexturePath}");
    }

    public static void LoadTexturesToMaterial(
         Material sharedMaterial,
         string albedoTexturePath = null,
         string metallicSmoothnessTexturePath = null,
         string normalTexturePath = null,
         string emissionTexturePath = null,
         ProceduralTextures rawTextures = null
    ) {
        if (sharedMaterial != null) {

            if (rawTextures == null) {
                // use file paths
                if (sharedMaterial.mainTexture == null) {
                    setAlbedoProps(LoadTextureFromFile(albedoTexturePath), sharedMaterial);
                }
                setMetallicProps(
                    LoadTextureFromFile(metallicSmoothnessTexturePath), 
                    sharedMaterial, 
                    swapRGBAtoRRRB: metallicSmoothnessTexturePath.ToLower().EndsWith(".jpg")
                );
                setNormalProps(LoadTextureFromFile(normalTexturePath), sharedMaterial);
                setEmissionProps(LoadTextureFromFile(emissionTexturePath), sharedMaterial);

                
            }
            else {
                // use string encoded textures
                if (sharedMaterial.mainTexture == null) {
                    setAlbedoProps(LoadTextureFromBase64(rawTextures.albedoBase64JPG), sharedMaterial);
                }
                // swap because it's a jpg
                setMetallicProps(LoadTextureFromBase64(rawTextures.metallicSmoothnessBase64JPG), sharedMaterial, swapRGBAtoRRRB: true);
                setNormalProps(LoadTextureFromBase64(rawTextures.normalBase64JPG), sharedMaterial);
                setEmissionProps(LoadTextureFromBase64(rawTextures.emissionBase64JPG), sharedMaterial);
                
            }
        }
    }
    

    public void reloadtextures(
        ProceduralAssetDatabase db
    ) {
         if (!string.IsNullOrEmpty(this.materialName) && sharedMaterial == null) {
            // Debug.Log($"-------- Getting material from assetdb {materialName}");
            var matMap = db.GetMaterialMap();
            if (matMap.ContainsKey(materialName)) {
                var mat = matMap.getAsset(materialName);
                this.sharedMaterial = mat;
            }
            else {
                Debug.LogError($"Material ${materialName} does not exist in asset Database, either save material in asset Database or set properties of runtimePrefab including sharedMaterial and one of the ways of loading textures either texure paths or rawTextures");
            }

            // Debug.Log($"-------- meshRendererObject null?  {meshRendererObject == null}");
            this.meshRendererObject = transform.Find("mesh");

            if (meshRendererObject != null && this.sharedMaterial != null) {
                // Debug.Log($"=========== Set meshRendererObject material to {this.sharedMaterial.name}");
                var renderer = meshRendererObject.GetComponent<MeshRenderer>();
                // renderer.sharedMaterial = this.sharedMaterial;
                renderer.material = this.sharedMaterial;
            }
        } 
        else if (sharedMaterial != null) {

            LoadTexturesToMaterial(
                sharedMaterial,
                albedoTexturePath: albedoTexturePath,
                metallicSmoothnessTexturePath: metallicSmoothnessTexturePath,
                normalTexturePath: normalTexturePath,
                emissionTexturePath: emissionTexturePath,
                rawTextures: rawTextures
            );
        }
       
    }

    public void Awake() {
        var db = FindObjectOfType<ProceduralAssetDatabase>();
        reloadtextures(db);
        
    }

#if UNITY_EDITOR
    [Button(Expanded = true)]
    public void RealoadTextures() {
        var db = FindObjectOfType<ProceduralAssetDatabase>();
        reloadtextures(db);
    }

#endif
}

}