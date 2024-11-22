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

public class RenderingManager : MonoBehaviour {


    private Dictionary<string, ICapturePass> availablePasses;

    private MultiCapture mainPass;

    private Dictionary<string, ICapturePass> activePasses;
    // Start is called before the first frame update

    // Captures that can go with the main render _img pass, optimization
    private HashSet<string> WithMainMultiPass = new HashSet<string> {
        "_depth",
        "_distortion"
    };
    
    // to set _img pass to display 0 in editor and standalone plaforms
    public bool IsMainCamera;

    public Material distortionMat;

    private Texture2D readTex;

    private static bool isMainCameraPassCreated = false;  

    void Initialize(Camera camera) {

        // var camera = GetComponent<Camera>();
        // var antiAliasLevel = 1;
        
       
        // var depthPass = new RenderToTexture(
        //     name: "_depth", camera: camera, antiAliasLevel: antiAliasLevel, shaderName: "Hidden/DepthBW"
        // );

        // var distPass = new RenderToTexture(
        //     name: "_distortion", camera: camera, antiAliasLevel: antiAliasLevel, shaderName: "Custom/BarrelDistortion"
        // );

        // var idPass = new ReplacementShaderCapture(
        //     name: "_id", cameraParent: this.transform, replacementMode: ReplacelementMode.ObjectId, antiAliasLevel: antiAliasLevel, shaderName: "Hidden/UberReplacement"
        // );

        // var classPass = new ReplacementShaderCapture(
        //     name: "_class", cameraParent: this.transform, replacementMode: ReplacelementMode.CatergoryId, antiAliasLevel: antiAliasLevel, shaderName: "Hidden/UberReplacement"
        // );

        // var normalsPass = new ReplacementShaderCapture(
        //     name: "_normals", cameraParent: this.transform, replacementMode: ReplacelementMode.Normals, antiAliasLevel: antiAliasLevel, shaderName: "Hidden/UberReplacement"
        // ); 

        // this.mainPass = new MultiCapture("_img", camera, new List<RenderToTexture>() {
            
        // });

        // availablePasses = new List<ICapturePass>() {
        //     this.mainPass,
        //     depthPass,
        //     distPass,
        //     idPass,
        //     classPass
        // }.ToDictionary(x => x.GetName(), x => x);

        // this.activePasses = new List<ICapturePass>() {
        //     this.mainPass
        // }.ToDictionary(x => x.GetName(), x => x);
        // mainPass.OnInitialize(camera);

    }

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

            Debug.Log($"--------- Enabling passes 3 toinitialize {string.Join(", ", toInitialize.Select(x => x.GetName()))}");
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


    void Awake() { 

        Debug.Log($"=-------- Rendering Manager Awake parent {this.gameObject.transform.name}");
        var camera = GetComponent<Camera>();
        bool supportsAntialiasing = false;
        var antiAliasLevel = supportsAntialiasing ? Mathf.Max(1, QualitySettings.antiAliasing) : 1;

        var cloudRenderingCapture = false;
        #if PLATFORM_CLOUD_RENDERING
            cloudRenderingCapture = true;
        #endif
        
       
        var depthPass = new RenderToTexture(
            new CaptureConfig() { name = "_depth", antiAliasLevel = antiAliasLevel, shaderName = "Hidden/DepthBW" },
            camera: camera
        );

        var distPass = new RenderToTexture(
            new CaptureConfig() { name = "_distortion", antiAliasLevel = antiAliasLevel, shaderName = "Custom/BarrelDistortion" },
            camera: camera
        );

        var idPass = new ReplacementShaderCapture(
            new CaptureConfig() { name = "_id", antiAliasLevel = antiAliasLevel, shaderName = "Hidden/UberReplacement", replacementMode = ReplacelementMode.ObjectId },
            cameraParent: camera.transform
        );

        var classPass = new ReplacementShaderCapture(
            new CaptureConfig() { name = "_class", antiAliasLevel = antiAliasLevel, shaderName = "Hidden/UberReplacement", replacementMode = ReplacelementMode.CatergoryId },
            cameraParent: camera.transform
        );

        var normalsPass = new ReplacementShaderCapture(
            new CaptureConfig() { name = "_normals", antiAliasLevel = antiAliasLevel, shaderName = "Hidden/UberReplacement", replacementMode = ReplacelementMode.Normals },
            cameraParent: camera.transform
        );
        
        // make first _img capture created render to Display
        int? toDisplay = null;
        this.mainPass = new MultiCapture(
            config: new CaptureConfig() { name = "_img", antiAliasLevel = antiAliasLevel, cloudRendering = cloudRenderingCapture, toDisplay = isMainCameraPassCreated ? toDisplay : 0}, 
            camera: camera, 
            passes: new List<RenderToTexture>() {
            } 
        );

        availablePasses = new List<ICapturePass>() {
            this.mainPass,
            depthPass,
            distPass,
            idPass,
            classPass
        }.ToDictionary(x => x.GetName(), x => x);

        this.activePasses = new List<ICapturePass>() {
            this.mainPass
        }.ToDictionary(x => x.GetName(), x => x);
        mainPass.OnInitialize(camera);
        mainPass.OnCameraChange(camera);
        // this.enabled = true;
    }

    public RenderToTexture GetCapturePass(string passName) {
        ICapturePass pass;
        if (!this.activePasses.TryGetValue(passName, out pass)) {
            Debug.LogError($"No active pass at GetPassRenderTexture {passName}");
            return null;
        }
        return pass as RenderToTexture;
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

    // Update is called once per frame
    void Update() { }
}