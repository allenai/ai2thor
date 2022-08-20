// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

#pragma warning disable 0618

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class AssetDetails
  {
    public readonly UInt64 AssetId;
    public readonly string AssetType;
    public readonly string DownloadStatus;
    public readonly string Filepath;
    public readonly string IapStatus;
    // May be null. Check before using.
    public readonly LanguagePackInfo LanguageOptional;
    [Obsolete("Deprecated in favor of LanguageOptional")]
    public readonly LanguagePackInfo Language;
    public readonly string Metadata;


    public AssetDetails(IntPtr o)
    {
      AssetId = CAPI.ovr_AssetDetails_GetAssetId(o);
      AssetType = CAPI.ovr_AssetDetails_GetAssetType(o);
      DownloadStatus = CAPI.ovr_AssetDetails_GetDownloadStatus(o);
      Filepath = CAPI.ovr_AssetDetails_GetFilepath(o);
      IapStatus = CAPI.ovr_AssetDetails_GetIapStatus(o);
      {
        var pointer = CAPI.ovr_AssetDetails_GetLanguage(o);
        Language = new LanguagePackInfo(pointer);
        if (pointer == IntPtr.Zero) {
          LanguageOptional = null;
        } else {
          LanguageOptional = Language;
        }
      }
      Metadata = CAPI.ovr_AssetDetails_GetMetadata(o);
    }
  }

  public class AssetDetailsList : DeserializableList<AssetDetails> {
    public AssetDetailsList(IntPtr a) {
      var count = (int)CAPI.ovr_AssetDetailsArray_GetSize(a);
      _Data = new List<AssetDetails>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new AssetDetails(CAPI.ovr_AssetDetailsArray_GetElement(a, (UIntPtr)i)));
      }

    }

  }
}
