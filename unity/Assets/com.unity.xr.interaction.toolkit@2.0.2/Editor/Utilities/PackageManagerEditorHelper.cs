using System;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.XR.Interaction.Toolkit.Utilities
{
    /// <summary>
    /// Helper utility class for Inspector <see cref="Editor"/> classes to warn about a package dependency.
    /// </summary>
    [MovedFrom("UnityEngine.XR.Interaction.Toolkit.Utilities")]
    public class PackageManagerEditorHelper
    {
        static class Contents
        {
            public static GUIContent installNow { get; } = EditorGUIUtility.TrTextContent("Install Now");
            public static GUIContent installationInProgress { get; } = EditorGUIUtility.TrTextContent("Installation in progress...");
        }

        static PackageManagerEditorHelper s_ARFoundationHelper;
        /// <summary>
        /// Shared helper for the <c>com.unity.xr.arfoundation</c> package.
        /// </summary>
        public static PackageManagerEditorHelper inputSystemHelper =>
            s_ARFoundationHelper ?? (s_ARFoundationHelper = new PackageManagerEditorHelper("com.unity.xr.arfoundation"));

        readonly string m_PackageIdentifier;

        readonly GUIContent m_DependencyMessage;

        AddRequest m_AddRequest;

        /// <summary>
        /// Creates a new <see cref="PackageManagerEditorHelper"/> to use for a package.
        /// </summary>
        /// <param name="packageIdentifier">A string representing the package to be added.</param>
        public PackageManagerEditorHelper(string packageIdentifier)
        {
            if (string.IsNullOrEmpty(packageIdentifier))
                throw new ArgumentException($"Package identifier cannot be null or empty.", nameof(packageIdentifier));

            m_PackageIdentifier = packageIdentifier;
            m_DependencyMessage = EditorGUIUtility.TrTextContent($"This component has a dependency on {m_PackageIdentifier}");
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        public void Reset()
        {
            m_AddRequest = null;
        }

        /// <summary>
        /// Draws a help box with a warning that the component has a dependency,
        /// and a button to install the package dependency.
        /// </summary>
        public void DrawDependencyHelpBox()
        {
            EditorGUI.BeginDisabledGroup(m_AddRequest != null && !m_AddRequest.IsCompleted);
            if (HelpBoxWithButton(m_DependencyMessage, Contents.installNow))
            {
                m_AddRequest = Client.Add(m_PackageIdentifier);
            }
            EditorGUI.EndDisabledGroup();

            if (m_AddRequest != null)
            {
                if (m_AddRequest.Error != null)
                {
                    EditorGUILayout.LabelField(EditorGUIUtility.TrTextContent($"Installation error: {m_AddRequest.Error.errorCode}: {m_AddRequest.Error.message}"), EditorStyles.miniLabel);
                }
                else if(!m_AddRequest.IsCompleted)
                {
                    EditorGUILayout.LabelField(Contents.installationInProgress, EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField(EditorGUIUtility.TrTextContent($"Installation status: {m_AddRequest.Status}"), EditorStyles.miniLabel);
                }
            }
        }

        /// <summary>
        /// Make a help box with a message and button.
        /// </summary>
        /// <param name="messageContent">The message text.</param>
        /// <param name="buttonContent">The button text.</param>
        /// <returns>Returns <see langword="true"/> if button was pressed. Otherwise, returns <see langword="false"/>.</returns>
        /// <seealso cref="MaterialEditor.HelpBoxWithButton"/>
        static bool HelpBoxWithButton(GUIContent messageContent, GUIContent buttonContent)
        {
            const float kButtonWidth = 90f;
            const float kSpacing = 5f;
            const float kButtonHeight = 20f;

            // Reserve size of wrapped text
            var contentRect = GUILayoutUtility.GetRect(messageContent, EditorStyles.helpBox);
            // Reserve size of button
            GUILayoutUtility.GetRect(1, kButtonHeight + kSpacing);

            // Render background box with text at full height
            contentRect.height += kButtonHeight + kSpacing;
            GUI.Label(contentRect, messageContent, EditorStyles.helpBox);

            // Button (align lower right)
            var buttonRect = new Rect(contentRect.xMax - kButtonWidth - 4f, contentRect.yMax - kButtonHeight - 4f, kButtonWidth, kButtonHeight);
            return GUI.Button(buttonRect, buttonContent);
        }
    }
}
