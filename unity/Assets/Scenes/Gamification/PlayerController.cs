using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    private CharacterController controller;
    public Camera playerCamera;
    public Transform playerArmManip;
    public Transform cameraThirdPersonReference;
    public Transform cameraLookAtReference;
    public SphereCollider tractorInfluence;
    public Transform audioManager;
    public float swapTime = 0.25f;
    public float armSpeed = 0.5f;
    public float forwardSpeed = 1f;
    public float rotationTime = 0.25f;
    public float dioramaModeFOV = 60f;
    public float FPSModeFOV = 70f;
    public bool isRotating;
    bool isViewSwapping;
    bool inFPSMode;
    bool isCarrying;
    List<Transform> pickedUpObjects = new List<Transform>();

    private void Start()
    {
        controller = gameObject.GetComponent<CharacterController>();
    }

    private void Update()
    {
        // Sync camera to reference's current position.
        // if (!isRotating && !isViewSwapping) {
        //     if (inFPSMode == false) {
        //         playerCamera.transform.position = cameraThirdPersonReference.transform.position;
        //         playerCamera.transform.rotation = cameraThirdPersonReference.transform.rotation;
        //     } else {
        //         playerCamera.transform.position = cameraLookAtReference.transform.position;
        //         playerCamera.transform.rotation = cameraLookAtReference.transform.rotation;
        //     }
        // }

        // View-swap Inputs
        if (Input.GetKey(KeyCode.Space) && isViewSwapping == false) {
            isViewSwapping = !isViewSwapping;
            StartCoroutine(SwapView(swapTime));
        }
        
        // Third-person controls
        if (!inFPSMode)
        {
            // Movement inputs
            if (!isRotating) {
                if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) {
                    audioManager.transform.Find("Slide").GetComponent<AudioSource>().enabled = true;
                    controller.Move(transform.forward * forwardSpeed * Time.deltaTime);
                }
                if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) {
                    audioManager.transform.Find("Slide").GetComponent<AudioSource>().enabled = true;
                    controller.Move(-transform.forward * forwardSpeed * Time.deltaTime);
                }
            }

            // Rotation Inputs
            if ((Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) ) && !isRotating) {
                isRotating = !isRotating;
                StartCoroutine(RotateSmooth(transform, -45, rotationTime));
            }
            if ((Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) ) && !isRotating) {
                isRotating = !isRotating;
                StartCoroutine(RotateSmooth(transform, 45, rotationTime));
            }

        // FPS Controls
        } else {
            // Movement inputs
            if (!isRotating) {
                if (Input.GetKey(KeyCode.W)) {
                    audioManager.transform.Find("Slide").GetComponent<AudioSource>().enabled = true;
                    controller.Move(transform.right * Vector3.Dot(transform.right, playerArmManip.forward) * armSpeed * Time.deltaTime);
                    playerArmManip.position += (playerArmManip.forward - transform.right * Vector3.Dot(transform.right, playerArmManip.forward)) * armSpeed * Time.deltaTime;
                } if (Input.GetKey(KeyCode.S)) {
                    audioManager.transform.Find("Slide").GetComponent<AudioSource>().enabled = true;
                    controller.Move(-transform.right * Vector3.Dot(transform.right, playerArmManip.forward) * armSpeed * Time.deltaTime);
                    playerArmManip.position -= (playerArmManip.forward - transform.right * Vector3.Dot(transform.right, playerArmManip.forward)) * armSpeed * Time.deltaTime;
                } if (Input.GetKey(KeyCode.A)) {
                    audioManager.transform.Find("Slide").GetComponent<AudioSource>().enabled = true;
                    controller.Move(-transform.right * Vector3.Dot(transform.right, playerArmManip.right) * armSpeed * Time.deltaTime);
                    playerArmManip.position -= (playerArmManip.right - transform.right * Vector3.Dot(transform.right, playerArmManip.right)) * armSpeed * Time.deltaTime;
                } if (Input.GetKey(KeyCode.D)) {
                    audioManager.transform.Find("Slide").GetComponent<AudioSource>().enabled = true;
                    controller.Move(transform.right * Vector3.Dot(transform.right, playerArmManip.right) * armSpeed * Time.deltaTime);
                    playerArmManip.position += ( playerArmManip.right - transform.right * Vector3.Dot(transform.right, playerArmManip.right)) * armSpeed * Time.deltaTime;
                } if (Input.GetKey(KeyCode.UpArrow)) {
                    audioManager.transform.Find("Slide").GetComponent<AudioSource>().enabled = true;
                    playerArmManip.position += playerArmManip.up * armSpeed * Time.deltaTime;
                } if (Input.GetKey(KeyCode.DownArrow)) {
                    audioManager.transform.Find("Slide").GetComponent<AudioSource>().enabled = true;
                    playerArmManip.position -= playerArmManip.up * armSpeed * Time.deltaTime;
                } if (Input.GetKey(KeyCode.LeftArrow)) {
                    isRotating = !isRotating;
                    StartCoroutine(RotateSmooth(playerArmManip, -45f, rotationTime/2));
                } if (Input.GetKey(KeyCode.RightArrow)) {
                    isRotating = !isRotating;
                    StartCoroutine(RotateSmooth(playerArmManip, 45f, rotationTime/2));
                } if (Input.GetKeyDown(KeyCode.F)) {
                    // Nothing picked up yet
                    if (!isCarrying) {
                        isCarrying = !isCarrying;
                        audioManager.transform.Find("Thud").GetComponent<AudioSource>().Play();
                        audioManager.transform.Find("Tractor").GetComponent<AudioSource>().enabled = true;
                        foreach (Collider overlappedObject in Physics.OverlapSphere(tractorInfluence.transform.TransformPoint(tractorInfluence.center), tractorInfluence.radius)) {
                            if (
                                //overlappedObject.gameObject.name != "stretch_robot_grp" &&
                                overlappedObject.GetType() != typeof(MeshCollider) &&
                                overlappedObject.isTrigger != true &&
                                overlappedObject.gameObject.isStatic != true
                                ) {
                                    Transform pickupCandidate = overlappedObject.transform;
                                    Debug.Log(pickupCandidate.gameObject.name);
                                    while (pickupCandidate.parent != GameObject.Find("Objects").transform) {
                                        pickupCandidate = pickupCandidate.parent.transform;
                                    }
                                    pickedUpObjects.Add(pickupCandidate);
                            }
                        }
                        pickedUpObjects = pickedUpObjects.Distinct().ToList();
                        foreach (Transform pickedUpObject in pickedUpObjects) {
                            pickedUpObject.SetParent(tractorInfluence.transform);
                            pickedUpObject.GetComponent<Rigidbody>().isKinematic = true;
                        }
                    }
                    // Stuff picked up
                    else {
                        isCarrying = !isCarrying;
                        audioManager.transform.Find("Thud").GetComponent<AudioSource>().Play();
                        audioManager.transform.Find("Tractor").GetComponent<AudioSource>().enabled = false;
                        foreach (Transform pickedUpObject in pickedUpObjects) {
                            pickedUpObject.SetParent(GameObject.Find("Objects").transform);
                            pickedUpObject.GetComponent<Rigidbody>().isKinematic = false;
                        }
                        pickedUpObjects.Clear();
                    }
                }
            }
        }

        if (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.D) ||
            Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.DownArrow) || Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.RightArrow)) {
            audioManager.transform.Find("Slide").GetComponent<AudioSource>().enabled = false;
            audioManager.transform.Find("Arm_Out").GetComponent<AudioSource>().Play();
        }
    }

     // Coroutine for rotating the player over time.
    IEnumerator RotateSmooth(Transform subject, float rotationAmount, float rotationTime)
    {
        audioManager.transform.Find("Shutter").GetComponent<AudioSource>().Play();
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
        audioManager.transform.Find("Zoom").GetComponent<AudioSource>().Play();
        playerCamera.transform.parent = null;
        Transform destination;
        Vector3 startPosition = playerCamera.transform.position;
        Quaternion startRotation = playerCamera.transform.rotation;
        float startFOV = playerCamera.fieldOfView;
        float endFOV;

        if (inFPSMode == true) {
            destination = cameraThirdPersonReference;
            endFOV = dioramaModeFOV;
        } else {
            destination = cameraLookAtReference;
            endFOV = FPSModeFOV;
        }

        for (float i = 0; i < 1; i += Time.deltaTime / swapTime)
        {
            playerCamera.transform.position = Vector3.Lerp(startPosition, destination.position, Mathf.SmoothStep(0f, 1f, i));
            playerCamera.transform.rotation = Quaternion.Lerp(startRotation, destination.rotation, Mathf.SmoothStep(0f, 1f, i));
            playerCamera.fieldOfView = Mathf.Lerp(startFOV, endFOV, Mathf.SmoothStep(0f, 1f, i));
            yield return null;
        }

        isViewSwapping = false;
        inFPSMode = !inFPSMode;

        if (inFPSMode == true) {
            playerCamera.transform.parent = cameraLookAtReference;
        } else {
            playerCamera.transform.parent = cameraThirdPersonReference;
        }

        yield return null;
    }
}