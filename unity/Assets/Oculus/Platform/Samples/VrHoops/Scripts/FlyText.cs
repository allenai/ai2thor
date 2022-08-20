namespace Oculus.Platform.Samples.VrHoops
{
	using UnityEngine;
	using UnityEngine.UI;
	using System.Collections;

	// helper script to render fading flytext above an object
	public class FlyText : MonoBehaviour
	{
		// destory the gameobject after this many seconds
		private const float LIFESPAN = 3.0f;

		// how far to move upwards per frame
		private readonly Vector3 m_movePerFrame = 0.5f * Vector3.up;

		// actual destruction time
		private float m_eol;

		void Start()
		{
			m_eol = Time.time + LIFESPAN;
			GetComponent<Text>().CrossFadeColor(Color.black, LIFESPAN * 1.7f, false, true);
		}

		void Update()
		{
			if (Time.time < m_eol)
			{
				transform.localPosition += m_movePerFrame;
			}
			else
			{
				Destroy(gameObject);
			}
		}
	}
}
