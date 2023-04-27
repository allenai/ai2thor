using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.InputSystem;
using RandomExtensions;

public enum MoveState {Idle = 0, Backward = -1, Forward = 1};

public enum RotateState {Idle = 0, Negative = -1, Positive = 1};

public class TestABController : MonoBehaviour
{
    //use this to set global random object
    protected static System.Random systemRandom = new System.Random();

    public GameObject forceTarget = null;
    public GameObject forceTargetBack = null;

    public bool applyActionNoise = false;
    public float movementGaussian = 0.1f;
    public float rotateGaussian = 0.1f;

    [SerializeField]
    [Header("Control with Keyboard or Action")]
    //ABControlMode controlMode = ABControlMode.Keyboard_Input;

    [Header("Reference to this Articulation Body")]
    public ArticulationBody ab;

    [Header("Speed for movement and rotation")]
    public float moveSpeed;
    public float rotateSpeed;
    private float move;
    private float rotate;

    [Header("Colliders used when moving or rotating")]
    public GameObject MoveColliders;
    public GameObject RotateColliders;

    [Header("Debug Move and Rotate states")]
    public MoveState moveState = MoveState.Idle;
    public RotateState rotateState = RotateState.Idle;
    
    public void Start()
    {
        print(ab.centerOfMass);
        forceTarget.transform.localPosition = ab.centerOfMass;
    }

    // public void OnMove(InputAction.CallbackContext context) 
    // {
    //     if(controlMode != ABControlMode.Keyboard_Input) 
    //     {
    //         return;
    //     }

    //     move = context.ReadValue<float>();
    //     SetMoveState(MoveStateFromInput(move));
    // }

    private void SetMoveState (MoveState state) 
    {
        moveState = state;
    }

    MoveState MoveStateFromInput (float input)
    {
        if (input > 0)
        {
            return MoveState.Forward;
        }
        else if (input < 0)
        {
            return MoveState.Backward;
        }
        else
        {
            return MoveState.Idle;
        }
    }

    // public void OnRotate(InputAction.CallbackContext context)
    // {
    //     if(controlMode != ABControlMode.Keyboard_Input) 
    //     {
    //         return;
    //     }

    //     rotate = context.ReadValue<float>();
    //     SetRotateState(RotateStateFromInput(rotate));
    // }

    private void SetRotateState(RotateState state)
    {
        rotateState = state;
    }

    RotateState RotateStateFromInput (float input)
    {
        if (input > 0)
        {
            return RotateState.Positive;
        }
        else if (input < 0)
        {
            return RotateState.Negative;
        }
        else
        {
            return RotateState.Idle;
        }    
    }

    private void FixedUpdate() 
    {
        //print(ab.centerOfMass);

        UpdateCollidersForMovement();
        Move();
        Rotate();
    }

    private void Update()
    {
        // if(Input.GetKeyDown(KeyCode.Space))
        // {
        //     StartCoroutine(MoveDistanceForward(1.0f));
        // }
    }

    //this is wrong, i'm using kinematic equations assuming constant acceleration which I don't think is right actually
    private IEnumerator MoveDistanceForward(float distance)
    {
        float currentTime = 0f;

        //ok try to move forward <distance>
        //use distance = ((v0 + vf)/2) * (time)

        //grab target final velocity based on move speed
        Vector3 targetVelocity = new Vector3(0, 0, 1);
        targetVelocity *= moveSpeed;

        Debug.Log($"target velocity: {targetVelocity}");
        
        float timeNeeded = ((2*distance)/(targetVelocity.z));
        Debug.Log($"time needed: {timeNeeded}");

        while (currentTime < timeNeeded)
        {
            Debug.Log(currentTime);

            //now apply force of given velocity change over this amount of time???
            Vector3 forcePosition = new Vector3();
            forcePosition = forceTarget.transform.position;
            GameObject targetObject = null;
            targetObject = forceTarget;

            //align direction
            targetVelocity = targetObject.transform.TransformDirection(targetVelocity);

            if(applyActionNoise && targetObject != null)
            {
                //this should probably change to applying a random torque rather than rotating the forward direction
                float dirRandom = Random.Range(-movementGaussian, movementGaussian);
                targetObject.transform.Rotate(0, dirRandom, 0);
            }

            Vector3 currentVelocity = ab.velocity;
            Vector3 velocityChange = (targetVelocity - currentVelocity);


            ab.AddForceAtPosition(velocityChange, forcePosition);

            currentTime += Time.smoothDeltaTime;
            yield return null;
        }

        yield return null;
    }

    void Move()
    {
        if(rotateState == RotateState.Idle && moveState != MoveState.Idle)
        {
            Vector3 forcePosition = new Vector3();

            //prep to apply noise to forward direction if flagged to do so
            GameObject targetObject = null;
            if(moveState == MoveState.Forward)
            {
                forcePosition = forceTarget.transform.position;
                targetObject = forceTarget;
            }

            else if(moveState == MoveState.Backward)
            {
                forcePosition = forceTargetBack.transform.position;
                targetObject = forceTargetBack;
            }
            
            if(applyActionNoise && targetObject != null)
            {
                float dirRandom = Random.Range(-movementGaussian, movementGaussian);
                targetObject.transform.Rotate(0, dirRandom, 0);
            }

            //find target velocity
            Vector3 currentVelocity = ab.velocity;

            Vector3 targetvelocity = new Vector3(0, 0, move);
            targetvelocity *= moveSpeed;

            //allign direction
            targetvelocity = targetObject.transform.TransformDirection(targetvelocity);

            //calculate forces
            Vector3 velocityChange = (targetvelocity - currentVelocity); //this is F = mvt I think?
            //Debug.Log(Time.fixedDeltaTime);

            ab.AddForceAtPosition(velocityChange, forcePosition);
        }
    }

    void Rotate()
    {
        if(rotateState != RotateState.Idle && moveState == MoveState.Idle)
        {
            float rotateAmount;

            if(applyActionNoise)
            {
                float rotRandom = Random.Range(-rotateGaussian, rotateGaussian);
                rotateAmount = rotate + rotRandom;
            }

            else
            {
                rotateAmount = rotate;
            }

            //Debug.Log(rotateAmount);

            if(rotateState != RotateState.Idle && moveState == MoveState.Idle)
            {
                //note 2021 unity adds a forceMode paramater to AddTorque so uhhhh
                ab.AddTorque(Vector3.up * rotateAmount * rotateSpeed);
            }
        }
    }

    public void UpdateCollidersForMovement()
    {
        if(MoveColliders == null)
        {
            return;
        }

        if(RotateColliders == null)
        {
            return;
        }

        //moving forward/backward
        if((moveState == MoveState.Forward || moveState == MoveState.Backward) && rotateState == RotateState.Idle)
        {
            if(MoveColliders.activeSelf == false)
            MoveColliders.SetActive(true);

            if(RotateColliders.activeSelf == true)
            RotateColliders.SetActive(false);
        }

        //rotating
        else if ((rotateState == RotateState.Positive || rotateState == RotateState.Negative) && moveState == MoveState.Idle)
        {
            if(MoveColliders.activeSelf == true)
            MoveColliders.SetActive(false);

            if(RotateColliders.activeSelf == false)
            RotateColliders.SetActive(true);
        }
    }

}
