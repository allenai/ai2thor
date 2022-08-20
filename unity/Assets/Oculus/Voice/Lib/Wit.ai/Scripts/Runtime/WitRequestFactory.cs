/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Text;
using System.Collections.Generic;
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Data.Configuration;
using Facebook.WitAi.Data.Entities;
using Facebook.WitAi.Interfaces;
using Facebook.WitAi.Lib;
using UnityEngine;

namespace Facebook.WitAi
{
    public static class WitRequestFactory
    {
        private static WitRequest.QueryParam QueryParam(string key, string value)
        {
            return new WitRequest.QueryParam() { key = key, value = value };
        }

        private static void HandleWitRequestOptions(WitRequestOptions requestOptions,
            IDynamicEntitiesProvider[] additionalEntityProviders,
            List<WitRequest.QueryParam> queryParams)
        {
            WitResponseClass entities = new WitResponseClass();
            bool hasEntities = false;

            if (null != additionalEntityProviders)
            {
                foreach (var provider in additionalEntityProviders)
                {
                    foreach (var providerEntity in provider.GetDynamicEntities())
                    {
                        hasEntities = true;
                        MergeEntities(entities, providerEntity);
                    }
                }
            }

            if (DynamicEntityKeywordRegistry.HasDynamicEntityRegistry)
            {
                foreach (var providerEntity in DynamicEntityKeywordRegistry.Instance.GetDynamicEntities())
                {
                    hasEntities = true;
                    MergeEntities(entities, providerEntity);
                }
            }

            if (null != requestOptions)
            {
                if (!string.IsNullOrEmpty(requestOptions.tag))
                {
                    queryParams.Add(QueryParam("tag", requestOptions.tag));
                }

                if (null != requestOptions.dynamicEntities)
                {
                    foreach (var entity in requestOptions.dynamicEntities.GetDynamicEntities())
                    {
                        hasEntities = true;
                        MergeEntities(entities, entity);
                    }
                }
            }

            if (hasEntities)
            {
                queryParams.Add(QueryParam("entities", entities.ToString()));
            }
        }

        private static void MergeEntities(WitResponseClass entities, WitDynamicEntity providerEntity)
        {
            if (!entities.HasChild(providerEntity.entity))
            {
                entities[providerEntity.entity] = new WitResponseArray();
            }
            var mergedArray = entities[providerEntity.entity];
            Dictionary<string, WitResponseClass> map = new Dictionary<string, WitResponseClass>();
            HashSet<string> synonyms = new HashSet<string>();
            var existingKeywords = mergedArray.AsArray;
            for (int i = 0; i < existingKeywords.Count; i++)
            {
                var keyword = existingKeywords[i].AsObject;
                var key = keyword["keyword"].Value;
                if(!map.ContainsKey(key))
                {
                    map[key] = keyword;
                }
            }
            foreach (var keyword in providerEntity.keywords)
            {
                if (map.TryGetValue(keyword.keyword, out var keywordObject))
                {
                    foreach (var synonym in keyword.synonyms)
                    {
                        keywordObject["synonyms"].Add(synonym);
                    }
                }
                else
                {
                    keywordObject = keyword.AsJson;
                    map[keyword.keyword] = keywordObject;
                    mergedArray.Add(keywordObject);
                }
            }
        }

        /// <summary>
        /// Creates a message request that will process a query string with NLU
        /// </summary>
        /// <param name="config"></param>
        /// <param name="query">Text string to process with the NLU</param>
        /// <returns></returns>
        public static WitRequest MessageRequest(this WitConfiguration config, string query, WitRequestOptions requestOptions, IDynamicEntitiesProvider[] additionalDynamicEntities = null)
        {
            List<WitRequest.QueryParam> queryParams = new List<WitRequest.QueryParam>
            {
                QueryParam("q", query)
            };

            if (null != requestOptions && -1 != requestOptions.nBestIntents)
            {
                queryParams.Add(QueryParam("n", requestOptions.nBestIntents.ToString()));
            }

            HandleWitRequestOptions(requestOptions, additionalDynamicEntities, queryParams);

            if (null != requestOptions && !string.IsNullOrEmpty(requestOptions.tag))
            {
                queryParams.Add(QueryParam("tag", requestOptions.tag));
            }

            var path = WitEndpointConfig.GetEndpointConfig(config).Message;
            WitRequest request = new WitRequest(config, path, queryParams.ToArray());

            if (null != requestOptions)
            {
                request.onResponse = requestOptions.onResponse;
            }

            return request;
        }

        /// <summary>
        /// Creates a request for nlu processing that includes a data stream for mic data
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static WitRequest SpeechRequest(this WitConfiguration config, WitRequestOptions requestOptions, IDynamicEntitiesProvider[] additionalEntityProviders = null)
        {
            List<WitRequest.QueryParam> queryParams = new List<WitRequest.QueryParam>();

            if (null != requestOptions && -1 != requestOptions.nBestIntents)
            {
                queryParams.Add(QueryParam("n", requestOptions.nBestIntents.ToString()));
            }

            HandleWitRequestOptions(requestOptions, additionalEntityProviders, queryParams);

            var path = WitEndpointConfig.GetEndpointConfig(config).Speech;
            WitRequest request = new WitRequest(config, path, queryParams.ToArray());

            if (null != requestOptions)
            {
                request.onResponse = requestOptions.onResponse;
            }

            return request;
        }

        #region IDE Only Requests
        #if UNITY_EDITOR

        /// <summary>
        /// Requests a list of intents available under this configuration
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static WitRequest ListIntentsRequest(this WitConfiguration config)
        {
            return new WitRequest(config, WitRequest.WIT_ENDPOINT_INTENTS);
        }

        /// <summary>
        /// Requests details on a specific intent
        /// </summary>
        /// <param name="config"></param>
        /// <param name="intentName">The name of the defined intent</param>
        /// <returns></returns>
        public static WitRequest GetIntentRequest(this WitConfiguration config, string intentName)
        {
            return new WitRequest(config, $"{WitRequest.WIT_ENDPOINT_INTENTS}/{intentName}");
        }

        /// <summary>
        /// Requests a list of utterances
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static WitRequest ListUtterancesRequest(this WitConfiguration config)
        {
            return new WitRequest(config, WitRequest.WIT_ENDPOINT_UTTERANCES);
        }

        /// <summary>
        /// Requests a list of available entites
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static WitRequest ListEntitiesRequest(this WitConfiguration config)
        {
            return new WitRequest(config, WitRequest.WIT_ENDPOINT_ENTITIES, true);
        }

        /// <summary>
        /// Requests details of a specific entity
        /// </summary>
        /// <param name="config"></param>
        /// <param name="entityName">The name of the entity as it is defined in wit.ai</param>
        /// <returns></returns>
        public static WitRequest GetEntityRequest(this WitConfiguration config, string entityName)
        {
            return new WitRequest(config, $"{WitRequest.WIT_ENDPOINT_ENTITIES}/{entityName}", true);
        }

        /// <summary>
        /// Requests a list of available traits
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static WitRequest ListTraitsRequest(this WitConfiguration config)
        {
            return new WitRequest(config, WitRequest.WIT_ENDPOINT_TRAITS, true);
        }

        /// <summary>
        /// Requests details of a specific trait
        /// </summary>
        /// <param name="config"></param>
        /// <param name="traitName">The name of the trait as it is defined in wit.ai</param>
        /// <returns></returns>
        public static WitRequest GetTraitRequest(this WitConfiguration config, string traitName)
        {
            return new WitRequest(config, $"{WitRequest.WIT_ENDPOINT_TRAITS}/{traitName}", true);
        }

        /// <summary>
        /// Requests a list of apps available to the account defined in the WitConfiguration
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static WitRequest ListAppsRequest(string serverToken, int limit, int offset = 0)
        {
            return new WitRequest(serverToken, WitRequest.WIT_ENDPOINT_APPS,
                QueryParam("limit", limit.ToString()),
                QueryParam("offset", offset.ToString()));
        }

        /// <summary>
        /// Requests details for a specific application
        /// </summary>
        /// <param name="config"></param>
        /// <param name="appId">The id of the app as it is defined in wit.ai</param>
        /// <returns></returns>
        public static WitRequest GetAppRequest(this WitConfiguration config, string appId)
        {
            return new WitRequest(config, $"{WitRequest.WIT_ENDPOINT_APPS}/{appId}", true);
        }

        /// <summary>
        /// Requests a client token for an application
        /// </summary>
        /// <param name="config"></param>
        /// <param name="appId">The id of the app as it is defined in wit.ai</param>
        /// <param name="refresh">Should the token be refreshed</param>
        /// <returns></returns>
        public static WitRequest GetClientToken(this WitConfiguration config, string appId, bool refresh = false)
        {
            var postString = "{\"refresh\":" + refresh.ToString().ToLower() + "}";
            var postData = Encoding.UTF8.GetBytes(postString);
            var request = new WitRequest(config, $"{WitRequest.WIT_ENDPOINT_APPS}/{appId}/client_tokens", true)
            {
                postContentType = "application/json",
                postData = postData
            };

            return request;
        }
        #endif
        #endregion
    }
}
