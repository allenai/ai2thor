namespace Oculus.Platform.Samples.VrHoops
{
	using UnityEngine;
	using UnityEngine.UI;

	// Uses two triggers to detect that a basket is made by traveling from top to bottom
	// through the hoop.
	public class DetectBasket : MonoBehaviour
	{
		private enum BasketPhase { NONE, TOP, BOTH, BOTTOM }

		private BasketPhase m_phase = BasketPhase.NONE;

		private Player m_owningPlayer;

		public Player Player
		{
			set { m_owningPlayer = value; }
		}

		void OnTriggerEnter(Collider other)
		{
			if (other.gameObject.name == "Basket Top" && m_phase == BasketPhase.NONE)
			{
				m_phase = BasketPhase.TOP;
			}
			else if (other.gameObject.name == "Basket Bottom" && m_phase == BasketPhase.TOP)
			{
				m_phase = BasketPhase.BOTH;
			}
			else
			{
				m_phase = BasketPhase.NONE;
			}
		}

		void OnTriggerExit(Collider other)
		{
			if (other.gameObject.name == "Basket Top" && m_phase == BasketPhase.BOTH)
			{
				m_phase = BasketPhase.BOTTOM;
			}
			else if (other.gameObject.name == "Basket Bottom" && m_phase == BasketPhase.BOTTOM)
			{
				m_phase = BasketPhase.NONE;

				switch (PlatformManager.CurrentState)
				{
					case PlatformManager.State.PLAYING_A_LOCAL_MATCH:
					case PlatformManager.State.PLAYING_A_NETWORKED_MATCH:
						if (m_owningPlayer)
						{
							m_owningPlayer.Score += 2;
						}
						break;
				}
			}
			else
			{
				m_phase = BasketPhase.NONE;
			}
		}
	}
}
