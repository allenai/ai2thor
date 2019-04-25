using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class agent_trackball_rotation : MonoBehaviour
{
    float ballPositionToRotationRatio = 3 * Mathf.PI / 1200; //Circumference over 360 euler degrees
    float changeInPosition; //How far agent has moved
    Vector3 currentPosition; //Ball's current position
    Vector3 prevPosition; //Ball's position last frame
    Vector3 directionVector; //Direction of ball's movement
    Vector3 axisOfRotation; //Axis of ball's rotation
    Vector3 rotationVector; //Ball's rotation values

    float currentLookDirection;
    float prevLookDirection;
    float lookRotation;

    void Start() //Initialize "previous" values
    {
        prevPosition = new Vector3(gameObject.transform.position.x, 0, gameObject.transform.position.z);
        prevLookDirection = gameObject.transform.parent.transform.rotation.eulerAngles.y;
    }

    void Update()
    {
        currentPosition = new Vector3(gameObject.transform.position.x, 0, gameObject.transform.position.z); //Ball's current position
        currentLookDirection = gameObject.transform.parent.transform.rotation.eulerAngles.y; //Direction agent is looking in

        directionVector = currentPosition - prevPosition; //Direction of ball's movement

        if (directionVector != Vector3.zero) //If agent is moving...
        {
            changeInPosition = Mathf.Abs(Vector3.Magnitude(directionVector)); //How far agent has moved
            axisOfRotation = Vector3.Cross(Vector3.up, directionVector); //Axis of ball's rotation
            rotationVector = Quaternion.AngleAxis(changeInPosition / ballPositionToRotationRatio, axisOfRotation).eulerAngles; //Ball's rotation values
            gameObject.transform.Rotate(rotationVector, Space.World); //Rotate ball
            prevPosition = currentPosition; //Record position to compare against next frame
        }

        else //If agent is still
        {
            lookRotation = currentLookDirection - prevLookDirection; //How far agent has rotated between frames

            if (lookRotation != 0) //If agent has rotated
            {
                gameObject.transform.Rotate(new Vector3(0, -lookRotation, 0), Space.World); //Ball cancels out rotation of agent, giving the appearance of magnetic levitation
                prevLookDirection = currentLookDirection; //Record rotation to compare against next frame
            }
        }
    }
}