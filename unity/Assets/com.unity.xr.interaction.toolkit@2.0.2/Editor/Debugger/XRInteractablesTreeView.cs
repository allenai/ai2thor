using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEditor.XR.Interaction.Toolkit
{
    /// <summary>
    /// Multi-column <see cref="TreeView"/> that shows Interactables.
    /// </summary>
    class XRInteractablesTreeView : TreeView
    {
        public static XRInteractablesTreeView Create(List<XRInteractionManager> interactionManagers, ref TreeViewState treeState, ref MultiColumnHeaderState headerState)
        {
            if (treeState == null)
                treeState = new TreeViewState();

            var newHeaderState = CreateHeaderState();
            if (headerState != null)
                MultiColumnHeaderState.OverwriteSerializedFields(headerState, newHeaderState);
            headerState = newHeaderState;

            var header = new MultiColumnHeader(headerState);
            return new XRInteractablesTreeView(interactionManagers, treeState, header);
        }

        const float k_RowHeight = 20f;

        class Item : TreeViewItem
        {
            public IXRInteractable interactable;
        }

        enum ColumnId
        {
            Name,
            Type,
            LayerMask,
            Colliders,
            Hovered,
            Selected,

            Count,
        }

        static bool exitingPlayMode => EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode;

        readonly List<XRInteractionManager> m_InteractionManagers = new List<XRInteractionManager>();

        static MultiColumnHeaderState CreateHeaderState()
        {
            var columns = new MultiColumnHeaderState.Column[(int)ColumnId.Count];

            columns[(int)ColumnId.Name]      = new MultiColumnHeaderState.Column { width = 180f, minWidth = 80f, headerContent = EditorGUIUtility.TrTextContent("Name") };
            columns[(int)ColumnId.Type]      = new MultiColumnHeaderState.Column { width = 120f, minWidth = 80f, headerContent = EditorGUIUtility.TrTextContent("Type") };
            columns[(int)ColumnId.LayerMask] = new MultiColumnHeaderState.Column { width = 120f, minWidth = 80f, headerContent = EditorGUIUtility.TrTextContent("Layer Mask") };
            columns[(int)ColumnId.Colliders] = new MultiColumnHeaderState.Column { width = 120f, minWidth = 80f, headerContent = EditorGUIUtility.TrTextContent("Colliders") };
            columns[(int)ColumnId.Hovered]     = new MultiColumnHeaderState.Column { width = 80f, minWidth = 80f, headerContent = EditorGUIUtility.TrTextContent("Hovered") };
            columns[(int)ColumnId.Selected]    = new MultiColumnHeaderState.Column { width = 80f, minWidth = 80f, headerContent = EditorGUIUtility.TrTextContent("Selected") };

            return new MultiColumnHeaderState(columns);
        }

        XRInteractablesTreeView(List<XRInteractionManager> managers, TreeViewState state, MultiColumnHeader header)
            : base(state, header)
        {
            foreach(var manager in managers)
                AddManager(manager);
            showBorder = false;
            rowHeight = k_RowHeight;
            Reload();
        }

        public void UpdateManagersList(List<XRInteractionManager> currentManagers)
        {
            var managerListChanged = false;

            // Check for Removal
            for (var i = 0; i < m_InteractionManagers.Count; i++)
            {
                var manager = m_InteractionManagers[i];
                if (!currentManagers.Contains(manager))
                {
                    RemoveManager(manager);
                    managerListChanged = true;
                    --i;
                }
            }

            // Check for Add
            foreach (var manager in currentManagers)
            {
                if (!m_InteractionManagers.Contains(manager))
                {
                    AddManager(manager);
                    managerListChanged = true;
                }
            }

            if (managerListChanged)
                Reload();
        }

        void AddManager(XRInteractionManager manager)
        {
            if (m_InteractionManagers.Contains(manager))
                return;

            manager.interactableRegistered += OnInteractableRegistered;
            manager.interactableUnregistered += OnInteractableUnregistered;

            m_InteractionManagers.Add(manager);
            Reload();
        }

        void RemoveManager(XRInteractionManager manager)
        {
            if (!m_InteractionManagers.Contains(manager))
                return;

            if (manager != null)
            {
                manager.interactableRegistered -= OnInteractableRegistered;
                manager.interactableUnregistered -= OnInteractableUnregistered;
            }

            m_InteractionManagers.Remove(manager);
            Reload();
        }

        void OnInteractableRegistered(InteractableRegisteredEventArgs eventArgs)
        {
            Reload();
        }

        void OnInteractableUnregistered(InteractableUnregisteredEventArgs eventArgs)
        {
            // Skip reloading as each interactable is being destroyed when exiting Play mode
            if (!exitingPlayMode)
                Reload();
        }

        /// <inheritdoc />
        protected override TreeViewItem BuildRoot()
        {
            // Wrap root control in invisible item required by TreeView.
            return new Item
            {
                id = 0,
                children = BuildInteractableTree(),
                depth = -1,
            };
        }

        List<TreeViewItem> BuildInteractableTree()
        {
            var items = new List<TreeViewItem>();
            var interactables = new List<IXRInteractable>();

            foreach (var interactionManager in m_InteractionManagers)
            {
                if (interactionManager == null)
                    continue;

                var rootTreeItem = new Item
                {
                    id = XRInteractionDebuggerWindow.GetUniqueTreeViewId(interactionManager),
                    displayName = XRInteractionDebuggerWindow.GetDisplayName(interactionManager),
                    depth = 0,
                };

                // Build children.
                interactionManager.GetRegisteredInteractables(interactables);
                if (interactables.Count > 0)
                {
                    var children = new List<TreeViewItem>();
                    foreach (var interactable in interactables)
                    {
                        var childItem = new Item
                        {
                            id = XRInteractionDebuggerWindow.GetUniqueTreeViewId(interactable),
                            displayName = XRInteractionDebuggerWindow.GetDisplayName(interactable),
                            interactable = interactable,
                            depth = 1,
                            parent = rootTreeItem,
                        };
                        children.Add(childItem);
                    }

                    // Sort children by name.
                    children.Sort((a, b) => string.Compare(a.displayName, b.displayName));
                    rootTreeItem.children = children;
                }

                items.Add(rootTreeItem);
            }

            return items;
        }

        /// <inheritdoc />
        protected override void RowGUI(RowGUIArgs args)
        {
            if (!Application.isPlaying || exitingPlayMode)
                return;

            var item = (Item)args.item;

            var columnCount = args.GetNumVisibleColumns();
            for (var i = 0; i < columnCount; ++i)
            {
                ColumnGUI(args.GetCellRect(i), item, args.GetColumn(i), ref args);
            }
        }

        void ColumnGUI(Rect cellRect, Item item, int column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            if (column == (int)ColumnId.Name)
            {
                args.rowRect = cellRect;
                base.RowGUI(args);
            }

            if (item.interactable != null)
            {
                switch (column)
                {
                    case (int)ColumnId.Type:
                        GUI.Label(cellRect, item.interactable.GetType().Name);
                        break;
                    case (int)ColumnId.LayerMask:
                        GUI.Label(cellRect, item.interactable.interactionLayers.value.ToString());
                        break;
                    case (int)ColumnId.Colliders:
                        GUI.Label(cellRect, XRInteractionDebuggerWindow.JoinNames(",", item.interactable.colliders));
                        break;
                    case (int)ColumnId.Hovered:
                        if (item.interactable is IXRHoverInteractable hoverable)
                            GUI.Label(cellRect, hoverable.isHovered ? "True" : "False");
                        break;
                    case (int)ColumnId.Selected:
                        if (item.interactable is IXRSelectInteractable selectable)
                            GUI.Label(cellRect, selectable.isSelected ? "True" : "False");
                        break;
                }
            }
        }

        /// <inheritdoc />
        protected override void DoubleClickedItem(int id)
        {
            base.DoubleClickedItem(id);

            EditorGUIUtility.PingObject(id);
            Selection.activeInstanceID = id;
        }
    }
}
