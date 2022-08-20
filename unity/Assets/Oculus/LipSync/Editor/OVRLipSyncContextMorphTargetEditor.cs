/************************************************************************************
Filename    :   OVRLipSyncContextMorphTargetEditor.cs
Content     :   This bridges the viseme output to the morph targets
Created     :   December 21st, 2018
Copyright   :   Copyright Facebook Technologies, LLC and its affiliates.
                All rights reserved.

Licensed under the Oculus Audio SDK License Version 3.3 (the "License");
you may not use the Oculus Audio SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developer.oculus.com/licenses/audio-3.3/

Unless required by applicable law or agreed to in writing, the Oculus Audio SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
************************************************************************************/
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(OVRLipSyncContextMorphTarget))]
public class OVRLipSyncContextMorphTargetEditor : Editor
{
  SerializedProperty skinnedMeshRenderer;
  SerializedProperty visemeToBlendTargets;
  SerializedProperty visemeTestKeys;
  SerializedProperty laughterKey;
  SerializedProperty laughterBlendTarget;
  SerializedProperty laughterThreshold;
  SerializedProperty laughterMultiplier;
  SerializedProperty smoothAmounth;
  private static string[] visemeNames = new string[] {
    "sil", "PP", "FF", "TH",
    "DD", "kk", "CH", "SS",
    "nn", "RR", "aa", "E",
    "ih", "oh", "ou" };
  void OnEnable()
  {
    skinnedMeshRenderer = serializedObject.FindProperty("skinnedMeshRenderer");
    visemeToBlendTargets = serializedObject.FindProperty("visemeToBlendTargets");
    visemeTestKeys = serializedObject.FindProperty("visemeTestKeys");
    laughterKey = serializedObject.FindProperty("laughterKey");
    laughterBlendTarget = serializedObject.FindProperty("laughterBlendTarget");
    laughterThreshold = serializedObject.FindProperty("laughterThreshold");
    laughterMultiplier = serializedObject.FindProperty("laughterMultiplier");
    smoothAmounth = serializedObject.FindProperty("smoothAmount");
  }

  private void BlendNameProperty(SerializedProperty prop, string name, string[] blendNames = null)
  {
    if (blendNames == null)
    {
      EditorGUILayout.PropertyField(prop, new GUIContent(name));
      return;
    }
    var values = new int[blendNames.Length + 1];
    var options = new GUIContent[blendNames.Length + 1];
    values[0] = -1;
    options[0] = new GUIContent("   ");
    for(int i = 0; i < blendNames.Length; ++i)
    {
      values[i + 1] = i;
      options[i + 1] = new GUIContent(blendNames[i]);
    }
    EditorGUILayout.IntPopup(prop, options, values, new GUIContent(name));
  }

  private string[] GetMeshBlendNames()
  {
    var morphTarget = (OVRLipSyncContextMorphTarget)serializedObject.targetObject;
    if (morphTarget == null || morphTarget.skinnedMeshRenderer == null)
    {
      return null;
    }
    var mesh = morphTarget.skinnedMeshRenderer.sharedMesh;
    var blendshapeCount = mesh.blendShapeCount;
    var blendNames = new string[blendshapeCount];
    for(int i = 0; i < mesh.blendShapeCount; ++i)
    {
      blendNames[i] = mesh.GetBlendShapeName(i);
    }
    return blendNames;
  }
  public override void OnInspectorGUI()
  {
    var blendNames = GetMeshBlendNames();
    var morphTarget = (OVRLipSyncContextMorphTarget)serializedObject.targetObject;

    serializedObject.Update();
    EditorGUILayout.PropertyField(skinnedMeshRenderer);
    if (EditorGUILayout.PropertyField(visemeToBlendTargets))
    {
      EditorGUI.indentLevel++;
      for(int i = 1; i < visemeNames.Length; ++i)
      {
        BlendNameProperty(visemeToBlendTargets.GetArrayElementAtIndex(i), visemeNames[i], blendNames);
      }
      BlendNameProperty(laughterBlendTarget, "Laughter", blendNames);
      EditorGUI.indentLevel--;
    }
    if (morphTarget)
    {
      morphTarget.enableVisemeTestKeys = EditorGUILayout.ToggleLeft("Enable Viseme Test Keys", morphTarget.enableVisemeTestKeys);
    }
    if (EditorGUILayout.PropertyField(visemeTestKeys))
    {
      EditorGUI.indentLevel++;
      for(int i = 1; i < visemeNames.Length; ++i)
      {
        EditorGUILayout.PropertyField(visemeTestKeys.GetArrayElementAtIndex(i), new GUIContent(visemeNames[i]));
      }
      EditorGUILayout.PropertyField(laughterKey, new GUIContent("Laughter"));
      EditorGUI.indentLevel--;
    }
    EditorGUILayout.PropertyField(laughterThreshold);
    EditorGUILayout.PropertyField(laughterMultiplier);
    EditorGUILayout.PropertyField(smoothAmounth);
    serializedObject.ApplyModifiedProperties();
  }
}
