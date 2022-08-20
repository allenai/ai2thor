// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class AchievementDefinition
  {
    public readonly AchievementType Type;
    public readonly string Name;
    public readonly uint BitfieldLength;
    public readonly ulong Target;


    public AchievementDefinition(IntPtr o)
    {
      Type = CAPI.ovr_AchievementDefinition_GetType(o);
      Name = CAPI.ovr_AchievementDefinition_GetName(o);
      BitfieldLength = CAPI.ovr_AchievementDefinition_GetBitfieldLength(o);
      Target = CAPI.ovr_AchievementDefinition_GetTarget(o);
    }
  }

  public class AchievementDefinitionList : DeserializableList<AchievementDefinition> {
    public AchievementDefinitionList(IntPtr a) {
      var count = (int)CAPI.ovr_AchievementDefinitionArray_GetSize(a);
      _Data = new List<AchievementDefinition>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new AchievementDefinition(CAPI.ovr_AchievementDefinitionArray_GetElement(a, (UIntPtr)i)));
      }

      _NextUrl = CAPI.ovr_AchievementDefinitionArray_GetNextUrl(a);
    }

  }
}
