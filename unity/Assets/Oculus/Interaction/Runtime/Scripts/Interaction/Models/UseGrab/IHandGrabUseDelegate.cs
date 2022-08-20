namespace Oculus.Interaction.HandGrab
{
    public interface IHandGrabUseDelegate
    {
        void BeginUse();
        void EndUse();

        float ComputeUseStrength(float strength);
    }
}
