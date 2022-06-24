using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityStandardAssets.Characters.FirstPerson;

public class TeleportationProviderWithFade : TeleportationProvider {
    [SerializeField] private ScreenFader _screenFader = null;
    [SerializeField] private GameObject _XROrigin;
    private VR_FPSAgentController agent;

    public VR_FPSAgentController Agent {
        get => this.agent;
        set => this.agent = value;
    }

    public override bool QueueTeleportRequest(TeleportRequest teleportRequest) {
        teleportRequest.destinationPosition.y = _XROrigin.transform.position.y;
        if (agent.TeleportCheck(teleportRequest.destinationPosition, teleportRequest.destinationRotation.eulerAngles, false)) {
            StartCoroutine(FadeSequence(teleportRequest));
            return true;
        }
        return false;
    }


    private IEnumerator FadeSequence(TeleportRequest teleportRequest) {
        // Fade to black
        yield return _screenFader.StartFadeOut();

        //agent.Teleport(teleportRequest.destinationPosition, teleportRequest.destinationRotation.eulerAngles, null, null, false);
        currentRequest = teleportRequest;
        validRequest = true;

        // Fade to clear
        yield return _screenFader.StartFadeIn();
    }
}
