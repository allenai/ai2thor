using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TopDownPlotter : MonoBehaviour
{
    public Camera topDownCamera;
    int countForFileName = 0;
    private RenderTexture rt;
    private MCSMain main;

    // Start is called before the first frame update
    void Start()
    {
        rt = new RenderTexture(1024, 1024, 24);
        topDownCamera.targetTexture = rt;
        main = GameObject.Find("MCS").GetComponent<MCSMain>();
    }

    public void TakeScreenshot(string path)
    {
        //Reposition camera according to scene size
        if(main.currentScene.roomDimensions.x > main.currentScene.roomDimensions.z)
        {
            topDownCamera.orthographicSize = (main.currentScene.roomDimensions.x/2) + 1;
        }
        else
        {
            topDownCamera.orthographicSize = (main.currentScene.roomDimensions.z/2) + 1;
        }

        topDownCamera.gameObject.transform.position = new Vector3(topDownCamera.gameObject.transform.position.x, main.currentScene.roomDimensions.y + 10, topDownCamera.gameObject.transform.position.z);

        //Set screenshot location for standalone application run through Python
        String screenshotsDirectory = path;

        //Reassign default screenshot location for in-editor testing when topDownImagePath field is not set on the MCSController        
        #if UNITY_EDITOR
            screenshotsDirectory = Application.dataPath + "/InEditorScreenshots/";
        #endif

        StartCoroutine(waitOneFrameToTakeScreenshot(screenshotsDirectory));
    }

    IEnumerator waitOneFrameToTakeScreenshot(string screenshotsDirectory)
    {
        yield return new WaitForEndOfFrame();

        //Take screenshot
        Texture2D textureFromRenderTexture = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
        RenderTexture.active = rt;
        textureFromRenderTexture.ReadPixels(new Rect(0,0, rt.width, rt.height), 0, 0);
        textureFromRenderTexture.Apply();
        Byte[] bytesToEncode = textureFromRenderTexture.EncodeToPNG();
        Directory.CreateDirectory(screenshotsDirectory);
        System.IO.File.WriteAllBytes(screenshotsDirectory + main.currentScene.name + "_topdown_" + countForFileName + ".png", bytesToEncode);
        RenderTexture.active = null;
        countForFileName++;
    }


    // The following coroutines are left here in case we want to take screenshots on certain frame intervals in the future.
    public static IEnumerator Frames(int framesToWait)
    {
        if (framesToWait < 1)
        {
            throw new ArgumentOutOfRangeException("Cannot wait for less that 1 frame");
        }

        while (framesToWait >= 1)
        {
            framesToWait--;
            yield return null;
        }
    }

    // If this is utilizied in another script, add a check for AgentManager.recordTopDown before starting the coroutine. 
    IEnumerator TakeScreenshotsOnInterval(int interval)
    {
        while(true)
        {
            //First wait X frames
            yield return StartCoroutine(Frames(interval));
            //Then wait until the end of the frame (required to not throw errors)
            yield return new WaitForEndOfFrame();

            //Take screenshot
            Texture2D textureFromRenderTexture = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
            RenderTexture.active = rt;
            textureFromRenderTexture.ReadPixels(new Rect(0,0, rt.width, rt.height), 0, 0);
            textureFromRenderTexture.Apply();
            Byte[] bytesToEncode = textureFromRenderTexture.EncodeToPNG();
            String screenshotsDirectory = Application.dataPath + "/Screenshots/";
            Directory.CreateDirectory(screenshotsDirectory);
            System.IO.File.WriteAllBytes(screenshotsDirectory + countForFileName + ".png", bytesToEncode);
            RenderTexture.active = null;
            countForFileName++;
        }
    }
}