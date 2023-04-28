public class CollisionListenerAB : CollisionListener {

    // TODO: no longer needed

    // public StretchABArmController armController;

    public override bool ShouldHalt() {
        
        // TODO: Implement halting condition, you can use armController.GetArmTarget() , and othe properties
        return false;
    }

}