using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
public static class UtilityFunctions {
    public static bool isObjectColliding(GameObject go, List<GameObject> ignoreGameObjects = null, float expandBy = 0.0f) {
        if (ignoreGameObjects == null) {
            ignoreGameObjects = new List<GameObject>();
        }
        ignoreGameObjects.Add(go);
        HashSet<Collider> ignoreColliders = new HashSet<Collider>();
        foreach (GameObject toIgnoreGo in ignoreGameObjects) {
            foreach (Collider c in toIgnoreGo.GetComponentsInChildren<Collider>()) {
                ignoreColliders.Add(c);
            }
        }

        int layerMask = 1 << 8;
        foreach (CapsuleCollider cc in go.GetComponentsInChildren<CapsuleCollider>()) {
            foreach (Collider c in PhysicsExtensions.OverlapCapsule(cc, layerMask, QueryTriggerInteraction.Ignore, expandBy)) {
                if (!ignoreColliders.Contains(c)) {
                    return true;
                }
            }
        }
        foreach (BoxCollider bc in go.GetComponentsInChildren<BoxCollider>()) {
            foreach (Collider c in PhysicsExtensions.OverlapBox(bc, layerMask, QueryTriggerInteraction.Ignore, expandBy)) {
                if (!ignoreColliders.Contains(c)) {
                    return true;
                }
            }
        }
        foreach (SphereCollider sc in go.GetComponentsInChildren<SphereCollider>()) {
            foreach (Collider c in PhysicsExtensions.OverlapSphere(sc, layerMask, QueryTriggerInteraction.Ignore, expandBy)) {
                if (!ignoreColliders.Contains(c)) {
                    return true;
                }
            }
        }
        return false;
    }

}