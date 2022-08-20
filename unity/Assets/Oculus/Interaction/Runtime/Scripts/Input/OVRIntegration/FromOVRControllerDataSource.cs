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
using UnityEngine.Assertions;

namespace Oculus.Interaction.Input
{
    struct UsageMapping
    {
        public UsageMapping(ControllerButtonUsage usage, OVRInput.Touch touch)
        {
            Usage = usage;
            Touch = touch;
            Button = OVRInput.Button.None;
        }

        public UsageMapping(ControllerButtonUsage usage, OVRInput.Button button)
        {
            Usage = usage;
            Touch = OVRInput.Touch.None;
            Button = button;
        }

        public bool IsTouch => Touch != OVRInput.Touch.None;
        public bool IsButton => Button != OVRInput.Button.None;
        public ControllerButtonUsage Usage { get; }
        public OVRInput.Touch Touch { get; }
        public OVRInput.Button Button { get; }
    }

    /// <summary>
    /// Returns the Pointer Pose for the active controller model
    /// as found in the official prefabs.
    /// This point is usually located at the front tip of the controller.
    /// </summary>
    struct OVRPointerPoseSelector
    {
        private static readonly Pose[] QUEST1_POINTERS = new Pose[2]
        {
            new Pose(new Vector3(-0.00779999979f,-0.00410000002f,0.0375000015f),
                Quaternion.Euler(359.209534f, 6.45196056f, 6.95544577f)),
            new Pose(new Vector3(0.00779999979f,-0.00410000002f,0.0375000015f),
                Quaternion.Euler(359.209534f, 353.548035f, 353.044556f))
        };

        private static readonly Pose[] QUEST2_POINTERS = new Pose[2]
        {
            new Pose(new Vector3(0.00899999961f, -0.00321028521f, 0.030869998f),
                Quaternion.Euler(359.209534f, 6.45196056f, 6.95544577f)),
            new Pose(new Vector3(-0.00899999961f, -0.00321028521f, 0.030869998f),
                Quaternion.Euler(359.209534f, 353.548035f, 353.044556f))
        };

        public Pose LocalPointerPose { get; private set; }

        public OVRPointerPoseSelector(Handedness handedness)
        {
            OVRPlugin.SystemHeadset headset = OVRPlugin.GetSystemHeadsetType();
            switch (headset)
            {
                case OVRPlugin.SystemHeadset.Oculus_Quest_2:
                case OVRPlugin.SystemHeadset.Oculus_Link_Quest_2:
                    LocalPointerPose = QUEST2_POINTERS[(int)handedness];
                    break;
                default:
                    LocalPointerPose = QUEST1_POINTERS[(int)handedness];
                    break;
            }
        }
    }

    public class FromOVRControllerDataSource : DataSource<ControllerDataAsset>
    {
        [Header("OVR Data Source")]
        [SerializeField, Interface(typeof(IOVRCameraRigRef))]
        private MonoBehaviour _cameraRigRef;
        public IOVRCameraRigRef CameraRigRef { get; private set; }

        [SerializeField]
        private bool _processLateUpdates = false;

        [Header("Shared Configuration")]
        [SerializeField]
        private Handedness _handedness;

        [SerializeField, Interface(typeof(ITrackingToWorldTransformer))]
        private MonoBehaviour _trackingToWorldTransformer;
        private ITrackingToWorldTransformer TrackingToWorldTransformer;

        [SerializeField, Interface(typeof(IDataSource<HmdDataAsset>))]
        private MonoBehaviour _hmdData;
        private IDataSource<HmdDataAsset> HmdData;

        public bool ProcessLateUpdates
        {
            get
            {
                return _processLateUpdates;
            }
            set
            {
                _processLateUpdates = value;
            }
        }

        private readonly ControllerDataAsset _controllerDataAsset = new ControllerDataAsset();
        private OVRInput.Controller _ovrController;
        private Transform _ovrControllerAnchor;
        private ControllerDataSourceConfig _config;

        private OVRPointerPoseSelector _pointerPoseSelector;

        #region OVR Controller Mappings

        // Mappings from Unity XR CommonUsage to Oculus Button/Touch.
        private static readonly UsageMapping[] ControllerUsageMappings =
        {
            new UsageMapping(ControllerButtonUsage.PrimaryButton, OVRInput.Button.One),
            new UsageMapping(ControllerButtonUsage.PrimaryTouch, OVRInput.Touch.One),
            new UsageMapping(ControllerButtonUsage.SecondaryButton, OVRInput.Button.Two),
            new UsageMapping(ControllerButtonUsage.SecondaryTouch, OVRInput.Touch.Two),
            new UsageMapping(ControllerButtonUsage.GripButton,
                OVRInput.Button.PrimaryHandTrigger),
            new UsageMapping(ControllerButtonUsage.TriggerButton,
                OVRInput.Button.PrimaryIndexTrigger),
            new UsageMapping(ControllerButtonUsage.MenuButton, OVRInput.Button.Start),
            new UsageMapping(ControllerButtonUsage.Primary2DAxisClick,
                OVRInput.Button.PrimaryThumbstick),
            new UsageMapping(ControllerButtonUsage.Primary2DAxisTouch,
                OVRInput.Touch.PrimaryThumbstick),
            new UsageMapping(ControllerButtonUsage.Thumbrest, OVRInput.Touch.PrimaryThumbRest)
        };

        #endregion

        protected void Awake()
        {
            TrackingToWorldTransformer = _trackingToWorldTransformer as ITrackingToWorldTransformer;
            HmdData = _hmdData as IDataSource<HmdDataAsset>;
            CameraRigRef = _cameraRigRef as IOVRCameraRigRef;

            UpdateConfig();
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            Assert.IsNotNull(CameraRigRef);
            Assert.IsNotNull(TrackingToWorldTransformer);
            Assert.IsNotNull(HmdData);
            if (_handedness == Handedness.Left)
            {
                Assert.IsNotNull(CameraRigRef.LeftController);
                _ovrControllerAnchor = CameraRigRef.LeftController;
                _ovrController = OVRInput.Controller.LTouch;
            }
            else
            {
                Assert.IsNotNull(CameraRigRef.RightController);
                _ovrControllerAnchor = CameraRigRef.RightController;
                _ovrController = OVRInput.Controller.RTouch;
            }
            _pointerPoseSelector = new OVRPointerPoseSelector(_handedness);

            UpdateConfig();
            this.EndStart(ref _started);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_started)
            {
                CameraRigRef.WhenInputDataDirtied += HandleInputDataDirtied;
            }
        }

        protected override void OnDisable()
        {
            if (_started)
            {
                CameraRigRef.WhenInputDataDirtied -= HandleInputDataDirtied;
            }

            base.OnDisable();
        }

        private void HandleInputDataDirtied(bool isLateUpdate)
        {
            if (isLateUpdate && !_processLateUpdates)
            {
                return;
            }
            MarkInputDataRequiresUpdate();
        }

        private ControllerDataSourceConfig Config
        {
            get
            {
                if (_config != null)
                {
                    return _config;
                }

                _config = new ControllerDataSourceConfig()
                {
                    Handedness = _handedness
                };

                return _config;
            }
        }

        private void UpdateConfig()
        {
            Config.Handedness = _handedness;
            Config.TrackingToWorldTransformer = TrackingToWorldTransformer;
            Config.HmdData = HmdData;
        }

        protected override void UpdateData()
        {
            _controllerDataAsset.Config = Config;
            var worldToTrackingSpace = CameraRigRef.CameraRig.transform.worldToLocalMatrix;
            Transform ovrController = _ovrControllerAnchor;

            _controllerDataAsset.IsDataValid = true;
            _controllerDataAsset.IsConnected =
                (OVRInput.GetConnectedControllers() & _ovrController) > 0;
            if (!_controllerDataAsset.IsConnected)
            {
                // revert state fields to their defaults
                _controllerDataAsset.IsTracked = default;
                _controllerDataAsset.ButtonUsageMask = default;
                _controllerDataAsset.RootPoseOrigin = default;
                return;
            }

            _controllerDataAsset.IsTracked = true;

            // Update button usages
            _controllerDataAsset.ButtonUsageMask = ControllerButtonUsage.None;
            OVRInput.Controller controllerMask = _ovrController;
            foreach (UsageMapping mapping in ControllerUsageMappings)
            {
                bool usageActive;
                if (mapping.IsTouch)
                {
                    usageActive = OVRInput.Get(mapping.Touch, controllerMask);
                }
                else
                {
                    Assert.IsTrue(mapping.IsButton);
                    usageActive = OVRInput.Get(mapping.Button, controllerMask);
                }

                if (usageActive)
                {
                    _controllerDataAsset.ButtonUsageMask |= mapping.Usage;
                }
            }

            // Update poses

            // Convert controller pose from world to tracking space.
            Pose worldRoot = new Pose(ovrController.position, ovrController.rotation);
            _controllerDataAsset.RootPose.position = worldToTrackingSpace.MultiplyPoint3x4(worldRoot.position);
            _controllerDataAsset.RootPose.rotation = worldToTrackingSpace.rotation * worldRoot.rotation;
            _controllerDataAsset.RootPoseOrigin = PoseOrigin.RawTrackedPose;


            // Convert controller pointer pose from local to tracking space.
            Pose pointerPose = PoseUtils.Multiply(worldRoot, _pointerPoseSelector.LocalPointerPose);
            _controllerDataAsset.PointerPose.position = worldToTrackingSpace.MultiplyPoint3x4(pointerPose.position);
            _controllerDataAsset.PointerPose.rotation = worldToTrackingSpace.rotation * pointerPose.rotation;
            _controllerDataAsset.PointerPoseOrigin = PoseOrigin.RawTrackedPose;

        }

        protected override ControllerDataAsset DataAsset => _controllerDataAsset;

        #region Inject

        public void InjectAllFromOVRControllerDataSource(UpdateModeFlags updateMode, IDataSource updateAfter,
            Handedness handedness, ITrackingToWorldTransformer trackingToWorldTransformer,
            IDataSource<HmdDataAsset> hmdData)
        {
            base.InjectAllDataSource(updateMode, updateAfter);
            InjectHandedness(handedness);
            InjectTrackingToWorldTransformer(trackingToWorldTransformer);
            InjectHmdData(hmdData);
        }

        public void InjectHandedness(Handedness handedness)
        {
            _handedness = handedness;
        }

        public void InjectTrackingToWorldTransformer(ITrackingToWorldTransformer trackingToWorldTransformer)
        {
            _trackingToWorldTransformer = trackingToWorldTransformer as MonoBehaviour;
            TrackingToWorldTransformer = trackingToWorldTransformer;
        }

        public void InjectHmdData(IDataSource<HmdDataAsset> hmdData)
        {
            _hmdData = hmdData as MonoBehaviour;
            HmdData = hmdData;
        }

        #endregion
    }
}
