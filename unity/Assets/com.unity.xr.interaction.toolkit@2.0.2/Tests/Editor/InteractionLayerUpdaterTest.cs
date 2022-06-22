using NUnit.Framework;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Tests;

namespace UnityEditor.XR.Interaction.Toolkit.Tests
{
    [TestFixture]
    class InteractionLayerUpdaterTest
    {
        [TearDown]
        public void TearDown()
        {
            TestUtilities.DestroyAllSceneObjects();
        }
        
#pragma warning disable 618
        [Test]
        public void InteractableUpdate()
        {
            TestUtilities.CreateInteractionManager();
            var interactable = TestUtilities.CreateGrabInteractable();
            var waterLayerMask = 1 << LayerMask.NameToLayer("Water");
            LayerMask usedLayers = 0;
            
            interactable.interactionLayerMask = waterLayerMask;
            Assert.That(interactable.interactionLayerMask.value, Is.EqualTo(waterLayerMask));
            Assert.That(interactable.interactionLayers.value, Is.EqualTo(1 << InteractionLayerMask.NameToLayer("Default")));

            InteractionLayerUpdater.TryUpdateInteractionLayerMaskProperty(interactable, ref usedLayers);
            
            Assert.That(interactable.interactionLayerMask.value, Is.EqualTo(waterLayerMask));
            Assert.That(interactable.interactionLayers.value , Is.EqualTo(waterLayerMask));
            Assert.That(usedLayers.value , Is.EqualTo(waterLayerMask));
        }
        
        [Test]
        public void InteractorUpdate()
        {
            TestUtilities.CreateInteractionManager();
            var interactor = TestUtilities.CreateSocketInteractor();
            LayerMask usedLayers = 0;
            interactor.interactionLayerMask = 0;
            
            Assert.That(interactor.interactionLayerMask.value, Is.EqualTo(0));
            Assert.That(interactor.interactionLayers.value, Is.EqualTo(-1));

            InteractionLayerUpdater.TryUpdateInteractionLayerMaskProperty(interactor, ref usedLayers);
            
            Assert.That(interactor.interactionLayerMask.value, Is.EqualTo(0));
            Assert.That(interactor.interactionLayers.value, Is.EqualTo(0));
            Assert.That(usedLayers.value, Is.EqualTo(0));
        }

        [Test]
        public void Layer31MaskUpdate()
        {
            TestUtilities.CreateInteractionManager();
            var interactor = TestUtilities.CreateSocketInteractor();
            var thirtyOneLayerMask = 1 << 31;
            LayerMask usedLayers = 0;
            
            interactor.interactionLayerMask = thirtyOneLayerMask;
            Assert.That(interactor.interactionLayerMask.value, Is.EqualTo(thirtyOneLayerMask));
            Assert.That(interactor.interactionLayers.value, Is.EqualTo(-1));
            
            InteractionLayerUpdater.TryUpdateInteractionLayerMaskProperty(interactor, ref usedLayers);
            
            Assert.That(interactor.interactionLayerMask.value, Is.EqualTo(thirtyOneLayerMask));
            Assert.That(interactor.interactionLayers.value , Is.EqualTo(thirtyOneLayerMask));
            Assert.That(usedLayers.value , Is.EqualTo(thirtyOneLayerMask));
        }

        [Test]
        public void EverythingMaskUpdate()
        {
            TestUtilities.CreateInteractionManager();
            var interactable = TestUtilities.CreateGrabInteractable();
            var everythingMask = -1;
            LayerMask usedLayers = 0;
            
            interactable.interactionLayerMask = everythingMask;
            Assert.That(interactable.interactionLayerMask.value, Is.EqualTo(everythingMask));
            Assert.That(interactable.interactionLayers.value, Is.EqualTo(1 << InteractionLayerMask.NameToLayer("Default")));
            
            InteractionLayerUpdater.TryUpdateInteractionLayerMaskProperty(interactable, ref usedLayers);
            
            Assert.That(interactable.interactionLayerMask.value, Is.EqualTo(everythingMask));
            Assert.That(interactable.interactionLayers.value , Is.EqualTo(everythingMask));
            Assert.That(usedLayers.value , Is.EqualTo(0));
        }
#pragma warning restore 618
    }
}
