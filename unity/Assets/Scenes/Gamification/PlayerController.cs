using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    public Camera playerCamera;
    public Transform cameraThirdPersonReference;
    public Transform cameraLookAtReference;
    public float forwardSpeed = 1f;

    bool isRotating;
    bool inFPSMode;

    private void Update()
    {
        // Get the player's current position.
        Vector3 position = transform.position;
        
        if (!isRotating) {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                transform.position += transform.forward * forwardSpeed * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.DownArrow))
            {
                transform.position -= transform.forward * forwardSpeed * Time.deltaTime;
            }
        }
        
        // Get inputs
        if (Input.GetKey(KeyCode.RightArrow) && !isRotating)
        {
            isRotating = !isRotating;
            StartCoroutine(RotatePlayer(45, 0.2f));
        }

        if (Input.GetKey(KeyCode.LeftArrow) && !isRotating)
        {
            isRotating = !isRotating;
            StartCoroutine(RotatePlayer(-45, 0.2f));
        }
    }

     // Coroutine for rotating the player over time.
    IEnumerator RotatePlayer(float rotationAmount, float rotationTime)
    {
        Debug.Log("WORKING!");
        float initialYRotation = transform.eulerAngles.y;
        float currentYRotation;
        for (float i = 0; i < 1; i += Time.deltaTime / rotationTime)
        {
            currentYRotation = Mathf.Lerp(initialYRotation, initialYRotation + rotationAmount, i);
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, currentYRotation, transform.eulerAngles.z);
            yield return null;
        }
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, 45f * Mathf.Round(transform.eulerAngles.y / 45), transform.eulerAngles.z);
        isRotating = false;
        yield return null;
    }

     IEnumerator TurnRight(Vector3 byAngles, float inTime) {
         var fromAngle = transform.rotation;
         var toAngle = Quaternion.Euler(transform.eulerAngles + byAngles);
         for(var t = 0f; t < 1; t += Time.deltaTime/inTime) {
             transform.rotation = Quaternion.Lerp(fromAngle, toAngle, t);
             yield return null;
         }
     }
}