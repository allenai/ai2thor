/************************************************************************************
Filename    :   ONSPPropagationGeometryEditor.cs
Content     :   Geometry editor class
                Attach to geometry to define material properties
Copyright   :   Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Licensed under the Oculus SDK Version 3.5 (the "License"); 
you may not use the Oculus SDK except in compliance with the License, 
which is provided at the time of installation or download, or which 
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developer.oculus.com/licenses/sdk-3.5/

Unless required by applicable law or agreed to in writing, the Oculus SDK 
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
************************************************************************************/
//#define ENABLE_DEBUG_EXPORT_OBJ

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ONSPPropagationGeometry))] 
public class ONSPPropagationGeometryEditor : Editor
{
	public override void OnInspectorGUI()
	{
        ONSPPropagationGeometry mesh = (ONSPPropagationGeometry)target;
		
		EditorGUI.BeginChangeCheck();
		
		bool newIncludeChildMeshes = EditorGUILayout.Toggle( new GUIContent("Include Child Meshes","Include all child meshes into single geometry instance"), mesh.includeChildMeshes );

        Separator();

        #if UNITY_EDITOR
        string newFilePath = mesh.filePath;
		bool editedPath = false;
		bool writeMesh = false;
		EditorGUI.BeginDisabledGroup( Application.isPlaying );
		bool newFileEnabled = EditorGUILayout.Toggle( new GUIContent("File Enabled","If set, the serialized mesh file is used as the mesh data source"), mesh.fileEnabled );
		EditorGUILayout.LabelField( new GUIContent("File Path:","The path to the serialized mesh file, relative to the StreamingAssets directory" ),
                                    new GUIContent(mesh.filePathRelative != null ? mesh.filePathRelative : ""));
		
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.PrefixLabel( " " );
		if ( GUILayout.Button("Set Path") )
		{
            if (!System.IO.Directory.Exists(Application.streamingAssetsPath))
            {
                System.IO.Directory.CreateDirectory(Application.streamingAssetsPath);
            }

            string directory = Application.streamingAssetsPath;
            string fileName = mesh.gameObject.name + "." + ONSPPropagationGeometry.GEOMETRY_FILE_EXTENSION;

            if (newFilePath != "")
            {
                directory = System.IO.Path.GetDirectoryName(newFilePath);
                fileName = System.IO.Path.GetFileName(newFilePath);
            }

			newFilePath = EditorUtility.SaveFilePanel(
                "Save baked mesh to file", directory, fileName,
                ONSPPropagationGeometry.GEOMETRY_FILE_EXTENSION);
			
			// If the user canceled, use the old path.
			if ( newFilePath == null || newFilePath.Length == 0 )
				newFilePath = mesh.filePath;
			else
				editedPath = true;
		}

		if ( GUILayout.Button("Bake Mesh to File") )
			writeMesh = true;
		
		EditorGUILayout.EndHorizontal();

#if ENABLE_DEBUG_EXPORT_OBJ
        // this allows you to export the geometry to a .obj for viewing
        // in an external model viewer for debugging/validation
        if ( GUILayout.Button("Write to .obj (debug)") )
            mesh.WriteToObj();
#endif

		EditorGUI.EndDisabledGroup();
		
        #endif
		
		if ( EditorGUI.EndChangeCheck() )
		{
			Undo.RecordObject( mesh, "Edited OVRAudioMesh" );

			mesh.includeChildMeshes = newIncludeChildMeshes;
			mesh.fileEnabled = newFileEnabled;

            newFilePath = newFilePath.Replace(Application.streamingAssetsPath + "/", "");

			if ( editedPath )
                mesh.filePathRelative = newFilePath;
			
			if ( editedPath || writeMesh )
				mesh.WriteFile();
		}

		if ( Application.isPlaying && GUILayout.Button("Upload Mesh") )
			mesh.UploadGeometry();
    }
    void Separator()
    {
        GUI.color = new Color(1, 1, 1, 0.25f);
        GUILayout.Box("", "HorizontalSlider", GUILayout.Height(16));
        GUI.color = Color.white;
    }

}

