using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using RandomExtensions;
using UnityEngine.AI;
using UnityEngine.Rendering.PostProcessing;

namespace UnityStandardAssets.Characters.FirstPerson {
    [RequireComponent(typeof(CharacterController))]
    public class DroneFPSAgentController : BaseFPSAgentController {
        public GameObject basket;
        public GameObject basketTrigger;
        public List<SimObjPhysics> caught_object = new List<SimObjPhysics>();
        private bool hasFixedUpdateHappened = true;// track if the fixed physics update has happened
        protected Vector3 thrust;
        public float dronePositionRandomNoiseSigma = 0f;
        // count of fixed updates for use in droneCurrentTime
        public float fixupdateCnt = 0f;
        // Update is called once per frame

        public DroneFPSAgentController(BaseAgentComponent baseAgentComponent, AgentManager agentManager) : base(baseAgentComponent, agentManager) { }


        protected override void resumePhysics() {
            if (Time.timeScale == 0 && !Physics.autoSimulation && physicsSceneManager.physicsSimulationPaused) {
                Time.timeScale = this.autoResetTimeScale;
                Physics.autoSimulation = true;
                physicsSceneManager.physicsSimulationPaused = false;
                this.hasFixedUpdateHappened = false;
            }
        }

        public override void InitializeBody() {
            VisibilityCapsule = DroneVisCap;
            m_CharacterController.center = new Vector3(0, 0, 0);
            m_CharacterController.radius = 0.2f;
            m_CharacterController.height = 0.0f;

            CapsuleCollider cc = this.GetComponent<CapsuleCollider>();
            cc.center = m_CharacterController.center;
            cc.radius = m_CharacterController.radius;
            cc.height = m_CharacterController.height;

            m_Camera.GetComponent<PostProcessVolume>().enabled = false;
            m_Camera.GetComponent<PostProcessLayer>().enabled = false;

            // camera position set forward a bit for drone
            m_Camera.transform.localPosition = new Vector3(0, 0, 0.2f);

            // camera FOV for drone
            m_Camera.fieldOfView = 150f;

            // default camera stand/crouch for drone mode since drone doesn't stand or crouch
            standingLocalCameraPosition = m_Camera.transform.localPosition;
            crouchingLocalCameraPosition = m_Camera.transform.localPosition;

            // drone also needs to toggle on the drone basket and vis cap
            DroneBasket.SetActive(true);
            DroneVisCap.SetActive(true);
        }


        public void MoveLeft(ServerAction action) {
            moveCharacter(action, 270);
        }

        public void MoveRight(ServerAction action) {
            moveCharacter(action, 90);
        }

        public void MoveAhead(ServerAction action) {
            moveCharacter(action, 0);
        }

        public void MoveBack(ServerAction action) {
            moveCharacter(action, 180);
        }

        public void MoveRelative(ServerAction action) {
            var moveLocal = new Vector3(action.x, 0, action.z);
            Vector3 moveWorldSpace = transform.rotation * moveLocal;
            moveWorldSpace.y = Physics.gravity.y * this.m_GravityMultiplier;
            m_CharacterController.Move(moveWorldSpace);
            actionFinished(true);
        }

        public void RotateRight(ServerAction action) {
            // if controlCommand.degrees is default (0), rotate by the default rotation amount set on initialize
            if (action.degrees == 0f) {
                action.degrees = rotateStepDegrees;
            }

            transform.Rotate(0, action.degrees, 0);
            actionFinished(true);
        }

        public void RotateLeft(ServerAction action) {
            // if controlCommand.degrees is default (0), rotate by the default rotation amount set on initialize
            if (action.degrees == 0f) {
                action.degrees = rotateStepDegrees;
            }

            transform.Rotate(0, -action.degrees, 0);
            actionFinished(true);
        }

        public override void FixedUpdate() {
            // when in drone mode, automatically pause time and physics simulation here
            // time and physics will continue once emitFrame is called
            // Note: this is to keep drone and object movement in sync, as pausing just object physics would
            // still allow the drone's character controller Move() to function in "real time" and we dont have
            // support for fully continuous drone movement and emitFrame metadata generation at the same time.

            // NOTE/XXX: because of the fixedupdate/lateupdate/delayed coroutine emitframe nonsense going on
            // here, the in-editor axisAlignedBoundingBox metadata for drone objects seems to be offset by some number of updates.
            // it's unclear whether this is only an in-editor debug draw issue, or the actual metadata for the axis
            // aligned box is messed up, but yeah.

            if (hasFixedUpdateHappened) {
                Time.timeScale = 0;
                Physics.autoSimulation = false;
                physicsSceneManager.physicsSimulationPaused = true;
            } else {
                fixupdateCnt++;
                hasFixedUpdateHappened = true;
            }

            if (thrust.magnitude > 0.0001 && Time.timeScale != 0) {
                if (dronePositionRandomNoiseSigma > 0) {
                    var noiseX = (float)systemRandom.NextGaussian(0.0f, dronePositionRandomNoiseSigma / 3.0f);
                    var noiseY = (float)systemRandom.NextGaussian(0.0f, dronePositionRandomNoiseSigma / 3.0f);
                    var noiseZ = (float)systemRandom.NextGaussian(0.0f, dronePositionRandomNoiseSigma / 3.0f);
                    Vector3 noise = new Vector3(noiseX, noiseY, noiseZ);
                    m_CharacterController.Move((thrust * Time.fixedDeltaTime) + noise);
                } else {
                    m_CharacterController.Move(thrust * Time.fixedDeltaTime);
                }
            }

            if (this.agentState == AgentState.PendingFixedUpdate) {
                this.agentState = AgentState.ActionComplete;
            }

        }

        // generates object metadata based on sim object's properties
        public override ObjectMetadata ObjectMetadataFromSimObjPhysics(SimObjPhysics simObj, bool isVisible, bool isInteractable) {
            DroneObjectMetadata objMeta = new DroneObjectMetadata();
            objMeta.isCaught = this.isObjectCaught(simObj);
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
            if (objMeta.breakable) {
                objMeta.isBroken = simObj.IsBroken;
            }

            objMeta.canFillWithLiquid = simObj.IsFillable;
            if (objMeta.canFillWithLiquid) {
                objMeta.isFilledWithLiquid = simObj.IsFilled;
                objMeta.fillLiquid = simObj.FillLiquid;
            }

            objMeta.dirtyable = simObj.IsDirtyable;
            if (objMeta.dirtyable) {
                objMeta.isDirty = simObj.IsDirty;
            }

            objMeta.cookable = simObj.IsCookable;
            if (objMeta.cookable) {
                objMeta.isCooked = simObj.IsCooked;
            }

            // if the sim object is moveable or pickupable
            if (simObj.IsPickupable || simObj.IsMoveable) {
                // this object should report back mass and salient materials

                string[] salientMaterialsToString = new string[simObj.salientMaterials.Length];

                for (int i = 0; i < simObj.salientMaterials.Length; i++) {
                    salientMaterialsToString[i] = simObj.salientMaterials[i].ToString();
                }

                objMeta.salientMaterials = salientMaterialsToString;

                // record the mass unless the object was caught by the drone, which means the
                // rigidbody was disabled
                if (!objMeta.isCaught) {
                    objMeta.mass = simObj.Mass;
                }
            }

            // can this object change others to hot?
            objMeta.isHeatSource = simObj.isHeatSource;

            // can this object change others to cold?
            objMeta.isColdSource = simObj.isColdSource;

            objMeta.sliceable = simObj.IsSliceable;
            if (objMeta.sliceable) {
                objMeta.isSliced = simObj.IsSliced;
            }

            objMeta.canBeUsedUp = simObj.CanBeUsedUp;
            if (objMeta.canBeUsedUp) {
                objMeta.isUsedUp = simObj.IsUsedUp;
            }

            // object temperature to string
            objMeta.temperature = simObj.CurrentObjTemp.ToString();

            objMeta.pickupable = simObj.PrimaryProperty == SimObjPrimaryProperty.CanPickup;// can this object be picked up?
            objMeta.isPickedUp = simObj.isPickedUp;// returns true for if this object is currently being held by the agent

            objMeta.moveable = simObj.PrimaryProperty == SimObjPrimaryProperty.Moveable;

            objMeta.objectId = simObj.ObjectID;

            // TODO: using the isVisible flag on the object causes weird problems
            // in the multiagent setting, explicitly giving this information for now.
            objMeta.visible = isVisible; // simObj.isVisible;

            //determines if the objects is unobstructed and interactable. Objects visible behind see-through geometry like glass will be isInteractable=False even if visible
            //note using forceAction=True will ignore the isInteractable requirement
            objMeta.isInteractable = isInteractable;

            objMeta.isMoving = simObj.inMotion;// keep track of if this object is actively moving

            objMeta.objectOrientedBoundingBox = simObj.ObjectOrientedBoundingBox;

            // return world axis aligned bounds for this sim object
            objMeta.axisAlignedBoundingBox = simObj.AxisAlignedBoundingBox;

            return objMeta;
        }

        public override MetadataWrapper generateMetadataWrapper() {
            MetadataWrapper metadata = base.generateMetadataWrapper();

            // For Drone controller, currentTime should be based on
            // fixed update passes so use DroneTimeSinceStart instead of TimeSinceStart
            metadata.currentTime = DroneTimeSinceStart();

            // TODO: clean this up with reflection.
            // it works, but will not update when something changes to AgentMetadata
            // metadata.agent = new 
            AgentMetadata baseAgent = metadata.agent;

            DroneAgentMetadata droneMeta = new DroneAgentMetadata();
            droneMeta.name = "drone";
            droneMeta.position = baseAgent.position;
            droneMeta.rotation = baseAgent.rotation;
            droneMeta.cameraHorizon = baseAgent.cameraHorizon;
            droneMeta.inHighFrictionArea = baseAgent.inHighFrictionArea;

            // New drone stuff for agent metadata
            droneMeta.launcherPosition = GetLauncherPosition();

            metadata.agent = droneMeta;
            return metadata;
        }

        public float DroneTimeSinceStart() {
            return fixupdateCnt * Time.fixedDeltaTime;
        }

        // Flying drone agent controls
        public Vector3 GetFlyingOrientation(float moveMagnitude, int targetOrientation) {
            Vector3 m;
            int currentRotation = (int)Math.Round(transform.rotation.eulerAngles.y, 0);
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

            m *= moveMagnitude;

            return m;
        }

        // Flying drone agent controls
        // use get reachable positions, get two positions, one in front of the other
        public Vector3[] SeekTwoPos(Vector3[] shuffledCurrentlyReachable) {
            Vector3[] output = new Vector3[2];
            List<float> y_candidates = new List<float>(new float[] { 1.0f, 1.25f, 1.5f });
            foreach (Vector3 p in shuffledCurrentlyReachable) {
                foreach (Vector3 p2 in shuffledCurrentlyReachable) {
                    if (!p.Equals(p2)) {
                        if (p2.z >= (p.z + 1.5f) && Mathf.Abs(p.z - p2.z) <= 2.5f) {
                            // if(Mathf.Abs(p.x-p2.x) < 0.5*Mathf.Abs(p.z-p2.z)){
                            // if(Mathf.Abs(p.x-p2.x) == 0){
                            if (Mathf.Abs(p.x - p2.x) <= 0.5) {
                                float y = y_candidates.OrderBy(x => systemRandom.Next()).ToArray()[0];
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

        // change what timeScale is automatically reset to on emitFrame when in FlightMode
        public void ChangeAutoResetTimeScale(float timeScale) {
            autoResetTimeScale = timeScale;
            actionFinished(true);
        }

        public void Teleport(
            Vector3? position = null, Vector3? rotation = null, float? horizon = null, bool forceAction = false
        ) {
            base.teleport(position: position, rotation: rotation, horizon: horizon, forceAction: forceAction);
            actionFinished(success: true);
        }

        public void TeleportFull(
            Vector3 position, Vector3 rotation, float horizon, bool forceAction = false
        ) {
            base.teleportFull(position: position, rotation: rotation, horizon: horizon, forceAction: forceAction);
            actionFinished(success: true);
        }

        public void FlyRandomStart(float y) {
            Vector3[] shuffledCurrentlyReachable = getReachablePositions().OrderBy(x => systemRandom.Next()).ToArray();
            Vector3[] Random_output = SeekTwoPos(shuffledCurrentlyReachable);

            var thrust_dt_drone = Random_output[0];
            var thrust_dt_launcher = Random_output[1];
            thrust_dt_launcher = new Vector3(thrust_dt_launcher.x, y, thrust_dt_launcher.z);
            transform.position = thrust_dt_drone;

            MoveLauncher(thrust_dt_launcher);
            actionFinished(true);
        }

        // move drone and launcher to some start position
        // using the 'position' variable name is an artifact from using ServerAction.position for the thrust_dt
        public void FlyAssignStart(Vector3 position, float x, float y, float z) {
            // drone uses action.position
            Vector3 thrust_dt = position;
            transform.position = thrust_dt;

            // use action.x,y,z for launcher
            Vector3 launcherPosition = new Vector3(x, y, z);
            Vector3 thrust_dt_launcher = launcherPosition;
            MoveLauncher(thrust_dt_launcher);

            actionFinished(true);
        }

        // Flying Drone Agent Controls
        public void FlyTo(float x, float y, float z, Vector3 rotation, float horizon) {
            transform.rotation = Quaternion.Euler(new Vector3(0.0f, rotation.y, 0.0f));
            m_Camera.transform.localEulerAngles = new Vector3(horizon, 0.0f, 0.0f);
            thrust += new Vector3(x, y, z);
            actionFinished(true);
        }

        // Flying Drone Agent Controls
        public void FlyAhead(float moveMagnitude) {
            thrust += GetFlyingOrientation(moveMagnitude, 0);
            actionFinished(true);
        }

        // Flying Drone Agent Controls
        public void FlyBack(float moveMagnitude) {
            thrust += GetFlyingOrientation(moveMagnitude, 180);
            actionFinished(true);
        }

        // Flying Drone Agent Controls
        public void FlyLeft(float moveMagnitude) {
            thrust += GetFlyingOrientation(moveMagnitude, 270);
            actionFinished(true);
        }

        // Flying Drone Agent Controls
        public void FlyRight(float moveMagnitude) {
            thrust += GetFlyingOrientation(moveMagnitude, 90);
            actionFinished(true);
        }

        // Flying Drone Agent Controls
        public void FlyUp(float moveMagnitude) {
            // Vector3 targetPosition = transform.position + transform.up * action.moveMagnitude;
            // transform.position = targetPosition;
            thrust += new Vector3(0, moveMagnitude, 0);
            actionFinished(true);
        }

        // Flying Drone Agent Controls
        public void FlyDown(float moveMagnitude) {
            // Vector3 targetPosition = transform.position + -transform.up * action.moveMagnitude;
            // transform.position = targetPosition;
            thrust += new Vector3(0, -moveMagnitude, 0);
            actionFinished(true);
        }

        // for use with the Drone to be able to launch an object into the air
        // Launch an object at a given Force (action.moveMagnitude), and angle (action.rotation)
        public void LaunchDroneObject(float moveMagnitude, string objectName, bool objectRandom, float x, float y, float z) {
            Launch(moveMagnitude, objectName, objectRandom, x, y, z);
            actionFinished(true);
            fixupdateCnt = 0f;
        }

        // spawn a launcher object at action.position coordinates
        public void SpawnDroneLauncher(Vector3 position) {

            SpawnLauncher(position);
            actionFinished(true);
        }

        // in case you want to change the fixed delta time
        public void ChangeFixedDeltaTime(float? fixedDeltaTime = null) {
            var fdtime = fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime);

            if (fdtime > 0) {
                Time.fixedDeltaTime = fdtime;
                actionFinished(true);
            } else {
                errorMessage = "FixedDeltaTime must be >0";
                actionFinished(false);
            }
        }

        public void ChangeDronePositionRandomNoiseSigma(float dronePositionRandomNoiseSigma = 0.0f) {
            this.dronePositionRandomNoiseSigma = dronePositionRandomNoiseSigma;
            actionFinished(true);
        }

        public bool HasLaunch(SimObjPhysics obj) {
            return DroneObjectLauncher.HasLaunch(obj);
        }

        public bool isObjectCaught(SimObjPhysics check_obj) {
            bool caught_object_bool = false;
            foreach (SimObjPhysics obj in caught_object) {
                if (obj.Type == check_obj.Type) {
                    if (obj.name == check_obj.name) {
                        caught_object_bool = true;
                        // Debug.Log("catch!!!");
                        break;
                    }
                }
            }
            return caught_object_bool;
        }

        public void Launch(float moveMagnitude, string objectName, bool objectRandom, float x, float y, float z) {
            Vector3 LaunchAngle = new Vector3(x, y, z);
            DroneObjectLauncher.Launch(this, moveMagnitude, LaunchAngle, objectName, objectRandom);
        }

        public void MoveLauncher(Vector3 position) {
            DroneObjectLauncher.transform.position = position;
        }

        public Vector3 GetLauncherPosition() {
            return DroneObjectLauncher.transform.position;
        }

        public void SpawnLauncher(Vector3 position) {
            UnityEngine.Object.Instantiate(DroneObjectLauncher, position, Quaternion.identity);
        }

    }
}
