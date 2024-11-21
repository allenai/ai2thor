using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Security.Cryptography;

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

    // public class PassName {
    //     const string Image = "_img",
    //     Depth = "_depth"
    // }

    public class CaptureConfig {
        public string name;

        public int antiAliasLevel = 1;

        public string shaderName;

        public ReplacelementMode replacementMode;

        public int? toDisplay = 0;

        public bool cloudRendering;
        
        
    }
    
    public interface ICapturePass {
        public RenderTexture GetRenderTexture();
        public void AddToCommandBuffer(CommandBuffer commandBuffer);
        public string GetName();
        public void OnInitialize(Camera mainCamera);
        public void OnCameraChange(Camera mainCamera);
        public bool IsInitialized();
        public byte[] GetBytes(bool jpg = false);
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
        protected bool cloudRendering;

        protected int? toDisplayId;

        private bool initialized;
        
        private TextureFormat readTextureFormat;

        

        // private Texture2D readTexture;

        public bool IsInitialized() {
            return initialized;
        }

        public RenderToTexture(CaptureConfig config, Camera camera) {
            this.camera = camera;
            this.antiAliasLevel = config.antiAliasLevel;
            this.shaderName = config.shaderName;
            this.name = config.name;
            this.cloudRendering = config.cloudRendering;
            this.toDisplayId = config.toDisplay;

            // TODO. if config.toDisplay is present then render to display buffer and copy to render texture 
            // for debugging purposes
        }

        public virtual RenderTexture GetRenderTexture() {
            return renderTexture;
        }

        public virtual string GetName() {
            return name;
        }
       
        public virtual void AddToCommandBuffer(CommandBuffer commandBuffer) {
            // Debug.Log("-------- AddToCommandBuffer " + name);
            
            // int screenCopyID = Shader.PropertyToID("_MainTex");
            // cb.GetTemporaryRT(screenCopyID, -1, -1, 0, FilterMode.Bilinear);
            // cb.Blit(BuiltinRenderTextureType.CurrentActive, screenCopyID);
            // if (material != null) {
            //     cb.Blit(screenCopyID, BuiltinRenderTextureType.CameraTarget, material);
            // }
            // else {
            //     cb.Blit(screenCopyID, BuiltinRenderTextureType.CameraTarget);
            // }
            //  cb.SetRenderTarget(RenderTargetIdentifier);
             // cb.Blit(this.GetRenderTarget(), RenderTexture.active);

            // Debug.Log("------- command buffer set for " + this.name);
            // cb.SetRenderTarget(new RenderTargetIdentifier(null as Texture));
            //this.camera.targetTexture = null;

            //  cb.Blit(this.GetRenderTarget(), BuiltinRenderTextureType.CurrentActive);
            
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

            // this.camera.AddCommandBuffer(CameraEvent.AfterImageEffects, cb);
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

        // TODO: add rescaling here
        if (this.renderTexture != null && this.renderTexture.IsCreated())
        {
            this.renderTexture.Release();
        }
        
       
        RenderTexture rt = null;
       
         
        if (cloudRendering) {
            // Why 0 for depth here ?
            rt = new RenderTexture(Screen.width, Screen.height, 0, GraphicsFormat.R8G8B8A8_UNorm);
            // TODO: if 0 then RGB24? if not RGB32?
            readTextureFormat = TextureFormat.RGBA32;
            
        //     RenderTexture rt = new RenderTexture(
        //     width: width,
        //     height: height,
        //     depth: 0,
        //     GraphicsFormat.R8G8B8A8_UNorm
        // );
        }
        else {
            rt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            readTextureFormat = TextureFormat.RGBA32; 
        }

         if (this.tex == null) {
            // Debug.Log($"------------ texture format  {Enum.GetName(typeof(TextureFormat), readTextureFormat)} val {readTextureFormat}");
            tex = new Texture2D(Screen.width, Screen.height, readTextureFormat, false);
        }
        rt.antiAliasing = antiAliasLevel;
        if (rt.Create()) {
            // Debug.Log("Created Render Texture with width= " + width + " height=" + height);
            return rt;
        } else {
            // throw exception ?
            Debug.LogError($"Could Not Create a RenderTexture for RenderCapture '{this.name}'");
            return null;
        }
    }

        public virtual void OnInitialize(Camera mainCamera) {

            // Debug.Log("++++ initialize called for " + this.name);
            if (!shader && !string.IsNullOrEmpty(shaderName)) {

                // Debug.Log("---- loading shader " + shaderName);
                shader = Shader.Find(shaderName);
            }
            
            if (shader && (!material || material.shader != shader)) {
                material = new Material(shader);
            }
            
            //OnInitialize(mainCamera);
            // OnCameraChange(mainCamera);
            initialized = true;
        }

        // public virtual void Initialize(Camera mainCamera) {
        //     if (!shader && !string.IsNullOrEmpty(shaderName)) {
        //         shader = Shader.Find(shaderName);
        //     }
            
        //     if (!material || material.shader != shader) {
        //         material = new Material(material);
        //     }
            
        //     OnInitialize(mainCamera);
        //     OnCameraChange(mainCamera);
        //     initialized = true;
        // }

        

        public virtual void OnCameraChange(Camera mainCamera) {
            Debug.Log($"-------OnCameraChange  {name} on object {mainCamera.gameObject.name}");
            if (tex != null) {
                UnityEngine.Object.Destroy(tex);
                tex = null;
            }
            // if (cb != null) {
            //     this.camera.RemoveCommandBuffer(CameraEvent.AfterImageEffects, cb);
            // }
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
        
        

        // renderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);

        this.renderTexture = CreateRenderTexture(Screen.width, Screen.height);
        // if (this.name != "_img") {
        // this.camera.targetTexture = this.renderTexture;
        // }
        this.camera.targetDisplay = this.toDisplayId.GetValueOrDefault();

        // If set to render to display don't set render texture because display buffer is only written to if targetTexture is null
        if (!this.toDisplayId.HasValue) {
            this.camera.targetTexture = this.renderTexture;          
        }
        
        
        if (cb != null) {
            cb.Clear();
        }
        else {
            cb = new CommandBuffer();
        }
        // Debug.Log("------- Before command buffer " + name);
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

    public virtual byte[] GetBytes(
        bool jpg = false
    ) {

        var renderTexture = this.GetRenderTexture();
        var prevActiveRT = RenderTexture.active;
        RenderTexture.active = renderTexture;
        
        // float startTime = Time.realtimeSinceStartup;

        tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        
        
        // startTime = Time.realtimeSinceStartup;

        // encode texture into PNG/JPG
        byte[] bytes;
        if (jpg) {
            bytes = tex.EncodeToJPG();
        } else {
            bytes = tex.GetRawTextureData();
        }
        RenderTexture.active = prevActiveRT;
        return bytes;
    }


}


public class ReplacementShaderCapture: RenderToTexture {

    private readonly Transform cameraParent;
    private ReplacelementMode mode;
    public ReplacementShaderCapture(CaptureConfig config, Transform cameraParent) : base(config, null) {
        this.cameraParent = cameraParent;
        this.mode = config.replacementMode;
    }

    public override void OnInitialize(Camera mainCamera) {
        this.camera = CreateHiddenCamera(cameraParent, this.name);
        base.OnInitialize(mainCamera);
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
            // Debug.Log($"------ AddToCommandBuffer for {this.name}");
            this.camera.SetReplacementShader(shader, "");
            this.camera.backgroundColor = Color.blue;
            this.camera.clearFlags = CameraClearFlags.SolidColor;
            this.camera.targetTexture = this.GetRenderTexture();
        }
    

}

    public class MultiCapture : RenderToTexture {

        IEnumerable<RenderToTexture> passes;
        Dictionary<string, RenderToTexture> passDict;
        public MultiCapture(CaptureConfig config, Camera camera, IEnumerable<RenderToTexture> passes) : base(config, camera) {
            this.passDict = passes.ToDictionary(p => p.GetName(), p => p);
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
            // cb.SetRenderTarget(new RenderTargetIdentifier(null as Texture));
            //this.camera.targetTexture = null;

            // cb.Blit(this.GetRenderTarget(), BuiltinRenderTextureType.CurrentActive);
            // Debug.Log($"----------- Blit for multipass");

            // If rendering to display
            if (this.toDisplayId.HasValue) {
                // if it's not cloudrendering camera.targetTexture is null which means it's rendering to the display buffer
                // so then we need to copy the display buffer into render texture

                // for cloudrendering rbb is rendered directly into our render texture so no need to do this
                cb.Blit(BuiltinRenderTextureType.CurrentActive, this.GetRenderTexture());
            }

            // cb.SetRenderTarget(this.GetRenderTarget());
            
            // commandBuffer.ReleaseTemporaryRT(screenCopyID);
            foreach (var pass in this.passDict.Values) {
                commandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, pass.GetRenderTexture(), pass.material);
            }
            

            this.camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, commandBuffer);
            this.camera.depthTextureMode = DepthTextureMode.Depth;
            
        }

        public override void OnInitialize(Camera mainCamera) {
            foreach (var pair in this.passDict) {
                pair.Value.OnInitialize(mainCamera);
            }
            base.OnInitialize(mainCamera);
        }

        private bool done = false;

        public override void OnCameraChange(Camera mainCamera) {
            // Debug.Log($"----------- Multipass OnCameraChange for {this.passDict.Values.Select(x => x.GetName())}");
            foreach (var pair in this.passDict) {
                pair.Value.OnCameraChange(mainCamera);
            }
            base.OnCameraChange(mainCamera);
        }
        // TODO: if order of captures matters?
        // public void UpdateCapturePasses(IEnumerable<ICapturePass> passes) {
            
        // }



        public void AddUpdateCapturePass(RenderToTexture pass) {
            this.passDict[pass.GetName()] = pass;
            // if (!this.passDict.ContainsKey(pass.name))
            //     this.passDict.Add(pass)
        }

        
    }
    
    // public class DistortionCapture : RenderToTexture {
    //     public DistortionCapture(CaptureConfig config, Camera camera) : base(config, camera) {
    //     }


    // }

}





// public class ImageD : MonoBehaviour {
    
// }