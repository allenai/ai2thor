 // Code from https://answers.unity.com/questions/1004666/change-material-rendering-mode-in-runtime.html
 
 using UnityEngine;
 
 public static class StandardShaderUtils
 {
     public enum BlendMode
     {
         Opaque,
         Cutout,
         Fade,
         Transparent
     }
 
     public static void ChangeRenderMode(Material standardShaderMaterial, BlendMode blendMode)
     {
         switch (blendMode)
         {
             case BlendMode.Opaque:
                 standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                 standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                 standardShaderMaterial.SetInt("_ZWrite", 1);
                 standardShaderMaterial.DisableKeyword("_ALPHATEST_ON");
                 standardShaderMaterial.DisableKeyword("_ALPHABLEND_ON");
                 standardShaderMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                 standardShaderMaterial.renderQueue = -1;
                 break;
             case BlendMode.Cutout:
                 standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                 standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                 standardShaderMaterial.SetInt("_ZWrite", 1);
                 standardShaderMaterial.EnableKeyword("_ALPHATEST_ON");
                 standardShaderMaterial.DisableKeyword("_ALPHABLEND_ON");
                 standardShaderMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                 standardShaderMaterial.renderQueue = 2450;
                 break;
             case BlendMode.Fade:
                 standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                 standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                 standardShaderMaterial.SetInt("_ZWrite", 0);
                 standardShaderMaterial.DisableKeyword("_ALPHATEST_ON");
                 standardShaderMaterial.EnableKeyword("_ALPHABLEND_ON");
                 standardShaderMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                 standardShaderMaterial.renderQueue = 3000;
                 break;
             case BlendMode.Transparent:
                 standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                 standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                 standardShaderMaterial.SetInt("_ZWrite", 0);
                 standardShaderMaterial.DisableKeyword("_ALPHATEST_ON");
                 standardShaderMaterial.DisableKeyword("_ALPHABLEND_ON");
                 standardShaderMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                 standardShaderMaterial.renderQueue = 3000;
                 break;
         }
 
     }
 }
