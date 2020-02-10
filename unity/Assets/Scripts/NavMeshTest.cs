using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using System.Net.Sockets;
using System.Net;
using Newtonsoft.Json;
using System.Text;
using UnityEngine.Networking;
using System.Linq;
public class NavMeshTest : MonoBehaviour
{
    public Transform goal;
    private UnityEngine.AI.NavMeshAgent navMeshAgent;
    private Transform hitPos;

    private PhysicsRemoteFPSAgentController PhysicsController = null;

    void Start()
    {
 PhysicsController = GetComponent<PhysicsRemoteFPSAgentController>();
    }

    public UnityEngine.AI.NavMeshPath SetSimObjectNavMeshTarget(SimObjPhysics targetSOP) {
        var targetTransform = targetSOP.transform;
        var targetPosition = new Vector3(targetTransform.position.x, targetTransform.position.y, targetTransform.position.z); 
        var targetSimObject = targetTransform.GetComponentInChildren<SimObjPhysics>();
        PhysicsController = GetComponent<PhysicsRemoteFPSAgentController>();
        var agentTransform = PhysicsController.transform;

        var reachaBlePositions = PhysicsController.getReachablePositions();
        var sortedPositions = reachaBlePositions.OrderBy(pos => (pos - agentTransform.position).sqrMagnitude ).ThenBy( pos => (targetPosition - pos).sqrMagnitude);
        var capsuleCollider = PhysicsController.GetComponent<CapsuleCollider>();
        var agentCamera = PhysicsController.GetComponentInChildren<Camera>();

        // var originalCamera = new Camera().CopyFrom(agentCamera);
        var camera = new Camera();
        var originalAgentPosition = agentTransform.position;
        var orignalAgentRotation = agentTransform.rotation;

        var targetPositionYAgent = targetPosition;
        targetPositionYAgent.y = agentTransform.position.y;
        Vector3 fixedPosition = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        bool success = false;
        foreach (var pos in sortedPositions) {
            agentTransform.position = pos;
            agentTransform.LookAt(targetPosition);

            var visibleSimObjects = PhysicsController.GetAllVisibleSimObjPhysics(PhysicsController.maxVisibleDistance);
            if (visibleSimObjects.Any(sop => sop.uniqueID == targetSimObject.uniqueID)) {
                fixedPosition = pos;
                success = true;
                break;
            }
        }

        agentTransform.position = originalAgentPosition;
        agentTransform.rotation = orignalAgentRotation;

        navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (success) {
            // navMeshAgent.destination = fixedPosition;
        }

        var path = new UnityEngine.AI.NavMeshPath();
        // fixedPosition = new Vector3(5.0f, 0.9f, -2.7f);
        bool pathSuccess = UnityEngine.AI.NavMesh.CalculatePath(agentTransform.position, fixedPosition,  UnityEngine.AI.NavMesh.AllAreas, path);
        Debug.Log("Sourcepos: " + agentTransform.position + "FixedPos: " + fixedPosition);
        Debug.Log("Destination: " + navMeshAgent.destination + " Distance: " + navMeshAgent.remainingDistance + " succ " + pathSuccess + " status " + path.status);

       var pathDistance = 0.0;
       for (int i = 0; i < path.corners.Length - 1; i++) {
            Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.red, 10.0f);
            Debug.Log("P i:" + i + " : " + path.corners[i] + " i+1:" + i + 1 + " : " + path.corners[i]);
            pathDistance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
       }
        return path;
    }

    bool done = false;

    void Update()
    {
        if (!done) {
            // var targetSimObject = goal.GetComponentInChildren<SimObjPhysics>();
            // Debug.Log("Set to target");
            // SetSimObjectNavMeshTarget(targetSimObject);
            // done = true;

            

            // this.PhysicsController.GetShortestPath( new ServerAction{ objectId = "Television|+06.14|+01.31|-04.22", useAgentTransform=true });
            // StartCoroutine(ExampleCoroutine());
            done = true;
        }
    }

     IEnumerator ExampleCoroutine()
    {
        //Print the time of when the function is first called.
        Debug.Log("Started Coroutine at timestamp : " + Time.time);

        //yield on a new YieldInstruction that waits for 5 seconds.
        yield return new WaitForSeconds(5);

        this.PhysicsController.GetShortestPath( new ServerAction{ objectId = "Television|+06.14|+01.31|-04.22", useAgentTransform=true });
        //After we have waited 5 seconds print the time again.
        Debug.Log("Finished Coroutine at timestamp : " + Time.time);
    }
}