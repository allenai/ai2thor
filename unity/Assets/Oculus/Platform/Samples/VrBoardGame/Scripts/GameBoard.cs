namespace Oculus.Platform.Samples.VrBoardGame
{
	using System;
	using UnityEngine;

	//
	// This script describes the game board along with the game pieces that
	// are in play.  The rules for the game board are:
	// 1) Player can place a normal GamePiece on any empty BoardPosition
	// 2) Player can place a power GamePiece on top of a normal piece
	// 3) The board is full when all positions have a normal piece
	// Player score is calculated as:
	// 1) +10 points for each normal piece on the board
	// 2) +10 points for each normal piece with 1 square of one of their power pieces
	// 3) -10 points for each opponent normal piece within 1 square of their power pieces
	//
	public class GameBoard : MonoBehaviour
	{
		public const int LENGTH_X = 3;
		public const int LENGTH_Y = 3;
		public const int MAX_PLAYERS = 2;

		// the placed-piece color for each player
		[SerializeField] private Color[] m_playerColors = new Color[MAX_PLAYERS];

		// color for pice the player is considering moving to
		[SerializeField] private Color m_proposedMoveColor = Color.white;

		// the player scores that are recalcuated after a pice is placed
		private int[] m_scores = new int[MAX_PLAYERS];

		// GameObjects that define each of the allowed piece positions
		[SerializeField] private BoardPosition[] m_positions = new BoardPosition[9];

		private struct PositionInfo
		{
			public GameObject piece;
			public int pieceOwner;
			public int powerPieceOwner;
		}

		// pieces in play for the current game
		private readonly PositionInfo[,] m_pieces = new PositionInfo[LENGTH_X, LENGTH_Y];

		// removes all game pieces from the board
		public void Reset()
		{
			for (int x = 0; x < LENGTH_X; x++)
			{
				for (int y = 0; y < LENGTH_Y; y++)
				{
					if (m_pieces[x,y].piece != null)
					{
						Destroy(m_pieces[x,y].piece);
						m_pieces[x,y].piece = null;
						m_pieces[x,y].pieceOwner = -1;
						m_pieces[x,y].powerPieceOwner = -1;
					}
				}
			}
		}

		#region Board status

		// returns true if all the board positions have a piece in them
		public bool IsFull()
		{
			for (int x = 0; x < LENGTH_X; x++)
			{
				for (int y = 0; y < LENGTH_Y; y++)
				{
					if (m_pieces[x,y].piece == null)
					{
						return false;
					}
				}
			}
			return true;
		}

		public bool CanPlayerMoveToPostion(int x, int y)
		{
			return m_pieces[x,y].piece == null;
		}

		public bool CanPlayerPowerUpPosition(int x, int y)
		{
			return m_pieces[x,y].piece != null;
		}

		#endregion

		#region creating game pieces

		public void AddPiece(int player, GameObject prefab, int x, int y)
		{
			var pos = m_positions[x * LENGTH_Y + y];
			var piece = Create(prefab, pos.gameObject, pos, Vector3.zero);
			piece.GetComponent<Renderer>().material.color = m_playerColors[player];
			m_pieces[x,y].piece = piece.gameObject;
			m_pieces[x,y].pieceOwner = player;
			m_pieces[x,y].powerPieceOwner = -1;

			UpdateScores();
		}

		public GamePiece AddProposedPiece(GameObject prefab, BoardPosition pos)
		{
			var piece = Create(prefab, pos.gameObject, pos, Vector3.zero);
			piece.GetComponent<Renderer>().material.color = m_proposedMoveColor;
			return piece;
		}

		public void AddPowerPiece(int player, GameObject prefab, int x, int y)
		{
			var piece = Create(prefab, m_pieces[x,y].piece, m_positions[x*LENGTH_Y+y], .2f*Vector3.up);
			piece.GetComponent<Renderer>().material.color = m_playerColors[player];
			m_pieces[x,y].powerPieceOwner = player;

			UpdateScores();
		}

		public GamePiece AddProposedPowerPiece(GameObject prefab, BoardPosition pos)
		{
			var piece = Create(prefab, m_pieces[pos.x, pos.y].piece, pos, .2f*Vector3.up);
			piece.GetComponent<Renderer>().material.color = m_proposedMoveColor;
			return piece;
		}

		private GamePiece Create(GameObject prefab, GameObject parent, BoardPosition pos, Vector3 off)
		{
			var go = Instantiate(prefab, parent.transform) as GameObject;
			go.transform.position = parent.transform.position + off;
			go.GetComponent<GamePiece>().Position = pos;
			return go.GetComponent<GamePiece>();
		}

		#endregion

		#region scores

		public int GetPlayerScore(int player)
		{
			return m_scores[player];
		}

		private void UpdateScores()
		{
			for (int i = 0; i < MAX_PLAYERS; i++)
			{
				m_scores[i] = 0;
			}

			for (int x = 0; x < LENGTH_X; x++)
			{
				for (int y = 0; y < LENGTH_Y; y++)
				{
					if (m_pieces[x,y].piece != null)
					{
						// for each piece on the board, the player gets 10 points
						m_scores[m_pieces[x,y].pieceOwner] += 10;

						// for each power piece, the player gains or loses 10 points
						// based on the ownership of nearby pieces
						if (m_pieces[x,y].powerPieceOwner >= 0)
						{
							for (int px = x-1; px <= x+1; px++)
							{
								for (int py = y-1; py <= y+1; py++)
								{
									if (px >= 0 && py >= 0 && px < LENGTH_X && py < LENGTH_Y)
									{
										var powerup =
											m_pieces[x,y].pieceOwner == m_pieces[x,y].powerPieceOwner ?
											+10 : -10;
										m_scores[m_pieces[x, y].powerPieceOwner] += powerup;
									}
								}
							}
						}
					}
				}
			}
		}

		#endregion
	}
}
