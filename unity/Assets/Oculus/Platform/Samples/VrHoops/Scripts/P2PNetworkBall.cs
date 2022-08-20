namespace Oculus.Platform.Samples.VrHoops
{
	using UnityEngine;
	using System.Collections;

	// This component handles network coordination for moving balls.
	// Synchronizing moving objects that are under the influence of physics
	// and other forces is somewhat of an art and this example only scratches
	// the surface.  Ultimately how you synchronize will depend on the requirements
	// of your application and its tolerance for users seeing slightly different
	// versions of the simulation.
	public class P2PNetworkBall : MonoBehaviour
	{
		// the last time this ball locally collided with something
		private float lastCollisionTime;

		// cached reference to the GameObject's Rigidbody component
		private Rigidbody rigidBody;

		void Awake()
		{
			rigidBody = gameObject.GetComponent<Rigidbody>();
		}

		public Vector3 velocity
		{
			get { return rigidBody.velocity; }
		}

		public bool IsHeld()
		{
			return !rigidBody.useGravity;
		}

		public void ProcessRemoteUpdate(float remoteTime, bool isHeld, Vector3 pos, Vector3 vel)
		{
			if (isHeld)
			{
				transform.localPosition = pos;
			}
			// if we've collided since the update was sent, our state is going to be more accurate so
			// it's better to ignore the update
			else if (lastCollisionTime < remoteTime)
			{
				// To correct the position this sample directly moves the ball.
				// Another approach would be to gradually lerp the ball there during
				// FixedUpdate.  However, that approach aggravates any errors that
				// come from estimatePosition and estimateVelocity so the lerp
				// should be done over few timesteps.
				float deltaT = Time.realtimeSinceStartup - remoteTime;
				transform.localPosition = estimatePosition(pos, vel, deltaT);
				rigidBody.velocity = estimateVelocity(vel, deltaT);

				// if the ball is transitioning from held to ballistic, we need to
				// update the RigidBody parameters
				if (IsHeld())
				{
					rigidBody.useGravity = true;
					rigidBody.detectCollisions = true;
				}
			}
		}

		// Estimates the new position assuming simple ballistic motion.
		private Vector3 estimatePosition(Vector3 startPosition, Vector3 startVelocty, float time)
		{
			return startPosition + startVelocty * time + 0.5f * Physics.gravity * time * time;
		}

		// Estimates the new velocity assuming ballistic motion and drag.
		private Vector3 estimateVelocity(Vector3 startVelocity, float time)
		{
			return startVelocity + Physics.gravity * time * Mathf.Clamp01 (1 - rigidBody.drag * time);
		}

		void OnCollisionEnter(Collision collision)
		{
			lastCollisionTime = Time.realtimeSinceStartup;
		}

		void OnDestroy()
		{
			PlatformManager.P2P.RemoveNetworkBall(gameObject);
		}

	}
}
