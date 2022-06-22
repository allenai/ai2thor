using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEditor.XR.Interaction.Toolkit
{
    /// <summary>
    /// Class configuration to display XR Interaction Toolkit settings in the Project Settings window.
    /// </summary>
    class XRInteractionToolkitSettingsProvider : SettingsProvider
    {
        /// <summary>
        /// Scope that adds a top and a left margin.
        /// </summary>
        class SettingsMarginScope : GUI.Scope
        {
            internal SettingsMarginScope()
            {
                const float topMargin = 10f;
                const float leftMargin = 10f;
                
                GUILayout.BeginHorizontal();
                GUILayout.Space(leftMargin);
                GUILayout.BeginVertical();
                GUILayout.Space(topMargin);
            }

            protected override void CloseScope()
            {
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// Contents of GUI elements used by this settings provider.
        /// </summary>
        protected static class Contents
        {
            public static readonly GUIContent layerMaskUpdater = EditorGUIUtility.TrTextContent("Run Interaction Layer Mask Updater", "Open the dialog box to update the Interaction Layer Mask for projects upgrading from older versions of the XR Interaction Toolkit.");
        }

        /// <summary>
        /// The path to display this settings provider in the Project Settings window.
        /// </summary>
        const string k_Path = "Project/XR Interaction Toolkit";
        
        Editor m_InteractionLayerEditor;
        Editor m_XRInteractionEditorSettingsEditor;
        
        /// <summary>
        /// Create and returns this settings provider.
        /// </summary>
        /// <returns>Returns a new instance of this settings provider.</returns>
        [SettingsProvider]
#pragma warning disable IDE0051 // Remove unused private members -- Invoked by Unity due to attribute
        static SettingsProvider CreateInteractionLayerProvider()
#pragma warning restore IDE0051
        {
            var keywordsList = GetSearchKeywordsFromPath(AssetDatabase.GetAssetPath(InteractionLayerSettings.instance)).ToList();
            return new XRInteractionToolkitSettingsProvider { keywords = keywordsList };
        }

        XRInteractionToolkitSettingsProvider(string path = k_Path, SettingsScope scopes = SettingsScope.Project,
            IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
        }
        
        /// <summary>
        /// Draw the interaction layer updater button.
        /// </summary>
        static void InteractionLayerUpdateButton()
        {
            var oldGuiEnabled = GUI.enabled;
            GUI.enabled = !EditorApplication.isPlayingOrWillChangePlaymode;
            
            if (GUILayout.Button(Contents.layerMaskUpdater, GUILayout.ExpandWidth(false)))
            {
                InteractionLayerUpdater.RunIfUserWantsTo();
                GUIUtility.ExitGUI();
            }

            GUI.enabled = oldGuiEnabled;
        }

        /// <summary>
        /// Draws the XR Interaction Settings editor.
        /// </summary>
        void XRInteractionEditorSettingsEditor()
        {
            if (m_XRInteractionEditorSettingsEditor)
                m_XRInteractionEditorSettingsEditor.OnInspectorGUI();
        }
        
        /// <summary>
        /// Draws the Interaction Layer Settings editor.
        /// </summary>
        void InteractionLayerSettingsEditor()
        {
            if (m_InteractionLayerEditor)
                m_InteractionLayerEditor.OnInspectorGUI();
        }
        
        /// <inheritdoc />
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            
            m_InteractionLayerEditor = Editor.CreateEditor(InteractionLayerSettings.instance);
            m_XRInteractionEditorSettingsEditor = Editor.CreateEditor(XRInteractionEditorSettings.instance);
        }

        /// <inheritdoc />
        public override void OnDeactivate()
        {
            base.OnDeactivate();
            
            if (m_InteractionLayerEditor != null)
                Object.DestroyImmediate(m_InteractionLayerEditor);
            
            if (m_XRInteractionEditorSettingsEditor != null)
                Object.DestroyImmediate(m_XRInteractionEditorSettingsEditor);
        }

        /// <inheritdoc />
        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);

            using (new SettingsMarginScope())
            {
                XRInteractionEditorSettingsEditor();
                InteractionLayerUpdateButton();
                EditorGUILayout.Space();
                InteractionLayerSettingsEditor();
            }
        }
    }
}
