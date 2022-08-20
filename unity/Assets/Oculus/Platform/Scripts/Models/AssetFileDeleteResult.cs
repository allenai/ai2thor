// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class AssetFileDeleteResult
  {
    public readonly UInt64 AssetFileId;
    public readonly UInt64 AssetId;
    public readonly string Filepath;
    public readonly bool Success;


    public AssetFileDeleteResult(IntPtr o)
    {
      AssetFileId = CAPI.ovr_AssetFileDeleteResult_GetAssetFileId(o);
      AssetId = CAPI.ovr_AssetFileDeleteResult_GetAssetId(o);
      Filepath = CAPI.ovr_AssetFileDeleteResult_GetFilepath(o);
      Success = CAPI.ovr_AssetFileDeleteResult_GetSuccess(o);
    }
  }

}
