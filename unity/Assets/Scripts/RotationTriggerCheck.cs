using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityStandardAssets.Characters.FirstPerson {
    public class RotationTriggerCheck : MonoBehaviour {
        public bool isColliding = false;
        public PhysicsRemoteFPSAgentController AgentRef;
        // Use this for initialization
        private AgentManager agentManager;
        void Start() {
            agentManager = GameObject.Find("PhysicsSceneManager").GetComponentInChildren<AgentManager>();
        }


        private void FixedUpdate() {
            isColliding = false;
        }

        public void OnTriggerStay(Collider other) {
            // this is in the Agent layer, so is the rest of the agent, so it won't collide with itself
            // print(other.name);
            //(other.GetComponentInParent<SimObjPhysics>().name);
            var agent = agentManager.PrimaryAgent as PhysicsRemoteFPSAgentController;
            var itemInHand = agent.WhatAmIHolding();
            if (itemInHand != null) {
                // if the item is a sim object....
                if (other.GetComponentInParent<SimObjPhysics>()) {
                    if (other.GetComponentInParent<SimObjPhysics>().name != itemInHand.name) {
                        if (other.GetComponent<Collider>()) {
                            if (!other.GetComponent<Collider>().isTrigger) {
                                isColliding = true;
                            }
                        }
                        // print(other.GetComponentInParent<SimObjPhysics>().name);
                    }
                }

                // ok so the collider we hit was not a sim obj
                else {
                    // print(this.name + " is hitting non sim "+ other.name);
                    isColliding = true;
                }


            }

        }

        public void OnTriggerExit(Collider other) {
            isColliding = false;
        }
    }
}

