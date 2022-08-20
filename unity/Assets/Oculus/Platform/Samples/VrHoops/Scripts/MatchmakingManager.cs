namespace Oculus.Platform.Samples.VrHoops
{
	using UnityEngine;
	using System.Collections.Generic;
	using Oculus.Platform;
	using Oculus.Platform.Models;

	// This class coordinates with the Oculus Platform Matchmaking Service to
	// establish a Quickmatch session with one or two other players.
	public class MatchmakingManager
	{
		// the name we setup on the Developer Dashboard for the quickmatch pool
		private const string NORMAL_POOL = "NORMAL_QUICKMATCH";

		// the ID of the Room the matchmaking service sent to join
		private ulong m_matchRoom;

		// the list of players that join the match room.
		// it may not be all the match players since some might disconnect
		// before joining the room, but then again they might disconnect
		// midway through a match as well.
		private readonly Dictionary<ulong, User> m_remotePlayers;

		public MatchmakingManager()
		{
			m_remotePlayers = new Dictionary<ulong, User>();

			Matchmaking.SetMatchFoundNotificationCallback(MatchFoundCallback);
			Rooms.SetUpdateNotificationCallback(MatchmakingRoomUpdateCallback);
		}

		public delegate void OnEnqueueResult(bool successful);
		public delegate Player OnMatchPlayerAdded(int slot, User user);

		private OnEnqueueResult m_enqueueCallback;
		private OnMatchPlayerAdded m_playerCallback;

		public OnEnqueueResult EnqueueResultCallback
		{
			private get { return m_enqueueCallback; }
			set { m_enqueueCallback = value; }
		}

		public OnMatchPlayerAdded MatchPlayerAddedCallback
		{
			private get { return m_playerCallback; }
			set { m_playerCallback = value; }
		}

		public void QueueForMatch()
		{
			Matchmaking.Enqueue (NORMAL_POOL).OnComplete(MatchmakingEnqueueCallback);
		}

		void MatchmakingEnqueueCallback(Message msg)
		{
			if (msg.IsError)
			{
				Debug.Log(msg.GetError().Message);
				EnqueueResultCallback(false);
				return;
			}
		}

		void MatchFoundCallback(Message<Room> msg)
		{
			m_matchRoom = msg.Data.ID;
			Matchmaking.JoinRoom(msg.Data.ID, true).OnComplete(MatchmakingJoinRoomCallback);
		}

		void MatchmakingJoinRoomCallback(Message<Room> msg)
		{
			if (msg.IsError)
			{
				Debug.Log (msg.GetError().Message);
				EnqueueResultCallback(false);
				return;
			}
			Debug.Log ("Match found and room joined " + m_matchRoom);

			EnqueueResultCallback(true);

			// this sample doesn't try to coordinate that all the players see consistent
			// positioning to assigned courts, but that would be a great next feature to add
			int slot = 0;

			if (msg.Data.UsersOptional != null)
			{
				foreach (var user in msg.Data.UsersOptional)
				{
					var player = MatchPlayerAddedCallback(slot++, user);
					if (PlatformManager.MyID != user.ID)
					{
						m_remotePlayers[user.ID] = user;
						PlatformManager.P2P.AddRemotePlayer (player as RemotePlayer);
					}
				}
			}
		}

		void MatchmakingRoomUpdateCallback(Message<Room> msg)
		{
			if (msg.IsError)
			{
				PlatformManager.TerminateWithError(msg);
				return;
			}

			// check to make sure the room is valid as there are a few odd timing issues (for
			// example when leaving a room) that can trigger an uninteresting update
			if (msg.Data.ID == m_matchRoom)
			{
				if (msg.Data.UsersOptional != null)
				{
					foreach (User user in msg.Data.UsersOptional)
					{
						if (PlatformManager.MyID != user.ID && !m_remotePlayers.ContainsKey(user.ID))
						{
							m_remotePlayers[user.ID] = user;
							var player = MatchPlayerAddedCallback(m_remotePlayers.Count, user);
							PlatformManager.P2P.AddRemotePlayer(player as RemotePlayer);
						}
					}
				}
			}
		}

		public void EndMatch()
		{
			if (m_matchRoom != 0)
			{
				Rooms.Leave (m_matchRoom);
				m_remotePlayers.Clear ();
				PlatformManager.P2P.DisconnectAll ();
				m_matchRoom = 0;
			}
		}

		public void LeaveQueue()
		{
			Matchmaking.Cancel();
			EndMatch();
		}
	}
}
