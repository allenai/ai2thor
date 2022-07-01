using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityStandardAssets.Characters.FirstPerson;

public class TeleportationProviderWithFade : TeleportationProvider {
    [SerializeField] private ScreenFader _screenFader = null;
    [SerializeField] private GameObject _XROrigin;

    public override bool QueueTeleportRequest(TeleportRequest teleportRequest) {
        teleportRequest.destinationPosition.y = _XROrigin.transform.position.y;
        StartCoroutine(FadeSequence(teleportRequest));
        return true;
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
