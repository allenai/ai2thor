namespace Oculus.Platform.Samples.VrVoiceChat
{
	using UnityEngine;
	using UnityEngine.UI;
	using System.Collections.Generic;
	using Oculus.Platform;
	using Oculus.Platform.Models;

	// This class coordinates communication with the Oculus Platform
	// Service running in your device.
	public class PlatformManager : MonoBehaviour
	{
		// the game object to build the invite list in
		[SerializeField] private GameObject m_invitesList = null;

		// Button to create for the user to answer an invite call
		[SerializeField] private GameObject m_invitePrefab = null;

		// State transition sets the background color as a visual status indication
		[SerializeField] private Camera m_camera = null;

		// GameObject that represents the Head of the remote Avatar
		[SerializeField] private GameObject m_remoteHead = null;

		private State m_currentState;

		private static PlatformManager s_instance = null;
		private RoomManager m_roomManager;
		private P2PManager m_p2pManager;
		private VoipManager m_voipManager;

		// my Application-scoped Oculus ID
		private ulong m_myID;

		// my Oculus user name
		private string m_myOculusID;

		void Update()
		{
			// occasionally poll for new call invites
			if (m_roomManager.ShouldPollInviteList)
			{
				m_roomManager.UpdateActiveInvitesList();
			}

			// occasionally send my transform to my interlocutor
			if (m_p2pManager.ShouldSendHeadUpdate)
			{
				m_p2pManager.SendHeadTransform(m_camera.transform);
			}

			// estimate the remote avatar transforms from the most recent network update
			m_p2pManager.GetRemoteHeadTransform(m_remoteHead.transform);
		}

		#region Initialization and Shutdown

		void Awake()
		{
			// make sure only one instance of this manager ever exists
			if (s_instance != null) {
				Destroy(gameObject);
				return;
			}

			s_instance = this;
			DontDestroyOnLoad(gameObject);

			TransitionToState(State.INITIALIZING);

			Core.Initialize();

			m_roomManager = new RoomManager();
			m_p2pManager = new P2PManager(m_remoteHead.transform);
			m_voipManager = new VoipManager(m_remoteHead);
		}

		void Start ()
		{
			// First thing we should do is perform an entitlement check to make sure
			// we successfully connected to the Oculus Platform Service.
			Entitlements.IsUserEntitledToApplication().OnComplete(IsEntitledCallback);
		}

		void IsEntitledCallback(Message msg)
		{
			if (msg.IsError)
			{
				TerminateWithError(msg);
				return;
			}

			// Next get the identity of the user that launched the Application.
			Users.GetLoggedInUser().OnComplete(GetLoggedInUserCallback);
		}

		void GetLoggedInUserCallback(Message<User> msg)
		{
			if (msg.IsError)
			{
				TerminateWithError(msg);
				return;
			}

			m_myID = msg.Data.ID;
			m_myOculusID = msg.Data.OculusID;

			TransitionToState(State.WAITING_TO_CALL_OR_ANSWER);

			// If the user launched the app by accepting the notification, then we want to
			// join that room.  Otherwise, start polling for invites.
			m_roomManager.CheckForLaunchInvite();
		}

		void OnApplicationQuit()
		{
			m_roomManager.LeaveCurrentRoom();
			m_p2pManager.Disconnect();
			m_voipManager.Disconnect();
		}

		// For most errors we terminate the Application since this example doesn't make
		// sense if the user is disconnected.
		public static void TerminateWithError(Message msg)
		{
			Debug.Log("Error: " + msg.GetError().Message);
			UnityEngine.Application.Quit();
		}

		#endregion

		#region Properties

		public static State CurrentState
		{
			get
			{
				return s_instance.m_currentState;
			}
		}

		public static ulong MyID
		{
			get
			{
				if (s_instance != null)
				{
					return s_instance.m_myID;
				}
				else
				{
					return 0;
				}
			}
		}

		public static string MyOculusID
		{
			get
			{
				if (s_instance != null && s_instance.m_myOculusID != null)
				{
					return s_instance.m_myOculusID;
				}
				else
				{
					return string.Empty;
				}
			}
		}

		#endregion

		#region Button Clicks

		public void CallFriendOnClick()
		{
			if (CurrentState == State.WAITING_TO_CALL_OR_ANSWER)
			{
				m_roomManager.CreateRoomAndLaunchInviteMenu();
			}
		}

		public void HangupOnClick()
		{
			m_roomManager.LeaveCurrentRoom();
		}

		public void QuitOnClick()
		{
			UnityEngine.Application.Quit();
		}

		public static void AnswerCallOnClick(ulong roomID)
		{
			if (s_instance)
			{
				s_instance.m_roomManager.JoinExistingRoom(roomID);
			}
		}

		#endregion

		#region State Management

		public enum State
		{
			// loading platform library, checking application entitlement,
			// getting the local user info
			INITIALIZING,

			// waiting on the user to invite a friend to chat, or
			// accept an invite sent to them
			WAITING_TO_CALL_OR_ANSWER,

			// in this state we've create a room, and hopefully
			// sent some invites, and we're waiting for a response
			WAITING_FOR_ANSWER,

			// we're in a room as the caller or the callee
			CONNECTED_IN_A_ROOM,

			// shutdown any connections and leave the current room
			HANGUP,
		};

		public static void TransitionToState(State newState)
		{
			Debug.LogFormat("State {0} -> {1}", s_instance.m_currentState, newState);

			if (s_instance && s_instance.m_currentState != newState)
			{
				s_instance.m_currentState = newState;

				// state transition logic
				switch (newState)
				{
					case State.HANGUP:
						s_instance.m_roomManager.LeaveCurrentRoom();
						s_instance.m_p2pManager.Disconnect();
						s_instance.m_voipManager.Disconnect();
						break;

					case State.WAITING_TO_CALL_OR_ANSWER:
						break;

					case State.CONNECTED_IN_A_ROOM:
						s_instance.m_p2pManager.ConnectTo(s_instance.m_roomManager.RemoteUserID);
						s_instance.m_voipManager.ConnectTo(s_instance.m_roomManager.RemoteUserID);
						break;
				}
			}

			// set the background color as a visual aid to the connection status
			SetBackgroundColorForState();
		}

		public static void SetBackgroundColorForState()
		{
			switch (s_instance.m_currentState)
			{
				case State.INITIALIZING:
				case State.HANGUP:
					s_instance.m_camera.backgroundColor = Color.black;
					break;

				case State.WAITING_TO_CALL_OR_ANSWER:
					s_instance.m_camera.backgroundColor = new Color(0f, 0f, .3f);
					break;

				case State.WAITING_FOR_ANSWER:
					s_instance.m_camera.backgroundColor = new Color(0, 0, .6f);
					break;

				case State.CONNECTED_IN_A_ROOM:
					float red = s_instance.m_p2pManager.Connected ? 1.0f : 0;
					float green = s_instance.m_voipManager.Connected ? 1.0f : 0;
					s_instance.m_camera.backgroundColor = new Color(red, green, 1.0f);
					break;
			}
		}

		public static void SetActiveInvites(List<RoomManager.Invite> invites)
		{
			if (s_instance && s_instance.m_invitesList && s_instance.m_invitePrefab)
			{
				// first remove all existing Invites
				foreach (Transform child in s_instance.m_invitesList.transform)
				{
					Destroy(child.gameObject);
				}

				foreach (var invite in invites)
				{
					GameObject button = Instantiate(s_instance.m_invitePrefab) as GameObject;
					button.GetComponentInChildren<Text>().text = invite.OwnerID;
					button.name = invite.RoomID.ToString();
					button.GetComponent<Button>().onClick.AddListener(
						() => PlatformManager.AnswerCallOnClick(invite.RoomID));
					button.transform.SetParent(s_instance.m_invitesList.transform, false);
				}
			}
		}

		#endregion
	}
}
