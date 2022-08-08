//C# Example
using UnityEditor;
using UnityEngine;


[ExecuteInEditMode]
public class Screenshot : EditorWindow
{

	int resWidth = Screen.width*4; 
	int resHeight = Screen.height*4;

	public Camera myCamera;
	int scale = 1;

	string path = "";
	bool showPreview = true;
	RenderTexture renderTexture;

	bool isTransparent = false;

	// Add menu item named "My Window" to the Window menu
	[MenuItem("Tools/Saad Khawaja/Instant High-Res Screenshot")]
	public static void ShowWindow()
	{
		//Show existing window instance. If one doesn't exist, make one.
		EditorWindow editorWindow = EditorWindow.GetWindow(typeof(Screenshot));
		editorWindow.autoRepaintOnSceneChange = true;
		editorWindow.Show();
		editorWindow.title = "Screenshot";
	}

	float lastTime;


	void OnGUI()
	{
		EditorGUILayout.LabelField ("Resolution", EditorStyles.boldLabel);
		resWidth = EditorGUILayout.IntField ("Width", resWidth);
		resHeight = EditorGUILayout.IntField ("Height", resHeight);

		EditorGUILayout.Space();

		scale = EditorGUILayout.IntSlider ("Scale", scale, 1, 15);

		EditorGUILayout.HelpBox("The default mode of screenshot is crop - so choose a proper width and height. The scale is a factor " +
			"to multiply or enlarge the renders without loosing quality.",MessageType.None);

		
		EditorGUILayout.Space();
		
		
		GUILayout.Label ("Save Path", EditorStyles.boldLabel);

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.TextField(path,GUILayout.ExpandWidth(false));
		if(GUILayout.Button("Browse",GUILayout.ExpandWidth(false)))
			path = EditorUtility.SaveFolderPanel("Path to Save Images",path,Application.dataPath);

		EditorGUILayout.EndHorizontal();

		EditorGUILayout.HelpBox("Choose the folder in which to save the screenshots ",MessageType.None);
		EditorGUILayout.Space();



		//isTransparent = EditorGUILayout.Toggle(isTransparent,"Transparent Background");



		GUILayout.Label ("Select Camera", EditorStyles.boldLabel);


		myCamera = EditorGUILayout.ObjectField(myCamera, typeof(Camera), true,null) as Camera;


		if(myCamera == null)
		{
			myCamera = Camera.main;
		}

		isTransparent = EditorGUILayout.Toggle("Transparent Background", isTransparent);


		EditorGUILayout.HelpBox("Choose the camera of which to capture the render. You can make the background transparent using the transparency option.",MessageType.None);

		EditorGUILayout.Space();
		EditorGUILayout.BeginVertical();
		EditorGUILayout.LabelField ("Default Options", EditorStyles.boldLabel);


		if(GUILayout.Button("Set To Screen Size"))
		{
			resHeight = (int)Handles.GetMainGameViewSize().y;
			resWidth = (int)Handles.GetMainGameViewSize().x;
		
		}


		if(GUILayout.Button("Default Size"))
		{
			resHeight = 1440;
			resWidth = 2560;
			scale = 1;
		}



		EditorGUILayout.EndVertical();

		EditorGUILayout.Space();
		EditorGUILayout.LabelField ("Screenshot will be taken at " + resWidth*scale + " x " + resHeight*scale + " px", EditorStyles.boldLabel);

		if(GUILayout.Button("Take Screenshot",GUILayout.MinHeight(60)))
		{
			if(path == "")
			{
				path = EditorUtility.SaveFolderPanel("Path to Save Images",path,Application.dataPath);
				Debug.Log("Path Set");
				TakeHiResShot();
			}
			else
			{
				TakeHiResShot();
			}
		}

		EditorGUILayout.Space();
		EditorGUILayout.BeginHorizontal();

		if(GUILayout.Button("Open Last Screenshot",GUILayout.MaxWidth(160),GUILayout.MinHeight(40)))
		{
			if(lastScreenshot != "")
			{
				Application.OpenURL("file://" + lastScreenshot);
				Debug.Log("Opening File " + lastScreenshot);
			}
		}

		if(GUILayout.Button("Open Folder",GUILayout.MaxWidth(100),GUILayout.MinHeight(40)))
		{

			Application.OpenURL("file://" + path);
		}

		if(GUILayout.Button("More Assets",GUILayout.MaxWidth(100),GUILayout.MinHeight(40)))
		{
			Application.OpenURL("https://www.assetstore.unity3d.com/en/#!/publisher/5951");
		}

		EditorGUILayout.EndHorizontal();


		if (takeHiResShot) 
		{
			int resWidthN = resWidth*scale;
			int resHeightN = resHeight*scale;
			RenderTexture rt = new RenderTexture(resWidthN, resHeightN, 24);
			myCamera.targetTexture = rt;

			TextureFormat tFormat;
			if(isTransparent)
				tFormat = TextureFormat.ARGB32;
			else
				tFormat = TextureFormat.RGB24;


			Texture2D screenShot = new Texture2D(resWidthN, resHeightN, tFormat,false);
			myCamera.Render();
			RenderTexture.active = rt;
			screenShot.ReadPixels(new Rect(0, 0, resWidthN, resHeightN), 0, 0);
			myCamera.targetTexture = null;
			RenderTexture.active = null; 
			byte[] bytes = screenShot.EncodeToPNG();
			string filename = ScreenShotName(resWidthN, resHeightN);
			
			System.IO.File.WriteAllBytes(filename, bytes);
			Debug.Log(string.Format("Took screenshot to: {0}", filename));
			Application.OpenURL(filename);
			takeHiResShot = false;
		}

		EditorGUILayout.HelpBox("In case of any error, make sure you have Unity Pro as the plugin requires Unity Pro to work.",MessageType.Info);


	}


	
	private bool takeHiResShot = false;
	public string lastScreenshot = "";
	
		
	public string ScreenShotName(int width, int height) {

		string strPath="";

		strPath = string.Format("{0}/screen_{1}x{2}_{3}.png", 
		                     path, 
		                     width, height, 
		                               System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
		lastScreenshot = strPath;
	
		return strPath;
	}



	public void TakeHiResShot() {
		Debug.Log("Taking Screenshot");
		takeHiResShot = true;
	}

}

