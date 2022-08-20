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
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.HandGrab.Editor
{
    public class HandGrabPoseWizard : EditorWindow
    { /// <summary>
      /// The Hand being used for recording HandGrabPoses
      /// </summary>
        [SerializeField]
        private Hand _hand;
        [SerializeField]
        private int _handID = 0;
        public Hand Hand
        {
            get
            {
                if (_hand == null && _handID != 0)
                {
                    _hand = EditorUtility.InstanceIDToObject(_handID) as Hand;
                    _handID = 0;
                }
                return _hand;
            }
            set
            {
                _hand = value;
                if (_hand != null)
                {
                    _handID = value.GetInstanceID();
                }
                else
                {
                    _handID = 0;
                }
            }
        }

        /// <summary>
        /// The Gameobject that the user is recording the HandGrabPose for. e.g. a key
        /// </summary>
        [SerializeField]
        private Rigidbody _item;

        /// <summary>
        /// References the hand prototypes used to represent the HandGrabInteractables. These are the
        /// static hands placed around the interactable to visualize the different holding hand-poses.
        /// Not mandatory.
        /// </summary>
        [SerializeField]
        private HandGhostProvider _ghostProvider;

        /// <summary>
        /// This ScriptableObject stores the HandGrabInteractables generated at Play-Mode so it survives
        /// the Play-Edit cycle.
        /// Create a collection and assign it even in Play Mode and make sure to store here the
        /// interactables, then restore it in Edit-Mode to be serialized.
        /// </summary>
        [SerializeField]
        private HandGrabInteractableDataCollection _posesCollection;

        /// <summary>
        /// The keyboard key that can be pressed for recording a hand grab pose
        /// </summary>
        [SerializeField]
        private KeyCode _recordKey = KeyCode.Space;

        private GUIStyle _richTextStyle;
        private Vector2 _scrollPos = Vector2.zero;

        [MenuItem("Oculus/Interaction/Hand Grab Pose Recorder")]
        private static void CreateWizard()
        {
            HandGrabPoseWizard window = EditorWindow.GetWindow<HandGrabPoseWizard>();
            window.titleContent = new GUIContent("Hand Grab Pose Recorder");
            window.Show();
        }

        private void OnEnable()
        {
            _richTextStyle = EditorGUIUtility.GetBuiltinSkin(EditorGUIUtility.isProSkin ? EditorSkin.Scene : EditorSkin.Inspector).label;
            _richTextStyle.richText = true;
            _richTextStyle.wordWrap = true;
            if (_ghostProvider == null)
            {
                HandGhostProviderUtils.TryGetDefaultProvider(out _ghostProvider);
            }
        }

        private void OnGUI()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown
                && e.keyCode == _recordKey)
            {
                RecordPose();
                e.Use();
            }
            GUILayout.Label("Generate HandGrabPoses for grabbing an item <b>using your Hand in Play Mode</b>.\nThen Store and retrieve them in Edit Mode to persist and tweak them.", _richTextStyle);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            GUILayout.Space(20);
            GUILayout.Label("<size=20>1</size>\nAssign the hand that will be tracked and the item for which you want to record HandGrabPoses", _richTextStyle);
            GUILayout.Label("Hand used for recording poses:");
            Hand = EditorGUILayout.ObjectField(Hand, typeof(Hand), true) as Hand;
            GUILayout.Label("GameObject to record the hand grab poses for:");
            GenerateObjectField(ref _item);
            GUILayout.Label("Prefabs provider for the hands (ghosts) to visualize the recorded poses:");
            GenerateObjectField(ref _ghostProvider);

            GUILayout.Space(20);
            GUILayout.Label("<size=20>2</size>\nGo to <b>Play Mode</b> and record as many poses as you need.", _richTextStyle);
            GUILayout.Label($"Press the big <b>Record</b> button with your free hand\nor the <b>{_recordKey}</b> key to record a HandGrabPose <b>(requires focus on this window)</b>.", _richTextStyle);
            _recordKey = (KeyCode)EditorGUILayout.EnumPopup(_recordKey);
            if (GUILayout.Button("Record HandGrabPose", GUILayout.Height(100)))
            {
                RecordPose();
            }

            GUILayout.Space(20);
            GUILayout.Label("<size=20>3</size>\nStore your poses before exiting <b>Play Mode</b>.\nIf no collection is provided <b>it will autogenerate one</b>", _richTextStyle);
            GenerateObjectField(ref _posesCollection);
            if (GUILayout.Button("Save To Collection"))
            {
                SaveToAsset();
            }

            GUILayout.Space(20);
            GUILayout.Label("<size=20>4</size>\nNow load the poses from the PosesCollection in <b>Edit Mode</b> to tweak and persist them as gameobjects", _richTextStyle);
            if (GUILayout.Button("Load From Collection"))
            {
                LoadFromAsset();
            }
            GUILayout.EndScrollView();
        }

        private void GenerateObjectField<T>(ref T obj) where T : Object
        {
            obj = EditorGUILayout.ObjectField(obj, typeof(T), true) as T;
        }

        /// <summary>
        /// Finds the nearest object that can be snapped to and adds a new HandGrabInteractable to
        /// it with the user hand representation.
        /// </summary>
        public void RecordPose()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("Recording tracked hands only works in Play Mode!", this);
                return;
            }

            if (Hand == null)
            {
                Debug.LogError("Missing Hand reference.", this);
                return;
            }

            if (_item == null)
            {
                Debug.LogError("Missing recordable item", this);
                return;
            }

            HandPose trackedHandPose = TrackedPose();
            if (trackedHandPose == null)
            {
                Debug.LogError("Tracked Pose could not be retrieved", this);
                return;
            }

            if (!Hand.GetRootPose(out Pose handRoot))
            {
                Debug.LogError("Hand Root pose could not be retrieved", this);
                return;
            }

            Pose gripPoint = _item.transform.Delta(handRoot);
            HandGrabPose point = AddHandGrabPose(trackedHandPose, gripPoint);
            AttachGhost(point);
        }

        private HandPose TrackedPose()
        {
            if (!Hand.GetJointPosesLocal(out ReadOnlyHandJointPoses localJoints))
            {
                return null;
            }
            HandPose result = new HandPose(Hand.Handedness);
            for (int i = 0; i < FingersMetadata.HAND_JOINT_IDS.Length; ++i)
            {
                HandJointId jointID = FingersMetadata.HAND_JOINT_IDS[i];
                result.JointRotations[i] = localJoints[jointID].rotation;
            }
            return result;
        }

        private void AttachGhost(HandGrabPose point)
        {
            if (_ghostProvider == null)
            {
                return;
            }
            HandGhost ghostPrefab = _ghostProvider.GetHand(Hand.Handedness);
            HandGhost ghost = GameObject.Instantiate(ghostPrefab, point.transform);
            ghost.SetPose(point);
        }

        /// <summary>
        /// Creates a new HandGrabInteractable at the exact pose of a given hand.
        /// Mostly used with Hand-Tracking at Play-Mode
        /// </summary>
        /// <param name="rawPose">The user controlled hand pose.</param>
        /// <param name="snapPoint">The user controlled hand pose.</param>
        /// <returns>The generated HandGrabPose.</returns>
        private HandGrabPose AddHandGrabPose(HandPose rawPose, Pose snapPoint)
        {
            HandGrabInteractable interactable = HandGrabInteractable.Create(_item.transform);
            HandGrabPoseData pointData = new HandGrabPoseData()
            {
                handPose = rawPose,
                scale = 1f,
                gripPose = snapPoint,
            };
            return interactable.LoadHandGrabPose(pointData);
        }

        /// <summary>
        /// Creates a new HandGrabInteractable from the stored data.
        /// Mostly used to restore a HandGrabInteractable that was stored during Play-Mode.
        /// </summary>
        /// <param name="data">The data of the HandGrabInteractable.</param>
        /// <returns>The generated HandGrabInteractable.</returns>
        private HandGrabInteractable LoadHandGrabInteractable(HandGrabInteractableData data)
        {
            HandGrabInteractable interactable = HandGrabInteractable.Create(_item.transform);
            interactable.LoadData(data);
            return interactable;
        }

        /// <summary>
        /// Stores the interactables to a SerializedObject (the empty object must be
        /// provided in the inspector or one will be auto-generated). First it translates the HandGrabInteractable to a serialized
        /// form HandGrabbableData).
        /// This method is called from a button in the Inspector.
        /// </summary>
        private void SaveToAsset()
        {
            if (_posesCollection == null)
            {
                GenerateCollectionAsset();
            }
            List<HandGrabInteractableData> savedPoses = new List<HandGrabInteractableData>();
            foreach (HandGrabInteractable snap in _item.GetComponentsInChildren<HandGrabInteractable>(false))
            {
                savedPoses.Add(snap.SaveData());
            }
            _posesCollection.StoreInteractables(savedPoses);
        }

        /// <summary>
        /// Load the HandGrabInteractable from a Collection.
        /// This method is called from a button in the Inspector and will load the posesCollection.
        /// </summary>
        private void LoadFromAsset()
        {
            if (_posesCollection == null)
            {
                return;
            }

            foreach (HandGrabInteractableData handPose in _posesCollection.InteractablesData)
            {
                LoadHandGrabInteractable(handPose);
            }
        }

        public void GenerateCollectionAsset()
        {
            _posesCollection = ScriptableObject.CreateInstance<HandGrabInteractableDataCollection>();
            string parentDir = Path.Combine("Assets", "HandGrabInteractableDataCollection");
            if (!Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }
            string name = _item != null ? _item.name : "Auto";
            AssetDatabase.CreateAsset(_posesCollection, Path.Combine(parentDir, $"{name}_HandGrabCollection.asset"));
            AssetDatabase.SaveAssets();
        }
    }
}
