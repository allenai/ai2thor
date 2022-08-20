using UnityEngine;
using System.Collections;

namespace OVR
{

public class TestScript : MonoBehaviour {

	[InspectorNote( "Sound Setup", "Press '1' to play testSound1 and '2' to play testSound2")]

	public SoundFXRef       testSound1;
	public SoundFXRef       testSound2;

	// Use this for initialization
	void Start () {
	
	}
	

	// Update is called once per frame
	void Update () 
    {
	    // use attached game object location
        if ( Input.GetKeyDown( KeyCode.Alpha1 ) ) 
        {
			testSound1.PlaySoundAt( transform.position );
		}

        // hard code information
		if ( Input.GetKeyDown( KeyCode.Alpha2 ) ) {
			testSound2.PlaySoundAt( new Vector3( 5.0f, 0.0f, 0.0f ) );
		}
	}
}

} // namespace OVR
