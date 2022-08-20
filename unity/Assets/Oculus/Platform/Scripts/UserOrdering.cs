// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{

  using Description = System.ComponentModel.DescriptionAttribute;

  public enum UserOrdering : int
  {
    [Description("UNKNOWN")]
    Unknown,

    /// No preference for ordering (could be in any or no order)
    [Description("NONE")]
    None,

    /// Orders by online users first and then offline users. Within each group the
    /// users are ordered alphabetically by display name
    [Description("PRESENCE_ALPHABETICAL")]
    PresenceAlphabetical,

  }

}
