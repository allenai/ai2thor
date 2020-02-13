using UnityEngine;
using UnityEditor;
// using System;
using System.Collections.Generic;

// This causes the class' static constructor to be called on load and on starting playmode
[InitializeOnLoad]
class PhysicsSettler
{
    // only ever register once
    static bool registered = false;

    // are we actively settling physics in our scene
    static bool active = false;

    // the work list of rigid bodies we can find loaded up
    static Rigidbody[] workList;

    // we need to disable auto simulation to manually tick physics
    static bool cachedAutoSimulation;

    // how long do we run physics for before we give up getting things to sleep
    const float timeToSettle = 30f;

    // how long have we been running
    static float activeTime = 0f;

    // this is the static constructor called by [InitializeOnLoad]
    static PhysicsSettler()
    {
        if (!registered)
        {
            // hook into the editor update
            EditorApplication.update += Update;

            // and the scene view OnGui
            SceneView.onSceneGUIDelegate += OnSceneGUI;
            registered = true;
        }
    }

    // let users turn on 
    [MenuItem("GameMenu/Settle Physics")]
    static void Activate()
    {
        if( !active )
        {
            active = true;

            List<Rigidbody> filter = new List<Rigidbody>();

            Rigidbody[] arrayOfAllRB = Object.FindObjectsOfType<Rigidbody>();

            foreach(Rigidbody rb in arrayOfAllRB)
            {
                //first make sure it' a sim object
                if(rb.GetComponentInParent<SimObjPhysics>())
                {
                    //ok now make sure that the sim object is moveable or pickupable
                    SimObjPhysics sop = rb.GetComponentInParent<SimObjPhysics>();

                    if(sop.PrimaryProperty == SimObjPrimaryProperty.Moveable || sop.PrimaryProperty == SimObjPrimaryProperty.CanPickup)
                    {
                        if(!rb.GetComponent<DecalCollision>())
                        {
                            //don't add object if it's in an object specific receptacle-so things like towels and toilet paper that are mounted by default
                            if(sop.transform.parent.name != "AttachPoint")
                            filter.Add(rb);
                        }
                    }

                    IgnoreCollision[] ignoreCollisionObjects = sop.GetComponentsInChildren<IgnoreCollision>();
                    foreach(IgnoreCollision ic in ignoreCollisionObjects)
                    {
                        ic.SetupIgnoreCollision();
                    }
                }
            }

            // Normally avoid Find functions, but this is editor time and only happens once
            workList = filter.ToArray();//Object.FindObjectsOfType<Rigidbody>();

            // we will need to ensure autoSimulation is off to manually tick physics
            cachedAutoSimulation = Physics.autoSimulation;
            activeTime = 0f;

            // make sure that all rigidbodies are awake so they will actively settle against changed geometry.
            foreach( Rigidbody body in workList )
            {
                body.isKinematic = false;
                body.WakeUp();
            }
        }
    }

    // grey out the menu item while we are settling physics
    [MenuItem("GameMenu/Settle Physics", true)]
    static bool checkMenu()
    {
        return !active;
    }

    static void Update()
    {
        if( active )
        {
            activeTime += Time.deltaTime;

            // make sure we are not autosimulating
            Physics.autoSimulation = false;

            // see if all our 
            bool allSleeping = true;
            foreach( Rigidbody body in workList )
            {
                if( body != null )
                {
                    allSleeping &= body.IsSleeping();
                }
            }

            if( allSleeping || activeTime >= timeToSettle)
            {
                Physics.autoSimulation = cachedAutoSimulation;
                active = false;
            }
            else
            {
                Physics.Simulate(0.01f);
            }
        }
    }

    static void OnSceneGUI(SceneView sceneView) 
    {
        if( active )
        {
            Handles.BeginGUI();
            Color cacheColor = GUI.color;
            GUI.color = Color.red;
            GUILayout.Label("Simulating Physics.", GUI.skin.box, GUILayout.Width(200));
            GUILayout.Label(string.Format("Time Remaining: {0:F2}",(timeToSettle - activeTime)), GUI.skin.box, GUILayout.Width(200));
            Handles.EndGUI();

            foreach( Rigidbody body in workList )
            {
                if( body != null )
                {
                    bool isSleeping = body.IsSleeping();
                    if( !isSleeping )
                    {
                        GUI.color = Color.green;
                        Handles.Label(body.transform.position, "SIMULATING");
                    }
                }
            }
            GUI.color = cacheColor;
        }
    }
}
