
using UnityEngine;

using Random = UnityEngine.Random;

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using UnityEngine.SceneManagement;

namespace UnityStandardAssets.Characters.FirstPerson
{
	[RequireComponent(typeof (CharacterController))]
	public class DebugFPSAgentController : MonoBehaviour
	{
		public float MaxDistance = 1f;

		[SerializeField] private bool m_IsWalking;
		[SerializeField] private float m_WalkSpeed;
		[SerializeField] private float m_RunSpeed;
		[SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
		[SerializeField] private float m_JumpSpeed;
		[SerializeField] private float m_StickToGroundForce;
		[SerializeField] private float m_GravityMultiplier;
		[SerializeField] private MouseLook m_MouseLook;
		[SerializeField] private bool m_UseFovKick;
		[SerializeField] private FOVKick m_FovKick = new FOVKick();
		[SerializeField] private bool m_UseHeadBob;
		[SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
		[SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
		[SerializeField] private float m_StepInterval;
		[SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
		[SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
		[SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.
		public SimObjType[] OpenableTypes = new SimObjType[]{ SimObjType.Fridge, SimObjType.Cabinet, SimObjType.Microwave};
		public SimObjType[] ImmobileTypes = new SimObjType[]{SimObjType.Chair, SimObjType.Toaster, SimObjType.CoffeeMachine, SimObjType.Television, SimObjType.StoveKnob};



		private Camera m_Camera;
		private bool m_Jump;
		private float m_YRotation;
		public bool rotateMouseLook;
		private Vector2 m_Input;
		private Vector3 m_MoveDir = Vector3.zero;
		private CharacterController m_CharacterController;
		private CollisionFlags m_CollisionFlags;
		private bool m_PreviouslyGrounded;
		private Vector3 m_OriginalCameraPosition;
		private float m_StepCycle;
		private float m_NextStep;
		private bool m_Jumping;
		private AudioSource m_AudioSource;

		SimObj[] currentVisibleObjects;
		public bool IsOpenable(SimObj so) {
			return Array.IndexOf (OpenableTypes, so.Type) >= 0 && so.IsAnimated;
		}


		public bool IsPickupable(SimObj so) {
			return !IsOpenable (so) && !so.IsReceptacle && !(Array.IndexOf (ImmobileTypes, so.Type) >= 0);
		}


		private void pickupAllObjects() {
			SimObj[] simObjects = GameObject.FindObjectsOfType (typeof(SimObj)) as SimObj[];

			foreach (SimObj so in simObjects) {
				if (IsPickupable (so) && so.Type != SimObjType.Bread) {
					SimUtil.TakeItem(so);
				} 

				if (so.Type == SimObjType.Mug) {
					Debug.Log ("got visibility mug: " + so.UniqueID);
				}
			} 
		}
		#if UNITY_EDITOR
		//used to show what's currently visible
		void OnGUI () {
			if (currentVisibleObjects != null) {
				if (currentVisibleObjects.Length > 10) {
					int horzIndex = -1;
					GUILayout.BeginHorizontal ();
					foreach (SimObj o in currentVisibleObjects) {
						horzIndex++;
						if (horzIndex >= 3) {
							GUILayout.EndHorizontal ();
							GUILayout.BeginHorizontal ();
							horzIndex = 0;
						}
						GUILayout.Button (o.UniqueID, UnityEditor.EditorStyles.miniButton, GUILayout.MaxWidth (200f));
					}
					GUILayout.EndHorizontal ();
				} else {
					Plane[] planes = GeometryUtility.CalculateFrustumPlanes(m_Camera);
					foreach (SimObj o in currentVisibleObjects) {
						string suffix = "";
						Bounds bounds = new Bounds (o.gameObject.transform.position, new Vector3 (0.05f, 0.05f, 0.05f));
						if (GeometryUtility.TestPlanesAABB (planes, bounds)) {
							suffix += " VISIBLE";
						}
							
							
						GUILayout.Button (o.UniqueID + suffix, UnityEditor.EditorStyles.miniButton, GUILayout.MinWidth (100f));
					}
				}
			}
		}
		#endif

		// Use this for initialization
		private void Start()
		{
			m_CharacterController = GetComponent<CharacterController>();
			m_Camera = Camera.main;
			m_OriginalCameraPosition = m_Camera.transform.localPosition;
			m_FovKick.Setup(m_Camera);
			m_HeadBob.Setup(m_Camera, m_StepInterval);
			m_StepCycle = 0f;
			m_NextStep = m_StepCycle/2f;
			m_Jumping = false;
			m_AudioSource = GetComponent<AudioSource>();
			m_MouseLook.Init(transform , m_Camera.transform);






//			pickupAllObjects ();
		}

		private Vector3 nearestGridPoint(GridPoint[] gridPoints, Vector3 target) {
			foreach (GridPoint gp in gridPoints) {
				if (Math.Abs(target.x - gp.x) < 0.01 && Math.Abs(target.z - gp.z) < 0.01) {
					return new Vector3 (gp.x, gp.y, gp.z);
				}
			}

			return new Vector3 ();
		}

		private void moveCharacterGrid(int targetOrientation) {
			float moveMagnitude = 0.25f;
			int currentRotation = (int)Math.Round(transform.rotation.eulerAngles.y, 0);
			Debug.Log ("current rotation" + currentRotation);
			Dictionary<int, Vector3> actionOrientation = new Dictionary<int, Vector3> ();
			actionOrientation.Add (0, new Vector3 (0f, 0f, 1.0f * moveMagnitude));
			actionOrientation.Add (90, new Vector3 (1.0f * moveMagnitude, 0.0f, 0.0f));
			actionOrientation.Add (180, new Vector3 (0f, 0f, -1.0f * moveMagnitude));
			actionOrientation.Add (270, new Vector3 (-1.0f * moveMagnitude, 0.0f, 0.0f));
			int delta = (currentRotation + targetOrientation) % 360;

			string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene ().name;
			string json = ThorChallengeInfo.RawSceneInfo[sceneName];
			SceneConfigurationList sceneConfigList = JsonUtility.FromJson<SceneConfigurationList>(json);
			GridPoint[] gridPoints = sceneConfigList.configs [0].gridPoints;
			Vector3 currentPoint = nearestGridPoint (gridPoints, transform.position);
			Vector3 targetVector = nearestGridPoint (gridPoints, (new Vector3 (currentPoint.x, currentPoint.y, currentPoint.z)) + actionOrientation [delta]);
			Debug.Log ("got target" + targetVector);
			Debug.Log (targetVector == new Vector3 ());


				
//
//			checkMove = true;
//			m_CharacterController.Move ();

		}

		protected bool openSimObj(SimObj so) {
			return updateAnimState (so.Animator, true);
		}

		private bool updateAnimState(Animator anim, bool value) {
			AnimatorControllerParameter param = anim.parameters [0];

			if (anim.GetBool(param.name) == value) {
				return false;
			} else {
				anim.SetBool (param.name, value);
				return true;
			}
		}


		// Update is called once per frame
		public bool thingdone;
		public bool captureScreenshot;
		public int screenshotCounter;
		public bool pickupObject;


		private void Update()	{
			RotateView();


			SimObj[] simObjects = GameObject.FindObjectsOfType (typeof(SimObj)) as SimObj[];


//			if (pickupObject) {
//				GameObject hand = GameObject.Find ("FirstPersonHand");
//
//				foreach (SimObj so in simObjects) {
//					if (so.UniqueID == "Lettuce|+01.36|+00.99|+00.79") {
//						Rigidbody rb = so.GetComponentInChildren (typeof(Rigidbody)) as Rigidbody;
//						rb.useGravity = false;
//						so.transform.position = hand.transform.position;
//						so.transform.parent = this.transform;
//						so.transform.parent = m_CharacterController.transform;
//						
//					}
//				}
//				Vector3 target = new Vector3 (hand.transform.position.x - 0.5f, hand.transform.position.y, hand.transform.position.z);
//				hand.transform.position = Vector3.MoveTowards (hand.transform.position, target, 0.01f);
//			} else {
//				foreach (SimObj so in simObjects) {
//					if (so.UniqueID == "Lettuce|+01.36|+00.99|+00.79") {
//						Rigidbody rb = so.GetComponentInChildren (typeof(Rigidbody)) as Rigidbody;
//						rb.useGravity = true;
//						so.transform.parent = null;
//
//					}
//				}
//			}
//

			if (captureScreenshot) {
				screenshotCounter++;
				StartCoroutine (EmitFrame ());
				captureScreenshot = false;
			}

			// the jump state needs to read here to make sure it is not missed
			if (!m_Jump)
			{
				m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
			}

			if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
			{
				StartCoroutine(m_JumpBob.DoBobCycle());
				m_MoveDir.y = 0f;
				m_Jumping = false;
			}
			if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
			{
				m_MoveDir.y = 0f;
			}

			currentVisibleObjects = SimUtil.GetAllVisibleSimObjs (m_Camera, MaxDistance);


			m_PreviouslyGrounded = m_CharacterController.isGrounded;
		}

		protected byte[] captureScreen() {
			int width = Screen.width;
			int height = Screen.height;

			Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

			// read screen contents into the texture
			tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
			tex.Apply();

			// encode texture into JPG - XXX SHOULD SET QUALITY
			byte[] bytes = tex.EncodeToPNG();
			Destroy(tex);
			return bytes;
		}

		private IEnumerator EmitFrame() {
			yield return new WaitForEndOfFrame ();
			File.WriteAllBytes ("/Users/erick/Desktop/screenshots/screenshot-" + screenshotCounter.ToString () + ".png", captureScreen ());
		}

		private void FixedUpdate()
		{
			float speed;
			GetInput(out speed);
			// always move along the camera forward as it is the direction that it being aimed at
			Vector3 desiredMove = transform.forward*m_Input.y + transform.right*m_Input.x;

			// get a normal for the surface that is being touched to move along it
			RaycastHit hitInfo;
			Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
				m_CharacterController.height/2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
			desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

			m_MoveDir.x = desiredMove.x*speed;
			m_MoveDir.z = desiredMove.z*speed;


			if (m_CharacterController.isGrounded)
			{
				m_MoveDir.y = -m_StickToGroundForce;

				if (m_Jump)
				{
					m_MoveDir.y = m_JumpSpeed;
					PlayJumpSound();
					m_Jump = false;
					m_Jumping = true;
				}
			}
			else
			{
				m_MoveDir += Physics.gravity*m_GravityMultiplier*Time.fixedDeltaTime;
			}
			m_CollisionFlags = m_CharacterController.Move(m_MoveDir*Time.fixedDeltaTime);

			ProgressStepCycle(speed);
			UpdateCameraPosition(speed);

			m_MouseLook.UpdateCursorLock();

			//currentVisibleObjects = SimUtil.GetAllVisibleSimObjs (m_Camera, MaxDistance);

		}


		private void PlayJumpSound()
		{
			m_AudioSource.clip = m_JumpSound;
			m_AudioSource.Play();
		}


		private void ProgressStepCycle(float speed)
		{
			if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
			{
				m_StepCycle += (m_CharacterController.velocity.magnitude + (speed*(m_IsWalking ? 1f : m_RunstepLenghten)))*
					Time.fixedDeltaTime;
			}

			if (!(m_StepCycle > m_NextStep))
			{
				return;
			}

			m_NextStep = m_StepCycle + m_StepInterval;

		}



		private void UpdateCameraPosition(float speed)
		{
			Vector3 newCameraPosition;
			if (!m_UseHeadBob)
			{
				return;
			}
			if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
			{
				m_Camera.transform.localPosition =
					m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
						(speed*(m_IsWalking ? 1f : m_RunstepLenghten)));
				newCameraPosition = m_Camera.transform.localPosition;
				newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
			}
			else
			{
				newCameraPosition = m_Camera.transform.localPosition;
				newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
			}
			m_Camera.transform.localPosition = newCameraPosition;
		}


		private void GetInput(out float speed)
		{
			// Read input
			float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
			float vertical = CrossPlatformInputManager.GetAxis("Vertical");

			bool waswalking = m_IsWalking;

			#if !MOBILE_INPUT
			// On standalone builds, walk/run speed is modified by a key press.
			// keep track of whether or not the character is walking or running
			m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
			#endif
			// set the desired speed to be walking or running
			speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
			m_Input = new Vector2(horizontal, vertical);

			// normalize input if it exceeds 1 in combined length:
			if (m_Input.sqrMagnitude > 1)
			{
				m_Input.Normalize();
			}

			// handle speed change to give an fov kick
			// only if the player is going to a run, is running and the fovkick is to be used
			if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
			{
				StopAllCoroutines();
				StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
			}
		}


		private void RotateView()
		{
			if (!captureScreenshot && rotateMouseLook) {
				m_MouseLook.LookRotation (transform, m_Camera.transform);
			}

		}


		private void OnControllerColliderHit(ControllerColliderHit hit)
		{
			Rigidbody body = hit.collider.attachedRigidbody;
			//dont move the rigidbody if the character is on top of it
			if (m_CollisionFlags == CollisionFlags.Below)
			{
				return;
			}

			if (body == null || body.isKinematic || m_CharacterController == null)
			{
				return;
			}
			body.AddForceAtPosition(m_CharacterController.velocity*0.1f, hit.point, ForceMode.Impulse);
		}
	}
}
