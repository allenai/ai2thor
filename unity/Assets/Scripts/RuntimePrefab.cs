using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EasyButtons;
using UnityEngine;
using Thor.Procedural.Data;
using Thor.Procedural;
using MessagePack.Unity.Extension;



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

    public float textureReplaceEnergyThreshold = float.MaxValue;

    public TexturesRGB texturesRGB = null;

    public ResizeTextureSettings resizeTextureSettings = null;

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

    public static Texture2D ResizeTexture(Texture2D source, float scale)
    {
        if (Mathf.Approximately(scale, 1.0f) || source == null) {
            return source;
        }

        int newWidth = Mathf.RoundToInt(source.width * scale);
        int newHeight = Mathf.RoundToInt(source.height * scale);

        RenderTexture rt = new RenderTexture(newWidth, newHeight, 0, RenderTextureFormat.ARGB32);
        rt.filterMode = FilterMode.Bilinear;

        RenderTexture.active = rt;
        Graphics.Blit(source, rt);

        Texture2D result = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        rt.Release();

        UnityEngine.Object.Destroy(source);
        return result;
    }

    public static Texture2D ResizeTexture(Texture2D source, float scale, Material material, FilterMode filter = FilterMode.Bilinear)
    {
        if (Mathf.Approximately(scale, 1f) || source == null) {
            return source;
        }

            int width = Mathf.RoundToInt(source.width * scale);
        int height = Mathf.RoundToInt(source.height * scale);

        RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        rt.filterMode = filter;

        Graphics.Blit(source, rt, material);
        RenderTexture.active = rt;

        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        UnityEngine.Object.Destroy(source); // Release memory

        return result;
    }

    public static Texture2D ResizeNormalMap(Texture2D source, float scale) {
        Material normalMat = new Material(Shader.Find("Custom/ResizeNormalMap"));
        return ResizeTexture(source, scale, normalMat);
    }

    public static Texture2D ResizeMetallicMap(Texture2D source, float scale) {
        Material normalMat = new Material(Shader.Find("Custom/ResizeMetallicMap"));
        return ResizeTexture(source, scale, normalMat);
    }



    public static Texture2D ResizeTextureBicubic(Texture2D source, float scale)
    {
        if (Mathf.Approximately(scale, 1.0f) || source == null) {
            return source;
        }

        int newWidth = Mathf.RoundToInt(source.width * scale);
        int newHeight = Mathf.RoundToInt(source.height * scale);

        RenderTexture rt = new RenderTexture(newWidth, newHeight, 0, RenderTextureFormat.ARGB32);
        rt.filterMode = FilterMode.Point;

        Shader shader = Shader.Find("Custom/BicubicResize");
        if (shader == null) {
            Debug.LogError("Shader not found!");
            throw new InvalidOperationException("No shader Hidden/BicubicResize");
        }
        
        Material bicubicMat = new Material(shader);
        Graphics.Blit(source, rt, bicubicMat);
        

        RenderTexture.active = rt;
        Texture2D result = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        rt.Release();

        UnityEngine.Object.Destroy(source);
        return result;
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

    // private static void setAlbedoProps(Texture2D tex, Material sharedMaterial) {
    //     sharedMaterial.mainTexture = tex;
    // }

    //  private static void setMetallicProps(Texture2D tex, Material sharedMaterial, bool swapRGBAtoRRRB = false) {
    //     if (tex != null) {
    //         sharedMaterial.EnableKeyword("_METALLICGLOSSMAP");
    //         if (swapRGBAtoRRRB) {
    //             tex = SwapChannelsRGBAtoRRRB(tex);
    //         }
    //         sharedMaterial.SetTexture("_MetallicGlossMap", tex);
    //     }
    //     else {
    //         sharedMaterial.SetFloat("_Metallic", 0f);
    //         sharedMaterial.SetFloat("_Glossiness", 0f);
    //     }
    // }

    // private static void setNormalProps(Texture2D tex, Material sharedMaterial) {
    //         sharedMaterial.EnableKeyword("_NORMALMAP");
    //         sharedMaterial.SetTexture("_BumpMap", tex);
    // }

    // private static void setEmissionProps(Texture2D tex, Material sharedMaterial) {
    //         sharedMaterial.globalIlluminationFlags =
    //             MaterialGlobalIlluminationFlags.RealtimeEmissive;
    //         sharedMaterial.EnableKeyword("_EMISSION");
    //         sharedMaterial.SetTexture("_EmissionMap", tex);
    //         sharedMaterial.SetColor("_EmissionColor", Color.white);
    // }

    // public static void LoadTexturesToMaterial(
    //      Material sharedMaterial,
    //      string albedoTexturePath = null,
    //      string metallicSmoothnessTexturePath = null,
    //      string normalTexturePath = null,
    //      string emissionTexturePath = null,
    //      ProceduralTextures rawTextures = null,
    //      float? textureReplaceEnergyThreshold = null,
    //      TexturesRGB texturesRGB = null
    // ) {
    //     if (sharedMaterial != null) {
            
    //         if (rawTextures == null) {
    //             // use file paths
    //             if (sharedMaterial.mainTexture == null) {
    //                 setAlbedoProps(LoadTextureFromFile(albedoTexturePath), sharedMaterial);
    //             }
    //             setMetallicProps(
    //                 LoadTextureFromFile(metallicSmoothnessTexturePath), 
    //                 sharedMaterial, 
    //                 swapRGBAtoRRRB: metallicSmoothnessTexturePath.ToLower().EndsWith(".jpg")
    //             );
    //             setNormalProps(LoadTextureFromFile(normalTexturePath), sharedMaterial);
    //             setEmissionProps(LoadTextureFromFile(emissionTexturePath), sharedMaterial);

                
    //         }
    //         else {
    //             // use string encoded textures
    //             if (sharedMaterial.mainTexture == null) {
    //                 setAlbedoProps(LoadTextureFromBase64(rawTextures.albedoBase64JPG), sharedMaterial);
    //             }
    //             // swap because it's a jpg
    //             setMetallicProps(LoadTextureFromBase64(rawTextures.metallicSmoothnessBase64JPG), sharedMaterial, swapRGBAtoRRRB: true);
    //             setNormalProps(LoadTextureFromBase64(rawTextures.normalBase64JPG), sharedMaterial);
    //             setEmissionProps(LoadTextureFromBase64(rawTextures.emissionBase64JPG), sharedMaterial);
                
    //         }
    //     }
    // }

    private static void setAlbedoProps(Texture2D tex, Material sharedMaterial, SerializableColor fallbackColor = null, bool useFallbackColor = false) {
    if (useFallbackColor && fallbackColor != null) {
        sharedMaterial.SetColor("_Color", fallbackColor.toUnityColor());
    } else {
        sharedMaterial.mainTexture = tex;
    }
}

private static void setMetallicProps(Texture2D tex, Material sharedMaterial, bool swapRGBAtoRRRB = false, SerializableColor fallbackColor = null, bool useFallbackColor = false) {
    if (useFallbackColor && fallbackColor != null) {
        Color metallic = fallbackColor.toUnityColor();
        sharedMaterial.SetFloat("_Metallic", metallic.r);
        sharedMaterial.SetFloat("_Glossiness", metallic.b);
        // Debug.Log($"---- _METALLICGLOSSMAP {fallbackColor.toUnityColor()} useFallbackColor {useFallbackColor}");
    } else if (tex != null) {
        sharedMaterial.EnableKeyword("_METALLICGLOSSMAP");
        
        if (swapRGBAtoRRRB) {
            tex = SwapChannelsRGBAtoRRRB(tex);
        }
        sharedMaterial.SetTexture("_MetallicGlossMap", tex);
    } else {
        sharedMaterial.SetFloat("_Metallic", 0f);
        sharedMaterial.SetFloat("_Glossiness", 0f);
    }
}

private static void setNormalProps(Texture2D tex, Material sharedMaterial, SerializableColor fallbackColor = null, bool useFallbackColor = false) {
    sharedMaterial.EnableKeyword("_NORMALMAP");

    if (useFallbackColor && fallbackColor != null) {
        // Convert fallback color to Unity Color
        Color normalColor = fallbackColor.toUnityColor();

        // Create 1x1 normal texture
        Texture2D flatNormalTex = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
        flatNormalTex.SetPixel(0, 0, normalColor);
        flatNormalTex.Apply();

        // Mark as normal map type (optional, helps Unity treat it correctly)
#if UNITY_EDITOR
        flatNormalTex.name = "FallbackNormal";
        flatNormalTex.Apply();
#endif
        sharedMaterial.SetTexture("_BumpMap", flatNormalTex);
    } else {
        sharedMaterial.SetTexture("_BumpMap", tex);
    }
    
}

private static void setEmissionProps(Texture2D tex, Material sharedMaterial, SerializableColor fallbackColor = null, bool useFallbackColor = false) {
    sharedMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
    sharedMaterial.EnableKeyword("_EMISSION");
    // Debug.Log($"---- _EmissionColor {fallbackColor.toUnityColor()} useFallbackColor {useFallbackColor}");
    if (useFallbackColor && fallbackColor != null) {
        // Debug.Log($"---- _EmissionColor {fallbackColor.toUnityColor()}");
        sharedMaterial.SetColor("_EmissionColor", fallbackColor.toUnityColor());
        sharedMaterial.SetTexture("_EmissionMap", null);
    } else {
        sharedMaterial.SetTexture("_EmissionMap", tex);
        sharedMaterial.SetColor("_EmissionColor", Color.white);
    }
}


      // Acts as a constructor
    public void SetProperties(
        Material newMaterial = null,
        string albedoTexturePath = null,
        string metallicSmoothnessTexturePath = null,
        string normalTexturePath = null,
        string emissionTexturePath = null,
        ProceduralTextures rawTextures = null,
        float? textureReplaceEnergyThreshold = null,
        TexturesRGB texturesRGB = null,
        ResizeTextureSettings resizeTextureSettings = null
    ) {
        // shoulw happen only when fist creating this component
            this.sharedMaterial = newMaterial;
            this.albedoTexturePath = albedoTexturePath;
            this.metallicSmoothnessTexturePath = metallicSmoothnessTexturePath;
            this.normalTexturePath = normalTexturePath;
            this.emissionTexturePath = emissionTexturePath;
            this.rawTextures = rawTextures;

            this.textureReplaceEnergyThreshold = textureReplaceEnergyThreshold.GetValueOrDefault(float.MaxValue);
            this.texturesRGB = texturesRGB;
            this.resizeTextureSettings = resizeTextureSettings;
            // this.materialName = newMaterial.name;

            // Debug.Log($"albedoTexturePath { albedoTexturePath} m {metallicSmoothnessTexturePath} n {normalTexturePath} e {emissionTexturePath}");
    }



    public static void LoadTexturesToMaterial(
        Material sharedMaterial,
        string albedoTexturePath = null,
        string metallicSmoothnessTexturePath = null,
        string normalTexturePath = null,
        string emissionTexturePath = null,
        ProceduralTextures rawTextures = null,
        float? textureReplaceEnergyThreshold = null,
        TexturesRGB texturesRGB = null,
        ResizeTextureSettings resizeTextureSettings = null
    ) {
        if (sharedMaterial == null) {
            return;
        }

        bool useFallback = textureReplaceEnergyThreshold.HasValue && texturesRGB != null;
        resizeTextureSettings ??= new ResizeTextureSettings() {
                albedoTextureScale = 1.0f,
                metallicTextureScale = 1.0f,
                normalTextureScale = 1.0f,
                emissionTextureScale = 1.0f
            };
        Func<string, Texture2D> loadTexture; 
        (string albedo, string metallic, string normal, string emission) textureStrings;
        if (rawTextures == null) { 
            loadTexture = (x) => LoadTextureFromFile(x);
            textureStrings = (
                albedoTexturePath,
                metallicSmoothnessTexturePath,
                normalTexturePath,
                emissionTexturePath
            );
        }
        else {
            loadTexture = (x) => LoadTextureFromBase64(x);
            textureStrings = (
                rawTextures.albedoBase64JPG,
                rawTextures.metallicSmoothnessBase64JPG,
                rawTextures.normalBase64JPG,
                rawTextures.emissionBase64JPG
            );
        }

    
        setAlbedoProps(
            tex: ResizeTextureBicubic(loadTexture(textureStrings.albedo), resizeTextureSettings.albedoTextureScale),
            sharedMaterial: sharedMaterial,
            fallbackColor: texturesRGB?.albedoRGBA,
            useFallbackColor: useFallback && texturesRGB.albedoTextureEnergyNormalized <= textureReplaceEnergyThreshold.Value
        );

        setMetallicProps(
            tex: ResizeMetallicMap(loadTexture(textureStrings.metallic), resizeTextureSettings.metallicTextureScale),
            sharedMaterial: sharedMaterial,
            swapRGBAtoRRRB: true,
            fallbackColor: texturesRGB?.metallicSmoothnessRGBA,
            useFallbackColor: useFallback && texturesRGB.metallicSmoothnessTextureEnergyNormalized <= textureReplaceEnergyThreshold.Value
        );

        setNormalProps(
            tex: ResizeNormalMap(loadTexture(textureStrings.normal), resizeTextureSettings.normalTextureScale),
            sharedMaterial: sharedMaterial,
            fallbackColor: texturesRGB?.normalRGBA,
            useFallbackColor: useFallback && texturesRGB.normalTextureEnergyNormalized <= textureReplaceEnergyThreshold.Value
        );

        setEmissionProps(
            tex: ResizeTextureBicubic(loadTexture(textureStrings.emission), resizeTextureSettings.emissionTextureScale),
            sharedMaterial: sharedMaterial,
            fallbackColor: texturesRGB?.emissionRGBA,
            useFallbackColor: useFallback && texturesRGB.emissionTextureEnergyNormalized <= textureReplaceEnergyThreshold.Value
        );
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
            float? nullableTextureRepaceThreshold = textureReplaceEnergyThreshold == float.MaxValue? null : (float?)textureReplaceEnergyThreshold;
            LoadTexturesToMaterial(
                sharedMaterial,
                albedoTexturePath: albedoTexturePath,
                metallicSmoothnessTexturePath: metallicSmoothnessTexturePath,
                normalTexturePath: normalTexturePath,
                emissionTexturePath: emissionTexturePath,
                rawTextures: rawTextures,
                textureReplaceEnergyThreshold: nullableTextureRepaceThreshold,
                texturesRGB: texturesRGB,
                resizeTextureSettings: this.resizeTextureSettings
            );
        }
       
    }

    public void Awake() {
        var db = FindObjectOfType<ProceduralAssetDatabase>();
        if (rawTextures != null 
            && string.IsNullOrEmpty(rawTextures.albedoBase64JPG) 
            && string.IsNullOrEmpty(rawTextures.emissionBase64JPG) 
            && string.IsNullOrEmpty(rawTextures.metallicSmoothnessBase64JPG) 
            && string.IsNullOrEmpty(rawTextures.normalBase64JPG)) {
                rawTextures = null;
        }
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