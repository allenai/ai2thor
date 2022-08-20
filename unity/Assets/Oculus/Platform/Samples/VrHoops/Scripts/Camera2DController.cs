namespace Oculus.Platform.Samples.VrHoops
{
	using UnityEngine;

	// Helper class to attach to the MainCamera so it can be moved with the mouse while debugging
	// in 2D mode on a PC.
	public class Camera2DController : MonoBehaviour
	{
		void Update ()
		{
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
