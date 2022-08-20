namespace Oculus.Platform.Samples.VrHoops
{
	using UnityEngine;
	using System.Collections;
	using Oculus.Platform;
	using Oculus.Platform.Models;

	public class AchievementsManager
	{
		// API NAME defined on the dashboard for the achievement
		private const string LIKES_TO_WIN = "LIKES_TO_WIN";

		// true if the local user hit the achievement Count setup on the dashboard
		private bool m_likesToWinUnlocked;

		public bool LikesToWin
		{
			get { return m_likesToWinUnlocked; }
		}

		public void CheckForAchievmentUpdates()
		{
			Achievements.GetProgressByName(new string[]{ LIKES_TO_WIN }).OnComplete(
				(Message<AchievementProgressList> msg) =>
				{
					foreach (var achievement in msg.Data)
					{
						if (achievement.Name == LIKES_TO_WIN)
						{
							m_likesToWinUnlocked = achievement.IsUnlocked;
						}
					}
				}
			);
		}

		public void RecordWinForLocalUser()
		{
			Achievements.AddCount(LIKES_TO_WIN, 1);
			CheckForAchievmentUpdates();
		}
	}
}
