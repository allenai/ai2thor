using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;
using UnityStandardAssets.Characters.FirstPerson;
using Thor.Procedural;
using Thor.Procedural.Data;
using System.Linq;
// using Priority_Queue;

namespace Tests {
    public class TestProceduralAssetCache
    {
        [UnityTest]
        public IEnumerator TestBinaryValuedPriority() {
            
            var preloadedContent = new List<string>() {
                "1",
                "2",
                "3",
                "4",
                "5"
            };

            int minPrioVal = 0;
            int maxPrioVal = 1;

            var cache = new ProceduralLRUCacheAssetMap<string>(
                preloadedContent.GroupBy(p => p).ToDictionary(p => p.Key, p => p.First()),
                rankingMinValue: minPrioVal,
                rankingMaxValue: maxPrioVal
            );

            cache.addAsset("6", "6", procedural: true);
            cache.addAsset("7", "7", procedural: true);
            cache.addAsset("8", "8", procedural: true);
           
            Assert.AreEqual(cache.Count(), 8);

            cache.touch(new List<string> {
                "6",
                "8"
            });

            Assert.AreEqual(cache.priorityMinValue, minPrioVal+1);
            Assert.AreEqual(cache.priorityMaxValue, maxPrioVal+1);

            cache.removeLRU(limit: 2);

            var shouldRemain = preloadedContent.Concat(new List<string>{
                "6",
                "8"
            });
            Debug.Log($"keys: {string.Join(", ", cache.Keys())}");

            Assert.IsTrue(cache.Keys().All(k => shouldRemain.Contains(k)));

            yield return true;
        }

        [UnityTest]
        public IEnumerator TestMultiValuedPriority() {
            
            var preloadedContent = new List<string>() {
                "1",
                "2",
                "3",
                "4",
                "5"
            };

            int minPrioVal = 0;
            int maxPrioVal = 10;

            var cache = new ProceduralLRUCacheAssetMap<string>(
                preloadedContent.GroupBy(p => p).ToDictionary(p => p.Key, p => p.First()),
                rankingMinValue: minPrioVal,
                rankingMaxValue: maxPrioVal
            );

            cache.addAsset("6", "6", procedural: true);
            cache.addAsset("7", "7", procedural: true);
            cache.addAsset("8", "8", procedural: true);
           
            Assert.AreEqual(cache.Count(), 8);

            cache.touch(new List<string> {
                "6",
                "8"
            });

            cache.touch(new List<string> {
                "6",
            });

            cache.touch(new List<string> {
                "6",
            });

            cache.touch(new List<string> {
                "8",
            });

            cache.removeLRU(limit: 1);

            var shouldRemain = preloadedContent.Concat(new List<string>{
                "8"
            });
            Debug.Log($"keys: {string.Join(", ", cache.Keys())}");

            Assert.IsTrue(cache.Keys().All(k => shouldRemain.Contains(k)));

            yield return true;
        }

        [UnityTest]
        public IEnumerator TestDontDeleteHighestPrio() {
            
            var preloadedContent = new List<string>() {
                "1",
                "2",
                "3",
                "4",
                "5"
            };

            int minPrioVal = 0;
            int maxPrioVal = 10;

            var cache = new ProceduralLRUCacheAssetMap<string>(
                preloadedContent.GroupBy(p => p).ToDictionary(p => p.Key, p => p.First()),
                rankingMinValue: minPrioVal,
                rankingMaxValue: maxPrioVal
            );

            cache.addAsset("6", "6", procedural: true);
            cache.addAsset("7", "7", procedural: true);
            cache.addAsset("8", "8", procedural: true);
            cache.addAsset("9", "9", procedural: true);
           
            Assert.AreEqual(cache.Count(), 9);

            cache.touch(new List<string> {
                "6",
                "8",
                "9"
            });

            cache.touch(new List<string> {
                "6",
                "9"
            });

            cache.touch(new List<string> {
                "6",
                "8",
                "9"
            });

            cache.touch(new List<string> {
                "6",
                "9"
            });

            cache.removeLRU(limit: 1, deleteWithHighestPriority: false);

            var shouldRemain = preloadedContent.Concat(new List<string>{
                "6",
                "9"
            });
            Debug.Log($"keys: {string.Join(", ", cache.Keys())}");

            Assert.IsTrue(cache.Keys().All(k => shouldRemain.Contains(k)));

            yield return true;
        }

         [UnityTest]
        public IEnumerator TestIntegerLimits() {
            
            var preloadedContent = new List<string>() {
                "1",
                "2",
                "3",
                "4",
                "5"
            };

            int minRankingVal = int.MaxValue-2;
            int maxRankingVal = int.MaxValue-1;

            var cache = new ProceduralLRUCacheAssetMap<string>(
                preloadedContent.GroupBy(p => p).ToDictionary(p => p.Key, p => p.First()),
                rankingMinValue: minRankingVal,
                rankingMaxValue: maxRankingVal
            );

            cache.addAsset("6", "6", procedural: true);
            cache.addAsset("7", "7", procedural: true);
            cache.addAsset("8", "8", procedural: true);
           
            Assert.AreEqual(cache.Count(), 8);

            cache.touch(new List<string> {
                "6",
            });

            Assert.AreEqual(cache.priorityMinValue, minRankingVal);
            Assert.AreEqual(cache.priorityMaxValue, maxRankingVal);

            cache.touch(new List<string> {
                "6",
            });

            Assert.AreEqual(cache.priorityMinValue, minRankingVal);
            Assert.AreEqual(cache.priorityMaxValue, maxRankingVal);

            yield return true;
        }
    }
}


