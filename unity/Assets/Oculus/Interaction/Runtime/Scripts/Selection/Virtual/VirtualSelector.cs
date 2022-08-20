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
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// This Virtual Selector can provide interactions with a sample selector
    /// that can be toggled from within the Unity insepctor using the HandleSelected Flag
    /// </summary>
    public class VirtualSelector : MonoBehaviour, ISelector
    {
        [SerializeField]
        private bool _selectFlag;

        public event Action WhenSelected = delegate { };
        public event Action WhenUnselected = delegate { };
        private bool _currentlySelected;

        public void Select()
        {
            _selectFlag = true;
            UpdateSelection();
        }

        public void Unselect()
        {
            _selectFlag = false;
            UpdateSelection();
        }

        protected virtual void OnValidate()
        {
            UpdateSelection();
        }

        protected void UpdateSelection()
        {
            if (_currentlySelected != _selectFlag)
            {
                _currentlySelected = _selectFlag;
                if (_currentlySelected)
                {
                    WhenSelected();
                }
                else
                {
                    WhenUnselected();
                }
            }
        }
    }
}
