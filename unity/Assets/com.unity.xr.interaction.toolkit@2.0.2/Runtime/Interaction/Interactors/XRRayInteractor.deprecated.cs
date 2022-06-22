using System;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace UnityEngine.XR.Interaction.Toolkit
{
    public partial class XRRayInteractor
    {
        /// <summary>
        /// (Deprecated) Initial velocity of the projectile. Increasing this value will make the curve reach further.
        /// </summary>
        /// <seealso cref="LineType.ProjectileCurve"/>
        /// <remarks>
        /// <c>Velocity</c> has been deprecated. Use <see cref="velocity"/> instead.
        /// </remarks>
#pragma warning disable IDE1006 // Naming Styles
        [Obsolete("Velocity has been deprecated. Use velocity instead. (UnityUpgradable) -> velocity")]
        public float Velocity
        {
            get => velocity;
            set => velocity = value;
        }
#pragma warning restore IDE1006

        /// <summary>
        /// (Deprecated) Gravity of the projectile in the reference frame.
        /// </summary>
        /// <seealso cref="LineType.ProjectileCurve"/>
        /// <remarks>
        /// <c>Acceleration</c> has been deprecated. Use <see cref="acceleration"/> instead.
        /// </remarks>
#pragma warning disable IDE1006 // Naming Styles
        [Obsolete("Acceleration has been deprecated. Use acceleration instead. (UnityUpgradable) -> acceleration")]
        public float Acceleration
        {
            get => acceleration;
            set => acceleration = value;
        }
#pragma warning restore IDE1006

        /// <inheritdoc cref="additionalFlightTime"/>
        /// <remarks>
        /// <c>AdditionalFlightTime</c> has been deprecated. Use <see cref="additionalFlightTime"/> instead.
        /// </remarks>
#pragma warning disable IDE1006 // Naming Styles
        [Obsolete("AdditionalFlightTime has been deprecated. Use additionalFlightTime instead. (UnityUpgradable) -> additionalFlightTime")]
        public float AdditionalFlightTime
        {
            get => additionalFlightTime;
            set => additionalFlightTime = value;
        }
#pragma warning restore IDE1006

        /// <inheritdoc cref="angle"/>
        /// <remarks>
        /// <c>Angle</c> has been deprecated. Use <see cref="angle"/> instead.
        /// </remarks>
#pragma warning disable IDE1006 // Naming Styles
        [Obsolete("Angle has been deprecated. Use angle instead. (UnityUpgradable) -> angle")]
        public float Angle => angle;
#pragma warning restore IDE1006

        /// <summary>
        /// The <see cref="Transform"/> that upon entering selection
        /// (when this Interactor first initiates selection of an Interactable),
        /// this Interactor will copy the pose of the attach <see cref="Transform"/> values into.
        /// </summary>
        /// <remarks>
        /// Automatically instantiated and set in <see cref="Awake"/>.
        /// Setting this will not automatically destroy the previous object.
        /// <br />
        /// <c>originalAttachTransform</c> has been deprecated. Use <see cref="rayOriginTransform"/> instead.
        /// </remarks>
        /// <seealso cref="XRBaseInteractor.attachTransform"/>
        [Obsolete("originalAttachTransform has been deprecated. Use rayOriginTransform instead. (UnityUpgradable) -> rayOriginTransform")]
        protected Transform originalAttachTransform
        {
            get => rayOriginTransform;
            set => rayOriginTransform = value;
        }

        /// <summary>
        /// (Obsolete) Use <see cref="ILineRenderable.GetLinePoints"/> instead.
        /// </summary>
        /// <param name="linePoints">Obsolete.</param>
        /// <param name="numPoints">Obsolete.</param>
        /// <param name="_">Dummy value to support old function signature.</param>
        /// <returns>Obsolete.</returns>
        /// <remarks>
        /// <c>GetLinePoints</c> with <c>ref int</c> parameter has been deprecated. Use signature with <c>out int</c> parameter instead.
        /// </remarks>
        [Obsolete("GetLinePoints with ref int parameter has been deprecated. Use signature with out int parameter instead.", true)]
        // ReSharper disable RedundantAssignment
        public bool GetLinePoints(ref Vector3[] linePoints, ref int numPoints, int _ = default)
            // ReSharper restore RedundantAssignment
        {
            return GetLinePoints(ref linePoints, out numPoints);
        }

        /// <summary>
        /// (Obsolete) Use <see cref="ILineRenderable.TryGetHitInfo"/> instead.
        /// </summary>
        /// <param name="position">Obsolete.</param>
        /// <param name="normal">Obsolete.</param>
        /// <param name="positionInLine">Obsolete.</param>
        /// <param name="isValidTarget">Obsolete.</param>
        /// <param name="_">Dummy value to support old function signature.</param>
        /// <returns>Obsolete.</returns>
        /// <remarks>
        /// <c>TryGetHitInfo</c> with <c>ref</c> parameters has been deprecated. Use signature with <c>out</c> parameters instead.
        /// </remarks>
        [Obsolete("TryGetHitInfo with ref parameters has been deprecated. Use signature with out parameters instead.", true)]
        // ReSharper disable RedundantAssignment
        public bool TryGetHitInfo(ref Vector3 position, ref Vector3 normal, ref int positionInLine, ref bool isValidTarget, int _ = default)
            // ReSharper restore RedundantAssignment
        {
            return TryGetHitInfo(out position, out normal, out positionInLine, out isValidTarget);
        }

        /// <inheritdoc cref="TryGetCurrent3DRaycastHit(out RaycastHit)"/>
        /// <remarks>
        /// <c>GetCurrentRaycastHit</c> has been deprecated. Use <see cref="TryGetCurrent3DRaycastHit(out RaycastHit)"/> instead.
        /// </remarks>
        [Obsolete("GetCurrentRaycastHit has been deprecated. Use TryGetCurrent3DRaycastHit instead. (UnityUpgradable) -> TryGetCurrent3DRaycastHit(*)")]
        public bool GetCurrentRaycastHit(out RaycastHit raycastHit)
        {
            return TryGetCurrent3DRaycastHit(out raycastHit);
        }

        /// <inheritdoc />
        /// <remarks>
        /// <c>CanHover(XRBaseInteractable)</c> has been deprecated. Use <see cref="CanHover(IXRHoverInteractable)"/> instead.
        /// </remarks>
        [Obsolete("CanHover(XRBaseInteractable) has been deprecated. Use CanHover(IXRHoverInteractable) instead.")]
        public override bool CanHover(XRBaseInteractable interactable) => CanHover((IXRHoverInteractable)interactable);

        /// <inheritdoc />
        /// <remarks>
        /// <c>CanSelect(XRBaseInteractable)</c> has been deprecated. Use <see cref="CanSelect(IXRSelectInteractable)"/> instead.
        /// </remarks>
        [Obsolete("CanSelect(XRBaseInteractable) has been deprecated. Use CanSelect(IXRSelectInteractable) instead.")]
        public override bool CanSelect(XRBaseInteractable interactable) => CanSelect((IXRSelectInteractable)interactable);
    }
}
