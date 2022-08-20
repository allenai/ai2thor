namespace Oculus.Platform.Samples.VrHoops
{
	using UnityEngine;
	using UnityEngine.Assertions;
	using UnityEngine.UI;
	using System.Collections.Generic;
	using Oculus.Platform.Models;

	// This class coordinates playing matches.  It mediates being idle
	// and entering a practice or online game match.
	public class MatchController : MonoBehaviour
	{
		// Text to display when the match will start or finish
		[SerializeField] private Text m_timerText = null;

		// the camera is moved between the idle position and the assigned court position
		[SerializeField] private Camera m_camera = null;

		// where the camera will be when not in a match
		[SerializeField] private Transform m_idleCameraTransform = null;

		// button that toggles between matchmaking and cancel
		[SerializeField] private Text m_matchmakeButtonText = null;

		// this should equal the maximum number of players configured on the Oculus Dashboard
		[SerializeField] private PlayerArea[] m_playerAreas = new PlayerArea[3];

		// the time to wait between selecting Practice and starting
		[SerializeField] private uint PRACTICE_WARMUP_TIME = 5;

		// seconds to wait to coordinate P2P setup with other match players before starting
		[SerializeField] private uint MATCH_WARMUP_TIME = 30;

		// seconds for the match
		[SerializeField] private uint MATCH_TIME = 20;

		// how long to remain in position after the match to view results
		[SerializeField] private uint MATCH_COOLDOWN_TIME = 10;

		// panel to add most-wins leaderboard entries to
		[SerializeField] private GameObject m_mostWinsLeaderboard = null;

		// panel to add high-score leaderboard entries to
		[SerializeField] private GameObject m_highestScoresLeaderboard = null;

		// leaderboard entry Text prefab
		[SerializeField] private GameObject m_leaderboardEntryPrefab = null;

		// Text prefab to use for achievements fly-text
		[SerializeField] private GameObject m_flytext = null;

		// the current state of the match controller
		private State m_currentState;

		// transition time for states that automatically transition to the next state,
		// for example ending the match when the timer expires
		private float m_nextStateTransitionTime;

		// the court the local player was assigned to
		private int m_localSlot;

		void Start()
		{
			PlatformManager.Matchmaking.EnqueueResultCallback = OnMatchFoundCallback;
			PlatformManager.Matchmaking.MatchPlayerAddedCallback = MatchPlayerAddedCallback;
			PlatformManager.P2P.StartTimeOfferCallback = StartTimeOfferCallback;
			PlatformManager.Leaderboards.MostWinsLeaderboardUpdatedCallback = MostWinsLeaderboardCallback;
			PlatformManager.Leaderboards.HighScoreLeaderboardUpdatedCallback = HighestScoreLeaderboardCallback;

			TransitionToState(State.NONE);
		}

		void Update()
		{
			UpdateCheckForNextTimedTransition();
			UpdateMatchTimer();
		}

		public float MatchStartTime
		{
			get
			{
				switch(m_currentState)
				{
					case State.WAITING_TO_START_PRACTICE:
					case State.WAITING_TO_SETUP_MATCH:
						return m_nextStateTransitionTime;

					default: return 0;
				}
			}
			private set { m_nextStateTransitionTime = value; }
		}

		#region State Management

		private enum State
		{
			UNKNOWN,

			// no current match, waiting for the local user to select something
			NONE,

			// user selected a practice match, waiting for the match timer to start
			WAITING_TO_START_PRACTICE,

			// playing a Practice match against AI players
			PRACTICING,

			// post practice match, time to view the scores
			VIEWING_RESULTS_PRACTICE,

			// selecting Player Online and waiting for the Matchmaking service to find and create a
			// match and join the assigned match room
			WAITING_FOR_MATCH,

			// match room is joined, waiting to coordinate with the other players
			WAITING_TO_SETUP_MATCH,

			// playing a competative match against other players
			PLAYING_MATCH,

			// match is complete, viewing the match scores
			VIEWING_MATCH_RESULTS,
		}

		void TransitionToState(State newState)
		{
			Debug.LogFormat("MatchController State {0} -> {1}", m_currentState, newState);

			if (m_currentState != newState)
			{
				var oldState = m_currentState;
				m_currentState = newState;

				// state transition logic
				switch (newState)
				{
					case State.NONE:
						SetupForIdle();
						MoveCameraToIdlePosition();
						PlatformManager.TransitionToState(PlatformManager.State.WAITING_TO_PRACTICE_OR_MATCHMAKE);
						m_matchmakeButtonText.text = "Play Online";
						break;

					case State.WAITING_TO_START_PRACTICE:
						Assert.AreEqual(oldState, State.NONE);
						SetupForPractice();
						MoveCameraToMatchPosition();
						PlatformManager.TransitionToState(PlatformManager.State.MATCH_TRANSITION);
						m_nextStateTransitionTime = Time.time + PRACTICE_WARMUP_TIME;
						break;

					case State.PRACTICING:
						Assert.AreEqual(oldState, State.WAITING_TO_START_PRACTICE);
						PlatformManager.TransitionToState(PlatformManager.State.PLAYING_A_LOCAL_MATCH);
						m_nextStateTransitionTime = Time.time + MATCH_TIME;
						break;

					case State.VIEWING_RESULTS_PRACTICE:
						Assert.AreEqual(oldState, State.PRACTICING);
						PlatformManager.TransitionToState(PlatformManager.State.MATCH_TRANSITION);
						m_nextStateTransitionTime = Time.time + MATCH_COOLDOWN_TIME;
						m_timerText.text = "0:00.00";
						break;

					case State.WAITING_FOR_MATCH:
						Assert.AreEqual(oldState, State.NONE);
						PlatformManager.TransitionToState(PlatformManager.State.MATCH_TRANSITION);
						m_matchmakeButtonText.text = "Cancel";
						break;

					case State.WAITING_TO_SETUP_MATCH:
						Assert.AreEqual(oldState, State.WAITING_FOR_MATCH);
						m_nextStateTransitionTime = Time.time + MATCH_WARMUP_TIME;
						break;

					case State.PLAYING_MATCH:
						Assert.AreEqual(oldState, State.WAITING_TO_SETUP_MATCH);
						PlatformManager.TransitionToState(PlatformManager.State.PLAYING_A_NETWORKED_MATCH);
						m_nextStateTransitionTime = Time.time + MATCH_TIME;
						break;

					case State.VIEWING_MATCH_RESULTS:
						Assert.AreEqual(oldState, State.PLAYING_MATCH);
						PlatformManager.TransitionToState(PlatformManager.State.MATCH_TRANSITION);
						m_nextStateTransitionTime = Time.time + MATCH_COOLDOWN_TIME;
						m_timerText.text = "0:00.00";
						CalculateMatchResults();
						break;
				}
			}
		}

		void UpdateCheckForNextTimedTransition()
		{
			if (m_currentState != State.NONE && Time.time >= m_nextStateTransitionTime)
			{
				switch (m_currentState)
				{
					case State.WAITING_TO_START_PRACTICE:
						TransitionToState(State.PRACTICING);
						break;

					case State.PRACTICING:
						TransitionToState(State.VIEWING_RESULTS_PRACTICE);
						break;

					case State.VIEWING_RESULTS_PRACTICE:
						TransitionToState(State.NONE);
						break;

					case State.WAITING_TO_SETUP_MATCH:
						TransitionToState(State.PLAYING_MATCH);
						break;

					case State.PLAYING_MATCH:
						TransitionToState(State.VIEWING_MATCH_RESULTS);
						break;

					case State.VIEWING_MATCH_RESULTS:
						PlatformManager.Matchmaking.EndMatch();
						TransitionToState(State.NONE);
						break;
				}
			}
		}

		void UpdateMatchTimer()
		{
			if (Time.time <= m_nextStateTransitionTime)
			{
				switch (m_currentState)
				{
					case State.WAITING_TO_START_PRACTICE:
					case State.WAITING_TO_SETUP_MATCH:
						m_timerText.text = string.Format("{0:0}", Mathf.Ceil(Time.time - MatchStartTime));
						break;

					case State.PRACTICING:
					case State.PLAYING_MATCH:
						var delta = m_nextStateTransitionTime - Time.time;
						m_timerText.text = string.Format("{0:#0}:{1:#00}.{2:00}",
							Mathf.Floor(delta / 60),
							Mathf.Floor(delta) % 60,
							Mathf.Floor(delta * 100) % 100);
						break;
				}
			}
		}

		#endregion

		#region Player Setup/Teardown

		void SetupForIdle()
		{
			for (int i = 0; i < m_playerAreas.Length; i++)
			{
				m_playerAreas[i].SetupForPlayer<AIPlayer>("* AI *");
			}
		}

		void SetupForPractice()
		{
			// randomly select a position for the local player
			m_localSlot = Random.Range(0,m_playerAreas.Length-1);

			for (int i=0; i < m_playerAreas.Length; i++)
			{
				if (i == m_localSlot)
				{
					m_playerAreas[i].SetupForPlayer<LocalPlayer>(PlatformManager.MyOculusID);
				}
				else
				{
					m_playerAreas[i].SetupForPlayer<AIPlayer>("* AI *");
				}
			}
		}

		Player MatchPlayerAddedCallback(int slot, User user)
		{
			Player player = null;

			if (m_currentState == State.WAITING_TO_SETUP_MATCH && slot < m_playerAreas.Length)
			{
				if (user.ID == PlatformManager.MyID)
				{
					var localPlayer = m_playerAreas[slot].SetupForPlayer<LocalPlayer>(user.OculusID);
					MoveCameraToMatchPosition();
					player = localPlayer;
					m_localSlot = slot;
				}
				else
				{
					var remotePlayer = m_playerAreas[slot].SetupForPlayer<RemotePlayer>(user.OculusID);
					remotePlayer.User = user;
					player = remotePlayer;
				}
			}

			return player;
		}

		#endregion

		#region Main Camera Movement

		void MoveCameraToIdlePosition()
		{
			var ejector = m_camera.gameObject.GetComponentInChildren<BallEjector>();
			if (ejector)
			{
				ejector.transform.SetParent(m_camera.transform.parent, false);
				m_camera.transform.SetParent(m_idleCameraTransform, false);
			}
		}

		void MoveCameraToMatchPosition()
		{
			foreach (var playerArea in m_playerAreas)
			{
				var player = playerArea.GetComponentInChildren<LocalPlayer>();
				if (player)
				{
					var ejector = player.GetComponentInChildren<BallEjector>();
					m_camera.transform.SetParent(player.transform, false);
					ejector.transform.SetParent(m_camera.transform, false);
					break;
				}
			}
			DisplayAchievementFlytext();
		}

		#endregion

		#region Match Initiation

		public void StartPracticeMatch()
		{
			if (m_currentState == State.NONE)
			{
				TransitionToState(State.WAITING_TO_START_PRACTICE);
			}
		}

		public void PlayOnlineOrCancel()
		{
			Debug.Log ("Play online or Cancel");

			if (m_currentState == State.NONE)
			{
				PlatformManager.Matchmaking.QueueForMatch();
				TransitionToState (State.WAITING_FOR_MATCH);
			}
			else if (m_currentState == State.WAITING_FOR_MATCH)
			{
				PlatformManager.Matchmaking.LeaveQueue();
				TransitionToState (State.NONE);
			}
		}

		// notification from the Matchmaking service if we succeeded in finding an online match
		void OnMatchFoundCallback(bool success)
		{
			if (success)
			{
				TransitionToState(State.WAITING_TO_SETUP_MATCH);
			}
			else
			{
				TransitionToState(State.NONE);
			}
		}

		// handle an offer from a remote player for a new match start time
		float StartTimeOfferCallback(float remoteTime)
		{
			if (m_currentState == State.WAITING_TO_SETUP_MATCH)
			{
				// if the remote start time is later use that, as long as it's not horribly wrong
				if (remoteTime > MatchStartTime && (remoteTime - 60) < MatchStartTime)
				{
					Debug.Log("Moving Start time by " + (remoteTime - MatchStartTime));
					MatchStartTime = remoteTime;
				}
			}
			return MatchStartTime;
		}

		#endregion

		#region Leaderboards and Achievements

		void MostWinsLeaderboardCallback(SortedDictionary<int, LeaderboardEntry> entries)
		{
			foreach (Transform entry in m_mostWinsLeaderboard.transform)
			{
				Destroy(entry.gameObject);
			}
			foreach (var entry in entries.Values)
			{
				GameObject label = Instantiate(m_leaderboardEntryPrefab);
				label.transform.SetParent(m_mostWinsLeaderboard.transform, false);
				label.GetComponent<Text>().text =
					string.Format("{0} - {1} - {2}", entry.Rank, entry.User.OculusID, entry.Score);
			}
		}

		void HighestScoreLeaderboardCallback(SortedDictionary<int, LeaderboardEntry> entries)
		{
			foreach (Transform entry in m_highestScoresLeaderboard.transform)
			{
				Destroy(entry.gameObject);
			}
			foreach (var entry in entries.Values)
			{
				GameObject label = Instantiate(m_leaderboardEntryPrefab);
				label.transform.SetParent(m_highestScoresLeaderboard.transform, false);
				label.GetComponent<Text>().text =
					string.Format("{0} - {1} - {2}", entry.Rank, entry.User.OculusID, entry.Score);
			}
		}

		void CalculateMatchResults()
		{
			LocalPlayer localPlayer = null;
			RemotePlayer remotePlayer = null;

			foreach (var court in m_playerAreas)
			{
				if (court.Player is LocalPlayer)
				{
					localPlayer = court.Player as LocalPlayer;
				}
				else if (court.Player is RemotePlayer &&
					(remotePlayer == null || court.Player.Score > remotePlayer.Score))
				{
					remotePlayer = court.Player as RemotePlayer;
				}
			}

			// ignore the match results if the player got into a session without an opponent
			if (!localPlayer || !remotePlayer)
			{
				return;
			}

			bool wonMatch = localPlayer.Score > remotePlayer.Score;
			PlatformManager.Leaderboards.SubmitMatchScores(wonMatch, localPlayer.Score);

			if (wonMatch)
			{
				PlatformManager.Achievements.RecordWinForLocalUser();
			}
		}

		void DisplayAchievementFlytext()
		{
			if (PlatformManager.Achievements.LikesToWin)
			{
				GameObject go = Instantiate(m_flytext);
				go.GetComponent<Text>().text = "Likes to Win!";
				go.transform.position = Vector3.up * 40;
				go.transform.SetParent(m_playerAreas[m_localSlot].NameText.transform, false);
			}
		}

		#endregion
	}
}
