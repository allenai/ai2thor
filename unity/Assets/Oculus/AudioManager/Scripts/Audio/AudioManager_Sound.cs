using UnityEngine;
using UnityEngine.Audio;

namespace OVR
{

//-------------------------------------------------------------------------
// Types
//-------------------------------------------------------------------------

public enum EmitterChannel {
	None = -1,
	Reserved = 0,	// plays on the single reserved emitter
	Any				// queues to the next available emitter
}

[System.Serializable]
public class MixerSnapshot {
	public AudioMixerSnapshot       snapshot = null;
	public float                    transitionTime = 0.25f;
}

/*
-----------------------
 
 GameManager Sound Routines

-----------------------
*/
public partial class AudioManager : MonoBehaviour {

	public enum Fade {
		In,
		Out
	}

	private float					audioMaxFallOffDistanceSqr = 25.0f * 25.0f;		// past this distance, sounds are ignored for the local player 

	private SoundEmitter[] 			soundEmitters = null;                           // pool of sound emitters to play sounds through

	private FastList<SoundEmitter>  playingEmitters = new FastList<SoundEmitter>();
	private FastList<SoundEmitter>  nextFreeEmitters = new FastList<SoundEmitter>();

	private MixerSnapshot			currentSnapshot = null;

	static private GameObject		soundEmitterParent = null;						// parent object for the sound emitters
	static private Transform		staticListenerPosition = null;                  // play position for regular 2D sounds

	static private bool				showPlayingEmitterCount = false;
	static private bool				forceShowEmitterCount = false;

	static private bool				soundEnabled = true;
	static public bool				SoundEnabled { get { return soundEnabled; } }

	static readonly AnimationCurve	defaultReverbZoneMix = new AnimationCurve( new Keyframe[2] { new Keyframe( 0f, 1.0f ), new Keyframe( 1f, 1f ) } );
 
	// Calculate the maximum number of emitters that can be running at one time.
	static private int CalculateMaxEmittersSize() {
		return theAudioManager.maxSoundEmitters + (int)EmitterChannel.Any;
	}

	// Verify if this index is valid
	static private bool ValidateEmitterIndex(int index) {
		return index > -1 && index < CalculateMaxEmittersSize();
	} 
	

	/*
	-----------------------
	InitializeSoundSystem()
	initialize persistent sound emitter objects that live across scene loads
	-----------------------
	*/
	void InitializeSoundSystem() {

		int bufferLength = 960;
		int numBuffers = 4;
		AudioSettings.GetDSPBufferSize( out bufferLength, out numBuffers );
		if ( Application.isPlaying ) {
			Debug.Log( "[AudioManager] Audio Sample Rate: " + AudioSettings.outputSampleRate );
			Debug.Log( "[AudioManager] Audio Buffer Length: " + bufferLength + " Size: " + numBuffers );
		}

		// find the audio listener for playing regular 2D sounds
		AudioListener audioListenerObject = GameObject.FindObjectOfType<AudioListener>() as AudioListener;
		if ( audioListenerObject == null ) {
			Debug.LogError( "[AudioManager] Missing AudioListener object!  Add one to the scene." );
		} else {
			staticListenerPosition = audioListenerObject.transform;
		}

		// we allocate maxSoundEmitters + reserved channels
		soundEmitters = new SoundEmitter[CalculateMaxEmittersSize()];
		
		// see if the sound emitters have already been created, if so, nuke it, it shouldn't exist in the scene upon load
		soundEmitterParent = GameObject.Find( "__SoundEmitters__" );
		if ( soundEmitterParent != null ) {
			// delete any sound emitters hanging around
			Destroy( soundEmitterParent );
		}

		// create them all															
		soundEmitterParent = new GameObject( "__SoundEmitters__" );
		for ( int i = 0; i < CalculateMaxEmittersSize(); i++ ) {
			GameObject emitterObject = new GameObject( "SoundEmitter_" + i ); 
			emitterObject.transform.parent = soundEmitterParent.transform;
			emitterObject.transform.position = Vector3.zero;
			// don't ever save this to the scene
			emitterObject.hideFlags = HideFlags.DontSaveInEditor;
			// add the sound emitter components
			soundEmitters[i] = emitterObject.AddComponent<SoundEmitter>();
			soundEmitters[i].SetDefaultParent( soundEmitterParent.transform );
			soundEmitters[i].SetChannel( i );
			soundEmitters[i].Stop();
			// save off the original index
			soundEmitters[i].originalIdx = i;
		}
		// reset the free emitter lists
		ResetFreeEmitters();
		soundEmitterParent.hideFlags = HideFlags.DontSaveInEditor;
			
		audioMaxFallOffDistanceSqr = audioMaxFallOffDistance * audioMaxFallOffDistance;
	}

	/*
	-----------------------
	UpdateFreeEmitters()
	-----------------------
	*/
	void UpdateFreeEmitters() {
		if ( verboseLogging ) {
			if ( Input.GetKeyDown( KeyCode.A ) ) {
				forceShowEmitterCount = !forceShowEmitterCount;
			}
			if ( forceShowEmitterCount ) {
				showPlayingEmitterCount = true;
			}
		}
		// display playing emitter count when the sound system is overwhelmed
		int total = 0, veryLow = 0, low = 0, def = 0, high = 0, veryHigh = 0;

		// find emitters that are done playing and add them to the nextFreeEmitters list
		for ( int i = 0; i < playingEmitters.size; ) {
			if ( playingEmitters[i] == null ) {
				Debug.LogError( "[AudioManager] ERROR: playingEmitters list had a null emitter! Something nuked a sound emitter!!!" );
				playingEmitters.RemoveAtFast( i );
				return;
			}
			if ( !playingEmitters[i].IsPlaying() ) {
				// add to the free list and remove from the playing list
				if ( verboseLogging ) {
					if ( nextFreeEmitters.Contains( playingEmitters[i] ) ) {
						Debug.LogError( "[AudioManager] ERROR: playing sound emitter already in the free emitters list!" );
					}
				}
				playingEmitters[i].Stop();
				nextFreeEmitters.Add( playingEmitters[i] );
				playingEmitters.RemoveAtFast( i );
				continue;
			}
			// debugging/profiling
			if ( verboseLogging && showPlayingEmitterCount ) {
				total++;
				switch ( playingEmitters[i].priority ) {
					case SoundPriority.VeryLow: veryLow++; break;
					case SoundPriority.Low: low++; break;
					case SoundPriority.Default: def++; break;
					case SoundPriority.High: high++; break;
					case SoundPriority.VeryHigh: veryHigh++; break;
				}
			}
			i++;
		}
		if ( verboseLogging && showPlayingEmitterCount ) {
			Debug.LogWarning( string.Format( "[AudioManager] Playing sounds: Total {0} | VeryLow {1} | Low {2} | Default {3} | High {4} | VeryHigh {5} | Free {6}", Fmt( total ), Fmt( veryLow ), Fmt( low ), Fmt( def ), Fmt( high ), Fmt( veryHigh ), FmtFree( nextFreeEmitters.Count ) ) );
			showPlayingEmitterCount = false;
		}
	}

	/*
	-----------------------
	Fmt()
	-----------------------
	*/
	string Fmt( int count ) {
		float t = count / (float)theAudioManager.maxSoundEmitters;
		if ( t < 0.5f ) {
			return "<color=green>" + count.ToString() + "</color>";
		} else if ( t < 0.7 ) {
			return "<color=yellow>" + count.ToString() + "</color>";
		} else {
			return "<color=red>" + count.ToString() + "</color>";
		}
	}

	/*
	-----------------------
	FmtFree()
	-----------------------
	*/
	string FmtFree( int count ) {
		float t = count / (float)theAudioManager.maxSoundEmitters;
		if ( t < 0.2f ) {
			return "<color=red>" + count.ToString() + "</color>";
		} else if ( t < 0.3 ) {
			return "<color=yellow>" + count.ToString() + "</color>";
		} else {
			return "<color=green>" + count.ToString() + "</color>";
		}
	}

	/*
	-----------------------
	OnPreSceneLoad()
	-----------------------
	*/
	void OnPreSceneLoad() {
		// move any attached sounds back to the sound emitters parent before changing levels so they don't get destroyed
		Debug.Log( "[AudioManager] OnPreSceneLoad cleanup" );
		for ( int i = 0; i < soundEmitters.Length; i++ ) {
			soundEmitters[i].Stop();
			soundEmitters[i].ResetParent( soundEmitterParent.transform );
		}
		// reset our emitter lists
		ResetFreeEmitters();
	}

	/*
	-----------------------
	ResetFreeEmitters()
	-----------------------
	*/
	void ResetFreeEmitters() {
		nextFreeEmitters.Clear();
		playingEmitters.Clear();
		for ( int i = (int)EmitterChannel.Any; i < soundEmitters.Length; i++ ) {
			nextFreeEmitters.Add( soundEmitters[i] );
		}
	}

 	/*
 	-----------------------
 	FadeOutSoundChannel()
 	utility function to fade out a playing sound channel
 	-----------------------
 	*/
 	static public void FadeOutSoundChannel( int channel, float delaySecs, float fadeTime ) {
		theAudioManager.soundEmitters[channel].FadeOutDelayed( delaySecs, fadeTime );
 	}

	/*
	-----------------------
	StopSound()
	-----------------------
	*/
	static public bool StopSound( int idx, bool fadeOut = true, bool stopReserved = false ) {
		if ( !stopReserved && ( idx == (int)EmitterChannel.Reserved ) ) {
			return false;
		}
		if ( !fadeOut ) {
			theAudioManager.soundEmitters[idx].Stop();
		}
		else {
			theAudioManager.soundEmitters[idx].FadeOut( theAudioManager.soundFxFadeSecs );
		}

		return true;
	}

	/*
	-----------------------
	FadeInSound()
	-----------------------
	*/
	public static void FadeInSound( int idx, float fadeTime, float volume ) {
		theAudioManager.soundEmitters[idx].FadeIn( fadeTime, volume );
	}

	/*
	-----------------------
	FadeInSound()
	-----------------------
	*/
	public static void FadeInSound( int idx, float fadeTime ) {
		theAudioManager.soundEmitters[idx].FadeIn( fadeTime );
	}

	/*
	-----------------------
	FadeOutSound()
	-----------------------
	*/
	public static void FadeOutSound( int idx, float fadeTime ) {
		theAudioManager.soundEmitters[idx].FadeOut( fadeTime );
	}

	/*
	-----------------------
	StopAllSounds()
	-----------------------
	*/
	public static void StopAllSounds( bool fadeOut, bool stopReserved = false ) {
		for ( int i = 0; i < theAudioManager.soundEmitters.Length; i++ ) {
			StopSound( i, fadeOut, stopReserved );
		}
	}
	
	/*
	-----------------------
	MuteAllSounds()
	-----------------------
	*/
	public void MuteAllSounds( bool mute, bool muteReserved = false ) {
		for ( int i = 0; i < soundEmitters.Length; i++ ) {
			if ( !muteReserved && ( i == (int)EmitterChannel.Reserved ) ) {
				continue;
			}
			soundEmitters[i].audioSource.mute = true;
		}
	}	

	/*
	-----------------------
	UnMuteAllSounds()
	-----------------------
	*/
	public void UnMuteAllSounds( bool unmute, bool unmuteReserved = false ) {
		for ( int i = 0; i < soundEmitters.Length; i++ ) {
			if ( !unmuteReserved && ( i == (int)EmitterChannel.Reserved ) ) {
				continue;
			}
			if ( soundEmitters[i].audioSource.isPlaying ) {
				soundEmitters[i].audioSource.mute = false;
			}
		}
	}

	/*
	-----------------------
	GetEmitterEndTime()
	-----------------------
	*/
	static public float GetEmitterEndTime( int idx ) {
		return theAudioManager.soundEmitters[idx].endPlayTime;
	}

	/*
	-----------------------
	SetEmitterTime()
	-----------------------
	*/
	static public float SetEmitterTime( int idx, float time ) {
		return theAudioManager.soundEmitters[idx].time = time;
	}

	/*
	-----------------------
	PlaySound()
	-----------------------
	*/
	static public int PlaySound( AudioClip clip, float volume, EmitterChannel src = EmitterChannel.Any, float delay = 0.0f, float pitchVariance = 1.0f, bool loop = false ) {
		if ( !SoundEnabled ) {
			return -1;
		}
		return PlaySoundAt( ( staticListenerPosition != null ) ? staticListenerPosition.position : Vector3.zero, clip, volume, src, delay, pitchVariance, loop );
	}

	/*
	-----------------------
	FindFreeEmitter()
	-----------------------
	*/
	static private int FindFreeEmitter( EmitterChannel src, SoundPriority priority ) {
		// default to the reserved emitter
		SoundEmitter next = theAudioManager.soundEmitters[0];
		if ( src == EmitterChannel.Any ) {
			// pull from the free emitter list if possible
			if ( theAudioManager.nextFreeEmitters.size > 0 ) {
				// return the first in the list
				next = theAudioManager.nextFreeEmitters[0];
				// remove it from the free list
				theAudioManager.nextFreeEmitters.RemoveAtFast( 0 );
			} else {
				// no free emitters available so pull from the lowest priority sound
				if ( priority == SoundPriority.VeryLow ) {
					// skip low priority sounds
					return -1;
				} else {
					// find a playing emitter that has a lower priority than what we're requesting
					// TODO - we could first search for Very Low, then Low, etc ... TBD if it's worth the effort
					next = theAudioManager.playingEmitters.Find( item => item != null && item.priority < priority );
					if ( next == null ) {
						// last chance to find a free emitter
						if ( priority < SoundPriority.Default ) {
							// skip sounds less than the default priority
							if ( theAudioManager.verboseLogging ) {
								Debug.LogWarning( "[AudioManager] skipping sound " + priority );
							}
							return -1;
						} else {
							// grab a default priority emitter so that we don't cannabalize a high priority sound
							next = theAudioManager.playingEmitters.Find( item => item != null && item.priority <= SoundPriority.Default ); ;
						}
					}
					if ( next != null ) {
						if ( theAudioManager.verboseLogging ) {
							Debug.LogWarning( "[AudioManager] cannabalizing " + next.originalIdx + " Time: " + Time.time );
						}
						// remove it from the playing list
						next.Stop();
						theAudioManager.playingEmitters.RemoveFast( next );
					}
				}
			}
		}
		if ( next == null ) {
			Debug.LogError( "[AudioManager] ERROR - absolutely couldn't find a free emitter! Priority = " + priority + " TOO MANY PlaySound* calls!" );
			showPlayingEmitterCount = true;
			return -1;
		}
		return next.originalIdx;
	}

	/*
	-----------------------
	PlaySound()
	-----------------------
	*/
	static public int PlaySound( SoundFX soundFX, EmitterChannel src = EmitterChannel.Any, float delay = 0.0f ) {
		if ( !SoundEnabled ) {
			return -1;
		}
		return PlaySoundAt( ( staticListenerPosition != null ) ? staticListenerPosition.position : Vector3.zero, soundFX, src, delay );
	}

	/*
	-----------------------
	PlaySoundAt()
	-----------------------
	*/
	static public int PlaySoundAt( Vector3 position, SoundFX soundFX, EmitterChannel src = EmitterChannel.Any, float delay = 0.0f, float volumeOverride = 1.0f, float pitchMultiplier = 1.0f ) {
		if ( !SoundEnabled ) {
			return -1;
		}

		AudioClip clip = soundFX.GetClip();
		if ( clip == null ) {
			return -1;
		}

		// check the distance from the local player and ignore sounds out of range
		if ( staticListenerPosition != null ) {
			float distFromListener = ( staticListenerPosition.position - position ).sqrMagnitude;
			if ( distFromListener > theAudioManager.audioMaxFallOffDistanceSqr ) {
				return -1;
			}
			if ( distFromListener > soundFX.MaxFalloffDistSquared ) {
				return -1;
			}
		}

		// check max playing sounds
		if ( soundFX.ReachedGroupPlayLimit() ) {
			if ( theAudioManager.verboseLogging ) {
				Debug.Log( "[AudioManager] PlaySoundAt() with " + soundFX.name + " skipped due to group play limit" );
			}
			return -1;
		}

		int idx = FindFreeEmitter( src, soundFX.priority );
		if ( idx == -1 ) {
			// no free emitters	- should only happen on very low priority sounds
			return -1;
		}
		SoundEmitter emitter = theAudioManager.soundEmitters[idx];

		// make sure to detach the emitter from a previous parent
		emitter.ResetParent( soundEmitterParent.transform );
		emitter.gameObject.SetActive( true );

		// set up the sound emitter
		AudioSource audioSource = emitter.audioSource;
		ONSPAudioSource osp = emitter.osp;

		audioSource.enabled = true;
		audioSource.volume = Mathf.Clamp01( Mathf.Clamp01( theAudioManager.volumeSoundFX * soundFX.volume ) * volumeOverride * soundFX.GroupVolumeOverride );
		audioSource.pitch = soundFX.GetPitch() * pitchMultiplier;
		audioSource.time = 0.0f;
		audioSource.spatialBlend = 1.0f;
		audioSource.rolloffMode = soundFX.falloffCurve;
		if ( soundFX.falloffCurve == AudioRolloffMode.Custom ) {
			audioSource.SetCustomCurve( AudioSourceCurveType.CustomRolloff, soundFX.volumeFalloffCurve );
		}
		audioSource.SetCustomCurve( AudioSourceCurveType.ReverbZoneMix, soundFX.reverbZoneMix );
		audioSource.dopplerLevel = 0;
		audioSource.clip = clip;
		audioSource.spread = soundFX.spread;
		audioSource.loop = soundFX.looping;
		audioSource.mute = false;
		audioSource.minDistance = soundFX.falloffDistance.x;
		audioSource.maxDistance = soundFX.falloffDistance.y;
		audioSource.outputAudioMixerGroup = soundFX.GetMixerGroup( AudioManager.EmitterGroup );
		// set the play time so we can check when sounds are done
		emitter.endPlayTime = Time.time + clip.length + delay;
		// cache the default volume for fading
		emitter.defaultVolume = audioSource.volume;
		// sound priority
		emitter.priority = soundFX.priority;
		// reset this
		emitter.onFinished = null;
		// update the sound group limits
		emitter.SetPlayingSoundGroup( soundFX.Group );
		// add to the playing list
		if ( src == EmitterChannel.Any ) {
			theAudioManager.playingEmitters.AddUnique( emitter );
		}

		// OSP properties
		if ( osp != null ) {
            osp.EnableSpatialization = soundFX.ospProps.enableSpatialization;
			osp.EnableRfl = theAudioManager.enableSpatializedFastOverride || soundFX.ospProps.useFastOverride ? true : false;
			osp.Gain = soundFX.ospProps.gain;
            osp.UseInvSqr = soundFX.ospProps.enableInvSquare;
			osp.Near = soundFX.ospProps.invSquareFalloff.x;
			osp.Far  = soundFX.ospProps.invSquareFalloff.y;
            audioSource.spatialBlend = (soundFX.ospProps.enableSpatialization) ? 1.0f : 0.8f;
            
			// make sure to set the properties in the audio source before playing
            osp.SetParameters(ref audioSource);
		}

		audioSource.transform.position = position;

		if ( theAudioManager.verboseLogging ) {
			Debug.Log( "[AudioManager] PlaySoundAt() channel = " + idx + " soundFX = " + soundFX.name + " volume = " + emitter.volume + " Delay = " + delay + " time = " + Time.time + "\n" );
		}
		
		// play the sound
		if ( delay > 0f ) {
			audioSource.PlayDelayed( delay );
		} else {
			audioSource.Play();
		}
		
		return idx;
	}	

	/*
	-----------------------
	PlayRandomSoundAt()
	-----------------------
	*/
	static public int PlayRandomSoundAt( Vector3 position, AudioClip[] clips, float volume, EmitterChannel src = EmitterChannel.Any, float delay = 0.0f, float pitch = 1.0f, bool loop = false ) {
		if ( ( clips == null ) || ( clips.Length == 0 ) ) {
			return -1;
		}
		int idx = Random.Range( 0, clips.Length );
		return PlaySoundAt( position, clips[idx], volume, src, delay, pitch, loop );
	}
	
	/*
	-----------------------
	PlaySoundAt()
	-----------------------
	*/
	static public int PlaySoundAt( Vector3 position, AudioClip clip, float volume = 1.0f, EmitterChannel src = EmitterChannel.Any, float delay = 0.0f, float pitch = 1.0f, bool loop = false ) {
		if ( !SoundEnabled ) {
			return -1;
		}
		
		if ( clip == null ) {
			return -1;
		}

		// check the distance from the local player and ignore sounds out of range
		if ( staticListenerPosition != null ) {
			if ( ( staticListenerPosition.position - position ).sqrMagnitude > theAudioManager.audioMaxFallOffDistanceSqr ) {
				// no chance of being heard
				return -1;
			}
		}

		int idx = FindFreeEmitter( src, 0 );
		if ( idx == -1 ) {
			// no free emitters	- should only happen on very low priority sounds
			return -1;
		}
		SoundEmitter emitter = theAudioManager.soundEmitters[idx];

		// make sure to detach the emitter from a previous parent
		emitter.ResetParent( soundEmitterParent.transform );
		emitter.gameObject.SetActive( true );

		// set up the sound emitter
		AudioSource audioSource = emitter.audioSource;
		ONSPAudioSource osp = emitter.osp;

		audioSource.enabled = true;
		audioSource.volume = Mathf.Clamp01( theAudioManager.volumeSoundFX * volume );
		audioSource.pitch = pitch;
		audioSource.spatialBlend = 0.8f;
		audioSource.rolloffMode = AudioRolloffMode.Linear;
		audioSource.SetCustomCurve( AudioSourceCurveType.ReverbZoneMix, defaultReverbZoneMix );
		audioSource.dopplerLevel = 0.0f;
		audioSource.clip = clip;
		audioSource.spread = 0.0f;
		audioSource.loop = loop;
		audioSource.mute = false;
		audioSource.minDistance = theAudioManager.audioMinFallOffDistance;
		audioSource.maxDistance = theAudioManager.audioMaxFallOffDistance;
		audioSource.outputAudioMixerGroup = AudioManager.EmitterGroup;
		// set the play time so we can check when sounds are done
		emitter.endPlayTime = Time.time + clip.length + delay;
		// cache the default volume for fading
		emitter.defaultVolume = audioSource.volume;
		// default priority
		emitter.priority = 0;
		// reset this
		emitter.onFinished = null;
		// update the sound group limits
		emitter.SetPlayingSoundGroup( null );
 		// add to the playing list
		if ( src == EmitterChannel.Any ) {
			theAudioManager.playingEmitters.AddUnique( emitter );
		}

		// disable spatialization (by default for regular AudioClips)
		if ( osp != null ) {
			osp.EnableSpatialization = false;
		}

		audioSource.transform.position = position;

		if ( theAudioManager.verboseLogging ) {
			Debug.Log( "[AudioManager] PlaySoundAt() channel = " + idx + " clip = " + clip.name + " volume = " + emitter.volume + " Delay = " + delay + " time = " + Time.time + "\n" );
		}

		// play the sound
		if ( delay > 0f ) {
			audioSource.PlayDelayed( delay );
		} else {
			audioSource.Play();
		}
		
		return idx;
	}

	/*
	-----------------------
	SetOnFinished()
	-----------------------
	*/
	public static void SetOnFinished( int emitterIdx, System.Action onFinished ) {
		if ( ValidateEmitterIndex(emitterIdx) ) {
			theAudioManager.soundEmitters[emitterIdx].SetOnFinished( onFinished );
		}
	}

	/*
	-----------------------
	SetOnFinished()
	-----------------------
	*/
	public static void SetOnFinished( int emitterIdx, System.Action<object> onFinished, object obj ) {
		if ( ValidateEmitterIndex(emitterIdx) ) {
			theAudioManager.soundEmitters[emitterIdx].SetOnFinished( onFinished, obj );
		}
	}

	/*
	-----------------------
	AttachSoundToParent()
	-----------------------
	*/
	public static void AttachSoundToParent( int idx, Transform parent ) {
		if ( theAudioManager.verboseLogging ) {
			string parentName = parent.name;
			if ( parent.parent != null ) {
				parentName = parent.parent.name + "/" + parentName;
			}
			Debug.Log( "[AudioManager] ATTACHING INDEX " + idx + " to " + parentName );
		}
		theAudioManager.soundEmitters[idx].ParentTo( parent );
	}

	/*
	-----------------------
	DetachSoundFromParent()
	-----------------------
	*/
	public static void DetachSoundFromParent( int idx )	{
		if ( theAudioManager.verboseLogging ) {
			Debug.Log( "[AudioManager] DETACHING INDEX " + idx );
		}
		theAudioManager.soundEmitters[idx].DetachFromParent();
	}

	/*
	-----------------------
	DetachSoundsFromParent()
	-----------------------
	*/
	public static void DetachSoundsFromParent( SoundEmitter[] emitters, bool stopSounds = true ) {
		if ( emitters == null ) {
			return;
		}
		foreach ( SoundEmitter emitter in emitters ) {
			if ( emitter.defaultParent != null ) {
				if ( stopSounds ) {
					emitter.Stop();
				}
				emitter.DetachFromParent();
				// make sure it's active
				emitter.gameObject.SetActive( true );
			} else {
				if ( stopSounds ) {
					emitter.Stop();
				}
			}
		}
	}

	/*
	-----------------------
	SetEmitterMixerGroup()
	-----------------------
	*/
	public static void SetEmitterMixerGroup( int idx, AudioMixerGroup mixerGroup ) {
		if ( ( theAudioManager != null ) && ( idx > -1 ) ) {
			theAudioManager.soundEmitters[idx].SetAudioMixer( mixerGroup );
		}	
	}

	/*
	-----------------------
	GetActiveSnapshot()
	-----------------------
	*/
	public static MixerSnapshot GetActiveSnapshot() {
		return ( theAudioManager != null ) ? theAudioManager.currentSnapshot : null;
	}

	/*
	-----------------------
	SetCurrentSnapshot()
	-----------------------
	*/
	public static void SetCurrentSnapshot( MixerSnapshot mixerSnapshot ) {
#if UNITY_EDITOR
		if ( mixerSnapshot == null || mixerSnapshot.snapshot == null ) {
			Debug.LogError( "[AudioManager] ERROR setting empty mixer snapshot!" );
		} else {
			Debug.Log( "[AudioManager] Setting audio mixer snapshot: " + ( ( mixerSnapshot != null && mixerSnapshot.snapshot != null ) ? mixerSnapshot.snapshot.name : "None" ) + " Time: " + Time.time );
		}
#endif
		if ( theAudioManager != null ) {
			if ( ( mixerSnapshot != null ) && ( mixerSnapshot.snapshot != null ) ) {
				mixerSnapshot.snapshot.TransitionTo( mixerSnapshot.transitionTime );
			} else {
				mixerSnapshot = null;
			}
			theAudioManager.currentSnapshot = mixerSnapshot;
		}
	}

	/*
	-----------------------
	BlendWithCurrentSnapshot()
	-----------------------
	*/
	public static void BlendWithCurrentSnapshot( MixerSnapshot blendSnapshot, float weight, float blendTime = 0.0f ) {
		if ( theAudioManager != null ) {
			if ( theAudioManager.audioMixer == null ) {
				Debug.LogWarning( "[AudioManager] can't call BlendWithCurrentSnapshot if the audio mixer is not set!" );	
				return;
			}
			if ( blendTime == 0.0f ) {
				blendTime = Time.deltaTime;
			}
			if ( ( theAudioManager.currentSnapshot != null ) && (theAudioManager.currentSnapshot.snapshot != null ) ) {
				if ( ( blendSnapshot != null ) && ( blendSnapshot.snapshot != null ) ) {
					weight = Mathf.Clamp01( weight );
					if ( weight == 0.0f ) {
						// revert to the default snapshot
						theAudioManager.currentSnapshot.snapshot.TransitionTo( blendTime );
					} else {
						AudioMixerSnapshot[] snapshots = new AudioMixerSnapshot[] { theAudioManager.currentSnapshot.snapshot, blendSnapshot.snapshot };
						float[] weights = new float[] { 1.0f - weight, weight };
						theAudioManager.audioMixer.TransitionToSnapshots( snapshots, weights, blendTime );
					}
				}
			}
		}
	}

}

} // namespace OVR
