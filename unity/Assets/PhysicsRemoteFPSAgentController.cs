// Copyright Allen Institute for Artificial Intelligence 2017

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using System.Globalization;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace UnityStandardAssets.Characters.FirstPerson
{
	[RequireComponent(typeof (CharacterController))]   
	public class PhysicsRemoteFPSAgentController : BaseFPSAgentController
    {
		[SerializeField] protected float MaxViewDistancePhysics = 1.7f; //change MaxVisibleDistance of BaseAgent to this value to account for Physics
        
		[SerializeField] protected GameObject AgentHand = null;
		[SerializeField] protected GameObject DefaultHandPosition = null;
        [SerializeField] protected GameObject ItemInHand = null;

		//for turning and look Sweeptests
		[SerializeField] protected GameObject LookSweepPosition = null;
		[SerializeField] protected GameObject LookSweepTestPivot = null; //if the Camera position ever moves, make sure this is set to the same local position as FirstPersonCharacter
		[SerializeField] protected GameObject TurnSweepPosition = null;
        [SerializeField] protected GameObject TurnSweepTestPivot = null;
        
		[SerializeField] protected SimObjPhysics[] VisibleSimObjPhysics; //all SimObjPhysics that are within camera viewport and range dictated by MaxViewDistancePhysics

        // Use this for initialization
        protected override void Start()
        {
			base.Start();

			//enable all the GameObjects on the Agent that Physics Mode requires

            //physics requires max distance to be extended to be able to see objects on ground
			maxVisibleDistance = MaxViewDistancePhysics;

			AgentHand.SetActive(true);
			DefaultHandPosition.SetActive(true);

			LookSweepTestPivot.SetActive(true);
			LookSweepPosition.SetActive(true);

			TurnSweepTestPivot.SetActive(true);
			TurnSweepPosition.SetActive(true);
        }

        // Update is called once per frame
        void Update()
        {
			VisibleSimObjPhysics = GetAllVisibleSimObjPhysics(m_Camera, maxVisibleDistance);
        }

		protected SimObjPhysics[] GetAllVisibleSimObjPhysics(Camera agentCamera, float maxDistance)
        {
            List<SimObjPhysics> currentlyVisibleItems = new List<SimObjPhysics>();

            Vector3 agentCameraPos = agentCamera.transform.position;

            //get all sim objects in range around us
            Collider[] colliders_in_view = Physics.OverlapSphere(agentCameraPos, maxDistance,
                                                         1 << 8, QueryTriggerInteraction.Collide); //layermask is 8

            if (colliders_in_view != null)
            {
                foreach (Collider item in colliders_in_view)
                {
                    if (item.tag == "SimObjPhysics")
                    {
                        SimObjPhysics sop;

                        //if the object has no compound trigger colliders
                        if (item.GetComponent<SimObjPhysics>())
                        {
                            sop = item.GetComponent<SimObjPhysics>();
                        }

                        //if the object does have compount trigger colliders, get the SimObjPhysics component from the parent
                        else
                        {
                            sop = item.GetComponentInParent<SimObjPhysics>();
                        }

                        if (sop.VisibilityPoints.Length > 0)
                        {
                            Transform[] visPoints = sop.VisibilityPoints;
                            int visPointCount = 0;

                            foreach (Transform point in visPoints)
                            {
                                //if this particular point is in view...
                                if (CheckIfVisibilityPointInViewport(point, agentCamera, maxDistance))
                                {
                                    visPointCount++;
                                }
                            }

                            if (visPointCount > 0)
                            {
                                sop.isVisible = true;
                                if (!currentlyVisibleItems.Contains(sop))
                                    currentlyVisibleItems.Add(sop);
                            }
                        }

                        else
                            Debug.Log("Error! Set at least 1 visibility point on SimObjPhysics prefab!");

                    }

                }

                //now that we have a list of currently visible items, let's see which ones are interactable!
                Rigidbody HandRB = AgentHand.GetComponent<Rigidbody>();
                //RaycastHit hit = new RaycastHit();

                foreach (SimObjPhysics visibleSimObjP in currentlyVisibleItems)
                {

                    //get all interaction points on the visible sim object we are checking here
                    Transform[] InteractionPoints = visibleSimObjP.InteractionPoints;

                    int ReachableInteractionPointCount = 0;
                    foreach (Transform ip in InteractionPoints)
                    {
                        //sweep test from agent's hand to each Interaction point
                        RaycastHit hit;
                        if (HandRB.SweepTest(ip.position - AgentHand.transform.position, out hit, maxDistance))
                        {
                            //if the object only has one interaction point to check
                            if (visibleSimObjP.InteractionPoints.Length == 1)
                            {
                                if (hit.transform == visibleSimObjP.transform)
                                {
                                    #if UNITY_EDITOR
                                    Debug.DrawLine(AgentHand.transform.position, ip.transform.position, Color.magenta);
                                    #endif

                                    visibleSimObjP.isInteractable = true;
                                }

                                else
                                    visibleSimObjP.isInteractable = false;
                            }

                            //this object has 2 or more interaction points
                            //if any one of them can be accessed by the Agent's hand, this object is interactable
                            if (visibleSimObjP.InteractionPoints.Length > 1)
                            {

                                if (hit.transform == visibleSimObjP.transform)
                                {
                                    #if UNITY_EDITOR
                                    Debug.DrawLine(AgentHand.transform.position, ip.transform.position, Color.magenta);
                                    #endif
                                    ReachableInteractionPointCount++;
                                }

                                //check if at least one of the interaction points on this multi interaction point object
                                //is accessible to the agent Hand
                                if (ReachableInteractionPointCount > 0)
                                {
                                    visibleSimObjP.isInteractable = true;
                                }

                                else
                                    visibleSimObjP.isInteractable = false;
                            }
                        }
                    }
                }
            }
            
            //populate array of visible items in order by distance
            currentlyVisibleItems.Sort((x, y) => Vector3.Distance(x.transform.position, agentCameraPos).CompareTo(Vector3.Distance(y.transform.position, agentCameraPos)));
            return currentlyVisibleItems.ToArray();
        }

        //
		protected bool CheckIfVisibilityPointInViewport(Transform point, Camera agentCamera, float maxDistance)
        {
            bool result = false;

            Vector3 viewPoint = agentCamera.WorldToViewportPoint(point.position);

            float ViewPointRangeHigh = 1.0f;
            float ViewPointRangeLow = 0.0f;

            if (viewPoint.z > 0 && viewPoint.z < maxDistance //is in front of camera and within range of visibility sphere
                   && viewPoint.x < ViewPointRangeHigh && viewPoint.x > ViewPointRangeLow//within x bounds of viewport
                    && viewPoint.y < ViewPointRangeHigh && viewPoint.y > ViewPointRangeLow)//within y bounds of viewport
            {
                result = true;
                #if UNITY_EDITOR
                Debug.DrawLine(agentCamera.transform.position, point.position, Color.yellow);
                #endif
            }

            else
                result = false;

            return result;

        }
        //see if a given SimObjPhysics is within the camera's range and field of view
        public bool CheckIfInViewport(SimObjPhysics item, Camera agentCamera, float maxDistance)
        {
            //return true result if object is within the Viewport, false if not in viewport or the viewport doesn't care about the object
            bool result = false;

            Vector3 viewPoint = agentCamera.WorldToViewportPoint(item.transform.position);

            //move these two up top as serialized variables later, or maybe not? values between 0 and 1 will cause "tunnel vision"
            float ViewPointRangeHigh = 1.0f;
            float ViewPointRangeLow = 0.0f;

            //note: Viewport space normalized as bottom left (0,0) and top right(1, 1)
            if (viewPoint.z > 0 && viewPoint.z < maxDistance //is in front of camera and within range of visibility sphere
               && viewPoint.x < ViewPointRangeHigh && viewPoint.x > ViewPointRangeLow//within x bounds of viewport
                && viewPoint.y < ViewPointRangeHigh && viewPoint.y > ViewPointRangeLow)//within y bounds of viewport
            {
                result = true;
            }

            else
                result = false;

            return result;
        }
    }

}

