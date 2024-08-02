using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public enum DecalType
{
    DIFFUSE_ONLY,
    EMISSIVE_SPECULAR,
    NORMAL_DIFFUSE,
    ALL,
    FORWARD // TODO: not supported
}

public class DeferredDecal : MonoBehaviour
{
    [SerializeField]
    public Material material;

    [SerializeField]
    private Mesh cubeMesh = null;

    [SerializeField]
    private DecalType type = DecalType.DIFFUSE_ONLY;

    [SerializeField]
    private CameraEvent atRenderEvent = CameraEvent.BeforeLighting;
    private CommandBuffer buffer;

    private List<Camera> cameras;

    void Start()
    {
        this.buffer = new CommandBuffer();
        buffer.name = "Deferred Decals";
        var sceneM = GameObject.Find("PhysicsSceneManager");
        var manager = sceneM.GetComponent<AgentManager>();
        // Debug.Log("agents")
        // This doesn't work as `agents` only has SecondaryCamera for stretch, bug to rework agentmanager
        // this.cameras = manager.agents.Select(a => a.gameObject.GetComponentInChildren<Camera>()).ToList();//.Concat(manager.thirdPartyCameras).ToList();
        // Debug.Log($"agents { manager.agents.Count} names { string.Join(", ", manager.agents.Select(a => a.gameObject.name))} cams {string.Join(", ", cameras.Select(a => a.gameObject.name))}" );

        this.cameras = new List<Camera>() { manager.primaryAgent.m_Camera }
            .Concat(manager.thirdPartyCameras)
            .ToList();
        foreach (var cam in cameras)
        {
            cam.AddCommandBuffer(atRenderEvent, buffer);
        }
    }

    public void OnWillRenderObject()
    {
        // Happens when editor swap in code
        if (buffer == null)
        {
            buffer = new CommandBuffer();
            buffer.name = "Deferred Decals";
        }
        buffer.Clear();

        if (type == DecalType.EMISSIVE_SPECULAR)
        {
            // Diffuse + specular decals
            RenderTargetIdentifier[] multipleRenderTargets =
            {
                BuiltinRenderTextureType.GBuffer0,
                BuiltinRenderTextureType.GBuffer1,
                BuiltinRenderTextureType.GBuffer3
            };
            buffer.SetRenderTarget(multipleRenderTargets, BuiltinRenderTextureType.CameraTarget);
        }
        else if (type == DecalType.NORMAL_DIFFUSE)
        {
            // For decals that have normals
            RenderTargetIdentifier[] multipleRenderTargets =
            {
                BuiltinRenderTextureType.GBuffer0,
                BuiltinRenderTextureType.GBuffer2
            };
            buffer.SetRenderTarget(multipleRenderTargets, BuiltinRenderTextureType.CameraTarget);
        }
        else if (type == DecalType.EMISSIVE_SPECULAR)
        {
            // All render targets
            RenderTargetIdentifier[] multipleRenderTargets =
            {
                BuiltinRenderTextureType.GBuffer0,
                BuiltinRenderTextureType.GBuffer1,
                BuiltinRenderTextureType.GBuffer2,
                BuiltinRenderTextureType.GBuffer3
            };
            buffer.SetRenderTarget(multipleRenderTargets, BuiltinRenderTextureType.CameraTarget);
        }
        else if (type == DecalType.DIFFUSE_ONLY)
        {
            // Diffuse only, no MTR
            buffer.SetRenderTarget(
                BuiltinRenderTextureType.GBuffer0,
                BuiltinRenderTextureType.CameraTarget
            );
        }
        else if (type == DecalType.FORWARD)
        {
            buffer.SetRenderTarget(
                BuiltinRenderTextureType.CurrentActive,
                BuiltinRenderTextureType.CameraTarget
            );
        }

        buffer.DrawMesh(this.cubeMesh, this.transform.localToWorldMatrix, this.material);
    }

    private void OnDisable()
    {
        foreach (var cam in cameras)
        {
            cam.RemoveCommandBuffer(atRenderEvent, buffer);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0.92f, 0.016f, 0.2f);
        Gizmos.DrawMesh(
            this.cubeMesh,
            this.transform.position,
            this.transform.rotation,
            this.transform.localScale
        );

        Gizmos.color = new Color(1, 0.92f, 0.016f, 0.6f);

        Vector3[] basePoints =
        {
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, +0.5f, +0.5f),
            new Vector3(+0.5f, +0.5f, -0.5f),
            new Vector3(+0.5f, -0.5f, +0.5f)
        };

        for (var i = 0; i < 4; i++)
        {
            for (var j = 0; j < 3; j++)
            {
                var from = basePoints[i];
                var to = from;
                to[j] += -Mathf.Sign(to[j]) * 1.0f;

                Gizmos.DrawLine(
                    this.transform.TransformPoint(from),
                    this.transform.TransformPoint(to)
                );
            }
        }
    }
}
