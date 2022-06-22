using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.XR;

namespace UnityEditor.XR.Interaction.Toolkit
{
    /// <summary>
    /// Multi-column <see cref="TreeView"/> that shows Input Devices.
    /// </summary>
    class XRInputDevicesTreeView : TreeView
    {
        public static XRInputDevicesTreeView Create(ref TreeViewState treeState, ref MultiColumnHeaderState headerState)
        {
            if (treeState == null)
                treeState = new TreeViewState();

            var newHeaderState = CreateHeaderState();
            if (headerState != null)
                MultiColumnHeaderState.OverwriteSerializedFields(headerState, newHeaderState);
            headerState = newHeaderState;

            var header = new MultiColumnHeader(headerState);
            return new XRInputDevicesTreeView(treeState, header);
        }

        const float k_RowHeight = 20f;

        class Item : TreeViewItem
        {
            public string deviceRole;
            public string featureType;
            public string featureValue;
        }

        enum ColumnId
        {
            Name,
            Role,
            Type,
            Value,

            COUNT,
        }

        static MultiColumnHeaderState CreateHeaderState()
        {
            var columns = new MultiColumnHeaderState.Column[(int)ColumnId.COUNT];

            columns[(int)ColumnId.Name] =
                new MultiColumnHeaderState.Column
                {
                    width = 240f,
                    minWidth = 60f,
                    headerContent = EditorGUIUtility.TrTextContent("Name"),
                };
            columns[(int)ColumnId.Role] =
                new MultiColumnHeaderState.Column
                {
                    width = 200f,
                    minWidth = 60f,
                    headerContent = EditorGUIUtility.TrTextContent("Role"),
                };
            columns[(int)ColumnId.Type] =
                new MultiColumnHeaderState.Column
                {
                    width = 200f,
                    headerContent = EditorGUIUtility.TrTextContent("Type"),
                };
            columns[(int)ColumnId.Value] =
                new MultiColumnHeaderState.Column
                {
                    width = 200f,
                    headerContent = EditorGUIUtility.TrTextContent("Value"),
                };

            return new MultiColumnHeaderState(columns);
        }

        XRInputDevicesTreeView(TreeViewState state, MultiColumnHeader header)
            : base(state, header)
        {
            showBorder = false;
            rowHeight = k_RowHeight;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            // Wrap root control in invisible item required by TreeView.
            return new Item
            {
                id = 0,
                children = new List<TreeViewItem> { BuildInputDevicesTree() },
                depth = -1,
            };
        }

        static string GetFeatureValue(InputDevice device, InputFeatureUsage featureUsage)
        {
            switch (featureUsage.type.ToString())
            {
                case "System.Boolean":
                    if (device.TryGetFeatureValue(featureUsage.As<bool>(), out var boolValue))
                        return boolValue.ToString();
                    break;
                case "System.UInt32":
                    if (device.TryGetFeatureValue(featureUsage.As<uint>(), out var uintValue))
                        return uintValue.ToString();
                    break;
                case "System.Single":
                    if (device.TryGetFeatureValue(featureUsage.As<float>(), out var floatValue))
                        return floatValue.ToString();
                    break;
                case "UnityEngine.Vector2":
                    if (device.TryGetFeatureValue(featureUsage.As<Vector2>(), out var vector2Value))
                        return vector2Value.ToString();
                    break;
                case "UnityEngine.Vector3":
                    if (device.TryGetFeatureValue(featureUsage.As<Vector3>(), out var vector3Value))
                        return vector3Value.ToString();
                    break;
                case "UnityEngine.Quaternion":
                    if (device.TryGetFeatureValue(featureUsage.As<Quaternion>(), out var quaternionValue))
                        return quaternionValue.ToString();
                    break;
                case "UnityEngine.XR.Hand":
                    if (device.TryGetFeatureValue(featureUsage.As<Hand>(), out var handValue))
                        return handValue.ToString();
                    break;
                case "UnityEngine.XR.Bone":
                    if (device.TryGetFeatureValue(featureUsage.As<Bone>(), out var boneValue))
                    {
                        if (boneValue.TryGetPosition(out var bonePosition) && boneValue.TryGetRotation(out var boneRotation))
                            return $"{bonePosition}, {boneRotation}";
                    }
                    break;
                case "UnityEngine.XR.Eyes":
                    if (device.TryGetFeatureValue(featureUsage.As<Eyes>(), out var eyesValue))
                    {
                        if (eyesValue.TryGetFixationPoint(out var fixation) &&
                            eyesValue.TryGetLeftEyePosition(out var left) &&
                            eyesValue.TryGetRightEyePosition(out var right) && 
                            eyesValue.TryGetLeftEyeOpenAmount(out var leftOpen) &&
                            eyesValue.TryGetRightEyeOpenAmount(out var rightOpen))
                            return $"{fixation}, {left}, {right}, {leftOpen}, {rightOpen}";
                    }
                    break;
            }

            return "";
        }

        static TreeViewItem BuildInputDevicesTree()
        {
            var rootItem = new Item
            {
                id = 1,
                displayName = "Devices",
                depth = 0,
            };

            // Build children.
            var inputDevices = new List<InputDevice>();
            InputDevices.GetDevices(inputDevices);

            var deviceChildren = new List<TreeViewItem>();

            // Add device children
            foreach (var device in inputDevices)
            {
                var deviceItem = new Item
                {
                    id = device.GetHashCode(),
                    displayName = device.name,
                    // TODO Need to display new characteristics API here.
#pragma warning disable 612, 618
                    deviceRole = device.role.ToString(),
#pragma warning restore 612, 618
                    depth = 1,
                    parent = rootItem,
                };

                var featureUsages = new List<InputFeatureUsage>();
                device.TryGetFeatureUsages(featureUsages);
                
                var featureChildren = new List<TreeViewItem>();
                foreach (var featureUsage in featureUsages)
                {
                    var featureItem = new Item
                    {
                        id = device.GetHashCode() ^ (featureUsage.GetHashCode() >> 1),
                        displayName = featureUsage.name,
                        featureType = featureUsage.type.ToString(),
                        featureValue = GetFeatureValue(device, featureUsage),
                        depth = 2,
                        parent = deviceItem,
                    };
                    featureChildren.Add(featureItem);
                }

                deviceItem.children = featureChildren;
                deviceChildren.Add(deviceItem);
            }

            // Sort deviceChildren by name.
            deviceChildren.Sort((a, b) => string.Compare(a.displayName, b.displayName));
            rootItem.children = deviceChildren;

            return rootItem;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
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

            switch (column)
            {
                case (int)ColumnId.Role:
                    GUI.Label(cellRect, item.deviceRole);
                    break;
                case (int)ColumnId.Type:
                    if (item.depth == 2)
                        GUI.Label(cellRect, item.featureType);
                    break;
                case (int)ColumnId.Value:
                    if (item.depth == 2)
                        GUI.Label(cellRect, item.featureValue);
                    break;
            }
        }
    }
}
