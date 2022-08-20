namespace Oculus.Platform.Samples.VrBoardGame
{
	using UnityEngine;
	using System.Collections;

	public class GamePiece : MonoBehaviour
	{
		[SerializeField] private Piece m_type = Piece.A;

		// Prefab for the game pieces
		[SerializeField] private GameObject m_prefabA = null;
		[SerializeField] private GameObject m_prefabB = null;
		[SerializeField] private GameObject m_prefabPower = null;

		public enum Piece { A, B, PowerBall }

		private BoardPosition m_position;

		public Piece Type
		{
			get { return m_type; }
		}

		public BoardPosition Position
		{
			get { return m_position; }
			set { m_position = value; }
		}

		public GameObject Prefab
		{
			get
			{
				switch (m_type)
				{
					case Piece.A: return m_prefabA;
					case Piece.B: return m_prefabB;
					default: return m_prefabPower;
				}
			}
		}

		public GameObject PrefabFor(Piece p)
		{
			switch (p)
			{
				case Piece.A: return m_prefabA;
				case Piece.B: return m_prefabB;
				default: return m_prefabPower;
			}
		}

	}
}
