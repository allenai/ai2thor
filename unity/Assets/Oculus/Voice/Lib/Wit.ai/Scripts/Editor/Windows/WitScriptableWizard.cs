/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEditor;
using UnityEngine;

namespace Facebook.WitAi.Windows
{
    public abstract class WitScriptableWizard : ScriptableWizard
    {
        protected Vector2 scrollOffset;

        protected virtual Texture2D HeaderIcon => WitTexts.HeaderIcon;
        protected virtual string HeaderUrl => WitTexts.WitUrl;

        protected abstract GUIContent Title { get; }
        protected abstract string ButtonLabel { get; }
        protected virtual string ContentHeaderLabel => Title.text;
        protected abstract string ContentSubheaderLabel { get; }

        protected virtual void OnEnable()
        {
            createButtonName = ButtonLabel;
        }
        protected override bool DrawWizardGUI()
        {
            // Reapply title if needed
            if (titleContent != Title)
            {
                titleContent = Title;
            }

            // Layout window
            Vector2 size = Vector2.zero;
            WitEditorUI.LayoutWindow(ContentHeaderLabel, HeaderIcon, HeaderUrl, LayoutContent, ref scrollOffset, out size);

            // Set wizard to max width
            size.x = WitStyles.WindowMaxWidth;
            // Wizards add additional padding
            size.y += 70f;

            // Clamp wizard sizes
            maxSize = minSize = size;

            // True if valid server token
            return false;
        }
        protected virtual void LayoutContent()
        {
            if (!string.IsNullOrEmpty(ContentSubheaderLabel))
            {
                WitEditorUI.LayoutSubheaderLabel(ContentSubheaderLabel);
                GUILayout.Space(WitStyles.HeaderPaddingBottom * 2f);
            }
            GUILayout.BeginHorizontal();
            GUILayout.Space(WitStyles.WizardFieldPadding);
            GUILayout.BeginVertical();
            LayoutFields();
            GUILayout.EndVertical();
            GUILayout.Space(WitStyles.WizardFieldPadding);
            GUILayout.EndHorizontal();
        }
        protected abstract void LayoutFields();
        protected virtual void OnWizardCreate()
        {

        }
    }
}
