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

    // Update is called once per frame
    void Update()
    {

        //Debug.Log(topShiftPrev + "vs " + topShift);

        if (topShift != topShiftPrev || bottomShift != bottomShiftPrev || widthShift != widthShiftPrev)
        {
            //Debug.Log("Something changed!");

            GameObject leftBaseJoint = GameObject.Find("sheet_pillar_l_base_jnt");
            GameObject rightBaseJoint = GameObject.Find("sheet_pillar_r_base_jnt");
            GameObject leftBottomJoint = GameObject.Find("sheet_pillar_l_bottom_jnt");
            GameObject rightBottomJoint = GameObject.Find("sheet_pillar_r_bottom_jnt");
            GameObject leftTopJoint = GameObject.Find("sheet_pillar_l_top_jnt");
            GameObject rightTopJoint = GameObject.Find("sheet_pillar_r_top_jnt");

            if (topShift != topShiftPrev || bottomShift != bottomShiftPrev)
            {
                //float newTopHeight = leftTopJoint.transform.localPosition.y + topShift - topShiftPrev;
                //leftTopJoint.transform.localPosition = new Vector3(leftTopJoint.transform.localPosition.x, newTopHeight, leftTopJoint.transform.localPosition.z);
                //rightTopJoint.transform.localPosition = new Vector3(rightTopJoint.transform.localPosition.x, newTopHeight, rightTopJoint.transform.localPosition.z);

                BoxCollider colSheet = transform.Find("Colliders").transform.Find("Col_s").transform.GetComponent<BoxCollider>();

                Bounds newBounds = newSheetBounds(colSheet);

                colSheet.center = newBounds.center - colSheet.gameObject.transform.position;
                colSheet.size = new Vector3(newBounds.size.x, newBounds.size.y, 0.03f);

                if (topShift != topShiftPrev)
                {
                    leftTopJoint.transform.position += leftTopJoint.transform.forward * (topShift - topShiftPrev);
                    rightTopJoint.transform.position += rightTopJoint.transform.forward * (topShift - topShiftPrev);

                    float colHeight = Vector3.Distance(GameObject.Find("sheet_pillar_l_base_jnt").transform.position, GameObject.Find("sheet_pillar_l_top_jnt").transform.position);
                    BoxCollider colL = transform.Find("Colliders").transform.Find("Col_l2").transform.GetComponent<BoxCollider>();
                    BoxCollider colR = transform.Find("Colliders").transform.Find("Col_r2").transform.GetComponent<BoxCollider>();

                    colL.center = ((GameObject.Find("sheet_pillar_l_top_jnt").transform.position + GameObject.Find("sheet_pillar_l_base_jnt").transform.position) / 2) - colL.transform.position;
                    colL.size = new Vector3(0.05f, colHeight, 0.05f);

                    colR.center = ((GameObject.Find("sheet_pillar_r_top_jnt").transform.position + GameObject.Find("sheet_pillar_r_base_jnt").transform.position) / 2) - colR.transform.position;
                    colR.size = new Vector3(0.05f, colHeight, 0.05f);
                }

                if (bottomShift != bottomShiftPrev)
                {
                    leftBottomJoint.transform.position += leftBottomJoint.transform.forward * (bottomShift - bottomShiftPrev);
                    rightBottomJoint.transform.position += rightBottomJoint.transform.forward * (bottomShift - bottomShiftPrev);
                }
            }

            if (widthShift != widthShiftPrev)
            {
                leftBaseJoint.transform.position += leftBaseJoint.transform.right * (widthShift - widthShiftPrev);
                rightBaseJoint.transform.position += -rightBaseJoint.transform.right * (widthShift - widthShiftPrev);

                Transform[] colLeft = new Transform[2];
                colLeft[0] = transform.Find("Colliders").transform.Find("Col_l1").transform;
                colLeft[1] = transform.Find("Colliders").transform.Find("Col_l2").transform;

                Transform[] colRight = new Transform[2];
                colRight[0] = transform.Find("Colliders").transform.Find("Col_r1").transform;
                colRight[1] = transform.Find("Colliders").transform.Find("Col_r2").transform;

                Transform colCenter = transform.Find("Colliders").transform.Find("Col_s").transform;

                foreach (Transform transform in colLeft)
                {
                    transform.position += transform.right * (widthShift - widthShiftPrev);
                }

                foreach (Transform transform in colRight)
                {
                    transform.position -= transform.right * (widthShift - widthShiftPrev);
                }

                colCenter.GetComponent<BoxCollider>().size += transform.right * 2 * (widthShift - widthShiftPrev);
            }

            transform.GetComponent<SimObjPhysics>().ContextSetUpSimObjPhysics();
        }

        topShiftPrev = topShift;
        bottomShiftPrev = bottomShift;
        widthShiftPrev = widthShift;
    }

    Bounds newSheetBounds(BoxCollider collider)
    {
        Bounds newBounds = new Bounds();

        newBounds.center = (GameObject.Find("sheet_pillar_l_top_jnt").transform.position + GameObject.Find("sheet_pillar_r_bottom_jnt").transform.position) / 2;
        newBounds.Encapsulate(GameObject.Find("sheet_pillar_l_top_jnt").transform.position);
        newBounds.Encapsulate(GameObject.Find("sheet_pillar_r_bottom_jnt").transform.position);

        return newBounds;
    }

    Bounds newLeftBounds(BoxCollider collider)
    {
        Bounds newBounds = new Bounds();

        newBounds.center = (GameObject.Find("sheet_pillar_l_top_jnt").transform.position + GameObject.Find("sheet_pillar_l_base_jnt").transform.position) / 2;
        newBounds.Encapsulate(GameObject.Find("sheet_pillar_l_top_jnt").transform.position);
        newBounds.Encapsulate(GameObject.Find("sheet_pillar_l_base_jnt").transform.position);

        return newBounds;
    }

    Bounds newRightBounds(BoxCollider collider)
    {
        Bounds newBounds = new Bounds();

        newBounds.center = (GameObject.Find("sheet_pillar_r_top_jnt").transform.position + GameObject.Find("sheet_pillar_r_base_jnt").transform.position) / 2;
        newBounds.Encapsulate(GameObject.Find("sheet_pillar_r_top_jnt").transform.position);
        newBounds.Encapsulate(GameObject.Find("sheet_pillar_r_base_jnt").transform.position);

        return newBounds;
    }
}
