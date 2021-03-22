using System.Collections;
using System.Collections.Generic;
//using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class EditModeTest
    {
        private GameObject gameObject;
        private MCSMain main;

        //[SetUp]
        public void Setup()
        {
            gameObject = GameObject.Instantiate(new GameObject());
            main = gameObject.AddComponent<MCSMain>();
        }
        

        //[Test]
        public void FirstTest() 
        {
            //Assert.AreEqual(true, true);
            //Assert.IsFalse(false);
            //Assert.False(false);        
        }
    }
}
