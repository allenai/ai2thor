namespace Oculus.Platform.Samples.VrHoops
{
	using UnityEngine;
	using UnityEngine.UI;

	// Helper class to attach to the main camera that raycasts where the
	// user is looking to select/deselect Buttons.
	public class VREyeRaycaster : MonoBehaviour
	{
		[SerializeField] private UnityEngine.EventSystems.EventSystem m_eventSystem = null;

		private Button m_currentButton;

		void Update ()
		{
			RaycastHit hit;
			Button button = null;

			// do a forward raycast to see if we hit a Button
			if (Physics.Raycast(transform.position, transform.forward, out hit, 50f))
			{
				button = hit.collider.GetComponent<Button>();
			}

			if (button != null)
			{
				if (m_currentButton != button)
				{
					m_currentButton = button;
					m_currentButton.Select();
				}
			}
			else if (m_currentButton != null)
			{
				m_currentButton = null;
				if (m_eventSystem != null)
				{
					m_eventSystem.SetSelectedGameObject(null);
				}
			}
		}
	}
}
