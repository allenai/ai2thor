// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

//this is used to tag Structural objects in the scene. Structural objects are objects with physical collision and are rendered, but are not SimObjects themselves.
//these objects are all located under the "Structure" object in the Heirarchy, and are always static and purely environmental.
public class StructureObject : MonoBehaviour
{
    [SerializeField]
    public StructureObjectTag WhatIsMyStructureObjectTag;

    public static float PLATFORM_LIP_WIDTH = 0.1f;
    public static float PLATFORM_LIP_HEIGHT = 0.3f;

    // Start is called before the first frame update
    void Start()
    {
        MCSController agent = FindObjectOfType<MCSController>();
        if(agent!=null)
            Physics.IgnoreCollision(agent.groundObjectsCollider, gameObject.GetComponentInChildren<Collider>(), true);
    }

    public void AddPlatformLips(float scaleX = 1, float scaleY = 1, float scaleZ = 1, MCSConfigPlatformLips lips = null) {
        bool addFront = false;
        bool addBack = false;
        bool addLeft = false;
        bool addRight = false;
        List<LipGapSpan> frontGaps = new List<LipGapSpan>();
        List<LipGapSpan> backGaps = new List<LipGapSpan>();
        List<LipGapSpan> leftGaps = new List<LipGapSpan>();
        List<LipGapSpan> rightGaps = new List<LipGapSpan>();

        if (lips != null) {
            addFront = lips.front;
            addBack = lips.back;
            addLeft = lips.left;
            addRight = lips.right;
            if (lips.gaps != null) {
                if (lips.gaps.front != null)
                    frontGaps = lips.gaps.front;
                if (lips.gaps.back != null)
                    backGaps = lips.gaps.back;
                if (lips.gaps.left != null)
                    leftGaps = lips.gaps.left;
                if (lips.gaps.right != null)
                    rightGaps = lips.gaps.right;
            }
        }

        float placementOffsetXWithScale = 0.5f - (PLATFORM_LIP_WIDTH / scaleX / 2);
        float placementOffsetYWithScale = 0.5f + (PLATFORM_LIP_HEIGHT / scaleY / 2);
        float placementOffsetZWithScale = 0.5f - (PLATFORM_LIP_WIDTH / scaleZ / 2);

        GameObject thisPlatform = this.gameObject;
        List<GameObject> fronts = new List<GameObject>();
        List<GameObject> backs = new List<GameObject>();
        List<GameObject> lefts = new List<GameObject>();
        List<GameObject> rights = new List<GameObject>();
        //instantiate identical lips
        if (addFront) {
            for (int i = 0; i < frontGaps.Count + 1; i++) {
                //using substring like this gets rid of (Clone) from the end of the instantiated object name
                string my_name = thisPlatform.name.Substring(0, name.Length) + "-front-" + i;
                GameObject front = init_lip(my_name);
                fronts.Add(front);
            }
        }

        if (addBack) {
            for (int i = 0; i < backGaps.Count + 1; i++) {
                string my_name = thisPlatform.name.Substring(0, name.Length) + "-back-" + i;
                GameObject back = init_lip(my_name);
                backs.Add(back);
            }
        }
        if (addLeft) {
            for (int i = 0; i < leftGaps.Count + 1; i++) {
                string my_name = thisPlatform.name.Substring(0, name.Length) + "-left-" + i;
                GameObject left = init_lip(my_name);
                lefts.Add(left);
            }
        }
        if (addRight) {
            for (int i = 0; i < rightGaps.Count + 1; i++) {
                string my_name = thisPlatform.name.Substring(0, name.Length) + "-right-" + i;
                GameObject right = init_lip(my_name);
                rights.Add(right);
            }
        }


        if (addFront) {
            positionLips(frontGaps, fronts, true, placementOffsetYWithScale, -placementOffsetZWithScale, PLATFORM_LIP_HEIGHT / scaleY, PLATFORM_LIP_WIDTH / scaleZ);
        }

        if (addBack) {
            positionLips(backGaps, backs, true, placementOffsetYWithScale, placementOffsetZWithScale, PLATFORM_LIP_HEIGHT / scaleY, PLATFORM_LIP_WIDTH / scaleZ);
        }

        if (addLeft) {
            positionLips(leftGaps, lefts, false, placementOffsetYWithScale, -placementOffsetXWithScale, PLATFORM_LIP_HEIGHT / scaleY, PLATFORM_LIP_WIDTH / scaleX);
        }

        if (addRight) {
            positionLips(rightGaps, rights, false, placementOffsetYWithScale, placementOffsetXWithScale, PLATFORM_LIP_HEIGHT / scaleY, PLATFORM_LIP_WIDTH / scaleX);
        }
    }

    private void positionLips(List<LipGapSpan> gaps, List<GameObject> gameObjects, bool isFrontBack, float placementOffsetYWithScale, float placementOffsetXZWithScale, float scaleY, float scaleXZ) {
        for (int i = 0; i < gaps.Count + 1; i++) {
            float start = (i == 0 ? 0 : gaps[i - 1].high);
            float end = (i != gaps.Count ? gaps[i].low : 1);
            GameObject my_lip = gameObjects[i];
            // Do we want to remove tiny slivers in the middle or only on the ends?
            bool tiny_end = end - start < .03;//&& (end == 1 || start == 0);
            if (end == start || tiny_end) {
                GameObject.Destroy(my_lip);
                continue;
            }
            start -= 0.5f;
            end -= 0.5f;
            float scale = end - start;
            float pos = (start + end) / 2.0f;
            my_lip.transform.parent = this.transform;
            if (isFrontBack) {
                my_lip.transform.localPosition = new Vector3(pos, placementOffsetYWithScale, placementOffsetXZWithScale);
                my_lip.transform.localScale = new Vector3(scale, scaleY, scaleXZ);
            } else {
                my_lip.transform.localPosition = new Vector3(placementOffsetXZWithScale, placementOffsetYWithScale, pos);
                my_lip.transform.localScale = new Vector3(scaleXZ, scaleY, scale);
            }
        }
    }

    private GameObject init_lip(string my_name) {
        GameObject lip = Instantiate(this.gameObject, transform.position, Quaternion.identity);
        lip.name = my_name;
        lip.GetComponent<SimObjPhysics>().objectID = lip.name;
        return lip;
    }
}

[Serializable]
public enum StructureObjectTag : int
{
    Undefined = 0,
    Wall = 1,
    Floor = 2,
    Ceiling = 3,
    LightFixture = 4,//for all hanging lights or other lights protruding out of something
    CeilingLight = 5,//for embedded lights in the ceiling
    Stove = 6,//for the uninteractable body of the stove
    DishWasher = 7,
    KitchenIsland = 8,
    Door = 9,
    WallCabinetBody = 10,
    OvenHood = 11,
    PaperClutter = 12,
    SkyLightWindow = 13,
    Clock = 14,
    Rug = 15,
    FirePlace = 16,
    DecorativeSticks = 17,
    
    

}

