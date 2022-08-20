// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{

  using Description = System.ComponentModel.DescriptionAttribute;

  public enum NetSyncDisconnectReason : int
  {
    [Description("UNKNOWN")]
    Unknown,

    /// when disconnect was requested
    [Description("LOCAL_TERMINATED")]
    LocalTerminated,

    /// server intentionally closed the connection
    [Description("SERVER_TERMINATED")]
    ServerTerminated,

    /// initial connection never succeeded
    [Description("FAILED")]
    Failed,

    /// network timeout
    [Description("LOST")]
    Lost,

  }

}
