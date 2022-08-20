/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Facebook.WitAi.Data.Configuration;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Facebook.WitAi
{
    public class WitAuthUtility
    {
        private static string serverToken;
        public static ITokenValidationProvider tokenValidator = new DefaultTokenValidatorProvider();

        public static bool IsServerTokenValid()
        {
            return tokenValidator.IsServerTokenValid(ServerToken);
        }

        public static bool IsServerTokenValid(string token)
        {
            return tokenValidator.IsServerTokenValid(token);
        }

        public static string GetAppServerToken(WitConfiguration configuration,
            string defaultValue = "")
        {
            return GetAppServerToken(configuration?.application?.id, defaultValue);
        }

        public static string GetAppServerToken(string appId, string defaultServerToken = "")
        {
#if UNITY_EDITOR
            return WitSettingsUtility.GetServerToken(appId, defaultServerToken);
#else
        return "";
#endif
        }

        public static string GetAppId(string serverToken, string defaultAppID = "")
        {
#if UNITY_EDITOR
            return WitSettingsUtility.GetServerTokenAppID(serverToken, defaultAppID);
#else
        return "";
#endif
        }

        public static void SetAppServerToken(string appId, string token)
        {
#if UNITY_EDITOR
            WitSettingsUtility.SetServerToken(appId, token);
#endif
        }

        public const string SERVER_TOKEN_ID = "SharedServerToken";
        public static string ServerToken
        {
#if UNITY_EDITOR
            get
            {
                if (null == serverToken)
                {
                    serverToken = WitSettingsUtility.GetServerToken(SERVER_TOKEN_ID);
                }
                return serverToken;
            }
            set
            {
                serverToken = value;
                WitSettingsUtility.SetServerToken(SERVER_TOKEN_ID, serverToken);
            }
#else
        get => "";
#endif
        }

        public class DefaultTokenValidatorProvider : ITokenValidationProvider
        {
            public bool IsTokenValid(string appId, string token)
            {
                return IsServerTokenValid(token);
            }

            public bool IsServerTokenValid(string serverToken)
            {
                return null != serverToken && serverToken.Length == 32;
            }
        }

        public interface ITokenValidationProvider
        {
            bool IsTokenValid(string appId, string token);
            bool IsServerTokenValid(string serverToken);
        }
    }
}
