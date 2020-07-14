using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class ChangeCurrentSceneTest
    {
        private GameObject gameObject;
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
            gameObject = GameObject.Instantiate(new GameObject());
            main = gameObject.AddComponent<MCSMain>();
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
            vector.y = 0;
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
            /////
            /////objects
            objects.Add(object1);
            objects.Add(object2);

        }
        
        [Test]
        public void ChangeCurrentSceneTest1()
        {
            
        }
    }
}
