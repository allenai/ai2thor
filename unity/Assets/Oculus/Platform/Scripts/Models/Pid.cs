// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class Pid
  {
    public readonly string Id;


    public Pid(IntPtr o)
    {
      Id = CAPI.ovr_Pid_GetId(o);
    }
  }

  public class PidList : DeserializableList<Pid> {
    public PidList(IntPtr a) {
      var count = (int)CAPI.ovr_PidArray_GetSize(a);
      _Data = new List<Pid>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new Pid(CAPI.ovr_PidArray_GetElement(a, (UIntPtr)i)));
      }

    }

  }
}
