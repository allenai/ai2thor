using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class FirstPersonCharacterCull : MonoBehaviour
{

    public MeshRenderer myMeshRenderer; // material you want the camera to change

    void OnPreRender()
    {
        //myMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        myMeshRenderer.enabled = false;
    }

    void OnPostRender()
    {
        //myMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        myMeshRenderer.enabled = true;
    }

}