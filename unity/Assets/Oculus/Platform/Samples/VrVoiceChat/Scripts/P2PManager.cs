namespace Oculus.Platform.Samples.VrVoiceChat
{
	using UnityEngine;
	using System;
	using Oculus.Platform;
	using Oculus.Platform.Models;

	// Helper class to manage a Peer-to-Peer connection to the other user.
	// The connection is used to send and received the Transforms for the
	// Avatars.  The Transforms are sent via unreliable UDP at a fixed
	// frequency.
	public class P2PManager
	{
		// number of seconds to delay between transform updates
		private static readonly float UPDATE_DELAY = 0.1f;

		// the ID of the remote player we expect to be connected to
		private ulong m_remoteID;

		// the result of the last connection state update message
		private PeerConnectionState m_state = PeerConnectionState.Unknown;

		// the next time to send an updated transform to the remote User
		private float m_timeForNextUpdate;

		// the size of the packet we are sending and receiving
		private static readonly byte PACKET_SIZE = 29;

		// packet format type just in case we want to add new future packet types
		private static readonly byte PACKET_FORMAT = 0;

		// reusable buffer to serialize the Transform into
		private readonly byte[] sendTransformBuffer = new byte[PACKET_SIZE];

		// reusable buffer to deserialize the Transform into
		private readonly byte[] receiveTransformBuffer = new byte[PACKET_SIZE];

		// the last received position update
		private Vector3 receivedPosition;

		// the previous received position to interpolate from
		private Vector3 receivedPositionPrior;

		// the last received rotation update
		private Quaternion receivedRotation;

		// the previous received rotation to interpolate from
		private Quaternion receivedRotationPrior;

		// when the last transform was received
		private float receivedTime;

		public P2PManager(Transform initialHeadTransform)
		{
			receivedPositionPrior = receivedPosition = initialHeadTransform.localPosition;
			receivedRotationPrior = receivedRotation = initialHeadTransform.localRotation;

			Net.SetPeerConnectRequestCallback(PeerConnectRequestCallback);
			Net.SetConnectionStateChangedCallback(ConnectionStateChangedCallback);
		}

		#region Connection Management

		public void ConnectTo(ulong userID)
		{
			m_remoteID = userID;

			// ID comparison is used to decide who calls Connect and who calls Accept
			if (PlatformManager.MyID < userID)
			{
				Net.Connect(userID);
			}
		}

		public void Disconnect()
		{
			if (m_remoteID != 0)
			{
				Net.Close(m_remoteID);
				m_remoteID = 0;
				m_state = PeerConnectionState.Unknown;
			}
		}

		public bool Connected
		{
			get
			{
				return m_state == PeerConnectionState.Connected;
			}
		}

		void PeerConnectRequestCallback(Message<NetworkingPeer> msg)
		{
			Debug.LogFormat("Connection request from {0}, authorized is {1}", msg.Data.ID, m_remoteID);

			if (msg.Data.ID == m_remoteID)
			{
				Net.Accept(msg.Data.ID);
			}
		}

		void ConnectionStateChangedCallback(Message<NetworkingPeer> msg)
		{
			Debug.LogFormat("Connection state to {0} changed to {1}", msg.Data.ID, msg.Data.State);

			if (msg.Data.ID == m_remoteID)
			{
				m_state = msg.Data.State;

				if (m_state == PeerConnectionState.Timeout &&
					// ID comparison is used to decide who calls Connect and who calls Accept
					PlatformManager.MyID < m_remoteID)
				{
					// keep trying until hangup!
					Net.Connect(m_remoteID);
				}
			}

			PlatformManager.SetBackgroundColorForState();
		}

		#endregion

		#region Send Update

		public bool ShouldSendHeadUpdate
		{
			get
			{
				return Time.time >= m_timeForNextUpdate && m_state == PeerConnectionState.Connected;
			}
		}

		public void SendHeadTransform(Transform headTransform)
		{
			m_timeForNextUpdate = Time.time + UPDATE_DELAY;

			sendTransformBuffer[0] = PACKET_FORMAT;
			int offset = 1;

			PackFloat(headTransform.localPosition.x, sendTransformBuffer, ref offset);
			PackFloat(headTransform.localPosition.y, sendTransformBuffer, ref offset);
			PackFloat(headTransform.localPosition.z, sendTransformBuffer, ref offset);
			PackFloat(headTransform.localRotation.x, sendTransformBuffer, ref offset);
			PackFloat(headTransform.localRotation.y, sendTransformBuffer, ref offset);
			PackFloat(headTransform.localRotation.z, sendTransformBuffer, ref offset);
			PackFloat(headTransform.localRotation.w, sendTransformBuffer, ref offset);

			Net.SendPacket(m_remoteID, sendTransformBuffer, SendPolicy.Unreliable);
		}

		void PackFloat(float f, byte[] buf, ref int offset)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(f), 0, buf, offset, 4);
			offset = offset + 4;
		}

		#endregion

		#region Receive Update

		public void GetRemoteHeadTransform(Transform headTransform)
		{
			bool hasNewTransform = false;

			Packet packet;
			while ((packet = Net.ReadPacket()) != null)
			{
				if (packet.Size != PACKET_SIZE)
				{
					Debug.Log("Invalid packet size: " + packet.Size);
					continue;
				}

				packet.ReadBytes(receiveTransformBuffer);

				if (receiveTransformBuffer[0] != PACKET_FORMAT)
				{
					Debug.Log("Invalid packet type: " + packet.Size);
					continue;
				}
				hasNewTransform = true;
			}

			if (hasNewTransform)
			{
				receivedPositionPrior = receivedPosition;
				receivedPosition.x = BitConverter.ToSingle(receiveTransformBuffer, 1);
				receivedPosition.y = BitConverter.ToSingle(receiveTransformBuffer, 5);
				receivedPosition.z = BitConverter.ToSingle(receiveTransformBuffer, 9);

				receivedRotationPrior = receivedRotation;
				receivedRotation.x = BitConverter.ToSingle(receiveTransformBuffer, 13);
				receivedRotation.y = BitConverter.ToSingle(receiveTransformBuffer, 17) * -1.0f;
				receivedRotation.z = BitConverter.ToSingle(receiveTransformBuffer, 21);
				receivedRotation.w = BitConverter.ToSingle(receiveTransformBuffer, 25) * -1.0f;

				receivedTime = Time.time;
			}

			// since we're receiving updates at a slower rate than we render,
			// interpolate to make the motion look smoother
			float completed = Math.Min(Time.time - receivedTime, UPDATE_DELAY) / UPDATE_DELAY;
			headTransform.localPosition =
				Vector3.Lerp(receivedPositionPrior, receivedPosition, completed);
			headTransform.localRotation =
				Quaternion.Slerp(receivedRotationPrior, receivedRotation, completed);
		}

		#endregion
	}
}
