using System;

namespace UnityEditor.XR.Interaction.Toolkit
{
    public partial class XRBaseControllerEditor
    {
        /// <inheritdoc cref="m_ModelParent"/>
        [Obsolete("m_ModelTransform has been deprecated due to being renamed. Use m_ModelParent instead. (UnityUpgradable) -> m_ModelParent")]
        protected SerializedProperty m_ModelTransform;
    }
}
