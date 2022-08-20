/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using Oculus.Interaction.GrabAPI;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// This interactable specifies the final pose a hand will have, via HandGrabPoints, when using the
    /// interactable, and also the rules to use it.
    /// It also provides the relaxed and tigh HandGrabPoses to modify the visual hand depending on
    /// the progress of the interaction.
    /// By default, it will update the Progress of the interaction to the strength of usage, but it is
    /// possible to reference a IHandGrabUseDelegate to derive this calculation to a separate script.
    /// </summary>
    public class HandGrabUseInteractable : Interactable<HandGrabUseInteractor, HandGrabUseInteractable>
    {
        /// <summary>
        /// This delegate allows redirecting the Strength to Progress calculations
        /// to a separate script. Implement it in the usable object so it also
        /// receives updates from this interaction automatically.
        /// </summary>
        [SerializeField, Optional, Interface(typeof(IHandGrabUseDelegate))]
        private MonoBehaviour _handUseDelegate;
        private IHandGrabUseDelegate HandUseDelegate { get; set; }

        /// <summary>
        /// The rules for using this item. All required fingers must be using in order
        /// to reach maximum progress, when no required fingers are present, the strongest
        /// optional finger can drive the progress value.
        /// </summary>
        [SerializeField]
        private GrabbingRule _useFingers;
        public GrabbingRule UseFingers
        {
            get
            {
                return _useFingers;
            }
            set
            {
                _useFingers = value;
            }
        }

        /// <summary>
        /// Fingers whose strength value is below this dead zone will not be
        /// considered as snappers.
        /// </summary>
        [SerializeField, Range(0f, 1f)]
        private float _strengthDeadzone = 0.2f;
        public float StrengthDeadzone
        {
            get
            {
                return _strengthDeadzone;
            }
            set
            {
                _strengthDeadzone = value;
            }

        }

        /// <summary>
        /// Hand grab poses representing the initial pose when the item is used at minimum progress
        /// </summary>
        [SerializeField, Optional]
        private List<HandGrabPose> _relaxedHandGrabPoses = new List<HandGrabPose>();
        /// <summary>
        /// Hand grab poses representing the final pose when the item is used at maximum progress
        /// </summary>
        [SerializeField, Optional]
        private List<HandGrabPose> _tightHandGrabPoses = new List<HandGrabPose>();

        /// <summary>
        /// Value indicating the progress of the use interaction.
        /// </summary>
        public float UseProgress { get; private set; }

        public List<HandGrabPose> RelaxGrabPoints => _relaxedHandGrabPoses;
        public List<HandGrabPose> TightGrabPoints => _tightHandGrabPoses;

        public float UseStrengthDeadZone => _strengthDeadzone;

        protected virtual void Reset()
        {
            HandGrabInteractable handGrabInteractable = this.GetComponentInParent<HandGrabInteractable>();
            if (handGrabInteractable != null)
            {
                _relaxedHandGrabPoses = new List<HandGrabPose>(handGrabInteractable.HandGrabPoses);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            HandUseDelegate = _handUseDelegate as IHandGrabUseDelegate;
        }

        protected override void SelectingInteractorAdded(HandGrabUseInteractor interactor)
        {
            base.SelectingInteractorAdded(interactor);
            HandUseDelegate?.BeginUse();
        }

        protected override void SelectingInteractorRemoved(HandGrabUseInteractor interactor)
        {
            base.SelectingInteractorRemoved(interactor);
            HandUseDelegate?.EndUse();

        }

        public float ComputeUseStrength(float strength)
        {
            UseProgress = HandUseDelegate != null ? HandUseDelegate.ComputeUseStrength(strength) : strength;
            return UseProgress;
        }

        public bool FindBestHandPoses(float handScale, ref HandPose relaxedHandPose, ref HandPose tightHandPose, out float score)
        {
            if (FindScaledHandPose(_relaxedHandGrabPoses, handScale, ref relaxedHandPose)
                && FindScaledHandPose(_tightHandGrabPoses, handScale, ref tightHandPose))
            {
                score = 1f;
                return true;
            }

            score = 0f;
            return false;
        }

        private bool FindScaledHandPose(List<HandGrabPose> _handGrabPoses, float handScale, ref HandPose handPose)
        {
            if (_handGrabPoses.Count == 1 && _handGrabPoses[0].HandPose != null)
            {
                handPose.CopyFrom(_handGrabPoses[0].HandPose);
                return true;
            }
            else if (_handGrabPoses.Count > 1)
            {
                GrabPoseFinder.FindInterpolationRange(handScale, _handGrabPoses, out HandGrabPose under, out HandGrabPose over, out float t);
                if (under.HandPose != null && over.HandPose != null)
                {
                    HandPose.Lerp(under.HandPose, over.HandPose, t, ref handPose);
                    return true;
                }
                else if (under.HandPose != null)
                {
                    handPose.CopyFrom(under.HandPose);
                    return true;
                }
                else if (over.HandPose != null)
                {
                    handPose.CopyFrom(over.HandPose);
                    return true;
                }

                return false;
            }

            return false;
        }

        #region Inject

        public void InjectOptionalForwardUseDelegate(IHandGrabUseDelegate useDelegate)
        {
            _handUseDelegate = useDelegate as MonoBehaviour;
            HandUseDelegate = useDelegate;
        }

        public void InjectOptionalRelaxedHandGrabPoints(List<HandGrabPose> relaxedHandGrabPoints)
        {
            _relaxedHandGrabPoses = relaxedHandGrabPoints;
        }

        public void InjectOptionalTightHandGrabPoints(List<HandGrabPose> tightHandGrabPoints)
        {
            _tightHandGrabPoses = tightHandGrabPoints;
        }

        #endregion
    }
}
