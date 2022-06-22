using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace UnityEditor.XR.Interaction.Toolkit
{
    class XRInteractionDebuggerWindow : EditorWindow
    {
        [SerializeField]
        Vector2 m_ScrollPosition;
        [SerializeField]
        bool m_ShowInputDevices; // Default off since the focus of this window is XRI.
        [SerializeField]
        bool m_ShowInteractors = true;
        [SerializeField]
        bool m_ShowInteractables = true;

        [SerializeField]
        Vector2 m_InputDevicesTreeScrollPosition;
        [SerializeField]
        TreeViewState m_InputDevicesTreeState;
        [SerializeField]
        MultiColumnHeaderState m_InputDevicesTreeHeaderState;

        [SerializeField]
        Vector2 m_InteractablesTreeScrollPosition;
        [SerializeField]
        TreeViewState m_InteractablesTreeState;
        [SerializeField]
        MultiColumnHeaderState m_InteractablesTreeHeaderState;

        [SerializeField]
        Vector2 m_InteractorsTreeScrollPosition;
        [SerializeField]
        TreeViewState m_InteractorsTreeState;
        [SerializeField]
        MultiColumnHeaderState m_InteractorsTreeHeaderState;

        XRInputDevicesTreeView m_InputDevicesTree;
        XRInteractorsTreeView m_InteractorsTree;
        XRInteractablesTreeView m_InteractablesTree;

        static XRInteractionDebuggerWindow s_Instance;

        static readonly List<string> s_Names = new List<string>();

        static readonly Dictionary<object, int> s_GeneratedUniqueIds = new Dictionary<object, int>();

        [MenuItem("Window/Analysis/XR Interaction Debugger", false, 2100)]
        public static void Init()
        {
            if (s_Instance == null)
            {
                s_GeneratedUniqueIds.Clear();
                s_Instance = GetWindow<XRInteractionDebuggerWindow>();
                s_Instance.Show();
                s_Instance.titleContent = EditorGUIUtility.TrTextContent("XR Interaction Debugger");
            }
            else
            {
                s_Instance.Show();
                s_Instance.Focus();
            }
        }

        void SetupInputDevicesTree()
        {
            if (m_InputDevicesTreeState == null)
                m_InputDevicesTreeState = new TreeViewState();
            m_InputDevicesTree = XRInputDevicesTreeView.Create(ref m_InputDevicesTreeState, ref m_InputDevicesTreeHeaderState);
            m_InputDevicesTree.ExpandAll();
        }

        void UpdateInteractorsTree()
        {
            var activeManagers = XRInteractionManager.activeInteractionManagers;
            if (m_InteractorsTree == null)
            {
                m_InteractorsTree = XRInteractorsTreeView.Create(activeManagers, ref m_InteractorsTreeState, ref m_InteractorsTreeHeaderState);
                m_InteractorsTree.ExpandAll();
            }
            else
            {
                m_InteractorsTree.UpdateManagersList(activeManagers);
            }
        }

        void UpdateInteractablesTree()
        {
            var activeManagers = XRInteractionManager.activeInteractionManagers;
            if (m_InteractablesTree == null)
            {
                m_InteractablesTree = XRInteractablesTreeView.Create(activeManagers, ref m_InteractablesTreeState, ref m_InteractablesTreeHeaderState);
                m_InteractablesTree.ExpandAll();
            }
            else
            {
                m_InteractablesTree.UpdateManagersList(activeManagers);
            }
        }

        public void OnInspectorUpdate()
        {
            // TODO Only do this when devices update
            SetupInputDevicesTree();

            UpdateInteractorsTree();
            UpdateInteractablesTree();

            if (m_InputDevicesTree != null)
            {
                m_InputDevicesTree.Reload();
                m_InputDevicesTree.Repaint();
            }

            m_InteractablesTree?.Repaint();
            Repaint();
        }

        public void OnGUI()
        {
            DrawToolbarGUI();
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            if (m_ShowInputDevices && m_InputDevicesTree != null)
                DrawInputDevicesGUI();
            if (m_ShowInteractors && m_InteractorsTree != null)
                DrawInteractorsGUI();
            if (m_ShowInteractables && m_InteractablesTree != null)
                DrawInteractablesGUI();

            EditorGUILayout.EndScrollView();
        }

        void DrawInputDevicesGUI()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Devices", GUILayout.MinWidth(100), GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            m_InputDevicesTreeScrollPosition = EditorGUILayout.BeginScrollView(m_InputDevicesTreeScrollPosition);
            var rect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
            m_InputDevicesTree.OnGUI(rect);
            EditorGUILayout.EndScrollView();
        }

        void DrawInteractorsGUI()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Interactors", GUILayout.MinWidth(100), GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // TODO I'm not sure tree view needs a scroll view or whether it does that automatically
            m_InteractorsTreeScrollPosition = EditorGUILayout.BeginScrollView(m_InteractorsTreeScrollPosition);
            var rect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
            m_InteractorsTree.OnGUI(rect);
            EditorGUILayout.EndScrollView();
        }

        void DrawInteractablesGUI()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Interactables", GUILayout.MinWidth(100), GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // TODO I'm not sure tree view needs a scroll view or whether it does that automatically
            m_InteractablesTreeScrollPosition = EditorGUILayout.BeginScrollView(m_InteractablesTreeScrollPosition);
            var rect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
            m_InteractablesTree.OnGUI(rect);
            EditorGUILayout.EndScrollView();
        }

        void DrawToolbarGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            m_ShowInputDevices
                = GUILayout.Toggle(m_ShowInputDevices, Contents.showInputDevices, EditorStyles.toolbarButton);
            m_ShowInteractors
                = GUILayout.Toggle(m_ShowInteractors, Contents.showInteractorsContent, EditorStyles.toolbarButton);
            m_ShowInteractables
                = GUILayout.Toggle(m_ShowInteractables, Contents.showInteractablesContent, EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        internal static string JoinNames<T>(string separator, List<T> objects)
        {
            s_Names.Clear();
            foreach (var obj in objects)
            {
                var name = GetDisplayName(obj);
                s_Names.Add(name);
            }

            return string.Join(separator, s_Names);
        }

        internal static string GetDisplayName(object obj)
        {
            if (obj is Object unityObject)
            {
                return unityObject != null ? unityObject.name : "<Destroyed>";
            }

            return obj.GetType().Name;
        }

        internal static int GetUniqueTreeViewId(object obj)
        {
            if (obj is Object unityObject)
            {
                return unityObject.GetInstanceID();
            }

            // Generate an ID if the object isn't a Unity Object,
            // making sure to not clash with an existing instance ID.
            if (!s_GeneratedUniqueIds.TryGetValue(obj, out var id))
            {
                do
                {
                    id = Random.Range(int.MinValue, int.MaxValue);
                } while (EditorUtility.InstanceIDToObject(id) != null);

                s_GeneratedUniqueIds.Add(obj, id);
            }

            return id;
        }

        static class Contents
        {
            public static GUIContent showInputDevices = EditorGUIUtility.TrTextContent("Input Devices");
            public static GUIContent showInteractablesContent = EditorGUIUtility.TrTextContent("Interactables");
            public static GUIContent showInteractorsContent = EditorGUIUtility.TrTextContent("Interactors");
        }
    }
}
