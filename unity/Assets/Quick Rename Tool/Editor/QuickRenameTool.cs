using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;




public class QuickRenameTool : EditorWindow
{
    

	//public bool inspectorTitlebar;

	public UnityEngine.Object objectField;

    private Rect windowRect = new Rect(20, 20, 10, 100);
    private int field_widthSize = 100;

    public bool _Initialize = false;

	private bool foldout_rename = true;
	private bool foldout_replace = true;
	private bool foldout_sepalte = true;
	private bool foldout_add = true;
	private bool foldout_sub = true;
	private bool foldout_addNumber = true;

	Vector2 leftScrollPos = Vector2.zero;


	void OnGUI()
    {

        QuickRenameTool_item.languageChange_run();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        {

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginHorizontal(GUI.skin.box, GUILayout.Width(250));
                {
                    QuickRenameTool_item.topBar();
                }
                EditorGUILayout.EndHorizontal();
                QuickRenameTool_item.languageBar();
            }
            EditorGUILayout.EndHorizontal();

            leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos, GUI.skin.box);
            {

                QuickRenameTool_item.layout("rename", ref foldout_rename);
                QuickRenameTool_item.layout("addNumber", ref foldout_addNumber);
                QuickRenameTool_item.layout("sub", ref foldout_sub);
                QuickRenameTool_item.layout("replace", ref foldout_replace);
                QuickRenameTool_item.layout("separat", ref foldout_sepalte);
                QuickRenameTool_item.layout("Add Word", ref foldout_add);
                
            } EditorGUILayout.EndScrollView();
        } EditorGUILayout.EndVertical();
            
    }



    [MenuItem("Window/Quick Rename Tool")]
    static void Open()
    {
        QuickRenameTool window = EditorWindow.GetWindow<QuickRenameTool>("Quick Rename Tool");

        window.Show();

    }

    

    
}

