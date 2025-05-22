using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Thor.Rendering;
using System.Linq;
using System;
using Unity.Rendering;
using MessagePack.Resolvers;
using UnityEngine.Rendering;
using System.Runtime.Remoting.Messaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thor.Attributes;

public class RenderingManager : MonoBehaviour {

    public Dictionary<string, ICapturePass> availablePasses {
        get;
        private set;
    }

    private MultiCapture mainPass;

    private Dictionary<string, ICapturePass> activePasses;
    // Start is called before the first frame update

    // Captures that can go with the main render _img pass, optimization
    private HashSet<string> WithMainMultiPass = new HashSet<string> {
        "_depth",
        "_distortion"
    };

    public Material distortionMat;

    private Texture2D readTex;

    public RenderToTexture distortionMap {
        get;
        private set;
    } 

    [SerializeField, Thor.Attributes.ReadOnly] private bool ready;


    public bool IsReady => ready;

    // void OnPreRender() {
    //     Debug.Log($"---RenderingManager, gameObject {this.gameObject.name},  OnPreRender");
    //  }

    //  void OnPostRender() {
    //     Debug.Log($"---RenderingManager, gameObject {this.gameObject.name}, OnPostRender");
    //  }

    public void EnablePasses(IEnumerable<string> activePassesNames, bool cameraChange) {
        var mainCamera = GetComponent<Camera>();
        
        if (activePassesNames != null) {
            // Debug.Log($"--------- Enabling passes 0 {string.Join(", ", activePassesNames)}");
            var newActive = activePassesNames.Select(name => {
                ICapturePass capturePass; 
                var exists = availablePasses.TryGetValue(name, out capturePass);
                return capturePass;
            });
            // var newActive = 
            if (newActive.Any(x => x == null)) {
                throw new InvalidOperationException($"Invalid capture passes `{string.Join(", ", newActive.Where(x => x == null))}`");
            }
            // Debug.Log($"--------- Enabling passes 2 {string.Join(", ", newActive.Select(x => x.GetName()))}");
            var toInitialize = newActive.Where( x => ! x.IsInitialized());
            // if this is one of the passes that is part of a MultiPass
            var mainMultiPassUpdate = toInitialize.Where(x => this.WithMainMultiPass.Contains(x.GetName()));
            foreach (var pass in mainMultiPassUpdate) {
                // TODO bad typecast, rework types
                mainPass.AddUpdateCapturePass(pass as RenderToTexture);
            }

            Debug.Log($"--------- Enabling passes toinitialize {string.Join(", ", toInitialize.Select(x => x.GetName()))}");
            //Sort by
            // Weird that multiPasCapture does not get Initialized? or already was

            // Don't initialize or onCamerachange passes that belong to other passes
            // toInitialize = toInitialize.Where(x => !this.WithMainMultiPass.Contains(x.GetName()));
            var initialized = new HashSet<string>();
            foreach (var newPass in toInitialize) {
                newPass.OnInitialize(mainCamera);
                initialized.Add(newPass.GetName());
            }
            // this.activePasses = newActive.Where(x => !this.WithMainMultiPass.Contains(x.GetName())).ToDictionary(x => x.GetName(), x => x);
            this.activePasses = newActive.ToDictionary(x => x.GetName(), x => x);
            if (cameraChange) {

                // TODO order important?
                // Initialize calls OnCameraChange
                var onCameraChange = activePasses.Values.Where(x => !this.WithMainMultiPass.Contains(x.GetName()));

                // Debug.Log($"--------- OnCameraChange passes 3 {string.Join(", ", onCameraChange)}");
                foreach (var pass in onCameraChange) { // && !initialized.Contains(x.GetName()))) {
                    pass.OnCameraChange(mainCamera);
                }
            }

            // Debug.Log($"--------- Enabling passes 4 activePasses {string.Join(", ", this.activePasses)}");
        }
    }


    public void OnCameraChange() {
        // Debug.Log($"===== OnCameraChange multipass for {string.Join(", ", this.activePasses.Values.Select(x => x.GetName()))}");
        var mainCamera = GetComponent<Camera>();
        foreach (var pass in this.activePasses.Values) {
            pass.OnCameraChange(mainCamera);
        }
    }

    //  public void OnCameraChange(Camera camera) {
    //     foreach (var pass in this.activePasses.Values) {
    //         pass.OnCameraChange(mainCamera);
    //     }
    // }

    // TODO make initializa configurable, pass a list of List<ICapturePass>() 
    public void Initialize(int? imgDisplayTarget = null, bool overwriteImgWithDistortion = false) {

        var camera = GetComponent<Camera>();
        bool supportsAntialiasing = false;
        var antiAliasLevel = supportsAntialiasing ? Mathf.Max(1, QualitySettings.antiAliasing) : 1;

        var cloudRenderingCapture = false;
        #if PLATFORM_CLOUD_RENDERING
            cloudRenderingCapture = true;
        #endif
        
       
        var depthPass = new RenderToTexture(
            new CaptureConfig() { name = "_depth", antiAliasLevel = antiAliasLevel, shaderName = "Hidden/DepthBW", renderTextureFormat = RenderTextureFormat.RFloat },
            camera: camera
        );

        var distPass = new RenderToTexture(
            new CaptureConfig() { name = "_distortion", antiAliasLevel = antiAliasLevel, shaderName = "Custom/BarrelDistortion" },
            camera: camera
        );

        var idPass = new ReplacementShaderCapture(
            new CaptureConfig() { name = "_id", antiAliasLevel = antiAliasLevel, shaderName = "Hidden/UberReplacement" },
            replacementMode: ReplacelementMode.ObjectId,
            cameraParent: camera.transform
        );

        var classPass = new ReplacementShaderCapture(
            new CaptureConfig() { name = "_class", antiAliasLevel = antiAliasLevel, shaderName = "Hidden/UberReplacement" },
            replacementMode: ReplacelementMode.CatergoryId,
            cameraParent: camera.transform
        );

        var normalsPass = new ReplacementShaderCapture(
            new CaptureConfig() { name = "_normals", antiAliasLevel = antiAliasLevel, shaderName = "Hidden/UberReplacement" },
            replacementMode: ReplacelementMode.Normals,
            cameraParent: camera.transform
        );

        this.distortionMap = new OnDemandCapture(
            new CaptureConfig() { name = "_distortion_map", antiAliasLevel = antiAliasLevel, shaderName = "Custom/BarrelDistortionMap" , cloudRendering = cloudRenderingCapture, toDisplay = 7, renderTextureFormat = RenderTextureFormat.RGFloat }
        );
        
        // make first _img capture created render to Display
        string k = !overwriteImgWithDistortion? null : distPass.name;
        this.mainPass = new MultiCapture(
            config: new CaptureConfig() { name = "_img", antiAliasLevel = antiAliasLevel, cloudRendering = cloudRenderingCapture, toDisplay = imgDisplayTarget}, 
            camera: camera, 
            passes: new List<RenderToTexture>() {
            },
            overwriteWithPass: !overwriteImgWithDistortion? null : distPass.name
        );

        availablePasses = new List<ICapturePass>() {
            this.mainPass,
            depthPass,
            distPass,
            this.distortionMap,
            idPass,
            classPass
        }.ToDictionary(x => x.GetName(), x => x);

        this.activePasses = new List<ICapturePass>() {
            this.mainPass
        }.ToDictionary(x => x.GetName(), x => x);
        mainPass.OnInitialize(camera);
        mainPass.OnCameraChange(camera);
        ready = true;
    }

    void Awake() { 

        // Debug.Log($"=-------- Rendering Manager Awake parent {this.gameObject.transform.name}");
        
        // this.enabled = true;
    }

    public T GetCapturePass<T>(string passName) where T : ICapturePass {
        ICapturePass pass;
        if (!this.activePasses.TryGetValue(passName, out pass)) {
            Debug.LogError($"No active pass at GetPassRenderTexture {passName}");
        }
        return (T)pass;
    }

    public RenderTexture GetPassRenderTexture(string passName) {
        ICapturePass pass;
        if (!this.activePasses.TryGetValue(passName, out pass)) {
            throw new InvalidOperationException($"No active pass at GetPassRenderTexture {passName}");
        }


        return pass.GetRenderTexture();
    }


    public byte[] GetCaptureBytes(string passName, bool jpeg = false) {
        ICapturePass pass;
        if (!this.activePasses.TryGetValue(passName, out pass)) {
            Debug.LogError($"No active pass at GetPassRenderTexture {passName}");
            return (new byte[0]);
        }
        else {
            // Debug.Log($"--- call GetBytes on pass {passName}");
            var bytes = pass.GetBytes(jpeg);
            // Debug.Log($"-------- bytes size {bytes.Length}");
            return bytes;
        }
    }

    public void OnDestroy() {
        Debug.Log("-------RenderingManager On destroy called");
        foreach (var pass in availablePasses) {
            (pass.Value as RenderToTexture).ReleaseRenderTexture();
        }
    }
    

    public void GetCaptureAsync(
        string passName,
        List<KeyValuePair<string, byte[]>> payload,
        string key
    ) {
        ICapturePass pass;
        if (!this.activePasses.TryGetValue(passName, out pass)) {
            Debug.LogError($"No active pass at GetPassRenderTexture {passName}");
        }
        RenderTexture tt = pass.GetRenderTexture();
        var prevActiveTex = RenderTexture.active;
        RenderTexture.active = tt;
        // camera.Render();
        AsyncGPUReadback.Request(
            tt,
            0,
            (request) => {
                if (!request.hasError) {
                    var data = request.GetData<byte>().ToArray();
                    payload.Add(new KeyValuePair<string, byte[]>(key, data));
                } else {
                    Debug.Log("Request error: " + request.hasError);
                }
            }
        );
    }

    public byte[] getDistortionMapBytes() {
        // return this.GetCapturePass<OnDemandCapture>("_distortion_map").GetBytes();
        return this.GetCapturePass<OnDemandCapture>("_distortion_map").GetBytes();
    }


    // Update is called once per frame
    void Update() { }
}