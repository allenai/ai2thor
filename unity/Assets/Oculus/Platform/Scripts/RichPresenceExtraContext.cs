// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{

  using Description = System.ComponentModel.DescriptionAttribute;

  public enum RichPresenceExtraContext : int
  {
    [Description("UNKNOWN")]
    Unknown,

    /// Display nothing
    [Description("NONE")]
    None,

    /// Display the current amount with the user over the max
    [Description("CURRENT_CAPACITY")]
    CurrentCapacity,

    /// Display how long ago the match/game/race/etc started
    [Description("STARTED_AGO")]
    StartedAgo,

    /// Display how soon the match/game/race/etc will end
    [Description("ENDING_IN")]
    EndingIn,

    /// Display that this user is looking for a match
    [Description("LOOKING_FOR_A_MATCH")]
    LookingForAMatch,

  }

}
