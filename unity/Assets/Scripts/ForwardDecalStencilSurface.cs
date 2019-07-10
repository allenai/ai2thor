using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


public class ForwardDecalStencilSurface : MonoBehaviour
{
    [SerializeField]
     private Material material;
     [SerializeField]
     private Mesh quadMesh;
    // Start is called before the first frame update
    private CommandBuffer buffer;
    private Camera viewCamera;
    void Start() {
        this.buffer = new CommandBuffer();
        buffer.name = "Deferred Decals Surfaces";
        this.viewCamera = Camera.main;
        viewCamera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, buffer);
     }
    public void OnWillRenderObject()
	{
        // Happens when editor swap in code
        if (buffer == null) {
            buffer = new CommandBuffer();
            buffer.name = "Deferred Decals Surfaces";
        }
        buffer.Clear();
        
        
        buffer.SetRenderTarget(BuiltinRenderTextureType.CurrentActive, BuiltinRenderTextureType.CameraTarget);

        
        buffer.DrawMesh(this.quadMesh, this.transform.localToWorldMatrix, this.material);
        Debug.Log(" RENDER SURFACE COMMAND");

        
    }
}
