namespace Oculus.Platform.Samples.VrHoops
{
	using UnityEngine;
	using System.Collections.Generic;
	using Oculus.Platform;
	using Oculus.Platform.Models;
	using System;
	using UnityEngine.Assertions;

	// This helper class coordinates establishing Peer-to-Peer connections between the
	// players in the match.  It tries to sychronize time between the devices and
	// handles position update messages for the backboard and moving balls.
	public class P2PManager
	{
		#region Member variables

		// helper class to hold data we need for remote players
		private class RemotePlayerData
		{
			// the last received Net connection state
			public PeerConnectionState state;
			// the Unity Monobehaviour
			public RemotePlayer player;
			// offset from my local time to the time of the remote host
			public float remoteTimeOffset;
			// the last ball update remote time, used to disgard out of order packets
			public float lastReceivedBallsTime;
			// remote Instance ID -> local MonoBahaviours for balls we're receiving updates on
			public readonly Dictionary<int, P2PNetworkBall> activeBalls = new Dictionary<int, P2PNetworkBall>();
		}

		// authorized users to connect to and associated data
		private readonly Dictionary<ulong, RemotePlayerData> m_remotePlayers = new Dictionary<ulong, RemotePlayerData>();

		// when to send the next update to remotes on the state on my local balls
		private float m_timeForNextBallUpdate;

		private const byte TIME_SYNC_MESSAGE = 1;
		private const uint TIME_SYNC_MESSAGE_SIZE = 1+4;
		private const int TIME_SYNC_MESSAGE_COUNT = 7;
		private const byte START_TIME_MESSAGE = 2;
		private const uint START_TIME_MESSAGE_SIZE = 1+4;
		private const byte BACKBOARD_UPDATE_MESSAGE = 3;
		private const uint BACKBOARD_UPDATE_MESSAGE_SIZE = 1+4+12+12+12;
		private const byte LOCAL_BALLS_UPDATE_MESSAGE = 4;
		private const uint LOCAL_BALLS_UPDATE_MESSATE_SIZE_MAX = 1+4+(2*Player.MAX_BALLS*(1+4+12+12));
		private const float LOCAL_BALLS_UPDATE_DELAY = 0.1f;
		private const byte SCORE_UPDATE_MESSAGE = 5;
		private const uint SCORE_UPDATE_MESSAGE_SIZE = 1 + 4;

		// cache of local balls that we are sending updates for
		private readonly Dictionary<int, P2PNetworkBall> m_localBalls = new Dictionary<int, P2PNetworkBall>();

		// reusable buffer to read network data into
		private readonly byte[] readBuffer = new byte[LOCAL_BALLS_UPDATE_MESSATE_SIZE_MAX];

		// temporary time-sync cache of the calculated time offsets
		private readonly Dictionary<ulong, List<float>> m_remoteSyncTimeCache = new Dictionary<ulong, List<float>>();

		// temporary time-sync cache of the last sent message
		private readonly Dictionary<ulong, float> m_remoteSentTimeCache = new Dictionary<ulong, float>();

		// the delegate to handle start-time coordination
		private StartTimeOffer m_startTimeOfferCallback;

		#endregion

		public P2PManager()
		{
			Net.SetPeerConnectRequestCallback(PeerConnectRequestCallback);
			Net.SetConnectionStateChangedCallback(ConnectionStateChangedCallback);
		}

		public void UpdateNetwork()
		{
			if (m_remotePlayers.Count == 0)
				return;

			// check for new messages
			Packet packet;
			while ((packet = Net.ReadPacket()) != null)
			{
				if (!m_remotePlayers.ContainsKey(packet.SenderID))
					continue;

				packet.ReadBytes(readBuffer);

				switch (readBuffer[0])
				{
					case TIME_SYNC_MESSAGE:
						Assert.AreEqual(TIME_SYNC_MESSAGE_SIZE, packet.Size);
						ReadTimeSyncMessage(packet.SenderID, readBuffer);
						break;

					case START_TIME_MESSAGE:
						Assert.AreEqual(START_TIME_MESSAGE_SIZE, packet.Size);
						ReceiveMatchStartTimeOffer(packet.SenderID, readBuffer);
						break;

					case BACKBOARD_UPDATE_MESSAGE:
						Assert.AreEqual(BACKBOARD_UPDATE_MESSAGE_SIZE, packet.Size);
						ReceiveBackboardUpdate(packet.SenderID, readBuffer);
						break;

					case LOCAL_BALLS_UPDATE_MESSAGE:
						ReceiveBallTransforms(packet.SenderID, readBuffer, packet.Size);
						break;

					case SCORE_UPDATE_MESSAGE:
						Assert.AreEqual(SCORE_UPDATE_MESSAGE_SIZE, packet.Size);
						ReceiveScoredUpdate(packet.SenderID, readBuffer);
						break;
				}
			}

			if (Time.time >= m_timeForNextBallUpdate && m_localBalls.Count > 0)
			{
				SendLocalBallTransforms();
			}
		}

		#region Connection Management

		// adds a remote player to establish a connection to, or accept a connection from
		public void AddRemotePlayer(RemotePlayer player)
		{
			if (!m_remotePlayers.ContainsKey (player.ID))
			{
				m_remotePlayers[player.ID] = new RemotePlayerData();
				m_remotePlayers[player.ID].state = PeerConnectionState.Unknown;
				m_remotePlayers [player.ID].player = player;

				// ID comparison is used to decide who Connects and who Accepts
				if (PlatformManager.MyID < player.ID)
				{
					Debug.Log ("P2P Try Connect to: " + player.ID);
					Net.Connect (player.ID);
				}
			}
		}

		public void DisconnectAll()
		{
			foreach (var id in m_remotePlayers.Keys)
			{
				Net.Close(id);
			}
			m_remotePlayers.Clear();
		}

		void PeerConnectRequestCallback(Message<NetworkingPeer> msg)
		{
			if (m_remotePlayers.ContainsKey(msg.Data.ID))
			{
				Debug.LogFormat("P2P Accepting Connection request from {0}", msg.Data.ID);
				Net.Accept(msg.Data.ID);
			}
			else
			{
				Debug.LogFormat("P2P Ignoring unauthorized Connection request from {0}", msg.Data.ID);
			}
		}

		void ConnectionStateChangedCallback(Message<NetworkingPeer> msg)
		{
			Debug.LogFormat("P2P {0} Connection state changed to {1}", msg.Data.ID, msg.Data.State);

			if (m_remotePlayers.ContainsKey(msg.Data.ID))
			{
				m_remotePlayers[msg.Data.ID].state = msg.Data.State;

				switch (msg.Data.State)
				{
				case PeerConnectionState.Connected:
					if (PlatformManager.MyID < msg.Data.ID)
					{
						SendTimeSyncMessage(msg.Data.ID);
					}
					break;

				case PeerConnectionState.Timeout:
					if (PlatformManager.MyID < msg.Data.ID)
					{
						Net.Connect(msg.Data.ID);
					}
					break;

				case PeerConnectionState.Closed:
					m_remotePlayers.Remove(msg.Data.ID);
					break;
				}
			}
		}

		#endregion

		#region Time Synchronizaiton

		// This section implements some basic time synchronization between the players.
		// The algorithm is:
		//   -Send a time-sync message and receive a time-sync message response
		//   -Estimate time offset
		//   -Repeat several times
		//   -Average values discarding any statistical anomalies
		// Normally delays would be added in case there is intermittent network congestion
		// however the match times are so short we don't do that here.  Also, if one client
		// pauses their game and Unity stops their simulation, all bets are off for time
		// synchronization.  Depending on the goals of your app, you could either reinitiate
		// time synchronization, or just disconnect that player.

		void SendTimeSyncMessage(ulong remoteID)
		{
			if (!m_remoteSyncTimeCache.ContainsKey(remoteID))
			{
				m_remoteSyncTimeCache[remoteID] = new List<float>();
			}

			float time = Time.realtimeSinceStartup;
			m_remoteSentTimeCache[remoteID] = time;

			byte[] buf = new byte[TIME_SYNC_MESSAGE_SIZE];
			buf[0] = TIME_SYNC_MESSAGE;
			int offset = 1;
			PackFloat(time, buf, ref offset);

			Net.SendPacket(remoteID, buf, SendPolicy.Reliable);
		}

		void ReadTimeSyncMessage(ulong remoteID, byte[] msg)
		{
			if (!m_remoteSentTimeCache.ContainsKey(remoteID))
			{
				SendTimeSyncMessage(remoteID);
				return;
			}

			int offset = 1;
			float remoteTime = UnpackFloat(msg, ref offset);
			float now = Time.realtimeSinceStartup;
			float latency = (now - m_remoteSentTimeCache[remoteID]) / 2;
			float remoteTimeOffset = now - (remoteTime + latency);

			m_remoteSyncTimeCache[remoteID].Add(remoteTimeOffset);

			if (m_remoteSyncTimeCache[remoteID].Count < TIME_SYNC_MESSAGE_COUNT)
			{
				SendTimeSyncMessage(remoteID);
			}
			else
			{
				if (PlatformManager.MyID < remoteID)
				{
					// this client started the sync, need to send one last message to
					// the remote so they can finish their sync calculation
					SendTimeSyncMessage(remoteID);
				}

				// sort the times and remember the median
				m_remoteSyncTimeCache[remoteID].Sort();
				float median = m_remoteSyncTimeCache[remoteID][TIME_SYNC_MESSAGE_COUNT/2];

				// calucate the mean and standard deviation
				double mean = 0;
				foreach (var time in m_remoteSyncTimeCache[remoteID])
				{
					mean += time;
				}
				mean /= TIME_SYNC_MESSAGE_COUNT;

				double std_dev = 0;
				foreach (var time in m_remoteSyncTimeCache[remoteID])
				{
					std_dev += (mean-time)*(mean-time);
				}
				std_dev = Math.Sqrt(std_dev)/TIME_SYNC_MESSAGE_COUNT;

				// time delta is the mean of the values less than 1 standard deviation from the median
				mean = 0;
				int meanCount = 0;
				foreach (var time in m_remoteSyncTimeCache[remoteID])
				{
					if (Math.Abs(time-median) < std_dev)
					{
						mean += time;
						meanCount++;
					}
				}
				mean /= meanCount;
				Debug.LogFormat("Time offset to {0} is {1}", remoteID, mean);

				m_remoteSyncTimeCache.Remove(remoteID);
				m_remoteSentTimeCache.Remove(remoteID);
				m_remotePlayers[remoteID].remoteTimeOffset = (float)mean;

				// now that times are synchronized, lets try to coordinate the
				// start time for the match
				OfferMatchStartTime();
			}
		}

		float ShiftRemoteTime(ulong remoteID, float remoteTime)
		{
			if (m_remotePlayers.ContainsKey(remoteID))
			{
				return remoteTime + m_remotePlayers[remoteID].remoteTimeOffset;
			}
			else
			{
				return remoteTime;
			}
		}

		#endregion

		#region Match Start Coordination

		// Since all the clients will calculate a slightly different start time, this
		// message tries to coordinate the match start time to be the lastest of all
		// the clients in the match.

		// Delegate to coordiate match start times - the return value is our start time
		// and the argument is the remote start time, or 0 if that hasn't been given yet.
		public delegate float StartTimeOffer(float remoteTime);

		public StartTimeOffer StartTimeOfferCallback
		{
			private get { return m_startTimeOfferCallback; }
			set { m_startTimeOfferCallback = value; }
		}

		void OfferMatchStartTime()
		{
			byte[] buf = new byte[START_TIME_MESSAGE_SIZE];
			buf[0] = START_TIME_MESSAGE;
			int offset = 1;
			PackFloat(StartTimeOfferCallback(0), buf, ref offset);

			foreach (var remoteID in m_remotePlayers.Keys)
			{
				if (m_remotePlayers [remoteID].state == PeerConnectionState.Connected)
				{
					Net.SendPacket (remoteID, buf, SendPolicy.Reliable);
				}
			}
		}

		void ReceiveMatchStartTimeOffer(ulong remoteID, byte[] msg)
		{
			int offset = 1;
			float remoteTime = UnpackTime(remoteID, msg, ref offset);
			StartTimeOfferCallback(remoteTime);
		}

		#endregion

		#region Backboard Transforms

		public void SendBackboardUpdate(float time, Vector3 pos, Vector3 moveDir, Vector3 nextMoveDir)
		{
			byte[] buf = new byte[BACKBOARD_UPDATE_MESSAGE_SIZE];
			buf[0] = BACKBOARD_UPDATE_MESSAGE;
			int offset = 1;
			PackFloat(time, buf, ref offset);
			PackVector3(pos, buf, ref offset);
			PackVector3(moveDir, buf, ref offset);
			PackVector3(nextMoveDir, buf, ref offset);

			foreach (KeyValuePair<ulong,RemotePlayerData> player in m_remotePlayers)
			{
				if (player.Value.state == PeerConnectionState.Connected)
				{
					Net.SendPacket(player.Key, buf, SendPolicy.Reliable);
				}
			}
		}

		void ReceiveBackboardUpdate(ulong remoteID, byte[] msg)
		{
			int offset = 1;
			float remoteTime = UnpackTime(remoteID, msg, ref offset);
			Vector3 pos = UnpackVector3(msg, ref offset);
			Vector3 moveDir = UnpackVector3(msg, ref offset);
			Vector3 nextMoveDir = UnpackVector3(msg, ref offset);

			var goal = m_remotePlayers [remoteID].player.Goal;
			goal.RemoteBackboardUpdate(remoteTime, pos, moveDir, nextMoveDir);
		}

		#endregion

		#region Ball Tansforms

		public void AddNetworkBall(GameObject ball)
		{
			m_localBalls[ball.GetInstanceID()] = ball.AddComponent<P2PNetworkBall>();
		}

		public void RemoveNetworkBall(GameObject ball)
		{
			m_localBalls.Remove(ball.GetInstanceID());
		}

		void SendLocalBallTransforms()
		{
			m_timeForNextBallUpdate = Time.time + LOCAL_BALLS_UPDATE_DELAY;

			int msgSize = 1 + 4 + (m_localBalls.Count * (1 + 4 + 12 + 12));
			byte[] sendBuffer = new byte[msgSize];
			sendBuffer[0] = LOCAL_BALLS_UPDATE_MESSAGE;
			int offset = 1;
			PackFloat(Time.realtimeSinceStartup, sendBuffer, ref offset);

			foreach (var ball in m_localBalls.Values)
			{
				PackBool(ball.IsHeld(), sendBuffer, ref offset);
				PackInt32(ball.gameObject.GetInstanceID(), sendBuffer, ref offset);
				PackVector3(ball.transform.localPosition, sendBuffer, ref offset);
				PackVector3(ball.velocity, sendBuffer, ref offset);
			}

			foreach (KeyValuePair<ulong, RemotePlayerData> player in m_remotePlayers)
			{
				if (player.Value.state == PeerConnectionState.Connected)
				{
					Net.SendPacket(player.Key, sendBuffer, SendPolicy.Unreliable);
				}
			}
		}

		void ReceiveBallTransforms(ulong remoteID, byte[] msg, ulong msgLength)
		{
			int offset = 1;
			float remoteTime = UnpackTime(remoteID, msg, ref offset);

			// because we're using unreliable networking the packets could come out of order
			// and the best thing to do is just ignore old packets because the data isn't
			// very useful anyway
			if (remoteTime < m_remotePlayers[remoteID].lastReceivedBallsTime)
				return;

			m_remotePlayers[remoteID].lastReceivedBallsTime = remoteTime;

			// loop over all ball updates in the message
			while (offset != (int)msgLength)
			{
				bool isHeld = UnpackBool(msg, ref offset);
				int instanceID = UnpackInt32(msg, ref offset);
				Vector3 pos = UnpackVector3(msg, ref offset);
				Vector3 vel = UnpackVector3(msg, ref offset);

				if (!m_remotePlayers[remoteID].activeBalls.ContainsKey(instanceID))
				{
					var newball = m_remotePlayers[remoteID].player.CreateBall().AddComponent<P2PNetworkBall>();
					newball.transform.SetParent(m_remotePlayers[remoteID].player.transform.parent);
					m_remotePlayers[remoteID].activeBalls[instanceID] = newball;
				}
				var ball = m_remotePlayers[remoteID].activeBalls[instanceID];
				if (ball)
				{
					ball.ProcessRemoteUpdate(remoteTime, isHeld, pos, vel);
				}
			}
		}

		#endregion

		#region Score Updates

		public void SendScoreUpdate(uint score)
		{
			byte[] buf = new byte[SCORE_UPDATE_MESSAGE_SIZE];
			buf[0] = SCORE_UPDATE_MESSAGE;
			int offset = 1;
			PackUint32(score, buf, ref offset);

			foreach (KeyValuePair<ulong, RemotePlayerData> player in m_remotePlayers)
			{
				if (player.Value.state == PeerConnectionState.Connected)
				{
					Net.SendPacket(player.Key, buf, SendPolicy.Reliable);
				}
			}
		}

		void ReceiveScoredUpdate(ulong remoteID, byte[] msg)
		{
			int offset = 1;
			uint score = UnpackUint32(msg, ref offset);

			m_remotePlayers[remoteID].player.ReceiveRemoteScore(score);
		}
		#endregion

		#region Serialization

		// This region contains basic data serialization logic.  This sample doesn't warrant
		// much optimization, but the opportunites are ripe those interested in the topic.

		void PackVector3(Vector3 vec, byte[] buf, ref int offset)
		{
			PackFloat(vec.x, buf, ref offset);
			PackFloat(vec.y, buf, ref offset);
			PackFloat(vec.z, buf, ref offset);
		}

		Vector3 UnpackVector3(byte[] buf, ref int offset)
		{
			Vector3 vec;
			vec.x = UnpackFloat(buf, ref offset);
			vec.y = UnpackFloat(buf, ref offset);
			vec.z = UnpackFloat(buf, ref offset);
			return vec;
		}

		void PackQuaternion(Quaternion quat, byte[] buf, ref int offset)
		{
			PackFloat(quat.x, buf, ref offset);
			PackFloat(quat.y, buf, ref offset);
			PackFloat(quat.z, buf, ref offset);
			PackFloat(quat.w, buf, ref offset);
		}

		void PackFloat(float value, byte[] buf, ref int offset)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(value), 0, buf, offset, 4);
			offset = offset + 4;
		}

		float UnpackFloat(byte[] buf, ref int offset)
		{
			float value = BitConverter.ToSingle(buf, offset);
			offset += 4;
			return value;
		}

		float UnpackTime(ulong remoteID, byte[] buf, ref int offset)
		{
			return ShiftRemoteTime(remoteID, UnpackFloat(buf, ref offset));
		}

		void PackInt32(int value, byte[] buf, ref int offset)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(value), 0, buf, offset, 4);
			offset = offset + 4;
		}

		int UnpackInt32(byte[] buf, ref int offset)
		{
			int value = BitConverter.ToInt32(buf, offset);
			offset += 4;
			return value;
		}

		void PackUint32(uint value, byte[] buf, ref int offset)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(value), 0, buf, offset, 4);
			offset = offset + 4;
		}

		uint UnpackUint32(byte[] buf, ref int offset)
		{
			uint value = BitConverter.ToUInt32(buf, offset);
			offset += 4;
			return value;
		}

		void PackBool(bool value, byte[] buf, ref int offset)
		{
			buf[offset++] = (byte)(value ? 1 : 0);
		}

		bool UnpackBool(byte[] buf, ref int offset)
		{
			return buf[offset++] != 0;;
		}

		#endregion
	}
}
