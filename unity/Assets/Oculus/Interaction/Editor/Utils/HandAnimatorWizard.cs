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

using Oculus.Interaction.Input;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;

namespace Oculus.Interaction.HandGrab.Editor
{
    /// <summary>
    /// This wizard helps creating a set of fixed Animation Clips using HandTracking
    /// to be used in a skinned synthetic hand with an Animator.
    /// Assign a HandSkeletonVisual and click the buttons as you perform the relevant
    /// poses with your tracked hand. The output will be an Animator that can be directly
    /// used in a Skinned hand. Once you are done you can automatically create the opposite
    /// hand data by providing the strings internally used for differentiating the left and
    /// right transforms. (typically _l_ and _r_)
    /// Works great in conjunction with FromOVRControllerHandData.cs
    /// </summary>
    public class HandAnimatorWizard : ScriptableWizard
    {
        [SerializeField]
        private HandVisual _handVisual;
        [SerializeField]
        private string _folder = "GeneratedAnimations";
        [SerializeField]
        private string _controllerName = "HandController";

        [Space]
        [InspectorButton("RecordHandFist")]
        [SerializeField]
        private string _recordHandFist;
        [SerializeField]
        private AnimationClip _handFist;

        [InspectorButton("RecordHand3qtrFist")]
        [SerializeField]
        private string _recordHand3qtrFist;
        [SerializeField]
        private AnimationClip _hand3qtrFist;

        [InspectorButton("RecordHandMidFist")]
        [SerializeField]
        private string _recordHandMidFist;
        [SerializeField]
        private AnimationClip _handMidFist;

        [InspectorButton("RecordHandPinch")]
        [SerializeField]
        private string _recordHandPinch;
        [SerializeField]
        private AnimationClip _handPinch;

        [InspectorButton("RecordHandCap")]
        [SerializeField]
        private string _recordHandCap;
        [SerializeField]
        private AnimationClip _handCap;

        [InspectorButton("RecordThumbUp")]
        [SerializeField]
        private string _recordThumbUp;
        [SerializeField]
        private AnimationClip _thumbUp;

        [InspectorButton("RecordIndexPoint")]
        [SerializeField]
        private string _recordIndexPoint;
        [SerializeField]
        private AnimationClip _indexPoint;

        [Space]
        [InspectorButton("GenerateMasks")]
        [SerializeField]
        private string _generateMasks;
        [SerializeField]
        private AvatarMask _indexMask;
        [SerializeField]
        private AvatarMask _thumbMask;

        [Space]
        [InspectorButton("GenerateAnimatorAsset")]
        [SerializeField]
        private string _generateAnimator;

        [Space(40f)]
        [SerializeField]
        private string _handLeftPrefix = "_l_";
        [SerializeField]
        private string _handRightPrefix = "_r_";
        [InspectorButton("GenerateMirrorAnimatorAsset")]
        [SerializeField]
        private string _generateMirrorAnimator;

        private Transform Root => _handVisual.Joints[0].parent;

        private static readonly List<HandJointId> INDEX_MASK = new List<HandJointId>()
        {
            HandJointId.HandIndex1,
            HandJointId.HandIndex2,
            HandJointId.HandIndex3,
            HandJointId.HandIndexTip
        };

        private static readonly List<HandJointId> THUMB_MASK = new List<HandJointId>()
        {
            HandJointId.HandThumb0,
            HandJointId.HandThumb1,
            HandJointId.HandThumb2,
            HandJointId.HandThumb3,
            HandJointId.HandThumbTip
        };

        private const string FLEX_PARAM = "Flex";
        private const string PINCH_PARAM = "Pinch";

        [MenuItem("Oculus/Interaction/Hand Animator Generator")]
        private static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<HandAnimatorWizard>("Hand Animator Generator", "Close");
        }

        private void OnWizardCreate() { }

        private bool TryGetHand(out IHand hand)
        {
            hand = null;

            if (_handVisual == null)
            {
                return false;
            }

            if (_handVisual.Hand != null)
            {
                hand = _handVisual.Hand;
                return true;
            }

            System.Type targetObjectClassType = _handVisual.GetType();
            System.Reflection.FieldInfo field = targetObjectClassType.GetField("_hand",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                object value = field.GetValue(_handVisual);
                hand = value as IHand;
                return hand != null;
            }
            return false;
        }

        private void RecordHandFist()
        {
            _handFist = GenerateClipAsset("HandFist");
        }

        private void RecordHand3qtrFist()
        {
            _hand3qtrFist = GenerateClipAsset("Hand3qtrFist");
        }

        private void RecordHandMidFist()
        {
            _handMidFist = GenerateClipAsset("HandMidFist");
        }

        private void RecordHandPinch()
        {
            _handPinch = GenerateClipAsset("HandPinch");
        }

        private void RecordHandCap()
        {
            _handCap = GenerateClipAsset("HandCap");
        }

        private void RecordThumbUp()
        {
            _thumbUp = GenerateClipAsset("ThumbUp");
        }

        private void RecordIndexPoint()
        {
            _indexPoint = GenerateClipAsset("IndexPoint");
        }

        public void GenerateMasks()
        {
            _indexMask = GenerateMaskAsset(INDEX_MASK, "indexMask");
            _thumbMask = GenerateMaskAsset(THUMB_MASK, "thumbMask");
        }

        private void GenerateAnimatorAsset()
        {
            HandClips clips = new HandClips()
            {
                handFist = _handFist,
                hand3qtrFist = _hand3qtrFist,
                handMidFist = _handMidFist,
                handPinch = _handPinch,
                handCap = _handCap,
                thumbUp = _thumbUp,
                indexPoint = _indexPoint,

                indexMask = _indexMask,
                thumbMask = _thumbMask
            };

            GetHandPrefixes(out string prefix, out string mirrorPrefix);
            string path = GenerateAnimatorPath(prefix);
            CreateAnimator(path, clips);
        }

        private void GenerateMirrorAnimatorAsset()
        {
            AnimationClip handFist = GenerateMirrorClipAsset(_handFist);
            AnimationClip hand3qtrFist = GenerateMirrorClipAsset(_hand3qtrFist);
            AnimationClip handMidFist = GenerateMirrorClipAsset(_handMidFist);
            AnimationClip handPinch = GenerateMirrorClipAsset(_handPinch);
            AnimationClip handCap = GenerateMirrorClipAsset(_handCap);
            AnimationClip thumbUp = GenerateMirrorClipAsset(_thumbUp);
            AnimationClip indexPoint = GenerateMirrorClipAsset(_indexPoint);

            AvatarMask indexMask = GenerateMirrorMaskAsset(_indexMask);
            AvatarMask thumbMask = GenerateMirrorMaskAsset(_thumbMask);

            HandClips clips = new HandClips()
            {
                handFist = handFist,
                hand3qtrFist = hand3qtrFist,
                handMidFist = handMidFist,
                handPinch = handPinch,
                handCap = handCap,
                thumbUp = thumbUp,
                indexPoint = indexPoint,
                indexMask = indexMask,
                thumbMask = thumbMask
            };

            GetHandPrefixes(out string prefix, out string mirrorPrefix);
            string path = GenerateAnimatorPath(mirrorPrefix);
            CreateAnimator(path, clips);
        }

        private AnimationClip GenerateClipAsset(string title)
        {
            GetHandPrefixes(out string prefix, out string mirrorPrefix);
            AnimationClip clip = new AnimationClip();

            for (int i = (int)HandJointId.HandStart; i < (int)HandJointId.HandEnd; ++i)
            {
                Transform jointTransform = _handVisual.Joints[i];
                string path = GetGameObjectPath(jointTransform, Root);
                RegisterLocalPose(ref clip, jointTransform.GetPose(Space.Self), path);
            }

            StoreAsset(clip, $"{title}{prefix}.anim");
            return clip;
        }

        private AvatarMask GenerateMaskAsset(List<HandJointId> maskData, string title)
        {
            GetHandPrefixes(out string prefix, out string mirrorPrefix);
            AvatarMask mask = new AvatarMask();
            List<string> paths = new List<string>(maskData.Count);

            foreach (var maskJoints in maskData)
            {
                Transform jointTransform = _handVisual.Joints[(int)maskJoints];
                string localPath = GetGameObjectPath(jointTransform, Root);
                paths.Add(localPath);
            }

            mask.transformCount = paths.Count;
            for (int i = 0; i < paths.Count; ++i)
            {
                mask.SetTransformPath(i, paths[i]);
                mask.SetTransformActive(i, true);
            }

            StoreAsset(mask, $"{title}{prefix}.mask");
            return mask;
        }

        private AnimationClip GenerateMirrorClipAsset(AnimationClip originalClip)
        {
            if (originalClip == null)
            {
                Debug.LogError("Please generate a valid Clip first");
                return null;
            }
            GetHandPrefixes(out string prefix, out string mirrorPrefix);

            AnimationClip mirrorClip = new AnimationClip();

            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(originalClip);
            foreach (EditorCurveBinding curveBinding in curveBindings)
            {
                string mirrorPath = curveBinding.path.Replace(prefix, mirrorPrefix);
                AnimationCurve curve = AnimationUtility.GetEditorCurve(originalClip, curveBinding);
                float invertFactor = curveBinding.propertyName.Contains("LocalPosition") ? -1f : 1f;

                AnimationCurve mirrorCurve = new AnimationCurve();

                for (int i = 0; i < curve.length; i++)
                {
                    mirrorCurve.AddKey(curve[i].time, curve[i].value * invertFactor);
                }
                mirrorClip.SetCurve(mirrorPath, curveBinding.type, curveBinding.propertyName, mirrorCurve);
            }
            StoreAsset(mirrorClip, $"{originalClip.name.Replace(prefix, mirrorPrefix)}.anim");
            return mirrorClip;
        }

        private AvatarMask GenerateMirrorMaskAsset(AvatarMask originalMask)
        {
            if (originalMask == null)
            {
                Debug.LogError("Please generate a valid mask first");
                return null;
            }
            GetHandPrefixes(out string prefix, out string mirrorPrefix);

            AvatarMask mirrorMask = new AvatarMask();

            mirrorMask.transformCount = originalMask.transformCount;
            for (int i = 0; i < originalMask.transformCount; ++i)
            {
                string mirrorPath = originalMask.GetTransformPath(i).Replace(prefix, mirrorPrefix);
                bool active = originalMask.GetTransformActive(i);
                mirrorMask.SetTransformPath(i, mirrorPath);
                mirrorMask.SetTransformActive(i, active);
            }

            StoreAsset(mirrorMask, $"{originalMask.name.Replace(prefix, mirrorPrefix)}.mask");

            return mirrorMask;
        }

        private void RegisterLocalPose(ref AnimationClip clip, Pose pose, string path)
        {
            Vector3 euler = pose.rotation.eulerAngles;
            clip.SetCurve(path, typeof(Transform), "localEulerAngles.x", AnimationCurve.Constant(0f, 0.01f, euler.x));
            clip.SetCurve(path, typeof(Transform), "localEulerAngles.y", AnimationCurve.Constant(0f, 0.01f, euler.y));
            clip.SetCurve(path, typeof(Transform), "localEulerAngles.z", AnimationCurve.Constant(0f, 0.01f, euler.z));

            Vector3 pos = pose.position;
            clip.SetCurve(path, typeof(Transform), "localPosition.x", AnimationCurve.Constant(0f, 0.01f, pos.x));
            clip.SetCurve(path, typeof(Transform), "localPosition.y", AnimationCurve.Constant(0f, 0.01f, pos.y));
            clip.SetCurve(path, typeof(Transform), "localPosition.z", AnimationCurve.Constant(0f, 0.01f, pos.z));
        }

        private AnimatorController CreateAnimator(string path, HandClips clips)
        {
            if (!clips.IsComplete())
            {
                Debug.LogError("Missing clips and masks to generate the animator");
                return null;
            }
            AnimatorController animator = AnimatorController.CreateAnimatorControllerAtPath(path);

            animator.AddParameter(FLEX_PARAM, AnimatorControllerParameterType.Float);
            animator.AddParameter(PINCH_PARAM, AnimatorControllerParameterType.Float);

            animator.RemoveLayer(0);

            CreateLayer(animator, "Flex Layer", null);
            CreateLayer(animator, "Thumb Layer", clips.thumbMask);
            CreateLayer(animator, "Point Layer", clips.indexMask);

            CreateFlexStates(animator, 0, clips);
            CreateThumbUpStates(animator, 1, clips);
            CreatePointStates(animator, 2, clips);

            return animator;
        }

        private AnimatorControllerLayer CreateLayer(AnimatorController animator, string layerName, AvatarMask mask = null)
        {
            AnimatorControllerLayer layer = new AnimatorControllerLayer();
            layer.name = layerName;
            AnimatorStateMachine stateMachine = new AnimatorStateMachine();
            stateMachine.name = layer.name;
            AssetDatabase.AddObjectToAsset(stateMachine, animator);
            stateMachine.hideFlags = HideFlags.HideInHierarchy;
            layer.stateMachine = stateMachine;
            layer.avatarMask = mask;
            animator.AddLayer(layer);
            return layer;
        }

        private void CreateFlexStates(AnimatorController animator, int layerIndex, HandClips clips)
        {
            BlendTree blendTree;
            AnimatorState flexState = animator.CreateBlendTreeInController("Flex", out blendTree, layerIndex);
            blendTree.blendType = BlendTreeType.FreeformCartesian2D;
            blendTree.blendParameter = FLEX_PARAM;
            blendTree.blendParameterY = PINCH_PARAM;
            blendTree.AddChild(clips.handCap, new Vector2(0f, 0f));
            blendTree.AddChild(clips.handPinch, new Vector2(0f, 0.835f));
            blendTree.AddChild(clips.handPinch, new Vector2(0f, 1f));
            blendTree.AddChild(clips.handMidFist, new Vector2(0.5f, 0f));
            blendTree.AddChild(clips.handMidFist, new Vector2(0.5f, 1f));
            blendTree.AddChild(clips.hand3qtrFist, new Vector2(0.835f, 0f));
            blendTree.AddChild(clips.hand3qtrFist, new Vector2(0.835f, 1f));
            blendTree.AddChild(clips.handFist, new Vector2(1f, 0f));
            blendTree.AddChild(clips.handFist, new Vector2(1f, 1f));
            animator.layers[layerIndex].stateMachine.defaultState = flexState;
        }

        private void CreateThumbUpStates(AnimatorController animator, int layerIndex, HandClips clips)
        {
            if (clips.thumbUp == null)
            {
                Debug.LogError("No thumb clip provided");
                return;
            }
            AnimatorState thumbupState = animator.AddMotion(clips.thumbUp, layerIndex);
            animator.layers[layerIndex].stateMachine.defaultState = thumbupState;
        }

        private void CreatePointStates(AnimatorController animator, int layerIndex, HandClips clips)
        {
            BlendTree blendTree;
            AnimatorState flexState = animator.CreateBlendTreeInController("Point", out blendTree, layerIndex);
            blendTree.blendType = BlendTreeType.Simple1D;
            blendTree.blendParameter = FLEX_PARAM;
            blendTree.AddChild(clips.handCap, 0f);
            blendTree.AddChild(clips.indexPoint, 1f);
            blendTree.useAutomaticThresholds = true;
            animator.layers[layerIndex].stateMachine.defaultState = flexState;
        }

        private void StoreAsset(Object asset, string name)
        {
#if UNITY_EDITOR
            string targetFolder = Path.Combine("Assets", _folder);
            CreateFolder(targetFolder);
            string path = Path.Combine(targetFolder, name);
            AssetDatabase.CreateAsset(asset, path);
#endif
        }

        private void CreateFolder(string targetFolder)
        {
#if UNITY_EDITOR
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }
#endif
        }

        private string GenerateAnimatorPath(string prefix)
        {
            string targetFolder = Path.Combine("Assets", _folder);
            CreateFolder(targetFolder);
            string path = Path.Combine(targetFolder, $"{_controllerName}{prefix}.controller");
            return path;
        }

        private static string GetGameObjectPath(Transform transform, Transform root)
        {
            string path = transform.name;
            while (transform.parent != null
                && transform.parent != root)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }
            return path;
        }

        private void GetHandPrefixes(out string prefix, out string mirrorPrefix)
        {
            if (!TryGetHand(out IHand hand))
            {
                Debug.LogError("Hand not found");
                prefix = mirrorPrefix = string.Empty;
                return;
            }

            Handedness originalHandedness = hand.Handedness;
            prefix = originalHandedness == Handedness.Left ? _handLeftPrefix : _handRightPrefix;
            mirrorPrefix = originalHandedness == Handedness.Left ? _handRightPrefix : _handLeftPrefix;
        }

        private class HandClips
        {
            public AnimationClip handFist;
            public AnimationClip hand3qtrFist;
            public AnimationClip handMidFist;
            public AnimationClip handPinch;
            public AnimationClip handCap;
            public AnimationClip thumbUp;
            public AnimationClip indexPoint;

            public AvatarMask indexMask;
            public AvatarMask thumbMask;

            public bool IsComplete()
            {
                return handFist != null
                    && hand3qtrFist != null
                    && handMidFist != null
                    && handPinch != null
                    && handCap != null
                    && thumbUp != null
                    && indexPoint != null
                    && indexMask != null
                    && thumbMask != null;
            }
        }
    }
}
