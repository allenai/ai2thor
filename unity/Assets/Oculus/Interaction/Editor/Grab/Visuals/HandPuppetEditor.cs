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

using Oculus.Interaction.Input;
using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;

namespace Oculus.Interaction.HandGrab.Visuals.Editor
{
    [CustomEditor(typeof(HandPuppet))]
    public class HandPuppetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            HandPuppet puppet = target as HandPuppet;
            if (GUILayout.Button("Auto-Assign Bones"))
            {
                SkinnedMeshRenderer skinnedHand = puppet.GetComponentInChildren<SkinnedMeshRenderer>();
                if (skinnedHand != null)
                {
                    SetPrivateValue(puppet, "_jointMaps", AutoAsignBones(skinnedHand));
                }
            }
        }

        private List<HandJointMap> AutoAsignBones(SkinnedMeshRenderer skinnedHand)
        {
            List<HandJointMap> maps = new List<HandJointMap>();
            Transform root = skinnedHand.rootBone;
            Regex regEx = new Regex(@"Hand(\w*)(\d)");
            foreach (var bone in FingersMetadata.HAND_JOINT_IDS)
            {
                Match match = regEx.Match(bone.ToString());
                if (match != Match.Empty)
                {
                    string boneName = match.Groups[1].Value.ToLower();
                    string boneNumber = match.Groups[2].Value;
                    Transform skinnedBone = RecursiveSearchForChildrenContainingPattern(root, "col", boneName, boneNumber);
                    if (skinnedBone != null)
                    {
                        maps.Add(new HandJointMap()
                        {
                            id = bone,
                            transform = skinnedBone,
                            rotationOffset = Vector3.zero
                        });
                    }
                }
            }
            return maps;
        }

        private Transform RecursiveSearchForChildrenContainingPattern(Transform root, string ignorePattern, params string[] args)
        {
            if (root == null)
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                string childName = child.name.ToLower();

                bool shouldCheck = string.IsNullOrEmpty(ignorePattern)|| !childName.Contains(ignorePattern);
                if (shouldCheck)
                {
                    bool containsAllArgs = args.All(a => childName.Contains(a));
                    Transform result = containsAllArgs ? child
                        : RecursiveSearchForChildrenContainingPattern(child, ignorePattern, args);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            return null;
        }

        private static void SetPrivateValue(object instance, string fieldName, object value)
        {
            FieldInfo fieldData = GetPrivateField(instance, fieldName);
            fieldData.SetValue(instance, value);
        }

        private static FieldInfo GetPrivateField(object instance, string fieldName)
        {
            Type type = instance.GetType();
            FieldInfo fieldData = null;
            while (type != null && fieldData == null)
            {
                fieldData = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                type = type.BaseType;
            }
            return fieldData;
        }
    }
}
