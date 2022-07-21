using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Utilities.Pooling;
using UnityStandardAssets.Characters.FirstPerson;

public class Agent_TeleportationArea : XRBaseInteractable {
    /// <summary>
    /// Indicates when the teleportation action happens.
    /// </summary>
    public enum TeleportTrigger {
        /// <summary>
        /// Teleportation occurs once selection is released without being canceled.
        /// </summary>
        OnSelectExited,

        /// <summary>
        /// Teleportation occurs right when area is selected.
        /// </summary>
        OnSelectEntered,

        /// <summary>
        /// Teleportation occurs on activate.
        /// </summary>
        OnActivated,

        /// <summary>
        /// Teleportation occurs on deactivate.
        /// </summary>
        OnDeactivated,

        /// <summary>
        /// (Deprecated) OnSelectExit has been deprecated. Use OnSelectExited instead.
        /// </summary>
        [Obsolete("OnSelectExit has been deprecated. Use OnSelectExited instead. (UnityUpgradable) -> OnSelectExited")]
        OnSelectExit = OnSelectExited,

        /// <summary>
        /// (Deprecated) OnSelectEnter has been deprecated. Use OnSelectEntered instead.
        /// </summary>
        [Obsolete("OnSelectEnter has been deprecated. Use OnSelectEntered instead. (UnityUpgradable) -> OnSelectEntered")]
        OnSelectEnter = OnSelectEntered,

        /// <summary>
        /// (Deprecated) OnSelectEnter has been deprecated. Use OnSelectEntered instead.
        /// </summary>
        [Obsolete("OnActivate has been deprecated. Use OnActivated instead. (UnityUpgradable) -> OnActivated")]
        OnActivate = OnActivated,

        /// <summary>
        /// (Deprecated) OnDeactivate has been deprecated. Use OnDeactivated instead.
        /// </summary>
        [Obsolete("OnDeactivate has been deprecated. Use OnDeactivated instead. (UnityUpgradable) -> OnDeactivated")]
        OnDeactivate = OnDeactivated,
    }

    [SerializeField]
    [Tooltip("How to orient the rig after teleportation." +
        "\nSet to:" +
        "\n\nWorld Space Up to stay oriented according to the world space up vector." +
        "\n\nSet to Target Up to orient according to the target BaseTeleportationInteractable Transform's up vector." +
        "\n\nSet to Target Up And Forward to orient according to the target BaseTeleportationInteractable Transform's rotation." +
        "\n\nSet to None to maintain the same orientation before and after teleporting.")]
    MatchOrientation m_MatchOrientation = MatchOrientation.WorldSpaceUp;

    /// <summary>
    /// How to orient the rig after teleportation.
    /// </summary>
    /// <remarks>
    /// Set to:
    /// <list type="bullet">
    /// <item>
    /// <term><see cref="MatchOrientation.WorldSpaceUp"/></term>
    /// <description> to stay oriented according to the world space up vector.</description>
    /// </item>
    /// <item>
    /// <term><see cref="MatchOrientation.TargetUp"/></term>
    /// <description> to orient according to the target <see cref="BaseTeleportationInteractable"/> Transform's up vector.</description>
    /// </item>
    /// <item>
    /// <term><see cref="MatchOrientation.TargetUpAndForward"/></term>
    /// <description> to orient according to the target <see cref="BaseTeleportationInteractable"/> Transform's rotation.</description>
    /// </item>
    /// <item>
    /// <term><see cref="MatchOrientation.None"/></term>
    /// <description> to maintain the same orientation before and after teleporting.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public MatchOrientation matchOrientation {
        get => m_MatchOrientation;
        set => m_MatchOrientation = value;
    }

    [SerializeField]
    [Tooltip("Specify when the teleportation will be triggered. Options map to when the trigger is pressed or when it is released.")]
    TeleportTrigger m_TeleportTrigger = TeleportTrigger.OnSelectExited;

    /// <summary>
    /// Specifies when the teleportation triggers.
    /// </summary>
    public TeleportTrigger teleportTrigger {
        get => m_TeleportTrigger;
        set => m_TeleportTrigger = value;
    }

    [SerializeField]
    TeleportingEvent m_Teleporting = new TeleportingEvent();

    /// <summary>
    /// Gets or sets the event that Unity calls when queuing to teleport via
    /// the <see cref="TeleportationProvider"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="TeleportingEventArgs"/> passed to each listener is only valid
    /// while the event is invoked, do not hold a reference to it.
    /// </remarks>
    public TeleportingEvent teleporting {
        get => m_Teleporting;
        set => m_Teleporting = value;
    }

    // Reusable event args
    readonly LinkedPool<TeleportingEventArgs> m_TeleportingEventArgs = new LinkedPool<TeleportingEventArgs>(() => new TeleportingEventArgs(), collectionCheck: false);

    private AgentManager AManager = null;

    /// <inheritdoc />
    protected override void Awake() {
        base.Awake();
    }
    private void Start() {
        AManager = GameObject.Find("PhysicsSceneManager").GetComponentInChildren<AgentManager>();
    }

    /// <inheritdoc />
    protected override void Reset() {
        base.Reset();
        selectMode = InteractableSelectMode.Multiple;
    }

    void Teleport(IXRInteractor interactor) {
        if (interactor == null)
            return;

        RaycastHit raycastHit = default;
        if (interactor is XRRayInteractor rayInteractor && rayInteractor != null) {
            if (rayInteractor.TryGetCurrent3DRaycastHit(out raycastHit)) {
                // Are we still selecting this object?
                var found = false;
                foreach (var interactionCollider in colliders) {
                    if (interactionCollider == raycastHit.collider) {
                        found = true;
                        break;
                    }
                }

                if (!found) {
                    return;
                }
            }
        }

        var teleportRequest = new TeleportRequest {
            matchOrientation = m_MatchOrientation,
            requestTime = Time.time,
        };

        // Teleport Agent
        if (raycastHit.collider != null) {
            teleportRequest.destinationPosition = raycastHit.point;
            teleportRequest.destinationRotation = transform.rotation;

            PhysicsRemoteFPSAgentController agent = (PhysicsRemoteFPSAgentController)AManager.PrimaryAgent;
            teleportRequest.destinationPosition.y = agent.transform.position.y;
            if (agent != null) {
                if (agent.TeleportCheck(teleportRequest.destinationPosition, teleportRequest.destinationRotation.eulerAngles, false)) {
                    StartCoroutine(TeleportCoroutine(agent, teleportRequest));

                    if (m_Teleporting != null) {
                        using (m_TeleportingEventArgs.Get(out var args)) {
                            args.interactorObject = interactor;
                            args.interactableObject = this;
                            args.teleportRequest = teleportRequest;
                            m_Teleporting.Invoke(args);
                        }
                    }
                }
            }
        }
    }

    private IEnumerator TeleportCoroutine(PhysicsRemoteFPSAgentController agent, TeleportRequest teleportRequest) {
        Dictionary<string, object> action = new Dictionary<string, object>();
        action["action"] = "TeleportFull";
        action["x"] = teleportRequest.destinationPosition.x;
        action["y"] = teleportRequest.destinationPosition.y;
        action["z"] = teleportRequest.destinationPosition.z;
        Vector3 rotation = teleportRequest.destinationRotation.eulerAngles;
        action["rotation"] = agent.transform.rotation.eulerAngles;
        action["horizon"] = agent.m_Camera.transform.localEulerAngles.x;
        action["standing"] = agent.isStanding();
        action["forceAction"] = true;

        if (ScreenFader.Instance != null && XRManager.Instance.IsFPSMode) {
            yield return ScreenFader.Instance.StartFadeOut();

            agent.ProcessControlCommand(action);

            yield return ScreenFader.Instance.StartFadeIn();
        } else {
            agent.ProcessControlCommand(action);
        }
    }

    /// <inheritdoc />
    protected override void OnSelectEntered(SelectEnterEventArgs args) {
        if (m_TeleportTrigger == TeleportTrigger.OnSelectEntered)
            Teleport(args.interactorObject);

        base.OnSelectEntered(args);
    }

    /// <inheritdoc />
    protected override void OnSelectExited(SelectExitEventArgs args) {
        if (m_TeleportTrigger == TeleportTrigger.OnSelectExited && !args.isCanceled)
            Teleport(args.interactorObject);

        base.OnSelectExited(args);
    }

    /// <inheritdoc />
    protected override void OnActivated(ActivateEventArgs args) {
        if (m_TeleportTrigger == TeleportTrigger.OnActivated)
            Teleport(args.interactorObject);

        base.OnActivated(args);
    }

    /// <inheritdoc />
    protected override void OnDeactivated(DeactivateEventArgs args) {
        if (m_TeleportTrigger == TeleportTrigger.OnDeactivated)
            Teleport(args.interactorObject);

        base.OnDeactivated(args);
    }
}
