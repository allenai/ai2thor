using UnityEditorInternal;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEditor.XR.Interaction.Toolkit
{
    /// <summary>
    /// Custom editor for an <see cref="InteractionLayerSettings"/>.
    /// </summary>
    [CustomEditor(typeof(InteractionLayerSettings))]
    class InteractionLayerSettingsEditor : Editor
    {
        /// <summary>
        /// Class that holds gui content values used by <see cref="InteractionLayerSettingsEditor"/>.
        /// </summary>
        static class Contents
        {
            public static readonly string userLayerLabelText = "User Layer {0}";
            public static readonly string builtinLayerLabelText = "Builtin Layer {0}";
            
            public static readonly GUIContent interactionLayers = EditorGUIUtility.TrTextContent("Interaction Layers");
        }

        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> <see cref="InteractionLayerSettings"/><c>.m_LayerNames</c>.</summary>
        SerializedProperty m_Layers;
        /// <summary>A gui list used to display the layers.</summary>
        ReorderableList m_LayersList;

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        /// <seealso cref="Editor"/>
        void OnEnable()
        {
            m_Layers = serializedObject.FindProperty("m_LayerNames");
            
            if (m_LayersList == null)
            {
                m_LayersList = new ReorderableList(serializedObject, m_Layers, false, false, false, false)
                {
                    drawElementCallback = DrawLayerListElement,
                    elementHeight = EditorGUIUtility.singleLineHeight + 2,
                    headerHeight = 3,
                };
            }
        }
        
        /// <summary>
        /// Called by the layers list do draw a layer element.
        /// </summary>
        /// <param name="rect">The rect used to draw the element.</param>
        /// <param name="index">The element index.</param>
        /// <param name="selected">Whether the element is selected.</param>
        /// <param name="focused">Whether the element is focused.</param>
        void DrawLayerListElement(Rect rect, int index, bool selected, bool focused)
        {
            rect.yMin += 1;
            rect.yMax -= 1;

            var isUserLayer = index >= InteractionLayerSettings.k_BuiltInLayerSize;

            var oldEnabled = GUI.enabled;
            GUI.enabled = isUserLayer;

            var label = EditorGUIUtility.TrTextContent(isUserLayer
                ? string.Format(Contents.userLayerLabelText, index)
                : string.Format(Contents.builtinLayerLabelText, index));
            var oldName = m_Layers.GetArrayElementAtIndex(index).stringValue;
            var newName = EditorGUI.TextField(rect, label, oldName);

            if (newName != oldName)
                m_Layers.GetArrayElementAtIndex(index).stringValue = newName;

            GUI.enabled = oldEnabled;
        }

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            m_Layers.isExpanded = EditorGUILayout.Foldout(m_Layers.isExpanded, Contents.interactionLayers, true);
            if (m_Layers.isExpanded)
            {
                m_LayersList.DoLayoutList();
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}
