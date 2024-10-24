using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// // @TODO:
// // . support custom color wheels in optical flow via lookup textures
// // . support custom depth encoding
// // . support multiple overlay cameras - note:this can be shown in editor already by creating multiple GAME windows and assigning a different display number to each
// // . tests
// // . better example scene(s)

// // @KNOWN ISSUES
// // . Motion Vectors can produce incorrect results in Unity 5.5.f3 when
// //      1) during the first rendering frame
// //      2) rendering several cameras with different aspect ratios - vectors do stretch to the sides of the screen

namespace Thor.Rendering {
    public class RenderCapture {
        
    }
}

namespace Thor.Rendering {

    public enum ReplacelementMode {
        ObjectId = 0,
        CatergoryId = 1,
        DepthCompressed = 2,
        DepthMultichannel = 3,
        Normals = 4,
        Flow = 5,
    };
    
    public interface ICapturePass {
        public RenderTargetIdentifier GetRenderTarget();
        public void AddToCommandBuffer(CommandBuffer commandBuffer);
        public string GetName();
    }


    public class RenderToTexture : ICapturePass {

        public static void SetupCameraWithPostShader(
            Camera cam,
            Material material,
            // Material screenCopyMaterial,
            DepthTextureMode depthTextureMode = DepthTextureMode.None
            ) {
            var cb = new CommandBuffer();

            int screenCopyID = Shader.PropertyToID("_MainTex");
            cb.GetTemporaryRT(screenCopyID, -1, -1, 0, FilterMode.Bilinear);
            cb.Blit(BuiltinRenderTextureType.CurrentActive, screenCopyID);
            cb.Blit(screenCopyID, BuiltinRenderTextureType.CameraTarget, material);
            
            cb.ReleaseTemporaryRT(screenCopyID);

            cam.AddCommandBuffer(CameraEvent.BeforeImageEffects, cb);
            cam.depthTextureMode = depthTextureMode;
        }


        public string name;
        public bool supportsAntialiasing;

        public int antiAliasLevel = 1;
        public bool needsRescale;
        public Camera camera;

        public string shaderName;

        private Texture2D tex;
        private RenderTexture renderTexture;
        public Material material;
        protected Shader shader;

        protected CommandBuffer cb;

        public RenderToTexture(string name, Camera camera, int antiAliasLevel = 1, string shaderName = null) {
            this.camera = camera;
            this.antiAliasLevel = antiAliasLevel;
            this.shaderName = shaderName;
            this.name = name;
        }

        public virtual RenderTargetIdentifier GetRenderTarget() {
            return new RenderTargetIdentifier(renderTexture);
        }

        public virtual string GetName() {
            return name;
        }
       
        public virtual void AddToCommandBuffer(CommandBuffer commandBuffer) {
            
            // int screenCopyID = Shader.PropertyToID("_MainTex");
            // cb.GetTemporaryRT(screenCopyID, -1, -1, 0, FilterMode.Bilinear);
            // cb.Blit(BuiltinRenderTextureType.CurrentActive, screenCopyID);
            // if (material != null) {
            //     cb.Blit(screenCopyID, BuiltinRenderTextureType.CameraTarget, material);
            // }
            // else {
            //     cb.Blit(screenCopyID, BuiltinRenderTextureType.CameraTarget);
            // }
            
            // cb.ReleaseTemporaryRT(screenCopyID);

            // this.camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, cb);
            // this.camera.depthTextureMode = DepthTextureMode.Depth;

            // int screenCopyID = Shader.PropertyToID("_MainTex");
            // cb.GetTemporaryRT(screenCopyID, -1, -1, 0, FilterMode.Bilinear);
            // cb.Blit(BuiltinRenderTextureType.CurrentActive, screenCopyID);
            // if (material != null) {
            //     cb.Blit(screenCopyID, BuiltinRenderTextureType.CameraTarget, material);
            // }
            // else {
            //     cb.Blit(screenCopyID, BuiltinRenderTextureType.CameraTarget);
            // }
            
            // cb.ReleaseTemporaryRT(screenCopyID);

            // this.camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, cb);
            // this.camera.depthTextureMode = DepthTextureMode.Depth;
        }


    private RenderTexture CreateRenderTexture(int width, int height) {

        // for cloud rendering GraphicsFormat.R8G8B8A8_UNorm

        // Used for main buffer
        // RenderTexture rt = new RenderTexture(
        //     width: width,
        //     height: height,
        //     depth: 0,
        //     RenderTextureFormat.ARGB32
        // );

        var rt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        rt.antiAliasing = antiAliasLevel;
        if (rt.Create()) {
            Debug.Log("Created Render Texture with width= " + width + " height=" + height);
            return rt;
        } else {
            // throw exception ?
            Debug.LogError($"Could Not Create a RenderTexture for RenderCapture '{this.name}'");
            return null;
        }
    }

        public virtual void OnInitialize(Camera mainCamera) {
            // if (!shader && !string.IsNullOrEmpty(shaderName)) {
            //     shader = Shader.Find(shaderName);
            // }
            
            // if (!material || material.shader != shader) {
            //     material = new Material(material);
            // }
            // OnInitialize();
            // OnCameraChange(camera);
        }

        public virtual void Initialize(Camera mainCamera) {
            if (!shader && !string.IsNullOrEmpty(shaderName)) {
                shader = Shader.Find(shaderName);
            }
            
            if (!material || material.shader != shader) {
                material = new Material(material);
            }
            
            OnInitialize(mainCamera);
            OnCameraChange(mainCamera);
        }

        

        public virtual void OnCameraChange(Camera mainCamera) {
            if (tex != null) {
                UnityEngine.Object.Destroy(tex);
                tex = null;
            }

            this.camera.RemoveAllCommandBuffers();

            // copy all "main" camera parameters into capturing camera

            // Code for debugging camera
            if (this.camera != mainCamera) {
                this.camera.CopyFrom(mainCamera);

                // Unsure about this
                this.camera.renderingPath = RenderingPath.Forward;
                this.camera.cullingMask = -1;
                this.camera.depth = 0;
            }
            

            // make sure the capturing camera is set to Forward rendering (main camera uses Deffered now)

            // this.camera.renderingPath = RenderingPath.Forward;

            // make sure capturing camera renders all layers (value copied from Main camera excludes PlaceableSurfaces layer, which needs to be rendered on this camera)
            
            // this.camera.cullingMask = -1;

            // this.camera.depth = 0; // This ensures the new camera does not get rendered on screen
        
        // TODO: add rescaling here
        if (renderTexture != null && renderTexture.IsCreated())
        {
            renderTexture.Release();
        }

        // renderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);

        renderTexture = CreateRenderTexture(Screen.width, Screen.height);

        this.camera.targetTexture = renderTexture;
        
        if (cb != null) {
            cb.Clear();
        }
        else {
            cb = new CommandBuffer();
        }
       
        this.AddToCommandBuffer(cb);
       

        // TODO: debug code for editor
// #if UNITY_EDITOR
//         for (int i = 0; i < capturePasses.Length; i++) {
//             // Debug.Log("Setting camera " + capturePasses[i].camera.gameObject.name + " to display " + i);
//             capturePasses[i].camera.targetDisplay = i;
//         }
// #endif

        /*
        SetupCameraWithReplacementShader(capturePasses[6].camera, positionShader);
        */
    }





// //     private Camera CreateHiddenCamera(string name) {
// //         var go = new GameObject(name, typeof(Camera));
// // #if !UNITY_EDITOR // Useful to be able to see these cameras in the editor
// //         go.hideFlags = HideFlags.HideAndDontSave;
// // #endif
// //         go.transform.parent = transform;

// //         // this is a check for if the image synth is being added to a ThirdPartyCamera, which doesn't have a FirstPersonCharacterCull component
// //         // Note: Check that all image synthesis works with third party cameras, as the image synth assumes that it is taking default settings
// //         // from the Agent's camera, and a ThirdPartyCamera does not have the same defaults, which may cause some errors
// //         if (go.transform.parent.GetComponent<FirstPersonCharacterCull>())
// //         // add the FirstPersonCharacterCull so this camera's agent is not rendered- other agents when multi agent is enabled should still be rendered
// //         {
// //             go.AddComponent<FirstPersonCharacterCull>(
// //                 go.transform.parent.GetComponent<FirstPersonCharacterCull>()
// //             );
// //         }

// //         var newCamera = go.GetComponent<Camera>();
// //         newCamera.cullingMask = 1; // render everything, including PlaceableSurfaces
// //         return newCamera;
// //     }

// //     private void UpdateDebugCamera(Camera mainCamera) {

// //     }

       

}


public class ReplacementShaderCapture: RenderToTexture {

    private readonly Transform cameraParent;
    private ReplacelementMode mode;
    public ReplacementShaderCapture(string name, Transform cameraParent, ReplacelementMode replacementMode, int antiAliasLevel = 1, string shaderName = null) : base(name, null, antiAliasLevel, shaderName) {
        this.cameraParent = cameraParent;
        this.mode = replacementMode;
    }

    public override void OnInitialize(Camera mainCamera) {
        this.camera = CreateHiddenCamera(cameraParent, this.name);
    }


    public static Camera CreateHiddenCamera(Transform parent, string name) {
        var go = new GameObject(name, typeof(Camera));
#if !UNITY_EDITOR // Useful to be able to see these cameras in the editor
        go.hideFlags = HideFlags.HideAndDontSave;
#endif
        go.transform.parent = parent;

        // this is a check for if the image synth is being added to a ThirdPartyCamera, which doesn't have a FirstPersonCharacterCull component
        // Note: Check that all image synthesis works with third party cameras, as the image synth assumes that it is taking default settings
        // from the Agent's camera, and a ThirdPartyCamera does not have the same defaults, which may cause some errors
        if (go.transform.parent.GetComponent<FirstPersonCharacterCull>())
        // add the FirstPersonCharacterCull so this camera's agent is not rendered- other agents when multi agent is enabled should still be rendered
        {
            go.AddComponent<FirstPersonCharacterCull>(
                go.transform.parent.GetComponent<FirstPersonCharacterCull>()
            );
        }

        var newCamera = go.GetComponent<Camera>();
        newCamera.cullingMask = 1; // render everything, including PlaceableSurfaces
        return newCamera;
    }

    public override void AddToCommandBuffer(CommandBuffer commandBuffer) {
            var cb = new CommandBuffer();
            cb.SetGlobalFloat("_OutputMode", (int)mode); // @TODO: CommandBuffer is missing SetGlobalInt() method
            this.camera.renderingPath = RenderingPath.Forward;
            this.camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, cb);
            this.camera.AddCommandBuffer(CameraEvent.BeforeFinalPass, cb);
            this.camera.SetReplacementShader(shader, "");
            this.camera.backgroundColor = Color.blue;
            this.camera.clearFlags = CameraClearFlags.SolidColor;
        }
    

}

    public class MultiCapture : RenderToTexture {

        IEnumerable<RenderToTexture> passes;
        Dictionary<string, RenderToTexture> passDict;
        public MultiCapture(string name, Camera camera, IEnumerable<RenderToTexture> passes, int antiAliasLevel = 1, string shaderName = null) : base(name, camera, antiAliasLevel, shaderName) {
            this.passDict = passes.ToDictionary(p => p.GetName());
        }

        public override void AddToCommandBuffer(CommandBuffer commandBuffer) {

            // camera.renderTex
            // ??
            // Texture texture = this.GetRenderTarget() as Texture;
            // camera.targetTexture = texture;

            // int screenCopyID = Shader.PropertyToID("_MainTex");
            // commandBuffer.SetRenderTarget(this.GetRenderTarget());

            // commandBuffer.GetTemporaryRT(screenCopyID, -1, -1, 0, FilterMode.Bilinear);
            // commandBuffer.Blit(BuiltinRenderTextureType.CurrentActive, screenCopyID);
            // foreach (var pass in passes) {
            //     commandBuffer.Blit(screenCopyID, pass.GetRenderTarget(), material);
            // }
            
            // commandBuffer.ReleaseTemporaryRT(screenCopyID);
            foreach (var pass in this.passDict.Values) {
                commandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, pass.GetRenderTarget(), pass.material);
            }
            

            this.camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, commandBuffer);
            this.camera.depthTextureMode = DepthTextureMode.Depth;
        }

        public override void Initialize(Camera mainCamera) {
            foreach (var pair in this.passDict) {
                pair.Value.Initialize(mainCamera);
            }
            base.Initialize(mainCamera);
        }

        public override void OnCameraChange(Camera mainCamera) {
            foreach (var pair in this.passDict) {
                pair.Value.OnCameraChange(mainCamera);
            }
            base.OnCameraChange(mainCamera);
        }
        // TODO: if order of captures matters?
        // public void UpdateCapturePasses(IEnumerable<ICapturePass> passes) {
            
        // }



        public void AddCapturePass(RenderToTexture pass) {
            this.passDict[pass.GetName()] = pass;
            // if (!this.passDict.ContainsKey(pass.name))
            //     this.passDict.Add(pass)
        }

        
    }

}


// public class ImageD : MonoBehaviour {
    
// }