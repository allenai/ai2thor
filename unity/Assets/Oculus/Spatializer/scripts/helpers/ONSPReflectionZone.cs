/************************************************************************************
Filename    :   ONSPReflectionZone.cs
Content     :   Add reflection zone volumes to set reflection parameters via snapshots.
Copyright   :   Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Licensed under the Oculus SDK Version 3.5 (the "License"); 
you may not use the Oculus SDK except in compliance with the License, 
which is provided at the time of installation or download, or which 
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developer.oculus.com/licenses/sdk-3.5/

Unless required by applicable law or agreed to in writing, the Oculus SDK 
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
************************************************************************************/
using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

public struct ReflectionSnapshot
{
    public  AudioMixerSnapshot mixerSnapshot;
    public  float              fadeTime;
}

public class ONSPReflectionZone : MonoBehaviour 
{
    public AudioMixerSnapshot mixerSnapshot = null;
    public float fadeTime                   = 0.0f;

	// Push/pop list
    private static Stack<ReflectionSnapshot> snapshotList        = new Stack<ReflectionSnapshot>();
    private static ReflectionSnapshot        currentSnapshot     = new ReflectionSnapshot();

	/// <summary>
	/// Start this instance.
	/// </summary>
	void Start () 
	{
	}

	/// <summary>
	/// Update this instance.
	/// </summary>
	void Update () 
	{
	}

	/// <summary>
	/// Raises the trigger enter event.
	/// </summary>
	/// <param name="other">Other.</param>
	void OnTriggerEnter(Collider other) 
	{
		if(CheckForAudioListener(other.gameObject) == true)
		{
            PushCurrentMixerShapshot();
		}
	}

	/// <summary>
	/// Raises the trigger exit event.
	/// </summary>
	/// <param name="other">Other.</param>
	void OnTriggerExit(Collider other)
	{
		if(CheckForAudioListener(other.gameObject) == true)
		{
			PopCurrentMixerSnapshot();			
		}
	}


	// * * * * * * * * * * * * *
	// Private functions

	/// <summary>
	/// Checks for audio listener.
	/// </summary>
	/// <returns><c>true</c>, if for audio listener was checked, <c>false</c> otherwise.</returns>
	/// <param name="gameObject">Game object.</param>
	bool CheckForAudioListener(GameObject gameObject)
	{
		AudioListener al = gameObject.GetComponentInChildren<AudioListener>();
		if(al != null)
			return true;

		return false;
	}
	
	/// <summary>
	/// Pushs the current mixer snapshot onto the snapshot stack
	/// </summary>
	void PushCurrentMixerShapshot()
	{
        ReflectionSnapshot css = currentSnapshot;
        snapshotList.Push(css);	

		// Set the zone reflection values
		// NOTE: There will be conditions that might need resolution when dealing with volumes that 
		// overlap. Best practice is to never have volumes half-way inside other volumes; larger
		// volumes should completely contain smaller volumes
		SetReflectionValues();
	}

    	/// <summary>
	/// Pops the current reflection values from reflectionsList stack.
	/// </summary>
	void PopCurrentMixerSnapshot()
	{
        ReflectionSnapshot snapshot = snapshotList.Pop();

		// Set the popped reflection values
        SetReflectionValues(ref snapshot);
	}

	/// <summary>
	/// Sets the reflection values. This is done when entering a zone (use zone values).
	/// </summary>
	void SetReflectionValues()
	{
        if (mixerSnapshot != null)
        {
            Debug.Log("Setting off snapshot " + mixerSnapshot.name);
            mixerSnapshot.TransitionTo(fadeTime);

            // Set the current snapshot to be equal to this one
            currentSnapshot.mixerSnapshot = mixerSnapshot;
            currentSnapshot.fadeTime      = fadeTime;
        }
        else
        {
            Debug.Log("Mixer snapshot not set - Please ensure play area has at least one encompassing snapshot.");
        }
    }

	/// <summary>
	/// Sets the reflection values. This is done when exiting a zone (use popped values).
	/// </summary>
	/// <param name="rm">Rm.</param>
    void SetReflectionValues(ref ReflectionSnapshot mss)
	{
        if(mss.mixerSnapshot != null)
        {
            Debug.Log("Setting off snapshot " + mss.mixerSnapshot.name);
            mss.mixerSnapshot.TransitionTo(mss.fadeTime);

            // Set the current snapshot to be equal to this one
            currentSnapshot.mixerSnapshot = mss.mixerSnapshot;
            currentSnapshot.fadeTime = mss.fadeTime;

        }
        else
        {
            Debug.Log("Mixer snapshot not set - Please ensure play area has at least one encompassing snapshot.");
        }
    }
}
