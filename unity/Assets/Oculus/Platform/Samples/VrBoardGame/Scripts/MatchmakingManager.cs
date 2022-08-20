namespace Oculus.Platform.Samples.VrBoardGame
{
	using UnityEngine;
	using Oculus.Platform;
	using Oculus.Platform.Models;
	using UnityEngine.UI;
	using System.Collections.Generic;
	using System;
	using UnityEngine.Assertions;

	// This classes uses the Oculus Matchmaking Service to find opponents of a similar
	// skill and play a match with them.  A skill pool is used with the matchmaking pool
	// to coordinate the skill matching.  Follow the instructions in the Readme to setup
	// the matchmaking pools.
	// The Datastore for the Room is used to communicate between the clients.  This only
	// works for relatively simple games with tolerance for latency.  For more complex
	// or realtime requirements, you'll want to use the Oculus.Platform.Net API.
	public class MatchmakingManager : MonoBehaviour
	{
		// GameController to notify about match completions or early endings
		[SerializeField] private GameController m_gameController = null;

		// Text for the button that controls matchmaking
		[SerializeField] private Text m_matchButtonText = null;

		// Test widget to render matmaking statistics
		[SerializeField] private Text m_infoText = null;

		// name of the Quckmatch Pool configured on the Oculus Developer Dashboard
		// which is expected to have an associated skill pool
		private const string POOL = "VR_BOARD_GAME_POOL";

		// the ID of the room for the current match
		private ulong m_matchRoom;

		// opponent User data
		private User m_remotePlayer;

		// last time we've received a room update
		private float m_lastUpdateTime;

		// how long to wait before polling for updates
		private const float POLL_FREQUENCY = 30.0f;

		private enum MatchRoomState { None, Queued, Configuring, MyTurn, RemoteTurn }

		private MatchRoomState m_state;

		void Start()
		{
			Matchmaking.SetMatchFoundNotificationCallback(MatchFoundCallback);
			Rooms.SetUpdateNotificationCallback(MatchmakingRoomUpdateCallback);

			TransitionToState(MatchRoomState.None);
		}

		void Update()
		{
			switch (m_state)
			{
				case MatchRoomState.Configuring:
				case MatchRoomState.MyTurn:
				case MatchRoomState.RemoteTurn:
					// if we're expecting an update form the remote player and we haven't
					// heard from them in a while, check the datastore just-in-case
					if (POLL_FREQUENCY < (Time.time - m_lastUpdateTime))
					{
						Debug.Log("Polling Room");
						m_lastUpdateTime = Time.time;
						Rooms.Get(m_matchRoom).OnComplete(MatchmakingRoomUpdateCallback);
					}
					break;
			}
		}

		public void MatchButtonPressed()
		{
			switch (m_state)
			{
				case MatchRoomState.None:
					TransitionToState(MatchRoomState.Queued);
					break;

				default:
					TransitionToState(MatchRoomState.None);
					break;
			}
		}

		public void EndMatch(int localScore, int remoteScore)
		{
			switch (m_state)
			{
				case MatchRoomState.MyTurn:
				case MatchRoomState.RemoteTurn:
					var myID = PlatformManager.MyID.ToString();
					var remoteID = m_remotePlayer.ID.ToString();
					var rankings = new Dictionary<string, int>();
					if (localScore > remoteScore)
					{
						rankings[myID] = 1;
						rankings[remoteID] = 2;
					}
					else if (localScore < remoteScore)
					{
						rankings[myID] = 2;
						rankings[remoteID] = 1;
					}
					else
					{
						rankings[myID] = 1;
						rankings[remoteID] = 1;
					}

					// since there is no secure server to simulate the game and report
					// verifiable results, each client needs to independently report their
					// results for the service to compate for inconsistencies
					Matchmaking.ReportResultsInsecure(m_matchRoom, rankings)
						.OnComplete(GenericErrorCheckCallback);
					break;
			}

			TransitionToState(MatchRoomState.None);
		}

		void OnApplicationQuit()
		{
			// be a good matchmaking citizen and leave any queue immediately
			Matchmaking.Cancel();
			if (m_matchRoom != 0)
			{
				Rooms.Leave(m_matchRoom);
			}
		}

		private void TransitionToState(MatchRoomState state)
		{
			var m_oldState = m_state;
			m_state = state;

			switch (m_state)
			{
				case MatchRoomState.None:
					m_matchButtonText.text = "Find Match";
					// the player can abort from any of the other states to the None state
					// so we need to be careful to clean up all state variables
					m_remotePlayer = null;
					Matchmaking.Cancel();
					if (m_matchRoom != 0)
					{
						Rooms.Leave(m_matchRoom);
						m_matchRoom = 0;
					}
					break;

				case MatchRoomState.Queued:
					Assert.AreEqual(MatchRoomState.None, m_oldState);
					m_matchButtonText.text = "Leave Queue";
					Matchmaking.Enqueue2(POOL).OnComplete(MatchmakingEnqueueCallback);
					break;

				case MatchRoomState.Configuring:
					Assert.AreEqual(MatchRoomState.Queued, m_oldState);
					m_matchButtonText.text = "Cancel Match";
					break;

				case MatchRoomState.MyTurn:
				case MatchRoomState.RemoteTurn:
					Assert.AreNotEqual(MatchRoomState.None, m_oldState);
					Assert.AreNotEqual(MatchRoomState.Queued, m_oldState);
					m_matchButtonText.text = "Cancel Match";
					break;
			}
		}

		void MatchmakingEnqueueCallback(Message untyped_msg)
		{
			if (untyped_msg.IsError)
			{
				Debug.Log(untyped_msg.GetError().Message);
				TransitionToState(MatchRoomState.None);
				return;
			}

			Message<MatchmakingEnqueueResult> msg = (Message<MatchmakingEnqueueResult>)untyped_msg;
			MatchmakingEnqueueResult info = msg.Data;
			m_infoText.text = string.Format(
				"Avg Wait Time: {0}s\n" +
				"Max Expected Wait: {1}s\n" +
				"In Last Hour: {2}\n" +
				"Recent Percentage: {3}%",
				info.AverageWait, info.MaxExpectedWait, info.MatchesInLastHourCount,
				info.RecentMatchPercentage);
		}

		void MatchFoundCallback(Message<Room> msg)
		{
			if (msg.IsError)
			{
				Debug.Log(msg.GetError().Message);
				TransitionToState(MatchRoomState.None);
				return;
			}

			if (m_state != MatchRoomState.Queued)
			{
				// ignore callback - user already cancelled
				return;
			}

			// since this example communicates via updates to the datastore, it's vital that
			// we subscribe to room updates
			Matchmaking.JoinRoom(msg.Data.ID, true /* subscribe to update notifications */)
				.OnComplete(MatchmakingJoinRoomCallback);
			m_matchRoom = msg.Data.ID;
		}

		void MatchmakingJoinRoomCallback(Message<Room> msg)
		{
			if (msg.IsError)
			{
				Debug.Log(msg.GetError().Message);
				TransitionToState(MatchRoomState.None);
				return;
			}

			if (m_state != MatchRoomState.Queued)
			{
				// ignore callback - user already cancelled
				return;
			}

			int numUsers = (msg.Data.UsersOptional != null) ? msg.Data.UsersOptional.Count : 0;
			Debug.Log ("Match room joined: " + m_matchRoom + " count: " + numUsers);

			TransitionToState(MatchRoomState.Configuring);

			// only process the room data if the other user has already joined
			if (msg.Data.UsersOptional != null && msg.Data.UsersOptional.Count == 2)
			{
				ProcessRoomData(msg.Data);
			}
		}

		// Room Datastore updates are used to send moves between players.  So if the MatchRoomState
		// is RemoteTurn I'm looking for the other player's move in the Datastore.  If the
		// MatchRoomState is MyTurn I'm waiting for the room ownership to change so that
		// I have authority to write to the datastore.
		void MatchmakingRoomUpdateCallback(Message<Room> msg)
		{
			if (msg.IsError)
			{
				Debug.Log(msg.GetError().Message);
				TransitionToState(MatchRoomState.None);
				return;
			}

			string ownerOculusID = msg.Data.OwnerOptional != null ? msg.Data.OwnerOptional.OculusID : "";
			int numUsers = (msg.Data.UsersOptional != null) ? msg.Data.UsersOptional.Count : 0;

			Debug.LogFormat(
				"Room Update {0}\n" +
				"  Owner {1}\n" +
				"  User Count {2}\n" +
				"  Datastore Count {3}\n",
				msg.Data.ID, ownerOculusID, numUsers, msg.Data.DataStore.Count);

			// check to make sure the room is valid as there are a few odd timing issues (for
			// example when leaving a room) that can trigger an uninteresting update
			if (msg.Data.ID != m_matchRoom)
			{
				Debug.Log("Unexpected room update from: " + msg.Data.ID);
				return;
			}

			ProcessRoomData(msg.Data);
		}

		private void ProcessRoomData(Room room)
		{
			m_lastUpdateTime = Time.time;

			if (m_state == MatchRoomState.Configuring)
			{
				// get the User info for the other player
				if (room.UsersOptional != null)
				{
					foreach (var user in room.UsersOptional)
					{
						if (PlatformManager.MyID != user.ID)
						{
							Debug.Log("Found remote user: " + user.OculusID);
							m_remotePlayer = user;
							break;
						}
					}
				}

				if (m_remotePlayer == null)
					return;

				bool i_go_first = DoesLocalUserGoFirst();
				TransitionToState(i_go_first ? MatchRoomState.MyTurn : MatchRoomState.RemoteTurn);
				Matchmaking.StartMatch(m_matchRoom).OnComplete(GenericErrorCheckCallback);
				m_gameController.StartOnlineMatch(m_remotePlayer.OculusID, i_go_first);
			}

			// if it's the remote player's turn, look for their move in the datastore
			if (m_state == MatchRoomState.RemoteTurn &&
				room.DataStore.ContainsKey(m_remotePlayer.OculusID) &&
				room.DataStore[m_remotePlayer.OculusID] != "")
			{
				// process remote move
				ProcessRemoteMove(room.DataStore[m_remotePlayer.OculusID]);
				TransitionToState(MatchRoomState.MyTurn);
			}

			// If the room ownership transferred to me, we can mark the remote turn complete.
			// We don't do this when the remote move comes in if we aren't yet the owner because
			// the local user will not be able to write to the datastore if they aren't the
			// owner of the room.
			if (m_state == MatchRoomState.MyTurn && room.OwnerOptional != null && room.OwnerOptional.ID == PlatformManager.MyID)
			{
				m_gameController.MarkRemoteTurnComplete();
			}

			if (room.UsersOptional == null || (room.UsersOptional != null && room.UsersOptional.Count != 2))
			{
				Debug.Log("Other user quit the room");
				m_gameController.RemoteMatchEnded();
			}
		}

		private void ProcessRemoteMove(string moveString)
		{
			Debug.Log("Processing remote move string: " + moveString);
			string[] tokens = moveString.Split(':');

			GamePiece.Piece piece = (GamePiece.Piece)Enum.Parse(typeof(GamePiece.Piece), tokens[0]);
			int x = Int32.Parse(tokens[1]);
			int y = Int32.Parse(tokens[2]);

			// swap the coordinates since each player assumes they are player 0
			x = GameBoard.LENGTH_X-1 - x;
			y = GameBoard.LENGTH_Y-1 - y;

			m_gameController.MakeRemoteMove(piece, x, y);
		}

		public void SendLocalMove(GamePiece.Piece piece, int boardX, int boardY)
		{
			string moveString = string.Format("{0}:{1}:{2}", piece.ToString(), boardX, boardY);
			Debug.Log("Sending move: " + moveString);

			var dict = new Dictionary<string, string>();
			dict[PlatformManager.MyOculusID] = moveString;
			dict[m_remotePlayer.OculusID] = "";

			Rooms.UpdateDataStore(m_matchRoom, dict).OnComplete(UpdateDataStoreCallback);
			TransitionToState(MatchRoomState.RemoteTurn);
		}

		private void UpdateDataStoreCallback(Message<Room> msg)
		{
			if (m_state != MatchRoomState.RemoteTurn)
			{
				// ignore calback - user already quit the match
				return;
			}

			// after I've updated the datastore with my move, change ownership so the other
			// user can perform their move
			Rooms.UpdateOwner(m_matchRoom, m_remotePlayer.ID);
		}

		// deterministic but somewhat random selection for who goes first
		private bool DoesLocalUserGoFirst()
		{
			// if the room ID is even, the lower ID goes first
			if (m_matchRoom % 2 == 0)
			{
				return PlatformManager.MyID < m_remotePlayer.ID;
			}
			// otherwise the higher ID goes first
			{
				return PlatformManager.MyID > m_remotePlayer.ID;
			}
		}

		private void GenericErrorCheckCallback(Message msg)
		{
			if (msg.IsError)
			{
				Debug.Log(msg.GetError().Message);
				TransitionToState(MatchRoomState.None);
				return;
			}
		}
	}
}
