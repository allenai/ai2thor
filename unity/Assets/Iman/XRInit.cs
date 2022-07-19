using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRInit : MonoBehaviour
{
    [SerializeField] private ControllerToggle _controllerToggle;

    private GameObject _agentFloorCol;
    private GameObject _userFloorCol;
    // Start is called before the first frame update
    void Awake()
    {
        var floor = GameObject.FindGameObjectsWithTag("SimObjPhysics").Single(i => i.GetComponent<SimObjPhysics>() != null && i.GetComponent<SimObjPhysics>().Type == SimObjType.Floor).GetComponent<SimObjPhysics>();
        _userFloorCol = floor.MyColliders[0].gameObject;

        // Dupilcate Collider
        _agentFloorCol = GameObject.Instantiate(_userFloorCol);
        _agentFloorCol.transform.parent = _userFloorCol.transform.parent;

        // Add Teleportation Area
        _agentFloorCol.AddComponent<Agent_TeleportationArea>();
        _userFloorCol.AddComponent<TeleportationArea>();
        _agentFloorCol.SetActive(false);
    }

}
