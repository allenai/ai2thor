using UnityEngine;
using UnityEditor;
using System.Collections;

public class GroupSelected : EditorWindow
{
    /// &lt;summary&gt;
    ///  Creates an empty node at the center of all selected nodes and parents all selected underneath it. 
    ///  Basically a nice re-creation of Maya grouping!
    /// &lt;/summary&gt;

    [MenuItem("GameObject / Group Selected %g", priority = 80)]
    static void Init()
    {
        Transform[] selected = Selection.GetTransforms(SelectionMode.ExcludePrefab | SelectionMode.TopLevel);

        GameObject emptyNode = new GameObject();
        Vector3 averagePosition = Vector3.zero;
        foreach (Transform node in selected)
        {
            averagePosition += node.position;
        }
        if (selected.Length != 0)
        {
            averagePosition /= selected.Length;
        }
        emptyNode.transform.position = averagePosition;
        emptyNode.name = "Group";
        foreach (Transform node in selected)
        {
            node.parent = emptyNode.transform;
        }
    }
}