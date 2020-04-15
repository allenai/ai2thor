using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityStandardAssets.Characters.FirstPerson
{
[Serializable]
public class ObjectSpanwMetadata
{
public string objectType;
public int objectVariation;
}
    public class DiscreteHidenSeekgentController : MonoBehaviour
    {
        [SerializeField] private float HandMoveMagnitude = 0.1f;
        public PhysicsRemoteFPSAgentController PhysicsController = null;
        private GameObject InputMode_Text = null;
        private ObjectHighlightController highlightController = null;
        private GameObject throwForceBar = null;
        private bool handMode = false;
        private bool visibleObject = true;
        private bool hidingPhase = false;
        public string onlyPickableObjectId = null;
        public bool disableCollistionWithPickupObject = false;
        void Start() 
        {
            var Debug_Canvas = GameObject.Find("DebugCanvasPhysics");
            PhysicsController = gameObject.GetComponent<PhysicsRemoteFPSAgentController>();

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Debug_Canvas.GetComponent<Canvas>().enabled = true; 

            highlightController = new ObjectHighlightController(PhysicsController, PhysicsController.maxVisibleDistance, true, false, 0, 0, true);
            highlightController.SetDisplayTargetText(false);

            // SpawnObjectToHide("{\"objectType\": \"Plunger\", \"objectVariation\": 1}");
        }

        public void OnEnable() {
            InputMode_Text = GameObject.Find("DebugCanvasPhysics/InputModeText");
            throwForceBar = GameObject.Find("DebugCanvasPhysics/ThrowForceBar");
            var camera =  GetComponentInChildren<Camera>();
            camera.fieldOfView = 90.0f;
            // camera.transform.rotation = Quaternion.Euler(30, 0, 0);
             camera.transform.Rotate(30, 0, 0);
            if (InputMode_Text) {
                InputMode_Text.GetComponent<Text>().text = "Point and Click Mode";
            }
            if (throwForceBar) {
                throwForceBar.SetActive(false);
            }
            // InputFieldObj = GameObject.Find("DebugCanvasPhysics/InputField");
            // TODO: move debug input field script from, Input Field and disable here
        }

         public void OnDisable() {
             if (throwForceBar) {
                throwForceBar.SetActive(true);
            }
             // TODO: move debug input field script from, Input Field and enable here
        }

        public void SpawnObjectToHide(string objectMeta) {

            var objectData = new ObjectSpanwMetadata();
            Debug.Log(objectMeta);
		    JsonUtility.FromJsonOverwrite(objectMeta, objectData);
              ServerAction action = new ServerAction(){
                  action = "CreateObject",
                  objectType = objectData.objectType,
                  objectVariation = objectData.objectVariation
              };
            PhysicsController.ProcessControlCommand(action);
            onlyPickableObjectId = objectData.objectType + "|" + objectData.objectVariation;
            this.highlightController.SetOnlyPickableId(onlyPickableObjectId);
            // DisableObjectCollisionWithAgent(onlyPickableObjectId);
        }

         public void SetOnlyPickableObject(string objectId) {
             onlyPickableObjectId = objectId;
            this.highlightController.SetOnlyPickableId(objectId);
        }

        public void SetOnlyObjectId(string objectId) {
            onlyPickableObjectId = objectId;
            this.highlightController.SetOnlyPickableId(objectId);
        }

         public void SetOnlyObjectIdSeeker(string objectId) {
             onlyPickableObjectId = objectId;
            this.highlightController.SetOnlyPickableId(objectId, true);
        }

        public void SpawnAgent(int randomSeed) {
            ServerAction action = new ServerAction(){
                action = "RandomlyMoveAgent",
                randomSeed = randomSeed
            };
            PhysicsController.ProcessControlCommand(action);
        }

        public void DisableObjectCollisionWithAgent(string objectId) {
            var physicsSceneManager = FindObjectOfType<PhysicsSceneManager>();
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                return;
            }
            
            SimObjPhysics target = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];
            disableCollistionWithPickupObject = true;
            foreach (Collider c0 in this.GetComponentsInChildren<Collider>()) {
                foreach (Collider c1 in target.GetComponentsInChildren<Collider>()) {
                    Physics.IgnoreCollision(c0, c1);
                }
            }
        }

        public void SetObjectVisible(bool visible) {
            
                                 visibleObject = visible;
                                 // Debug.Log("Calling disply with "+ visibleObject);
                                 var go = PhysicsController.WhatAmIHolding();
                                PhysicsController.UpdateDisplayGameObject( go, visibleObject);
                                var layer = go.layer;
                                if (!visibleObject) {
                                    // go.layer = LayerMask.NameToLayer("SimObjInvisible");
                                    SetLayerRecursively(go, LayerMask.NameToLayer("SimObjInvisible"));
                                }
                                else {
                                    SetLayerRecursively(go, LayerMask.NameToLayer("SimObjVisible"));
                                }
        }

        public void Step(string serverAction)
		{
			ServerAction controlCommand = new ServerAction();
			JsonUtility.FromJsonOverwrite(serverAction, controlCommand);
			PhysicsController.ProcessControlCommand(controlCommand);
		}

        // public void TeleportAgent(string actionStr) {
        //     var command = new ServerAction();
		//     JsonUtility.FromJsonOverwrite(actionStr, command);
        //     PhysicsController.ProcessControlCommand(command);
        // }

        void Update()
        {
                highlightController.UpdateHighlightedObject(Input.mousePosition);
                highlightController.MouseControls();

                if (PhysicsController.actionComplete) {
                        handMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                        float WalkMagnitude = 0.25f;
                        if (!handMode && !hidingPhase) {
                            if(Input.GetKeyDown(KeyCode.W))
                            {
                                ServerAction action = new ServerAction();
                                action.action = "MoveAhead";
                                action.moveMagnitude = WalkMagnitude;		
                                PhysicsController.ProcessControlCommand(action);
                            
                            }

                            if(Input.GetKeyDown(KeyCode.S))
                            {
                                ServerAction action = new ServerAction();
                                action.action = "MoveBack";
                                action.moveMagnitude = WalkMagnitude;		
                                PhysicsController.ProcessControlCommand(action);
                            
                            }

                            if(Input.GetKeyDown(KeyCode.A))
                            {
                                ServerAction action = new ServerAction();
                                action.action = "MoveLeft";
                                action.moveMagnitude = WalkMagnitude;		
                                PhysicsController.ProcessControlCommand(action);
                            
                            }

                            if(Input.GetKeyDown(KeyCode.D))
                            {
                                ServerAction action = new ServerAction();
                                action.action = "MoveRight";
                                action.moveMagnitude = WalkMagnitude;		
                                PhysicsController.ProcessControlCommand(action);
                            
                            }

                            if(Input.GetKeyDown(KeyCode.LeftArrow) )//|| Input.GetKeyDown(KeyCode.J))
                            {
                                ServerAction action = new ServerAction();
                                // action.action = "RotateLeft";
                                action.action = "RotateLeftSmooth";
                                action.timeStep = 0.4f;
                                PhysicsController.ProcessControlCommand(action); 
                            }

                            if(Input.GetKeyDown(KeyCode.RightArrow) )//|| Input.GetKeyDown(KeyCode.L))
                            {
                                ServerAction action = new ServerAction();
                                // action.action = "RotateRight";
                                action.action = "RotateRightSmooth";
                                action.timeStep = 0.4f;
                                PhysicsController.ProcessControlCommand(action); 
                            }
                        }

                        

                        if (this.PhysicsController.WhatAmIHolding() != null  && handMode)
                        {
                            var actionName = "MoveHandForce";
                            var localPos = new Vector3(0, 0, 0);
                            // Debug.Log(" Key down shift ? " + Input.GetKey(KeyCode.LeftAlt) + " up " + Input.GetKeyDown(KeyCode.UpArrow));
                            if (Input.GetKeyDown(KeyCode.W)) {
                                localPos.y += HandMoveMagnitude;
                            }
                            else if (Input.GetKeyDown(KeyCode.S)) {
                                localPos.y -= HandMoveMagnitude;
                            }
                            else if (Input.GetKeyDown(KeyCode.UpArrow)) {
                                localPos.z += HandMoveMagnitude;
                            }
                            else if (Input.GetKeyDown(KeyCode.DownArrow)) {
                                localPos.z -= HandMoveMagnitude;
                            }
                            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) {
                                localPos.x -= HandMoveMagnitude;
                            }
                            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) {
                                localPos.x += HandMoveMagnitude;
                            }
                            if (actionName != "" && localPos.sqrMagnitude > 0) {
                                ServerAction action = new ServerAction
                                {
                                    action = "MoveHandForce",
                                    x = localPos.x,
                                    y = localPos.y,
                                    z = localPos.z
                                };
                                this.PhysicsController.ProcessControlCommand(action);
                            }

                            if (Input.GetKeyDown(KeyCode.Space)) {
                                SetObjectVisible(true);
                                  var action = new ServerAction
                                    {
                                        action = "DropHandObject",
                                        forceAction = true
                                    };
                                this.PhysicsController.ProcessControlCommand(action);
                            }
                        }
                        else if (handMode) {
                            if (Input.GetKeyDown(KeyCode.Space)) {
                                var withinReach = PhysicsController.FindObjectInVisibleSimObjPhysics(onlyPickableObjectId) != null;
                                if (withinReach) {
                                    ServerAction action = new ServerAction();
                                    action.objectId = onlyPickableObjectId;
                                    action.action = "PickupObject";
                                    PhysicsController.ProcessControlCommand(action);
                                }
                            }
                        }
                        if ((Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.RightControl) ) && PhysicsController.actionComplete) {
                            ServerAction action = new ServerAction();
                            if (this.PhysicsController.isStanding()) {
                                action.action = "Crouch";
                                PhysicsController.ProcessControlCommand(action);
                            }
                            else {
                                 action.action = "Stand";
                                 PhysicsController.ProcessControlCommand(action);
                            }
                            

                        }

                        if (PhysicsController.WhatAmIHolding() != null) {
                             if (Input.GetKeyDown(KeyCode.Space) && !hidingPhase && !handMode) {
                                
                                SetObjectVisible(!visibleObject);
                             }
                        }
            }
        }

        private static void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (null == obj)
        {
            return;
        }
       
        obj.layer = newLayer;
       
        foreach (Transform child in obj.transform)
        {
            if (null == child)
            {
                continue;
            }
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
    }
    
}