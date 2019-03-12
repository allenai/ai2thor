using UnityEngine;
using System.Collections;

public class MirrorReflectionScript : MonoBehaviour
{
    private MirrorCameraScript childScript;

    private void Start()
    {
        childScript = gameObject.transform.parent.gameObject.GetComponentInChildren<MirrorCameraScript>();

        if (childScript == null)
        {
            Debug.LogError("Child script (MirrorCameraScript) should be in sibling object");
        }
    }

    private void OnWillRenderObject()
    {
        childScript.RenderMirror();
    }
}
