using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityStandardAssets.Characters.FirstPerson;

[ExecuteInEditMode]
public class FirstPersonCharacterCull : MonoBehaviour {
    private bool _stopCullingThingsForASecond = false;

    public bool StopCullingThingsForASecond {
        get {
            return this._stopCullingThingsForASecond;
        }
        set {
            this._stopCullingThingsForASecond = value;
        }
    }

    public MeshRenderer[] RenderersToHide; // Mesh renderer that you want this script's camera to cull
    public BaseAgentComponent FPSController;

    public void SwitchRenderersToHide(GameObject visibilityCapsule) {
        List<MeshRenderer> renderers = new List<MeshRenderer>();
        foreach (var r in visibilityCapsule.GetComponentsInChildren<MeshRenderer>()) {
            if (r.shadowCastingMode == ShadowCastingMode.Off) {
                renderers.Add(r);
            }
        }

        RenderersToHide = renderers.ToArray();

    }

    void OnPreRender() // Just before this camera starts to render...
    {
        if (!StopCullingThingsForASecond) {

            if (
                FPSController != null
                && FPSController.agent != null
                && (RenderersToHide != null || RenderersToHide.Length != 0)
                && FPSController.agent.IsVisible
            ) { // only do this if visibility capsule has been toggled on
                foreach (MeshRenderer mr in RenderersToHide) {
                    mr.enabled = false; // Turn off renderer
                }
            }
        }

    }

    void OnPostRender() // Immediately after this camera renders...
    {
        if (!StopCullingThingsForASecond) {
            if (
                FPSController != null
                && FPSController.agent != null
                && (RenderersToHide != null || RenderersToHide.Length != 0)
                && FPSController.agent.IsVisible
            ) { // only do this if visibility capsule is toggled on

                foreach (MeshRenderer mr in RenderersToHide) {
                    mr.enabled = true; // Turn it back on
                }
            }
        }
    }

}