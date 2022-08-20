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
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Oculus.Interaction
{
    /// <summary>
    /// Override Toggle to clear state on drag while still bubbling events up through
    /// the hierarchy. Particularly useful for buttons inside of scroll views.
    /// </summary>
    public class ToggleDeselect : Toggle
    {
        [SerializeField]
        private bool _clearStateOnDrag = false;

        public bool ClearStateOnDrag
        {
            get
            {
                return _clearStateOnDrag;
            }

            set

            {
                _clearStateOnDrag = value;
            }
        }

        public void OnBeginDrag(PointerEventData pointerEventData)
        {
            if (!_clearStateOnDrag)
            {
                return;
            }
            InstantClearState();
            DoStateTransition(SelectionState.Normal, true);
            ExecuteEvents.ExecuteHierarchy(
                transform.parent.gameObject,
                pointerEventData,
                ExecuteEvents.beginDragHandler
            );
        }
    }
}
