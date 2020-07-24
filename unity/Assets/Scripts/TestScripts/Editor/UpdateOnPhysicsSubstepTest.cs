using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class UpdateOnPhysicsSubstepTest
    {
        private int stepBeginInt = 0;
        private int stepEndInt = 2;
        private GameObject gameObject1;
        private GameObject gameObject2;
        private GameObject gameObject3;

        private GameObject gameObject4;
        private GameObject gameObject5;
        private MCSMain main;

        private MCSConfigScene configScene = new MCSConfigScene();
        private MCSConfigGoal goal = new MCSConfigGoal();
        MCSConfigTransform mcsTransform = new MCSConfigTransform();
        MCSConfigVector vector = new MCSConfigVector();
        MCSConfigSize size = new MCSConfigSize();
        MCSConfigPhysicsProperties physicsProperties = new MCSConfigPhysicsProperties();


        List<MCSConfigGameObject> objects = new List<MCSConfigGameObject>();
        MCSConfigGameObject object1 = new MCSConfigGameObject();
        MCSConfigGameObject object2 = new MCSConfigGameObject();
        /////
        [SetUp]
        public void Setup()
        {
            gameObject1 = GameObject.Instantiate(new GameObject());
            main = gameObject1.AddComponent<MCSMain>();
            /////
            /////scene
            configScene.name = "NewScene";
            configScene.ceilingMaterial = "wood";
            configScene.floorMaterial = "wood";
            configScene.wallMaterial = "wood";
            configScene.observation = false;
            configScene.screenshot = false;

            goal.description = "Collect the item";
            configScene.goal = goal;
            /////
            /////transform
            vector.x = 0;
            vector.y = 1;
            vector.z = 2;

            mcsTransform.position = vector;
            mcsTransform.rotation = vector;
            mcsTransform.scale = size;

            configScene.performerStart = mcsTransform;
            /////
            /////physics
            physicsProperties.enable = true;
            physicsProperties.angularDrag = 1;
            physicsProperties.drag = 1;
            physicsProperties.bounciness = 1;
            physicsProperties.dynamicFriction = 2;
            physicsProperties.staticFriction = 3;

            configScene.wallProperties = physicsProperties;
            configScene.floorProperties = physicsProperties;

            /////object1
            object1.id = "apple_2";
            object1.kinematic = false;
            object1.mass = 0.5f;
            object1.moveable = true;
            object1.openable = false;
            object1.opened = false;
            object1.physics = true;
            object1.pickupable = true;
            object1.receptacle = false;
            object1.stacking = false;
            
            object1.materials = new List<string> {"wood"};

            object1.salientMaterials = new List<string> {"wood"};
            object1.physicsProperties = physicsProperties;

            object1.controller = "FPS Agent";
            object1.locationParent = "FPS Agent";
            object1.materialFile = "AI2-THOR/Objects/Physics/SimObjsPhysics/Kitchen Objects/Apple/Materials/Apple1_Mat2";
            object1.nullParent = null;
            object1.structure = false;
            object1.type = "apple";

            MCSConfigAction action1 = new MCSConfigAction();
            action1.id = "MoveAhead";
            MCSConfigAction action2 = new MCSConfigAction();
            action1.id = "MoveLeft";
            object1.actions = new List<MCSConfigAction> {action1, action2};

            MCSConfigMove configMove1= new MCSConfigMove();
            configMove1.vector = vector;
            configMove1.stepBegin = stepBeginInt;
            configMove1.stepEnd = stepEndInt;
            MCSConfigMove configMove2= new MCSConfigMove();
            configMove2.vector = vector;
            configMove2.stepBegin = stepBeginInt;
            configMove2.stepEnd = stepEndInt;
            object1.forces = new List<MCSConfigMove>() {configMove1, configMove2};
            object1.moves = new List<MCSConfigMove>() {configMove1, configMove2};

            MCSConfigStepBegin stepBegin = new MCSConfigStepBegin();
            stepBegin.stepBegin = stepBeginInt;
            List<MCSConfigStepBegin> stepBegins = new List<MCSConfigStepBegin> {stepBegin};
            object1.hides = stepBegins;

            MCSConfigResize resize = new MCSConfigResize();
            resize.size = size;
            resize.stepBegin = stepBeginInt;
            resize.stepEnd = stepEndInt;
            List<MCSConfigResize> resizes = new List<MCSConfigResize> {resize};
            object1.resizes = resizes;

            List<MCSConfigMove> configMoves3 = new List<MCSConfigMove>() {configMove1, configMove2};
            object1.rotates = configMoves3;
            
            MCSConfigShow show = new MCSConfigShow();
            show.position = vector;
            show.rotation = vector;
            show.scale = size;
            show.stepBegin = stepBeginInt;
            List<MCSConfigShow> shows = new List<MCSConfigShow>() {show};
            object1.shows = shows;

            MCSConfigTeleport teleport = new MCSConfigTeleport();
            teleport.position = vector;
            object1.teleports = new List<MCSConfigTeleport>() {teleport};

            List<MCSConfigMove> configMoves4 = new List<MCSConfigMove>() {configMove1, configMove2};
            object1.torques = configMoves4;

            object1.SetGameObject(gameObject2);
            object1.SetParentObject(gameObject3);


            /////object2
            object2.id = "chair_2";
            object2.kinematic = false;
            object2.mass = 0.5f;
            object2.moveable = true;
            object2.openable = false;
            object2.opened = false;
            object2.physics = true;
            object2.pickupable = true;
            object2.receptacle = false;
            object2.stacking = false;
            
            object2.materials = new List<string> {"plastic"};

            object2.salientMaterials = new List<string> {"plastic"};
            object2.physicsProperties = physicsProperties;

            object2.controller = "FPS Agent";
            object2.locationParent = "FPS Agent";
            object2.materialFile = "MCS/AI2-THOR/Physics/Materials/Plastics/BlackPlastic";
            object2.nullParent = null;
            object2.structure = false;
            object2.type = "chair";

            MCSConfigAction action3 = new MCSConfigAction();
            action3.id = "MoveAhead";
            MCSConfigAction action4 = new MCSConfigAction();
            action3.id = "MoveLeft";
            object2.actions = new List<MCSConfigAction> {action3, action4};

            MCSConfigMove configMove3= new MCSConfigMove();
            configMove1.vector = vector;
            configMove1.stepBegin = stepBeginInt;
            configMove1.stepEnd = stepEndInt;
            MCSConfigMove configMove4= new MCSConfigMove();
            configMove2.vector = vector;
            configMove2.stepBegin = stepBeginInt;
            configMove2.stepEnd = stepEndInt;
            object2.forces = new List<MCSConfigMove>() {configMove3, configMove4};
            object2.moves = new List<MCSConfigMove>() {configMove3, configMove4};

            MCSConfigStepBegin stepBegin2 = new MCSConfigStepBegin();
            stepBegin.stepBegin = stepBeginInt;
            List<MCSConfigStepBegin> stepBegins2 = new List<MCSConfigStepBegin> {stepBegin2};
            object2.hides = stepBegins;

            MCSConfigResize resize2 = new MCSConfigResize();
            resize.size = size;
            resize.stepBegin = stepBeginInt;
            resize.stepEnd = stepEndInt;
            List<MCSConfigResize> resizes2 = new List<MCSConfigResize> {resize};
            object2.resizes = resizes;

            List<MCSConfigMove> configMoves5 = new List<MCSConfigMove>() {configMove3, configMove4};
            object2.rotates = configMoves5;
            
            MCSConfigShow show2 = new MCSConfigShow();
            show.position = vector;
            show.rotation = vector;
            show.scale = size;
            show.stepBegin = stepBeginInt;
            List<MCSConfigShow> shows2 = new List<MCSConfigShow>() {show};
            object2.shows = shows;

            MCSConfigTeleport teleport2 = new MCSConfigTeleport();
            teleport.position = vector;
            object2.teleports = new List<MCSConfigTeleport>() {teleport};

            List<MCSConfigMove> configMoves6 = new List<MCSConfigMove>() {configMove3, configMove4};
            object2.torques = configMoves6;

            object2.SetGameObject(gameObject4);
            object2.SetParentObject(gameObject5);



            /////object list for scene
            objects.Add(object1);
            objects.Add(object2);

            //sets current scene
            main.SetCurrentScene(configScene);

        }
        
        [Test]
        public void UpdateOnPhysicsSubstepTest1()
        {
            main.UpdateOnPhysicsSubstep(1);
        }
    }
}
