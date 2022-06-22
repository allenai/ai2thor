using System;

namespace UnityEngine.XR.Interaction.Toolkit
{
    static partial class XRHelpURLConstants
    {
        /// <summary>
        /// Scripting API URL for <see cref="XRRig"/>.
        /// </summary>
        [Obsolete("k_XRRig is now deprecated since XRRig was replaced by XROrigin. Please use documentation from com.unity.xr.core-utils instead.", true)]
        public const string k_XRRig = k_BaseApi + k_BaseNamespace + nameof(XRRig) + ".html";
    }
}
