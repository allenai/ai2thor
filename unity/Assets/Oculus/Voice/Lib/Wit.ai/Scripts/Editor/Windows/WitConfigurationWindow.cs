/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;
using Facebook.WitAi.Data.Configuration;

namespace Facebook.WitAi.Windows
{
    public abstract class WitConfigurationWindow : BaseWitWindow
    {
        // Configuration data
        protected int witConfigIndex = -1;
        protected WitConfiguration witConfiguration;

        protected override string HeaderUrl
        {
            get
            {
                string appID = WitConfigurationUtility.GetAppID(witConfiguration);
                if (!string.IsNullOrEmpty(appID))
                {
                    return WitTexts.GetAppURL(appID, HeaderEndpointType);
                }
                return base.HeaderUrl;
            }
        }
        protected virtual WitTexts.WitAppEndpointType HeaderEndpointType => WitTexts.WitAppEndpointType.Settings;
        protected virtual void SetConfiguration(int newConfigIndex)
        {
            witConfigIndex = newConfigIndex;
            WitConfiguration[] witConfigs = WitConfigurationUtility.WitConfigs;
            witConfiguration = witConfigs != null && witConfigIndex >= 0 && witConfigIndex < witConfigs.Length ? witConfigs[witConfigIndex] : null;
        }
        public virtual void SetConfiguration(WitConfiguration newConfiguration)
        {
            int newConfigIndex = newConfiguration == null ? -1 : Array.IndexOf(WitConfigurationUtility.WitConfigs, newConfiguration);
            if (newConfigIndex != -1)
            {
                SetConfiguration(newConfigIndex);
            }
        }
        protected override void LayoutContent()
        {
            // Reload if config is removed
            if (witConfiguration == null && witConfigIndex != -1)
            {
                WitConfigurationUtility.ReloadConfigurationData();
                SetConfiguration(-1);
            }

            // Layout popup
            int index = witConfigIndex;
            WitConfigurationEditorUI.LayoutConfigurationSelect(ref index);
            GUILayout.Space(WitStyles.ButtonMargin);
            // Selection changed
            if (index != witConfigIndex)
            {
                SetConfiguration(index);
            }
        }
    }
}
