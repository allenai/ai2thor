// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{

  using Description = System.ComponentModel.DescriptionAttribute;

  public enum PeerConnectionState : int
  {
    [Description("UNKNOWN")]
    Unknown,

    /// Connection to the peer is established.
    [Description("CONNECTED")]
    Connected,

    /// A timeout expired while attempting to (re)establish a connection. This can
    /// happen if peer is unreachable or rejected the connection.
    [Description("TIMEOUT")]
    Timeout,

    /// Connection to the peer is closed. A connection transitions into this state
    /// when it is explicitly closed by either the local or remote peer calling
    /// Net.Close(). It also enters this state if the remote peer no longer
    /// responds to our keep-alive probes.
    [Description("CLOSED")]
    Closed,

  }

}
