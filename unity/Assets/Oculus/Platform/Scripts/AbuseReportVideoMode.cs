// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{

  using Description = System.ComponentModel.DescriptionAttribute;

  public enum AbuseReportVideoMode : int
  {
    [Description("UNKNOWN")]
    Unknown,

    /// The UI will collect video evidence if the object_type supports it.
    [Description("COLLECT")]
    Collect,

    /// The UI will try to collect video evidence if the object_type supports it,
    /// but will allow the user to skip that step if they wish.
    [Description("OPTIONAL")]
    Optional,

    /// The UI will not collect video evidence.
    [Description("SKIP")]
    Skip,

  }

}
