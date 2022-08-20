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

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

using Object = UnityEngine.Object;

namespace Oculus.Interaction.Editor
{
    public class AssetListWindow : EditorWindow
    {
        public IReadOnlyList<string> AssetPaths => _assetPaths;

        private List<string> _assetPaths;
        private Vector2 _scrollPos;

        private Action<AssetListWindow> _headerDrawer;
        private Action<AssetListWindow> _footerDrawer;

        public static AssetListWindow Show(
            string title,
            IEnumerable<string> assetPaths,
            bool modal = false,
            Action<AssetListWindow> headerDrawer = null,
            Action<AssetListWindow> footerDrawer = null)
        {
            AssetListWindow window = GetWindow<AssetListWindow>(true);
            window._assetPaths = new List<string>(assetPaths);
            window.SetTitle(title);
            window.SetHeader(headerDrawer);
            window.SetFooter(footerDrawer);

            if (modal)
            {
                window.ShowModalUtility();
            }
            else
            {
                window.ShowUtility();
            }

            return window;
        }

        public static void CloseAll()
        {
            if (HasOpenInstances<AssetListWindow>())
            {
                AssetListWindow window = GetWindow<AssetListWindow>(true);
                window.Close();
            }
        }

        public void SetTitle(string title)
        {
            titleContent = new GUIContent(title);
        }

        public void SetHeader(Action<AssetListWindow> headerDrawer)
        {
            _headerDrawer = headerDrawer;
        }

        public void SetFooter(Action<AssetListWindow> footerDrawer)
        {
            _footerDrawer = footerDrawer;
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawContent();
            DrawFooter();
        }

        private void DrawHeader()
        {
            if (_headerDrawer == null)
            {
                return;
            }

            EditorGUILayout.BeginVertical();
            _headerDrawer.Invoke(this);
            EditorGUILayout.EndVertical();
        }

        private void DrawFooter()
        {
            if (_footerDrawer == null)
            {
                return;
            }

            EditorGUILayout.BeginVertical();
            _footerDrawer.Invoke(this);
            EditorGUILayout.EndVertical();
        }

        private void DrawContent()
        {
            EditorGUILayout.BeginVertical();
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            foreach (var assetName in _assetPaths)
            {
                var rect = EditorGUILayout.BeginHorizontal();
                if (GUI.Button(rect, "", GUIStyle.none))
                {
                    PingObject(assetName);
                }
                EditorGUILayout.LabelField(assetName);
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void PingObject(string assetPath)
        {
            Object obj = AssetDatabase.LoadAssetAtPath(
                assetPath, typeof(Object));

            if (obj != null)
            {
                EditorGUIUtility.PingObject(obj);
            }
        }
    }
}
