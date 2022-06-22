using System;

namespace UnityEngine.XR.Interaction.Toolkit
{
    public partial class ActionBasedController
    {
        [SerializeField]
        float m_ButtonPressPoint = 0.5f;

        /// <summary>
        /// (Deprecated) The value threshold for when a button is considered pressed to trigger an interaction event.
        /// If a button has a value equal to or greater than this value, it is considered pressed.
        /// </summary>
#if INPUT_SYSTEM_1_1_OR_NEWER
        [Obsolete("Deprecated, this obsolete property is not used when Input System version is 1.1.0 or higher. Configure press point on the action or binding instead.", true)]
#else
        [Obsolete("Marked for deprecation, this property will be removed when Input System dependency version is bumped to 1.1.0.")]
#endif
        public float buttonPressPoint
        {
            get => m_ButtonPressPoint;
            set => m_ButtonPressPoint = value;
        }
    }
}
