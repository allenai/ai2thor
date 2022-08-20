// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class CalApplicationSuggestion
  {
    public readonly UInt64 ID;
    public readonly string SocialContext;


    public CalApplicationSuggestion(IntPtr o)
    {
      ID = CAPI.ovr_CalApplicationSuggestion_GetID(o);
      SocialContext = CAPI.ovr_CalApplicationSuggestion_GetSocialContext(o);
    }
  }

  public class CalApplicationSuggestionList : DeserializableList<CalApplicationSuggestion> {
    public CalApplicationSuggestionList(IntPtr a) {
      var count = (int)CAPI.ovr_CalApplicationSuggestionArray_GetSize(a);
      _Data = new List<CalApplicationSuggestion>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new CalApplicationSuggestion(CAPI.ovr_CalApplicationSuggestionArray_GetElement(a, (UIntPtr)i)));
      }

    }

  }
}
