using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

// @TODO:
// . support custom color wheels in optical flow via lookup textures
// . support custom depth encoding
// . support multiple overlay cameras - note:this can be shown in editor already by creating multiple GAME windows and assigning a different display number to each
// . tests
// . better example scene(s)

// @KNOWN ISSUES
// . Motion Vectors can produce incorrect results in Unity 5.5.f3 when
//      1) during the first rendering frame
//      2) rendering several cameras with different aspect ratios - vectors do stretch to the sides of the screen

[RequireComponent (typeof(Camera))]
public class ImageSynthesis : MonoBehaviour {

	// pass configuration
	private CapturePass[] capturePasses = new CapturePass[] {
		new CapturePass() { name = "_img" },
		new CapturePass() { name = "_depth" },
		new CapturePass() { name = "_id", supportsAntialiasing = false },
		new CapturePass() { name = "_class", supportsAntialiasing = false },
		new CapturePass() { name = "_normals"},
		new CapturePass() { name = "_flow", supportsAntialiasing = false, needsRescale = true }, // (see issue with Motion Vectors in @KNOWN ISSUES)


		//new CapturePass() { name = "_position" },

	};

	struct CapturePass {
		// configuration
		public string name;
		public bool supportsAntialiasing;
		public bool needsRescale;
		public CapturePass(string name_) { name = name_; supportsAntialiasing = true; needsRescale = false; camera = null; }

		// impl
		public Camera camera;
	};

	public bool hasCapturePass(string name) {
		for (int i = 0; i < capturePasses.Length; i++) {
			if (capturePasses[i].name == name) {
				return true;
			}
		}
		return false;
	}
	
	private Shader uberReplacementShader;
    private Shader opticalFlowShader;
    private Shader depthShader;
	//public Shader positionShader;


	public Dictionary<Color, string> colorIds;

	public float opticalFlowSensitivity;

	private Dictionary<int, string> nonSimObjObjectIds = new Dictionary<int, string>();

	// cached materials
	private Material opticalFlowMaterial;
	private Material depthMaterial;
	System.Security.Cryptography.MD5 md5;

	public bool sentColorCorrespondence;

	public Texture2D tex;

	void Start()
	{
		//XXXXXXXXXXX************
		//Remember, adding any new Shaders requires them to be included in Project Settings->Graphics->Always Included Shaders
		//otherwise the standlone will build without the shaders and you will be sad


		// default fallbacks, if shaders are unspecified
		if (!uberReplacementShader)
			uberReplacementShader = Shader.Find("Hidden/UberReplacement");

		if (!opticalFlowShader)
			opticalFlowShader = Shader.Find("Hidden/OpticalFlow");

        #if UNITY_EDITOR

            if (!depthShader)
                depthShader = Shader.Find("Hidden/DepthBW");
        #else
            if (!depthShader)
                depthShader = Shader.Find("Hidden/Depth");

        #endif

        //if (!positionShader)
        //	positionShader = Shader.Find("Hidden/World");

        opticalFlowSensitivity = 50.0f;

		// use real camera to capture final image
		capturePasses[0].camera = GetComponent<Camera>();
		for (int q = 1; q < capturePasses.Length; q++) {
			capturePasses[q].camera = CreateHiddenCamera (capturePasses[q].name);
		}
		md5 = System.Security.Cryptography.MD5.Create();

		OnCameraChange();
		OnSceneChange();

	}

	void LateUpdate()
	{
		#if UNITY_EDITOR
		if (DetectPotentialSceneChangeInEditor())
			OnSceneChange();

		#endif // UNITY_EDITOR

		// @TODO: detect if camera properties actually changed
		//OnCameraChange();
	}
	
	private Camera CreateHiddenCamera(string name)
	{
		var go = new GameObject (name, typeof (Camera));
    #if !UNITY_EDITOR // Useful to be able to see these cameras in the editor
		go.hideFlags = HideFlags.HideAndDontSave; 
    #endif
		go.transform.parent = transform;

        //this is a check for if the image synth is being added to a ThirdPartyCamera, which doesn't have a FirstPersonCharacterCull component
        //Note: Check that all image synthesis works with third party cameras, as the image synth assumes that it is taking default settings
        //from the Agent's camera, and a ThirdPartyCamera does not have the same defaults, which may cause some errors
        if(go.transform.parent.GetComponent<FirstPersonCharacterCull>())
		//add the FirstPersonCharacterCull so this camera's agent is not rendered- other agents when multi agent is enabled should still be rendered
		go.AddComponent<FirstPersonCharacterCull>(go.transform.parent.GetComponent<FirstPersonCharacterCull>());

		var newCamera = go.GetComponent<Camera>();
		newCamera.cullingMask = 1;//render everything, including PlaceableSurfaces
		return newCamera;
	}


	static private void SetupCameraWithReplacementShader(Camera cam, Shader shader)
	{
		var cb = new CommandBuffer();
		cam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, cb);
		cam.AddCommandBuffer(CameraEvent.BeforeFinalPass, cb);
		cam.SetReplacementShader(shader, "");
		cam.backgroundColor = Color.black;
		cam.clearFlags = CameraClearFlags.SolidColor;
	}

	static private void SetupCameraWithReplacementShader(Camera cam, Shader shader, ReplacelementModes mode)
	{
		SetupCameraWithReplacementShader(cam, shader, mode, Color.blue);
	}

	static private void SetupCameraWithReplacementShader(Camera cam, Shader shader, ReplacelementModes mode, Color clearColor)
	{
		var cb = new CommandBuffer();
		cb.SetGlobalFloat("_OutputMode", (int)mode); // @TODO: CommandBuffer is missing SetGlobalInt() method
		cam.renderingPath = RenderingPath.Forward;
		cam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, cb);
		cam.AddCommandBuffer(CameraEvent.BeforeFinalPass, cb);
		cam.SetReplacementShader(shader, "");
		cam.backgroundColor = clearColor;
		cam.clearFlags = CameraClearFlags.SolidColor;
	}

	static private void SetupCameraWithPostShader(Camera cam, Material material, DepthTextureMode depthTextureMode = DepthTextureMode.None)
	{
		var cb = new CommandBuffer();
		cb.Blit(null, BuiltinRenderTextureType.CurrentActive, material);
		cam.AddCommandBuffer(CameraEvent.AfterEverything, cb);
		cam.depthTextureMode = depthTextureMode;
	}

	enum ReplacelementModes {
		ObjectId 			= 0,
		CatergoryId			= 1,
		DepthCompressed		= 2,
		DepthMultichannel	= 3,
		Normals				= 4,
        Flow                = 5,
	};

	//Call this if the settings on the main camera ever change? But the main camera now uses slightly different layer masks and deffered/forward render settings than these image synth cameras
	//do, so maybe it's fine for now I dunno
	public void OnCameraChange()
	{
		var mainCamera = GetComponent<Camera>();
		mainCamera.depth = 9999; // This ensures the main camera is rendered on screen

		foreach (var pass in capturePasses)
		{
			if (pass.camera == mainCamera)
				continue;

			// cleanup capturing camera
 			pass.camera.RemoveAllCommandBuffers();

			// copy all "main" camera parameters into capturing camera
			pass.camera.CopyFrom(mainCamera);

			//make sure the capturing camera is set to Forward rendering (main camera uses Deffered now)
			pass.camera.renderingPath = RenderingPath.Forward;
			//make sure capturing camera renders all layers (value copied from Main camera excludes PlaceableSurfaces layer, which needs to be rendered on this camera)
			pass.camera.cullingMask = -1;

			pass.camera.depth = 0; // This ensures the new camera does not get rendered on screen
		}
	
		//set the display corresponding to which capturePass this is
		for(int i = 0; i < capturePasses.Length; i++)
		{
			capturePasses[i].camera.targetDisplay = i;
		}

		// cache materials and setup material properties
		if (!opticalFlowMaterial || opticalFlowMaterial.shader != opticalFlowShader) {
			opticalFlowMaterial = new Material (opticalFlowShader);
		}
		opticalFlowMaterial.SetFloat ("_Sensitivity", opticalFlowSensitivity);

		if (!depthMaterial || depthMaterial.shader != depthShader) {
			depthMaterial = new Material (depthShader);	
		}
		//capturePasses [1].camera.farClipPlane = 100;
		//SetupCameraWithReplacementShader(capturePasses[1].camera, uberReplacementShader, ReplacelementModes.DepthMultichannel);
		SetupCameraWithPostShader(capturePasses[1].camera, depthMaterial, DepthTextureMode.Depth);


        SetupCameraWithReplacementShader(capturePasses[2].camera, uberReplacementShader, ReplacelementModes.ObjectId);
		SetupCameraWithReplacementShader(capturePasses[3].camera, uberReplacementShader, ReplacelementModes.CatergoryId);
		SetupCameraWithReplacementShader(capturePasses[4].camera, uberReplacementShader, ReplacelementModes.Normals);
		SetupCameraWithPostShader(capturePasses[5].camera, opticalFlowMaterial, DepthTextureMode.Depth | DepthTextureMode.MotionVectors);

        #if UNITY_EDITOR
            for(int i = 0; i < capturePasses.Length; i++)
            {
                capturePasses[i].camera.targetDisplay = i;
            }
        #endif

        /*
		SetupCameraWithReplacementShader(capturePasses[6].camera, positionShader);
		*/

    }


    public string MD5Hash(string input) {
		byte[] data = md5.ComputeHash(System.Text.Encoding.Default.GetBytes(input));
		// Create string representation
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		for (int i = 0; i < data.Length; ++i) {
			sb.Append(data[i].ToString("x2"));
		}
		return sb.ToString();
	}


	private string getObjectId(GameObject gameObject) {
		// the object id is generated this way to handle the edge case
		// where a non-simobject could get moved from its initial position 
		// during a simulation.  This forces the objectId to get generated once
		// on scene startup
		int key = gameObject.GetInstanceID();
		if (nonSimObjObjectIds.ContainsKey(key)) {
			return nonSimObjObjectIds[key];
		} else {
			Transform t = gameObject.transform;
			string objectId = gameObject.name + "|" + t.position.x + "|" + t.position.y + "|" + t.position.z;
			nonSimObjObjectIds[key] = objectId;
			return objectId;
		}
	}


	public void OnSceneChange()
	{
		sentColorCorrespondence = false;
		var renderers = UnityEngine.Object.FindObjectsOfType<Renderer>();
		colorIds = new Dictionary<Color, string> ();
		var mpb = new MaterialPropertyBlock();



		foreach (var r in renderers)
		{
			// var layer = r.gameObject.layer;
			// var tag = r.gameObject.tag;

			string classTag = r.name;
			string objTag = getObjectId(r.gameObject);

			StructureObject so = r.gameObject.GetComponent<StructureObject> ();
			if (so == null) 
			{
				so = r.gameObject.GetComponentInParent<StructureObject> ();
			}

			SimObjPhysics sop = r.gameObject.GetComponent<SimObjPhysics> ();
			if (sop == null) {
				sop = r.gameObject.GetComponentInParent<SimObjPhysics> ();
			}

			if (so != null) {
				classTag = "" + so.WhatIsMyStructureObjectTag;
				//objTag = so.gameObject.name;
			} 
			if (sop != null) 
			{
				classTag = "" + sop.Type;
				objTag = sop.ObjectID;
			}


			Color classColor;
			Color objColor;
			classColor = ColorEncoding.EncodeTagAsColor (classTag);
			objColor = ColorEncoding.EncodeTagAsColor(objTag);

			capturePasses [0].camera.WorldToScreenPoint (r.bounds.center);

			if (so != null || sop != null) 
			{
				colorIds [objColor] = objTag;
				colorIds [classColor] = classTag;
			} 

			else 
			{
				colorIds [objColor] = r.gameObject.name;
			}

//			if (r.material.name.ToLower().Contains ("lightray")) {
//				objColor.a = 0;
//				classColor.a = 0;
//				mpb.SetFloat ("_Opacity", 0);
//
//			} else {
//				objColor.a = 1;
//				classColor.a = 1;
//				mpb.SetFloat ("_Opacity", 1);
//			}
//
			// updated per @danielg - replaces commented out code
			if (r.material.name.ToLower ().Contains ("lightray")) {
				r.enabled = false;
				continue;
			}

			objColor.a = 1;
			classColor.a = 1;
			mpb.SetFloat ("_Opacity", 1);
			mpb.SetColor("_CategoryColor", classColor);
			mpb.SetColor("_ObjectColor", objColor);

			r.SetPropertyBlock(mpb);
		}
	}

    public byte[] Encode(
        string passName,
        RenderTextureFormat format = RenderTextureFormat.Default, 
        RenderTextureReadWrite textureReadMode = RenderTextureReadWrite.Default, 
        int width = -1, 
        int height = -1, 
        bool jpg = false
    ) 
    {
        // Must be called after end of Frame

		if (width <= 0) {
			width = Screen.width;
		}
		if (height <= 0) {
			height = Screen.height;
		}

		foreach (var pass in capturePasses)
            if(pass.name == passName)
                return Encode(pass.camera, width, height, pass.supportsAntialiasing, pass.needsRescale, jpg, format, textureReadMode);

		return(new byte[0]);
    }

	public void Save(string filename, int width = -1, int height = -1, string path = "")
	{
		if (width <= 0 || height <= 0)
		{
			width = Screen.width;
			height = Screen.height;
		}

		var filenameExtension = System.IO.Path.GetExtension(filename);
		if (filenameExtension == "")
			filenameExtension = ".png";
		var filenameWithoutExtension = Path.GetFileNameWithoutExtension(filename);

		var pathWithoutExtension = Path.Combine(path, filenameWithoutExtension);

		// execute as coroutine to wait for the EndOfFrame before starting capture
		StartCoroutine(
			WaitForEndOfFrameAndSave(pathWithoutExtension, filenameExtension, width, height));
	}

	private IEnumerator WaitForEndOfFrameAndSave(string filenameWithoutExtension, string filenameExtension, int width, int height)
	{
		yield return new WaitForEndOfFrame();
		Save(filenameWithoutExtension, filenameExtension, width, height);
	}

	private void Save(string filenameWithoutExtension, string filenameExtension, int width, int height)
	{
		foreach (var pass in capturePasses)
			Save(pass.camera, filenameWithoutExtension + pass.name + filenameExtension, width, height, pass.supportsAntialiasing, pass.needsRescale);
	}

	private byte[] Encode(
        Camera cam,
        int width,
        int height, 
        bool supportsAntialiasing, 
        bool needsRescale, 
        bool jpg = false, 
        RenderTextureFormat format = RenderTextureFormat.Default, 
        RenderTextureReadWrite textureReadMode = RenderTextureReadWrite.Default
    )
	{
		var mainCamera = GetComponent<Camera>();
		var depth = 24;
		var readWrite = textureReadMode;
		var antiAliasing = (supportsAntialiasing) ? Mathf.Max(1, QualitySettings.antiAliasing) : 1;

		var finalRT =
			RenderTexture.GetTemporary(width, height, depth, format, readWrite, antiAliasing);
		var renderRT = (!needsRescale) ? finalRT :
			RenderTexture.GetTemporary(mainCamera.pixelWidth, mainCamera.pixelHeight, depth, format, readWrite, antiAliasing);
		if (tex == null) {
			tex = new Texture2D (width, height, TextureFormat.RGB24, false);
		}

		var prevActiveRT = RenderTexture.active;
		var prevCameraRT = cam.targetTexture;

		// render to offscreen texture (readonly from CPU side)
		RenderTexture.active = renderRT;
		cam.targetTexture = renderRT;

		cam.Render();

		if (needsRescale)
		{
			// blit to rescale (see issue with Motion Vectors in @KNOWN ISSUES)
			RenderTexture.active = finalRT;
			Graphics.Blit(renderRT, finalRT);
			RenderTexture.ReleaseTemporary(renderRT);
		}

		// read offsreen texture contents into the CPU readable texture
		float startTime = Time.realtimeSinceStartup;

		tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
		tex.Apply();
		Debug.Log ("imageSynth encode time" + (Time.realtimeSinceStartup - startTime));

		startTime = Time.realtimeSinceStartup;

		// encode texture into PNG/JPG
		byte[] bytes;
		if(jpg)
		    bytes = tex.EncodeToJPG();
        else
		    //bytes = tex.EncodeToPNG();
			bytes = tex.GetRawTextureData();
		Debug.Log ("imageSynth format time" + (Time.realtimeSinceStartup - startTime));


		// restore state and cleanup
		cam.targetTexture = prevCameraRT;
		RenderTexture.active = prevActiveRT;

	

		//UnityEngine.Object.Destroy(tex);
		RenderTexture.ReleaseTemporary(finalRT);
		return bytes;
	}

	private void Save(Camera cam, string filename, int width, int height, bool supportsAntialiasing, bool needsRescale)
	{
		byte[] bytes = Encode(cam, width, height, supportsAntialiasing, needsRescale);
		File.WriteAllBytes(filename, bytes);					
    }

#if UNITY_EDITOR
	private GameObject lastSelectedGO;
	private int lastSelectedGOLayer = -1;
	private string lastSelectedGOTag = "unknown";
	private bool DetectPotentialSceneChangeInEditor()
	{
		bool change = false;
		// there is no callback in Unity Editor to automatically detect changes in scene objects
		// as a workaround lets track selected objects and check, if properties that are 
		// interesting for us (layer or tag) did not change since the last frame
		if (UnityEditor.Selection.transforms.Length > 1)
		{
			// multiple objects are selected, all bets are off!
			// we have to assume these objects are being edited
			change = true;
			lastSelectedGO = null;
		}
		else if (UnityEditor.Selection.activeGameObject)
		{
			var go = UnityEditor.Selection.activeGameObject;
			// check if layer or tag of a selected object have changed since the last frame
			var potentialChangeHappened = lastSelectedGOLayer != go.layer || lastSelectedGOTag != go.tag;
			if (go == lastSelectedGO && potentialChangeHappened)
				change = true;

			lastSelectedGO = go;
			lastSelectedGOLayer = go.layer;
			lastSelectedGOTag = go.tag;
		}

		return change;
	}
#endif // UNITY_EDITOR
}
