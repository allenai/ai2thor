using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.TestTools;
using UnityStandardAssets.Characters.FirstPerson;
using Assert = UnityEngine.Assertions.Assert;

namespace Tests
{
    public class TestResourceAssetManager : TestBase
    {
        [UnityTest]
        public IEnumerator TestFindAssets()
        {
            yield return null;
            string testAssetPath = "Assets/Resources/FakeResourceAssetManagerMaterial.mat";
            string label = "FakeFindAssetsLabel";
            ResourceAssetManager manager = new ResourceAssetManager();
            try
            {
                List<ResourceAssetReference<Material>> mats =
                    manager.FindResourceAssetReferences<Material>(label);
                Assert.AreEqual(mats.Count, 0);

                Material material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, testAssetPath);
                material = AssetDatabase.LoadMainAssetAtPath(testAssetPath) as Material;
                AssetDatabase.SetLabels(material, new string[] { label });
                manager.RefreshCatalog();
                mats = manager.FindResourceAssetReferences<Material>(label);
                Assert.AreEqual(mats.Count, 1);
            }
            finally
            {
                AssetDatabase.DeleteAsset(testAssetPath);
            }
        }
    }
}
