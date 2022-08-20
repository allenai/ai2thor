namespace Oculus.Platform.Samples.VrHoops
{
	using Oculus.Platform.Models;

	public class RemotePlayer : Player
	{
		private User m_user;
		private P2PNetworkGoal m_goal;

		public User User
		{
			set { m_user = value; }
		}

		public ulong ID
		{
			get { return m_user.ID; }
		}

		public P2PNetworkGoal Goal
		{
			get { return m_goal; }
			set { m_goal = value; }
		}

		public override uint Score
		{
			set
			{
				// For now we ignore the score determined from locally scoring backets.
				// To get an indication of how close the physics simulations were between devices,
				// or whether the remote player was cheating, an estimate of the score could be
				// kept and compared against what the remote player was sending us.
			}
		}

		public void ReceiveRemoteScore(uint score)
		{
			base.Score = score;
		}
	}
}
