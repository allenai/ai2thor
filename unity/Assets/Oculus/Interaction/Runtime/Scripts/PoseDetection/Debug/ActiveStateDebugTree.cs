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
using System.Linq;
using System;

namespace Oculus.Interaction.PoseDetection.Debug
{
    public interface IActiveStateTreeNode
    {
        IActiveState ActiveState { get; }
        IEnumerable<IActiveStateTreeNode> Children { get; }
    }

    public class ActiveStateDebugTree
    {
        private class Node : IActiveStateTreeNode
        {
            IActiveState IActiveStateTreeNode.ActiveState => ActiveState;
            IEnumerable<IActiveStateTreeNode> IActiveStateTreeNode.Children => Children;

            public IActiveState ActiveState { get; set; }
            public List<Node> Children { get; set; }
        }

        private static Dictionary<Type, IActiveStateModel> _models =
            new Dictionary<Type, IActiveStateModel>()
            {
                [typeof(ActiveStateGroup)] = new ActiveStateGroupModel(),
                [typeof(SequenceActiveState)] = new SequenceActiveStateModel(),
                [typeof(Sequence)] = new SequenceModel(),
                [typeof(ActiveStateNot)] = new ActiveStateNotModel(),
            };

        private Dictionary<IActiveState, Node> _existingNodes =
            new Dictionary<IActiveState, Node>();

        private readonly IActiveState Root;
        private Node _rootNode;

        public ActiveStateDebugTree(IActiveState root)
        {
            Root = root;
        }

        public IActiveStateTreeNode GetRootNode()
        {
            if (_rootNode == null)
            {
                _rootNode = BuildTree(Root);
            }
            return _rootNode;
        }

        public void Rebuild()
        {
            _rootNode = BuildTree(Root);
        }

        private Node BuildTree(IActiveState root)
        {
            _existingNodes.Clear();
            return BuildTreeRecursive(root);
        }

        private Node BuildTreeRecursive(IActiveState activeState)
        {
            if (activeState == null)
            {
                return null;
            }

            if (_existingNodes.ContainsKey(activeState))
            {
                return _existingNodes[activeState];
            }

            List<Node> children = new List<Node>();
            if (_models.TryGetValue(activeState.GetType(),
                out IActiveStateModel model))
            {
                children.AddRange(model.GetChildren(activeState)
                    .Select((child) => BuildTreeRecursive(child))
                    .Where((child) => child != null));
            }

            Node self = new Node()
            {
                ActiveState = activeState,
                Children = children,
            };

            _existingNodes.Add(activeState, self);
            return self;
        }
    }
}
