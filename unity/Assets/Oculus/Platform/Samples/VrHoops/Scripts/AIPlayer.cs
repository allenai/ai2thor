namespace Oculus.Platform.Samples.VrHoops
{
	using UnityEngine;
	using System.Collections;

	// An AI Player just shoots a ball forward with some random delay.
	public class AIPlayer : Player {

		void FixedUpdate ()
		{
			if (HasBall)
			{
				// add a little randomness to the shoot rate so the AI's don't look synchronized
				if (Random.Range(0f, 1f) < 0.03f)
				{
					ShootBall();
				}
			}
			else
			{
				CheckSpawnBall();
			}
		}
	}
}
