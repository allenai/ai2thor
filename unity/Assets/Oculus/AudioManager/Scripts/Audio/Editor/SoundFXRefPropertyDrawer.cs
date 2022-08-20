using UnityEngine;
using UnityEditor;
using System.Collections;

namespace OVR
{

/*
-----------------------

SoundFXRefPropertyDrawer

-----------------------
*/
[CustomPropertyDrawer(typeof(SoundFXRef))]
public class SoundFXRefPropertyDrawer : PropertyDrawer {

	static private GUIStyle disabledStyle = null;

	/*
	-----------------------
	OnGUI()
	-----------------------
	*/
	public override void OnGUI( Rect position, SerializedProperty prop, GUIContent label ) {
		int idx = 0;
		Rect buttonPosition = position;
		buttonPosition.x = position.x + position.width - 40f;
		buttonPosition.width = 20f;
		position.width = buttonPosition.x - position.x - 2f;
		SerializedProperty nameProp = prop.FindPropertyRelative( "soundFXName" );
		if ( AudioManager.GetGameObject() == null ) {
			if ( disabledStyle == null ) {
				disabledStyle = new GUIStyle();
				disabledStyle.normal.textColor = Color.gray;
			}
			EditorGUI.LabelField(position, label.text, nameProp.stringValue, disabledStyle );
		}
		else {
			string[] soundFXNames = AudioManager.GetSoundFXNames( nameProp.stringValue, out idx );
		
			idx = EditorGUI.Popup( position, label.text, idx, soundFXNames );
			nameProp.stringValue = AudioManager.NameMinusGroup( soundFXNames[idx] );
			// play button
			if ( GUI.Button( buttonPosition, "\u25BA" ) ) {
				if ( AudioManager.IsSoundPlaying( nameProp.stringValue ) ) {
					AudioManager.StopSound( nameProp.stringValue );
				} else {
					AudioManager.PlaySound( nameProp.stringValue );
				}
			}
			buttonPosition.x += 22.0f;
			// select audio manager
			if ( GUI.Button( buttonPosition, "\u2630" ) ) { 
				Selection.activeGameObject = AudioManager.GetGameObject();
			}

		}
	}
}

} // namespace OVR
