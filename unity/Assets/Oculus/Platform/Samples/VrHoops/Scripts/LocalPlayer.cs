namespace Oculus.Platform.Samples.VrHoops
{
	using UnityEngine;
	using System.Collections;

	// This class listens for Input events to shoot a ball, and also notifies the P2PManager when
	// ball or scores needs to be synchronized to remote players.
	public class LocalPlayer : Player {

		public override uint Score
		{
			set
			{
				base.Score = value;

				if (PlatformManager.CurrentState == PlatformManager.State.PLAYING_A_NETWORKED_MATCH)
				{
					PlatformManager.P2P.SendScoreUpdate(base.Score);
				}
			}
		}

		void Update ()
		{
			GameObject newball = null;

			// if the player is holding a ball
			if (HasBall)
			{
				// check to see if the User is hitting the shoot button
				if (Input.GetButton("Fire1") || Input.GetKey(KeyCode.Space))
				{
					newball = ShootBall();
				}
			}
			// spawn a new held ball if we can
			else
			{
				newball = CheckSpawnBall();
			}

			if (newball && PlatformManager.CurrentState == PlatformManager.State.PLAYING_A_NETWORKED_MATCH)
			{
				PlatformManager.P2P.AddNetworkBall(newball);
			}
		}
	}
}
