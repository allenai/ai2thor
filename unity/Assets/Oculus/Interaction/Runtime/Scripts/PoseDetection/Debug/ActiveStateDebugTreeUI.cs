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

using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Oculus.Interaction.PoseDetection.Debug
{
    public interface IActiveStateNodeUI
    {
        RectTransform ChildArea { get; }
        void Bind(IActiveStateTreeNode node, bool isRoot, bool isDuplicate);
    }

    public class ActiveStateDebugTreeUI : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IActiveState))]
        private MonoBehaviour _activeState;

        [SerializeField, Interface(typeof(IActiveStateNodeUI))]
        private MonoBehaviour _nodePrefab;

        [SerializeField]
        private RectTransform _contentArea;

        [SerializeField]
        private bool _buildTreeOnStart;

        [SerializeField, Optional]
        private TMP_Text _title;

        private IActiveState ActiveState;
        private ActiveStateDebugTree _tree;

        private Dictionary<IActiveStateTreeNode, IActiveStateNodeUI> _nodeToUI
            = new Dictionary<IActiveStateTreeNode, IActiveStateNodeUI>();

        protected virtual void Awake()
        {
            ActiveState = _activeState as IActiveState;
            _tree = new ActiveStateDebugTree(ActiveState);
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(ActiveState);
            Assert.IsNotNull(_nodePrefab);
            Assert.IsNotNull(_contentArea);

            if (_buildTreeOnStart)
            {
                BuildTree();
            }
        }

        public void BuildTree()
        {
            _nodeToUI.Clear();
            ClearContentArea();
            SetTitleText();
            BuildTreeRecursive(_contentArea, _tree.GetRootNode(), true);
        }

        private void BuildTreeRecursive(
            RectTransform parent, IActiveStateTreeNode node, bool isRoot)
        {
            IActiveStateNodeUI nodeUI = Instantiate(_nodePrefab, parent) as IActiveStateNodeUI;

            bool isDuplicate = _nodeToUI.ContainsKey(node);
            nodeUI.Bind(node, isRoot, isDuplicate);

            if (!isDuplicate)
            {
                _nodeToUI.Add(node, nodeUI);
                foreach (var child in node.Children)
                {
                    BuildTreeRecursive(nodeUI.ChildArea, child, false);
                }
            }
        }

        private void ClearContentArea()
        {
            for (int i = 0; i < _contentArea.childCount; ++i)
            {
                Transform child = _contentArea.GetChild(i);
                if (child != null && child.TryGetComponent<IActiveStateNodeUI>(out _))
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void SetTitleText()
        {
            if (_title != null)
            {
                _title.text = _activeState != null ?
                    _activeState.gameObject.name : "";
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            SetTitleText();
        }
#endif
    }
}
