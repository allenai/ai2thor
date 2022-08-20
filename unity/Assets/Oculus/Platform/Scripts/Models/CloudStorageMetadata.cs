// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class CloudStorageMetadata
  {
    public readonly string Bucket;
    public readonly long Counter;
    public readonly uint DataSize;
    public readonly string ExtraData;
    public readonly string Key;
    public readonly ulong SaveTime;
    public readonly CloudStorageDataStatus Status;
    public readonly string VersionHandle;


    public CloudStorageMetadata(IntPtr o)
    {
      Bucket = CAPI.ovr_CloudStorageMetadata_GetBucket(o);
      Counter = CAPI.ovr_CloudStorageMetadata_GetCounter(o);
      DataSize = CAPI.ovr_CloudStorageMetadata_GetDataSize(o);
      ExtraData = CAPI.ovr_CloudStorageMetadata_GetExtraData(o);
      Key = CAPI.ovr_CloudStorageMetadata_GetKey(o);
      SaveTime = CAPI.ovr_CloudStorageMetadata_GetSaveTime(o);
      Status = CAPI.ovr_CloudStorageMetadata_GetStatus(o);
      VersionHandle = CAPI.ovr_CloudStorageMetadata_GetVersionHandle(o);
    }
  }

  public class CloudStorageMetadataList : DeserializableList<CloudStorageMetadata> {
    public CloudStorageMetadataList(IntPtr a) {
      var count = (int)CAPI.ovr_CloudStorageMetadataArray_GetSize(a);
      _Data = new List<CloudStorageMetadata>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new CloudStorageMetadata(CAPI.ovr_CloudStorageMetadataArray_GetElement(a, (UIntPtr)i)));
      }

      _NextUrl = CAPI.ovr_CloudStorageMetadataArray_GetNextUrl(a);
    }

  }
}
