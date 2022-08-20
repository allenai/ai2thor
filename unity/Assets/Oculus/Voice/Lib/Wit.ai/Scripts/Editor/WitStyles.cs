/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Facebook.WitAi
{
    public static class WitStyles
    {
        // Window Layout Data
        public const float WindowMinWidth = 0f;
        public const float WindowMaxWidth = 450f;
        public const float WindowMinHeight = 400f;
        public const float WindowMaxSize = 5000f;
        public const float WindowPaddingTop = 8f;
        public const float WindowPaddingBottom = 8f;
        public const float WindowPaddingLeft = 8f;
        public const float WindowPaddingRight = 8f;
        public const float WindowScrollBarSize = 15f;
        // Spacing
        public const float HeaderWidth = 350f;
        public const float HeaderPaddingBottom = 8f;
        public const float WizardFieldPadding = 16f;
        // Text padding
        public const float ButtonMargin = 5f;

        // Icons
        public static GUIContent PasteIcon;
        public static GUIContent EditIcon;
        public static GUIContent ResetIcon;
        public static GUIContent AcceptIcon;
        public static GUIContent ObjectPickerIcon;
        public static GUIContent HelpIcon;
        // Label Styles
        public static GUIStyle Label;
        public static GUIStyle LabelWrap;
        public static GUIStyle LabelError;
        public static GUIStyle LabelHeader;
        public static GUIStyle LabelSubheader;
        public static GUIStyle LabelStatus;
        public static GUIStyle LabelStatusBackground;

        // Button styles
        public static GUIStyle TextButton;
        private const float TextButtonHeight = 25f;
        public const float TextButtonPadding = 5f;
        public static GUIStyle IconButton;
        public const float IconButtonSize = 16f; // Width & Height
        public static GUIStyle TabButton;
        private const float TabButtonHeight = 40f;
        public static GUIStyle HeaderButton;
        public static Color HeaderTextColor = new Color(0.09f, 0.47f, 0.95f); // FB
        // Wit link color
        public static string WitLinkColor = "#ccccff"; // "blue" if not pro
        public const string WitLinkKey = "[COLOR]";

        // Text Field Styles
        public static GUIStyle TextField;
        public static GUIStyle IntField;
        public static GUIStyle PasswordField;
        // Foldout Style
        public static GUIStyle Foldout;
        // Toggle Style
        public static GUIStyle Toggle;
        // Popup/Dropdown Styles
        public static GUIStyle Popup;
        // Texture
        public static Texture2D TextureBlack25P;

        // Init
        static WitStyles()
        {
            // Setup icons
            PasteIcon = EditorGUIUtility.IconContent("Clipboard");
            EditIcon = EditorGUIUtility.IconContent("editicon.sml");
            ResetIcon = EditorGUIUtility.IconContent("TreeEditor.Trash");
            AcceptIcon = EditorGUIUtility.IconContent("FilterSelectedOnly");
            ObjectPickerIcon = EditorGUIUtility.IconContent("d_Record Off");
            HelpIcon = EditorGUIUtility.IconContent("_Help");

            // Label Styles
            Label = new GUIStyle();
            Label.fontSize = 11;
            Label.padding = new RectOffset(5, 5, 0, 0);
            Label.margin = new RectOffset(5, 5, 0, 0);
            Label.alignment = TextAnchor.MiddleLeft;
            Label.normal.textColor = Color.white;
            Label.hover.textColor = Color.white;
            Label.active.textColor = Color.white;
            Label.richText = true;
            Label.wordWrap = false;
            LabelWrap = new GUIStyle(Label);
            LabelWrap.wordWrap = true;
            LabelSubheader = new GUIStyle(Label);
            LabelSubheader.fontSize = 14;
            LabelHeader = new GUIStyle(Label);
            LabelHeader.fontSize = 24;
            LabelHeader.padding = new RectOffset(0, 0, 10, 10);
            LabelHeader.margin = new RectOffset(0, 0, 10, 10);
            LabelHeader.wordWrap = true;
            LabelError = new GUIStyle(Label);
            LabelError.wordWrap = true;
            LabelError.normal.textColor = Color.red;
            LabelStatus = new GUIStyle(Label);
            TextureBlack25P = new Texture2D(1, 1);
            TextureBlack25P.SetPixel(0, 0, new Color(0, 0, 0, .25f));
            TextureBlack25P.Apply();
            LabelStatusBackground = new GUIStyle();
            LabelStatusBackground.normal.background = TextureBlack25P;
            LabelStatus.normal.background = TextureBlack25P;
            LabelStatus.wordWrap = true;
            LabelStatus.fontSize++;
            LabelStatus.alignment = TextAnchor.LowerLeft;
            LabelStatus.margin = new RectOffset(0, 0, 0, 0);
            LabelStatus.wordWrap = false;
            LabelStatus.fontSize = 10;
            // Set to blue if not pro
            if (!EditorGUIUtility.isProSkin)
            {
                WitLinkColor = "blue";
            }

            // Button Styles
            TextButton = new GUIStyle(EditorStyles.miniButton);
            TextButton.alignment = TextAnchor.MiddleCenter;
            TextButton.fixedHeight = TextButtonHeight;
            TabButton = new GUIStyle(TextButton);
            TabButton.fixedHeight = TabButtonHeight;
            IconButton = new GUIStyle(Label);
            IconButton.margin = new RectOffset(0, 0, 0, 0);
            IconButton.padding = new RectOffset(0, 0, 0, 0);
            IconButton.fixedWidth = IconButtonSize;
            IconButton.fixedHeight = IconButtonSize;
            HeaderButton = new GUIStyle(Label);
            HeaderButton.normal.textColor = HeaderTextColor;

            // Text Field Styles
            TextField = new GUIStyle(EditorStyles.textField);
            TextField.padding = Label.padding;
            TextField.margin = Label.margin;
            TextField.alignment = Label.alignment;
            TextField.clipping = TextClipping.Clip;
            PasswordField = new GUIStyle(TextField);
            IntField = new GUIStyle(TextField);
            // Miscellaneous
            Foldout = new GUIStyle(EditorStyles.foldout);
            Toggle = new GUIStyle(EditorStyles.toggle);
            Popup = new GUIStyle(EditorStyles.popup);
        }
    }
}
