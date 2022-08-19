using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityStandardAssets.Characters.FirstPerson;

public class TeleportationProviderWithFade : TeleportationProvider {
    [SerializeField] private GameObject _XROrigin;

    public override bool QueueTeleportRequest(TeleportRequest teleportRequest) {
        if (XRManager.Instance.IsFPSMode) {
            XRManager.Instance.ModeText.text = "Cannot Teleprot User \nIn FPS Mode!!!";
            XRManager.Instance.ModeText.color = Color.red;
            //XRManager.Instance.FadeText();
            return false;
        }

        teleportRequest.destinationPosition.y = _XROrigin.transform.position.y;
        StartCoroutine(FadeSequence(teleportRequest));
        return true;
    }


    private IEnumerator FadeSequence(TeleportRequest teleportRequest) {
        // Fade to black
        yield return ScreenFader.Instance?.StartFadeOut();

        //agent.Teleport(teleportRequest.destinationPosition, teleportRequest.destinationRotation.eulerAngles, null, null, false);
        currentRequest = teleportRequest;
        validRequest = true;

        // Fade to clear
        yield return ScreenFader.Instance?.StartFadeIn();
    }
}
