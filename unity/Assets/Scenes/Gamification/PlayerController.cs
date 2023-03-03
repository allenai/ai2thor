using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private GameObject _arm;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject cameraLookAtReference;
 
    public float forwardSpeed = 1f;
    public float rotationTime = 10f;
    private float currentRotationAngle = 0f;
    private bool isRotating = false;

    [SerializeField] private float _speed = 5;
    [SerializeField] private float _turnSpeed = 360;
    private Vector3 _input;
    [SerializeField] private bool FPSMode = false;

    private void Start() {
        currentRotationAngle = transform.eulerAngles.y;
    }

    private void Update() {
    GatherInput();

    // Check for view toggle
        if (Input.GetKeyDown(KeyCode.F)) {
            FPSMode = !FPSMode;
            ToggleView();
        }
        
        Debug.Log(playerCamera.transform.position + " and " + cameraLookAtReference.transform.position);
        //if (playerCamera.transform.position == cameraLookAtReference.transform.position) {
            // Sync camera to lookat reference
            Debug.Log("I'm runnign!");
            playerCamera.transform.eulerAngles = new Vector3 (playerCamera.transform.eulerAngles.x, cameraLookAtReference.transform.eulerAngles.y, playerCamera.transform.eulerAngles.z);
        //}
        

        if (FPSMode == false) {
            Look();
            // MovePlayer();
        } else {
            MoveArm();
        }
    }

    private void FixedUpdate() {
        if (FPSMode == false) {
            MoveBody();
        }
    }

    private void GatherInput() {
        _input = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxis("Mouse ScrollWheel") * 50, Input.GetAxisRaw("Vertical"));
    }

    private void Look() {
        if (_input == Vector3.zero) return;

        var rot = Quaternion.LookRotation(_input.ToIso(), Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, _turnSpeed * Time.deltaTime);
    }

    // private void MovePlayer() {
    //     // If the player is not currently rotating, check for movement and rotation input.
    //     if (!isRotating)
    //     {
    //         // Get the player's current position.
    //         Vector3 position = transform.position;

    //         // Move the player forward when the "Up" key is pressed.
    //         if (Input.GetKey(KeyCode.UpArrow))
    //         {
    //             position += transform.forward * forwardSpeed * Time.deltaTime;
    //         }

    //         // Rotate the player 45 degrees clockwise when the "Right" key is pressed.
    //         if (Input.GetKey(KeyCode.RightArrow))
    //         {
    //             StartCoroutine(RotatePlayer(-45f));
    //         }

    //         // Rotate the player 45 degrees counter-clockwise when the "Left" key is pressed.
    //         if (Input.GetKey(KeyCode.LeftArrow))
    //         {
    //             StartCoroutine(RotatePlayer(45f));
    //         }

    //         // Update the player's position.
    //         transform.position = position;
    //     }
    // }

    private void MoveBody() {
        Vector3 movePosition = transform.position + transform.forward * _input.normalized.magnitude * _speed * Time.deltaTime;
        
        // Move Rigidbody
        _rb.MovePosition(movePosition);

        // Move camera
        playerCamera.transform.position = transform.position + new Vector3(0f, 2.75f, -1.2f);
    }

    private void MoveArm() {

        // Move wrist
        _arm.transform.Translate(_input *  _speed * Time.deltaTime);
    }

    private void ToggleView() {
        float transitionTime = 0.5f;
        if (FPSMode == true) {
            iTween.MoveTo(playerCamera.gameObject, cameraLookAtReference.transform.position, 2f * transitionTime);
            iTween.RotateTo(playerCamera.gameObject, new Vector3(60, _rb.transform.eulerAngles.y, _rb.transform.eulerAngles.z), 2f * transitionTime);
        }
        else {
            iTween.MoveTo(playerCamera.gameObject, transform.position + new Vector3(0f, 2.75f, -1.2f), transitionTime);
            iTween.RotateTo(playerCamera.gameObject, new Vector3(62.5f, 0, 0), transitionTime);
        }
    }

    // Coroutine for rotating the player over time.
    IEnumerator RotatePlayer(float angle)
    {
        // Check if the player is already rotating.
        if (isRotating)
        {
            yield break;
        }

        // Set the rotation flag to true.
        isRotating = true;

        // Calculate the target rotation angle.
        float targetRotationAngle = currentRotationAngle + angle;

        // Normalize the target rotation angle.
        targetRotationAngle = (targetRotationAngle + 360f) % 360f;

        // Calculate the rotation distance.
        float rotationDistance = Mathf.Abs(targetRotationAngle - currentRotationAngle);

        // Calculate the rotation direction.
        int rotationDirection = (int)Mathf.Sign(angle);

        // Calculate the rotation speed based on the desired rotation time and distance.
        float rotationSpeed = rotationDistance / rotationTime;

        // Rotate the player over time.
        while (currentRotationAngle != targetRotationAngle)
        {
            // Calculate the amount to rotate this frame.
            float rotationAmount = rotationSpeed * Time.deltaTime * rotationDirection;

            // Rotate the player by the calculated amount.
            transform.Rotate(Vector3.up, rotationAmount);

            // Update the current rotation angle.
            currentRotationAngle = (currentRotationAngle + rotationAmount + 360f) % 360f;

            // Wait for the next frame.
            yield return null;
        }

        // Set the rotation flag back to false.
        isRotating = false;

        // Normalize the current rotation angle to 360 degrees
        currentRotationAngle = (currentRotationAngle + 360f) % 360f;
    }
}

public static class Helpers
{
    private static Matrix4x4 _isoMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, 0));
    public static Vector3 ToIso(this Vector3 input) => _isoMatrix.MultiplyPoint3x4(input);
}