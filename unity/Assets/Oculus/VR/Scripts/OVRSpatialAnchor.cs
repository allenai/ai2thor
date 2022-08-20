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
using System.Diagnostics;
using Unity.Collections;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Represents a spatial anchor.
/// </summary>
/// <remarks>
/// This component can be used in two ways: to create a new spatial anchor or to associate with an existing spatial
/// anchor.
///
/// To create a new spatial anchor, simply add this component to any GameObject. The transform of the GameObject is used
/// to create a new spatial anchor in the Oculus Runtime. Afterwards, the GameObject's transform will be updated
/// automatically. The creation operation is asynchronous, and, if it fails, this component will be destroyed.
///
/// To associate an existing spatial anchor with this component, you will need the spatial anchor's space handle and its
/// uuid, which can be obtained using <see cref="OVRPlugin.QuerySpaces"/>. After adding this component to a GameObject,
/// call <see cref="InitializeFromExisting"/> before the next frame, i.e., before `Start` is called on this component.
/// </remarks>
[DisallowMultipleComponent]
public class OVRSpatialAnchor : MonoBehaviour
{
    private bool _startCalled;

    private ulong _requestId;


    /// <summary>
    /// The space associated with this spatial anchor.
    /// </summary>
    /// <remarks>
    /// The <see cref="OVRSpace"/> represents the runtime instance of the spatial anchor and will change across
    /// different sessions.
    /// </remarks>
    public OVRSpace Space { get; private set; }

    /// <summary>
    /// The UUID associated with this spatial anchor.
    /// </summary>
    /// <remarks>
    /// UUIDs persist across sessions and applications. If you load a persisted anchor, you can use the UUID to identify
    /// it.
    /// </remarks>
    public Guid Uuid { get; private set; }

    /// <summary>
    /// Whether the spatial anchor has been created.
    /// </summary>
    /// <remarks>
    /// Creation is asynchronous and may take several frames. If creation fails, this component is destroyed.
    /// </remarks>
    public bool Created => Space.Valid;

    /// <summary>
    /// Initializes this component from an existing space handle and uuid, e.g., the result of a call to
    /// <see cref="OVRPlugin.QuerySpaces"/>.
    /// </summary>
    /// <remarks>
    /// This method allows you to associate this component with an existing spatial anchor, e.g., one that was saved in
    /// a previous session. Do not call this method to create a new spatial anchor.
    ///
    /// If you call this method, you must do so prior to the component's `Start` method. You cannot change the spatial
    /// anchor associated with this component after that.
    /// </remarks>
    /// <param name="space">The existing <see cref="OVRSpace"/> to associate with this spatial anchor.</param>
    /// <param name="uuid">The universally unique identifier to associate with this spatial anchor.</param>
    /// <exception cref="InvalidOperationException">Thrown if `Start` has already been called on this component.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="space"/> is not <see cref="OVRSpace.Valid"/>.</exception>
    public void InitializeFromExisting(OVRSpace space, Guid uuid)
    {
        if (_startCalled)
            throw new InvalidOperationException($"Cannot call {nameof(InitializeFromExisting)} after {nameof(Start)}. This must be set once upon creation.");

        if (!space.Valid)
        {
            Destroy(this);
            throw new ArgumentException($"Invalid space {space}.", nameof(space));
        }

        InitializeUnchecked(space, uuid);
    }

    /// <summary>
    /// Saves the <see cref="OVRSpatialAnchor"/> to local persistent storage.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous; use <paramref name="onComplete"/> to be notified of completion.
    ///
    /// When saved, an <see cref="OVRSpatialAnchor"/> can be loaded by a different session or application. Use the
    /// <see cref="Uuid"/> to identify the same <see cref="OVRSpatialAnchor"/> at a future time.
    /// </remarks>
    /// <param name="onComplete">
    /// Invoked when the save operation completes. May be null. Parameters are
    /// - <see cref="OVRSpatialAnchor"/>: The anchor being saved.
    /// - `bool`: A value indicating whether the save operation succeeded.
    /// </param>
    public void Save(Action<OVRSpatialAnchor, bool> onComplete = null)
    {
        if (OVRPlugin.SaveSpace(Space, OVRPlugin.SpaceStorageLocation.Local,
                OVRPlugin.SpaceStoragePersistenceMode.Indefinite, out var requestId))
        {
            Development.LogRequest(requestId, $"[{Uuid}] Saving spatial anchor...");
            if (onComplete != null)
            {
                _completionDelegates[requestId] = new AnchorDelegatePair
                {
                    Anchor = this,
                    Delegate = onComplete
                };
            }
        }
        else
        {
            Development.LogError($"[{Uuid}] {nameof(OVRPlugin)}.{nameof(OVRPlugin.SaveSpace)} failed.");
            onComplete?.Invoke(this, false);
        }
    }





    /// <summary>
    /// Erases the <see cref="OVRSpatialAnchor"/> from persistent storage.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous; use <paramref name="onComplete"/> to be notified of completion.
    /// </remarks>
    /// <param name="onComplete">
    /// Invoked when the erase operation completes. May be null. Parameters are
    /// - <see cref="OVRSpatialAnchor"/>: The anchor being erased.
    /// - `bool`: A value indicating whether the erase operation succeeded.
    /// </param>
    public void Erase(Action<OVRSpatialAnchor, bool> onComplete = null)
    {
        if (OVRPlugin.EraseSpace(Space, OVRPlugin.SpaceStorageLocation.Local, out var requestId))
        {
            Development.LogRequest(requestId, $"[{Uuid}] Erasing spatial anchor...");
            if (onComplete != null)
            {
                _completionDelegates[requestId] = new AnchorDelegatePair
                {
                    Anchor = this,
                    Delegate = onComplete
                };
            }
        }
        else
        {
            Development.LogError($"[{Uuid}] {nameof(OVRPlugin)}.{nameof(OVRPlugin.EraseSpace)} failed.");
            onComplete?.Invoke(this, false);
        }
    }

    // Initializes this component without checking preconditions
    private void InitializeUnchecked(OVRSpace space, Guid uuid)
    {
        Space = space;
        Uuid = uuid;
        OVRPlugin.SetSpaceComponentStatus(Space, OVRPlugin.SpaceComponentType.Locatable, true, 0, out _);
        OVRPlugin.SetSpaceComponentStatus(Space, OVRPlugin.SpaceComponentType.Storable, true, 0, out _);

        // Try to update the pose as soon as we can.
        UpdateTransform();
    }

    private void Start()
    {
        _startCalled = true;

        if (Space.Valid)
        {
            Development.Log($"[{Space}] Created spatial anchor from existing space.");
        }
        else
        {
            CreateSpatialAnchor();
        }
    }

    private void Update()
    {
        if (Space.Valid)
        {
            UpdateTransform();
        }
    }

    private void OnDestroy()
    {
        if (Space.Valid)
        {
            OVRPlugin.DestroySpace(Space);
        }
    }

    private OVRPose GetTrackingSpacePose()
    {
        var mainCamera = Camera.main;
        if (mainCamera)
        {
            return transform.ToTrackingSpacePose(mainCamera);
        }

        Development.LogWarning($"No main camera found. Using world-space pose.");
        return transform.ToOVRPose(isLocal: false);
    }

    private void CreateSpatialAnchor()
    {
        if (OVRPlugin.CreateSpatialAnchor(new OVRPlugin.SpatialAnchorCreateInfo
        {
            BaseTracking = OVRPlugin.GetTrackingOriginType(),
            PoseInSpace = GetTrackingSpacePose().ToPosef(),
            Time = OVRPlugin.GetTimeInSeconds(),
        }, out _requestId))
        {
            Development.LogRequest(_requestId, $"Creating spatial anchor...");
            _creationRequests[_requestId] = this;
        }
        else
        {
            Destroy(this);
            Development.LogError($"{nameof(OVRPlugin)}.{nameof(OVRPlugin.CreateSpatialAnchor)} failed. Destroying {nameof(OVRSpatialAnchor)} component.");
        }
    }

    private void UpdateTransform()
    {
        if (!OVRPlugin.TryLocateSpace(Space, OVRPlugin.GetTrackingOriginType(), out var posef)) return;

        var pose = posef.ToOVRPose();
        var mainCamera = Camera.main;
        if (mainCamera)
        {
            pose = pose.ToWorldSpacePose(mainCamera);
        }

        transform.SetPositionAndRotation(pose.position, pose.orientation);
    }

    private struct AnchorDelegatePair
    {
        public OVRSpatialAnchor Anchor;
        public Action<OVRSpatialAnchor, bool> Delegate;
    }


    private static readonly Dictionary<ulong, OVRSpatialAnchor> _creationRequests =
        new Dictionary<ulong, OVRSpatialAnchor>();

    private static readonly Dictionary<ulong, AnchorDelegatePair> _completionDelegates =
        new Dictionary<ulong, AnchorDelegatePair>();


    static OVRSpatialAnchor()
    {
        OVRManager.SpatialAnchorCreateComplete += OnSpatialAnchorCreateComplete;
        OVRManager.SpaceSaveComplete += OnSpaceSaveComplete;
        OVRManager.SpaceEraseComplete += OnSpaceEraseComplete;
    }

    private static void InvokeDelegate(ulong requestId, bool result)
    {
        if (_completionDelegates.TryGetValue(requestId, out var value))
        {
            _completionDelegates.Remove(requestId);
            value.Delegate(value.Anchor, result);
        }
    }


    private static void OnSpatialAnchorCreateComplete(ulong requestId, bool success, OVRSpace space, Guid uuid)
    {
        Development.LogRequestResult(requestId, success,
            $"[{uuid}] Spatial anchor created.",
            $"Failed to create spatial anchor. Destroying {nameof(OVRSpatialAnchor)} component.");

        if (!_creationRequests.TryGetValue(requestId, out var anchor)) return;

        _creationRequests.Remove(requestId);

        if (success && anchor)
        {
            // All good; complete setup of OVRSpatialAnchor component.
            anchor.InitializeUnchecked(space, uuid);
        }
        else if (success && !anchor)
        {
            // Creation succeeded, but the OVRSpatialAnchor component was destroyed before the callback completed.
            OVRPlugin.DestroySpace(space);
        }
        else if (!success && anchor)
        {
            // The OVRSpatialAnchor component exists but creation failed.
            Destroy(anchor);
        }
        // else if creation failed and the OVRSpatialAnchor component was destroyed, nothing to do.
    }

    private static void OnSpaceSaveComplete(ulong requestId, OVRSpace space, bool result, Guid uuid)
    {
        Development.LogRequestResult(requestId, result,
            $"[{uuid}] Saved.",
            $"[{uuid}] Save failed.");

        InvokeDelegate(requestId, result);
    }


    private static void OnSpaceEraseComplete(ulong requestId, bool result, Guid uuid, OVRPlugin.SpaceStorageLocation location)
    {
        Development.LogRequestResult(requestId, result,
            $"[{uuid}] Erased.",
            $"[{uuid}] Erase failed.");

        InvokeDelegate(requestId, result);
    }

    private static class Development
    {
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message) => Debug.Log($"[{nameof(OVRSpatialAnchor)}] {message}");

        [Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(string message) => Debug.LogWarning($"[{nameof(OVRSpatialAnchor)}] {message}");

        [Conditional("DEVELOPMENT_BUILD")]
        public static void LogError(string message) => Debug.LogError($"[{nameof(OVRSpatialAnchor)}] {message}");

#if DEVELOPMENT_BUILD
        private static readonly HashSet<ulong> _requests = new HashSet<ulong>();
#endif // DEVELOPMENT_BUILD

        [Conditional("DEVELOPMENT_BUILD")]
        public static void LogRequest(ulong requestId, string message)
        {
#if DEVELOPMENT_BUILD
            _requests.Add(requestId);
#endif // DEVELOPMENT_BUILD
            Log($"({requestId}) {message}");
        }

        [Conditional("DEVELOPMENT_BUILD")]
        public static void LogRequestResult(ulong requestId, bool result, string successMessage, string failureMessage)
        {
#if DEVELOPMENT_BUILD
            // Not a request we're tracking
            if (!_requests.Remove(requestId)) return;
#endif // DEVELOPMENT_BUILD
            if (result)
            {
                Log($"({requestId}) {successMessage}");
            }
            else
            {
                LogError($"({requestId}) {failureMessage}");
            }
        }
    }

}
