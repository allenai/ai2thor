namespace Oculus.Platform.Samples.VrBoardGame
{
	using UnityEngine;
	using UnityEngine.UI;

	// This is the primary class that implements the game logic.
	public class GameController : MonoBehaviour
	{
		// instance of the object interfacing with the matchmaking service
		[SerializeField] private MatchmakingManager m_matchmaking = null;

		[SerializeField] private GameBoard m_board = null;
		[SerializeField] private GamePiece m_pieceA = null;
		[SerializeField] private GamePiece m_pieceB = null;
		[SerializeField] private GamePiece m_powerPiece = null;

		// colors for the various states of the selectable games pieces
		[SerializeField] private Color m_unusableColor = Color.white;
		[SerializeField] private Color m_unselectedColor = Color.white;
		[SerializeField] private Color m_selectedColor = Color.white;
		[SerializeField] private Color m_highlightedColor = Color.white;

		[SerializeField] private Text m_ballCountText = null;
		[SerializeField] private Text m_player0Text = null;
		[SerializeField] private Text m_player1Text = null;

		private enum GameState {
			None,
			PracticingMyTurn, PracticingAiTurn,
			OnlineMatchMyTurn, OnlineMatchRemoteTurn
		}

		private GameState m_state;

		// the game piece the player is currently looking at
		private GamePiece m_interestedPiece;

		// the piece the player selected with the Fire button
		private GamePiece m_selectedPiece;

		// the piece that would be placed if the player pressed the Fire button
		private GamePiece m_proposedPiece;

		// how many IAP power-balls the user has
		private uint m_powerBallcount;

		// the name of the current opponent
		private string m_opponentName;

		void Start()
		{
			TransitionToState(GameState.None);
			UpdateScores();
		}

		void Update()
		{
			PerFrameStateUpdate();
		}

		#region Game State

		private void TransitionToState(GameState state)
		{
			m_state = state;

			UpdateGamePieceColors();
		}

		private void TransitionToNextState()
		{
			if (!m_board.IsFull())
			{
				switch (m_state)
				{
					case GameState.PracticingAiTurn:
						TransitionToState(GameState.PracticingMyTurn);
						break;
					case GameState.PracticingMyTurn:
						TransitionToState(GameState.PracticingAiTurn);
						break;
					case GameState.OnlineMatchRemoteTurn:
						TransitionToState(GameState.OnlineMatchMyTurn);
						break;
					case GameState.OnlineMatchMyTurn:
						TransitionToState(GameState.OnlineMatchRemoteTurn);
						break;
				}
			}
			else
			{
				switch (m_state)
				{
					case GameState.OnlineMatchRemoteTurn:
					case GameState.OnlineMatchMyTurn:
						m_matchmaking.EndMatch(m_board.GetPlayerScore(0), m_board.GetPlayerScore(1));
						break;
				}
				TransitionToState(GameState.None);
			}
		}

		private void PerFrameStateUpdate()
		{
			switch (m_state)
			{
				case GameState.PracticingAiTurn:
					// don't move immediately to give the AI time to 'think'
					if (Random.Range(1, 100) < 3)
					{
						MakeAIMove(1);
					}
					break;

				case GameState.PracticingMyTurn:
				case GameState.OnlineMatchMyTurn:
					if (Input.GetButton("Fire1"))
					{
						TrySelectPiece();
						TryPlacePiece();
					}
					break;
			}
		}

		#endregion

		#region Practicing with an AI Player

		public void PracticeButtonPressed()
		{
			m_opponentName = "* AI *";

			switch (m_state)
			{
				case GameState.OnlineMatchMyTurn:
				case GameState.OnlineMatchRemoteTurn:
					m_matchmaking.EndMatch(m_board.GetPlayerScore(0), m_board.GetPlayerScore(1));
					break;
			}
			m_board.Reset();

			// randomly decised whether the player or AI goes first
			if (Random.Range(0, 2) == 1)
			{
				TransitionToState(GameState.PracticingMyTurn);
			}
			else
			{
				TransitionToState(GameState.PracticingAiTurn);
			}

			UpdateScores();
		}

		private void MakeAIMove(int player)
		{
			bool moved = false;

			// pick a random search start position
			int rx = Random.Range(0, GameBoard.LENGTH_X - 1);
			int ry = Random.Range(0, GameBoard.LENGTH_Y - 1);

			// from (rx,ry) search of an available move
			for (int i = 0; i < GameBoard.LENGTH_X && !moved; i++)
			{
				for (int j = 0; j < GameBoard.LENGTH_Y && !moved; j++)
				{
					int x = (rx + i) % GameBoard.LENGTH_X;
					int y = (ry + j) % GameBoard.LENGTH_Y;

					// first try to place a piece on the current position
					if (m_board.CanPlayerMoveToPostion(x, y))
					{
						GamePiece p = Random.Range(0, 2) == 0 ? m_pieceA : m_pieceB;
						m_board.AddPiece(player, p.Prefab, x, y);
						moved = true;
					}
					// a random percentage of the time, try to powerup this position
					else if (m_board.CanPlayerPowerUpPosition(x, y) && Random.Range(0, 8) < 2)
					{
						m_board.AddPowerPiece(player, m_powerPiece.Prefab, x, y);
						moved = true;
					}
				}
			}

			if (moved)
			{
				UpdateScores();
				TransitionToNextState();
			}
		}

		#endregion

		#region Playing Online Match

		// called from the MatchmakingManager was a successly online match is made
		public void StartOnlineMatch (string opponentName, bool localUserGoesFirst)
		{
			m_board.Reset();
			m_opponentName = opponentName;

			if (localUserGoesFirst)
			{
				TransitionToState(GameState.OnlineMatchMyTurn);
			}
			else
			{
				TransitionToState(GameState.OnlineMatchRemoteTurn);
			}

			UpdateScores();
		}

		// called from the Matchmaking Manager when the remote users their next move
		public void MakeRemoteMove(GamePiece.Piece piece, int x, int y)
		{
			GameObject prefab = m_pieceA.PrefabFor(piece);

			if (piece == GamePiece.Piece.PowerBall)
			{
				m_board.AddPowerPiece(1, prefab, x, y);
			}
			else
			{
				m_board.AddPiece(1, prefab, x, y);
			}

			UpdateScores();
		}

		// called from the MatchmakingManager when the local user becomes the room
		// owner and thus it's safe for the local user to make their move
		public void MarkRemoteTurnComplete()
		{
			if (m_state == GameState.OnlineMatchRemoteTurn)
			{
				TransitionToNextState();
			}
		}

		// the match ended from a player leaving before the board was complete
		public void RemoteMatchEnded()
		{
			m_matchmaking.EndMatch(m_board.GetPlayerScore(0), m_board.GetPlayerScore(1));
		}

		#endregion

		#region Selecting and Placing a Game Place

		public void StartedLookingAtPiece(GamePiece piece)
		{
			m_interestedPiece = piece;
			UpdateGamePieceColors();
		}

		public void StoppedLookingAtPiece()
		{
			m_interestedPiece = null;
			UpdateGamePieceColors();
		}

		// This method is used to display an example piece where the player is looking
		// so they know what to expect when they press the Fire button.
		public void StartedLookingAtPosition(BoardPosition position)
		{
			if (m_state != GameState.OnlineMatchMyTurn && m_state != GameState.PracticingMyTurn)
				return;

			GamePiece newPiece = null;

			if ((m_selectedPiece == m_pieceA || m_selectedPiece == m_pieceB) &&
				m_board.CanPlayerMoveToPostion(position.x, position.y))
			{
				newPiece = m_board.AddProposedPiece(m_selectedPiece.Prefab, position);
			}
			else if (m_selectedPiece == m_powerPiece &&
				m_board.CanPlayerPowerUpPosition(position.x, position.y))
			{
				newPiece = m_board.AddProposedPowerPiece(m_selectedPiece.Prefab, position);
			}

			if (newPiece != null)
			{
				if (m_proposedPiece != null)
				{
					Destroy(m_proposedPiece.gameObject);
				}
				m_proposedPiece = newPiece;
			}
		}

		public void ClearProposedMove()
		{
			if (m_proposedPiece != null)
			{
				Destroy(m_proposedPiece.gameObject);
			}
		}

		public void TrySelectPiece()
		{
			if (m_interestedPiece == m_pieceA || m_interestedPiece == m_pieceB)
			{
				m_selectedPiece = m_interestedPiece;
			}
			else if (m_interestedPiece == m_powerPiece &&
					(m_powerBallcount > 0 || m_state == GameState.PracticingMyTurn))
			{
				m_selectedPiece = m_interestedPiece;
			}
			UpdateGamePieceColors();
		}

		public void TryPlacePiece()
		{
			if (m_proposedPiece == null)
				return;

			var position = m_proposedPiece.Position;
			switch(m_proposedPiece.Type)
			{
				case GamePiece.Piece.A:
				case GamePiece.Piece.B:
					m_board.AddPiece(0, m_proposedPiece.Prefab, position.x, position.y);
					break;
				case GamePiece.Piece.PowerBall:
					m_board.AddPowerPiece(0, m_proposedPiece.Prefab, position.x, position.y);
					break;
			}
			Destroy(m_proposedPiece.gameObject);

			if (m_state == GameState.OnlineMatchMyTurn)
			{
				m_matchmaking.SendLocalMove(m_proposedPiece.Type, position.x, position.y);
			}

			UpdateScores();
			TransitionToNextState();
		}

		#endregion

		#region UI

		public void QuitButtonPressed()
		{
			UnityEngine.Application.Quit();
		}

		public void AddPowerballs(uint count)
		{
			m_powerBallcount += count;
			m_ballCountText.text = "x" + m_powerBallcount.ToString();
		}

		private void UpdateScores()
		{
			m_player0Text.text = string.Format("{0}\n\n{1}",
				PlatformManager.MyOculusID, m_board.GetPlayerScore(0));

			m_player1Text.text = string.Format("{0}\n\n{1}",
				m_opponentName, m_board.GetPlayerScore(1));
		}

		private void UpdateGamePieceColors()
		{
			switch (m_state)
			{
				case GameState.None:
				case GameState.PracticingAiTurn:
				case GameState.OnlineMatchRemoteTurn:
					m_pieceA.GetComponent<Renderer>().material.color = m_unusableColor;
					m_pieceB.GetComponent<Renderer>().material.color = m_unusableColor;
					m_powerPiece.GetComponent<Renderer>().material.color = m_unusableColor;
					if (m_proposedPiece != null)
					{
						Destroy(m_proposedPiece.gameObject);
					}
					break;

				case GameState.PracticingMyTurn:
				case GameState.OnlineMatchMyTurn:
					m_pieceA.GetComponent<Renderer>().material.color = m_unselectedColor;
					m_pieceB.GetComponent<Renderer>().material.color = m_unselectedColor;
					m_powerPiece.GetComponent<Renderer>().material.color = m_unselectedColor;
					if (m_interestedPiece == m_pieceA || m_interestedPiece == m_pieceB ||
						m_interestedPiece == m_powerPiece)
					{
						m_interestedPiece.GetComponent<Renderer>().material.color = m_highlightedColor;
					}
					if (m_selectedPiece != null)
					{
						m_selectedPiece.GetComponent<Renderer>().material.color = m_selectedColor;
					}
					break;
			}
		}

		#endregion
	}
}
