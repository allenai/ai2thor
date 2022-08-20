// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{

  using Description = System.ComponentModel.DescriptionAttribute;

  public enum ChallengeVisibility : int
  {
    [Description("UNKNOWN")]
    Unknown,

    /// Only those invited can participate in it. Everyone can see it
    [Description("INVITE_ONLY")]
    InviteOnly,

    /// Everyone can participate and see this challenge
    [Description("PUBLIC")]
    Public,

    /// Only those invited can participate and see this challenge
    [Description("PRIVATE")]
    Private,

  }

}
