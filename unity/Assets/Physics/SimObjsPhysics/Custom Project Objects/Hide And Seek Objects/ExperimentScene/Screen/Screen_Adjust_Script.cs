using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//NOTE: This fails to compile because it is calling some Setup scripts that are currently EDITOR define only
//for now, i'm also wrapping this script so it can only be used in editor until we have more time to investigate
#if UNITY_EDITOR

[ExecuteInEditMode]
public class Screen_Adjust_Script : MonoBehaviour
{
    [Range(0f, 2f)]
    public float spacing = 1;
    [Range(-1f, 1f)]
    public float topShift = 0;
    [Range(-1f, 1f)]
    public float bottomShift = 0;
    [Range(-1f, 1f)]
    public float widthShift = 0;

    float spacingPrev, topShiftPrev, bottomShiftPrev, widthShiftPrev;

    int stableVisPoints = 4;

    void Start()
    {
        if (PrefabUtility.GetCorrespondingObjectFromSource(gameObject) != null)
        {
            PrefabUtility.UnpackPrefabInstance(transform.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        spacingPrev = spacing + 0.000001f;
        topShiftPrev = topShift;
        bottomShiftPrev = bottomShift;
        widthShiftPrev = widthShift;
    }

    // Update is called once per frame
    void Update()
    {   
        if (spacing != spacingPrev || topShift != topShiftPrev || bottomShift != bottomShiftPrev || widthShift != widthShiftPrev)
        {
            //Zero out rotation for entirety of operation, for simplicity
            Quaternion rotationSaver = transform.rotation;
            transform.rotation = Quaternion.identity;

            Transform screenObject = transform.Find("screen_reference");
            Transform sheetObject = transform.Find("screen_reference").transform.Find("screen_sheet");
            
            Transform leftBaseJoint = screenObject.Find("screen_master_jnt").Find("screen_pillar_l_base_jnt");
            Transform rightBaseJoint = screenObject.Find("screen_master_jnt").Find("screen_pillar_r_base_jnt");
            Transform leftBottomJoint = screenObject.Find("screen_master_jnt").Find("screen_pillar_l_base_jnt").Find("screen_pillar_l_bottom_jnt");
            Transform rightBottomJoint = screenObject.Find("screen_master_jnt").Find("screen_pillar_r_base_jnt").Find("screen_pillar_r_bottom_jnt");
            Transform leftTopJoint = screenObject.Find("screen_master_jnt").Find("screen_pillar_l_base_jnt").Find("screen_pillar_l_top_jnt");
            Transform rightTopJoint = screenObject.Find("screen_master_jnt").Find("screen_pillar_r_base_jnt").Find("screen_pillar_r_top_jnt");

            float pillarToSheetVisPointBuffer = 0.05f;

            if (topShift != topShiftPrev || bottomShift != bottomShiftPrev)
            {
                if (topShift != topShiftPrev)
                {
                    //Adjust joints
                    leftTopJoint.position += leftTopJoint.forward * (topShift - topShiftPrev);
                    rightTopJoint.position += rightTopJoint.forward * (topShift - topShiftPrev);

                    //Adjust colliders
                    float colHeight = Vector3.Distance(leftBaseJoint.position, leftTopJoint.position);
                    BoxCollider colL = screenObject.Find("Colliders").Find("Col_l2").GetComponent<BoxCollider>();
                    BoxCollider colR = screenObject.Find("Colliders").Find("Col_r2").GetComponent<BoxCollider>();

                    colL.center = ((leftTopJoint.position + leftBaseJoint.position) / 2) - colL.transform.position;
                    colL.center -= new Vector3(0, 0, colL.center.z);
                    colL.size = new Vector3(0.05f, colHeight, 0.05f);

                    colR.center = ((rightTopJoint.position + rightBaseJoint.position) / 2) - colR.transform.position;
                    colR.center -= new Vector3(0, 0, colR.center.z);
                    colR.size = new Vector3(0.05f, colHeight, 0.05f);
                }

                if (bottomShift != bottomShiftPrev)
                {
                    //Adjust joints
                    leftBottomJoint.position += leftBottomJoint.forward * (bottomShift - bottomShiftPrev);
                    rightBottomJoint.position += rightBottomJoint.forward * (bottomShift - bottomShiftPrev);
                }

                //Adjust sheet collider
                BoxCollider colSheet = sheetObject.Find("Colliders").Find("Col_s").transform.GetComponent<BoxCollider>();

                Bounds newBounds = newSheetBounds(colSheet, leftTopJoint, rightBottomJoint);

                colSheet.center = newBounds.center - colSheet.gameObject.transform.position;
                colSheet.center -= new Vector3(0, 0, colSheet.center.z);
                colSheet.size = new Vector3(newBounds.size.x, newBounds.size.y, 0.03f);
            }

            if (widthShift != widthShiftPrev)
            {
                //Adjust joints
                leftBaseJoint.position += leftBaseJoint.right * (widthShift - widthShiftPrev);
                rightBaseJoint.position += -rightBaseJoint.right * (widthShift - widthShiftPrev);

                //Adjust left pillar collider
                Transform[] colLeft = new Transform[2];
                colLeft[0] = screenObject.Find("Colliders").Find("Col_l1").transform;
                colLeft[1] = screenObject.Find("Colliders").Find("Col_l2").transform;

                foreach (Transform transform in colLeft)
                {
                    transform.position += transform.right * (widthShift - widthShiftPrev);
                }

                //Adjust right pillar collider
                Transform[] colRight = new Transform[2];
                colRight[0] = screenObject.Find("Colliders").Find("Col_r1").transform;
                colRight[1] = screenObject.Find("Colliders").Find("Col_r2").transform;

                foreach (Transform transform in colRight)
                {
                    transform.position -= transform.right * (widthShift - widthShiftPrev);
                }

                //Adjust sheet collider
                Transform colCenter = sheetObject.Find("Colliders").Find("Col_s").transform;
                colCenter.GetComponent<BoxCollider>().size += transform.right * 2 * (widthShift - widthShiftPrev);

                //Adjust left pillar base-vispoints
                Transform[] visPointsLeft = new Transform[2];
                visPointsLeft[0] = screenObject.Find("VisibilityPoints").Find("vPoint_stable_1").transform;
                visPointsLeft[1] = screenObject.Find("VisibilityPoints").Find("vPoint_stable_2").transform;

                foreach (Transform transform in visPointsLeft)
                {
                    transform.position += transform.right * (widthShift - widthShiftPrev);
                }

                //Adjust right pillar base-vispoints
                Transform[] visPointsRight = new Transform[2];
                visPointsRight[0] = screenObject.Find("VisibilityPoints").Find("vPoint_stable_3").transform;
                visPointsRight[1] = screenObject.Find("VisibilityPoints").Find("vPoint_stable_4").transform;

                foreach (Transform transform in visPointsRight)
                {
                    transform.position -= transform.right * (widthShift - widthShiftPrev);
                }
            }

            //Define vispoint adjustment inputs
            float pillarHeight = Vector3.Distance(rightBaseJoint.position, rightTopJoint.position);
            float sheetWidth = Vector3.Distance(rightBottomJoint.position, leftBottomJoint.position) - pillarToSheetVisPointBuffer * 2;
            float sheetHeight = Vector3.Distance(rightBottomJoint.position, rightTopJoint.position);

            int sheetWidthVisCount = (int)Mathf.Floor(2 + (sheetWidth * 3));
            int sheetHeightVisCount = (int)Mathf.Floor(2 + (sheetHeight * 3));

            //Use first existing vispoint as reference
            GameObject visPointObject = screenObject.Find("VisibilityPoints").GetChild(0).gameObject;

            //Delete previous dynamic pillar vispoints
            int prevVisPoints = screenObject.Find("VisibilityPoints").childCount;
            for (int i = stableVisPoints; i < prevVisPoints; i++)
            {
                DestroyImmediate(screenObject.Find("VisibilityPoints").GetChild(stableVisPoints).gameObject);
            }

            //Define pillar vispoints array
            Vector3[] pillarVisPoints = new Vector3[2 * (sheetHeightVisCount - 1)];
            for (int i = 0; i < sheetHeightVisCount - 1; i++)
            {
                pillarVisPoints[i] = rightBaseJoint.TransformPoint(rightBottomJoint.TransformDirection(0, 0, (i + 1) * pillarHeight / (sheetHeightVisCount - 1)));
                pillarVisPoints[i + sheetHeightVisCount - 1] = leftBaseJoint.TransformPoint(leftBottomJoint.TransformDirection(0, 0, (i + 1) * pillarHeight / (sheetHeightVisCount - 1)));
            }

            //Generate pillar vispoints
            for (int i = 0; i < pillarVisPoints.Length; i++)
            {
                GameObject newVisPoint = Instantiate(visPointObject, screenObject.Find("VisibilityPoints"));
                newVisPoint.name = ("vPoint_dynamic_" + (i + 1));
                newVisPoint.transform.position = pillarVisPoints[i];
            }

            //Define sheet vispoints array
            Vector3[] sheetVisPoints = new Vector3[sheetWidthVisCount * sheetHeightVisCount];
            for (int i = 0; i < sheetHeightVisCount; i++)
            {
                for (int j = 0; j < sheetWidthVisCount; j++)
                {
                    sheetVisPoints[i * sheetWidthVisCount + j] = rightBottomJoint.TransformPoint(new Vector3(pillarToSheetVisPointBuffer + (sheetWidth / (sheetWidthVisCount - 1)) * j, 0, 0.05f * sheetHeight + (0.9f * sheetHeight / (sheetHeightVisCount - 1)) * i));
                    //Debug.Log("Vispoint " + (i * sheetWidthVisCount + j + 1) + " has coordinates " + sheetVisPoints[i * sheetWidthVisCount + j]);
                }
            }
           
            //Delete any excess vispoints for sheet
            int prevSheetVisPoints = sheetObject.Find("VisibilityPoints").childCount;
            if (sheetVisPoints.Length < prevSheetVisPoints)
            {
                for (int i = 0; i < prevSheetVisPoints - sheetVisPoints.Length; i++)
                {
                    DestroyImmediate(sheetObject.Find("VisibilityPoints").GetChild(sheetVisPoints.Length).gameObject);
                }
            }

            //Set up new vispoint array for sheet
            for (int i = 0; i < sheetVisPoints.Length; i++)
            {
                //Repurpose existing vispoints
                if (i < sheetObject.Find("VisibilityPoints").childCount)
                {

                    sheetObject.Find("VisibilityPoints").transform.GetChild(i).name = "vPoint_dynamic_" + (i + 1);
                    sheetObject.Find("VisibilityPoints").transform.GetChild(i).transform.position = sheetVisPoints[i];
                }

                //Generate more vispoints if needed
                else
                {
                    GameObject newVisPoint = Instantiate(visPointObject, sheetObject.Find("VisibilityPoints"));
                    newVisPoint.name = ("vPoint_dynamic_" + (i + 1));
                    newVisPoint.transform.position = sheetVisPoints[i];
                }
            }

            //Send snapshot of skinned screen to rendered versions
            Mesh screenSnapshot = new Mesh();
            screenObject.Find("mesh").GetComponent<SkinnedMeshRenderer>().BakeMesh(screenSnapshot);
            for (int i = 0; i < transform.Find("mesh").childCount; i++)
            {
                transform.Find("mesh").GetChild(i).localPosition = new Vector3(0, 0, spacing * (i / (transform.Find("mesh").childCount - 1) - 0.5f));
                transform.Find("mesh").GetChild(i).GetComponent<MeshFilter>().mesh = screenSnapshot;
            }

            //Create list of childGameObjects
            List<Transform> childGameObjects = new List<Transform>();

            //Send snapshot of skinned sheet to rendered versions
            Mesh sheetSnapshot = new Mesh();
            sheetObject.Find("mesh").GetComponent<SkinnedMeshRenderer>().BakeMesh(sheetSnapshot);
            for (int i = 0; i < transform.childCount; i++)
            {
                //If it's a sub-gameobject...
                if (transform.GetChild(i).GetComponent<SimObjPhysics>() != null)
                {
                    transform.GetChild(i).Find("mesh").GetComponent<MeshFilter>().mesh = sheetSnapshot;

                    //Add to the list in the meantime
                    childGameObjects.Add(transform.GetChild(i));
                }
            }

            //Set up duplicate colliders, vispoints, and sheet gameobjects

            deleteCollidersAndVisPoints(transform);

            Transform currentMetadataGroup;
            Transform currentSubObjectMetadataGroup;
            int currentchildGameObject = 0;
            foreach (Transform subObjectMesh in transform.Find("mesh"))
            {

                Instantiate(screenObject.Find("Colliders"), subObjectMesh);
                Instantiate(screenObject.Find("VisibilityPoints"), subObjectMesh);
                Instantiate(screenObject.Find("screen_sheet"), subObjectMesh);

                while (subObjectMesh.childCount != 0)
                {
                    currentMetadataGroup = subObjectMesh.GetChild(0);

                    if (currentMetadataGroup.name == "Colliders(Clone)")
                    {
                        while (currentMetadataGroup.childCount != 0)
                        {
                            currentMetadataGroup.GetChild(0).SetParent(transform.Find("Colliders"));
                        }

                        DestroyImmediate(currentMetadataGroup.gameObject);
                    }

                    else if (currentMetadataGroup.name == "VisibilityPoints(Clone)")
                    {
                        while (currentMetadataGroup.childCount != 0)
                        {
                            currentMetadataGroup.GetChild(0).SetParent(transform.Find("VisibilityPoints"));
                        }

                        DestroyImmediate(currentMetadataGroup.gameObject);
                    }

                    //For sub-SimObjects (move VisPoints and Colliders part of metadata from the first to the second...)
                    else
                    {
                        deleteCollidersAndVisPoints(childGameObjects[currentchildGameObject]);


                        while (currentMetadataGroup.childCount != 0)
                        {

                            currentSubObjectMetadataGroup = currentMetadataGroup.GetChild(0);
                            //If it's a collider group...

                            //Debug.Log(currentMetadataGroup.GetChild(0).name);
                            if (currentSubObjectMetadataGroup.name == "Colliders")
                            {

                                Debug.Log("Adding colliders from " + currentMetadataGroup + " to " + childGameObjects[currentchildGameObject].Find("Colliders") + " which is a child of " + childGameObjects[currentchildGameObject]);
                                while (currentSubObjectMetadataGroup.childCount != 0)
                                {
                                    Debug.Log("Moving " + currentSubObjectMetadataGroup.GetChild(0) + " to proper spot, which is " + childGameObjects[currentchildGameObject].Find("Colliders"));
                                    //DestroyImmediate(currentSubObjectMetadataGroup.GetChild(0).gameObject);
                                    currentSubObjectMetadataGroup.GetChild(0).SetParent(childGameObjects[currentchildGameObject].Find("Colliders"));

                                }
                            }

                            //If it's a visiblity point group...
                            else if (currentSubObjectMetadataGroup.name == "VisibilityPoints")
                            {
                                while (currentSubObjectMetadataGroup.childCount != 0)
                                {
                                    //Debug.Log("Moving " + currentSubObjectMetadataGroup.GetChild(0) + " to proper spot.");
                                    //DestroyImmediate(currentSubObjectMetadataGroup.GetChild(0).gameObject);
                                    currentSubObjectMetadataGroup.GetChild(0).SetParent(childGameObjects[currentchildGameObject].Find("VisibilityPoints"));
                                }
                            }

                            //Debug.Log("Now deleting " + currentMetadataGroup.GetChild(0));
                            DestroyImmediate(currentSubObjectMetadataGroup.gameObject);
                        }

                        //Regardless, destroy it at the end
                        //Debug.Log("Now deleting " + currentMetadataGroup);
                        DestroyImmediate(currentMetadataGroup.gameObject);
                        currentchildGameObject++;
                    }
                }
            }

            //Move subobjects to their correct places
            for (int i = 0; i < childGameObjects.Count; i++)
            {
                childGameObjects[i].localPosition = new Vector3(0, 0, spacing * (i / (transform.Find("mesh").childCount - 1) - 0.5f));
            }


            ///Run SimObjPhysics Setup
            //screenObject.GetComponent<SimObjPhysics>().ContextSetUpSimObjPhysics();
            //sheetObject.GetComponent<SimObjPhysics>().ContextSetUpSimObjPhysics();
            transform.GetComponent<SimObjPhysics>().ContextSetUpSimObjPhysics();

            foreach (Transform childGameObject in childGameObjects)
            {
                childGameObject.GetComponent<SimObjPhysics>().ContextSetUpSimObjPhysics();
            }
            
            //Restore initial rotation
            transform.rotation = rotationSaver;

            spacingPrev = spacing;
            topShiftPrev = topShift;
            bottomShiftPrev = bottomShift;
            widthShiftPrev = widthShift;
        }
    }

    Bounds newSheetBounds(BoxCollider collider, Transform p1, Transform p2)
    {
        Bounds newBounds = new Bounds();

        newBounds.center = (p1.position + p2.position) / 2;
        newBounds.Encapsulate(p1.position);
        newBounds.Encapsulate(p2.position);

        return newBounds;
    }

    void deleteCollidersAndVisPoints(Transform gameObject)
    {
        //Delete existing colliders
        while (gameObject.Find("Colliders").childCount != 0)
        {
            GameObject.DestroyImmediate(gameObject.Find("Colliders").GetChild(0).gameObject);
        }

        //Delete existing visibility points
        while (gameObject.Find("VisibilityPoints").childCount != 0)
        {
            GameObject.DestroyImmediate(gameObject.Find("VisibilityPoints").GetChild(0).gameObject);
        }

        return;
    }

    //p1 is left-top
    //p2 is left-base
    //Bounds newLeftBounds(BoxCollider collider, Transform p1, Transform p2)
    //{
    //    Bounds newBounds = new Bounds();

    //    newBounds.center = (p1.position + p2.position) / 2;
    //    newBounds.Encapsulate(p1.position);
    //    newBounds.Encapsulate(p2.position);

    //    return newBounds;
    //}

    ////p1 is right-top
    ////p2 is right-base
    //Bounds newRightBounds(BoxCollider collider, Transform p1, Transform p2)
    //{
    //    Bounds newBounds = new Bounds();

    //    newBounds.center = (p1.position + p2.position) / 2;
    //    newBounds.Encapsulate(p1.position);
    //    newBounds.Encapsulate(p2.position);

    //    return newBounds;
    //}
}
#endif
