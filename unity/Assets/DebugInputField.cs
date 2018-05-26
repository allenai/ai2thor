using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityStandardAssets.Characters.FirstPerson
{
	public class DebugInputField : MonoBehaviour
    {
		public GameObject Agent = null;
		public DebugFPSAgentController AgentController = null;

        // Use this for initialization
        void Start()
        {
			//UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(gameObject);
			AgentController = Agent.GetComponent<DebugFPSAgentController>();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void Execute(string command)
        {
            ///////////////////
			/// move forward, back, left, right
            if (command == "moveforward")
            {
				//MoveForward();
				AgentController.MoveAgent("forward", 0.5f);
            }

            if (command == "moveleft")
			{
				AgentController.MoveAgent("left", 0.5f);
			}

			if (command == "moveright")
			{
				AgentController.MoveAgent("right", 0.5f);
			}

            if (command == "movebackward")
			{
				AgentController.MoveAgent("backward", 0.5f);
			}

            //////////////////////
			/// camera rotate stuff
            if(command =="lookup")
			{
				AgentController.Look("up");
    		}

			if (command == "lookforward")
            {
				AgentController.Look("forward");
            }

			if (command == "lookdown")
            {
				AgentController.Look("down");
            }

            if( command == "looksuperdown")
			{
				AgentController.Look("superdown");

			}

            /////////////////////
			/// rotate
            if (command == "turnleft")
			{
				AgentController.Turn("left");
			}

			if (command == "turnright")
            {
				AgentController.Turn("right");

            }

           
        }
    }
}

