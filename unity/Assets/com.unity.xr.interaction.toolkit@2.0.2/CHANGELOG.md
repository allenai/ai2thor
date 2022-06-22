# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

<!-- Headers should be listed in this order: Added, Changed, Deprecated, Removed, Fixed, Security -->
## [2.0.2] - 2022-04-29

### Fixed
- Fixed wrong offset when selecting an `XRGrabInteractable` with Track Rotation disabled when the Attach Transform had a different rotation than the Interactable's rotation. This configuration was not covered in the related fix made previously in version [2.0.0-pre.6](#200-pre6---2021-12-15). ([1361271](https://issuetracker.unity3d.com/product/unity/issues/guid/1361271))
- Fixed XR Socket Interactor hover mesh position and rotation for an XR Grab Interactable with Track Position and/or Track Rotation disabled.
- Fixed the simulated controllers not working in projects where the Scripting Backend was set to IL2CPP.
- Fixed the simulated HMD `deviceRotation` not being set. It now matches the `centerEyeRotation`.
- Fixed the **GameObject &gt; XR &gt; AR Annotation Interactable** menu item when AR Foundation is installed to add the correct component.
- Fixed **UIInputModule** so it uses and resets [`PointerEventData.useDragThreshold`](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.EventSystems.PointerEventData.html#UnityEngine_EventSystems_PointerEventData_useDragThreshold) to allow users to ignore the drag threshold by implementing [`IInitializePotentialDragHandler`](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.EventSystems.IInitializePotentialDragHandler.html). It was previously being ignored and causing sliders and scrollbars to incorrectly use a drag threshold.

## [2.0.1] - 2022-03-04

### Changed
- Changed the `XRI Default Input Actions` asset in the Starter Assets sample by removing the `primaryButton` bindings from Teleport Select and Teleport Mode Activate. If you want to restore the old behavior of both bindings, add an Up\Down\Left\Right Composite, reassign the Up composite part binding, and add the Sector interaction for that direction. The actions were also reorganized into additional Action Maps.

### Fixed
- Fixed regression introduced with version [2.0.0](#200---2022-02-16) so the hover mesh draws in the correct location when the Interactable's Attach Transform is not a child Transform or deep child Transform.
- Fixed the `XRI Default Input Actions` asset in the Starter Assets sample showing the warning "(Incompatible Value Type)" on the bindings for Teleport Select and Teleport Mode Activate by changing the action type from Button to Value with an expected control type of `Vector2`. The sample needs to be imported again if you already imported it into your project for you to see these changes.
- Fixed missing `UNITY_INCLUDE_TESTS` constraint in test assembly definition.

## [2.0.0] - 2022-02-16

### Added
- Added a warning message to the Inspector of `XRGrabInteractable` with non-uniformly scaled parent. A child `XRGrabInteractable` with non-uniformly scaled parent that is rotated relative to that parent may appear skewed when you grab it and then release it. See [Limitations with Non-Uniform Scaling](https://docs.unity3d.com/Manual/class-Transform.html). ([1228990](https://issuetracker.unity3d.com/product/unity/issues/guid/1228990))
- Added support for gamepad and joystick input when using the XR UI Input Module for more complete UGUI integration.

### Changed
- Changed sockets so selections are only maintained when exclusive. `XRSocketInteractor.CanSelect` changed so that sockets only maintain their selection when it is the sole interactor selecting the interactable. Previously, this was causing interactables that support multiple selection to not get released from the socket when grabbed by another interactor, which is not typically desired.
- Changed sockets so the hover mesh is positioned at the original attach transform pose for selected interactables. This fixes the case where the hover mesh would be at the wrong location when the attach transform is dynamically modified when an XR Grab Interactable is grabbed.
- Changed `XRDirectInteractor` and `XRSocketInteractor` by adding an `OnTriggerStay` method to fix an issue where those interactors did not detect when a Collider had exited in some cases where `OnTriggerExit` is not invoked, such as the Collider of the interactable being disabled. Users who had already implemented `OnTriggerStay` in derived classes will need to call the base method.
- Changed `GestureTransformationUtility.Raycast` default parameter value of `trackableTypes` from `TrackableType.All` to `TrackableType.AllTypes` to fix use of deprecated enum in AR Foundation 4.2. The new value includes `TrackableType.Depth`.
- Renamed the Default Input Actions sample to Starter Assets.
- Updated the manual to move most sections to separate pages.
- Moved some components from the **Component &gt; Scripts** menu into **Component &gt; XR**, **Component &gt; Event**, and **Component &gt; Input**.
- Changed `com.unity.xr.core-utils` dependency to 2.0.0.

### Fixed
- Fixed `XRDirectInteractor` and `XRSocketInteractor` still hovering an `XRGrabInteractable` after it was deactivated or destroyed.
- Fixed properties in event args for select and hover being incorrect when the same event is invoked again during the event due to the instance being reused for both. An object pool is now used by the `XRInteractionManager` to avoid the second event from overwriting the instance for the first event.
- GC.Alloc calls have been reduced: ray interactors with UI interaction disabled no longer allocate each frame, XR UI Input Module now avoids an allocating call, and AR gesture recognizers no longer re-allocate gestures when an old one is available.
- Fixed Editor classes improperly using `enumValueIndex` instead of `intValue` in some `SerializedProperty` cases. In practice, this bug did not affect users since the values matched in those cases.
- Fixed issue where `EventManager.current.IsPointerOverGameObject` would always return false when using `XRUIInputModule` for UI interaction. ([1387567](https://issuetracker.unity3d.com/product/unity/issues/guid/1387567))
- Fixed XR Tint Interactable Visual from clearing the tint in some cases when it was set to tint on both hover and selection. Also fixed the case when the interactable supports multiple selections so it only clears the tint when all selections end. It will now also set tint during `Awake` if needed.
- Fixed `ARTests` failing with Enhanced touches due to version upgrade of Input System.
- Fixed use of deprecated methods and enum values in `GestureTransformationUtility` when using AR Foundation 4.1 and 4.2.

## [2.0.0-pre.7] - 2022-01-31

### Fixed
- Fixed `ScriptableSettings` so it no longer logs to the console when creating the settings asset.

## [2.0.0-pre.6] - 2021-12-15

### Fixed
- Fixed wrong offset when selecting an `XRGrabInteractable` with Track Rotation disabled. ([1361271](https://issuetracker.unity3d.com/product/unity/issues/guid/1361271))
- Fixed `XRInteractorLineVisual` causing the error "Saving Prefab to immutable folder is not allowed". Also fixed the undo stack by no longer modifying the Line Renderer during `Reset`. ([1378651](https://issuetracker.unity3d.com/product/unity/issues/guid/1378651))
- Fixed UI interactions not clicking when simultaneously using multiple Ray Interactors. ([1336124](https://issuetracker.unity3d.com/product/unity/issues/guid/1336124))
- Fixed `Raycast Padding` of `Graphic` UI objects not being considered by `TrackedDeviceGraphicRaycaster`. ([1333300](https://issuetracker.unity3d.com/product/unity/issues/guid/1333300))
- Fixed `OnEndDrag` not being called on behaviors that implement `IEndDragHandler` when the mouse starts a drag, leaves the bounds of the object, and returns to the object without releasing the mouse button when using the `XRUIInputModule` upon finally releasing the mouse button.
- Fixed runtime crashing upon tapping the screen when using AR touch gestures in Unity 2021.2 in projects where the Scripting Backend was set to IL2CPP.
- Fixed `MissingReferenceException` caused by `XRBaseInteractable` when one of its Colliders was destroyed while it was hovering over a Direct Interactor or Socket Interactor.
- Fixed obsolete message for `XRControllerState.poseDataFlags` to reference the correct replacement field name.

## [2.0.0-pre.5] - 2021-11-17

### Fixed
- Fixed name of the profiler marker for `PreprocessInteractors`.

## [2.0.0-pre.4] - 2021-11-17

### Added
- Added ability to change the `MovementType` of an `XRGrabInteractable` while it is selected. The methods `SetupRigidbodyDrop` and `SetupRigidbodyGrab` will be invoked in this case, you can check if the `XRGrabInteractable` it's not selected or use the methods `Grab` and `Drop` to perform operations that should only occur once during the select state.

### Changed
- Changed so the Interaction Layer Mask check is done in the `XRInteractionManager` instead of within `XRBaseInteractor.CanSelect`/`XRBaseInteractable.IsSelectableBy` and `XRBaseInteractor.CanHover`/`XRBaseInteractable.IsHoverableBy`.
- Changed `com.unity.inputsystem` dependency from 1.0.2 to 1.2.0.
- Changed `com.unity.xr.core-utils` dependency from 2.0.0-pre.3 to 2.0.0-pre.5.
- Changed `com.unity.xr.legacyinputhelpers` dependency from 2.1.7 to 2.1.8.
- Changed `XRHelpURLConstants` from `public` to `internal`.

### Fixed
- Updated the property names of [XROrigin](https://docs.unity3d.com/Packages/com.unity.xr.core-utils@2.0/api/Unity.XR.CoreUtils.XROrigin.html) to adhere to PascalCase.

## [2.0.0-pre.3] - 2021-11-09

### Added
- Added XR Interaction Toolkit Settings to the **Edit &gt; Project Settings** window to allow for editing of the Interaction Layers. These settings are stored within a new `Assets/XRI` folder by default.
- Added a Select Mode property to Interactables that controls the number of Interactors that can select it at the same time. This allows Interactables that support it to be configured to allow multiple hands to interact with it at the same time. The Multiple option can be disabled in the Inspector window by adding `[CanSelectMultiple(false)]` to your component script.
- Added ability to double click a row in the XR Interaction Debugger window to select the Interactor or Interactable.
- Added the `ActionBasedController.trackingStateAction` property that allows users to bind the `InputTrackingState`. This new action is used when updating the controller's position and rotation. When not set, it falls back to the old behavior of using the tracked device's tracking state that is driving the position or rotation action.
- Added the interaction `float` value to the controller state. This will allow users to read the `float` value from `InteractionState`, not just the `bool` value, to drive visuals.
- Added methods to `XRBaseInteractor` and `XRBaseInteractable` to return the pose of the Attach Transform captured during the moment of selection (`GetAttachPoseOnSelect` and `GetLocalAttachPoseOnSelect`).
- Added a property to `XRBaseInteractor` and `XRBaseInteractable` to return the first interactor or interactable during the current select stack (`firstInteractableSelected` and `firstInteractorSelecting`).
- Added Allow Hovered Activate option to Ray Interactor and Direct Interactor to allow sending activate and deactivate events to interactables that the interactor is hovered over but not selected when there is no current selection. Override `GetActivateTargets(List<IXRActivateInteractable>)` to control which interactables can be activated.
- Added `teleporting` event to `BaseTeleportationInteractable` (`TeleportationAnchor`, `TeleportationArea`). Fires according to timing defined by that type's `teleportTrigger`.

### Changed
- Changed `ProcessInteractor` so that it is called after interaction events instead of before. Added a new `PreprocessInteractor` method to interactors which is called before interaction events. Scripts which used `ProcessInteractor` to compute valid targets should move that logic into `PreprocessInteractor` instead.
- Changed the signature of all methods with `XRBaseInteractor` or `XRBaseInteractable` parameters to instead take one of the new interfaces for Interactors (`IXRInteractor`, `IXRActivateInteractor`, `IXRHoverInteractor`, `IXRSelectInteractor`) or Interactables (`IXRInteractable`, `IXRActivateInteractable`, `IXRHoverInteractable`, `IXRSelectInteractable`). This change allows users to completely override and develop their own implementation of Interactors and Interactables instead of being required to derive from `XRBaseInteractor` or `XRBaseInteractable`.
  |Old Signature|New Signature|
  |---|---|
  |XRBaseInteractor<br/>`void GetValidTargets(List<XRBaseInteractable> targets)`|IXRInteractor<br/>`void GetValidTargets(List<IXRInteractable> targets)`|
  |XRBaseInteractor<br/>`bool CanHover(XRBaseInteractable interactable)`|IXRHoverInteractor<br/>`bool CanHover(IXRHoverInteractable interactable)`|
  |XRBaseInteractor<br/>`bool CanSelect(XRBaseInteractable interactable)`|IXRSelectInteractor<br/>`bool CanSelect(IXRSelectInteractable interactable)`|
  |XRBaseInteractable<br/>`bool IsHoverableBy(XRBaseInteractor interactor)`|IXRHoverInteractable<br/>`bool IsHoverableBy(IXRHoverInteractor interactor)`|
  |XRBaseInteractable<br/>`bool IsSelectableBy(XRBaseInteractor interactor)`|IXRSelectInteractable<br/>`bool IsSelectableBy(IXRSelectInteractor interactor)`|
  |BaseInteractionEventArgs<br/>`XRBaseInteractor interactor { get; set; }`<br/>`XRBaseInteractable interactable { get; set; }`|ActivateEventArgs and DeactivateEventArgs<br/>`IXRActivateInteractor interactorObject { get; set; }`<br/>`IXRActivateInteractable interactableObject { get; set; }`<br/><br/>HoverEnterEventArgs and HoverExitEventArgs<br/>`IXRHoverInteractor interactorObject { get; set; }`<br/>`IXRHoverInteractable interactableObject { get; set; }`<br/><br/>SelectEnterEventArgs and SelectExitEventArgs<br/>`IXRSelectInteractor interactorObject { get; set; }`<br/>`IXRSelectInteractable interactableObject { get; set; }`|
  ```csharp
  // Example Interactable that overrides an interaction event method.
  public class ExampleInteractable : XRBaseInteractable
  {
      // Old code
      protected override void OnSelectEntering(SelectEnterEventArgs args)
      {
          base.OnSelectEntering(args);
          XRBaseInteractor interactor = args.interactor;
          // Do something with interactor
      }

      // New code
      protected override void OnSelectEntering(SelectEnterEventArgs args)
      {
          base.OnSelectEntering(args);
          var interactor = args.interactorObject;
          // Do something with interactor
      }
  }

  // Example Interactor that overrides GetValidTargets.
  public class ExampleInteractor : XRRayInteractor
  {
      // Old code
      public override void GetValidTargets(List<XRBaseInteractable> targets)
      {
          base.GetValidTargets(targets);
          // Do additional filtering or prioritizing of Interactable candidates in targets list
      }

      // New code
      public override void GetValidTargets(List<IXRInteractable> targets)
      {
          base.GetValidTargets(targets);
          // Do additional filtering or prioritizing of Interactable candidates in targets list
      }
  }
  ```
- Changed Interactors and Interactables so they support having multiple selections, similarly to how they could have multiple components they were either hovering over or being hovered over by.
  |Old Pseudocode Snippets|New Pseudocode Snippets|
  |---|---|
  |`XRBaseInteractor.selectTarget != null`|`IXRSelectInteractor.hasSelection`|
  |`XRBaseInteractor.selectTarget`|`// Getting the first selected Interactable`<br/>`IXRSelectInteractor.hasSelection ? IXRSelectInteractor.interactablesSelected[0] : null`<br/>or<br/>`using System.Linq;`<br/>`IXRSelectInteractor.interactablesSelected.FirstOrDefault();`|
  |`var targets = new List<XRBaseInteractable>();`<br/>`XRBaseInteractor.GetHoverTargets(targets);`|`IXRHoverInteractor.interactablesHovered`|
  |`XRBaseInteractable.hoveringInteractors`|`IXRHoverInteractable.interactorsHovering`|
  |`XRBaseInteractable.selectingInteractor`|`IXRSelectInteractable.interactorsSelecting`|
  ```csharp
  // Example Interactor that overrides a predicate method.
  public class ExampleInteractor : XRBaseInteractor
  {
      // Old code
      public override bool CanSelect(XRBaseInteractable interactable)
      {
          return base.CanSelect(interactable) && (selectTarget == null || selectTarget == interactable);
      }

      // New code
      public override bool CanSelect(IXRSelectInteractable interactable)
      {
          return base.CanSelect(interactable) && (!hasSelection || IsSelecting(interactable));
      }
  }
  ```
- Changed `XRInteractionManager` methods `ClearInteractorSelection` and `ClearInteractorHover` from `public` to `protected`. These are invoked each frame automatically and were not intended to be called by external scripts.
- Changed behaviors that used the `attachTransform` property of `XRBaseInteractor` and `XRGrabInteractable` to instead use `IXRInteractor.GetAttachTransform(IXRInteractable)` and `IXRInteractable.GetAttachTransform(IXRInteractor)` when possible. Users can override the `GetAttachTransform` methods to customize which `Transform` should be used for a given Interactor or Interactable.
- Changed Interactor and Interactable interaction Layer checks to use the new `InteractionLayerMask` instead of the Unity physics `LayerMask`. Layers for the Interaction Layer Mask can be edited separately from Unity physics Layers. A migration tool was added to upgrade the field in all Prefabs and scenes. You will be prompted automatically after upgrading the package, and it can also be done at any time by opening **Edit &gt; Project Settings &gt; XR Interaction Toolkit** and clicking **Run Interaction Layer Mask Updater**.
- Changed Toggle and Sticky in Select Action Trigger so the toggled on state is now based on whether a selection actually occurred rather than whether there was simply a valid target. This means that a user that presses the select button while pointing at a valid target but one that can not be selected will no longer be in a toggled on state to select other interactables that can be selected.
- Changed Socket Interactor so the hover mesh can appear for all valid Interactable components, not just Grab Interactable components.
- Changed `XRRayInteractor.TranslateAnchor` so the Ray Origin Transform is passed instead of the Original Attach Transform, and renamed the parameter from `originalAnchor` to `rayOrigin`.
- Changed `HoverEnterEventArgs`, `HoverExitEventArgs`, `SelectEnterEventArgs`, and `SelectExitEventArgs` by adding a `manager` property of type `XRInteractionManager`.
- Changed minimum supported version of the Unity Editor from 2019.3 to 2019.4 (LTS).

### Deprecated
- Deprecated `XRRig` which was replaced by [XROrigin](https://docs.unity3d.com/Packages/com.unity.xr.core-utils@2.0/api/Unity.XR.CoreUtils.XROrigin.html) in a new dependent package [XR Core Utilities](https://docs.unity3d.com/Packages/com.unity.xr.core-utils@2.0/manual/index.html). `XROrigin` combines the functionality of `XRRig` and `ARSessionOrigin`.
- Deprecated `XRBaseInteractor.requireSelectExclusive` which was used by `XRSocketInteractor`. That logic was moved into `CanSelect` by utilizing the `isSelected` property of the interactable.
- Deprecated `XRRayInteractor.originalAttachTransform` and replaced with `rayOriginTransform`. The original pose of the Attach Transform can now be obtained with new methods (`GetAttachPoseOnSelect` and `GetLocalAttachPoseOnSelect`).
- Deprecated `GetControllerState` and `SetControllerState` from the `XRBaseController`. That logic was moved into the `currentControllerState` property.
- Deprecated `XRControllerState.poseDataFlags` due to being replaced by the new field `XRControllerState.inputTrackingState` to track the controller pose state.
- Deprecated the `XRControllerState` constructor; the `inputTrackingState` parameter is now required.
- Deprecated `AddRecordingFrame(double, Vector3, Quaternion, bool, bool, bool)` in the `XRControllerRecording`; use `AddRecordingFrame(XRControllerState)` or `AddRecordingFrameNonAlloc` instead.

### Fixed
- Fixed Teleportation Areas and Anchors causing undesired teleports when two different Ray Interactors are pointed at them by setting their default Select Mode to Multiple. By default, a teleport would be triggered On Select Exit, but that would occur when each Ray Interactor would take selection. Users with existing projects should change the Select Mode to Multiple.
- Fixed Sockets sometimes showing either the wrong hover mesh or appearing while selected for a single frame when the selection state changed that frame.
- Fixed Sockets sometimes showing the hover mesh for a single frame after another Interactor would take its selection when the Recycle Delay Time should have suppressed it from appearing.
- Fixed controller's recording serialization losing data when restarting the Unity Editor.
- Fixed releasing `XRGrabInteractable` objects after a teleport from having too much energy.
- Fixed `pixelDragThresholdMultiplier` not being squared when calculating the threshold in `UIInputModule`. To keep the same drag threshold you should update the `Tracked Device Drag Threshold Multiplier` property of your `XRUIInputModule` (and your subclasses of `UIInputModule`) to its square root in the Inspector window; for example, a value of `2` should be changed to `1.414214` (or `sqrt(2)`). ([1348680](https://issuetracker.unity3d.com/product/unity/issues/guid/1348680))

## [2.0.0-pre.2] - 2021-11-04

### Changed
- Changed package version for internal release.

## [2.0.0-pre.1] - 2021-10-22

### Changed
- Changed package version for internal release.

## [1.0.0-pre.8] - 2021-10-26

### Changed
- Changed the setter of `XRBaseInteractable.selectingInteractor` from `private` to `protected`.

### Fixed
- Fixed `XRBaseController` so its default `XRControllerState` is no longer constructed with a field initializer to avoid allocation when not needed, such as when it is replaced with `SetControllerState`.
- Fixed `XRUIInputModule` not processing clicks properly when using simulated touches, such as when using the Device Simulator view. This change means mouse input is not processed when there are touches, matching the behavior of other modules like the Standalone Input Module.
- Fixed Direct Interactor logging a warning about not having a required trigger Collider when it has a Rigidbody.
- Fixed missing dependency on `com.unity.modules.physics`.
- Fixed the sort order of ray casts returned by `TrackedDevicePhysicsRaycaster.Raycast` so that distance is in ascending order (closest first). It was previously returning in descending order (furthest first). In practice, this bug did not affect users since `EventSystem.RaycastAll` would correct the order.

## [1.0.0-pre.6] - 2021-09-10

### Changed
- Changed `ARGestureInteractor.GetValidTargets` to no longer filter out Interactable objects based on the camera direction. The optimization method used was faulty and could cause Interactable objects that were still visible to be excluded from the list. ([1354009](https://issuetracker.unity3d.com/product/unity/issues/guid/1354009))

### Fixed
- Fixed Tracked Device Physics Raycaster so it will include ray cast hits for GameObjects that did not have an event handler. This bug was causing events like `IPointerEnterHandler.OnPointerEnter` to not be invoked when the hit was on a child Collider that did not itself have an event handler. ([1356459](https://issuetracker.unity3d.com/product/unity/issues/guid/1356459))
- Fixed `XRBaseInteractable.isHovered` so it only gets set to `false` when all Interactors exit hovering. It was previously getting set to `false` when any Interactor would exit hovering even if another Interactor was still hovering.
- Fixed use of obsolete properties in `TrackedPoseDriver` when using Input System package version 1.1.0-pre.6 or newer.
- Fixed the Default Input Actions sample to be compatible with Input System package version 1.1.0 by merging the two bindings for the Turn action into one binding with both Sector interactions.
- Fixed the Socket Interactor hover mesh not matching the actual pose the Grab Interactable would attach to in the case when its attach transform was offset or rotated. Also fixed the pose of child meshes. ([1358567](https://issuetracker.unity3d.com/product/unity/issues/guid/1358567))
- Fixed Interactable objects not being considered valid targets for Direct and Socket Interactors when the Interactable was registered after it had entered the trigger collider of the Interactor. Note that Unity rules for [Colliders](https://docs.unity3d.com/Manual/CollidersOverview.html) and [OnTriggerEnter](https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnTriggerEnter.html)/[OnTriggerExit](https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnTriggerExit.html) still applies where the Interactable GameObject being deactivated and then moved will cause the Interactor to miss the trigger enter/exit event. If the object is manipulated in that way, those trigger methods need to be manually called to inform the Direct or Socket Interactor. ([1340469](https://issuetracker.unity3d.com/product/unity/issues/guid/1340469))
- Fixed the Trigger Pressed and Grip Pressed buttons not working on the XR Controller (Device-based). They were also renamed to Trigger Button and Grip Button to match the corresponding `CommonUsages` name.

## [1.0.0-pre.5] - 2021-08-02

### Added
- Added public events to `UIInputModule` which correspond to calls to `EventSystem.Execute` and `EventSystem.ExecuteHierarchy` to allow the events to be globally handled.
- Added profiler markers to `XRInteractionManager` to help with performance analysis.
- Added ability for the Animation and Physics 2D built-in packages to be optional.

### Changed
- Changed `XRBaseInteractable.GetDistanceSqrToInteractor` to not consider disabled Colliders or Colliders on disabled GameObjects. This logic is used by `XRDirectInteractor` and `XRSocketInteractor` to find the closest interactable to select.

### Fixed
- Fixed poor performance scaling of `XRInteractionManager` as the number of valid targets and hover targets of an Interactor increased. AR projects with hundreds of gesture interactables should see a large speedup.
- Fixed AR Gesture Recognizers producing GC allocations each frame when there were no touches.
- Fixed issue involving multiple Interactables that reference the same Collider in their Colliders list. Unregistering an Interactable will now only cause the Collider association to be removed from the `XRInteractionManager` if it's actually associated with that same Interactable.
- Fixed the Inspector showing a warning about a missing XR Controller when the Interactor is able to find one on a parent GameObject.

## [1.0.0-pre.4] - 2021-05-14

### Added
- Added Tracked Device Physics Raycaster component to enable physics-based UI interaction through Unity's Event System. This is similar to Physics Raycaster from the Unity UI package, but with support for ray casts from XR Controllers.
- Added `finalizeRaycastResults` event to `UIInputModule` that allows a callback to modify ray cast results before they are used by the event system.
- Added column to XR Interaction Debugger to show an Interactor's valid targets from `XRBaseInteractor.GetValidTargets`.
- Added property to XR Controller to allow the model to be set to a child object instead of forcing it to be instantiated from prefab.

### Changed
- Changed Grab Interactable to have a consistent attach point between all Movement Type values, fixing it not attaching at the Attach Transform when using Instantaneous when the object's Transform position was different from the Rigidbody's center of mass. To use the old method of determining the attach point in order to avoid needing to modify the Attach Transform for existing projects, set Attach Point Compatibility Mode to Legacy. Legacy mode will be removed in a future version. ([1294410](https://issuetracker.unity3d.com/product/unity/issues/guid/1294410))
- Changed Grab Interactable to also set the Rigidbody to kinematic upon being grabbed when the Movement Type is Instantaneous, not just when Kinematic. This improves how it collides with other Rigidbody objects.
- Changed Grab Interactable to allow its Attach Transform to be updated while grabbed instead of only using its pose at the moment of being grabbed. This requires not using Legacy mode.
- Changed Grab Interactable to no longer use the scale of the selecting Interactor's Attach Transform. This often caused unintended offsets when grabbing objects. The position of the Attach Transform should be used for this purpose rather than the scale. Projects that depended on that functionality can use Legacy mode to revert to the old method.
- Changed Grab Interactable default Movement Type from Kinematic to Instantaneous.
- Changed Grab Interactable default values for damping and scale so Velocity Tracking moves more similar to the other Movement Type values, making the distinguishing feature instead be how it collides with other Colliders without Rigidbody components. Changed `velocityDamping` from 0.4 to 1, `angularVelocityDamping` from 0.4 to 1, and `angularVelocityScale` from 0.95 to 1.
- Changed Socket Interactor override of the Movement Type of Interactables from Kinematic to Instantaneous.
- Changed XR Controller so it does not modify the Transform position, rotation, or scale of the instantiated model prefab upon startup instead of resetting those values.
- Changed Controller Interactors to let the XR Controller be on a parent GameObject.
- Changed so XR Interaction Debugger's Input Devices view is off by default.
- Changed Tracked Device Graphic Raycaster to fallback to using `Camera.main` when the Canvas does not have an Event Camera set.
- Changed XR Rig property for the Tracking Origin Mode to only contain supported modes. A value of Not Specified will use the default mode of the XR device.
- Changed **GameObject &gt; XR** menu to only have a single XR Rig rather than separate menu items for Room-Scale and Stationary. Change the Tracking Origin Mode property on the created XR Rig to Floor or Device, respectively, for the same behavior as before.

### Deprecated
- Deprecated `XRBaseController.modelTransform` due to being renamed to `XRBaseController.modelParent`.
- Deprecated `XRRig.trackingOriginMode` due to being replaced with an enum type that only contains supported modes. Use `XRRig.requestedTrackingOriginMode` and `XRRig.currentTrackingOriginMode` instead.

### Fixed
- Fixed Interaction Manager throwing exception `InvalidOperationException: Collection was modified; enumeration operation may not execute.` when an Interactor or Interactable was registered or unregistered during processing and events.
- Fixed Windows Mixed Reality controllers having an incorrect pose when using the Default Input Actions sample. The Position and Rotation input actions will try to bind to `pointerPosition` and `pointerRotation`, and fallback to `devicePosition` and `deviceRotation`. If the sample has already been imported into your project, you will need to import again to get the update.
- Fixed Input System actions such as Select not being recognized as pressed in `ActionBasedController` when it was bound to an Axis control (for example '<XRController>/grip') rather than a Button control (for example '<XRController>/gripPressed').
- Fixed XR Interaction Debugger to display Interactors and Interactables from multiple Interaction Managers.
- Fixed XR Interaction Debugger having overlapping text when an Interactor was hovering over multiple Interactables.
- Fixed Tree View panels in the XR Interaction Debugger to be collapsible.
- Fixed `TestFixture` classes in the test assembly to be `internal` instead of `public`.
- Fixed Grab Interactable to use scaled time for easing and smoothing instead of unscaled time.
- Fixed Direct and Socket Interactor not being able to interact with an Interactable with multiple Colliders when any of the Colliders leaves the trigger instead of only when all of them leave. ([1325375](https://issuetracker.unity3d.com/product/unity/issues/guid/1325375))
- Fixed Direct and Socket Interactor not being able to interact with an Interactable when either were registered after the trigger collision occurred.
- Fixed `XRSocketInteractor` to include the select target in its list of valid targets returned by `GetValidTargets`.
- Fixed `XRBaseController` so it applies the controller state during Before Render even when Input Tracking is disabled.
- Fixed missing namespace of `InputHelpers` to be `UnityEngine.XR.Interaction.Toolkit`.

## [1.0.0-pre.3] - 2021-03-18

### Added
- Added ability for serialized fields added in derived behaviors to automatically appear in the Inspector. Users will no longer need to create a custom [Editor](https://docs.unity3d.com/ScriptReference/Editor.html) to be able to see those fields in the Inspector. See [Extending the XR Interaction Toolkit](../manual/extending-xri.html) in the manual for details about customizing how they are drawn.
- Added support for `EnhancedTouch` from the Input System for AR gesture classes. This means AR interaction is functional when the Active Input Handling project setting is set to Input System Package (New).
- Added registration events to `XRBaseInteractable` and `XRBaseInteractor` which work like those in `XRInteractionManager` but for just that object.
- Added new methods in `ARPlacementInteractable` to divide the logic in `OnEndManipulation` into `TryGetPlacementPose`, `PlaceObject`, and `OnObjectPlaced`.
- Added `XRRayInteractor.hitClosestOnly` property to limit the number of valid targets. Enable this to make only the closest Interactable receive hover events rather than all Interactables in the full length of the ray cast.
- Added new methods in `XRRayInteractor` for getting information about UI hits, and made more methods `virtual` or `public`.
- Added several properties to Grab Interactable (Damping and Scale) to allow for tweaking the velocity and angular velocity when the Movement Type is Velocity Tracking. These values can be adjusted to reduce oscillation and latency from the Interactor.

### Changed
- Changed script execution order so `LocomotionProvider` occurs before Interactors are processed, fixing Ray Interactor from casting with stale controller poses when moving or turning the rig and causing visual flicker of the line.
- Changed script execution order so `XRUIInputModule` processing occurs after `LocomotionProvider` and before Interactors are processed to fix the frame delay with UI hits due to using stale ray cast rays. `XRUIInputModule.Process` now does nothing, override `XRUIInputModule.DoProcess` which is called directly from `Update`.
- Changed `XRUIInputModule.DoProcess` from `abstract` to `virtual`. Overriding methods in derived classes should call `base.DoProcess` to ensure `IUpdateSelectedHandler` event sending occurs as before.
- Changed Ray Interactor's Reference Frame property to use global up as a fallback when not set instead of the Interactor's up.
- Changed Ray Interactor Projectile Curve to end at ground height rather than controller height. Additional Ground Height and Additional Flight Time properties can be adjusted to control how long the curve travels, but this change means the curve will be longer than it was in previous versions.
- Changed `TrackedDeviceGraphicRaycaster` to ignore Trigger Colliders by default when checking for 3D occlusion. Added `raycastTriggerInteraction` property to control this.
- Changed `XRBaseInteractor.allowHover` and `XRBaseInteractor.allowSelect` to retain their value instead of getting changed to `true` during `OnEnable`. Their initial values are unchanged, remaining `true`.
- Changed some AR behaviors to be more configurable rather than using some hardcoded values or requiring using MainCamera. AR Placement Interactable and AR Translation Interactable must now specify a Fallback Layer Mask to support non-trackables instead of always using Layer 9.
- Changed `IUIInteractor` to not inherit from `ILineRenderable`.

### Deprecated
- Deprecated `XRBaseInteractor.enableInteractions`, use `XRBaseInteractor.allowHover` and `XRBaseInteractor.allowSelect` instead.

### Removed
- Removed several MonoBehaviour message functions in AR behaviors to use `ProcessInteractable` and `ProcessInteractor` instead.

### Fixed
- Fixed issue where the end of a Projectile or Bezier Curve lags behind and appears bent when the controller is moved too fast. ([1291060](https://issuetracker.unity3d.com/product/unity/issues/guid/1291060))
- Fixed Ray Interactor interacting with Interactables that are behind UI. ([1312217](https://issuetracker.unity3d.com/product/unity/issues/guid/1312217))
- Fixed `XRRayInteractor.hoverToSelect` not being functional. ([1301630](https://issuetracker.unity3d.com/product/unity/issues/guid/1301630))
- Fixed Ray Interactor not allowing for valid targets behind an Interactable with multiple Collider objects when the ray hits more than one of those Colliders.
- Fixed Ray Interactor performance to only perform ray casts once per frame instead of each time `GetValidTargets` is called by doing it during `ProcessInteractor` instead.
- Fixed exception in `XRInteractorLineVisual` when changing the Sample Frequency or Line Type of a Ray Interactor.
- Fixed Ray Interactor anchor control rotation when the Rig plane was not up. Added a property `anchorRotateReferenceFrame` to control the rotation axis.
- Fixed Reference Frame missing from the Ray Interactor Inspector when the Line Type was Bezier Curve.
- Fixed mouse scroll amount being too large in `XRUIInputModule` when using Input System.
- Fixed Scrollbar initially scrolling to incorrect position at XR pointer down when using `TrackedDeviceGraphicRaycaster`, which was caused by `RaycastResult.screenPosition` never being set.
- Fixed `GestureRecognizer` skipping updating some gestures during the same frame when another gesture finished.
- Fixed namespace of several Editor classes to be in `UnityEditor.XR.Interaction.Toolkit` instead of `UnityEngine.XR.Interaction.Toolkit`.
- Fixed default value of Blocking Mask on Tracked Device Graphic Raycaster to be Everything (was skipping Layer 31).

## [1.0.0-pre.2] - 2021-01-20

### Added
- Added registration events to `XRInteractionManager` and `OnRegistered`/`OnUnregistered` methods to `XRBaseInteractable` and `XRBaseInteractor`.
- Added and improved XML documentation comments and tooltips.
- Added warnings to XR Controller (Action-based) when referenced Input Actions have not been enabled.
- Added warning to Tracked Device Graphic Raycaster when the Event Camera is not set on the World Space Canvas.

### Changed
- Changed `XRBaseInteractable` and `XRBaseInteractor` to no longer register with `XRInteractionManager` in `Awake` and instead register and unregister in `OnEnable` and `OnDisable`, respectively.
- Changed the signature of all interaction event methods (e.g. `OnSelectEntering`) to take event data through a class argument rather than being passed the `XRBaseInteractable` or `XRBaseInteractor` directly. This was done to allow for additional related data to be provided by the Interaction Manager without requiring users to handle additional methods. This also makes it easier to handle the case when the selection or hover is canceled (due to either the Interactor or Interactable being unregistered as a result of being disabled or destroyed) without needing to duplicate code in an `OnSelectCanceling` and `OnSelectCanceled`.
  |Old Signature|New Signature|
  |---|---|
  |`OnHoverEnter*(XRBaseInteractor interactor)`<br/>-and-<br/>`OnHoverEnter*(XRBaseInteractable interactable)`|`OnHoverEnter*(HoverEnterEventArgs args)`|
  |`OnHoverExit*(XRBaseInteractor interactor)`<br/>-and-<br/>`OnHoverExit*(XRBaseInteractable interactable)`|`OnHoverExit*(HoverExitEventArgs args)`|
  |`OnSelectEnter*(XRBaseInteractor interactor)`<br/>-and-<br/>`OnSelectEnter*(XRBaseInteractable interactable)`|`OnSelectEnter*(SelectEnterEventArgs args)`|
  |`OnSelectExit*(XRBaseInteractor interactor)`<br/>-and-<br/>`OnSelectExit*(XRBaseInteractable interactable)`|`OnSelectExit*(SelectExitEventArgs args)` and using `!args.isCanceled`|
  |`OnSelectCancel*(XRBaseInteractor interactor)`|`OnSelectExit*(SelectExitEventArgs args)` and using `args.isCanceled`|
  |`OnActivate(XRBaseInteractor interactor)`|`OnActivated(ActivateEventArgs args)`|
  |`OnDeactivate(XRBaseInteractor interactor)`|`OnDeactivated(DeactivateEventArgs args)`|
  ```csharp
  // Example Interactable that overrides an interaction event method.
  public class ExampleInteractable : XRBaseInteractable
  {
      // Old code -- delete after migrating to new method signature
      protected override void OnSelectEntering(XRBaseInteractor interactor)
      {
          base.OnSelectEntering(interactor);
          // Do something with interactor
      }

      // New code
      protected override void OnSelectEntering(SelectEnterEventArgs args)
      {
          base.OnSelectEntering(args);
          var interactor = args.interactor;
          // Do something with interactor
      }
  }

  // Example behavior that is the target of an Interactable Event set in the Inspector with a Dynamic binding.
  public class ExampleListener : MonoBehaviour
  {
      // Old code -- delete after migrating to new method signature and fixing reference in Inspector
      public void OnSelectEntered(XRBaseInteractor interactor)
      {
          // Do something with interactor
      }

      // New code
      public void OnSelectEntered(SelectEnterEventArgs args)
      {
          var interactor = args.interactor;
          // Do something with interactor
      }
  }
  ```
- Changed which methods are called by the Interaction Manager when either the Interactor or Interactable is unregistered. Previously `XRBaseInteractable` had `OnSelectCanceling` and `OnSelectCanceled` called on select cancel, and `OnSelectExiting` and `OnSelectExited` called when not canceled. This has been combined into `OnSelectExiting(SelectExitEventArgs)` and `OnSelectExited(SelectExitEventArgs)` and the `isCanceled` property is used to distinguish as needed. The **Select Exited** event in the Inspector is invoked in either case.
  ```csharp
  public class ExampleInteractable : XRBaseInteractable
  {
      protected override void OnSelectExiting(SelectExitEventArgs args)
      {
          base.OnSelectExiting(args);
          // Do something common to both.
          if (args.isCanceled)
              // Do something when canceled only.
          else
              // Do something when not canceled.
      }

  }
  ```
- Changed many custom Editors to also apply to child classes so they inherit the custom layout of the Inspector. If your derived class adds a `SerializeField` or public field, you will need to create a custom [Editor](https://docs.unity3d.com/ScriptReference/Editor.html) to be able to see those fields in the Inspector. For Interactor and Interactable classes, you will typically only need to override the `DrawProperties` method in `XRBaseInteractorEditor` or `XRBaseInteractableEditor` rather than the entire `OnInspectorGUI`. See [Extending the XR Interaction Toolkit](../manual/extending-xri.html) in the manual for a code example.
- Changed `XRInteractionManager.SelectCancel` to call `OnSelectExiting` and `OnSelectExited` on both the `XRBaseInteractable` and `XRBaseInteractor` in a similar interleaved order to other interaction state changes and when either is unregistered.
- Changed order of `XRInteractionManager.UnregisterInteractor` to first cancel the select state before canceling hover state for consistency with the normal update loop which exits select before exiting hover.
- Changed `XRBaseInteractor.StartManualInteraction` and `XRBaseInteractor.EndManualInteraction` to go through `XRInteractionManager` rather than bypassing constraints and events on the Interactable.
- Changed the **GameObject > XR > Grab Interactable** menu item to create a visible cube and use a Box Collider so that it is easier to use.
- Renamed `LocomotionProvider.startLocomotion` to `LocomotionProvider.beginLocomotion` for consistency with method name.

### Fixed
- Fixed Direct Interactor and Socket Interactor causing exceptions when a valid target was unregistered, such as from being destroyed.
- Fixed Ray Interactor clearing custom direction when initializing (fixed initialization of the Original Attach Transform so it copies values from the Attach Transform instead of setting position and rotation values to defaults). ([1291523](https://issuetracker.unity3d.com/product/unity/issues/guid/1291523))
- Fixed Socket Interactor so only an enabled Renderer is drawn while drawing meshes for hovered Interactables.
- Fixed Grab Interactable to respect Interaction Layer Mask for whether it can be hovered by an Interactor instead of always allowing it.
- Fixed Grab Interactable so it restores the Rigidbody's drag and angular drag values on drop.
- Fixed mouse input not working with Unity UI when Active Input Handling was set to Input System Package.
- Fixed issue where Interactables in AR were translated at the height of the highest plane regardless of where the ray is cast.
- Fixed so steps to setup camera in `XRRig` only occurs in Play mode in the Editor.
- Fixed file names of .asmdef files to match assembly name.
- Fixed broken links for the help button (?) in the Inspector so it opens Scripting API documentation for each behavior in the package. ([1291475](https://issuetracker.unity3d.com/product/unity/issues/guid/1291475))
- Fixed XR Rig so it handles the Tracking Origin Mode changing on the device.
- Fixed XR Controller so it only sets position and rotation while the controller device is being tracked instead of resetting to the origin (such as from the device disconnecting or opening a system menu).

## [1.0.0-pre.1] - 2020-11-14

### Removed
- Removed anchor control deadzone properties from XR Controller (Action-based) used by Ray Interactor, it should now be configured on the Actions themselves

## [0.10.0-preview.7] - 2020-11-03

### Added
- Added multi-object editing support to all Editors

### Fixed
- Fixed Inspector foldouts to keep expanded state when clicking between GameObjects

## [0.10.0-preview.6] - 2020-10-30

### Added
- Added support for haptic impulses in XR Controller (Action-based)

### Fixed
- Fixed issue with actions not being considered pressed the frame after triggered
- Fixed issue where an AR test would fail due to the size of the Game view
- Fixed exception when adding an Input Action Manager while playing

## [0.10.0-preview.5] - 2020-10-23

### Added
- Added sample containing default set of input actions and presets

### Fixed
- Fixed issue with PrimaryAxis2D input from mouse not moving the scroll bars on UI as expected. ([1278162](https://issuetracker.unity3d.com/product/unity/issues/guid/1278162))
- Fixed issue where Bezier Curve did not take into account controller tilt. ([1245614](https://issuetracker.unity3d.com/product/unity/issues/guid/1245614))
- Fixed issue where a socket's hover mesh was offset. ([1285693](https://issuetracker.unity3d.com/product/unity/issues/guid/1285693))
- Fixed issue where disabling parent before `XRGrabInteractable` child was causing an error in `OnSelectCanceling`

## [0.10.0-preview.4] - 2020-10-14

### Fixed
- Fixed migration of a renamed field in interactors

## [0.10.0-preview.3] - 2020-10-14

### Added
- Added ability to control whether the line will always be cut short at the first ray cast hit, even when invalid, to the Interactor Line Visual ([1252532](https://issuetracker.unity3d.com/product/unity/issues/guid/1252532))

### Changed
- Renamed `OnSelectEnter`, `OnSelectExit`, `OnSelectCancel`, `OnHoverEnter`, `OnHoverExit`, `OnFirstHoverEnter`, and `OnLastHoverExit` to `OnSelectEntered`, `OnSelectExited`, `OnSelectCanceled`, `OnHoverEntered`, `OnHoverExited`, `OnFirstHoverEntered`, and `OnLastHoverExited` respectively.
- Replaced some `ref` parameters with `out` parameters in `ILineRenderable`; callers should replace `ref` with `out`

### Fixed
- Fixed Tracked Device Graphic Raycaster not respecting the Raycast Target property of UGUI Graphic when unchecked ([1221300](https://issuetracker.unity3d.com/product/unity/issues/guid/1221300))
- Fixed XR Ray Interactor flooding the console with assertion errors when sphere cast is used ([1259554](https://issuetracker.unity3d.com/product/unity/issues/guid/1259554), [1266781](https://issuetracker.unity3d.com/product/unity/issues/guid/1266781))
- Fixed foldouts in the Inspector to expand or collapse when clicking the label, not just the icon ([1259683](https://issuetracker.unity3d.com/product/unity/issues/guid/1259683))
- Fixed created objects having a duplicate name of a sibling ([1259702](https://issuetracker.unity3d.com/product/unity/issues/guid/1259702))
- Fixed created objects not being selected automatically ([1259682](https://issuetracker.unity3d.com/product/unity/issues/guid/1259682))
- Fixed XRUI Input Module component being duplicated in EventSystem GameObject after creating it from UI Canvas menu option ([1218216](https://issuetracker.unity3d.com/product/unity/issues/guid/1218216))
- Fixed missing AudioListener on created XR Rig Camera ([1241970](https://issuetracker.unity3d.com/product/unity/issues/guid/1241970))
- Fixed several issues related to creating objects from the GameObject menu, such as broken undo/redo and proper use of context object
- Fixed issue where GameObjects parented under an `XRGrabInteractable` did not retain their local position and rotation when drawn as a Socket Interactor Hover Mesh ([1256693](https://issuetracker.unity3d.com/product/unity/issues/guid/1256693))
- Fixed issue where Interaction callbacks (`OnSelectEnter`, `OnSelectExit`, `OnHoverEnter`, and `OnHoverExit`) are triggered before interactor and interactable objects are updated. ([1231662](https://issuetracker.unity3d.com/product/unity/issues/guid/1231662), [1228907](https://issuetracker.unity3d.com/product/unity/issues/guid/1228907), [1231482](https://issuetracker.unity3d.com/product/unity/issues/guid/1231482))

## [0.10.0-preview.2] - 2020-08-26

### Added
- Added XR Device Simulator and sample assets for simulating an XR HMD and controllers using keyboard & mouse

## [0.10.0-preview.1] - 2020-08-10

### Added
- Added continuous move and turn locomotion

### Changed
- Changed accessibility levels to avoid `protected` fields, instead exposed through properties
- Components that use Input System actions no longer automatically enable or disable them. Add the `InputActionManager` component to a GameObject in a scene and use the Inspector to reference the `InputActionAsset` you want to automatically enable at startup.
- Some properties have been renamed from PascalCase to camelCase to conform with coding standard; the API Updater should update usage automatically in most cases

### Fixed
- Fixed compilation issue when AR Foundation package is also installed
- Fixed the Interactor Line Visual lagging behind the controller ([1264748](https://issuetracker.unity3d.com/product/unity/issues/guid/1264748))
- Fixed Socket Interactor not creating default hover materials, and backwards usage of the materials ([1225734](https://issuetracker.unity3d.com/product/unity/issues/guid/1225734))
- Fixed Tint Interactable Visual to allow it to work with objects that have multiple materials
- Improved Tint Interactable Visual to not create a material instance when Emission is enabled on the material

## [0.9.9-preview.3] - 2020-06-24

### Changed
- In progress changes to visibility

## [0.9.9-preview.2] - 2020-06-22

### Changed
- Hack week version push.

## [0.9.9-preview.1] - 2020-06-04

### Changed
- Swaps axis for feature API anchor manipulation

### Fixed
- Fixed controller recording not working
- Start controller recording at 0 time so you do not have to wait for the recording to start playing.

## [0.9.9-preview] - 2020-06-04

### Added
- Added Input System support
- Added ability to query the controller from the interactor

### Changed
- Changed a number of members and properties to be `protected` rather than `private`
- Changed to remove `sealed` from a number of classes.

## [0.9.4-preview] - 2020-04-01

### Fixed
- Fixed to allow 1.3.X or 2.X versions of legacy input helpers to work with the XR Interaction Toolkit.

## [0.9.3-preview] - 2020-01-23

### Added
- Added pose provider support to XR Controller
- Added ability to put objects back to their original hierarchy position when dropping them
- Made teleport configurable to use either activate or select
- Removed need for box colliders behind UI to stop line visuals from drawing through them

### Fixed
- Fixed minor documentation issues
- Fixed passing from hand to hand of objects using direct interactors
- Fixed null ref in controller states clear
- Fixed no "OnRelease" even for Activate on Grabbable

## [0.9.2-preview] - 2019-12-17

### Changed
- Rolled LIH version back until 1.3.9 is on production.

## [0.9.1-preview] - 2019-12-12

### Fixed
- Documentation image fix

## [0.9.0-preview] - 2019-12-06

### Changed
- Release candidate

## [0.0.9-preview] - 2019-12-06

### Changed
- Further release prep

## [0.0.8-preview] - 2019-12-05

### Changed
- Pre-release release.

## [0.0.6-preview] - 2019-10-15

### Changed
- Changes to README.md file

### Fixed
- Further CI/CD fixes.

## [0.0.5-preview] - 2019-10-03

### Changed
- Renamed everything to com.unity.xr.interaction.toolkit / XR Interaction Toolkit

### Fixed
- Setup CI correctly.

## [0.0.4-preview] - 2019-05-08

### Changed
- Bump package version for CI tests.

## [0.0.3-preview] - 2019-05-07

### Added
- Initial preview release of the XR Interaction framework.
