using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityStandardAssets.Characters.FirstPerson
{
	public class RotationTriggerCheck : MonoBehaviour
    {
        public bool isColliding = false;
        public GameObject ItemInHand;
		public PhysicsRemoteFPSAgentController AgentRef;
        // Use this for initialization
        void Start()
        {
			AgentRef = gameObject.GetComponentInParent<PhysicsRemoteFPSAgentController>();
        }

        // Update is called once per frame
        void Update()
        {
			ItemInHand = AgentRef.WhatAmIHolding();
        }

        private void FixedUpdate()
        {
            isColliding = false;         
        }

        public void OnTriggerStay(Collider other)
        {
            //this is in the Agent layer, so is the rest of the agent, so it won't collide with itself
            //print(other.name);
            //(other.GetComponentInParent<SimObjPhysics>().name);
			if(ItemInHand != null)
			{
				//if the item is a sim object....
				if(other.GetComponentInParent<SimObjPhysics>())
				{
					if(other.GetComponentInParent<SimObjPhysics>().name != ItemInHand.name)
					{
						if(other.GetComponent<Collider>())
						{
							if(!other.GetComponent<Collider>().isTrigger)
							isColliding = true;
						}
						//print(other.GetComponentInParent<SimObjPhysics>().name);
					}
				}

                //ok so the collider we hit was not a sim obj
				else
				{
					//print(this.name + " is hitting non sim "+ other.name);
					isColliding = true;
				}


			}

        }

		public void OnTriggerExit(Collider other)
		{
			isColliding = false;
		}
	}
}

