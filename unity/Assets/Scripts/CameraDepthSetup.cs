using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteInEditMode]

public class CameraDepthSetup : MonoBehaviour
{
    void Start()
    {
        Camera.main.depthTextureMode = DepthTextureMode.Depth;
        // GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
        Debug.Log("CAMERA DEPTH TEXTURE SET TO: " + Camera.main.depthTextureMode);
    }
}
