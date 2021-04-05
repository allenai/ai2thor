using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrackedCameraManager : MonoBehaviour
{
    public List<GameObject> cracks;

    public void SpawnCrack(int seed = 0)
    {
        UnityEngine.Random.InitState(seed);

        GameObject whichCrack = cracks[Random.Range(0, cracks.Count)];
        RectTransform canvasRect = gameObject.GetComponent<RectTransform>();

        float xPos = Random.Range(0, canvasRect.rect.width);
        float yPos = Random.Range(0, canvasRect.rect.height);

        RectTransform crackRect = whichCrack.GetComponent<RectTransform>();

        //random position within the bounds of the canvas's current width and height
        crackRect.anchoredPosition = new Vector2(xPos, yPos);
        //random scale from 2 to 3
        crackRect.localScale = new Vector3(Random.Range(2f, 4f), Random.Range(2f, 4f), 0);
        //random rotation
        crackRect.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360f));

        whichCrack.SetActive(true);
    }
}
