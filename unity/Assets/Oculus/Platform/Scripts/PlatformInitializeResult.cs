// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{

  using Description = System.ComponentModel.DescriptionAttribute;

  public enum PlatformInitializeResult : int
  {
    [Description("SUCCESS")]
    Success = 0,

    [Description("UNINITIALIZED")]
    Uninitialized = -1,

    [Description("PRE_LOADED")]
    PreLoaded = -2,

    [Description("FILE_INVALID")]
    FileInvalid = -3,

    [Description("SIGNATURE_INVALID")]
    SignatureInvalid = -4,

    [Description("UNABLE_TO_VERIFY")]
    UnableToVerify = -5,

    [Description("VERSION_MISMATCH")]
    VersionMismatch = -6,

    [Description("UNKNOWN")]
    Unknown = -7,

    [Description("INVALID_CREDENTIALS")]
    InvalidCredentials = -8,

    [Description("NOT_ENTITLED")]
    NotEntitled = -9,

  }

}
