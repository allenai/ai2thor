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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    public class ActiveStateGroup : MonoBehaviour, IActiveState
    {
        public enum ActiveStateGroupLogicOperator
        {
            AND = 0,
            OR = 1,
            XOR = 2
        }

        [SerializeField, Interface(typeof(IActiveState))]
        private List<MonoBehaviour> _activeStates;
        private List<IActiveState> ActiveStates;

        [SerializeField]
        private ActiveStateGroupLogicOperator _logicOperator = ActiveStateGroupLogicOperator.AND;

        protected virtual void Awake()
        {
            ActiveStates = _activeStates.ConvertAll(mono => mono as IActiveState);
        }

        protected virtual void Start()
        {
            foreach (IActiveState activeState in ActiveStates)
            {
                Assert.IsNotNull(activeState);
            }
        }

        public bool Active
        {
            get
            {
                if (ActiveStates == null)
                {
                    return false;
                }

                switch(_logicOperator)
                {
                    case ActiveStateGroupLogicOperator.AND:
                        foreach(IActiveState activeState in ActiveStates)
                        {
                            if(!activeState.Active) return false;
                        }
                        return true;

                    case ActiveStateGroupLogicOperator.OR:
                        foreach(IActiveState activeState in ActiveStates)
                        {
                            if(activeState.Active) return true;
                        }
                        return false;

                    case ActiveStateGroupLogicOperator.XOR:
                        bool foundActive = false;
                        foreach(IActiveState activeState in ActiveStates)
                        {
                            if(activeState.Active)
                            {
                                if(foundActive) return false;
                                foundActive = true;
                            }
                        }
                        return foundActive;

                    default:
                        return false;
                }
            }
        }

        #region Inject

        public void InjectAllActiveStateGroup(List<IActiveState> activeStates)
        {
            InjectActiveStates(activeStates);
        }

        public void InjectActiveStates(List<IActiveState> activeStates)
        {
            ActiveStates = activeStates;
            _activeStates = activeStates.ConvertAll(activeState => activeState as MonoBehaviour);
        }

        public void InjectOptionalLogicOperator(ActiveStateGroupLogicOperator logicOperator)
        {
            _logicOperator = logicOperator;
        }

        #endregion
    }
}
