/************************************************************************************
Filename    :   OVRLipSyncContextMorphTarget.cs
Content     :   This bridges the viseme output to the morph targets
Created     :   August 7th, 2015
Copyright   :   Copyright Facebook Technologies, LLC and its affiliates.
                All rights reserved.

Licensed under the Oculus Audio SDK License Version 3.3 (the "License");
you may not use the Oculus Audio SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developer.oculus.com/licenses/audio-3.3/

Unless required by applicable law or agreed to in writing, the Oculus Audio SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
************************************************************************************/
using UnityEngine;
using System.Linq;

public class OVRLipSyncContextMorphTarget : MonoBehaviour
{
    // PUBLIC

    // Manually assign the skinned mesh renderer to this script
    [Tooltip("Skinned Mesh Rendered target to be driven by Oculus Lipsync")]
    public SkinnedMeshRenderer skinnedMeshRenderer = null;

    // Set the blendshape index to go to (-1 means there is not one assigned)
    [Tooltip("Blendshape index to trigger for each viseme.")]
    public int [] visemeToBlendTargets = Enumerable.Range(0, OVRLipSync.VisemeCount).ToArray();

    // enable/disable sending signals to viseme engine
    [Tooltip("Enable using the test keys defined below to manually trigger each viseme.")]
    public bool enableVisemeTestKeys = false;
    [Tooltip("Test keys used to manually trigger an individual viseme - by " +
        "default the QWERTY row of a US keyboard.")]
    public KeyCode[] visemeTestKeys =
    {
        KeyCode.BackQuote,
        KeyCode.Tab,
        KeyCode.Q,
        KeyCode.W,
        KeyCode.E,
        KeyCode.R,
        KeyCode.T,
        KeyCode.Y,
        KeyCode.U,
        KeyCode.I,
        KeyCode.O,
        KeyCode.P,
        KeyCode.LeftBracket,
        KeyCode.RightBracket,
        KeyCode.Backslash,
    };

    [Tooltip("Test key used to manually trigger laughter and visualise the results")]
    public KeyCode laughterKey = KeyCode.CapsLock;

    [Tooltip("Blendshape index to trigger for laughter")]
    public int laughterBlendTarget = OVRLipSync.VisemeCount;

    [Range(0.0f, 1.0f)]
    [Tooltip("Laughter probability threshold above which the laughter blendshape will be activated")]
    public float laughterThreshold = 0.5f;

    [Range(0.0f, 3.0f)]
    [Tooltip("Laughter animation linear multiplier, the final output will be clamped to 1.0")]
    public float laughterMultiplier = 1.5f;

    // smoothing amount
    [Range(1, 100)]
    [Tooltip("Smoothing of 1 will yield only the current predicted viseme, 100 will yield an extremely smooth viseme response.")]
    public int smoothAmount = 70;

    // PRIVATE

    // Look for a lip-sync Context (should be set at the same level as this component)
    private OVRLipSyncContextBase lipsyncContext = null;


    /// <summary>
    /// Start this instance.
    /// </summary>
    void Start ()
    {
        // morph target needs to be set manually; possibly other components will need the same
        if(skinnedMeshRenderer == null)
        {
            Debug.LogError("LipSyncContextMorphTarget.Start Error: " +
                "Please set the target Skinned Mesh Renderer to be controlled!");
            return;
        }

        // make sure there is a phoneme context assigned to this object
        lipsyncContext = GetComponent<OVRLipSyncContextBase>();
        if(lipsyncContext == null)
        {
            Debug.LogError("LipSyncContextMorphTarget.Start Error: " +
                "No OVRLipSyncContext component on this object!");
        }
        else
        {
            // Send smoothing amount to context
            lipsyncContext.Smoothing = smoothAmount;
        }
    }

    /// <summary>
    /// Update this instance.
    /// </summary>
    void Update ()
    {
        if((lipsyncContext != null) && (skinnedMeshRenderer != null))
        {
            // get the current viseme frame
            OVRLipSync.Frame frame = lipsyncContext.GetCurrentPhonemeFrame();
            if (frame != null)
            {
                SetVisemeToMorphTarget(frame);

                SetLaughterToMorphTarget(frame);
            }

            // TEST visemes by capturing key inputs and sending a signal
            CheckForKeys();

            // Update smoothing value
            if (smoothAmount != lipsyncContext.Smoothing)
            {
                lipsyncContext.Smoothing = smoothAmount;
            }
        }
    }

    /// <summary>
    /// Sends the signals.
    /// </summary>
    void CheckForKeys()
    {
        if (enableVisemeTestKeys)
        {
            for (int i = 0; i < OVRLipSync.VisemeCount; ++i)
            {
                CheckVisemeKey(visemeTestKeys[i], i, 100);
            }
        }

        CheckLaughterKey();
    }

    /// <summary>
    /// Sets the viseme to morph target.
    /// </summary>
    void SetVisemeToMorphTarget(OVRLipSync.Frame frame)
    {
        for (int i = 0; i < visemeToBlendTargets.Length; i++)
        {
            if (visemeToBlendTargets[i] != -1)
            {
                // Viseme blend weights are in range of 0->1.0, we need to make range 100
                skinnedMeshRenderer.SetBlendShapeWeight(
                    visemeToBlendTargets[i],
                    frame.Visemes[i] * 100.0f);
            }
        }
    }

    /// <summary>
    /// Sets the laughter to morph target.
    /// </summary>
    void SetLaughterToMorphTarget(OVRLipSync.Frame frame)
    {
        if (laughterBlendTarget != -1)
        {
            // Laughter score will be raw classifier output in [0,1]
            float laughterScore = frame.laughterScore;

            // Threshold then re-map to [0,1]
            laughterScore = laughterScore < laughterThreshold ? 0.0f : laughterScore - laughterThreshold;
            laughterScore = Mathf.Min(laughterScore * laughterMultiplier, 1.0f);
            laughterScore *= 1.0f / laughterThreshold;

            skinnedMeshRenderer.SetBlendShapeWeight(
                laughterBlendTarget,
                laughterScore * 100.0f);
        }
    }

    /// <summary>
    /// Sends the viseme signal.
    /// </summary>
    /// <param name="key">Key.</param>
    /// <param name="viseme">Viseme.</param>
    /// <param name="arg1">Arg1.</param>
    void CheckVisemeKey(KeyCode key, int viseme, int amount)
    {
        if (Input.GetKeyDown(key))
        {
            lipsyncContext.SetVisemeBlend(visemeToBlendTargets[viseme], amount);
        }
        if (Input.GetKeyUp(key))
        {
            lipsyncContext.SetVisemeBlend(visemeToBlendTargets[viseme], 0);
        }
    }

    /// <summary>
    /// Sends the laughter signal.
    /// </summary>
    void CheckLaughterKey()
    {
        if (Input.GetKeyDown(laughterKey))
        {
            lipsyncContext.SetLaughterBlend(100);
        }
        if (Input.GetKeyUp(laughterKey))
        {
            lipsyncContext.SetLaughterBlend(0);
        }
    }
}
