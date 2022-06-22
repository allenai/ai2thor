using System;

namespace UnityEngine.XR.Interaction.Toolkit
{
    public abstract partial class BaseTeleportationInteractable
    {
        /// <summary>
        /// Automatically called upon the teleport trigger when a teleport request should be generated.
        /// </summary>
        /// <param name="interactor">The interactor that initiated the teleport trigger.</param>
        /// <param name="raycastHit">The ray cast hit information from the interactor.</param>
        /// <param name="teleportRequest">The teleport request that should be filled out during this method call.</param>
        /// <returns>Returns <see langword="true"/> if the teleport request was successfully updated and should be queued. Otherwise, returns <see langword="false"/>.</returns>
        /// <seealso cref="TeleportationProvider.QueueTeleportRequest"/>
        /// <remarks>
        /// <c>GenerateTeleportRequest(XRBaseInteractor, RaycastHit, ref TeleportRequest)</c> has been deprecated. Use <see cref="GenerateTeleportRequest(IXRInteractor, RaycastHit, ref TeleportRequest)"/> instead.
        /// </remarks>
        [Obsolete("GenerateTeleportRequest(XRBaseInteractor, RaycastHit, ref TeleportRequest) has been deprecated. Use GenerateTeleportRequest(IXRInteractor, RaycastHit, ref TeleportRequest) instead.")]
        protected virtual bool GenerateTeleportRequest(XRBaseInteractor interactor, RaycastHit raycastHit, ref TeleportRequest teleportRequest)
            => GenerateTeleportRequest((IXRInteractor)interactor, raycastHit, ref teleportRequest);
    }
}
