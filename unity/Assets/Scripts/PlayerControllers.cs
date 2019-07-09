using System.Collections;
using System.Collections.Generic;
using System;
// using PlayerControllers;
// using UnityStandardAssets.Characters.FirstPerson;

namespace UnityStandardAssets.Characters.FirstPerson {
    public enum ControlMode {
        DEBUG_TEXT_INPUT,
        FPS,
        DISCRETE_POINT_CLICK
    }

    class PlayerControllers {
        public static Dictionary<ControlMode, Type> controlModeToComponent = new Dictionary<ControlMode, Type>{
                {ControlMode.DEBUG_TEXT_INPUT, typeof(DebugDiscreteAgentController)},
                {ControlMode.FPS, typeof(DebugFPSAgentController)},
                {ControlMode.DISCRETE_POINT_CLICK, typeof(DiscretePointClickAgentController)}
        };
    }

}