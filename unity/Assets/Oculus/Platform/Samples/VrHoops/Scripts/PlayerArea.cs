namespace Oculus.Platform.Samples.VrHoops
{
	using UnityEngine;
	using UnityEngine.UI;
	using Oculus.Platform.Models;

	public class PlayerArea : MonoBehaviour
	{
		// the prefab for the ball that players will shoot
		[SerializeField] private GameObject m_ballPrefab = null;

		// cached gameobject that where the player camera will move to
		private GameObject m_playerHead;

		// cached Text component where we'll render the player's name
		private Text m_nameText;

		// cached component used to align the backboard movement between devices
		private P2PNetworkGoal m_p2pGoal;

		public Player Player
		{
			get { return m_playerHead.GetComponent<Player>(); }
		}

		public Text NameText
		{
			get { return m_nameText; }
		}

		void Awake()
		{
			m_playerHead = gameObject.transform.Find("Player Head").gameObject;
			m_nameText = gameObject.GetComponentsInChildren<Text>()[1];
			m_p2pGoal = gameObject.GetComponentInChildren<P2PNetworkGoal> ();
		}

		public T SetupForPlayer<T>(string name) where T : Player
		{
			var oldplayer = m_playerHead.GetComponent<Player>();
			if (oldplayer) Destroy(oldplayer);

			var player = m_playerHead.AddComponent<T>();
			player.BallPrefab = m_ballPrefab;
			m_nameText.text = name;

			if (player is RemotePlayer)
			{
				(player as RemotePlayer).Goal = m_p2pGoal;
				m_p2pGoal.SendUpdates = false;
			}
			else if (player is LocalPlayer)
			{
				m_p2pGoal.SendUpdates = true;
			}
			else
			{
				m_p2pGoal.SendUpdates = false;
			}

			return player;
		}
	}
}
