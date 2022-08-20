namespace Oculus.Platform.Samples.VrHoops
{
	using UnityEngine;
	using UnityEngine.UI;
	using System.Collections.Generic;

	// The base Player component manages the balls that are in play.  Besides spawning new balls,
	// old balls are destroyed when too many are around or the Player object itself is destroyed.
	public abstract class Player : MonoBehaviour {

		// maximum number of balls allowed at a time
		public const uint MAX_BALLS = 6;

		// the initial force to impart when shooting a ball
		private const float INITIAL_FORCE = 870f;

		// delay time before a new ball will spawn.
		private const float RESPAWN_SECONDS = 2.0f;

		// current score for the player
		private uint m_score;

		// cached reference to the Text component to render the score
		private Text m_scoreUI;

		// prefab for the GameObject representing a ball
		private GameObject m_ballPrefab;

		// gameobject for the position and orientation of where the ball will be shot
		private BallEjector m_ballEjector;

		// queue of active balls for the player to make sure too many arent in play
		private Queue<GameObject> m_balls = new Queue<GameObject>();

		// reference to a ball that hasn't been shot yet and is tied to the camera
		private GameObject m_heldBall;

		// when to spawn a new ball
		private float m_nextSpawnTime;

		#region Properties

		public virtual uint Score
		{
			get { return m_score; }
			set
			{
				m_score = value;

				if (m_scoreUI)
				{
					m_scoreUI.text = m_score.ToString();
				}
			}
		}

		public GameObject BallPrefab
		{
			set { m_ballPrefab = value; }
		}

		protected bool HasBall
		{
			get { return m_heldBall != null; }
		}

		#endregion

		void Start()
		{
			m_ballEjector = transform.GetComponentInChildren<BallEjector>();
			m_scoreUI = transform.parent.GetComponentInChildren<Text>();
			m_scoreUI.text = "0";
		}

		public GameObject CreateBall()
		{
			if (m_balls.Count >= MAX_BALLS)
			{
				Destroy(m_balls.Dequeue());
			}
			var ball = Instantiate(m_ballPrefab);
			m_balls.Enqueue(ball);

			ball.transform.position = m_ballEjector.transform.position;
			ball.transform.SetParent(m_ballEjector.transform, true);
			ball.GetComponent<Rigidbody>().useGravity = false;
			ball.GetComponent<Rigidbody>().detectCollisions = false;
			ball.GetComponent<DetectBasket>().Player = this;

			return ball;
		}

		protected GameObject CheckSpawnBall()
		{
			switch (PlatformManager.CurrentState)
			{
				case PlatformManager.State.WAITING_TO_PRACTICE_OR_MATCHMAKE:
				case PlatformManager.State.PLAYING_A_LOCAL_MATCH:
				case PlatformManager.State.PLAYING_A_NETWORKED_MATCH:
					if (Time.time >= m_nextSpawnTime && !HasBall)
					{
						m_heldBall = CreateBall();
						return m_heldBall;
					}
					break;
			}
			return null;
		}

		protected GameObject ShootBall()
		{
			GameObject ball = m_heldBall;
			m_heldBall = null;

			ball.GetComponent<Rigidbody>().useGravity = true;
			ball.GetComponent<Rigidbody>().detectCollisions = true;
			ball.GetComponent<Rigidbody>().AddForce(m_ballEjector.transform.forward * INITIAL_FORCE, ForceMode.Acceleration);
			ball.transform.SetParent(transform.parent, true);

			m_nextSpawnTime = Time.time + RESPAWN_SECONDS;
			return ball;
		}

		void OnDestroy()
		{
			foreach (var ball in m_balls)
			{
				Destroy(ball);
			}
		}
	}
}
