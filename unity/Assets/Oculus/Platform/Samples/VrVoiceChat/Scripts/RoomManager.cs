namespace Oculus.Platform.Samples.VrVoiceChat
{
	using UnityEngine;
	using System;
	using System.Collections.Generic;
	using Oculus.Platform;
	using Oculus.Platform.Models;

	// Helper class to manage Room creation, membership and invites.
	// Rooms are a mechanism to help Oculus users create a shared experience.
	// Users can only be in one Room at a time.  If the Owner of a room
	// leaves, then ownership is transferred to some other member.
	// Here we use rooms to create the notion of a 'call' to help us
	// invite a Friend and establish a VOIP and P2P connection.
	public class RoomManager
	{
		// the ID of the Room that I'm in
		private ulong m_roomID;

		// the other User in the Room
		private User m_remoteUser;

		// how often I should poll for invites
		private static readonly float INVITE_POLL_FREQ_SECONDS = 5.0f;

		// the next time I should poll Oculus Platform for valid Room Invite requests
		private float m_nextPollTime;

		public struct Invite
		{
			public readonly ulong RoomID;
			public readonly string OwnerID;

			public Invite(ulong roomID, string owner)
			{
				this.RoomID = roomID;
				this.OwnerID = owner;
			}
		}

		// cached list of rooms that I've been invited to and I'm waiting
		// for more information about
		private HashSet<ulong> m_pendingRoomRequests;

		// accumulation list of room invites and the room owner
		private List<Invite> m_invites;

		public RoomManager()
		{
			Rooms.SetRoomInviteAcceptedNotificationCallback(LaunchedFromAcceptingInviteCallback);
			Rooms.SetUpdateNotificationCallback(RoomUpdateCallback);
		}

		public ulong RemoteUserID
		{
			get
			{
				return m_remoteUser != null ? m_remoteUser.ID : 0;
			}
		}

		public String RemoteOculusID
		{
			get
			{
				return m_remoteUser != null ? m_remoteUser.OculusID : String.Empty;
			}
		}

		#region Launched Application from Accepting Invite

		// Callback to check whether the User accepted the invite as
		// a notification which caused the Application to launch.  If so, then
		// we know we need to try to join that room.
		void LaunchedFromAcceptingInviteCallback(Message<string> msg)
		{
			if (msg.IsError)
			{
				PlatformManager.TerminateWithError(msg);
				return;
			}

			Debug.Log("Launched Invite to join Room: " + msg.Data);

			m_roomID = Convert.ToUInt64(msg.GetString());
		}

		// Check to see if the App was launched by accepting the Notication from the main Oculus app.
		// If so, we can directly join that room.  (If it's still available.)
		public bool CheckForLaunchInvite()
		{
			if (m_roomID != 0)
			{
				JoinExistingRoom(m_roomID);
				return true;
			}
			else
			{
				return false;
			}
		}

		#endregion

		#region Create a Room and Invite Friend(s) from the Oculus Universal Menu

		public void CreateRoomAndLaunchInviteMenu()
		{
			Rooms.CreateAndJoinPrivate(RoomJoinPolicy.InvitedUsers, 2, true)
				 .OnComplete(CreateAndJoinPrivateRoomCallback);
		}

		void CreateAndJoinPrivateRoomCallback(Message<Room> msg)
		{
			if (msg.IsError)
			{
				PlatformManager.TerminateWithError(msg);
				return;
			}

			m_roomID = msg.Data.ID;
			m_remoteUser = null;
			PlatformManager.TransitionToState(PlatformManager.State.WAITING_FOR_ANSWER);

			// launch the Room Invite workflow in the Oculus Univeral Menu
			Rooms.LaunchInvitableUserFlow(m_roomID).OnComplete(OnLaunchInviteWorkflowComplete);
		}

		void OnLaunchInviteWorkflowComplete(Message msg)
		{
			if (msg.IsError)
			{
				PlatformManager.TerminateWithError(msg);
				return;
			}
		}

		#endregion

		#region Polling for Invites

		public bool ShouldPollInviteList
		{
			get
			{
				return m_pendingRoomRequests == null && Time.time >= m_nextPollTime;
			}
		}

		public void UpdateActiveInvitesList()
		{
			m_nextPollTime = Time.time + INVITE_POLL_FREQ_SECONDS;
			m_pendingRoomRequests = new HashSet<ulong>();
			m_invites = new List<Invite>();
			Notifications.GetRoomInviteNotifications().OnComplete(GetRoomInviteNotificationsCallback);
		}

		// task 13572454: add the type to callback definition
		void GetRoomInviteNotificationsCallback(Message msg_untyped)
		{
			Message<RoomInviteNotificationList> msg = (Message<RoomInviteNotificationList>)msg_untyped;

			if (msg.IsError)
			{
				PlatformManager.TerminateWithError(msg);
				return;
			}

			// loop over all the rooms we're invited to and request more info
			foreach (RoomInviteNotification invite in msg.Data)
			{
				m_pendingRoomRequests.Add(invite.RoomID);
				Rooms.Get(invite.RoomID).OnComplete(GetRoomInfoCallback);
			}

			if (msg.Data.Count == 0)
			{
				m_pendingRoomRequests = null;
				PlatformManager.SetActiveInvites(m_invites);
			}
		}

		void GetRoomInfoCallback(Message<Room> msg)
		{
			if (msg.IsError)
			{
				PlatformManager.TerminateWithError(msg);
				return;
			}

			if (msg.Data.OwnerOptional != null)
			{
				Invite invite = new Invite(msg.Data.ID, msg.Data.OwnerOptional.OculusID);
				m_pendingRoomRequests.Remove(invite.RoomID);

				// make sure the room still looks usable
				// (e.g. they aren't currently talking to someone)
				if (msg.Data.UsersOptional != null && msg.Data.UsersOptional.Count == 1)
				{
					m_invites.Add(invite);
				}
			}

			// once we're received all the room info, let the platform update
			// its display
			if (m_pendingRoomRequests.Count == 0)
			{
				m_pendingRoomRequests = null;
				PlatformManager.SetActiveInvites(m_invites);
			}
		}

		#endregion

		#region Accept Invite

		public void JoinExistingRoom(ulong roomID)
		{
			Rooms.Join(roomID, true).OnComplete(JoinRoomCallback);
		}

		void JoinRoomCallback(Message<Room> msg)
		{
			if (msg.IsError)
			{
				// is reasonable if caller called more than 1 person, and I didn't answer first
				return;
			}

			string oculusOwnerID = msg.Data.OwnerOptional != null ? msg.Data.OwnerOptional.OculusID : "";
			int numUsers = msg.Data.UsersOptional != null ? msg.Data.UsersOptional.Count : 0;

			Debug.LogFormat("Joined room: {0} owner: {1} count: ",
				msg.Data.ID, oculusOwnerID, numUsers);

			m_roomID = msg.Data.ID;

			// if the caller left while I was in the process of joining, just hangup
			if (msg.Data.UsersOptional == null || msg.Data.UsersOptional.Count != 2)
			{
				PlatformManager.TransitionToState(PlatformManager.State.HANGUP);
			}
			else
			{
				foreach (User user in msg.Data.UsersOptional)
				{
					if (user.ID != PlatformManager.MyID)
					{
						m_remoteUser = user;
					}
				}

				PlatformManager.TransitionToState(PlatformManager.State.CONNECTED_IN_A_ROOM);
			}

			// update the invite list sooner
			m_nextPollTime = Time.time;
		}

		#endregion

		#region Room Updates

		void RoomUpdateCallback(Message<Room> msg)
		{
			if (msg.IsError)
			{
				PlatformManager.TerminateWithError(msg);
				return;
			}

			string oculusOwnerID = msg.Data.OwnerOptional != null ? msg.Data.OwnerOptional.OculusID : "";
			int numUsers = msg.Data.UsersOptional != null ? msg.Data.UsersOptional.Count : 0;

			Debug.LogFormat("Room {0} Update: {1} owner: {2} count: ",
				msg.Data.ID, oculusOwnerID, numUsers);

			// if the Room count is not 2 then the other party has left.
			// We'll just hangup the connection here.
			// If the other User created then room, ownership would switch to me.
			if (msg.Data.UsersOptional == null || msg.Data.UsersOptional.Count != 2)
			{
				PlatformManager.TransitionToState(PlatformManager.State.HANGUP);
			}
			else
			{
				foreach (User user in msg.Data.UsersOptional)
				{
					if (user.ID != PlatformManager.MyID)
					{
						m_remoteUser = user;
					}
				}

				PlatformManager.TransitionToState(PlatformManager.State.CONNECTED_IN_A_ROOM);
			}
		}

		#endregion

		#region Room Exit

		public void LeaveCurrentRoom()
		{
			if (m_roomID != 0)
			{
				Rooms.Leave(m_roomID);
				m_roomID = 0;
				m_remoteUser = null;
			}
			PlatformManager.TransitionToState(PlatformManager.State.WAITING_TO_CALL_OR_ANSWER);
		}

		#endregion
	}
}
