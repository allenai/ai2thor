// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class LinkedAccount
  {
    public readonly string AccessToken;
    public readonly ServiceProvider ServiceProvider;
    public readonly string UserId;


    public LinkedAccount(IntPtr o)
    {
      AccessToken = CAPI.ovr_LinkedAccount_GetAccessToken(o);
      ServiceProvider = CAPI.ovr_LinkedAccount_GetServiceProvider(o);
      UserId = CAPI.ovr_LinkedAccount_GetUserId(o);
    }
  }

  public class LinkedAccountList : DeserializableList<LinkedAccount> {
    public LinkedAccountList(IntPtr a) {
      var count = (int)CAPI.ovr_LinkedAccountArray_GetSize(a);
      _Data = new List<LinkedAccount>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new LinkedAccount(CAPI.ovr_LinkedAccountArray_GetElement(a, (UIntPtr)i)));
      }

    }

  }
}
