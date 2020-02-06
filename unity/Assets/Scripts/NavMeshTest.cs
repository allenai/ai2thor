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
    // Start is called before the first frame update
    void Start()
    {
        //  var targetPosition = new Vector3(goal.position.x, goal.position.y, goal.position.z); 
        //  var targetSimObject = goal.GetComponentInChildren<SimObjPhysics>();
        // PhysicsController = GetComponent<PhysicsRemoteFPSAgentController>();

        // var reachaBlePositions = PhysicsController.getReachablePositions();
        // var sortedPositions = reachaBlePositions.OrderBy( pos => (targetPosition - pos).sqrMagnitude);
        // var capsuleCollider = PhysicsController.GetComponent<CapsuleCollider>();
        // var agentCamera = PhysicsController.GetComponentInChildren<Camera>();
        // var agentTransform = PhysicsController.transform;
        // // var originalCamera = new Camera().CopyFrom(agentCamera);
        // var camera = new Camera();
        // var originalAgentPosition = agentTransform.position;
        // var orignalAgentRotation = agentTransform.rotation;

        // var targetPositionYAgent = targetPosition;
        // targetPositionYAgent.y = agentTransform.position.y;
        // foreach (var pos in sortedPositions) {
        //     agentTransform.position = pos;
        //     agentTransform.LookAt(targetPosition);

        //     var visibleSimObjects = PhysicsController.GetAllVisibleSimObjPhysics(PhysicsController.maxVisibleDistance);
        //     if (visibleSimObjects.Any(sop => sop.uniqueID == targetSimObject.uniqueID)) {
        //         break;
        //     }
        //     // Vector3 point0, point1;
        //     // float radius;
        //     // PhysicsExtensions.ToWorldSpaceCapsule(capsuleCollider, out point0, out point1, out radius);
        //     // PhysicsRemoteFPSAgentController.GetAllVisibleSimObjects(Vector3 capsuleTopWorld, Vector3 capsuleBottomWorld, float radius, Camera camera, float maxDistance);
        // }

        // agentTransform.position = originalAgentPosition;
        // agentTransform.rotation = orignalAgentRotation;

        // navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        // Debug.Log("Initial: " + navMeshAgent.destination);

        // NavMeshHit hit;
        // // NavMesh.SamplePosition(pos, out hit, 1.0f, NavMesh.AllAreas)
        // Debug.Log("Walkable " + (1 << NavMesh.GetAreaFromName("Walkable")) + " pos " + targetPosition);
        // // NavMesh.SamplePosition(pos, out hit, 3.0f, 1 << NavMesh.GetAreaFromName("Walkable"));
        // // NavMesh.FindClosestEdge(pos, out hit, NavMesh.GetAreaFromName("Walkable"));
        // // hit.position
        // // Gizmos.color = Color.red;
        // // Gizmos.DrawSphere(hit.position, 1.0f);
        // // Debug.DrawLine(pos, hit.position, Color.red, 10.0f);


        // // navMeshAgent.destination = targetPosition;


        // // Debug.Log("POs " + hit.position);
        //  Debug.Log("Dest: " + navMeshAgent.destination);

        //  RaycastHit raycastHit;
        //  var diff = (navMeshAgent.destination - targetPosition);
        //  diff.y = 0.0f;
        //  var dir = diff.normalized;

        //  bool hasHit = Physics.Raycast(targetPosition, dir, out raycastHit, 20.0f);
        //  Debug.Log("has hit " + hasHit + " post " + raycastHit.point);
        //  Debug.DrawLine(targetPosition, navMeshAgent.destination, Color.red, 50.0f);

       


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
            // Vector3 point0, point1;
            // float radius;
             // PhysicsExtensions.ToWorldSpaceCapsule(capsuleCollider, out point0, out point1, out radius);
            // PhysicsRemoteFPSAgentController.GetAllVisibleSimObjects(Vector3 capsuleTopWorld, Vector3 capsuleBottomWorld, float radius, Camera camera, float maxDistance);
        }

        agentTransform.position = originalAgentPosition;
        agentTransform.rotation = orignalAgentRotation;

        navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (success) {
            // navMeshAgent.destination = fixedPosition;
        }

        var path = new UnityEngine.AI.NavMeshPath();
        bool pathSuccess = UnityEngine.AI.NavMesh.CalculatePath(agentTransform.position, fixedPosition,  UnityEngine.AI.NavMesh.AllAreas, path);
        Debug.Log("Destination: " + navMeshAgent.destination + " Distance: " + navMeshAgent.remainingDistance + " succ " + pathSuccess + " status " + path.status);

       var pathDistance = 0.0;
       for (int i = 0; i < path.corners.Length - 1; i++) {
            Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.red, 10.0f);
            Debug.Log("P i:" + i + " : " + path.corners[i] + " i+1:" + i + 1 + " : " + path.corners[i]);
            pathDistance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
       }
        // NavMesh.SamplePosition(pos, out hit, 1.0f, NavMesh.AllAreas)
        // Debug.Log("Walkable " + (1 << NavMesh.GetAreaFromName("Walkable")) + " pos " + targetPosition);

        return path;
    }

    bool done = false;

    // Update is called once per frame
    void Update()
    {
        if (!done) {
            var targetSimObject = goal.GetComponentInChildren<SimObjPhysics>();
            Debug.Log("Set to target");
            SetSimObjectNavMeshTarget(targetSimObject);
            done = true;
        }
        // Debug.Log(navMeshAgent.destination);

        // navMeshAgent.destination = new Vector3(goal.position.x, 0.0f, goal.position.z); 
        // Debug.Log("PAth Status: " + navMeshAgent.pathStatus);
    }
}