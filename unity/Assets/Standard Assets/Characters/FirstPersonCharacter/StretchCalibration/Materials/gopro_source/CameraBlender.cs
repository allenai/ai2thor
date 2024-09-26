using UnityEngine;

[ExecuteInEditMode]
public class CameraBlender : MonoBehaviour
{
    public bool ControlFromComponent = true;
    public Camera camera1;
    public Camera camera2;
    public Camera blendedCamera;

    public RenderTexture renderTexture1;
    public RenderTexture renderTexture2;
    public Material blendMaterial;

    [Range(0, 100)]
    public float blendAmount = 50;

    void Start()
    {
        if (camera1 != null)
        {
            camera1.targetTexture = renderTexture1;
        }
        if (camera2 != null)
        {
            camera2.targetTexture = renderTexture2;
        }

        if (blendMaterial != null)
        {
            blendMaterial.SetTexture("_MainTex1", renderTexture1);
            blendMaterial.SetTexture("_MainTex2", renderTexture2);
        }
    }

    void Update()
    {
        if (blendMaterial != null && ControlFromComponent)
        {
            blendMaterial.SetFloat("_BlendAmount", blendAmount / 100f);
        }
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (blendMaterial != null)
        {
            Graphics.Blit(null, dest, blendMaterial);
        }
    }
}