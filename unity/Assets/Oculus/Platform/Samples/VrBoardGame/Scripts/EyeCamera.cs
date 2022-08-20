namespace Oculus.Platform.Samples.VrBoardGame
{
	using UnityEngine;
	using UnityEngine.UI;
	using UnityEngine.EventSystems;

	// This is a helper class for selecting objects that the user is looking at.
	// It will select UI objects that have an attach 3d collision volume and helps
	// the GameController locate GamePieces and GamePositions.
	public class EyeCamera : MonoBehaviour
	{
		// the EventSystem used by the UI elements
		[SerializeField] private EventSystem m_eventSystem = null;

		// the GameController to notify
		[SerializeField] private GameController m_gameController = null;

		// a tine ball in the distance to debug where the user is looking
		[SerializeField] private SphereCollider m_gazeTracker = null;

		// the current Button, if any, being looked at
		private Button m_currentButton;

		// the current GamePiece, if any, being looked at
		private GamePiece m_currentPiece;

		// the current BoardPosition, if any, being looked at
		private BoardPosition m_boardPosition;

		void Update()
		{
			RaycastHit hit;
			Button button = null;
			GamePiece piece = null;
			BoardPosition pos = null;

			// do a forward raycast to see if we hit a selectable object
			bool hitSomething = Physics.Raycast(transform.position, transform.forward, out hit, 50f);
			if (hitSomething) {
				button = hit.collider.GetComponent<Button>();
				piece = hit.collider.GetComponent<GamePiece>();
				pos = hit.collider.GetComponent<BoardPosition>();
			}

			if (m_currentButton != button)
			{
				if (m_eventSystem != null)
				{
					m_eventSystem.SetSelectedGameObject(null);
				}
				m_currentButton = button;
				if (m_currentButton != null)
				{
					m_currentButton.Select();
				}
			}

			if (m_currentPiece != piece)
			{
				if (m_currentPiece != null)
				{
					m_gameController.StoppedLookingAtPiece();
				}
				m_currentPiece = piece;
				if (m_currentPiece != null)
				{
					m_gameController.StartedLookingAtPiece(m_currentPiece);
				}
			}

			if (m_boardPosition != pos)
			{
				m_boardPosition = pos;
				if (m_boardPosition != null)
				{
					m_gameController.StartedLookingAtPosition(m_boardPosition);
				}
			}

			// clear the potential move if they gaze off the board
			if (hit.collider == m_gazeTracker)
			{
				m_gameController.ClearProposedMove();
			}

			// moves the camera with the mouse - very useful for debugging in to 2D mode.
			if (Input.GetButton("Fire2"))
			{
				var v = Input.GetAxis("Mouse Y");
				var h = Input.GetAxis("Mouse X");
				transform.rotation *= Quaternion.AngleAxis(h, Vector3.up);
				transform.rotation *= Quaternion.AngleAxis(-v, Vector3.right);
				Vector3 eulers = transform.eulerAngles;
				eulers.z = 0;
				transform.eulerAngles = eulers;
			}
		}
	}
}
