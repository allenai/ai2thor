using System;
using UnityEngine;

public class RotateMoveCamera : MonoBehaviour
{
    public GameObject Camera = null;
    public float minX = -360.0f;
    public float maxX = 360.0f;

    public float minY = -45.0f;
    public float maxY = 45.0f;

    public float sensX = 100.0f;
    public float sensY = 100.0f;

    float rotationY = 0.0f;
    float rotationX = 0.0f;

    float MouseX;
    float MouseY;

    void Update()
    {
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
        var x = Input.GetAxis("Mouse X");
        var y = Input.GetAxis("Mouse Y");
        if (x != MouseX || y != MouseY)
        {
            rotationX += x * sensX * Time.deltaTime;
            rotationY += y * sensY * Time.deltaTime;
            rotationY = Mathf.Clamp(rotationY, minY, maxY);
            MouseX = x;
            MouseY = y;
            Camera.transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
        }
        if (Input.GetKey(KeyCode.W))
        { // al precionar la tecla.W))
            transform.Translate(new Vector3(0, 0, 0.1f)); //cambiar posision.trasladar (aun nuevo vector(usando estas codenadas)
        }
        else {
            if (Input.GetKey(KeyCode.S))
            {
                transform.Translate(new Vector3(0, 0, -0.1f)); //cambiar posision.trasladar (aun nuevo vector(usando estas codenadas)
            }
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.Translate(new Vector3(0.1f, 0, 0)); //cambiar posision.trasladar (aun nuevo vector(usando estas codenadas)
        }
        else {
            if (Input.GetKey(KeyCode.A))
            {
                transform.Translate(new Vector3(-0.1f, 0, 0)); //cambiar posision.trasladar (aun nuevo vector(usando estas codenadas)
            }
        }
        }
}