namespace Oculus.Platform
{
  public interface IVoipPCMSource
  {
    int GetPCM(float[] dest, int length);

    void SetSenderID(ulong senderID);

    void Update();

    int PeekSizeElements();
  }
}
