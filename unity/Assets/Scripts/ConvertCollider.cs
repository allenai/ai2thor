using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ConvertCollider
{
    public class ConvertedBox
    {
        public Vector3 center;
        public Vector3 halfExtents;
        public Quaternion orientation;
    }

    public class ConvertedCapsule
    {
        public Vector3 p1;
        public Vector3 p2;
        public float radius;
    }

    public static ConvertedBox ConvertBox(BoxCollider b)
    {
        ConvertedBox convertedBox = new ConvertedBox();
        convertedBox.center = b.transform.TransformPoint(b.center);
        convertedBox.halfExtents = Vector3.Scale(b.size, b.transform.lossyScale) / 2;
        convertedBox.orientation = b.transform.rotation;
        return convertedBox;
    }

    public static ConvertedCapsule ConvertCapsule(SphereCollider s)
    {
        ConvertedCapsule convertedCapsule = new ConvertedCapsule();
        convertedCapsule.p1 = s.transform.TransformPoint(s.center);
        convertedCapsule.p2 = s.transform.TransformPoint(s.center);
        convertedCapsule.radius = s.radius * Mathf.Max(s.transform.lossyScale.x, s.transform.lossyScale.y, s.transform.lossyScale.z);
        return convertedCapsule;
    }

    public static ConvertedCapsule ConvertCapsule(CapsuleCollider c)
    {
        Vector3 localAxis;
        float radiusScale;

        if (c.direction == 0)
        {
            localAxis = Vector3.right;
            radiusScale = Mathf.Max(c.transform.lossyScale.y, c.transform.lossyScale.z);
        }

        else if (c.direction == 1)
        {
            localAxis = Vector3.up;
            radiusScale = Mathf.Max(c.transform.lossyScale.x, c.transform.lossyScale.z);
        }

        else
        {
            localAxis = Vector3.forward;
            radiusScale = Mathf.Max(c.transform.lossyScale.x, c.transform.lossyScale.y);
        }

        ConvertedCapsule convertedCapsule = new ConvertedCapsule();
        convertedCapsule.radius = c.radius * radiusScale;

        if (c.height * c.transform.lossyScale[c.direction] >= 2 * radiusScale * c.radius)
        {
            convertedCapsule.p1 = c.transform.TransformPoint(c.center + (c.height / 2) * localAxis) - c.transform.TransformDirection(c.radius * radiusScale * localAxis);
            convertedCapsule.p2 = c.transform.TransformPoint(c.center - (c.height / 2) * localAxis) + c.transform.TransformDirection(c.radius * radiusScale * localAxis);
        }

        else
        {
            convertedCapsule.p1 = c.transform.TransformPoint(c.center);
            convertedCapsule.p2 = c.transform.TransformPoint(c.center);
        }

        return convertedCapsule;
    }
}