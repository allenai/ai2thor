namespace Oculus.Platform.Samples.VrHoops
{
	using UnityEngine;
	using System.Collections;

	// This component handles network coordination for the moving backboard.
	// Although there is randomness in the next direction, the movement is
	// otherwise completely predictable, much like a moving platform or door,
	// thus we only need to send occasional updates.  If the position of the
	// backboard is not correct, the GoalMover will gradually nudge it in the
	// correct direction until the local and remote motion is synchronized.
	public class P2PNetworkGoal : MonoBehaviour
	{
		// cached reference to the associated GoalMover component
		private GoalMover m_goal;

		// the last move direction we sent to remote clients
		private Vector3 m_lastSentMoveDirection;

		private bool m_sendUpdates;

		public bool SendUpdates
		{
			set { m_sendUpdates = value; }
		}

		void Awake()
		{
			m_goal = gameObject.GetComponent<GoalMover>();
		}

		void FixedUpdate ()
		{
			// since the backboard's movement is deterministic, we don't need to send position
			// updates constantly, just when the move direction changes
			if (m_sendUpdates && m_goal.MoveDirection != m_lastSentMoveDirection)
			{
				SendBackboardUpdate();
			}
		}

		public void SendBackboardUpdate()
		{
			m_lastSentMoveDirection = m_goal.MoveDirection;

			float time = Time.realtimeSinceStartup;
			PlatformManager.P2P.SendBackboardUpdate(
				time, transform.localPosition,
				m_goal.MoveDirection, m_goal.NextMoveDirection);
		}

		// message from the remote player with new transforms
		public void RemoteBackboardUpdate(float remoteTime, Vector3 pos, Vector3 moveDir, Vector3 nextMoveDir)
		{
			// interpolate the position forward since the backboard would have moved over
			// the time it took to send the message
			float time = Time.realtimeSinceStartup;
			float numMissedSteps = (time - remoteTime) / Time.fixedDeltaTime;
			m_goal.ExpectedPosition = pos + (Mathf.Round(numMissedSteps) * moveDir);

			m_goal.MoveDirection = moveDir;
			m_goal.NextMoveDirection = nextMoveDir;
		}
	}
}
