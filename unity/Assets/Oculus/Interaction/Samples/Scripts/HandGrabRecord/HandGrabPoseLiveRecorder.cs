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

using Oculus.Interaction.HandGrab.Visuals;
using Oculus.Interaction.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Oculus.Interaction.HandGrab.Recorder
{
    public class HandGrabPoseLiveRecorder : MonoBehaviour, IActiveState
    {
        [SerializeField]
        private HandGrabInteractor _leftHand;
        [SerializeField]
        private HandGrabInteractor _rightHand;

        [SerializeField]
        [Tooltip("Prototypes of the static hands (ghosts) that visualize holding poses")]
        private HandGhostProvider _ghostProvider;

        [SerializeField, Optional]
        private TimerUIControl _timerControl;

        [SerializeField, Optional]
        private TMPro.TextMeshPro _delayLabel;

        private RigidbodyDetector _leftDetector;
        private RigidbodyDetector _rightDetector;

        private WaitForSeconds _waitOneSeconds = new WaitForSeconds(1f);
        private Coroutine _delayedSnapRoutine;

        public UnityEvent WhenTimeStep;
        public UnityEvent WhenSnapshot;
        public UnityEvent WhenError;
        [Space]
        public UnityEvent<bool> WhenCanUndo;
        public UnityEvent<bool> WhenCanRedo;
        public UnityEvent WhenGrabAllowed;
        public UnityEvent WhenGrabDisallowed;

        private struct RecorderStep
        {
            public HandPose RawHandPose { get; private set; }
            public Pose GrabPoint { get; private set; }
            public Rigidbody Item { get; private set; }

            public HandGrabInteractable interactable;

            public RecorderStep(HandPose rawPose, Pose grabPoint, Rigidbody item)
            {
                this.RawHandPose = new HandPose(rawPose);
                this.GrabPoint = grabPoint;
                this.Item = item;
                interactable = null;
            }

            public void ClearInteractable()
            {
                if (interactable != null)
                {
                    Destroy(interactable.gameObject);
                }
            }
        }

        private List<RecorderStep> _recorderSteps = new List<RecorderStep>();

        private int _currentStepIndex;
        private int CurrentStepIndex
        {
            get
            {
                return _currentStepIndex;
            }
            set
            {
                _currentStepIndex = value;
                WhenCanUndo?.Invoke(_currentStepIndex >= 0);
                WhenCanRedo?.Invoke(_currentStepIndex + 1 < _recorderSteps.Count);
            }
        }


        public bool Active => _grabbingEnabled;

        private bool _grabbingEnabled = true;

        private void Awake()
        {
            _leftHand.InjectOptionalActiveState(this);
            _rightHand.InjectOptionalActiveState(this);
        }

        private void Start()
        {
            ClearSnapshot();
            _leftDetector = _leftHand.Rigidbody.gameObject.AddComponent<RigidbodyDetector>();
            _leftDetector.IgnoreBody(_rightHand.Rigidbody);

            _rightDetector = _rightHand.Rigidbody.gameObject.AddComponent<RigidbodyDetector>();
            _rightDetector.IgnoreBody(_leftHand.Rigidbody);

            CurrentStepIndex = -1;
            EnableGrabbing(true);
        }

        public void Record()
        {
            ClearSnapshot();
            if (_timerControl != null)
            {
                _delayedSnapRoutine = StartCoroutine(DelayedSnapshot(_timerControl.DelaySeconds));
            }
            else
            {
                TakeSnapshot();
            }
        }

        private void ClearSnapshot()
        {
            if (_delayedSnapRoutine != null)
            {
                StopCoroutine(_delayedSnapRoutine);
                _delayedSnapRoutine = null;
            }
            _delayLabel.text = string.Empty;
        }

        private IEnumerator DelayedSnapshot(int seconds)
        {
            for (int i = seconds; i > 0; i--)
            {
                _delayLabel.text = i.ToString();
                WhenTimeStep?.Invoke();
                yield return _waitOneSeconds;
            }
            if (TakeSnapshot())
            {
                _delayLabel.text = "<size=10>Snap!";
                WhenSnapshot?.Invoke();
            }
            else
            {
                _delayLabel.text = "<size=10>Error";
                WhenError?.Invoke();
            }
            yield return _waitOneSeconds;
            _delayLabel.text = string.Empty;
        }

        private bool TakeSnapshot()
        {
            Rigidbody leftItem = FindNearestItem(_leftHand.Rigidbody, _leftDetector, out float leftDistance);
            Rigidbody rightItem = FindNearestItem(_rightHand.Rigidbody, _rightDetector, out float rightDistance);

            if (leftDistance < rightDistance
                && leftItem != null)
            {
                return Record(_leftHand.Hand, leftItem);
            }
            else if (rightItem != null)
            {
                return Record(_rightHand.Hand, rightItem);
            }

            Debug.LogError("No rigidbody detected near any hand");
            return false;
        }

        private Rigidbody FindNearestItem(Rigidbody handBody, RigidbodyDetector detector, out float bestDistance)
        {
            Vector3 referencePoint = handBody.worldCenterOfMass;
            float minDistance = float.PositiveInfinity;
            Rigidbody bestItem = null;
            foreach (Rigidbody item in detector.IntersectingBodies)
            {
                Vector3 point = item.worldCenterOfMass;
                float distance = Vector3.Distance(point, referencePoint);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestItem = item;
                }
            }

            bestDistance = minDistance;
            return bestItem;
        }

        public void Undo()
        {
            if (CurrentStepIndex < 0)
            {
                return;
            }
            _recorderSteps[CurrentStepIndex].ClearInteractable();
            CurrentStepIndex--;
        }

        public void Redo()
        {
            if (CurrentStepIndex + 1 >= _recorderSteps.Count)
            {
                return;
            }
            CurrentStepIndex++;
            RecorderStep recorderStep = _recorderSteps[CurrentStepIndex];
            AddHandGrabPose(recorderStep, out recorderStep.interactable, out HandGrabPose point);
            AttachGhost(point);
            _recorderSteps[CurrentStepIndex] = recorderStep;
        }

        public void EnableGrabbing(bool enable)
        {
            _grabbingEnabled = enable;
            if (enable)
            {
                WhenGrabAllowed?.Invoke();
            }
            else
            {
                WhenGrabDisallowed?.Invoke();
            }
        }

        private bool Record(IHand hand, Rigidbody item)
        {
            HandPose trackedHandPose = TrackedPose(hand);
            if (trackedHandPose == null)
            {
                Debug.LogError("Tracked Pose could not be retrieved", this);
                return false;
            }

            if (!hand.GetRootPose(out Pose handRoot))
            {
                Debug.LogError("Hand Root pose could not be retrieved", this);
                return false;
            }

            Pose gripPoint = item.transform.Delta(handRoot);
            RecorderStep recorderStep = new RecorderStep(trackedHandPose, gripPoint, item);
            AddHandGrabPose(recorderStep, out recorderStep.interactable, out HandGrabPose point);
            AttachGhost(point);

            int nextStep = CurrentStepIndex + 1;
            if (nextStep < _recorderSteps.Count)
            {
                _recorderSteps.RemoveRange(nextStep, _recorderSteps.Count - nextStep);
            }
            _recorderSteps.Add(recorderStep);
            CurrentStepIndex = _recorderSteps.Count - 1;

            return true;
        }

        private HandPose TrackedPose(IHand hand)
        {
            if (!hand.GetJointPosesLocal(out ReadOnlyHandJointPoses localJoints))
            {
                return null;
            }
            HandPose result = new HandPose(hand.Handedness);
            for (int i = 0; i < FingersMetadata.HAND_JOINT_IDS.Length; ++i)
            {
                HandJointId jointID = FingersMetadata.HAND_JOINT_IDS[i];
                result.JointRotations[i] = localJoints[jointID].rotation;
            }
            return result;
        }

        private void AddHandGrabPose(RecorderStep recorderStep,
            out HandGrabInteractable interactable, out HandGrabPose handGrabPose)
        {
            interactable = HandGrabInteractable.Create(recorderStep.Item.transform);
            if (recorderStep.Item.TryGetComponent(out Grabbable grabbable))
            {
                interactable.InjectOptionalPointableElement(grabbable);
            }
            HandGrabPoseData pointData = new HandGrabPoseData()
            {
                handPose = recorderStep.RawHandPose,
                scale = 1f,
                gripPose = recorderStep.GrabPoint,
            };
            handGrabPose = interactable.LoadHandGrabPose(pointData);
        }

        private void AttachGhost(HandGrabPose point)
        {
            if (_ghostProvider == null)
            {
                return;
            }
            HandGhost ghostPrefab = _ghostProvider.GetHand(point.HandPose.Handedness);
            HandGhost ghost = GameObject.Instantiate(ghostPrefab, point.transform);
            ghost.SetPose(point);
        }
    }

    public class RigidbodyDetector : MonoBehaviour
    {
        private HashSet<Rigidbody> _ignoredBodies = new HashSet<Rigidbody>();

        public List<Rigidbody> IntersectingBodies { get; private set; } = new List<Rigidbody>();

        public void IgnoreBody(Rigidbody body)
        {
            if (!_ignoredBodies.Contains(body))
            {
                _ignoredBodies.Add(body);
            }

            if (IntersectingBodies.Contains(body))
            {
                IntersectingBodies.Remove(body);
            }
        }

        public void UnIgnoreBody(Rigidbody body)
        {
            if (_ignoredBodies.Contains(body))
            {
                _ignoredBodies.Remove(body);
            }
        }

        private void OnTriggerEnter(Collider collider)
        {
            Rigidbody rigidbody = collider.attachedRigidbody;
            if (rigidbody == null || _ignoredBodies.Contains(rigidbody))
            {
                return;
            }
            if (!IntersectingBodies.Contains(rigidbody))
            {
                IntersectingBodies.Add(rigidbody);
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            Rigidbody rigidbody = collider.attachedRigidbody;
            if (rigidbody == null)
            {
                return;
            }
            if (IntersectingBodies.Contains(rigidbody))
            {
                IntersectingBodies.Remove(rigidbody);
            }
        }

    }
}
