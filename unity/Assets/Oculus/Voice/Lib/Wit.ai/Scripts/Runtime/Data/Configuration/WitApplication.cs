/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Lib;
using UnityEngine;

namespace Facebook.WitAi.Data.Configuration
{

    [Serializable]
    public class WitApplication : WitConfigurationData
    {
        [SerializeField] public string name;
        [SerializeField] public string id;
        [SerializeField] public string lang;
        [SerializeField] public bool isPrivate;
        [SerializeField] public string createdAt;

        #if UNITY_EDITOR
        protected override WitRequest OnCreateRequest()
        {
            return witConfiguration.GetAppRequest(id);
        }

        public override void UpdateData(WitResponseNode appWitResponse)
        {
            id = appWitResponse["id"].Value;
            name = appWitResponse["name"].Value;
            lang = appWitResponse["lang"].Value;
            isPrivate = appWitResponse["private"].AsBool;
            createdAt = appWitResponse["created_at"].Value;
        }

        public static WitApplication FromJson(WitResponseNode appWitResponse)
        {
            var app = new WitApplication();
            app.UpdateData(appWitResponse);
            return app;
        }
        #endif
    }
}
