using UnityEditor;
using UnityEngine;

namespace OVR
{

/*
-----------------------

 MinMaxPropertyDrawer

-----------------------
*/
[CustomPropertyDrawer (typeof (MinMaxAttribute))]
public class MinMaxPropertyDrawer : PropertyDrawer {

	// Provide easy access to the MinMaxAttribute for reading information from it.
	MinMaxAttribute minMax { get { return ((MinMaxAttribute)attribute); } }

	/*
	-----------------------
	GetPropertyHeight()
	-----------------------
	*/
	public override float GetPropertyHeight( SerializedProperty prop, GUIContent label ) {
		return base.GetPropertyHeight( prop, label ) * 2f;
	}
	
	/*
	-----------------------
	OnGUI()
	-----------------------
	*/
	public override void OnGUI( Rect position, SerializedProperty property, GUIContent label ) {
		Rect sliderPosition = EditorGUI.PrefixLabel( position, label );
		SerializedProperty min = property.FindPropertyRelative( "x" );
		SerializedProperty max = property.FindPropertyRelative( "y" );

		// draw the range and the reset button first so that the slider doesn't grab all the input
		Rect rangePosition = sliderPosition;
		rangePosition.y += rangePosition.height * 0.5f;
		rangePosition.height *= 0.5f;
		Rect contentPosition = rangePosition;
		EditorGUI.indentLevel = 0;
		EditorGUIUtility.labelWidth = 30f;
		contentPosition.width *= 0.3f;
		EditorGUI.PropertyField(contentPosition, min, new GUIContent( "Min" ) );
		contentPosition.x += contentPosition.width + 20f;
		EditorGUI.PropertyField( contentPosition, max, new GUIContent( "Max" ) );
		contentPosition.x += contentPosition.width + 20f;
		contentPosition.width = 50.0f;
		if ( GUI.Button( contentPosition, "Reset" ) ) {
			min.floatValue = minMax.minDefaultVal;
			max.floatValue = minMax.maxDefaultVal;
		}
		float minValue = min.floatValue;
		float maxValue = max.floatValue;
#if UNITY_2017_1_OR_NEWER
		EditorGUI.MinMaxSlider( sliderPosition, GUIContent.none, ref minValue, ref maxValue, minMax.min, minMax.max );
#else
        EditorGUI.MinMaxSlider( GUIContent.none, sliderPosition, ref minValue, ref maxValue, minMax.min, minMax.max );
#endif
        // round to readable values
        min.floatValue = Mathf.Round( minValue / 0.01f ) * 0.01f;
		max.floatValue = Mathf.Round( maxValue / 0.01f ) * 0.01f;
	}

}

} // namespace OVR
