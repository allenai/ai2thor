// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{

  using Description = System.ComponentModel.DescriptionAttribute;

  public enum AbuseReportType : int
  {
    [Description("UNKNOWN")]
    Unknown,

    /// A report for something besides a user, like a world.
    [Description("OBJECT")]
    Object,

    /// A report for a user's behavior or profile.
    [Description("USER")]
    User,

  }

}
