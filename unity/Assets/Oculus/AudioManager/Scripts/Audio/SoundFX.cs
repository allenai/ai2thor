using UnityEngine;
using UnityEngine.Audio;

namespace OVR
{

public enum SoundFXNext {
	Random = 0,
	Sequential = 1,
}

public enum FreqHint {
	None = 0,
	Wide = 1,
	Narrow = 2,
}

public enum SoundPriority {
	VeryLow = -2,
	Low = -1,
	Default = 0,
	High = 1,
	VeryHigh = 2,
}

[System.Serializable]
public class OSPProps {
	public OSPProps() {
        enableSpatialization = false;
		useFastOverride = false;
		gain = 0.0f;
        enableInvSquare = false;
        volumetric = 0.0f;
		invSquareFalloff = new Vector2( 1.0f, 25.0f );
	}

	[Tooltip( "Set to true to play the sound FX spatialized with binaural HRTF, default = false")]
    public bool enableSpatialization = false;
	[Tooltip( "Play the sound FX with reflections, default = false")]
	public bool			useFastOverride = false;
	[Tooltip( "Boost the gain on the spatialized sound FX, default = 0.0")]
	[Range( 0.0f, 24.0f )]
	public float		gain = 0.0f;
    [Tooltip("Enable Inverse Square attenuation curve, default = false")]
    public bool         enableInvSquare = false;
    [Tooltip("Change the sound from point source (0.0f) to a spherical volume, default = 0.0")]
    [Range(0.0f, 1000.0f)]
    public float volumetric = 0.0f;
    [Tooltip("Set the near and far falloff value for the OSP attenuation curve, default = 1.0")]
	[MinMax ( 1.0f, 25.0f, 0.0f, 250.0f )]
	public Vector2		invSquareFalloff = new Vector2( 1.0f, 25.0f );
}

/*
-----------------------

SoundFX

-----------------------
*/
[System.Serializable]
public class SoundFX {
	public SoundFX() { 
		playback = SoundFXNext.Random;
		volume = 1.0f;
		pitchVariance = Vector2.one;
		falloffDistance = new Vector2( 1.0f, 25.0f );
		falloffCurve = AudioRolloffMode.Linear;
		volumeFalloffCurve = new AnimationCurve( new Keyframe[2] { new Keyframe( 0f, 1.0f ), new Keyframe( 1f, 1f ) } );
		reverbZoneMix = new AnimationCurve( new Keyframe[2] { new Keyframe( 0f, 1.0f ), new Keyframe( 1f, 1f ) } );
		spread = 0.0f;
		pctChanceToPlay = 1.0f;
		priority = SoundPriority.Default;
		delay = Vector2.zero;
		looping = false;
		ospProps = new OSPProps();
	}

	[Tooltip( "Each sound FX should have a unique name")]
	public string			name = string.Empty;
	[Tooltip( "Sound diversity playback option when multiple audio clips are defined, default = Random")]
	public SoundFXNext		playback = SoundFXNext.Random;
	[Tooltip( "Default volume for this sound FX, default = 1.0")]
	[Range (0.0f, 1.0f)]
	public float			volume = 1.0f;
	[Tooltip( "Random pitch variance each time a sound FX is played, default = 1.0 (none)")]
	[MinMax ( 1.0f, 1.0f, 0.0f, 2.0f )]
	public Vector2			pitchVariance = Vector2.one;
	[Tooltip( "Falloff distance for the sound FX, default = 1m min to 25m max")]
	[MinMax ( 1.0f, 25.0f, 0.0f, 250.0f )]
	public Vector2			falloffDistance = new Vector2( 1.0f, 25.0f );
	[Tooltip( "Volume falloff curve - sets how the sound FX attenuates over distance, default = Linear")]
	public AudioRolloffMode	falloffCurve = AudioRolloffMode.Linear;
	[Tooltip( "Defines the custom volume falloff curve")]
	public AnimationCurve   volumeFalloffCurve = new AnimationCurve( new Keyframe[2] { new Keyframe( 0f, 1.0f ), new Keyframe( 1f, 1f ) } );
	[Tooltip( "The amount by which the signal from the AudioSource will be mixed into the global reverb associated with the Reverb Zones | Valid range is 0.0 - 1.1, default = 1.0" )]
	public AnimationCurve   reverbZoneMix = new AnimationCurve( new Keyframe[2] { new Keyframe( 0f, 1.0f ), new Keyframe( 1f, 1f ) } );
	[Tooltip( "Sets the spread angle (in degrees) of a 3d stereo or multichannel sound in speaker space, default = 0")]
	[Range (0.0f, 360.0f)]
	public float			spread = 0.0f;
	[Tooltip( "The percentage chance that this sound FX will play | 0.0 = none, 1.0 = 100%, default = 1.0")]
	[Range (0.0f, 1.0f)]
	public float			pctChanceToPlay = 1.0f;
	[Tooltip( "Sets the priority for this sound to play and/or to override a currently playing sound FX, default = Default")]
	public SoundPriority	priority = SoundPriority.Default;
	[Tooltip( "Specifies the default delay when this sound FX is played, default = 0.0 secs")]
	[MinMax ( 0.0f, 0.0f, 0.0f, 2.0f )]
	public Vector2          delay = Vector2.zero;   // this overrides any delay passed into PlaySound() or PlaySoundAt()
	[Tooltip( "Set to true for the sound to loop continuously, default = false")]
	public bool				looping = false;
	public OSPProps			ospProps = new OSPProps();
	[Tooltip( "List of the audio clips assigned to this sound FX")]
	public AudioClip[]		soundClips = new AudioClip[1];
	// editor only - unfortunately if we set it not to serialize, we can't query it from the editor
	public bool             visibilityToggle = false;
	// runtime vars
	[System.NonSerialized]
	private SoundGroup      soundGroup = null;
	private int				lastIdx = -1;
	private int				playingIdx = -1;

	public int 				Length { get { return soundClips.Length; } }
	public bool				IsValid { get { return ( ( soundClips.Length != 0 ) && ( soundClips[0] != null ) ); } }
	public SoundGroup		Group { get { return soundGroup; } set { soundGroup = value; } }
	public float			MaxFalloffDistSquared { get { return falloffDistance.y * falloffDistance.y; } }
	public float			GroupVolumeOverride { get { return ( soundGroup != null ) ? soundGroup.volumeOverride : 1.0f; } }

	/*
	-----------------------
	GetClip()
	-----------------------
	*/
	public AudioClip GetClip() {
		if ( soundClips.Length == 0 ) {
			return null;
		} else if ( soundClips.Length == 1 ) {
			return soundClips[0];
		}
		if ( playback == SoundFXNext.Random ) {
			// random, but don't pick the last one
			int idx = Random.Range( 0, soundClips.Length );
			while ( idx == lastIdx ) {
				idx = Random.Range( 0, soundClips.Length );
			}
			lastIdx = idx;
			return soundClips[idx];
		} else {
			// sequential
			if ( ++lastIdx >= soundClips.Length ) {
				lastIdx = 0;
			}
			return soundClips[lastIdx];
		}
	}

	/*
	-----------------------
	GetMixerGroup()
	-----------------------
	*/
	public AudioMixerGroup GetMixerGroup( AudioMixerGroup defaultMixerGroup ) {
		if ( soundGroup != null ) {
			return ( soundGroup.mixerGroup != null ) ? soundGroup.mixerGroup : defaultMixerGroup;	
		}
		return defaultMixerGroup;
	} 

	/*
	-----------------------
	ReachedGroupPlayLimit()
	-----------------------
	*/
	public bool ReachedGroupPlayLimit() {
		if ( soundGroup != null ) {
			return !soundGroup.CanPlaySound();
		}
		return false;
	}

	/*
	-----------------------
	GetClipLength()
	-----------------------
	*/
	public float GetClipLength( int idx ) {
		if ( ( idx == -1 ) || ( soundClips.Length == 0 ) || ( idx >= soundClips.Length ) || ( soundClips[idx] == null ) ) {
			return 0.0f;
		} else {
			return soundClips[idx].length;
		}
	}

	/*
	-----------------------
	GetPitch()
	-----------------------
	*/
	public float GetPitch() {
		return Random.Range( pitchVariance.x, pitchVariance.y );
	}

	/*
	-----------------------
	PlaySound()
	-----------------------
	*/
	public int PlaySound( float delaySecs = 0.0f ) {
		playingIdx = -1;

		if ( !IsValid ) {
			return playingIdx;
		}

		// check the random chance to play here to save the function calls
		if ( ( pctChanceToPlay > 0.99f ) || ( Random.value < pctChanceToPlay ) ) {
			if ( delay.y > 0.0f ) {
				delaySecs = Random.Range( delay.x, delay.y );
			}
			playingIdx = AudioManager.PlaySound( this, EmitterChannel.Any, delaySecs );
		}

		return playingIdx;
	}

	/*
	-----------------------
	PlaySoundAt()
	-----------------------
	*/
	public int PlaySoundAt( Vector3 pos, float delaySecs = 0.0f, float volumeOverride = 1.0f, float pitchMultiplier = 1.0f ) {
		playingIdx = -1;

		if ( !IsValid ) {
			return playingIdx;
		}

		// check the random chance to play here to save the function calls
		if ( ( pctChanceToPlay > 0.99f ) || ( Random.value < pctChanceToPlay ) ) {
			if ( delay.y > 0.0f ) {
				delaySecs = Random.Range( delay.x, delay.y );
			}
			playingIdx = AudioManager.PlaySoundAt( pos, this, EmitterChannel.Any, delaySecs, volumeOverride, pitchMultiplier );
		}

		return playingIdx;
	}

	/*
	-----------------------
	SetOnFinished()
	get a callback when the sound is finished playing
	-----------------------
	*/
	public void SetOnFinished( System.Action onFinished ) {
		if ( playingIdx > -1 ) {
			AudioManager.SetOnFinished( playingIdx, onFinished );
		}
	}

	/*
	-----------------------
	SetOnFinished()
	get a callback with an object parameter when the sound is finished playing
	-----------------------
	*/
	public void SetOnFinished( System.Action<object> onFinished, object obj ) {
		if ( playingIdx > -1 ) {
			AudioManager.SetOnFinished( playingIdx, onFinished, obj );
		}
	}

	/*
	-----------------------
	StopSound()
	-----------------------
	*/
	public bool StopSound() {
		bool stopped = false;

		if (playingIdx > -1){
			stopped = AudioManager.StopSound(playingIdx);
			playingIdx = -1;
		}

		return stopped;
	}

	/*
	-----------------------
	AttachToParent()
	-----------------------
	*/
	public void AttachToParent( Transform parent) {
		if (playingIdx > -1) {
			AudioManager.AttachSoundToParent(playingIdx, parent);
		}
	}

	/*
	-----------------------
	DetachFromParent()
	-----------------------
	*/
	public void DetachFromParent() {
		if (playingIdx > -1) {
			AudioManager.DetachSoundFromParent(playingIdx);
		}
	}
}

} // namespace OVR
