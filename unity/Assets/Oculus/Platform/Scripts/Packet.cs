namespace Oculus.Platform
{
  using System;
  using System.Runtime.InteropServices;

  public sealed class Packet : IDisposable
  {
    private readonly ulong size;
    private readonly IntPtr packetHandle;

    public Packet(IntPtr packetHandle)
    {
      this.packetHandle = packetHandle;
      size = (ulong) CAPI.ovr_Packet_GetSize(packetHandle);
    }

    /**
     * Copies all the bytes in the payload into byte[] destination.  ex:
     *   Package package ...
     *   byte[] destination = new byte[package.Size];
     *   package.ReadBytes(destination);
     */
    public ulong ReadBytes(byte[] destination)
    {
      if ((ulong) destination.LongLength < size)
      {
        throw new System.ArgumentException(String.Format("Destination array was not big enough to hold {0} bytes", size));
      }
      Marshal.Copy(CAPI.ovr_Packet_GetBytes(packetHandle), destination, 0, (int) size);
      return size;
    }

    public UInt64 SenderID
    {
      get { return CAPI.ovr_Packet_GetSenderID(packetHandle); }
    }

    public ulong Size
    {
      get { return size; }
    }

    public SendPolicy Policy
    {
      get { return (SendPolicy)CAPI.ovr_Packet_GetSendPolicy(packetHandle); }
    }

    #region IDisposable

    ~Packet()
    {
      Dispose();
    }

    public void Dispose()
    {
      CAPI.ovr_Packet_Free(packetHandle);
      GC.SuppressFinalize(this);
    }

    #endregion
  }
}
