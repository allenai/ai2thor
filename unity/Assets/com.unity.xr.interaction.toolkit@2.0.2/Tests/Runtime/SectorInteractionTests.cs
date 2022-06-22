// There is a problem with upm-ci package test builds where the Unity.InputSystem.TestFramework assembly
// is not included, which causes this test to fail to compile. To allow these tests to be run,
// modify your project's Packages\manifest.json file to include com.unity.inputsystem in the testables list.
// See [Project Manifest](https://docs.unity3d.com/Manual/upm-manifestPrj.html)
// Example:
//   "testables": [
//     "com.unity.inputsystem",
//     "com.unity.xr.interaction.toolkit"
//   ]
// Then open Edit > Project Settings... > Player and edit the Scripting Define Symbols to add this.
// It is enabled in the XR Interaction Toolkit Examples project to allow these
// tests to be manually run, but skipped during some types of automated builds where the symbol is not defined.
#if ENABLE_INPUT_SYSTEM_TESTFRAMEWORK_TESTS

using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Interactions;

namespace UnityEngine.XR.Interaction.Toolkit.Tests
{
    [TestFixture]
    class SectorInteractionTests : InputTestFixture
    {
        public override void Setup()
        {
            base.Setup();

            InputSystem.InputSystem.RegisterInteraction<SectorInteraction>();
        }

        [TestCase(SectorInteraction.SweepBehavior.Locked)]
        [TestCase(SectorInteraction.SweepBehavior.AllowReentry)]
        [TestCase(SectorInteraction.SweepBehavior.DisallowReentry)]
        [TestCase(SectorInteraction.SweepBehavior.HistoryIndependent)]
        public void StartedValidSweepBehavior(SectorInteraction.SweepBehavior sweepBehavior)
        {
            var gamepad = InputSystem.InputSystem.AddDevice<Gamepad>();

            var action = new InputAction(
                type: InputActionType.Value,
                binding: "<Gamepad>/rightStick",
                interactions: CreateInteractionString(SectorInteraction.Directions.North, sweepBehavior));

            action.Enable();

            var north = new Vector2(0f, 1f);
            var east = new Vector2(1f, 0f);
            var center = Vector2.zero;

            // Starting below threshold
            Assert.That(gamepad.rightStick.IsActuated(), Is.False);
            Assert.That(action.triggered, Is.False);

            using (var trace = new InputActionTrace())
            {
                trace.SubscribeToAll();

                // Actuate above threshold, in valid sector
                Set(gamepad.rightStick, north);

                Assert.That(gamepad.rightStick.IsActuated(), Is.True);
                Assert.That(trace, Started<SectorInteraction>(action).AndThen(Performed<SectorInteraction>(action)));
                Assert.That(action.triggered, Is.True);

                trace.Clear();

                // Sweep to invalid sector
                Set(gamepad.rightStick, east);

                Assert.That(gamepad.rightStick.IsActuated(), Is.True);

                switch (sweepBehavior)
                {
                    case SectorInteraction.SweepBehavior.Locked:
                        Assert.That(trace, Is.Empty);
                        break;
                    case SectorInteraction.SweepBehavior.AllowReentry:
                    case SectorInteraction.SweepBehavior.DisallowReentry:
                    case SectorInteraction.SweepBehavior.HistoryIndependent:
                        Assert.That(trace, Canceled<SectorInteraction>(action));
                        break;
                    default:
                        Assert.Fail($"Unhandled {nameof(SectorInteraction.SweepBehavior)}={sweepBehavior}");
                        return;
                }

                Assert.That(action.triggered, Is.False);

                trace.Clear();

                // Sweep back into valid sector
                Set(gamepad.rightStick, north);

                Assert.That(gamepad.rightStick.IsActuated(), Is.True);

                switch (sweepBehavior)
                {
                    case SectorInteraction.SweepBehavior.Locked:
                    case SectorInteraction.SweepBehavior.DisallowReentry:
                        Assert.That(trace, Is.Empty);
                        Assert.That(action.triggered, Is.False);
                        break;
                    case SectorInteraction.SweepBehavior.AllowReentry:
                    case SectorInteraction.SweepBehavior.HistoryIndependent:
                        Assert.That(trace, Started<SectorInteraction>(action).AndThen(Performed<SectorInteraction>(action)));
                        Assert.That(action.triggered, Is.True);
                        break;
                    default:
                        Assert.Fail($"Unhandled {nameof(SectorInteraction.SweepBehavior)}={sweepBehavior}");
                        return;
                }

                trace.Clear();

                // Return to center, under threshold
                Set(gamepad.rightStick, center);

                Assert.That(gamepad.rightStick.IsActuated(), Is.False);
                Assert.That(trace, Canceled<SectorInteraction>(action));
                Assert.That(action.triggered, Is.False);
            }
        }

        [TestCase(SectorInteraction.SweepBehavior.Locked)]
        [TestCase(SectorInteraction.SweepBehavior.AllowReentry)]
        [TestCase(SectorInteraction.SweepBehavior.DisallowReentry)]
        [TestCase(SectorInteraction.SweepBehavior.HistoryIndependent)]
        public void StartedInvalidSweepBehavior(SectorInteraction.SweepBehavior sweepBehavior)
        {
            var gamepad = InputSystem.InputSystem.AddDevice<Gamepad>();

            var action = new InputAction(
                type: InputActionType.Value,
                binding: "<Gamepad>/rightStick",
                interactions: CreateInteractionString(SectorInteraction.Directions.North, sweepBehavior));

            action.Enable();

            var north = new Vector2(0f, 1f);
            var east = new Vector2(1f, 0f);
            var west = new Vector2(-1f, 0f);
            var center = Vector2.zero;

            // Starting below threshold
            Assert.That(gamepad.rightStick.IsActuated(), Is.False);
            Assert.That(action.triggered, Is.False);

            using (var trace = new InputActionTrace())
            {
                trace.SubscribeToAll();

                // Actuate above threshold, in invalid sector
                Set(gamepad.rightStick, east);

                Assert.That(gamepad.rightStick.IsActuated(), Is.True);
                Assert.That(trace, Is.Empty);
                Assert.That(action.triggered, Is.False);

                trace.Clear();

                // Sweep to valid sector
                Set(gamepad.rightStick, north);

                Assert.That(gamepad.rightStick.IsActuated(), Is.True);

                switch (sweepBehavior)
                {
                    case SectorInteraction.SweepBehavior.Locked:
                    case SectorInteraction.SweepBehavior.AllowReentry:
                    case SectorInteraction.SweepBehavior.DisallowReentry:
                        Assert.That(trace, Is.Empty);
                        Assert.That(action.triggered, Is.False);
                        break;
                    case SectorInteraction.SweepBehavior.HistoryIndependent:
                        Assert.That(trace, Started<SectorInteraction>(action).AndThen(Performed<SectorInteraction>(action)));
                        Assert.That(action.triggered, Is.True);
                        break;
                    default:
                        Assert.Fail($"Unhandled {nameof(SectorInteraction.SweepBehavior)}={sweepBehavior}");
                        return;
                }

                trace.Clear();

                // Sweep into invalid sector
                Set(gamepad.rightStick, west);

                Assert.That(gamepad.rightStick.IsActuated(), Is.True);

                switch (sweepBehavior)
                {
                    case SectorInteraction.SweepBehavior.Locked:
                    case SectorInteraction.SweepBehavior.AllowReentry:
                    case SectorInteraction.SweepBehavior.DisallowReentry:
                        Assert.That(trace, Is.Empty);
                        Assert.That(action.triggered, Is.False);
                        break;
                    case SectorInteraction.SweepBehavior.HistoryIndependent:
                        Assert.That(trace, Canceled<SectorInteraction>(action));
                        break;
                    default:
                        Assert.Fail($"Unhandled {nameof(SectorInteraction.SweepBehavior)}={sweepBehavior}");
                        return;
                }

                Assert.That(action.triggered, Is.False);

                trace.Clear();

                // Return to center, under threshold
                Set(gamepad.rightStick, center);

                Assert.That(gamepad.rightStick.IsActuated(), Is.False);
                Assert.That(trace, Canceled<SectorInteraction>(action));
                Assert.That(action.triggered, Is.False);
            }
        }

        static string CreateInteractionString(SectorInteraction.Directions directions, SectorInteraction.SweepBehavior sweepBehavior)
        {
            return $"sector(directions={(int)directions},sweepBehavior={(int)sweepBehavior})";
        }
    }
}

#endif
