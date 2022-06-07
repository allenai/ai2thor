using UnityEngine;
using System.Collections;

public class FlyThroughController : MonoBehaviour {

	[SerializeField]
	GameObject QueryObject;

	float speed;
	const float ROTATE_SPEED = 2.0f;
	const float MAX_SPEED  = 4.0f;
	const float ACCELERATE = 2.0f;
	const float DECELERATE = 3.0f;
	
	QueryAnimationController.QueryChanAnimationType nowFlyingState;
	QueryAnimationController.QueryChanAnimationType previousFlyingState;

	[SerializeField]
	GameObject groundCollider;
	
	// Use this for initialization
	void Start () {
		
		InitQuery ();
		
	}

	public void InitQuery () {

		speed = 0.0f;
		nowFlyingState = QueryAnimationController.QueryChanAnimationType.FLY_IDLE;
		previousFlyingState = nowFlyingState;
		QueryObject.GetComponent<QueryAnimationController>().ChangeAnimation(QueryAnimationController.QueryChanAnimationType.FLY_IDLE);

	}
	
	
	// Update is called once per frame
	void Update () {
		
		updateMove ();
		
	}
	
	
	void updateMove()
	{
		CharacterController controller = GetComponent<CharacterController>();
		
		// Rotate Right or Left
		if (Input.GetAxis("Horizontal") != 0)
		{
			transform.Rotate(0, Input.GetAxis("Horizontal") * ROTATE_SPEED, 0);
			if (Input.GetAxis("Horizontal") > 0)
			{
				nowFlyingState = QueryAnimationController.QueryChanAnimationType.FLY_TORIGHT;
			}
			else if (Input.GetAxis("Horizontal") < 0)
			{ 
				nowFlyingState = QueryAnimationController.QueryChanAnimationType.FLY_TOLEFT;
			}
		}
		else
		{
			this.transform.localEulerAngles = new Vector3(0, this.transform.localEulerAngles.y, 0);
			nowFlyingState = QueryAnimationController.QueryChanAnimationType.FLY_STRAIGHT;
		}
		
		// Rotate Up or Down
		if (Input.GetAxis("Vertical") != 0)
		{
			transform.Translate(Vector3.up * Input.GetAxis("Vertical") * ROTATE_SPEED *  Time.deltaTime);
			if (Input.GetAxis("Vertical") > 0)
			{
				nowFlyingState = QueryAnimationController.QueryChanAnimationType.FLY_UP;
			}
			else if (Input.GetAxis("Vertical") < 0)
			{ 
				nowFlyingState = QueryAnimationController.QueryChanAnimationType.FLY_DOWN;
			}

			if (this.transform.localPosition.y < groundCollider.transform.localPosition.y)
			{
				this.transform.localPosition = new Vector3 (this.transform.localPosition.x, groundCollider.transform.localPosition.y, this.transform.localPosition.z);
			}
		}
		
		// Move Forward
		Vector3 forwardSpeed = transform.TransformDirection(Vector3.forward * Time.deltaTime * speed);
		controller.Move (forwardSpeed);
		
		// Speed Control
		if (Input.GetKey("x"))
		{
			speed += ACCELERATE * Time.deltaTime;
			if (speed >  MAX_SPEED)
			{
				speed = MAX_SPEED;
			}
		}
		else if (Input.GetKey("z"))
		{
			speed -= DECELERATE * Time.deltaTime;
			if (speed < 0.0f)
			{
				speed = 0.0f;
			}
		}
		
		if (speed == 0.0f)
		{
			nowFlyingState = QueryAnimationController.QueryChanAnimationType.FLY_IDLE;
		}
		
		// ChangeAnimation
		if (previousFlyingState != nowFlyingState)
		{
			QueryObject.GetComponent<QueryAnimationController>().ChangeAnimation(nowFlyingState);
		}
		
		previousFlyingState = nowFlyingState;
		
	}
	

	void OnGUI () {

		GUI.Box( new Rect(30 , 10, 150, 30), "speed = " + speed);
		
	}

}
