namespace Oculus.Platform.Samples.VrHoops
{
	using UnityEngine;


	// This script moves to goal around in a random direction to add a bit more difficulty
	// to the game.
	public class GoalMover : MonoBehaviour {

		// how far to from the center before changing direction
		[SerializeField] private float MAX_OFFSET = 2.0f;

		// how fast the backboard will move
		[SerializeField] private float m_speed = 0.005f;

		// maximum interpolation distance allow to correct per update
		private const float MOVE_TOLERANCE = 0.1f;

		// the position the goal should be in - only differs if network updates come in
		private Vector3 m_expectedPosition;

		// the current move vector * m_speed;
		private Vector3 m_moveDirection;

		// the direction to move when we run into the boundary
		private Vector3 m_nextMoveDirection;

		public Vector3 ExpectedPosition
		{
			get { return m_expectedPosition; }
			set { m_expectedPosition = value; }
		}

		public Vector3 MoveDirection
		{
			get { return m_moveDirection; }
			set { m_moveDirection = value; }
		}

		public Vector3 NextMoveDirection
		{
			get { return m_nextMoveDirection; }
			set { m_nextMoveDirection = value; }
		}

		void Start ()
		{
			ExpectedPosition = transform.localPosition;

			m_moveDirection.x = Random.Range(-1.0f, 1.0f);
			m_moveDirection.y = Random.Range(-1.0f, 1.0f);
			m_moveDirection = Vector3.ClampMagnitude(m_moveDirection, m_speed);

			m_nextMoveDirection.x = -Mathf.Sign(m_moveDirection.x) * Random.Range(0f, 1.0f);
			m_nextMoveDirection.y = -Mathf.Sign(m_moveDirection.y) * Random.Range(0f, 1.0f);
			m_nextMoveDirection = Vector3.ClampMagnitude(m_nextMoveDirection, m_speed);
		}

		void FixedUpdate ()
		{
			// move a bit along our random direction
			transform.localPosition += MoveDirection;
			ExpectedPosition += MoveDirection;

			// make a slight correction to the position if we're not where we should be
			Vector3 correction = ExpectedPosition - transform.localPosition;
			correction = Vector3.ClampMagnitude(correction, MOVE_TOLERANCE);
			transform.localPosition += correction;

			// if we've gone too far from the center point, correct and change direction
			if (transform.localPosition.sqrMagnitude > (MAX_OFFSET*MAX_OFFSET))
			{
				transform.localPosition = Vector3.ClampMagnitude(transform.localPosition, MAX_OFFSET);
				ExpectedPosition = transform.localPosition;
				MoveDirection = NextMoveDirection;

				// select a the next randomish direction to move in
				m_nextMoveDirection.x = -Mathf.Sign(m_moveDirection.x) * Random.Range(0f, 1.0f);
				m_nextMoveDirection.y = -Mathf.Sign(m_moveDirection.y) * Random.Range(0f, 1.0f);
				m_nextMoveDirection = Vector3.ClampMagnitude(m_nextMoveDirection, m_speed);
			}
		}
	}
}
