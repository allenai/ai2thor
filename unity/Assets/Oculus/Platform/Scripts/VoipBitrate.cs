// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform
{

  using Description = System.ComponentModel.DescriptionAttribute;

  public enum VoipBitrate : int
  {
    [Description("UNKNOWN")]
    Unknown,

    /// Very low audio quality for minimal network usage. This may not give the
    /// full range of Hz for audio, but it will save on network usage.
    [Description("B16000")]
    B16000,

    /// Lower audio quality but also less network usage.
    [Description("B24000")]
    B24000,

    /// This is the default bitrate for voip connections. It should be the best
    /// tradeoff between audio quality and network usage.
    [Description("B32000")]
    B32000,

    /// Higher audio quality at the expense of network usage. Good if there's music
    /// being streamed over the connections
    [Description("B64000")]
    B64000,

    /// Even higher audio quality for music streaming or radio-like quality.
    [Description("B96000")]
    B96000,

    /// At this point the audio quality should be preceptually indistinguishable
    /// from the uncompressed input.
    [Description("B128000")]
    B128000,

  }

}
