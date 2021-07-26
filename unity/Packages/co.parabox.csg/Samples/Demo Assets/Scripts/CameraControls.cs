using UnityEngine;

namespace Parabox.CSG.Demo
{
	public class CameraControls : MonoBehaviour
	{
		const string INPUT_MOUSE_SCROLLWHEEL = "Mouse ScrollWheel";
		const string INPUT_MOUSE_X = "Mouse X";
		const string INPUT_MOUSE_Y = "Mouse Y";
		const float MIN_CAM_DISTANCE = 2f;
		const float MAX_CAM_DISTANCE = 20f;

		// how fast the camera orbits
		[Range(2f, 15f)]
		public float orbitSpeed = 6f;

		// how fast the camera zooms in and out
		[Range(.3f, 2f)]
		public float zoomSpeed = .8f;

		// the current distance from pivot point (locked to Vector3.zero)
		float distance = 0f;

		void Start()
		{
			distance = Vector3.Distance(transform.position, Vector3.zero);
		}

		void LateUpdate()
		{
			// orbits
			if (Input.GetMouseButton(0))
			{
				float rot_x = Input.GetAxis(INPUT_MOUSE_X);
				float rot_y = -Input.GetAxis(INPUT_MOUSE_Y);

				Vector3 eulerRotation = transform.localRotation.eulerAngles;

				eulerRotation.x += rot_y * orbitSpeed;
				eulerRotation.y += rot_x * orbitSpeed;

				eulerRotation.z = 0f;

				transform.localRotation = Quaternion.Euler(eulerRotation);
				transform.position = transform.localRotation * (Vector3.forward * -distance);
			}

			if (Input.GetAxis(INPUT_MOUSE_SCROLLWHEEL) != 0f)
			{
				float delta = Input.GetAxis(INPUT_MOUSE_SCROLLWHEEL);

				distance -= delta * (distance / MAX_CAM_DISTANCE) * (zoomSpeed * 1000) * Time.deltaTime;
				distance = Mathf.Clamp(distance, MIN_CAM_DISTANCE, MAX_CAM_DISTANCE);
				transform.position = transform.localRotation * (Vector3.forward * -distance);
			}
		}
	}
}
