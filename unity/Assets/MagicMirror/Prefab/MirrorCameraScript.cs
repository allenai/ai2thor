using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.VR;

public class MirrorCameraScript : MonoBehaviour
{
    public GameObject MirrorObject;
	public bool VRMode;

    private Renderer mirrorRenderer;
    private Material mirrorMaterial;

    private MirrorScript mirrorScript;
    private Camera cameraObject;
    private RenderTexture reflectionTexture;
    private Matrix4x4 reflectionMatrix;
    private int oldReflectionTextureSize;
    private static bool renderingMirror;

    private void Start()
    {
        mirrorScript = GetComponentInParent<MirrorScript>();
        cameraObject = GetComponent<Camera>();
        //cameraObject.enabled = true;

        if (mirrorScript.AddFlareLayer)
        {
            cameraObject.gameObject.AddComponent<FlareLayer>();
        }

        mirrorRenderer = MirrorObject.GetComponent<Renderer>();
        if (Application.isPlaying)
        {
            foreach (Material m in mirrorRenderer.sharedMaterials)
            {
                if (m.name == "MirrorMaterial")
                {
                    mirrorRenderer.sharedMaterial = m;
                    break;
                }
            }
        }
        mirrorMaterial = mirrorRenderer.sharedMaterial;

        CreateRenderTexture();
    }

    private void CreateRenderTexture()
    {
        if (reflectionTexture == null || oldReflectionTextureSize != mirrorScript.TextureSize)
        {
            if (reflectionTexture)
            {
                DestroyImmediate(reflectionTexture);
            }
            reflectionTexture = new RenderTexture(mirrorScript.TextureSize, mirrorScript.TextureSize, 16);
            reflectionTexture.filterMode = FilterMode.Bilinear;
            reflectionTexture.antiAliasing = 1;
            reflectionTexture.name = "MirrorRenderTexture_" + GetInstanceID();
            reflectionTexture.hideFlags = HideFlags.HideAndDontSave;
            reflectionTexture.autoGenerateMips = false;
            reflectionTexture.wrapMode = TextureWrapMode.Clamp;
            mirrorMaterial.SetTexture("_MainTex", reflectionTexture);
            oldReflectionTextureSize = mirrorScript.TextureSize;
        }

        if (cameraObject.targetTexture != reflectionTexture)
        {
            cameraObject.targetTexture = reflectionTexture;
        }
    }

    private void Update()
    {
		if (VRMode && Camera.current == Camera.main) {
			return;
		}
        CreateRenderTexture();
    }

    private void UpdateCameraProperties(Camera src, Camera dest)
    {
        dest.clearFlags = src.clearFlags;
        dest.backgroundColor = src.backgroundColor;
        if (src.clearFlags == CameraClearFlags.Skybox)
        {
            Skybox sky = src.GetComponent<Skybox>();
            Skybox mysky = dest.GetComponent<Skybox>();
            if (!sky || !sky.material)
            {
                mysky.enabled = false;
            }
            else
            {
                mysky.enabled = true;
                mysky.material = sky.material;
            }
        }

        dest.orthographic = src.orthographic;
        dest.orthographicSize = src.orthographicSize;
        if (mirrorScript.AspectRatio > 0.0f)
        {
            dest.aspect = mirrorScript.AspectRatio;
        }
        else
        {
            dest.aspect = src.aspect;
        }
		dest.renderingPath = src.renderingPath;
    }

    internal void RenderMirror()
    {
        Camera cameraLookingAtThisMirror;

        // bail if we don't have a camera or renderer
		if (renderingMirror || !enabled || (cameraLookingAtThisMirror = Camera.current) == null ||
            mirrorRenderer == null || mirrorMaterial == null || !mirrorRenderer.enabled)
        {
            return;
        }

        renderingMirror = true;

        int oldPixelLightCount = QualitySettings.pixelLightCount;
        if (QualitySettings.pixelLightCount != mirrorScript.MaximumPerPixelLights)
        {
            QualitySettings.pixelLightCount = mirrorScript.MaximumPerPixelLights;
        }

        try
        {
            UpdateCameraProperties(cameraLookingAtThisMirror, cameraObject);

            if (mirrorScript.MirrorRecursion)
            {
                mirrorMaterial.EnableKeyword("MIRROR_RECURSION");
                cameraObject.ResetWorldToCameraMatrix();
                cameraObject.ResetProjectionMatrix();
                cameraObject.projectionMatrix = cameraObject.projectionMatrix * Matrix4x4.Scale(new Vector3(-1, 1, 1));
				cameraObject.cullingMask = ~(1 << 4) & mirrorScript.ReflectLayers.value;
                GL.invertCulling = true;
                cameraObject.Render();
                GL.invertCulling = false;
            }
            else
            {
                mirrorMaterial.DisableKeyword("MIRROR_RECURSION");
                Vector3 pos = transform.position;
                Vector3 normal = (mirrorScript.NormalIsForward ? transform.forward : transform.up);

                // Reflect camera around reflection plane
                float d = -Vector3.Dot(normal, pos) - mirrorScript.ClipPlaneOffset;
                Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);
                CalculateReflectionMatrix(ref reflectionPlane);
                Vector3 oldpos = cameraObject.transform.position;
                float oldclip = cameraObject.farClipPlane;
                Vector3 newpos = reflectionMatrix.MultiplyPoint(oldpos);

				Matrix4x4 worldToCameraMatrix = cameraLookingAtThisMirror.worldToCameraMatrix;

				if (VRMode)
                {
					if(cameraLookingAtThisMirror.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left)
                    {
						worldToCameraMatrix[12] += 0.011f;
					}
					else if (cameraLookingAtThisMirror.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right)
                    {
						worldToCameraMatrix[12] -= 0.011f;
					}
				}

				worldToCameraMatrix *= reflectionMatrix;
				cameraObject.worldToCameraMatrix = worldToCameraMatrix;

               	// Clip out background
               	Vector4 clipPlane = CameraSpacePlane(ref worldToCameraMatrix, ref pos, ref normal, 1.0f);
            	cameraObject.projectionMatrix = cameraLookingAtThisMirror.CalculateObliqueMatrix(clipPlane);
               	GL.invertCulling = true;
               	cameraObject.transform.position = newpos;
               	cameraObject.farClipPlane = mirrorScript.FarClipPlane;
				cameraObject.cullingMask = ~(1 << 4) & mirrorScript.ReflectLayers.value;
               	cameraObject.Render();
               	cameraObject.transform.position = oldpos;
               	cameraObject.farClipPlane = oldclip;
                GL.invertCulling = false;


            }
        }
        finally
        {
            renderingMirror = false;
            if (QualitySettings.pixelLightCount != oldPixelLightCount)
            {
                QualitySettings.pixelLightCount = oldPixelLightCount;
            }
        }
    }

    // Cleanup all the objects we possibly have created
    private void OnDisable()
    {
        if (reflectionTexture)
        {
            DestroyImmediate(reflectionTexture);
            reflectionTexture = null;
        }
    }

    private Vector4 CameraSpacePlane(ref Matrix4x4 worldToCameraMatrix, ref Vector3 pos, ref Vector3 normal, float sideSign)
    {
        Vector3 offsetPos = pos + normal * mirrorScript.ClipPlaneOffset;
        Vector3 cpos = worldToCameraMatrix.MultiplyPoint(offsetPos);
        Vector3 cnormal = worldToCameraMatrix.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
    }

    private void CalculateReflectionMatrix(ref Vector4 plane)
    {
        // Calculates reflection matrix around the given plane

        reflectionMatrix.m00 = (1F - 2F * plane[0] * plane[0]);
        reflectionMatrix.m01 = (-2F * plane[0] * plane[1]);
        reflectionMatrix.m02 = (-2F * plane[0] * plane[2]);
        reflectionMatrix.m03 = (-2F * plane[3] * plane[0]);

        reflectionMatrix.m10 = (-2F * plane[1] * plane[0]);
        reflectionMatrix.m11 = (1F - 2F * plane[1] * plane[1]);
        reflectionMatrix.m12 = (-2F * plane[1] * plane[2]);
        reflectionMatrix.m13 = (-2F * plane[3] * plane[1]);

        reflectionMatrix.m20 = (-2F * plane[2] * plane[0]);
        reflectionMatrix.m21 = (-2F * plane[2] * plane[1]);
        reflectionMatrix.m22 = (1F - 2F * plane[2] * plane[2]);
        reflectionMatrix.m23 = (-2F * plane[3] * plane[2]);

        reflectionMatrix.m30 = 0F;
        reflectionMatrix.m31 = 0F;
        reflectionMatrix.m32 = 0F;
        reflectionMatrix.m33 = 1F;
    }

    private static void CalculateObliqueMatrix(ref Matrix4x4 projection, ref Vector4 clipPlane)
    {
        Vector4 q = projection.inverse * new Vector4
        (
            Sign(clipPlane.x),
            Sign(clipPlane.y),
            1.0f,
            1.0f
        );
        Vector4 c = clipPlane * (2.0F / (Vector4.Dot(clipPlane, q)));
        // third row = clip plane - fourth row
        projection[2] = c.x - projection[3];
        projection[6] = c.y - projection[7];
        projection[10] = c.z - projection[11];
        projection[14] = c.w - projection[15];
    }

    private static float Sign(float a)
    {
        if (a > 0.0f) return 1.0f;
        if (a < 0.0f) return -1.0f;
        return 0.0f;
    }
}
