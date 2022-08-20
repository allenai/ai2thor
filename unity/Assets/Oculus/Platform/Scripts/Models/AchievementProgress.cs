// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class AchievementProgress
  {
    public readonly string Bitfield;
    public readonly ulong Count;
    public readonly bool IsUnlocked;
    public readonly string Name;
    public readonly DateTime UnlockTime;


    public AchievementProgress(IntPtr o)
    {
      Bitfield = CAPI.ovr_AchievementProgress_GetBitfield(o);
      Count = CAPI.ovr_AchievementProgress_GetCount(o);
      IsUnlocked = CAPI.ovr_AchievementProgress_GetIsUnlocked(o);
      Name = CAPI.ovr_AchievementProgress_GetName(o);
      UnlockTime = CAPI.ovr_AchievementProgress_GetUnlockTime(o);
    }
  }

  public class AchievementProgressList : DeserializableList<AchievementProgress> {
    public AchievementProgressList(IntPtr a) {
      var count = (int)CAPI.ovr_AchievementProgressArray_GetSize(a);
      _Data = new List<AchievementProgress>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new AchievementProgress(CAPI.ovr_AchievementProgressArray_GetElement(a, (UIntPtr)i)));
      }

      _NextUrl = CAPI.ovr_AchievementProgressArray_GetNextUrl(a);
    }

  }
}
