using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using System.Reflection;
using Thor.Procedural.Data;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public static class UtilityFunctions {

    //store the max number of layers unity supports in its layer system. By default this is 32
    //and will likely not change but here it is just in case
    public const int maxUnityLayerCount = 32;

    public static Bounds CreateEmptyBounds() {
        Bounds b = new Bounds();
        Vector3 inf = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        b.SetMinMax(min: inf, max: -inf);
        return b;
    }

    private static IEnumerable<int[]> Combinations(int m, int n) {
        // Enumerate all possible m-size combinations of [0, 1, ..., n-1] array
        // in lexicographic order (first [0, 1, 2, ..., m-1]).
        // Taken from https://codereview.stackexchange.com/questions/194967/get-all-combinations-of-selecting-k-elements-from-an-n-sized-array
        int[] result = new int[m];
        Stack<int> stack = new Stack<int>(m);
        stack.Push(0);
        while (stack.Count > 0) {
            int index = stack.Count - 1;
            int value = stack.Pop();
            while (value < n) {
                result[index++] = value++;
                stack.Push(value);
                if (index != m) {
                    continue;
                }

                yield return (int[])result.Clone(); // thanks to @xanatos
                // yield return result;
                break;
            }
        }
    }

    public static IEnumerable<T[]> Combinations<T>(T[] array, int m) {
        // Taken from https://codereview.stackexchange.com/questions/194967/get-all-combinations-of-selecting-k-elements-from-an-n-sized-array
        if (array.Length < m) {
            throw new ArgumentException("Array length can't be less than number of selected elements");
        }
        if (m < 1) {
            throw new ArgumentException("Number of selected elements can't be less than 1");
        }
        T[] result = new T[m];
        foreach (int[] j in Combinations(m, array.Length)) {
            for (int i = 0; i < m; i++) {
                result[i] = array[j[i]];
            }
            yield return result;
        }
    }

    public static bool isObjectColliding(
        GameObject go,
        List<GameObject> ignoreGameObjects = null,
        float expandBy = 0.0f,
        bool useBoundingBoxInChecks = false
     ) {
        return null != firstColliderObjectCollidingWith(
            go: go,
            ignoreGameObjects: ignoreGameObjects,
            expandBy: expandBy,
            useBoundingBoxInChecks: useBoundingBoxInChecks
        );
    }

    public static Collider firstColliderObjectCollidingWith(
        GameObject go,
        List<GameObject> ignoreGameObjects = null,
        float expandBy = 0.0f,
        bool useBoundingBoxInChecks = false
     ) {
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

        int layerMask = LayerMask.GetMask("Agent", "SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0");
        foreach (CapsuleCollider cc in go.GetComponentsInChildren<CapsuleCollider>()) {
            if (cc.isTrigger) {
                continue;
            }
            foreach (Collider c in PhysicsExtensions.OverlapCapsule(cc, layerMask, QueryTriggerInteraction.Ignore, expandBy)) {
                if (!ignoreColliders.Contains(c)) {
                    return c;
                }
            }
        }
        foreach (BoxCollider bc in go.GetComponentsInChildren<BoxCollider>()) {
            if (bc.isTrigger || ("BoundingBox" == bc.gameObject.name && (!useBoundingBoxInChecks))) {
                continue;
            }
            foreach (Collider c in PhysicsExtensions.OverlapBox(bc, layerMask, QueryTriggerInteraction.Ignore, expandBy)) {
                if (!ignoreColliders.Contains(c)) {
                    return c;
                }
            }
        }
        foreach (SphereCollider sc in go.GetComponentsInChildren<SphereCollider>()) {
            if (sc.isTrigger) {
                continue;
            }
            foreach (Collider c in PhysicsExtensions.OverlapSphere(sc, layerMask, QueryTriggerInteraction.Ignore, expandBy)) {
                if (!ignoreColliders.Contains(c)) {
                    return c;
                }
            }
        }
        return null;
    }

    public static Collider[] collidersObjectCollidingWith(
        GameObject go,
        List<GameObject> ignoreGameObjects = null,
        float expandBy = 0.0f,
        bool useBoundingBoxInChecks = false
        ) {
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

        HashSet<Collider> collidersSet = new HashSet<Collider>();
        int layerMask = LayerMask.GetMask("SimObjVisible", "Agent");
        foreach (CapsuleCollider cc in go.GetComponentsInChildren<CapsuleCollider>()) {
            foreach (Collider c in PhysicsExtensions.OverlapCapsule(cc, layerMask, QueryTriggerInteraction.Ignore, expandBy)) {
                if (!ignoreColliders.Contains(c)) {
                    collidersSet.Add(c);
                }
            }
        }
        foreach (BoxCollider bc in go.GetComponentsInChildren<BoxCollider>()) {
            if ("BoundingBox" == bc.gameObject.name && (!useBoundingBoxInChecks)) {
                continue;
            }
            foreach (Collider c in PhysicsExtensions.OverlapBox(bc, layerMask, QueryTriggerInteraction.Ignore, expandBy)) {
                if (!ignoreColliders.Contains(c)) {
                    collidersSet.Add(c);
                }
            }
        }
        foreach (SphereCollider sc in go.GetComponentsInChildren<SphereCollider>()) {
            foreach (Collider c in PhysicsExtensions.OverlapSphere(sc, layerMask, QueryTriggerInteraction.Ignore, expandBy)) {
                if (!ignoreColliders.Contains(c)) {
                    collidersSet.Add(c);
                }
            }
        }
        return collidersSet.ToArray();
    }

    public static RaycastHit[] CastAllPrimitiveColliders(GameObject go, Vector3 direction, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) {
        HashSet<Transform> transformsToIgnore = new HashSet<Transform>();
        foreach (Transform t in go.GetComponentsInChildren<Transform>()) {
            transformsToIgnore.Add(t);
        }
        List<RaycastHit> hits = new List<RaycastHit>();
        foreach (CapsuleCollider cc in go.GetComponentsInChildren<CapsuleCollider>()) {
            foreach (RaycastHit h in PhysicsExtensions.CapsuleCastAll(cc, direction, maxDistance, layerMask, queryTriggerInteraction)) {
                if (!transformsToIgnore.Contains(h.transform)) {
                    hits.Add(h);
                }
            }
        }
        foreach (BoxCollider bc in go.GetComponentsInChildren<BoxCollider>()) {
            foreach (RaycastHit h in PhysicsExtensions.BoxCastAll(bc, direction, maxDistance, layerMask, queryTriggerInteraction)) {
                if (!transformsToIgnore.Contains(h.transform)) {
                    hits.Add(h);
                }
            }
        }
        foreach (SphereCollider sc in go.GetComponentsInChildren<SphereCollider>()) {
            foreach (RaycastHit h in PhysicsExtensions.SphereCastAll(sc, direction, maxDistance, layerMask, queryTriggerInteraction)) {
                if (!transformsToIgnore.Contains(h.transform)) {
                    hits.Add(h);
                }
            }
        }
        return hits.ToArray();
    }

    // get a copy of a specific component and apply it to another object at runtime
    // usage: var copy = myComp.GetCopyOf(someOtherComponent);
    public static T GetCopyOf<T>(this Component comp, T other) where T : Component {
        Type type = comp.GetType();
        if (type != other.GetType()) {
            return null; // type mis-match
        }

        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
        PropertyInfo[] pinfos = type.GetProperties(flags);
        foreach (var pinfo in pinfos) {
            if (pinfo.CanWrite) {
                try {
                    pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                } catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
            }
        }
        FieldInfo[] finfos = type.GetFields(flags);
        foreach (var finfo in finfos) {
            finfo.SetValue(comp, finfo.GetValue(other));
        }
        return comp as T;
    }

    // usage: Health myHealth = gameObject.AddComponent<Health>(enemy.health); or something like that
    public static T AddComponent<T>(this GameObject go, T toAdd) where T : Component {
        return go.AddComponent<T>().GetCopyOf(toAdd) as T;
    }


    // Taken from https://answers.unity.com/questions/589983/using-mathfround-for-a-vector3.html
    public static Vector3 Round(this Vector3 vector3, int decimalPlaces = 2) {
        float multiplier = 1;
        for (int i = 0; i < decimalPlaces; i++) {
            multiplier *= 10f;
        }
        return new Vector3(
            Mathf.Round(vector3.x * multiplier) / multiplier,
            Mathf.Round(vector3.y * multiplier) / multiplier,
            Mathf.Round(vector3.z * multiplier) / multiplier);
    }

    public static Vector3[] CornerCoordinatesOfBoxColliderToWorld(BoxCollider b) {
        Vector3[] corners = new Vector3[8];

        corners[0] = b.transform.TransformPoint(b.center + new Vector3(b.size.x, -b.size.y, b.size.z) * 0.5f);
        corners[1] = b.transform.TransformPoint(b.center + new Vector3(-b.size.x, -b.size.y, b.size.z) * 0.5f);
        corners[2] = b.transform.TransformPoint(b.center + new Vector3(-b.size.x, -b.size.y, -b.size.z) * 0.5f);
        corners[3] = b.transform.TransformPoint(b.center + new Vector3(b.size.x, -b.size.y, -b.size.z) * 0.5f);

        corners[4] = b.transform.TransformPoint(b.center + new Vector3(b.size.x, b.size.y, b.size.z) * 0.5f);
        corners[5] = b.transform.TransformPoint(b.center + new Vector3(-b.size.x, b.size.y, b.size.z) * 0.5f);
        corners[6] = b.transform.TransformPoint(b.center + new Vector3(-b.size.x, b.size.y, -b.size.z) * 0.5f);
        corners[7] = b.transform.TransformPoint(b.center + new Vector3(b.size.x, b.size.y, -b.size.z) * 0.5f);

        return corners;
    }

    public static List<LightParameters> GetLightPropertiesOfScene() {

            Debug.Log("we are inside GetLIghtPropertiesOfScene");
            var lightsInScene = UnityEngine.Object.FindObjectsOfType<Light>(true);

            List<LightParameters> allOfTheLights = new List<LightParameters>();

            //generate the LightParameters for all lights in the scene
            foreach (Light hikari in lightsInScene) {

                LightParameters lp = new LightParameters();

                lp.id = hikari.transform.name;

                lp.type = LightType.GetName(typeof(LightType), hikari.type);

                lp.position = hikari.transform.position;

                lp.localPosition = hikari.transform.localPosition;

                //culling mask stuff
                List<string> cullingMaskOff = new List<String>();

                for (int i = 0; i < UtilityFunctions.maxUnityLayerCount; ++i) {
                    //check what layers are off for this light's mask
                    if (((1 << i) & hikari.cullingMask) == 0) {
                        //check if this layer is actually being used (ie: has a name)
                        if (LayerMask.LayerToName(i).Length != 0) {
                            cullingMaskOff.Add(LayerMask.LayerToName(i));
                        }
                    }
                }

                lp.cullingMaskOff = cullingMaskOff.ToArray();

                lp.rotation = FlexibleRotation.fromQuaternion(hikari.transform.rotation);

                lp.intensity = hikari.intensity;

                lp.indirectMultiplier = hikari.bounceIntensity;

                lp.range = hikari.range;

                //only do this if this is a spot light
                if(hikari.type == LightType.Spot) {
                    lp.spotAngle = hikari.spotAngle;
                }

                lp.rgb = new SerializableColor() { r = hikari.color.r, g = hikari.color.g, b = hikari.color.b, a = hikari.color.a };
                
                //generate shadow params
                ShadowParameters xemnas = new ShadowParameters() {
                        strength = hikari.shadowStrength,
                        type = Enum.GetName(typeof(LightShadows), hikari.shadows),
                        normalBias = hikari.shadowNormalBias,
                        bias = hikari.shadowBias,
                        nearPlane = hikari.shadowNearPlane,
                        resolution = Enum.GetName(typeof(UnityEngine.Rendering.LightShadowResolution), hikari.shadowResolution)
                };

                lp.shadow = xemnas;

                //linked sim object
                //lp.linkedSimObj = ;

                lp.enabled = hikari.enabled;

                if(hikari.GetComponentInParent<SimObjPhysics>()) {
                    lp.parentSimObjId = hikari.GetComponentInParent<SimObjPhysics>().objectID;
                    lp.parentSimObjName = hikari.GetComponentInParent<SimObjPhysics>().transform.name;
                }

                allOfTheLights.Add(lp);
            }

            //find all sim obj physics in scene

            return allOfTheLights;
    }

#if UNITY_EDITOR

    public static void debugGetLightPropertiesOfScene(List<LightParameters> lights) {
        Debug.Log("we are inside debugGetLightProperties...");

        var file = "debugLightProperties.txt";
        var create = File.CreateText("Assets/DebugTextFiles/" + file);

        create.WriteLine($"Total number of Lights in scene: {lights.Count()}");

        foreach (LightParameters lp in lights) {
            create.WriteLine($"ID: {lp.id}");

            create.WriteLine($"Type: {lp.type}");

            create.WriteLine($"position: {lp.position}");

            create.WriteLine($"localPosition: {lp.localPosition}");

            if (lp.cullingMaskOff.Length > 0) {
                create.WriteLine($"Culling Mask Off Layers:");

                foreach (string s in lp.cullingMaskOff) {
                    create.WriteLine("     " + s);
                }
            } else {
                create.WriteLine($"Culling Mask Off Layers: none");
            }

            create.WriteLine($"rotation degrees: {lp.rotation.degrees}");

            create.WriteLine($"rotation axis: {lp.rotation.axis}");

            create.WriteLine($"intensity: {lp.intensity}");

            create.WriteLine($"indirect Multiplier: {lp.indirectMultiplier}");

            create.WriteLine($"range: {lp.range}");

            //this should be 0 if not a spotlight
            if (Mathf.Approximately(lp.spotAngle, 0.0f)) {
                create.WriteLine("spotAngle: not a spot light!");
            } else {
                create.WriteLine($"spotAngle: {lp.spotAngle}");
            }

            create.WriteLine($"rgba: {lp.rgb.r} {lp.rgb.g} {lp.rgb.b} {lp.rgb.a}");

            if (lp.shadow != null) {
                create.WriteLine($"shadow params:");
                create.WriteLine($"     shadow type: {lp.shadow.type}");
                create.WriteLine($"     shadow strength: {lp.shadow.strength}");
                create.WriteLine($"     shadow normalBias: {lp.shadow.normalBias}");
                create.WriteLine($"     shadow bias: {lp.shadow.bias}");
                create.WriteLine($"     shadow nearPlane: {lp.shadow.nearPlane}");
                create.WriteLine($"     shadow resolution: {lp.shadow.resolution}");
            } else {
                create.WriteLine($"shadow params NULL!!!");
            }

            create.WriteLine($"linkedSimObj: {lp.linkedSimObj}");

            create.WriteLine($"enabled: {lp.enabled}");

            create.WriteLine($"parent Sim Obj Id: {lp.parentSimObjId}");

            create.WriteLine($"parent Sim Obj Name: {lp.parentSimObjName}");

            create.WriteLine("");
        }

        create.Close();
    }

    [MenuItem("SimObjectPhysics/Toggle Off PlaceableSurface Material")]
    private static void ToggleOffPlaceableSurface() {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++) {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(i), OpenSceneMode.Single);
            var meshes = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();

            foreach (MeshRenderer m in meshes) {
                if (m.sharedMaterial.ToString() == "Placeable_Surface_Mat (UnityEngine.Material)") {
                    m.enabled = false;
                }
            }
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }

    [MenuItem("SimObjectPhysics/Toggle On PlaceableSurface Material")]
    private static void ToggleOnPlaceableSurface() {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++) {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(i), OpenSceneMode.Single);
            var meshes = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();

            foreach (MeshRenderer m in meshes) {
                if (m.sharedMaterial.ToString() == "Placeable_Surface_Mat (UnityEngine.Material)") {
                    m.enabled = true;
                }
            }
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        }
    }

    [UnityEditor.MenuItem("AI2-THOR/Add GUID to Object Names")]
    public static void AddGUIDToSimObjPhys() {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++) {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(i), OpenSceneMode.Single);
            SimObjPhysics[] objects = GameObject.FindObjectsOfType<SimObjPhysics>(true);

            //explicitly track used Guids since we are only using the first 8 characters in the generated ID and want to be sure they are universally unique
            List<string> usedGuidsJustInCaseCauseIDunnoThisIsRandom = new List<string>();

            foreach (SimObjPhysics sop in objects) {
                Guid g;
                bool isThisNew = true;
                string uid = "";

                while(isThisNew) {
                    g = Guid.NewGuid();
                    string first8 = g.ToString("N").Substring(0, 8);

                    //this guid is new and has not been used before
                    if(!usedGuidsJustInCaseCauseIDunnoThisIsRandom.Contains(first8)) {
                        usedGuidsJustInCaseCauseIDunnoThisIsRandom.Add(first8);
                        isThisNew = false;
                        uid = first8;
                    }

                    else { 
                        Debug.Log($"wow what are the odds that {first8} was generated again!?!?");
                    }
                }

                sop.name = sop.GetComponent<SimObjPhysics>().Type.ToString() + "_" + uid;
            }

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }

    [MenuItem("AI2-THOR/Name All Scene Light Objects")]
    //light naming convention: {PrefabName/scene}|{Light Type}|{instance}
    //Editor-only function used to set names of all light assets in scenes that have Lights in them prior to any additional lights being
    //dynamically spawned in by something like a Procedural action.
    private static void NameAllSceneLightObjects() {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++) {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(i), OpenSceneMode.Single);
            var lights = UnityEngine.Object.FindObjectsOfType<Light>(true);

            //separate lights into scene-level lights, and lights that are children of sim objects cause they have to be handled separately
            //"scene" level lights effectively treat the entire scene as "their sim object" for the purposes of instance counting
            Dictionary<Light, LightType> sceneLights = new Dictionary<Light, LightType>();
            Dictionary<Light, LightType> simObjChildLights = new Dictionary<Light, LightType>();

            foreach (Light l in lights) {
                 if(!l.GetComponentInParent<SimObjPhysics>()) {
                    //one caveat here is that light switch objects do control specific "scene" lights
                    //because they are not children of the light switches, these referenced lights must be set up in editor first
                    //or at initialization in a Procedural scene. Check the <LightParameters> member <linkedObjectId> to see if some scene light
                    //is actually controlled by a light switch sim object.
                    sceneLights.Add(l, l.type);
                 }

                 else {
                    simObjChildLights.Add(l, l.type);
                 }
            }

            int directionalInstance = 0;
            int spotInstance = 0;
            int pointInstance = 0;
            int areaInstance = 0;

            //sort the scene lights into point, directional, or spot
            foreach (KeyValuePair<Light, LightType> l in sceneLights) {
                //Debug.Log(directionalInstance);
                if(l.Value == LightType.Spot) {
                    l.Key.name = "scene|" + l.Value.ToString()+ "|" + spotInstance.ToString();
                    spotInstance++;
                }

                else if(l.Value == LightType.Directional) {
                    l.Key.name = "scene|" + l.Value.ToString()+ "|" + directionalInstance.ToString();
                    directionalInstance++;
                }

                else if(l.Value == LightType.Point) {
                    l.Key.name = "scene|" + l.Value.ToString()+ "|" + pointInstance.ToString();
                    pointInstance++;
                }
            
                else if(l.Value == LightType.Area) {
                    l.Key.name = "scene|" + l.Value.ToString()+ "|" + areaInstance.ToString();
                    areaInstance++;
                }
            }

            //make new dictionary to pair specific Sim Object instances with potentially multiple child lights, so multiple child light keys might have same SimObj parent value
            Dictionary<KeyValuePair<Light, LightType>, SimObjPhysics> lightAndTypeToSimObjPhys = new Dictionary<KeyValuePair<Light, LightType>, SimObjPhysics>();
            
            //map each light/lightType pair to the sim object that they are associated with
            foreach (KeyValuePair<Light, LightType> l in simObjChildLights) {
                lightAndTypeToSimObjPhys.Add(l, l.Key.GetComponentInParent<SimObjPhysics>());
            }

            //track if multiple key lights in simObjChildLIghts are children of the same SimObjPhysics
            Dictionary<SimObjPhysics, int> simObjToSpotInstanceCountInThatSimObj = new Dictionary<SimObjPhysics, int>();
            Dictionary<SimObjPhysics, int> simObjToDirectionalInstanceCountInThatSimObj = new Dictionary<SimObjPhysics, int>();
            Dictionary<SimObjPhysics, int> simObjToPointInstanceCountInThatSimObj = new Dictionary<SimObjPhysics, int>();
            Dictionary<SimObjPhysics, int> simObjToAreaInstanceCountInThatSimObj = new Dictionary<SimObjPhysics, int>();

            foreach(KeyValuePair< KeyValuePair<Light, LightType>, SimObjPhysics> light in lightAndTypeToSimObjPhys) {
                
                if(light.Key.Value == LightType.Spot) {
                    if(!simObjToSpotInstanceCountInThatSimObj.ContainsKey(light.Value)){
                        //this is the first instance of a Spot light found in this sim object
                        simObjToSpotInstanceCountInThatSimObj.Add(light.Value, 0);
                    }

                    else {
                        //we have found another instance of this type of light in this previously found sim object before
                        simObjToSpotInstanceCountInThatSimObj[light.Value]++;
                    }

                    light.Key.Key.name = light.Value.transform.name + "|" + light.Key.Value.ToString() + "|" + simObjToSpotInstanceCountInThatSimObj[light.Value].ToString();
                }

                else if(light.Key.Value == LightType.Directional) {
                    if(!simObjToDirectionalInstanceCountInThatSimObj.ContainsKey(light.Value)){
                        //this is the first instance of a Directional light found in this sim object (PROBS DONT PUT A DIRECTIONAL LIGHT IN A SIM OBJ PREFAB BUT YOU DO YOU I GUESS)
                        simObjToDirectionalInstanceCountInThatSimObj.Add(light.Value, 0);
                    }

                    else {
                        //we have found another instance of this type of light in this previously found sim object before
                        simObjToDirectionalInstanceCountInThatSimObj[light.Value]++;
                    }

                    light.Key.Key.name = light.Value.transform.name + "|" + light.Key.Value.ToString() + "|" + simObjToDirectionalInstanceCountInThatSimObj[light.Value].ToString();
                }

                else if(light.Key.Value == LightType.Point) {    
                    if(!simObjToPointInstanceCountInThatSimObj.ContainsKey(light.Value)){
                        //this is the first instance of a Point light found in this sim object
                        simObjToPointInstanceCountInThatSimObj.Add(light.Value, 0);
                    }

                    else {
                        //we have found another instance of a Point light in this previously found sim object before
                        simObjToPointInstanceCountInThatSimObj[light.Value]++;
                    }

                    light.Key.Key.name = light.Value.transform.name + "|" + light.Key.Value.ToString() + "|" + simObjToPointInstanceCountInThatSimObj[light.Value].ToString();
                }

                //we currently don't really use area lights since they are baked only but this is here just in case
                else if(light.Key.Value == LightType.Area) {

                    if(!simObjToAreaInstanceCountInThatSimObj.ContainsKey(light.Value)){
                        simObjToAreaInstanceCountInThatSimObj.Add(light.Value, 0);
                    }

                    else {
                        simObjToAreaInstanceCountInThatSimObj[light.Value]++;
                    }

                    light.Key.Key.name = light.Value.transform.name + "|" + light.Key.Value.ToString() + "|" + simObjToAreaInstanceCountInThatSimObj[light.Value].ToString();
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }
#endif
}