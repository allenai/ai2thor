using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    public Camera playerCamera;
    public Transform playerArmManip;
    public Transform cameraThirdPersonReference;
    public Transform cameraLookAtReference;
    public float swapTime = 0.25f;
    public float armSpeed = 0.5f;
    public float forwardSpeed = 1f;
    public float rotationTime = 0.25f;

    bool isRotating;
    public bool isViewSwapping;
    public bool inFPSMode;

    private void Update()
    {
        // Sync camera to reference's current position.
        if (!isRotating && !isViewSwapping) {
            if (inFPSMode == false) {
                playerCamera.transform.position = cameraThirdPersonReference.transform.position;
                playerCamera.transform.rotation = cameraThirdPersonReference.transform.rotation;
            } else {
                playerCamera.transform.position = cameraLookAtReference.transform.position;
                playerCamera.transform.rotation = cameraLookAtReference.transform.rotation;
            }
        }

        // View-swap Inputs
        if (Input.GetKey(KeyCode.F) && isViewSwapping == false) {
            isViewSwapping = !isViewSwapping;
            StartCoroutine(SwapView(swapTime));
        }
        
        // Third-person controls
        if (!inFPSMode)
        {
            // Movement inputs
            if (!isRotating) {
                if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
                {
                    transform.position += transform.forward * forwardSpeed * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
                {
                    transform.position -= transform.forward * forwardSpeed * Time.deltaTime;
                }
            }

            // Rotation Inputs
            if ((Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) ) && !isRotating)
            {
                isRotating = !isRotating;
                StartCoroutine(RotateSmooth(transform, -45, rotationTime));
            }
            if ((Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) ) && !isRotating)
            {
                isRotating = !isRotating;
                StartCoroutine(RotateSmooth(transform, 45, rotationTime));
            }

        // FPS Controls
        } else {
            // Movement inputs
            if (!isRotating) {
                if (Input.GetKey(KeyCode.W))
                {
                    playerArmManip.position += transform.forward * armSpeed * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.S))
                {
                    playerArmManip.position -= transform.forward * armSpeed * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.A))
                {
                    playerArmManip.position -= transform.right * armSpeed * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.D))
                {
                    playerArmManip.position += transform.right * armSpeed * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    playerArmManip.position += transform.up * armSpeed * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.DownArrow))
                {
                    playerArmManip.position -= transform.up * armSpeed * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    isRotating = !isRotating;
                    StartCoroutine(RotateSmooth(playerArmManip, -45, rotationTime/2));
                }
                if (Input.GetKey(KeyCode.RightArrow))
                {
                    isRotating = !isRotating;
                    StartCoroutine(RotateSmooth(playerArmManip, 45, rotationTime/2));
                }

            }
        }
    }

     // Coroutine for rotating the player over time.
    IEnumerator RotateSmooth(Transform subject, float rotationAmount, float rotationTime)
    {
        float initialYRotation = subject.eulerAngles.y;
        float currentYRotation;
        for (float i = 0; i < 1; i += Time.deltaTime / rotationTime)
        {
            currentYRotation = Mathf.Lerp(initialYRotation, initialYRotation + rotationAmount, Mathf.SmoothStep(0f, 1f, i));
            subject.eulerAngles = new Vector3(subject.eulerAngles.x, currentYRotation, subject.eulerAngles.z);
            yield return null;
        }

        subject.eulerAngles = new Vector3(subject.eulerAngles.x, 45f * Mathf.Round(subject.eulerAngles.y / 45), subject.eulerAngles.z);
        isRotating = false;
        yield return null;
    }

    IEnumerator SwapView(float swapTime)
    {
        playerCamera.transform.parent = null;
        Transform destination;
        Vector3 startPosition = playerCamera.transform.position;
        Quaternion startRotation = playerCamera.transform.rotation;

        if (inFPSMode == true) {
            destination = cameraThirdPersonReference;
        } else {
            destination = cameraLookAtReference;
        }

        for (float i = 0; i < 1; i += Time.deltaTime / swapTime)
        {
            playerCamera.transform.position = Vector3.Lerp(startPosition, destination.position, Mathf.SmoothStep(0f, 1f, i));
            playerCamera.transform.rotation = Quaternion.Lerp(startRotation, destination.rotation, Mathf.SmoothStep(0f, 1f, i));
            yield return null;
        }

        isViewSwapping = false;
        inFPSMode = !inFPSMode;

        if (inFPSMode == true) {
            playerCamera.transform.parent = cameraThirdPersonReference;
        } else {
            playerCamera.transform.parent = cameraLookAtReference;
        }

        yield return null;
    }
}