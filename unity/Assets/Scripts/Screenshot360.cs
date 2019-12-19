using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Screenshot360 : MonoBehaviour
{
    #if UNITY_EDITOR
    [MenuItem("GameObject/Take 360 Screenshot")]
    #endif
    private static void Generate360Screenshot()
    {
        int imageWidth = 4096;
        bool saveAsJPEG = true;
        bool newFileName = false;
        int currentCount = 1;
        string path;

        byte[] bytes = I360Render.Capture(imageWidth, saveAsJPEG);
            
            if (bytes != null)
            {
                while (!newFileName)
                {
                    if (File.Exists("Assets/360Photos/360Render_" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "_" + currentCount + (saveAsJPEG ? ".jpeg" : ".png")))
                    {
                        currentCount++;
                    }

                    else
                    {
                        path = Path.Combine("Assets/360Photos", "360Render_" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "_" + currentCount + (saveAsJPEG ? ".jpeg" : ".png"));
                        File.WriteAllBytes(path, bytes);
                        Debug.Log("360 render saved to " + path);
                        newFileName = true;
                    }
                }
            }
        }
}
