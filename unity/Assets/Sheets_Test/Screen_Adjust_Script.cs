using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Screen_Adjust_Script : MonoBehaviour
{
    [Range(-1f, 1f)]
    public float topShift = 0;
    [Range(-1f, 1f)]
    public float bottomShift = 0;
    [Range(-1f, 1f)]
    public float widthShift = 0;

    float topShiftPrev, bottomShiftPrev, widthShiftPrev = 0;

    int staticVisPoints = 4;

    // Update is called once per frame
    void Update()
    {
            //Debug.Log(topShiftPrev + "vs " + topShift);

        if (topShift != topShiftPrev || bottomShift != bottomShiftPrev || widthShift != widthShiftPrev)
        {
            //Debug.Log("Something changed!");

            Vector3 rotationSurrogate = transform.eulerAngles;
            transform.eulerAngles = Vector3.zero;

            GameObject leftBaseJoint = GameObject.Find("screen_pillar_l_base_jnt");
            GameObject rightBaseJoint = GameObject.Find("screen_pillar_r_base_jnt");
            GameObject leftBottomJoint = GameObject.Find("screen_pillar_l_bottom_jnt");
            GameObject rightBottomJoint = GameObject.Find("screen_pillar_r_bottom_jnt");
            GameObject leftTopJoint = GameObject.Find("screen_pillar_l_top_jnt");
            GameObject rightTopJoint = GameObject.Find("screen_pillar_r_top_jnt");

            if (topShift != topShiftPrev || bottomShift != bottomShiftPrev)
            {

                if (topShift != topShiftPrev)
                {
                    //Adjust sheet
                    leftTopJoint.transform.position += leftTopJoint.transform.forward * (topShift - topShiftPrev);
                    rightTopJoint.transform.position += rightTopJoint.transform.forward * (topShift - topShiftPrev);

                    //Adjust pillars
                    float colHeight = Vector3.Distance(GameObject.Find("screen_pillar_l_base_jnt").transform.position, GameObject.Find("screen_pillar_l_top_jnt").transform.position);
                    BoxCollider colL = transform.Find("Colliders").transform.Find("Col_l2").transform.GetComponent<BoxCollider>();
                    BoxCollider colR = transform.Find("Colliders").transform.Find("Col_r2").transform.GetComponent<BoxCollider>();

                    colL.center = ((GameObject.Find("screen_pillar_l_top_jnt").transform.position + GameObject.Find("screen_pillar_l_base_jnt").transform.position) / 2) - colL.transform.position;
                    colL.center -= new Vector3(0, 0, colL.center.z);
                    colL.size = new Vector3(0.05f, colHeight, 0.05f);

                    colR.center = ((GameObject.Find("screen_pillar_r_top_jnt").transform.position + GameObject.Find("screen_pillar_r_base_jnt").transform.position) / 2) - colR.transform.position;
                    colR.center -= new Vector3(0, 0, colR.center.z);
                    colR.size = new Vector3(0.05f, colHeight, 0.05f);
                }

                if (bottomShift != bottomShiftPrev)
                {
                    //Adjust sheet
                    leftBottomJoint.transform.position += leftBottomJoint.transform.forward * (bottomShift - bottomShiftPrev);
                    rightBottomJoint.transform.position += rightBottomJoint.transform.forward * (bottomShift - bottomShiftPrev);
                }

                //Adjust sheet collider
                BoxCollider colSheet = transform.Find("Colliders").transform.Find("Col_s").transform.GetComponent<BoxCollider>();

                Bounds newBounds = newSheetBounds(colSheet);

                colSheet.center = newBounds.center - colSheet.gameObject.transform.position;
                colSheet.center -= new Vector3(0, 0, colSheet.center.z);
                colSheet.size = new Vector3(newBounds.size.x, newBounds.size.y, 0.03f);
            }

            if (widthShift != widthShiftPrev)
            {
                //Adjust sheet
                leftBaseJoint.transform.position += leftBaseJoint.transform.right * (widthShift - widthShiftPrev);
                rightBaseJoint.transform.position += -rightBaseJoint.transform.right * (widthShift - widthShiftPrev);

                //Adjust left pillar collider
                Transform[] colLeft = new Transform[2];
                colLeft[0] = transform.Find("Colliders").transform.Find("Col_l1").transform;
                colLeft[1] = transform.Find("Colliders").transform.Find("Col_l2").transform;

                foreach (Transform transform in colLeft)
                {
                    transform.position += transform.right * (widthShift - widthShiftPrev);
                }

                //Adjust right pillar collider
                Transform[] colRight = new Transform[2];
                colRight[0] = transform.Find("Colliders").transform.Find("Col_r1").transform;
                colRight[1] = transform.Find("Colliders").transform.Find("Col_r2").transform;

                foreach (Transform transform in colRight)
                {
                    transform.position -= transform.right * (widthShift - widthShiftPrev);
                }

                //Adjust sheet collider
                Transform colCenter = transform.Find("Colliders").transform.Find("Col_s").transform;
                colCenter.GetComponent<BoxCollider>().size += transform.right * 2 * (widthShift - widthShiftPrev);

                //Adjust left pillar base-vispoints
                Transform[] visPointsLeft = new Transform[2];
                visPointsLeft[0] = transform.Find("VisibilityPoints").transform.Find("vPoint_stable_1").transform;
                visPointsLeft[1] = transform.Find("VisibilityPoints").transform.Find("vPoint_stable_2").transform;

                foreach (Transform transform in visPointsLeft)
                {
                    transform.position += transform.right * (widthShift - widthShiftPrev);
                }

                Transform[] visPointsRight = new Transform[2];
                visPointsRight[0] = transform.Find("VisibilityPoints").transform.Find("vPoint_stable_3").transform;
                visPointsRight[1] = transform.Find("VisibilityPoints").transform.Find("vPoint_stable_4").transform;

                foreach (Transform transform in visPointsRight)
                {
                    transform.position -= transform.right * (widthShift - widthShiftPrev);
                }
            }

            //Adjust sheet vispoints
            float sheetWidth = Vector3.Distance(rightBottomJoint.transform.position, leftBottomJoint.transform.position);
            float sheetHeight = Vector3.Distance(rightBottomJoint.transform.position, rightTopJoint.transform.position);

            int sheetWidthVisCount = (int)Mathf.Floor(2 + (sheetWidth * 3));
            int sheetHeightVisCount = (int)Mathf.Floor(2 + (sheetHeight * 3));
            Vector3[] sheetVisPoints = new Vector3[sheetWidthVisCount * sheetHeightVisCount];

            int prevVisPoints = transform.Find("VisibilityPoints").childCount - staticVisPoints;

            //Define sheet vispoints array
            for (int i = 0; i < sheetHeightVisCount; i++)
            {
                for (int j = 0; j < sheetWidthVisCount; j++)
                {
                    sheetVisPoints[i * sheetWidthVisCount + j] = rightBottomJoint.transform.TransformPoint(new Vector3((sheetWidth / (sheetWidthVisCount - 1)) * j, 0, 0.05f * sheetHeight + (0.9f * sheetHeight / (sheetHeightVisCount - 1)) * i));
                    //Debug.Log("Vispoint " + (i * sheetWidthVisCount + j + 1) + " has coordinates " + sheetVisPoints[i * sheetWidthVisCount + j]);
                }
            }

            //Use first existing vispoint as reference
            GameObject visPointObject = transform.Find("VisibilityPoints").GetChild(staticVisPoints).gameObject;

            //Delete any excess vispoints
            if (sheetVisPoints.Length < prevVisPoints)
            {
                for (int i = 0; i < prevVisPoints - sheetVisPoints.Length; i++)
                {
                    DestroyImmediate(transform.Find("VisibilityPoints").GetChild(staticVisPoints + sheetVisPoints.Length).gameObject);
                }
            }

            //Generate new vispoints
            for (int i = 0; i < sheetVisPoints.Length; i++)
            {
                if (i < transform.Find("VisibilityPoints").childCount - staticVisPoints)
                {
                    transform.Find("VisibilityPoints").transform.GetChild(staticVisPoints + i).name = ("vPoint_dynamic_" + (i + 1));
                    transform.Find("VisibilityPoints").transform.GetChild(staticVisPoints + i).transform.position = sheetVisPoints[i];
                }

                else
                {
                    GameObject newVisPoint = Instantiate(visPointObject, visPointObject.transform.parent);
                    newVisPoint.name = ("vPoint_dynamic_" + (i + 1));
                    newVisPoint.transform.position = sheetVisPoints[i];
                }
            }

            //Debug.Log("The sheet has a width of " + sheetWidth + ", and a height of " + sheetHeight + ", so there should be exactly " + sheetVisPoints.Length + " vispoints.");

            ///Run SimObjPhysics Setup
            transform.GetComponent<SimObjPhysics>().ContextSetUpSimObjPhysics();

            transform.eulerAngles = rotationSurrogate;

            topShiftPrev = topShift;
            bottomShiftPrev = bottomShift;
            widthShiftPrev = widthShift;
        }
    }

    Bounds newSheetBounds(BoxCollider collider)
    {
        Bounds newBounds = new Bounds();

        newBounds.center = (GameObject.Find("screen_pillar_l_top_jnt").transform.position + GameObject.Find("screen_pillar_r_bottom_jnt").transform.position) / 2;
        newBounds.Encapsulate(GameObject.Find("screen_pillar_l_top_jnt").transform.position);
        newBounds.Encapsulate(GameObject.Find("screen_pillar_r_bottom_jnt").transform.position);

        return newBounds;
    }

    Bounds newLeftBounds(BoxCollider collider)
    {
        Bounds newBounds = new Bounds();

        newBounds.center = (GameObject.Find("screen_pillar_l_top_jnt").transform.position + GameObject.Find("screen_pillar_l_base_jnt").transform.position) / 2;
        newBounds.Encapsulate(GameObject.Find("screen_pillar_l_top_jnt").transform.position);
        newBounds.Encapsulate(GameObject.Find("screen_pillar_l_base_jnt").transform.position);

        return newBounds;
    }

    Bounds newRightBounds(BoxCollider collider)
    {
        Bounds newBounds = new Bounds();

        newBounds.center = (GameObject.Find("screen_pillar_r_top_jnt").transform.position + GameObject.Find("screen_pillar_r_base_jnt").transform.position) / 2;
        newBounds.Encapsulate(GameObject.Find("screen_pillar_r_top_jnt").transform.position);
        newBounds.Encapsulate(GameObject.Find("screen_pillar_r_base_jnt").transform.position);

        return newBounds;
    }
}
