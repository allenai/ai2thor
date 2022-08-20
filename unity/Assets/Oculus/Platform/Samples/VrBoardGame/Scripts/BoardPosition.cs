namespace Oculus.Platform.Samples.VrBoardGame
{
	using UnityEngine;
	using System.Collections;

	// This behaviour is attached to GameObjects whose collision mesh
	// describes a specific position on the GameBoard.  The collision
	// mesh doesn't need to fully cover the board position, but enough
	// for eye raycasts to detect that the user is looking there.
	public class BoardPosition : MonoBehaviour {

		[SerializeField] [Range(0,2)] public int x = 0;
		[SerializeField] [Range(0,2)] public int y = 0;
	}
}
