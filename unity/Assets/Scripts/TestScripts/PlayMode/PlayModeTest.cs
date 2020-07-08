using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;


namespace Tests {
    public class PlayModeTest
    {
        private GameObject gameObject;
        private MCSMain main;

        [SetUp]
        public void Setup(){
            gameObject = GameObject.Instantiate(new GameObject());
            main = gameObject.AddComponent<MCSMain>();

        }
        

        [UnityTest]
        public IEnumerator FirstTest() {
            yield return null;
            Assert.AreEqual(false, false);

        }

        [TearDown]
        public void TearDown()
        {
            GameObject.Destroy(gameObject);
        }
    
    }
}

