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
using UnityEngine.UI;
using UnityEngine.Assertions;

namespace Oculus.Interaction.PoseDetection.Debug
{
    public class ActiveStateNodeUIHorizontal : MonoBehaviour, IActiveStateNodeUI
    {
        [SerializeField]
        private RectTransform _childArea;

        [SerializeField]
        private RectTransform _connectingLine;

        [SerializeField]
        private TextMeshProUGUI _label;

        [SerializeField]
        private Image _activeImage;

        [SerializeField]
        private Color _activeColor = Color.green;

        [SerializeField]
        private Color _inactiveColor = Color.red;

        private const string OBJNAME_FORMAT = "<color=#dddddd><size=85%>{0}</size></color>";

        public RectTransform ChildArea => _childArea;

        private IActiveStateTreeNode _boundNode;
        private bool _isRoot = false;
        private bool _isDuplicate = false;

        public void Bind(IActiveStateTreeNode node, bool isRoot, bool isDuplicate)
        {
            Assert.IsNotNull(node);

            _isRoot = isRoot;
            _isDuplicate = isDuplicate;
            _boundNode = node;
            _label.text = GetLabelText(node);
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(_childArea);
            Assert.IsNotNull(_connectingLine);
            Assert.IsNotNull(_activeImage);
            Assert.IsNotNull(_label);
        }

        protected virtual void Update()
        {
            _activeImage.color = _boundNode.ActiveState.Active ? _activeColor : _inactiveColor;
            _childArea.gameObject.SetActive(_childArea.childCount > 0);
            _connectingLine.gameObject.SetActive(!_isRoot);
        }

        private string GetLabelText(IActiveStateTreeNode node)
        {
            string label = _isDuplicate ? "<i>" : "";
            if (node.ActiveState is MonoBehaviour mono)
            {
                label += mono.gameObject.name + System.Environment.NewLine;
            }
            label += string.Format(OBJNAME_FORMAT, node.ActiveState.GetType().Name);
            return label;
        }
    }
}
