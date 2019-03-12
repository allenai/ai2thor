using System;
using System.Collections.Generic;
using UnityEngine;

// [ExecuteInEditMode] // Make mirror live-update even when not in play mode
public class MirrorScript : MonoBehaviour
{
    [Tooltip("Maximum number of per pixel lights that will show in the mirrored image")]
    public int MaximumPerPixelLights = 2;

    [Tooltip("Texture size for the mirror, depending on how close the player can get to the mirror, this will need to be larger")]
    public int TextureSize = 768;

    [Tooltip("Subtracted from the near plane of the mirror")]
    public float ClipPlaneOffset = 0.07f;

    [Tooltip("Far clip plane for mirro camera")]
    public float FarClipPlane = 1000.0f;

    [Tooltip("What layers will be reflected?")]
    public LayerMask ReflectLayers = -1;

    [Tooltip("Add a flare layer to the reflection camera?")]
    public bool AddFlareLayer = false;

    [Tooltip("For quads, the normal points forward (true). For planes, the normal points up (false)")]
    public bool NormalIsForward = true;

    [Tooltip("Aspect ratio (width / height). Set to 0 to use default.")]
    public float AspectRatio = 0.0f;

    [Tooltip("Set to true if you have multiple mirrors facing each other to get an infinite effect, otherwise leave as false for a more realistic mirror effect.")]
    public bool MirrorRecursion;
}