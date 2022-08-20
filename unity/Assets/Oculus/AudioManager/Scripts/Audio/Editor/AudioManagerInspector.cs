using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace OVR
{

/*
-----------------------
 
 AudioManagerInspector

-----------------------
*/
[CustomEditor(typeof(AudioManager))]
public class AudioManagerInspector : Editor {

	private AudioManager 	audioManager = null;
	private string			dragDropIdentifier = "MoveSoundFX";
	private GUIStyle        customDividerStyle = null;

	/*
	-----------------------
	OnInspectorGUI()
	-----------------------
	*/
	public override void OnInspectorGUI() {

		audioManager = target as AudioManager;

		Event e = Event.current;

		// draw the default properties
		DrawDefaultProperties();

		// draw the categories section
		DrawCategories( e );

		serializedObject.Update();

		// draw the sound effects for the selected category
		DrawSoundEffects( e );

		serializedObject.ApplyModifiedProperties();

		CreateStyles();
	}

	/*
	-----------------------
	MarkDirty()
	-----------------------
	*/
	void MarkDirty() {
		serializedObject.SetIsDifferentCacheDirty();
		EditorUtility.SetDirty( audioManager );
	}

	static private int selectedGroup = 0;
	private int nextGroup = -1;
	private int editGroup = -1;
	private FastList<SoundGroup> 	soundGroups = new FastList<SoundGroup>();
	private FastList<ItemRect> 		groups = new FastList<ItemRect>();
	private Rect 					dropArea = new Rect();
	private bool                    addSound = false;
	private int                     deleteSoundIdx = -1;
	private int                     dupeSoundIdx = -1;
	private bool					sortSounds = false;
	private bool					moveQueued = false;
	private int						origGroup = -1;
	private int						origIndex = -1;
	private int						moveToGroup = -1;

	/*
	-----------------------
	DrawDefaultProperties()
	-----------------------
	*/
	void DrawDefaultProperties() {

		BeginContents();
		if ( DrawHeader( "Default Properties", true ) ) {
			EditorGUILayout.BeginVertical( GUI.skin.box );
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "makePersistent" ), new GUIContent( "Don't Destroy on Load" ) );
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "enableSpatializedAudio" ), new GUIContent( "Enable Spatialized Audio" ) );
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "enableSpatializedFastOverride" ), new GUIContent( "Force Disable Reflections" ) );
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "audioMixer" ), new GUIContent( "Master Audio Mixer" ) );
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "defaultMixerGroup" ), new GUIContent( "Pooled Emitter Mixer Group" ) );
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "reservedMixerGroup" ), new GUIContent( "Reserved Emitter Mixer Group" ) );
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "voiceChatMixerGroup" ), new GUIContent( "Voice Chat Mixer Group" ) );
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "verboseLogging" ), new GUIContent( "Verbose Logging" ) );
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "maxSoundEmitters" ), new GUIContent( "Max Sound Emitters" ) );
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "volumeSoundFX" ), new GUIContent( "Default Volume" ) );
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "soundFxFadeSecs" ), new GUIContent( "Sound FX Fade Secs" ) );
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "audioMinFallOffDistance" ), new GUIContent( "Minimum Falloff Distance" ) );
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "audioMaxFallOffDistance" ), new GUIContent( "Maximum Falloff Distance" ) );
			EditorGUILayout.EndVertical();
			serializedObject.ApplyModifiedProperties();
		}
		EndContents();
	}

	/*
	-----------------------
	DrawSoundGroupProperties()
	-----------------------
	*/
	void DrawSoundGroupProperties() {
		if ( selectedGroup == -1 ) {
			return;
		}

		SerializedProperty soundGroupsArray = serializedObject.FindProperty( "soundGroupings" );
		if ( selectedGroup >= soundGroupsArray.arraySize ) {
			return;
		}
		SerializedProperty soundGroup = soundGroupsArray.GetArrayElementAtIndex( selectedGroup );
		string soundGroupName = soundGroup.FindPropertyRelative( "name" ).stringValue;
		if ( DrawHeader( string.Format( "{0} Properties", soundGroupName ), true ) ) {
			EditorGUILayout.BeginVertical( GUI.skin.box );
			EditorGUILayout.PropertyField( soundGroup.FindPropertyRelative( "mixerGroup" ), new GUIContent( "Override Mixer Group", "Leave empty to use the Audio Manager's default mixer group" ) );
			if ( !Application.isPlaying ) {
				EditorGUILayout.PropertyField( soundGroup.FindPropertyRelative( "maxPlayingSounds" ), new GUIContent( "Max Playing Sounds Limit", "Max playing sounds for this sound group, 0 = no limit" ) );
			} else {
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PropertyField( soundGroup.FindPropertyRelative( "maxPlayingSounds" ), new GUIContent( "Max Playing Sounds Limit", "Max playing sounds for this sound group, 0 = no limit" ) );
				// cast to the actual object
				int playingSounds = soundGroup.FindPropertyRelative( "playingSoundCount" ).intValue;
				EditorGUILayout.LabelField( string.Format( "Playing: {0}", playingSounds ), GUILayout.Width( 80.0f ) );
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.PropertyField( soundGroup.FindPropertyRelative( "preloadAudio" ), new GUIContent( "Preload Audio Clips", "Default = No special preloading, Preload = Audio clips are set to 'Preload', Manual Preload = Audio clips are set to not 'Preload'" ) );
			EditorGUILayout.PropertyField( soundGroup.FindPropertyRelative( "volumeOverride" ), new GUIContent( "Volume Override", "All sounds played in this group will have volume scaled by this amount" ) );
			if ( soundGroup.FindPropertyRelative( "volumeOverride" ).floatValue == 0.0f ) {
				EditorGUILayout.HelpBox( "With a volumeOverride of 0.0, these sounds will not play!", MessageType.Warning );
			}
			EditorGUILayout.EndVertical();
			serializedObject.ApplyModifiedProperties();
		}
	}

	/*
	-----------------------
	DrawCategories()
	-----------------------
	*/
	void DrawCategories( Event e ) {

		// do any housework before we start drawing
		if ( moveQueued ) {
			// make a temp copy
			List<SoundFX> origSoundList = new List<SoundFX>( audioManager.soundGroupings[origGroup].soundList );
			SoundFX temp = origSoundList[origIndex];
			List<SoundFX> moveToSoundList = new List<SoundFX>( audioManager.soundGroupings[moveToGroup].soundList );
			// add it to the move to group
			moveToSoundList.Add( temp );
			audioManager.soundGroupings[moveToGroup].soundList = moveToSoundList.ToArray();
			// and finally, remove it from the original group
			origSoundList.RemoveAt( origIndex );
			audioManager.soundGroupings[origGroup].soundList = origSoundList.ToArray();
			Debug.Log( "> Moved '" + temp.name + "' from " + "'" + audioManager.soundGroupings[origGroup].name + "' to '" + audioManager.soundGroupings[moveToGroup].name );
			MarkDirty();
			moveQueued = false;
		}
		// switch to the next group
		if ( nextGroup > -1 ) {
			selectedGroup = nextGroup;
			nextGroup = -1;
		}
		// add a sound
		if ( addSound ) {
			List<SoundFX> soundList = new List<SoundFX>( audioManager.soundGroupings[selectedGroup].soundList );
			SoundFX soundFX = new SoundFX();
			soundFX.name = audioManager.soundGroupings[selectedGroup].name.ToLower() + "_new_unnamed_sound_fx";
			soundList.Add( soundFX );
			audioManager.soundGroupings[selectedGroup].soundList = soundList.ToArray();
			MarkDirty();
			addSound = false;
		}
		// sort the sounds
		if ( sortSounds ) {
			List<SoundFX> soundList = new List<SoundFX>( audioManager.soundGroupings[selectedGroup].soundList );
			soundList.Sort( delegate ( SoundFX sfx1, SoundFX sfx2 ) { return string.Compare( sfx1.name, sfx2.name ); } );
			audioManager.soundGroupings[selectedGroup].soundList = soundList.ToArray();
			MarkDirty();
			sortSounds = false;
		}
		// delete a sound
		if ( deleteSoundIdx > -1 ) {
			List<SoundFX> soundList = new List<SoundFX>( audioManager.soundGroupings[selectedGroup].soundList );
			soundList.RemoveAt( deleteSoundIdx );
			audioManager.soundGroupings[selectedGroup].soundList = soundList.ToArray();
			MarkDirty();
			deleteSoundIdx = -1;
		}
		// duplicate a sound
		if ( dupeSoundIdx > -1 ) {
			List<SoundFX> soundList = new List<SoundFX>( audioManager.soundGroupings[selectedGroup].soundList );
			SoundFX origSoundFX = soundList[dupeSoundIdx];
			// clone this soundFX
			string json = JsonUtility.ToJson( origSoundFX );
			SoundFX soundFX = JsonUtility.FromJson<SoundFX>( json );
			soundFX.name += "_duplicated";
			soundList.Insert( dupeSoundIdx + 1, soundFX );
			audioManager.soundGroupings[selectedGroup].soundList = soundList.ToArray();
			MarkDirty();
			dupeSoundIdx = -1;
		}

		if ( e.type == EventType.Repaint ) {
			groups.Clear();
		}

		GUILayout.Space( 6f );
		
		Color defaultColor = GUI.contentColor;
		BeginContents();

		if ( DrawHeader( "Sound FX Groups", true ) ) {
			EditorGUILayout.BeginVertical( GUI.skin.box );
			soundGroups.Clear();
			for ( int i = 0; i < audioManager.soundGroupings.Length; i++ ) {
				soundGroups.Add( audioManager.soundGroupings[i] );
			}
			for ( int i = 0; i < soundGroups.size; i++ ) {
				EditorGUILayout.BeginHorizontal();
				{
					if ( i == selectedGroup ) {
						GUI.contentColor = ( i == editGroup ) ? Color.white : Color.yellow;
					} else {
						GUI.contentColor = defaultColor;
					}
					if ( ( e.type == EventType.KeyDown ) && ( ( e.keyCode == KeyCode.Return ) || ( e.keyCode == KeyCode.KeypadEnter ) ) ) {
						// toggle editing
						if ( editGroup >= 0 ) {
							editGroup = -1;
						}
						Event.current.Use();
					}
					if ( i == editGroup ) {
						soundGroups[i].name = GUILayout.TextField( soundGroups[i].name, GUILayout.MinWidth( Screen.width - 80f ) );
					} else {
						GUILayout.Label( soundGroups[i].name, ( i == selectedGroup ) ? EditorStyles.whiteLabel : EditorStyles.label, GUILayout.ExpandWidth( true ) );
					}
					GUILayout.FlexibleSpace();
					if ( GUILayout.Button( GUIContent.none, "OL Minus", GUILayout.Width(17f) ) ) {	// minus button
						if ( EditorUtility.DisplayDialog( "Delete '" + soundGroups[i].name + "'", "Are you sure you want to delete the selected sound group?", "Continue", "Cancel" ) ) {
							soundGroups.RemoveAt( i );
							MarkDirty();
						}
					}
				}
				EditorGUILayout.EndHorizontal();
				// build a list of items
				Rect lastRect = GUILayoutUtility.GetLastRect();
				if ( e.type == EventType.Repaint ) {
					groups.Add ( new ItemRect( i, lastRect, null ) );
				}
				if ( ( e.type == EventType.MouseDown ) && lastRect.Contains( e.mousePosition ) ) {
					if ( ( i != selectedGroup ) || ( e.clickCount == 2 ) ) {
						nextGroup = i;
						if ( e.clickCount == 2 ) {
							editGroup = i;
						} else if ( editGroup != nextGroup ) {
							editGroup = -1;
						}
						Repaint();
					}
				}
			}
			// add the final plus button
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if ( GUILayout.Button( GUIContent.none, "OL Plus", GUILayout.Width(17f) ) ) {	// plus button
				soundGroups.Add( new SoundGroup( "unnamed sound group" ) );
				selectedGroup = editGroup = soundGroups.size - 1;
				MarkDirty();
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();

			// reset the color
			GUI.contentColor = defaultColor;

			// the sort and import buttons
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if ( GUILayout.Button( "Sort", GUILayout.Width( 70f ) ) ) {
				soundGroups.Sort( delegate( SoundGroup sg1, SoundGroup sg2 ) { return string.Compare( sg1.name, sg2.name ); } );
				MarkDirty();
			}
			EditorGUILayout.EndHorizontal();

			// draw a rect around the selected item
			if ( ( selectedGroup >= 0 ) && ( selectedGroup < groups.size ) ) {
				EditorGUI.DrawRect( groups[selectedGroup].rect, new Color( 1f, 1f, 1f, 0.06f ) );      
			}

			// finally move the sound groups back into the audio manager
			if ( soundGroups.size > 0 ) {
				audioManager.soundGroupings = soundGroups.ToArray();
			}

			// calculate the drop area rect
			if ( ( e.type == EventType.Repaint ) && ( groups.size > 0 ) ) {
				dropArea.x = groups[0].rect.x;
				dropArea.y = groups[0].rect.y;
				dropArea.width = groups[0].rect.width;
				dropArea.height = ( groups[groups.size-1].rect.y - groups[0].rect.y ) + groups[groups.size-1].rect.height;
			}
		}
		// draw the sound group properties now
		DrawSoundGroupProperties();

		EndContents();

		EditorGUILayout.HelpBox("Create and delete sound groups by clicking + and - respectively.  Double click to rename sound groups.  Drag and drop sounds from below to the groups above to move them.", MessageType.Info);

	}

	public class CustomDragData{
		public int originalGroupIndex;
		public int originalIndex;
		public SerializedProperty originalProperty;
	}

	public class ItemRect {
		public ItemRect( int index, Rect rect, SerializedProperty prop ) {
			this.index = index;
			this.rect = rect;
			this.prop = prop;
		}																	   
		public int					index;
		public Rect 				rect;
		public SerializedProperty	prop;
	}

	private FastList<ItemRect> 	items = new FastList<ItemRect>();

	/*
	-----------------------
	CreateStyles()
	-----------------------
	*/
	void CreateStyles() {
		if ( customDividerStyle == null ) {
			customDividerStyle = new GUIStyle( EditorStyles.label );
			customDividerStyle.normal.background = MakeTex( 4, 4, new Color( 0.5f, 0.5f, 0.5f, 0.25f ) );
			customDividerStyle.margin.right -= 16;
		}
	}

	/*
	-----------------------
	MakeTex()
	-----------------------
	*/
	private Texture2D MakeTex( int width, int height, Color col ) {
		Color[] pix = new Color[width*height];

		for ( int i = 0; i < pix.Length; i++ )
			pix[i] = col;

		Texture2D result = new Texture2D(width, height);
		result.SetPixels( pix );
		result.Apply();

		return result;
	}

	/*
	-----------------------
	DrawSoundEffects()
	-----------------------
	*/
	void DrawSoundEffects( Event e ) {
		if ( ( selectedGroup < 0 ) || ( audioManager.soundGroupings.Length == 0 ) || ( selectedGroup >= audioManager.soundGroupings.Length ) ) {
			return;
		}

		if ( e.type == EventType.Repaint ) {
			items.Clear();
		} else {
			CheckStartDrag( e );
		}

		BeginContents();
		if ( DrawHeader( "Sound Effects", true ) ) {
			GUILayout.Space(3f);
			GUILayout.BeginVertical( GUI.skin.box );

			SerializedProperty soundGroupsArray = serializedObject.FindProperty( "soundGroupings" );
			SerializedProperty soundGroup = soundGroupsArray.GetArrayElementAtIndex( selectedGroup );
			SerializedProperty soundList = soundGroup.FindPropertyRelative( "soundList" );

			CreateStyles();

			Rect prevRect = new Rect();
			if ( soundList.arraySize > 0 ) {
				// show all the sounds
				for ( int i = 0; i < soundList.arraySize; i++ ) {
					EditorGUI.indentLevel = 1;
					SerializedProperty soundFX = soundList.GetArrayElementAtIndex( i );
					SerializedProperty visToggle = soundFX.FindPropertyRelative( "visibilityToggle"  );
					EditorGUILayout.BeginHorizontal( customDividerStyle );
					{
						string soundFXName = soundFX.FindPropertyRelative( "name" ).stringValue;
						// save the visibility state
						visToggle.boolValue = EditorGUILayout.Foldout( visToggle.boolValue, soundFXName );

						// play button
						if ( GUILayout.Button( "\u25BA", GUILayout.Width( 17f ), GUILayout.Height( 16f ) ) ) {
							if ( AudioManager.IsSoundPlaying( soundFXName ) ) {
								AudioManager.StopSound( soundFXName );
							} else {
								AudioManager.PlaySound( soundFXName );
							}
						}
					}
					EditorGUILayout.EndHorizontal();
					if ( visToggle.boolValue ) {
						EditorGUILayout.PropertyField( soundFX, true );
						EditorGUILayout.BeginHorizontal();
						GUILayout.FlexibleSpace();
						if ( GUILayout.Button( "Delete FX", GUILayout.Width( Screen.width / 3.0f ) ) ) {
							if ( EditorUtility.DisplayDialog( "Delete " + soundFX.displayName, "Are you sure?", "Yes", "No!" ) ) {
								deleteSoundIdx = i;
							}
						}
						if ( GUILayout.Button( "Duplicate FX", GUILayout.Width( Screen.width / 3.0f ) ) ) {
							dupeSoundIdx = i;
						}
						GUILayout.FlexibleSpace();
						EditorGUILayout.EndHorizontal();
						GUILayout.Space( 10.0f );
					}
					if ( e.type == EventType.Repaint ) {
						// GetLastRect() is now returning the last rect drawn in the property drawer,
						// not the rect used for the entire SoundFX
						Rect curRect = prevRect;
						curRect.y = prevRect.y + EditorGUIUtility.singleLineHeight;
						Rect lastRect = GUILayoutUtility.GetLastRect();
						curRect.height = ( lastRect.y + lastRect.height ) - curRect.y;
						curRect.width = Screen.width;
						items.Add( new ItemRect( i, curRect, soundFX ) );
					}
					prevRect = GUILayoutUtility.GetLastRect();
				}
			} else {
				EditorGUILayout.LabelField( " " );
			}
			GUILayout.EndVertical();
			GUILayout.Space(3f);
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if ( GUILayout.Button( "Add FX", GUILayout.Width( 70f ) ) ) {
				//soundList.InsertArrayElementAtIndex( soundList.arraySize );
				//MarkDirty();
				addSound = true;
			}
			if ( GUILayout.Button( "Sort", GUILayout.Width( 70f ) ) ) {
				sortSounds = true;
			}
			EditorGUILayout.EndHorizontal();

		}
		EndContents();

		UpdateDrag( e );

	}

	/*
	-----------------------
	CheckStartDrag()
	-----------------------
	*/
	void CheckStartDrag( Event e ) {

		if ( ( e.type == EventType.MouseDrag ) && ( e.button == 0 ) ) {
			for ( int i = 0; i < items.size; i++ ) {
				if ( items[i].rect.Contains( e.mousePosition ) ) {
					DragAndDrop.PrepareStartDrag();// reset data
					
					CustomDragData dragData = new CustomDragData();
					dragData.originalGroupIndex = selectedGroup;
					dragData.originalIndex = items[i].index;
					dragData.originalProperty = items[i].prop;

					DragAndDrop.SetGenericData( dragDropIdentifier, dragData );
					
					DragAndDrop.objectReferences = new Object[0];

					DragAndDrop.StartDrag( dragData.originalProperty.FindPropertyRelative( "name" ).stringValue );
					e.Use();
				}
			}
		}
	}

	/*
	-----------------------
	FindGroupIndex()
	-----------------------
	*/
	int FindGroupIndex( Event e ) {
		for ( int i = 0; i < groups.size; i++ ) {
			if ( groups[i].rect.Contains( e.mousePosition ) ) {
				return i;
			}
		}
		return -1;
	}

	/*
	-----------------------
	UpdateDrag()
	-----------------------
	*/
	void UpdateDrag( Event e ) {

		CustomDragData dragData = DragAndDrop.GetGenericData( dragDropIdentifier ) as CustomDragData;
		if ( dragData == null ) {
			return;
		}

		int groupIndex = FindGroupIndex( e );

		switch ( e.type ) {
		case EventType.DragUpdated:
			if ( ( groupIndex >= 0 ) && ( groupIndex != selectedGroup ) ) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Move;
			} else {
				DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
			}
			e.Use();
			break;     
		case EventType.Repaint:
			if ( ( DragAndDrop.visualMode == DragAndDropVisualMode.None ) ||
				 ( DragAndDrop.visualMode == DragAndDropVisualMode.Rejected ) ) {
				break;
			}
			if ( groupIndex >= 0 && groupIndex < groups.size ) {
				EditorGUI.DrawRect( groups[groupIndex].rect, new Color( 0f, 1f, 0f, 0.1f ) );      
			}
			break;
		case EventType.DragPerform:
			DragAndDrop.AcceptDrag();
			// queue the sound FX move
			QueueSoundFXMove( dragData.originalGroupIndex, dragData.originalIndex, groupIndex );
			e.Use();
			break;
		case EventType.MouseUp:
			// in case MouseDrag never occurred:
			DragAndDrop.PrepareStartDrag();
			break;
		}
	}

	/*
	-----------------------
	QueueSoundFXMove()
	-----------------------
	*/
	void QueueSoundFXMove( int origGroupIndex, int origSoundIndex, int newGroupIndex ) {
		moveQueued = true;
		origGroup = origGroupIndex;
		origIndex = origSoundIndex;
		moveToGroup = newGroupIndex;
	}

	/*
	-----------------------
	DrawHeader()
	-----------------------
	*/
	static public bool DrawHeader (string text) { return DrawHeader(text, text, false); }
	static public bool DrawHeader (string text, string key) { return DrawHeader(text, key, false); }
	static public bool DrawHeader (string text, bool forceOn) { return DrawHeader(text, text, forceOn); }
	static public bool DrawHeader( string text, string key, bool forceOn ) {
		bool state = EditorPrefs.GetBool(key, true);
		
		GUILayout.Space(3f);
		if (!forceOn && !state) GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f);
		GUILayout.BeginHorizontal();
		GUILayout.Space(3f);
		
		GUI.changed = false;
		text = "<b><size=11>" + text + "</size></b>";
		if (state) text = "\u25BC " + text;
		else text = "\u25B6 " + text;
		if (!GUILayout.Toggle(true, text, "dragtab", GUILayout.MinWidth(20f))) state = !state;
		if (GUI.changed) EditorPrefs.SetBool(key, state);
		
		GUILayout.Space(2f);
		GUILayout.EndHorizontal();
		GUI.backgroundColor = Color.white;
		if (!forceOn && !state) GUILayout.Space(3f);
		return state;
	}
	
	/*
	-----------------------
	BeginContents()
	-----------------------
	*/
	static public void BeginContents() {
		GUILayout.BeginHorizontal();
		GUILayout.Space(4f);
		EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(10f));
		GUILayout.BeginVertical();
		GUILayout.Space(2f);
	}
	
	/*
	-----------------------
	EndContents()
	-----------------------
	*/
	static public void EndContents() {
		GUILayout.Space(3f);
		GUILayout.EndVertical();
		EditorGUILayout.EndHorizontal();
		GUILayout.Space(3f);
		GUILayout.EndHorizontal();
		GUILayout.Space(3f);
	}

}

} // namespace OVR
