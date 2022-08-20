/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Facebook.WitAi.Data.Entities;
using Facebook.WitAi.Data.Intents;
using Facebook.WitAi.Lib;

namespace Facebook.WitAi
{
    public static class WitResultUtilities
    {
        /// <summary>
        /// Gets the string value of the first entity
        /// </summary>
        /// <param name="witResponse"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetFirstEntityValue(this WitResponseNode witResponse, string name)
        {
            return witResponse?["entities"]?[name]?[0]?["value"]?.Value;
        }

        /// <summary>
        /// Gets a collection of string value containing the selected value from
        /// each entity in the response.
        /// </summary>
        /// <param name="witResponse"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string[] GetAllEntityValues(this WitResponseNode witResponse, string name)
        {
            var values = new string[witResponse?["entities"]?[name]?.Count ?? 0];
            for (var i = 0; i < witResponse?["entities"]?[name]?.Count; i++)
            {
                values[i] = witResponse?["entities"]?[name]?[i]?["value"]?.Value;
            }
            return values;
        }

        /// <summary>
        /// Gets the first entity as a WitResponseNode
        /// </summary>
        /// <param name="witResponse"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static WitResponseNode GetFirstEntity(this WitResponseNode witResponse, string name)
        {
            return witResponse?["entities"]?[name][0];
        }

        /// <summary>
        /// Gets the first entity with the given name as string data
        /// </summary>
        /// <param name="witResponse"></param>
        /// <param name="name">The entity name typically something like name:name</param>
        /// <returns></returns>
        public static WitEntityData GetFirstWitEntity(this WitResponseNode witResponse, string name)
        {
            var array = witResponse?["entities"]?[name].AsArray;
            return array?.Count > 0 ? array[0].AsWitEntity : null;
        }

        /// <summary>
        /// Gets The first entity with the given name as int data
        /// </summary>
        /// <param name="witResponse"></param>
        /// <param name="name">The entity name typically something like name:name</param>
        /// <returns></returns>
        public static WitEntityIntData GetFirstWitIntEntity(this WitResponseNode witResponse,
            string name)
        {
            var array = witResponse?["entities"]?[name].AsArray;
            return array?.Count > 0 ? array[0].AsWitIntEntity : null;
        }

        /// <summary>
        /// Gets The first entity with the given name as int data
        /// </summary>
        /// <param name="witResponse"></param>
        /// <param name="name">The entity name typically something like name:name</param>
        /// <returns></returns>
        public static int GetFirstWitIntValue(this WitResponseNode witResponse,
            string name, int defaultValue)
        {
            var array = witResponse?["entities"]?[name].AsArray;

            if (null == array || array.Count == 0) return defaultValue;
            return array[0].AsWitIntEntity.value;
        }

        /// <summary>
        /// Gets the first entity with the given name as float data
        /// </summary>
        /// <param name="witResponse"></param>
        /// <param name="name">The entity name typically something like name:name</param>
        /// <returns></returns>
        public static WitEntityFloatData GetFirstWitFloatEntity(this WitResponseNode witResponse, string name)
        {
            var array = witResponse?["entities"]?[name].AsArray;
            return array?.Count > 0 ? array[0].AsWitFloatEntity : null;
        }

        /// <summary>
        /// Gets The first entity with the given name as int data
        /// </summary>
        /// <param name="witResponse"></param>
        /// <param name="name">The entity name typically something like name:name</param>
        /// <returns></returns>
        public static float GetFirstWitFloatValue(this WitResponseNode witResponse,
            string name, float defaultValue)
        {
            var array = witResponse?["entities"]?[name].AsArray;

            if (null == array || array.Count == 0) return defaultValue;
            return array[0].AsWitFloatEntity.value;
        }

        /// <summary>
        /// Gets the first intent's name
        /// </summary>
        /// <param name="witResponse"></param>
        /// <returns></returns>
        public static string GetIntentName(this WitResponseNode witResponse)
        {
            return witResponse?["intents"]?[0]?["name"]?.Value;
        }

        /// <summary>
        /// Gets the first intent node
        /// </summary>
        /// <param name="witResponse"></param>
        /// <returns></returns>
        public static WitResponseNode GetFirstIntent(this WitResponseNode witResponse)
        {
            return witResponse?["intents"]?[0];
        }

        /// <summary>
        /// Gets the first set of intent data
        /// </summary>
        /// <param name="witResponse"></param>
        /// <returns>WitIntentData or null if no intents are found</returns>
        public static WitIntentData GetFirstIntentData(this WitResponseNode witResponse)
        {
            var array = witResponse?["intents"]?.AsArray;
            return array?.Count > 0 ? array[0].AsWitIntent : null;
        }

        /// <summary>
        /// Gets all intents in the given response
        /// </summary>
        /// <param name="witResponse">The root response node of an VoiceService.events.OnResponse event</param>
        /// <returns></returns>
        public static WitIntentData[] GetIntents(this WitResponseNode witResponse)
        {
            var intentResponseArray = witResponse?["intents"].AsArray;
            var intents = new WitIntentData[intentResponseArray?.Count ?? 0];
            for (int i = 0; i < intents.Length; i++)
            {
                intents[i] = intentResponseArray[i].AsWitIntent;
            }

            return intents;
        }

        /// <summary>
        /// Gets all entities in the given response
        /// </summary>
        /// <param name="witResponse">The root response node of an VoiceService.events.OnResponse event</param>
        /// <returns></returns>
        public static WitEntityData[] GetEntities(this WitResponseNode witResponse, string name)
        {
            var entityJsonArray = witResponse?["entities"]?[name].AsArray;
            var entities = new WitEntityData[entityJsonArray?.Count ?? 0];
            for (int i = 0; i < entities.Length; i++)
            {
                entities[i] = entityJsonArray[i].AsWitEntity;
            }

            return entities;
        }

        /// <summary>
        /// Gets all float entity values in the given response with the specified entity name
        /// </summary>
        /// <param name="witResponse">The root response node of an VoiceService.events.OnResponse event</param>
        /// <param name="name">The entity name typically something like name:name</param>
        /// <returns></returns>
        public static WitEntityFloatData[] GetFloatEntities(this WitResponseNode witResponse, string name)
        {
            var entityJsonArray = witResponse?["entities"]?[name].AsArray;
            var entities = new WitEntityFloatData[entityJsonArray?.Count ?? 0];
            for (int i = 0; i < entities.Length; i++)
            {
                entities[i] = entityJsonArray[i].AsWitFloatEntity;
            }

            return entities;
        }

        /// <summary>
        /// Gets all int entity values in the given response with the specified entity name
        /// </summary>
        /// <param name="witResponse">The root response node of an VoiceService.events.OnResponse event</param>
        /// <param name="name">The entity name typically something like name:name</param>
        /// <returns></returns>
        public static WitEntityIntData[] GetIntEntities(this WitResponseNode witResponse, string name)
        {
            var entityJsonArray = witResponse?["entities"]?[name].AsArray;
            var entities = new WitEntityIntData[entityJsonArray?.Count ?? 0];
            for (int i = 0; i < entities.Length; i++)
            {
                entities[i] = entityJsonArray[i].AsWitIntEntity;
            }

            return entities;
        }

        public static string GetPathValue(this WitResponseNode response, string path)
        {

            string[] nodes = path.Trim('.').Split('.');

            var node = response;

            foreach (var nodeName in nodes)
            {
                string[] arrayElements = SplitArrays(nodeName);

                node = node[arrayElements[0]];
                for (int i = 1; i < arrayElements.Length; i++)
                {
                    node = node[int.Parse(arrayElements[i])];
                }
            }

            return node.Value;
        }

        public static WitResponseReference GetWitResponseReference(string path)
        {

            string[] nodes = path.Trim('.').Split('.');

            var rootNode = new WitResponseReference()
            {
                path = path
            };
            var node = rootNode;

            foreach (var nodeName in nodes)
            {
                string[] arrayElements = SplitArrays(nodeName);

                var childObject = new ObjectNodeReference()
                {
                    path = path
                };
                childObject.key = arrayElements[0];
                node.child = childObject;
                node = childObject;
                for (int i = 1; i < arrayElements.Length; i++)
                {
                    var childIndex = new ArrayNodeReference()
                    {
                        path = path
                    };
                    childIndex.index = int.Parse(arrayElements[i]);
                    node.child = childIndex;
                    node = childIndex;
                }
            }

            return rootNode;
        }

        public static string GetCodeFromPath(string path)
        {
            string[] nodes = path.Trim('.').Split('.');
            string code = "witResponse";
            foreach (var nodeName in nodes)
            {
                string[] arrayElements = SplitArrays(nodeName);

                code += $"[\"{arrayElements[0]}\"]";
                for (int i = 1; i < arrayElements.Length; i++)
                {
                    code += $"[{arrayElements[i]}]";
                }
            }

            code += ".Value";
            return code;
        }

        private static string[] SplitArrays(string nodeName)
        {
            var nodes = nodeName.Split('[');
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = nodes[i].Trim(']');
            }

            return nodes;
        }
    }

    public class WitResponseReference
    {
        public WitResponseReference child;
        public string path;

        public virtual string GetStringValue(WitResponseNode response)
        {
            return child.GetStringValue(response);
        }

        public virtual int GetIntValue(WitResponseNode response)
        {
            return child.GetIntValue(response);
        }

        public virtual float GetFloatValue(WitResponseNode response)
        {
            return child.GetFloatValue(response);
        }
    }

    public class ArrayNodeReference : WitResponseReference
    {
        public int index;

        public override string GetStringValue(WitResponseNode response)
        {
            if (null != child)
            {
                return child.GetStringValue(response[index]);
            }

            return response[index].Value;
        }

        public override int GetIntValue(WitResponseNode response)
        {
            if (null != child)
            {
                return child.GetIntValue(response[index]);
            }

            return response[index].AsInt;
        }

        public override float GetFloatValue(WitResponseNode response)
        {
            if (null != child)
            {
                return child.GetFloatValue(response[index]);
            }

            return response[index].AsInt;
        }
    }

    public class ObjectNodeReference : WitResponseReference
    {
        public string key;

        public override string GetStringValue(WitResponseNode response)
        {
            if (null != child && null != response?[key])
            {
                return child.GetStringValue(response[key]);
            }

            return response?[key]?.Value;
        }

        public override int GetIntValue(WitResponseNode response)
        {
            if (null != child)
            {
                return child.GetIntValue(response[key]);
            }

            return response[key].AsInt;
        }

        public override float GetFloatValue(WitResponseNode response)
        {
            if (null != child)
            {
                return child.GetFloatValue(response[key]);
            }

            return response[key].AsFloat;
        }
    }
}
