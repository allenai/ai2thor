using UnityEngine;
using System.Collections;

namespace OVR
{

/*
-----------------------
SoundFXRef
just a references to a SoundFX.. all the SoundFX methods are called indirectly from here
-----------------------
*/
[System.Serializable]
public class SoundFXRef {

	public string			soundFXName = string.Empty;

	private bool			initialized = false;
	private SoundFX			soundFXCached = null;
	public SoundFX			soundFX { 
		get {
			if ( !initialized ) {
				Init();
			}
			return soundFXCached;
		}
	}
	public string name { get { return soundFXName; } set { soundFXName = value; Init(); } }

	/*
	-----------------------
	Init()
	-----------------------
	*/
	void Init() {
		// look up the actual SoundFX object
		soundFXCached = AudioManager.FindSoundFX( soundFXName );
		if ( soundFXCached == null ) {
			soundFXCached = AudioManager.FindSoundFX( string.Empty );
		}
		initialized = true;
	}

	/*
	-----------------------
	Length()
	-----------------------
	*/
	public int 	Length { get { return soundFX.Length; } }

	/*
	-----------------------
	IsValid()
	-----------------------
	*/
	public bool IsValid { get { return soundFX.IsValid; } }

	/*
	-----------------------
	GetClip()
	-----------------------
	*/
	public AudioClip GetClip() {	
		return soundFX.GetClip(); 
	}

	/*
	-----------------------
	GetClipLength()
	-----------------------
	*/
	public float GetClipLength( int idx ) {
		return soundFX.GetClipLength( idx );
	}

	/*
	-----------------------
	PlaySound()
	-----------------------
	*/
	public int PlaySound( float delaySecs = 0.0f ) {
		return soundFX.PlaySound( delaySecs );
	}
	
	/*
	-----------------------
	PlaySoundAt()
	-----------------------
	*/
	public int PlaySoundAt( Vector3 pos, float delaySecs = 0.0f, float volume = 1.0f, float pitchMultiplier = 1.0f ) {
		return soundFX.PlaySoundAt( pos, delaySecs, volume, pitchMultiplier );
	}

	/*
	-----------------------
	SetOnFinished()
	get a callback when the sound is finished playing
	-----------------------
	*/
	public void SetOnFinished( System.Action onFinished ) {
		soundFX.SetOnFinished( onFinished );
	}

	/*
	-----------------------
	SetOnFinished()
	get a callback with an object parameter when the sound is finished playing
	-----------------------
	*/
	public void SetOnFinished( System.Action<object> onFinished, object obj ) {
		soundFX.SetOnFinished( onFinished, obj );
	}

	/*
	-----------------------
	StopSound()
	-----------------------
	*/
	public bool StopSound() {
		return soundFX.StopSound();
	}

	/*
	-----------------------
	AttachToParent()
	-----------------------
	*/
	public void AttachToParent( Transform parent)
	{
		soundFX.AttachToParent( parent);
	}

	/*
	-----------------------
	DetachFromParent()
	-----------------------
	*/
	public void DetachFromParent()
	{
		soundFX.DetachFromParent();
	}
}

} // namespace OVR
