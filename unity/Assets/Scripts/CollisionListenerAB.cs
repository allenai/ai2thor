public class CollisionListenerAB : CollisionListener
{
    // public ArticulatedArmController armController;

    public override bool ShouldHalt()
    {
        // TODO: Implement halting condition, you can use armController.GetArmTarget() , and othe properties
        return false;
    }
}
