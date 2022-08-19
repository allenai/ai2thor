using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq;
using UnityEngine.SceneManagement;

public class LocomotionToggle : MonoBehaviour {
    /// <summary>
    /// The <c>TeleportController<c> class manages teleportation. 
    /// When the primary button is pressed down activates teleporation mode.
    /// When primary button let go reverts back to base controllers
    /// </summary>
    /// 

    [SerializeField] private GameObject _reticle;

    [Header("Left Hand")]
    [SerializeField] private ActionBasedController _leftAgentXRController;
    [SerializeField] private XRRayInteractor _leftAgentXRRayInteractor;
    [SerializeField] private ActionBasedController _leftUserXRController;
    [SerializeField] private XRRayInteractor _leftUserXRRayInteractor;


    [Header("Right Hand")]
    [SerializeField] private ActionBasedController _rightAgentXRController;
    [SerializeField] private XRRayInteractor _rightAgentXRRayInteractor;
    [SerializeField] private ActionBasedController _rightUserXRController;
    [SerializeField] private XRRayInteractor _rightUserXRRayInteractor;

    private GameObject _agentFloorCol;
    private GameObject _userFloorCol;

    void Start() { 
        var floor = GameObject.FindGameObjectsWithTag("SimObjPhysics").Single(i => i.GetComponent<SimObjPhysics>() != null && i.GetComponent<SimObjPhysics>().Type == SimObjType.Floor).GetComponent<SimObjPhysics>();
        if (floor != null) {
            _userFloorCol = floor.MyColliders[0].gameObject;

            // Dupilcate Collider
            _agentFloorCol = GameObject.Instantiate(_userFloorCol);
            _agentFloorCol.transform.parent = _userFloorCol.transform.parent;

            // Add Teleportation Area
            Agent_TeleportationArea agentArea = _agentFloorCol.AddComponent<Agent_TeleportationArea>();
            TeleportationArea userArea = _userFloorCol.AddComponent<TeleportationArea>();

            // Add reticle
            agentArea.customReticle = _reticle;
            userArea.customReticle = _reticle;


            _agentFloorCol.SetActive(false);
        }
    }

    // Called when you want to activate teleport mode
    public void ToggleLocomotion(bool value) {
        if (_agentFloorCol != null)
            _agentFloorCol.SetActive(value);

        if (_userFloorCol != null)
            _userFloorCol.SetActive(!value);

        // Left
        _leftAgentXRController.enableInputActions = value;
        _leftUserXRController.enableInputActions = !value;
        _leftAgentXRRayInteractor.enabled = value;
        _leftUserXRRayInteractor.enabled = !value;
        if (_leftUserXRController.model != null)
            _leftUserXRController.model.gameObject.SetActive(!value);

        // Right
        _rightAgentXRController.enableInputActions = value;
        _rightUserXRController.enableInputActions = !value;
        _rightAgentXRRayInteractor.enabled = value;
        _rightUserXRRayInteractor.enabled = !value;
        if (_rightUserXRController.model != null)
            _rightUserXRController.model.gameObject.SetActive(!value);
    }

    public void DisableLocomotion() {
        _agentFloorCol.SetActive(false);

        _userFloorCol.SetActive(false);
    }
}
