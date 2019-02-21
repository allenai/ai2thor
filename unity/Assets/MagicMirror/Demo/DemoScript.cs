using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class DemoScript : MonoBehaviour
{
    public List<GameObject> Mirrors;
    public GameObject LightBulb;
    public UnityEngine.UI.Toggle RecursionToggle;

    private float rotationModifier = -1.0f;
    private float moveModifier = 1.0f;
    private Material lightBulbMaterial;

    private enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
    private RotationAxes axes = RotationAxes.MouseXAndY;
    private float sensitivityX = 15F;
    private float sensitivityY = 15F;
    private float minimumX = -360F;
    private float maximumX = 360F;
    private float minimumY = -60F;
    private float maximumY = 60F;
    private float rotationX = 0F;
    private float rotationY = 0F;
    private Quaternion originalRotation;

    // Use this for initialization
    void Start()
    {
        originalRotation = transform.localRotation;
        Renderer r = LightBulb.GetComponent<Renderer>();
        if (Application.isPlaying)
        {
            r.sharedMaterial = r.material;
        }
        lightBulbMaterial = r.sharedMaterial;
    }

    // Update is called once per frame
    void Update()
    {
        RotateMirror();
        MoveLightBulb();
        UpdateMouseLook();
        UpdateMovement();
    }

    public void MirrorRecursionToggled()
    {
        ChangeMirrorRecursion();
    }

    public void ChangeMirrorRecursion()
    {
        foreach (GameObject o in Mirrors)
        {
            MirrorScript s = o.GetComponent<MirrorScript>();
            s.MirrorRecursion = RecursionToggle.isOn;
        }
    }

    private void UpdateMovement()
    {
        float speed = 4.0f * Time.deltaTime;

        if (Input.GetKey(KeyCode.W))
        {
            transform.Translate(0.0f, 0.0f, speed);
        }
        else if (Input.GetKey(KeyCode.S))
        {
            transform.Translate(0.0f, 0.0f, -speed);
        }

        if (Input.GetKey(KeyCode.A))
        {
            transform.Translate(-speed, 0.0f, 0.0f);
        }
        else if (Input.GetKey(KeyCode.D))
        {
            transform.Translate(speed, 0.0f, 0.0f);
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            RecursionToggle.isOn = !RecursionToggle.isOn;
        }
    }

    private void RotateMirror()
    {
        GameObject Mirror = Mirrors[0];
        float angle = Mirror.transform.rotation.eulerAngles.y;
        if (angle > 65 && angle < 100)
        {
            rotationModifier = -rotationModifier;
            angle = angle - 65.0f;
            Mirror.transform.Rotate(0.0f, -angle, 0.0f);
        }
        else if (angle > 100 && angle < 295)
        {
            rotationModifier = -rotationModifier;
            angle = 295.0f - angle;
            Mirror.transform.Rotate(0.0f, angle, 0.0f);
        }
        else
        {
            Mirror.transform.Rotate(0.0f, rotationModifier * Time.deltaTime * 20.0f, 0.0f);
        }
    }

    private void MoveLightBulb()
    {
        float x = LightBulb.transform.position.x;
        if (x > 5)
        {
            moveModifier = -moveModifier;
            x = 5;
        }
        else if (x < -5)
        {
            moveModifier = -moveModifier;
            x = -5;
        }
        else
        {
            x += (Time.deltaTime * moveModifier);
        }

        Light l = LightBulb.GetComponent<Light>();
        LightBulb.transform.position = new Vector3(x, LightBulb.transform.position.y, LightBulb.transform.position.z);
        float i = Mathf.Min(1.0f, l.intensity);
        lightBulbMaterial.SetColor("_EmissionColor", new Color(i, i, i));
    }

    private void UpdateMouseLook()
    {
        if (axes == RotationAxes.MouseXAndY)
        {
            // Read the mouse input axis
            rotationX += Input.GetAxis("Mouse X") * sensitivityX;
            rotationY += Input.GetAxis("Mouse Y") * sensitivityY;

            rotationX = ClampAngle(rotationX, minimumX, maximumX);
            rotationY = ClampAngle(rotationY, minimumY, maximumY);

            Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
            Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, -Vector3.right);

            transform.localRotation = originalRotation * xQuaternion * yQuaternion;
        }
        else if (axes == RotationAxes.MouseX)
        {
            rotationX += Input.GetAxis("Mouse X") * sensitivityX;
            rotationX = ClampAngle(rotationX, minimumX, maximumX);

            Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
            transform.localRotation = originalRotation * xQuaternion;
        }
        else
        {
            rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
            rotationY = ClampAngle(rotationY, minimumY, maximumY);

            Quaternion yQuaternion = Quaternion.AngleAxis(-rotationY, Vector3.right);
            transform.localRotation = originalRotation * yQuaternion;
        }
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
        {
            angle += 360F;
        }
        if (angle > 360F)
        {
            angle -= 360F;
        }

        return Mathf.Clamp(angle, min, max);
    }
}
