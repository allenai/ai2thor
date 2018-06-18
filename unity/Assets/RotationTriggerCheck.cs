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
			if(other.GetComponentInParent<SimObjPhysics>().name != ItemInHand.name)
            isColliding = true;
        }
    }
}

