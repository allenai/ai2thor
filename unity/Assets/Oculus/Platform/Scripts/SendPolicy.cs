// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{

  using Description = System.ComponentModel.DescriptionAttribute;

  public enum SendPolicy : int
  {
    /// Sends a message using an unreliable data channel (UDP-based). No delivery
    /// or ordering guarantees are provided. Sending will fail unless a connection
    /// to the peer is already established, either via a previous call to
    /// Net.SendPacket() or an explicit Net.Connect().
    ///
    /// Ideally, each message should fit into a single packet. Therefore, it is
    /// recommended to keep them under 1200 bytes.
    [Description("UNRELIABLE")]
    Unreliable,

    /// Messages are delivered reliably and in order. The networking layer retries
    /// until each message is acknowledged by the peer. Outgoing messages are
    /// buffered until a working connection to the peer is established.
    [Description("RELIABLE")]
    Reliable,

    [Description("UNKNOWN")]
    Unknown,

  }

}
