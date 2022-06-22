#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine.Scripting;

namespace UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation
{
    /// <summary>
    /// This class automatically registers control layouts used by the <see cref="XRDeviceSimulator"/>.
    /// </summary>
    /// <seealso cref="XRSimulatedHMD"/>
    /// <seealso cref="XRSimulatedController"/>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    [Preserve]
    public static class SimulatedInputLayoutLoader
    {
        [Preserve]
        static SimulatedInputLayoutLoader()
        {
            RegisterInputLayouts();
        }

        /// <summary>
        /// See <see cref="RuntimeInitializeLoadType.BeforeSceneLoad"/>.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad), Preserve]
        public static void Initialize()
        {
            // Will execute the static constructor as a side effect.
        }

        static void RegisterInputLayouts()
        {
            InputSystem.InputSystem.RegisterLayout<XRSimulatedHMD>();
            InputSystem.InputSystem.RegisterLayout<XRSimulatedController>();
        }
    }
}
