using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using RandomExtensions;
using UnityEngine.AI;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof(CharacterController))]
    public class DroneFPSAgentController : BaseFPSAgentController 
    {
        public GameObject basket;
        public GameObject basketTrigger;
        public DroneObjectLauncher DroneObjectLauncher;
        public List<SimObjPhysics> caught_object = new List<SimObjPhysics>();
        public bool hasFixedUpdateHappened = true;//track if the fixed physics update has happened
        protected Vector3 thrust;
        public float dronePositionRandomNoiseSigma = 0f;
        //count of fixed updates for use in droneCurrentTime
        public float fixupdateCnt = 0f;
        // Update is called once per frame
        void Update () 
        {
            
        }

        public override void Start()
        {
			m_Camera = this.gameObject.GetComponentInChildren<Camera>();

			// set agent initial states
			targetRotation = transform.rotation;
			collidedObjects = new string[0];
			collisionsInAction = new List<string>();

            //setting default renderer settings
            //this hides renderers not used in tall mode, and also sets renderer
            //culling in FirstPersonCharacterCull.cs to ignore tall mode renderers
            HideAllAgentRenderers();

			// record initial positions and rotations
			init_position = transform.position;
			init_rotation = transform.rotation;

			agentManager = GameObject.Find("PhysicsSceneManager").GetComponentInChildren<AgentManager>();

            //default nav mesh agent to false cause WHY DOES THIS BREAK THINGS I GUESS IT DOESN TLIKE TELEPORTING
            this.GetComponent<NavMeshAgent>().enabled = false;
        }

        private void LateUpdate()
        {
            #if UNITY_EDITOR || UNITY_WEBGL
            ServerAction action = new ServerAction();
            VisibleSimObjPhysics = VisibleSimObjs(action);
            #endif
        }

        public override void RotateRight(ServerAction action) 
        {
            //if controlCommand.degrees is default (0), rotate by the default rotation amount set on initialize
            if(action.degrees == 0f)
            action.degrees = rotateStepDegrees;

            base.RotateRight(action);
        }

        public override void RotateLeft(ServerAction action) 
        {
            //if controlCommand.degrees is default (0), rotate by the default rotation amount set on initialize
            if(action.degrees == 0f)
            action.degrees = rotateStepDegrees;

            base.RotateLeft(action);
        }

        void FixedUpdate()
        {
            //when in drone mode, automatically pause time and physics simulation here
            //time and physics will continue once emitFrame is called
            //Note: this is to keep drone and object movement in sync, as pausing just object physics would
            //still allow the drone's character controller Move() to function in "real time" and we dont have
            //support for fully continuous drone movement and emitFrame metadata generation at the same time.

            //NOTE/XXX: because of the fixedupdate/lateupdate/delayed coroutine emitframe nonsense going on
            //here, the in-editor axisAlignedBoundingBox metadata for drone objects seems to be offset by some number of updates.
            //it's unclear whether this is only an in-editor debug draw issue, or the actual metadata for the axis
            //aligned box is messed up, but yeah.
            if (hasFixedUpdateHappened)
            {   
                Time.timeScale = 0;
                Physics.autoSimulation = false;
                physicsSceneManager.physicsSimulationPaused = true;
            }   
            else
            {
                fixupdateCnt++;
                hasFixedUpdateHappened = true;
            }

            if (thrust.magnitude > 0.0001 && Time.timeScale != 0)
            {
                if (dronePositionRandomNoiseSigma > 0){
                    var random = new System.Random();
                    var noiseX = (float)random.NextGaussian(0.0f, dronePositionRandomNoiseSigma/3.0f);
                    var noiseY = (float)random.NextGaussian(0.0f, dronePositionRandomNoiseSigma/3.0f);
                    var noiseZ = (float)random.NextGaussian(0.0f, dronePositionRandomNoiseSigma/3.0f);
                    Vector3 noise = new Vector3(noiseX, noiseY, noiseZ);
                    m_CharacterController.Move((thrust * Time.fixedDeltaTime) + noise);
                }else{
                    m_CharacterController.Move(thrust * Time.fixedDeltaTime);
                }
            }
        }

        //generates object metatada based on sim object's properties
        public override ObjectMetadata ObjectMetadataFromSimObjPhysics(SimObjPhysics simObj, bool isVisible) 
        {            
            DroneObjectMetadata objMeta = new DroneObjectMetadata();
            objMeta.isCaught = this.GetComponent<DroneFPSAgentController>().isObjectCaught(simObj);
            objMeta.numSimObjHits = simObj.numSimObjHit;
            objMeta.numFloorHits = simObj.numFloorHit;
            objMeta.numStructureHits = simObj.numStructureHit;
            objMeta.lastVelocity = simObj.lastVelocity;
            
            GameObject o = simObj.gameObject;
            objMeta.name = o.name;
            objMeta.position = o.transform.position;
            objMeta.rotation = o.transform.eulerAngles;
            objMeta.objectType = Enum.GetName(typeof(SimObjType), simObj.Type);
            objMeta.receptacle = simObj.IsReceptacle;

            objMeta.openable = simObj.IsOpenable;
            if (objMeta.openable) {
                objMeta.isOpen = simObj.IsOpen;
            }

            objMeta.toggleable = simObj.IsToggleable;
            if (objMeta.toggleable) {
                objMeta.isToggled = simObj.IsToggled;
            }

            objMeta.breakable = simObj.IsBreakable;
            if(objMeta.breakable) {
                objMeta.isBroken = simObj.IsBroken;
            }

            objMeta.canFillWithLiquid = simObj.IsFillable;
            if (objMeta.canFillWithLiquid) {
                objMeta.isFilledWithLiquid = simObj.IsFilled;
            }

            objMeta.dirtyable = simObj.IsDirtyable;
            if (objMeta.dirtyable) {
                objMeta.isDirty = simObj.IsDirty;
            }

            objMeta.cookable = simObj.IsCookable;
            if (objMeta.cookable) {
                objMeta.isCooked = simObj.IsCooked;
            }

            //if the sim object is moveable or pickupable
            if(simObj.IsPickupable || simObj.IsMoveable)
            {
                //this object should report back mass and salient materials

                string [] salientMaterialsToString = new string [simObj.salientMaterials.Length];

                for(int i = 0; i < simObj.salientMaterials.Length; i++)
                {
                    salientMaterialsToString[i] = simObj.salientMaterials[i].ToString();
                }

                objMeta.salientMaterials = salientMaterialsToString;

                //record the mass unless the object was caught by the drone, which means the
                //rigidbody was disabled
                if (!objMeta.isCaught)
                {
                    objMeta.mass = simObj.Mass;
                }
            }



            //can this object change others to hot?
            objMeta.canChangeTempToHot = simObj.canChangeTempToHot;

            //can this object change others to cold?
            objMeta.canChangeTempToCold = simObj.canChangeTempToCold;

            objMeta.sliceable = simObj.IsSliceable;
            if (objMeta.sliceable) {
                objMeta.isSliced = simObj.IsSliced;
            }

            objMeta.canBeUsedUp = simObj.CanBeUsedUp;
            if (objMeta.canBeUsedUp) {
                objMeta.isUsedUp = simObj.IsUsedUp;
            }

            //object temperature to string
            objMeta.ObjectTemperature = simObj.CurrentObjTemp.ToString();

            objMeta.pickupable = simObj.PrimaryProperty == SimObjPrimaryProperty.CanPickup;//can this object be picked up?
            objMeta.isPickedUp = simObj.isPickedUp;//returns true for if this object is currently being held by the agent

            objMeta.moveable = simObj.PrimaryProperty == SimObjPrimaryProperty.Moveable;

            objMeta.objectId = simObj.ObjectID;

            // TODO: using the isVisible flag on the object causes weird problems
            // in the multiagent setting, explicitly giving this information for now.
            objMeta.visible = isVisible; //simObj.isVisible;

            objMeta.isMoving = simObj.inMotion;//keep track of if this object is actively moving

            if(simObj.PrimaryProperty == SimObjPrimaryProperty.CanPickup || simObj.PrimaryProperty == SimObjPrimaryProperty.Moveable) 
            {
                objMeta.objectOrientedBoundingBox = GenerateObjectOrientedBoundingBox(simObj);
            }
            
            //return world axis aligned bounds for this sim object
            objMeta.axisAlignedBoundingBox = GenerateAxisAlignedBoundingBox(simObj);

            return objMeta;
        }

        public override MetadataWrapper generateMetadataWrapper()
        {
            // AGENT METADATA
            DroneAgentMetadata agentMeta = new DroneAgentMetadata();
            agentMeta.name = "agent";
            agentMeta.position = transform.position;
            agentMeta.rotation = transform.eulerAngles;
            agentMeta.cameraHorizon = m_Camera.transform.rotation.eulerAngles.x;
            if (agentMeta.cameraHorizon > 180) 
            {
                agentMeta.cameraHorizon -= 360;
            }

            //New Drone Stuff for agentMeta
            agentMeta.LauncherPosition = GetLauncherPosition();

            // OTHER METADATA
            MetadataWrapper metaMessage = new MetadataWrapper();
    
            //For Drone controller, currentTime should be based on
            //fixed update passes so use DroneTimeSinceStart instead of TimeSinceStart
            metaMessage.currentTime = DroneTimeSinceStart();

            //agentMeta.FlightMode = FlightMode;
            metaMessage.agent = agentMeta;
            metaMessage.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            metaMessage.objects = this.generateObjectMetadata();
            //check scene manager to see if the scene's objects are at rest
            metaMessage.isSceneAtRest = physicsSceneManager.isSceneAtRest;
            metaMessage.collided = collidedObjects.Length > 0;
            metaMessage.collidedObjects = collidedObjects;
            metaMessage.screenWidth = Screen.width;
            metaMessage.screenHeight = Screen.height;
            metaMessage.cameraPosition = m_Camera.transform.position;
            metaMessage.cameraOrthSize = cameraOrthSize;
            cameraOrthSize = -1f;
            metaMessage.fov = m_Camera.fieldOfView;
            metaMessage.lastAction = lastAction;
            metaMessage.lastActionSuccess = lastActionSuccess;
            metaMessage.errorMessage = errorMessage;
            metaMessage.actionReturn = this.actionReturn;

            if (errorCode != ServerActionErrorCode.Undefined) {
                metaMessage.errorCode = Enum.GetName(typeof(ServerActionErrorCode), errorCode);
            }

            List<InventoryObject> ios = new List<InventoryObject>();

            if (ItemInHand != null) {
                SimObjPhysics so = ItemInHand.GetComponent<SimObjPhysics>();
                InventoryObject io = new InventoryObject();
                io.objectId = so.ObjectID;
                io.objectType = Enum.GetName(typeof(SimObjType), so.Type);
                ios.Add(io);
            }

            metaMessage.inventoryObjects = ios.ToArray();

            // HAND
            metaMessage.hand = new HandMetadata();
            metaMessage.hand.position = AgentHand.transform.position;
            metaMessage.hand.localPosition = AgentHand.transform.localPosition;
            metaMessage.hand.rotation = AgentHand.transform.eulerAngles;
            metaMessage.hand.localRotation = AgentHand.transform.localEulerAngles;

            // EXTRAS
            metaMessage.reachablePositions = reachablePositions;
            metaMessage.flatSurfacesOnGrid = flatten3DimArray(flatSurfacesOnGrid);
            metaMessage.distances = flatten2DimArray(distances);
            metaMessage.normals = flatten3DimArray(normals);
            metaMessage.isOpenableGrid = flatten2DimArray(isOpenableGrid);
            metaMessage.segmentedObjectIds = segmentedObjectIds;
            metaMessage.objectIdsInBox = objectIdsInBox;
            metaMessage.actionIntReturn = actionIntReturn;
            metaMessage.actionFloatReturn = actionFloatReturn;
            metaMessage.actionFloatsReturn = actionFloatsReturn;
            metaMessage.actionStringsReturn = actionStringsReturn;
            metaMessage.actionVector3sReturn = actionVector3sReturn;

            if (alwaysReturnVisibleRange) {
                metaMessage.visibleRange = visibleRange();
            }

            // Resetting things
            reachablePositions = new Vector3[0];
            flatSurfacesOnGrid = new float[0, 0, 0];
            distances = new float[0, 0];
            normals = new float[0, 0, 0];
            isOpenableGrid = new bool[0, 0];
            segmentedObjectIds = new string[0];
            objectIdsInBox = new string[0];
            actionIntReturn = 0;
            actionFloatReturn = 0.0f;
            actionFloatsReturn = new float[0];
            actionStringsReturn = new string[0];
            actionVector3sReturn = new Vector3[0];

            return metaMessage;
		}

        public float DroneTimeSinceStart() {
            return fixupdateCnt * Time.fixedDeltaTime;
        }

        //Flying Drone Agent Controls
        public Vector3 GetFlyingOrientation(ServerAction action, int targetOrientation) {
            Vector3 m;
            int currentRotation = (int) Math.Round(transform.rotation.eulerAngles.y, 0);
            Dictionary<int, Vector3> actionOrientation = new Dictionary<int, Vector3>();
            actionOrientation.Add(0, new Vector3(0f, 0f, 1.0f));
            actionOrientation.Add(90, new Vector3(1.0f, 0.0f, 0.0f));
            actionOrientation.Add(180, new Vector3(0f, 0f, -1.0f));
            actionOrientation.Add(270, new Vector3(-1.0f, 0.0f, 0.0f));
            int delta = (currentRotation + targetOrientation) % 360;

            if (actionOrientation.ContainsKey(delta)) {
                m = actionOrientation[delta];

            } else {
                actionOrientation = new Dictionary<int, Vector3>();
                actionOrientation.Add(0, transform.forward);
                actionOrientation.Add(90, transform.right);
                actionOrientation.Add(180, transform.forward * -1);
                actionOrientation.Add(270, transform.right * -1);
                m = actionOrientation[targetOrientation];
            }

            m *= action.moveMagnitude;

            return m;
        }

        //Flying Drone Agent Controls
        //use get reachable positions, get two positions, one in front of the other
        public Vector3[] SeekTwoPos(Vector3[] shuffledCurrentlyReachable){
            Vector3[] output = new Vector3[2];
            System.Random rnd = new System.Random();
            List<float> y_candidates = new List<float>(new float[] {1.0f, 1.25f, 1.5f});
            foreach (Vector3 p in shuffledCurrentlyReachable){
                foreach (Vector3 p2 in shuffledCurrentlyReachable){
                    if(!p.Equals(p2)){
                        if(p2.z>=(p.z+1.5f) && Mathf.Abs(p.z-p2.z)<=2.5f){
                            //if(Mathf.Abs(p.x-p2.x) < 0.5*Mathf.Abs(p.z-p2.z)){
                            //if(Mathf.Abs(p.x-p2.x) == 0){
                            if(Mathf.Abs(p.x-p2.x) <= 0.5){
                                float y = y_candidates.OrderBy(x => rnd.Next()).ToArray()[0];
                                output[0] = new Vector3(p.x, 1.0f, p.z);
                                output[1] = new Vector3(p2.x, y, p2.z);
                                return output;
                            }
                        }
                    }
                }
            }
            return output;
        }

        //change what timeScale is automatically reset to on emitFrame when in FlightMode
        public void ChangeAutoResetTimeScale(ServerAction action)
        {
            autoResetTimeScale = action.timeScale;
            actionFinished(true);
        }

        public void FlyRandomStart(ServerAction action)
        {   
            System.Random rnd = new System.Random();
            Vector3[] shuffledCurrentlyReachable = getReachablePositions().OrderBy(x => rnd.Next()).ToArray();
            Vector3[] Random_output = SeekTwoPos(shuffledCurrentlyReachable);

            var thrust_dt_drone = Random_output[0];
            var thrust_dt_launcher = Random_output[1];
            thrust_dt_launcher = new Vector3(thrust_dt_launcher.x, action.y, thrust_dt_launcher.z);
            transform.position = thrust_dt_drone;

            this.GetComponent<DroneFPSAgentController>().MoveLauncher(thrust_dt_launcher);
            actionFinished(true);
        }   

        //move drone and launcher to some start position
        public void FlyAssignStart(ServerAction action)
        {   
            //drone uses action.position
            Vector3 thrust_dt = action.position;
            transform.position = thrust_dt;

            //use action.x,y,z for launcher
            Vector3 launcherPosition = new Vector3(action.x, action.y, action.z);
            Vector3 thrust_dt_launcher = launcherPosition;
            this.GetComponent<DroneFPSAgentController>().MoveLauncher(thrust_dt_launcher);

            actionFinished(true);
        }

        //Flying Drone Agent Controls
        public void FlyTo(ServerAction action)
        {   
            transform.rotation = Quaternion.Euler(new Vector3(0.0f, action.rotation.y, 0.0f));
            m_Camera.transform.localEulerAngles = new Vector3(action.horizon, 0.0f, 0.0f);
            thrust += new Vector3(action.x, action.y, action.z);
            actionFinished(true);
        }

        //Flying Drone Agent Controls
        public void FlyAhead(ServerAction action) 
        {
            thrust += GetFlyingOrientation(action, 0);
            actionFinished(true);
        }

        //Flying Drone Agent Controls
        public void FlyBack(ServerAction action) 
        {
            thrust += GetFlyingOrientation(action, 180);
            actionFinished(true);
        }

        //Flying Drone Agent Controls
        public void FlyLeft(ServerAction action) 
        {
            thrust += GetFlyingOrientation(action, 270);
            actionFinished(true);
        }

        //Flying Drone Agent Controls
        public void FlyRight(ServerAction action) 
        {
            thrust += GetFlyingOrientation(action, 90);
            actionFinished(true);
        }

        //Flying Drone Agent Controls
        public void FlyUp(ServerAction action) 
        {
            //Vector3 targetPosition = transform.position + transform.up * action.moveMagnitude;
            //transform.position = targetPosition;
            thrust += new Vector3(0, action.moveMagnitude, 0);
            actionFinished(true);
        }

        //Flying Drone Agent Controls
        public void FlyDown(ServerAction action) 
        {
            //Vector3 targetPosition = transform.position + -transform.up * action.moveMagnitude;
            //transform.position = targetPosition;
            thrust += new Vector3(0, -action.moveMagnitude, 0);
            actionFinished(true);
        }

        //for use with the Drone to be able to launch an object into the air
        //Launch an object at a given Force (action.moveMagnitude), and angle (action.rotation)
        public void LaunchDroneObject(ServerAction action) 
        {
            this.GetComponent<DroneFPSAgentController>().Launch(action);
            actionFinished(true);
            fixupdateCnt = 0f;
        }

        //spawn a launcher object at action.position coordinates
        public void SpawnDroneLauncher(ServerAction action)
        {

            this.GetComponent<DroneFPSAgentController>().SpawnLauncher(action.position);
            actionFinished(true);
        }

        //in case you want to change the fixed delta time
        public void ChangeFixedDeltaTime(ServerAction action) 
        {
            if (action.fixedDeltaTime > 0) 
            {
                Time.fixedDeltaTime = action.fixedDeltaTime;
                actionFinished(true);
            } 
            
            else 
            {
                errorMessage = "FixedDeltaTime must be >0";
                actionFinished(false);
            }
        }

        public void ChangeDronePositionRandomNoiseSigma(ServerAction action) 
        {
            dronePositionRandomNoiseSigma = action.dronePositionRandomNoiseSigma;
            actionFinished(true);
        }

        public bool HasLaunch(SimObjPhysics obj)
        {   
            return DroneObjectLauncher.HasLaunch(obj);
        }

        public bool isObjectCaught(SimObjPhysics check_obj)
        {
            bool caught_object_bool = false;
            foreach (SimObjPhysics obj in caught_object)
            {
                if(obj.Type == check_obj.Type)
                {
                    if(obj.name == check_obj.name)
                    {
                        caught_object_bool = true;
                        //Debug.Log("catch!!!");
                        break;
                    }
                }
            }
            return caught_object_bool;
        }

        public void Launch(ServerAction action)
        {
            Vector3 LaunchAngle = new Vector3(action.x, action.y, action.z);
            DroneObjectLauncher.Launch(action.moveMagnitude, LaunchAngle, action.objectName, action.objectRandom);
        }

        public void MoveLauncher(Vector3 position)
        {
            DroneObjectLauncher.transform.position = position;
        }

        public Vector3 GetLauncherPosition()
        {
            return DroneObjectLauncher.transform.position;
        }

        public void SpawnLauncher(Vector3 position)
        {
            Instantiate(DroneObjectLauncher, position, Quaternion.identity);
        }
    }
}
