using UnityEngine;
using System.Collections;
using UnityStandardAssets.Characters.FirstPerson;

[ExecuteInEditMode]
public class FirstPersonCharacterCull : MonoBehaviour
{
    private bool _stopCullingThingsForASecond = false;

    public bool StopCullingThingsForASecond
    {
        get
        {
            return this._stopCullingThingsForASecond;
        }
        set
        {
            this._stopCullingThingsForASecond = value;
        }
    }

    public MeshRenderer [] RenderersToHide; //Mesh renderer that you want this script's camera to cull
    public PhysicsRemoteFPSAgentController FPSController;

    //references to renderers for when Agent is in Tall mode
    public MeshRenderer [] TallRenderers;
    //references to renderers for when the Agent is in Bot mode
    public MeshRenderer [] BotRenderers;
    //references to renderers for when agent is in Drone mode
    public MeshRenderer [] DroneRenderers;

    public void SwitchRenderersToHide(string mode)
    {
        if(mode == "default")
        RenderersToHide = TallRenderers;

        else if(mode == "bot")
        RenderersToHide = BotRenderers;

        else if(mode == "drone")
        RenderersToHide = DroneRenderers;
    }

    void OnPreRender() //Just before this camera starts to render...
    {
        if(!StopCullingThingsForASecond)
        {
            if(FPSController != null && (RenderersToHide != null || RenderersToHide.Length != 0) 
                && FPSController.IsVisible)//only do this if visibility capsule has been toggled on
            {
                foreach (MeshRenderer mr in RenderersToHide)
                {
                    mr.enabled = false; //Turn off renderer
                }
            }
        }

    }

    void OnPostRender() //Immediately after this camera renders...
    {
        if(!StopCullingThingsForASecond)
        {
            if(FPSController != null && (RenderersToHide != null || RenderersToHide.Length != 0)
                && FPSController.IsVisible)//only do this if visibility capsule is toggled on
            {
                foreach (MeshRenderer mr in RenderersToHide)
                {
                    mr.enabled = true; //Turn it back on
                }
            }
        }
    }

}