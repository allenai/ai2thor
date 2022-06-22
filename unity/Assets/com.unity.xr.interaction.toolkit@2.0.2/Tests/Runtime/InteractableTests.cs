using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UnityEngine.XR.Interaction.Toolkit.Tests
{
    [TestFixture]
    class InteractableTests
    {
        static readonly InteractableSelectMode[] s_SelectModes =
        {
            InteractableSelectMode.Single,
            InteractableSelectMode.Multiple,
        };

        [TearDown]
        public void TearDown()
        {
            TestUtilities.DestroyAllSceneObjects();
        }

        [UnityTest]
        public IEnumerator InteractableIsHoveredWhileAnyInteractorHovering()
        {
            TestUtilities.CreateInteractionManager();
            var interactor1 = TestUtilities.CreateMockInteractor();
            var interactor2 = TestUtilities.CreateMockInteractor();
            var interactable = TestUtilities.CreateGrabInteractable();

            Assert.That(interactable.isHovered, Is.False);
            Assert.That(interactable.interactorsHovering, Is.Empty);

            interactor1.validTargets.Add(interactable);
            interactor2.validTargets.Add(interactable);

            yield return null;

            Assert.That(interactable.isHovered, Is.True);
            Assert.That(interactable.interactorsHovering, Is.EquivalentTo(new[] { interactor1, interactor2 }));

            interactor2.validTargets.Clear();

            yield return null;

            Assert.That(interactable.isHovered, Is.True);
            Assert.That(interactable.interactorsHovering, Is.EquivalentTo(new[] { interactor1 }));

            interactor1.validTargets.Clear();

            yield return null;

            Assert.That(interactable.isHovered, Is.False);
            Assert.That(interactable.interactorsHovering, Is.Empty);
        }

        [UnityTest]
        public IEnumerator InteractableSelectModeSelect([ValueSource(nameof(s_SelectModes))] InteractableSelectMode selectMode)
        {
            TestUtilities.CreateInteractionManager();
            var interactor1 = TestUtilities.CreateMockInteractor();
            var interactor2 = TestUtilities.CreateMockInteractor();
            var interactable = TestUtilities.CreateGrabInteractable();
            interactable.selectMode = selectMode;

            interactor1.validTargets.Add(interactable);

            yield return null;

            Assert.That(interactable.isSelected, Is.True);
            Assert.That(interactable.interactorsSelecting, Is.EqualTo(new[] { interactor1 }));

            interactor2.validTargets.Add(interactable);

            yield return null;

            Assert.That(interactable.isSelected, Is.True);
            switch (selectMode)
            {
                case InteractableSelectMode.Single:
                    Assert.That(interactable.interactorsSelecting, Is.EqualTo(new[] { interactor2 }));
                    break;
                case InteractableSelectMode.Multiple:
                    Assert.That(interactable.interactorsSelecting, Is.EqualTo(new[] { interactor1, interactor2 }));
                    break;
                default:
                    Assert.Fail($"Unhandled {nameof(InteractableSelectMode)}={selectMode}");
                    break;
            }
        }
    }
}
