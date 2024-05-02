using UnityEngine;
using UnityEditor;

public class LightmapUVSetter : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        ModelImporter modelImporter = (ModelImporter)assetImporter;
        modelImporter.generateSecondaryUV = true; // Enable generation of lightmap UVs
    }
}