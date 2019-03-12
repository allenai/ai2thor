// MIT License
// 
// Copyright (c) 2017 Justin Larrabee <justonia@gmail.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public static class PhysicsExtensions
{
    //
    // Box
    //
    
    public static bool BoxCast(BoxCollider box, Vector3 direction, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        Vector3 center, halfExtents;
        Quaternion orientation;
        box.ToWorldSpaceBox(out center, out halfExtents, out orientation);
        return Physics.BoxCast(center, halfExtents, direction, orientation, maxDistance, layerMask, queryTriggerInteraction);
    }

    public static bool BoxCast(BoxCollider box, Vector3 direction, out RaycastHit hitInfo, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        Vector3 center, halfExtents;
        Quaternion orientation;
        box.ToWorldSpaceBox(out center, out halfExtents, out orientation);
        return Physics.BoxCast(center, halfExtents, direction, out hitInfo, orientation, maxDistance, layerMask, queryTriggerInteraction);
    }

    public static RaycastHit[] BoxCastAll(BoxCollider box, Vector3 direction, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        Vector3 center, halfExtents;
        Quaternion orientation;
        box.ToWorldSpaceBox(out center, out halfExtents, out orientation);
        return Physics.BoxCastAll(center, halfExtents, direction, orientation, maxDistance, layerMask, queryTriggerInteraction);
    }

    public static int BoxCastNonAlloc(BoxCollider box, Vector3 direction, RaycastHit[] results, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        Vector3 center, halfExtents;
        Quaternion orientation;
        box.ToWorldSpaceBox(out center, out halfExtents, out orientation);
        return Physics.BoxCastNonAlloc(center, halfExtents, direction, results, orientation, maxDistance, layerMask, queryTriggerInteraction);
    }

    public static bool CheckBox(BoxCollider box, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        Vector3 center, halfExtents;
        Quaternion orientation;
        box.ToWorldSpaceBox(out center, out halfExtents, out orientation);
        return Physics.CheckBox(center, halfExtents, orientation, layerMask, queryTriggerInteraction);
    }

    public static Collider[] OverlapBox(
        BoxCollider box, 
        int layerMask = Physics.DefaultRaycastLayers,
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
        float expandBy = 0.0f)
    {
        Vector3 center, halfExtents;
        Quaternion orientation;
        box.ToWorldSpaceBox(out center, out halfExtents, out orientation);
        if (expandBy != 0.0f) {
            halfExtents = new Vector3(expandBy + halfExtents.x, expandBy + halfExtents.y, expandBy + halfExtents.z);
        }
        return Physics.OverlapBox(center, halfExtents, orientation, layerMask, queryTriggerInteraction);
    }

    public static int OverlapBoxNonAlloc(BoxCollider box, Collider[] results, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        Vector3 center, halfExtents;
        Quaternion orientation;
        box.ToWorldSpaceBox(out center, out halfExtents, out orientation);
        return Physics.OverlapBoxNonAlloc(center, halfExtents, results, orientation, layerMask, queryTriggerInteraction);
    }

    public static void ToWorldSpaceBox(this BoxCollider box, out Vector3 center, out Vector3 halfExtents, out Quaternion orientation)
    {
        orientation = box.transform.rotation;
        center = box.transform.TransformPoint(box.center);
        var lossyScale = box.transform.lossyScale;
        var scale = AbsVec3(lossyScale);
        halfExtents = Vector3.Scale(scale, box.size) * 0.5f;
    }

    //
    // Sphere
    //
    
    public static bool SphereCast(SphereCollider sphere, Vector3 direction, out RaycastHit hitInfo, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        Vector3 center;
        float radius;
        sphere.ToWorldSpaceSphere(out center, out radius);
        return Physics.SphereCast(center, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
    }

    public static RaycastHit[] SphereCastAll(SphereCollider sphere, Vector3 direction, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        Vector3 center;
        float radius;
        sphere.ToWorldSpaceSphere(out center, out radius);
        return Physics.SphereCastAll(center, radius, direction, maxDistance, layerMask, queryTriggerInteraction);
    }

    public static int SphereCastNonAlloc(SphereCollider sphere, Vector3 direction, RaycastHit[] results, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        Vector3 center;
        float radius;
        sphere.ToWorldSpaceSphere(out center, out radius);
        return Physics.SphereCastNonAlloc(center, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction);
    }

    public static bool CheckSphere(SphereCollider sphere, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        Vector3 center;
        float radius;
        sphere.ToWorldSpaceSphere(out center, out radius);
        return Physics.CheckSphere(center, radius, layerMask, queryTriggerInteraction);
    }

    public static Collider[] OverlapSphere
    (SphereCollider sphere,
    int layerMask = Physics.DefaultRaycastLayers,
    QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
    float expandBy = 0.0f)
    {
        Vector3 center;
        float radius;
        sphere.ToWorldSpaceSphere(out center, out radius);
        radius += expandBy;
        return Physics.OverlapSphere(center, radius, layerMask, queryTriggerInteraction);
    }

    public static int OverlapSphereNonAlloc(SphereCollider sphere, Collider[] results, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        Vector3 center;
        float radius;
        sphere.ToWorldSpaceSphere(out center, out radius);
        return Physics.OverlapSphereNonAlloc(center, radius, results, layerMask, queryTriggerInteraction);
    }

    public static void ToWorldSpaceSphere(this SphereCollider sphere, out Vector3 center, out float radius) 
    {
        center = sphere.transform.TransformPoint(sphere.center);
        radius = sphere.radius * MaxVec3(AbsVec3(sphere.transform.lossyScale));
    }

    //
    // Capsule
    //
    
    public static bool CapsuleCast(CapsuleCollider capsule, Vector3 direction, out RaycastHit hitInfo, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        Vector3 point0, point1;
        float radius;
        capsule.ToWorldSpaceCapsule(out point0, out point1, out radius);
        return Physics.CapsuleCast(point0, point1, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
    }

    public static RaycastHit[] CapsuleCastAll(CapsuleCollider capsule, Vector3 direction, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        Vector3 point0, point1;
        float radius;
        capsule.ToWorldSpaceCapsule(out point0, out point1, out radius);
        return Physics.CapsuleCastAll(point0, point1, radius, direction, maxDistance, layerMask, queryTriggerInteraction);
    }

    public static int CapsuleCastNonAlloc(CapsuleCollider capsule, Vector3 direction, RaycastHit[] results, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        Vector3 point0, point1;
        float radius;
        capsule.ToWorldSpaceCapsule(out point0, out point1, out radius);
        return Physics.CapsuleCastNonAlloc(point0, point1, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction);
    }

    public static bool CheckCapsule(CapsuleCollider capsule, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        Vector3 point0, point1;
        float radius;
        capsule.ToWorldSpaceCapsule(out point0, out point1, out radius);
        return Physics.CheckCapsule(point0, point1, radius, layerMask, queryTriggerInteraction);
    }

    public static Collider[] OverlapCapsule(
        CapsuleCollider capsule, 
        int layerMask = Physics.DefaultRaycastLayers, 
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
        float expandBy = 0.0f)
    {
        Vector3 point0, point1;
        float radius;
        capsule.ToWorldSpaceCapsule(out point0, out point1, out radius);
        return Physics.OverlapCapsule(point0, point1, expandBy + radius, layerMask, queryTriggerInteraction);
    }

    public static int OverlapCapsuleNonAlloc(CapsuleCollider capsule, Collider[] results, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
    {
        Vector3 point0, point1;
        float radius;
        capsule.ToWorldSpaceCapsule(out point0, out point1, out radius);
        return Physics.OverlapCapsuleNonAlloc(point0, point1, radius, results, layerMask, queryTriggerInteraction);
    }

    public static void ToWorldSpaceCapsule(this CapsuleCollider capsule, out Vector3 point0, out Vector3 point1, out float radius)
    {
        var center = capsule.transform.TransformPoint(capsule.center);
        radius = 0f;
        float height = 0f;
        Vector3 lossyScale = AbsVec3(capsule.transform.lossyScale);
        Vector3 dir = Vector3.zero;

        switch (capsule.direction) {
        case 0: // x
            radius = Mathf.Max(lossyScale.y, lossyScale.z) * capsule.radius;
            height = lossyScale.x * capsule.height;
            dir = capsule.transform.TransformDirection(Vector3.right);
            break;
        case 1: // y
            radius = Mathf.Max(lossyScale.x, lossyScale.z) * capsule.radius;
            height = lossyScale.y * capsule.height;
            dir = capsule.transform.TransformDirection(Vector3.up);
            break;
        case 2: // z
            radius = Mathf.Max(lossyScale.x, lossyScale.y) * capsule.radius;
            height = lossyScale.z * capsule.height;
            dir = capsule.transform.TransformDirection(Vector3.forward);
            break;
        }

        if (height < radius*2f) {
            dir = Vector3.zero;
        }

        point0 = center + dir * (height * 0.5f - radius);
        point1 = center - dir * (height * 0.5f - radius);
    }

    //  
    // Util
    //

    public static void SortClosestToFurthest(RaycastHit[] hits, int hitCount = -1)
    {
        if (hitCount == 0) {
            return;
        }

        if (hitCount < 0) {
            hitCount = hits.Length;
        }

        Array.Sort<RaycastHit>(hits, 0, hitCount, ascendDistance);
    }

    //
    // Private 
    //
    
    private class AscendingDistanceComparer : IComparer<RaycastHit>
    {
        public int Compare(RaycastHit h1, RaycastHit h2)
        {
            return h1.distance < h2.distance ? -1 : (h1.distance > h2.distance ? 1 : 0);
        }
    }

    private static AscendingDistanceComparer ascendDistance = new AscendingDistanceComparer();

    private static Vector3 AbsVec3(Vector3 v)
    {
        return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

    private static float MaxVec3(Vector3 v)
    {
        return Mathf.Max(v.x, Mathf.Max(v.y, v.z));
    }
}
