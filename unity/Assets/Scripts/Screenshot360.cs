using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Screenshot360 : MonoBehaviour
{
    public int imageWidth = 4096;
    public bool saveAsJPEG = true;
    int currentCount;
    bool newFileName;
    string path;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            byte[] bytes = I360Render.Capture(imageWidth, saveAsJPEG);
            newFileName = false;
            currentCount = 1;

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
                        newFileName = true;
                    }
                }
                    File.WriteAllBytes(path, bytes);
                    Debug.Log("360 render saved to " + path);
                    currentCount++;
            }
        }
    }
}