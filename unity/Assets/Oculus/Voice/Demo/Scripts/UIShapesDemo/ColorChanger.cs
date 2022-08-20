/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Facebook.WitAi;
using Facebook.WitAi.Lib;
using UnityEngine;

namespace Oculus.Voice.Demo.UIShapesDemo
{
    public class ColorChanger : MonoBehaviour
    {
        /// <summary>
        /// Sets the color of the specified transform.
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="color"></param>
        private void SetColor(Transform trans, Color color)
        {
            trans.GetComponent<Renderer>().material.color = color;
        }

        /// <summary>
        /// Directly processes a command result getting the slots with WitResult utilities
        /// </summary>
        /// <param name="commandResult">Result data from Wit.ai activation to be processed</param>
        public void UpdateColor(WitResponseNode commandResult)
        {
            string[] colorNames = commandResult.GetAllEntityValues("color:color");
            string[] shapes = commandResult.GetAllEntityValues("shape:shape");
            UpdateColor(colorNames, shapes);
        }

        /// <summary>
        /// Updates the colors of a set of shape, or all colors split across the shapes
        /// </summary>
        /// <param name="colorNames">The names of the colors to be processed</param>
        /// <param name="shapes">The shape names or if empty all shapes</param>
        public void UpdateColor(string[] colorNames, string[] shapes)
        {
            if (shapes.Length != 0 && colorNames.Length != shapes.Length)
            {
                Debug.LogWarning("Mismatched entity pairings.");
                return;
            }
            if (shapes.Length == 0 || shapes[0] == "color"){
                Debug.LogWarning("updating all.");
                UpdateColorAllShapes(colorNames);
                return;
            }

            for(var entity = 0; entity < colorNames.Length; entity++)
            {
                if (!ColorUtility.TryParseHtmlString(colorNames[entity], out var color)) return;

                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    if (String.Equals(shapes[entity], child.name,
                            StringComparison.CurrentCultureIgnoreCase))
                    {
                        SetColor(child, color);
                        break;
                    }
                }
            }
        }
        /// <summary>
        /// Updates the colors of the shapes, with colours split across the shapes
        /// </summary>
        /// <param name="colorNames">The names of the colors to be processed</param>
        public void UpdateColorAllShapes(string[] colorNames)
        {
            var unspecifiedShape = 0;
            for(var entity = 0; entity < colorNames.Length; entity++)
            {
                if (!ColorUtility.TryParseHtmlString(colorNames[entity], out var color)) return;

                var splitLimit = (transform.childCount/colorNames.Length) * (entity+1);
                while (unspecifiedShape < splitLimit)
                {
                    SetColor(transform.GetChild(unspecifiedShape), color);
                    unspecifiedShape++;
                }
            }
        }
    }
}
