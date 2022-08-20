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
using UnityEngine;
using System.Reflection;
using System.Linq;
using UnityEngine.Assertions;
using System;

namespace Oculus.Interaction.PoseDetection.Debug
{
    public interface IActiveStateModel
    {
        IEnumerable<IActiveState> GetChildren(IActiveState activeState);
    }

    public abstract class ActiveStateModel<TActiveState> : IActiveStateModel
        where TActiveState : MonoBehaviour, IActiveState
    {
        protected Type Type => typeof(TActiveState);

        public virtual IEnumerable<IActiveState> GetChildren(TActiveState activeState)
        {
            return Enumerable.Empty<IActiveState>();
        }

        public IEnumerable<IActiveState> GetChildren(IActiveState activeState)
        {
            Assert.AreEqual(activeState.GetType(), Type,
                $"Expected MonoBehaviour of type {Type.Name}");
            return GetChildren(activeState as TActiveState);
        }
    }

    public class ActiveStateGroupModel : ActiveStateModel<ActiveStateGroup>
    {
        public override IEnumerable<IActiveState> GetChildren(ActiveStateGroup group)
        {
            List<IActiveState> children =
                Type.GetField("ActiveStates", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(group) as List<IActiveState>;
            return children;
        }
    }

    public class SequenceModel : ActiveStateModel<Sequence>
    {
        private IActiveState GetActiveStateFromStep(Sequence.ActivationStep step)
        {
            step.Start();
            return step.ActiveState;
        }

        public override IEnumerable<IActiveState> GetChildren(Sequence sequence)
        {
            Sequence.ActivationStep[] steps =
                Type.GetField("_stepsToActivate", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(sequence) as Sequence.ActivationStep[];
            List<IActiveState> children = new List<IActiveState>(
                steps.Select(GetActiveStateFromStep));

            IActiveState remainActiveWhile =
                Type.GetProperty("RemainActiveWhile", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(sequence) as IActiveState;

            if (remainActiveWhile != null)
            {
                children.Add(remainActiveWhile);
            }
            return children;
        }
    }

    public class SequenceActiveStateModel : ActiveStateModel<SequenceActiveState>
    {
        public override IEnumerable<IActiveState> GetChildren(SequenceActiveState seqActiveState)
        {
            Sequence sequence =
                Type.GetField("_sequence", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(seqActiveState) as Sequence;
            return new List<IActiveState>() { sequence };
        }
    }

    public class ActiveStateNotModel : ActiveStateModel<ActiveStateNot>
    {
        public override IEnumerable<IActiveState> GetChildren(ActiveStateNot not)
        {
            List<IActiveState> children = new List<IActiveState>()
            {
                Type.GetField("ActiveState", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(not) as IActiveState
            };
            return children;
        }
    }
}
