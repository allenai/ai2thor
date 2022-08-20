/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Facebook.WitAi.Data.Configuration;
using UnityEngine;

namespace Facebook.WitAi.Configuration
{
    [Serializable]
    public class WitEndpointConfig
    {
        private static WitEndpointConfig defaultEndpointConfig = new WitEndpointConfig();

        public string uriScheme;
        public string authority;
        public int port;

        public string witApiVersion;

        public string speech;
        public string message;

        public string UriScheme => string.IsNullOrEmpty(uriScheme) ? WitRequest.URI_SCHEME : uriScheme;
        public string Authority =>
            string.IsNullOrEmpty(authority) ? WitRequest.URI_AUTHORITY : authority;
        public int Port => port <= 0 ? WitRequest.URI_DEFAULT_PORT : port;
        public string WitApiVersion => string.IsNullOrEmpty(witApiVersion)
            ? WitRequest.WIT_API_VERSION
            : witApiVersion;

        public string Speech =>
            string.IsNullOrEmpty(speech) ? WitRequest.WIT_ENDPOINT_SPEECH : speech;

        public string Message =>
            string.IsNullOrEmpty(message) ? WitRequest.WIT_ENDPOINT_MESSAGE : message;

        public static WitEndpointConfig GetEndpointConfig(WitConfiguration witConfig)
        {
            return witConfig && null != witConfig.endpointConfiguration
                ? witConfig.endpointConfiguration
                : defaultEndpointConfig;
        }
    }
}
