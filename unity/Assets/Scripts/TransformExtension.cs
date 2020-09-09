
using System;
using UnityEngine;

public static class TransformExtension
{
    public static Transform FirstChildOrDefault(this Transform parent, Func<Transform, bool> query)
    {
        if (query(parent)) {
            return parent;
        }
        else {
            Transform result = null;
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var match = child.FirstChildOrDefault(query);
                result = match != null ? match : result;
            }
            return result;
        }
        
    }
}