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
using Facebook.WitAi.CallbackHandlers;
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Data;
using Facebook.WitAi.Lib;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Facebook.WitAi.Windows
{
    public class WitUnderstandingViewer : WitConfigurationWindow
    {
        [FormerlySerializedAs("witHeader")] [SerializeField] private Texture2D _witHeader;
        [FormerlySerializedAs("responseText")] [SerializeField] private string _responseText;
        private string _utterance;
        private WitResponseNode _response;
        private Dictionary<string, bool> _foldouts;

        // Current service
        private VoiceService[] _services;
        private string[] _serviceNames;
        private int _currentService = -1;
        public VoiceService service => _services != null && _currentService >= 0 && _currentService < _services.Length ? _services[_currentService] : null;
        public bool HasWit => service != null;

        private DateTime _submitStart;
        private TimeSpan _requestLength;
        private string _status;
        private int _responseCode;
        private WitRequest _request;
        private int _savePopup;
        private GUIStyle _hamburgerButton;


        class Content
        {
            public static GUIContent CopyPath;
            public static GUIContent CopyCode;
            public static GUIContent CreateStringValue;
            public static GUIContent CreateIntValue;
            public static GUIContent CreateFloatValue;

            static Content()
            {
                CreateStringValue = new GUIContent("Create Value Reference/Create String");
                CreateIntValue = new GUIContent("Create Value Reference/Create Int");
                CreateFloatValue = new GUIContent("Create Value Reference/Create Float");

                CopyPath = new GUIContent("Copy Path to Clipboard");
                CopyCode = new GUIContent("Copy Code to Clipboard");
            }
        }

        protected override GUIContent Title => WitTexts.UnderstandingTitleContent;
        protected override WitTexts.WitAppEndpointType HeaderEndpointType => WitTexts.WitAppEndpointType.Understanding;

        protected override void OnEnable()
        {
            base.OnEnable();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            RefreshVoiceServices();
            if (!string.IsNullOrEmpty(_responseText))
            {
                _response = WitResponseNode.Parse(_responseText);
            }
            _status = WitTexts.Texts.UnderstandingViewerPromptLabel;
        }

        protected override void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && !HasWit)
            {
                RefreshVoiceServices();
            }
        }

        private void OnSelectionChange()
        {
            if (Selection.activeGameObject)
            {
                SetVoiceService(Selection.activeGameObject.GetComponent<VoiceService>());
            }
        }

        private void ResetStartTime()
        {
            _submitStart = System.DateTime.Now;
        }

        private void OnError(string title, string message)
        {
            _status = message;
        }

        private void OnRequestCreated(WitRequest request)
        {
            this._request = request;
            ResetStartTime();
        }

        private void ShowTranscription(string transcription)
        {
            _utterance = transcription;
            Repaint();
        }

        // On gui
        protected override void OnGUI()
        {
            base.OnGUI();
            EditorGUILayout.BeginHorizontal();
            WitEditorUI.LayoutStatusLabel(_status);
            GUILayout.BeginVertical(GUILayout.Width(24));
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            var rect = GUILayoutUtility.GetLastRect();

            if (null == _hamburgerButton)
            {
                // GUI.skin must be called from OnGUI
                _hamburgerButton = new GUIStyle(GUI.skin.GetStyle("PaneOptions"));
                _hamburgerButton.imagePosition = ImagePosition.ImageOnly;
            }

            var value = EditorGUILayout.Popup(-1, new string[] {"Save", "Copy to Clipboard"}, _hamburgerButton, GUILayout.Width(24));
            if (-1 != value)
            {
                if (value == 0)
                {
                    var path = EditorUtility.SaveFilePanel("Save Response Json", Application.dataPath,
                        "result", "json");
                    if (!string.IsNullOrEmpty(path))
                    {
                        File.WriteAllText(path, _response.ToString());
                    }
                }
                else
                {
                    EditorGUIUtility.systemCopyBuffer = _response.ToString();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        protected override void LayoutContent()
        {
            // Get service
            VoiceService voiceService = null;

            // Runtime Mode
            if (Application.isPlaying)
            {
                // Refresh services
                if (_services == null)
                {
                    RefreshVoiceServices();
                }
                // Services missing
                if (_services == null || _serviceNames == null || _services.Length == 0)
                {
                    WitEditorUI.LayoutErrorLabel(WitTexts.Texts.UnderstandingViewerMissingServicesLabel);
                    return;
                }
                // Voice service select
                int newService = _currentService;
                bool serviceUpdate = false;
                GUILayout.BeginHorizontal();
                // Clamp
                if (newService < 0 || newService >= _services.Length)
                {
                    newService = 0;
                    serviceUpdate = true;
                }
                // Layout
                WitEditorUI.LayoutPopup(WitTexts.Texts.UnderstandingViewerServicesLabel, _serviceNames, ref newService, ref serviceUpdate);
                // Update
                if (serviceUpdate)
                {
                    SetVoiceService(newService);
                }
                // Refresh
                if (WitEditorUI.LayoutTextButton(WitTexts.Texts.ConfigurationRefreshButtonLabel))
                {
                    RefreshVoiceServices();
                }
                GUILayout.EndHorizontal();
                // Ensure service exists
                voiceService = service;
            }
            // Editor Only
            else
            {
                // Configuration select
                base.LayoutContent();
                // Ensure configuration exists
                if (!witConfiguration)
                {
                    WitEditorUI.LayoutErrorLabel(WitTexts.Texts.UnderstandingViewerMissingConfigLabel);
                    return;
                }
                // Check client access token
                string clientAccessToken = witConfiguration.clientAccessToken;
                if (string.IsNullOrEmpty(clientAccessToken))
                {
                    WitEditorUI.LayoutErrorLabel(WitTexts.Texts.UnderstandingViewerMissingClientTokenLabel);
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (WitEditorUI.LayoutTextButton(WitTexts.Texts.UnderstandingViewerSettingsButtonLabel))
                    {
                        Selection.activeObject = witConfiguration;
                    }
                    GUILayout.EndHorizontal();
                    return;
                }
            }

            // Determine if input is allowed
            bool allowInput = !Application.isPlaying || (service != null && !service.Active);
            GUI.enabled = allowInput;

            // Utterance field
            bool updated = false;
            WitEditorUI.LayoutTextField(new GUIContent(WitTexts.Texts.UnderstandingViewerUtteranceLabel), ref _utterance, ref updated);

            // Begin Buttons
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // Submit utterance
            if (allowInput && WitEditorUI.LayoutTextButton(WitTexts.Texts.UnderstandingViewerSubmitButtonLabel))
            {
                _responseText = "";
                if (!string.IsNullOrEmpty(_utterance))
                {
                    SubmitUtterance();
                }
                else
                {
                    _response = null;
                }
            }

            // Service buttons
            GUI.enabled = true;
            if (EditorApplication.isPlaying && voiceService)
            {
                if (!voiceService.Active)
                {
                    // Activate
                    if (WitEditorUI.LayoutTextButton(WitTexts.Texts.UnderstandingViewerActivateButtonLabel))
                    {
                        voiceService.Activate();
                    }
                }
                else
                {
                    // Deactivate
                    if (WitEditorUI.LayoutTextButton(WitTexts.Texts.UnderstandingViewerDeactivateButtonLabel))
                    {
                        voiceService.Deactivate();
                    }
                    // Abort
                    if (WitEditorUI.LayoutTextButton(WitTexts.Texts.UnderstandingViewerAbortButtonLabel))
                    {
                        voiceService.DeactivateAndAbortRequest();
                    }
                }
            }
            GUILayout.EndHorizontal();

            // Results
            GUILayout.BeginVertical(EditorStyles.helpBox);
            if (voiceService && voiceService.MicActive)
            {
                WitEditorUI.LayoutWrapLabel(WitTexts.Texts.UnderstandingViewerListeningLabel);
            }
            else if (voiceService && voiceService.IsRequestActive)
            {
                WitEditorUI.LayoutWrapLabel(WitTexts.Texts.UnderstandingViewerLoadingLabel);
            }
            else if (_response != null)
            {
                DrawResponse();
            }
            else if (string.IsNullOrEmpty(_responseText))
            {
                WitEditorUI.LayoutWrapLabel(WitTexts.Texts.UnderstandingViewerPromptLabel);
            }
            else
            {
                WitEditorUI.LayoutWrapLabel(_responseText);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        private void SubmitUtterance()
        {
            // Remove response
            _response = null;

            if (Application.isPlaying)
            {
                if (service)
                {
                    _status = WitTexts.Texts.UnderstandingViewerListeningLabel;
                    _responseText = _status;
                    service.Activate(_utterance);
                    // Hack to watch for loading to complete. Response does not
                    // come back on the main thread so Repaint in onResponse in
                    // the editor does nothing.
                    EditorApplication.update += WatchForWitResponse;
                }
            }
            else
            {
                _status = WitTexts.Texts.UnderstandingViewerLoadingLabel;
                _responseText = _status;
                _submitStart = System.DateTime.Now;
                _request = witConfiguration.MessageRequest(_utterance, new WitRequestOptions());
                _request.onResponse = OnResponse;
                _request.Request();
            }
        }

        private void WatchForWitResponse()
        {
            if (service && !service.Active)
            {
                Repaint();
                EditorApplication.update -= WatchForWitResponse;
            }
        }

        private void OnResponse(WitRequest request)
        {
            _responseCode = request.StatusCode;
            if (null != request.ResponseData)
            {
                ShowResponse(request.ResponseData);
            }
            else if (!string.IsNullOrEmpty(request.StatusDescription))
            {
                _responseText = request.StatusDescription;
            }
            else
            {
                _responseText = "No response. Status: " + request.StatusCode;
            }
        }

        private void ShowResponse(WitResponseNode r)
        {
            _response = r;
            _responseText = _response.ToString();
            _requestLength = DateTime.Now - _submitStart;
            _status = $"Response time: {_requestLength}";
        }

        private void DrawResponse()
        {
            DrawResponseNode(_response);
        }

        private void DrawResponseNode(WitResponseNode witResponseNode, string path = "")
        {
            if (null == witResponseNode?.AsObject) return;

            if(string.IsNullOrEmpty(path)) DrawNode(witResponseNode["text"], "text", path);

            var names = witResponseNode.AsObject.ChildNodeNames;
            Array.Sort(names);
            foreach (string child in names)
            {
                if (!(string.IsNullOrEmpty(path) && child == "text"))
                {
                    var childNode = witResponseNode[child];
                    DrawNode(childNode, child, path);
                }
            }
        }

        private void DrawNode(WitResponseNode childNode, string child, string path, bool isArrayElement = false)
        {
            if (childNode == null)
            {
                return;
            }
            string childPath;

            if (path.Length > 0)
            {
                childPath = isArrayElement ? $"{path}[{child}]" : $"{path}.{child}";
            }
            else
            {
                childPath = child;
            }

            if (!string.IsNullOrEmpty(childNode.Value))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(15 * EditorGUI.indentLevel);
                if (GUILayout.Button($"{child} = {childNode.Value}", "Label"))
                {
                    ShowNodeMenu(childNode, childPath);
                }

                GUILayout.EndHorizontal();
            }
            else
            {
                var childObject = childNode.AsObject;
                var childArray = childNode.AsArray;

                if ((null != childObject || null != childArray) && Foldout(childPath, child))
                {
                    EditorGUI.indentLevel++;
                    if (null != childObject)
                    {
                        DrawResponseNode(childNode, childPath);
                    }

                    if (null != childArray)
                    {
                        DrawArray(childArray, childPath);
                    }

                    EditorGUI.indentLevel--;
                }
            }
        }

        private void ShowNodeMenu(WitResponseNode node, string path)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(Content.CreateStringValue, false, () => WitDataCreation.CreateStringValue(path));
            menu.AddItem(Content.CreateIntValue, false, () => WitDataCreation.CreateIntValue(path));
            menu.AddItem(Content.CreateFloatValue, false, () => WitDataCreation.CreateFloatValue(path));
            menu.AddSeparator("");
            menu.AddItem(Content.CopyPath, false, () =>
            {
                EditorGUIUtility.systemCopyBuffer = path;
            });
            menu.AddItem(Content.CopyCode, false, () =>
            {
                EditorGUIUtility.systemCopyBuffer = WitResultUtilities.GetCodeFromPath(path);
            });

            if (Selection.activeGameObject)
            {
                menu.AddSeparator("");

                var label =
                    new GUIContent($"Add response matcher to {Selection.activeObject.name}");

                menu.AddItem(label, false, () =>
                {
                    var valueHandler = Selection.activeGameObject.AddComponent<WitResponseMatcher>();
                    valueHandler.intent = _response.GetIntentName();
                    valueHandler.valueMatchers = new ValuePathMatcher[]
                    {
                        new ValuePathMatcher() { path = path }
                    };
                });

                AddMultiValueUpdateItems(path, menu);
            }

            menu.ShowAsContext();
        }

        private void AddMultiValueUpdateItems(string path, GenericMenu menu)
        {

            string name = path;
            int index = path.LastIndexOf('.');
            if (index > 0)
            {
                name = name.Substring(index + 1);
            }

            var mvhs = Selection.activeGameObject.GetComponents<WitResponseMatcher>();
            if (mvhs.Length > 1)
            {
                for (int i = 0; i < mvhs.Length; i++)
                {
                    var handler = mvhs[i];
                    menu.AddItem(
                        new GUIContent($"Add {name} matcher to {Selection.activeGameObject.name}/Handler {(i + 1)}"),
                        false, (h) => AddNewEventHandlerPath((WitResponseMatcher) h, path), handler);
                }
            }
            else if (mvhs.Length == 1)
            {
                var handler = mvhs[0];
                menu.AddItem(
                    new GUIContent($"Add {name} matcher to {Selection.activeGameObject.name}'s Response Matcher"),
                    false, (h) => AddNewEventHandlerPath((WitResponseMatcher) h, path), handler);
            }
        }

        private void AddNewEventHandlerPath(WitResponseMatcher handler, string path)
        {
            Array.Resize(ref handler.valueMatchers, handler.valueMatchers.Length + 1);
            handler.valueMatchers[handler.valueMatchers.Length - 1] = new ValuePathMatcher()
            {
                path = path
            };
        }

        private void DrawArray(WitResponseArray childArray, string childPath)
        {
            for (int i = 0; i < childArray.Count; i++)
            {
                DrawNode(childArray[i], i.ToString(), childPath, true);
            }
        }

        private bool Foldout(string path, string label)
        {
            if (null == _foldouts) _foldouts = new Dictionary<string, bool>();
            if (!_foldouts.TryGetValue(path, out var state))
            {
                state = false;
                _foldouts[path] = state;
            }

            var newState = EditorGUILayout.Foldout(state, label);
            if (newState != state)
            {
                _foldouts[path] = newState;
            }

            return newState;
        }

        #region SERVICES
        // Refresh voice services
        protected void RefreshVoiceServices()
        {
            // Remove previous service
            VoiceService previous = service;
            SetVoiceService(-1);

            // Get all services
            VoiceService[] services = Resources.FindObjectsOfTypeAll<VoiceService>();

            // Get unique services
            List<GameObject> serviceGOs = new List<GameObject>();
            List<VoiceService> serviceList = new List<VoiceService>();
            foreach (var s in services)
            {
                // Add unique gameobjects
                GameObject serviceGO = s.gameObject;
                if (serviceGO.scene.rootCount > 0 && !serviceGOs.Contains(serviceGO))
                {
                    serviceGOs.Add(serviceGO);
                    serviceList.Add(serviceGO.GetComponent<VoiceService>());
                }
            }

            // Get service gameobject names
            _services = serviceList.ToArray();
            _serviceNames = new string[_services.Length];
            for (int i = 0; i < _services.Length; i++)
            {
                _serviceNames[i] = GetVoiceServiceName(_services[i]);
            }

            // Set as first found
            if (previous == null)
            {
                SetVoiceService(0);
            }
            // Set as previous
            else
            {
                SetVoiceService(previous);
            }
        }
        // Get voice service name
        private string GetVoiceServiceName(VoiceService service)
        {
            IWitRuntimeConfigProvider configProvider = service.GetComponent<IWitRuntimeConfigProvider>();
            if (configProvider != null)
            {
                return $"{configProvider.RuntimeConfiguration.witConfiguration.name} [{service.gameObject.name}]";
            }
            return service.gameObject.name;
        }
        // Set voice service
        protected void SetVoiceService(VoiceService newService)
        {
            // Cannot set without services
            if (_services == null)
            {
                return;
            }

            // Find & apply
            int newServiceIndex = Array.FindIndex(_services, (s) => s == newService);

            // Apply
            SetVoiceService(newServiceIndex);
        }
        // Set
        protected void SetVoiceService(int newServiceIndex)
        {
            // Cannot set without services
            if (_services == null)
            {
                return;
            }

            // Remove listeners to current service
            RemoveVoiceListeners(service);

            // Get current index
            _currentService = newServiceIndex;

            // Add listeners to current service
            AddVoiceListeners(service);
        }
        // Remove listeners
        private void RemoveVoiceListeners(VoiceService v)
        {
            // Ignore
            if (v == null)
            {
                return;
            }
            // Remove delegates
            v.events.OnRequestCreated.RemoveListener(OnRequestCreated);
            v.events.OnError.RemoveListener(OnError);
            v.events.OnResponse.RemoveListener(ShowResponse);
            v.events.OnFullTranscription.RemoveListener(ShowTranscription);
            v.events.OnPartialTranscription.RemoveListener(ShowTranscription);
            v.events.OnStoppedListening.RemoveListener(ResetStartTime);
        }
        // Add listeners
        private void AddVoiceListeners(VoiceService v)
        {
            // Ignore
            if (v == null)
            {
                return;
            }
            // Add delegates
            v.events.OnRequestCreated.AddListener(OnRequestCreated);
            v.events.OnError.AddListener(OnError);
            v.events.OnResponse.AddListener(ShowResponse);
            v.events.OnPartialTranscription.AddListener(ShowTranscription);
            v.events.OnFullTranscription.AddListener(ShowTranscription);
            v.events.OnStoppedListening.AddListener(ResetStartTime);
        }
        #endregion
    }
}
