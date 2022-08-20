using UnityEngine;
using UnityEditor;

namespace OVR
{

/*
-----------------------
AudioImportPostProcessor()
-----------------------
*/
public class AudioImportPostProcessor : AssetPostprocessor {

	static void OnPostprocessAllAssets( string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths ) {
		AudioManager audioManager = AudioManager.Instance;
		if ( audioManager != null ) {
      // find the asset path to the loaded audio manager prefab
#if UNITY_2018_2_OR_NEWER
      Object prefabObject = PrefabUtility.GetCorrespondingObjectFromSource(audioManager);
#else
      Object prefabObject = PrefabUtility.GetPrefabParent( audioManager );
#endif
      if ( prefabObject != null ) {
				string path = AssetDatabase.GetAssetPath( prefabObject );
				// check to see if the AudioManager prefab has been reimported.
				// if so, rebuild everything
				foreach ( string asset in importedAssets ) {
					if ( asset.ToLower() == path.ToLower() ) {
						// in the event the audio manager is selected, deselect it first before reloading
						Debug.Log( "[AudioManager] AudioManager prefab reloaded: " + path );
						Selection.objects = new Object[0] { };
						// unfortunately even saving the audio manager prefab will trigger this action
						//string msg = "The Audio Manager was reloaded.  If you are going to be making modifications to the Audio Manager, ";
						//msg += "please verify you have the latest version before proceeding.  If in doubt, restart Unity before making modifications.";
						//EditorUtility.DisplayDialog( "Audio Manager Prefab Reloaded", msg, "OK" );
						// do the actual reload
						AudioManager.OnPrefabReimported();
						break;
					}
				}
			}
		}
	}
}

} // namespace OVR
