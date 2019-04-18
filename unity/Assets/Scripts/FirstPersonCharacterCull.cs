using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class FirstPersonCharacterCull : MonoBehaviour
{

    public MeshRenderer myMeshRenderer; //Mesh renderer that you want this script's camera to cull

    void OnPreRender() //Just before this camera starts to render...
    {
        myMeshRenderer.enabled = false; //Turn off renderer
    }

    void OnPostRender() //Immediately after this camera renders...
    {  
        myMeshRenderer.enabled = true; //Turn renderer back on
    }

}