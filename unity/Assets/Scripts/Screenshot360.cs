using System.IO;
using UnityEngine;

public class Screenshot360 : MonoBehaviour
{
    public int imageWidth = 4096;
    public bool saveAsJPEG = true;
    public string savePath;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            byte[] bytes = I360Render.Capture(imageWidth, saveAsJPEG);
            if (bytes != null)
            {
                string path = Path.Combine(savePath, "360render" + (saveAsJPEG ? ".jpeg" : ".png"));
                File.WriteAllBytes(path, bytes);
                Debug.Log("360 render saved to " + path);
            }
        }
    }
}