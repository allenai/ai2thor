using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityStandardAssets.Characters.FirstPerson
{
	public class DebugInputField : MonoBehaviour
    {
		public GameObject Agent = null;

        // Use this for initialization
        void Start()
        {
            //UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(gameObject);
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void Execute(string command)
        {
            //print(command);
            if (command == "moveforward")
            {
				//MoveForward();
				Agent.GetComponent<DebugFPSAgentController>().MoveForward();
            }
        }
    }
}

