// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{

  using Description = System.ComponentModel.DescriptionAttribute;

  public enum LaunchResult : int
  {
    [Description("UNKNOWN")]
    Unknown,

    [Description("SUCCESS")]
    Success,

    [Description("FAILED_ROOM_FULL")]
    FailedRoomFull,

    [Description("FAILED_GAME_ALREADY_STARTED")]
    FailedGameAlreadyStarted,

    [Description("FAILED_ROOM_NOT_FOUND")]
    FailedRoomNotFound,

    [Description("FAILED_USER_DECLINED")]
    FailedUserDeclined,

    [Description("FAILED_OTHER_REASON")]
    FailedOtherReason,

  }

}
