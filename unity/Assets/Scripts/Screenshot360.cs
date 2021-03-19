using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Screenshot360 : MonoBehaviour {
    #if UNITY_EDITOR
        [UnityEditor.MenuItem("GameObject/Take 360 Screenshot")]
    #endif

    public static void Generate360Screenshot() {
        int imageWidth = 4096;
        bool saveAsJPEG = true;
        bool newFileName = false;
        int currentCount = 1;
        string path;

        byte[] bytes = I360Render.Capture(imageWidth, saveAsJPEG);
        if (bytes != null) {
            while (!newFileName) {
                if (File.Exists(genFilename(currentCount, "rgb"))) {
                    currentCount++;
                } else {
                    path = genFilename(currentCount, "rgb");
                    File.WriteAllBytes(path, bytes);
                    Debug.Log("360 render saved to " + path);
                    newFileName = true;
                }
            }
        }
    }

    #if UNITY_EDITOR
        [UnityEditor.MenuItem("GameObject/Take 360 Instance Segmentation Screenshot")]
    #endif

    public static void Generate360InstanceSegmentationScreenshot() {
        int imageWidth = 4096;
        bool saveAsJPEG = true;
        bool newFileName = false;
        int currentCount = 1;
        string path;
        var imageSynth = GameObject.FindObjectOfType<ImageSynthesis>();
        if (imageSynth != null && imageSynth.enabled) {

            byte[] bytes = I360Render.Capture(imageWidth, saveAsJPEG, imageSynth.GetCapturePassCamera("_id"));
            if (bytes != null) {
                while (!newFileName) {
                    if (File.Exists(genFilename(currentCount, "instance"))) {
                        currentCount++;
                    } else {
                        path = genFilename(currentCount, "instance");
                        File.WriteAllBytes(path, bytes);
                        Debug.Log("360 render saved to " + path);
                        newFileName = true;
                    }
                }
            }
        }
        else {
            Debug.LogError("Enable imagesynth");
        }
    }

    #if UNITY_EDITOR
        [UnityEditor.MenuItem("GameObject/Take 360 Class Segmentation Screenshot")]
    #endif

    public static void Generate360ClassSegmentationScreenshot() {
        int imageWidth = 4096;
        bool saveAsJPEG = true;
        bool newFileName = false;
        int currentCount = 1;
        string path;
        var imageSynth = GameObject.FindObjectOfType<ImageSynthesis>();
        if (imageSynth != null && imageSynth.enabled) {

            byte[] bytes = I360Render.Capture(imageWidth, saveAsJPEG, imageSynth.GetCapturePassCamera("_class"));
            if (bytes != null) {
                while (!newFileName) {
                    if (File.Exists(genFilename(currentCount, "class"))) {
                        currentCount++;
                    } else {
                        path = genFilename(currentCount, "class");
                        File.WriteAllBytes(path, bytes);
                        Debug.Log("360 render saved to " + path);
                        newFileName = true;
                    }
                }
            }
        }
        else {
            Debug.LogError("Enable imagesynth");
        }
    }

    private static string genFilename(int currentCount, string imageType = "rgb", string format = "jpeg", string path = "../360Photos") {
        return Path.Combine(path, $"360Render_{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}_{imageType}_{currentCount}.{format}");
    }
    
}
