using UnityEngine;
using System.Collections;

public class InteractiveObject : MonoBehaviour 
{
	private AudioSource sonido;
	public AudioClip OpenSound;
	public AudioClip CloseSound;
	public GameObject DoorWindow;

	void OnGUI() {
			GUI.Label(new Rect(10, 10, 100, 20), "Baby Room  Version 1.0");
		}

	public enum eInteractiveState
	{
		Active, //OPen
		Inactive, //CLose
	}

	private eInteractiveState m_state;

	void Start()
	{
		m_state = eInteractiveState.Inactive;
	    sonido = GetComponent<AudioSource>();
	}

	public void TrigegrInteraction()
	{
		if(!DoorWindow.GetComponent<Animation>().isPlaying)
		{
			Debug.Log("Interactive object");
			switch (m_state) 
			{
			case eInteractiveState.Active:
				DoorWindow.GetComponent<Animation>().Play("Close");
				m_state = eInteractiveState.Inactive;
				sonido.clip = CloseSound;
			    sonido.Play();
				break;
			case eInteractiveState.Inactive:
				DoorWindow.GetComponent<Animation>().Play("Open");
				m_state = eInteractiveState.Active;
				sonido.clip = OpenSound;
			    sonido.Play();
				break;
			default:
				break;
			}
		}
	}
}