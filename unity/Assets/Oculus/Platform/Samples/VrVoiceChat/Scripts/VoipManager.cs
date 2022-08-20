namespace Oculus.Platform.Samples.VrVoiceChat
{
	using UnityEngine;
	using System.Collections;

	using Oculus.Platform;
	using Oculus.Platform.Models;

	// Helper class to manage the Voice-over-IP connection to the
	// remote user
	public class VoipManager
	{
		// the ID of the remote user I expect to talk to
		private ulong m_remoteID;

		// the last reported state of the VOIP connection
		private PeerConnectionState m_state = PeerConnectionState.Unknown;

		// the GameObject where the remote VOIP will project from
		private readonly GameObject m_remoteHead;

		public VoipManager(GameObject remoteHead)
		{
			m_remoteHead = remoteHead;

			Voip.SetVoipConnectRequestCallback(VoipConnectRequestCallback);
			Voip.SetVoipStateChangeCallback(VoipStateChangedCallback);
		}

		public void ConnectTo(ulong userID)
		{
			m_remoteID = userID;
			var audioSource = m_remoteHead.AddComponent<VoipAudioSourceHiLevel>();
			audioSource.senderID = userID;

			// ID comparison is used to decide who initiates and who gets the Callback
			if (PlatformManager.MyID < m_remoteID)
			{
				Voip.Start(userID);
			}
		}


		public void Disconnect()
		{
			if (m_remoteID != 0)
			{
				Voip.Stop(m_remoteID);
				Object.Destroy(m_remoteHead.GetComponent<VoipAudioSourceHiLevel>(), 0);
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

		void VoipConnectRequestCallback(Message<NetworkingPeer> msg)
		{
			Debug.LogFormat("Voip request from {0}, authorized is {1}", msg.Data.ID, m_remoteID);

			if (msg.Data.ID == m_remoteID)
			{
				Voip.Accept(msg.Data.ID);
			}
		}

		void VoipStateChangedCallback(Message<NetworkingPeer> msg)
		{
			Debug.LogFormat("Voip state to {0} changed to {1}", msg.Data.ID, msg.Data.State);

			if (msg.Data.ID == m_remoteID)
			{
				m_state = msg.Data.State;

				if (m_state == PeerConnectionState.Timeout &&
					// ID comparison is used to decide who initiates and who gets the Callback
					PlatformManager.MyID < m_remoteID)
				{
					// keep trying until hangup!
					Voip.Start(m_remoteID);
				}
			}

			PlatformManager.SetBackgroundColorForState();
		}
	}
}
