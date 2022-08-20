// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class LanguagePackInfo
  {
    public readonly string EnglishName;
    public readonly string NativeName;
    public readonly string Tag;


    public LanguagePackInfo(IntPtr o)
    {
      EnglishName = CAPI.ovr_LanguagePackInfo_GetEnglishName(o);
      NativeName = CAPI.ovr_LanguagePackInfo_GetNativeName(o);
      Tag = CAPI.ovr_LanguagePackInfo_GetTag(o);
    }
  }

}
