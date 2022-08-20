// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

namespace Oculus.Platform.Models
{
  using System;
  using System.Collections;
  using Oculus.Platform.Models;
  using System.Collections.Generic;
  using UnityEngine;

  public class BlockedUser
  {
    public readonly UInt64 Id;


    public BlockedUser(IntPtr o)
    {
      Id = CAPI.ovr_BlockedUser_GetId(o);
    }
  }

  public class BlockedUserList : DeserializableList<BlockedUser> {
    public BlockedUserList(IntPtr a) {
      var count = (int)CAPI.ovr_BlockedUserArray_GetSize(a);
      _Data = new List<BlockedUser>(count);
      for (int i = 0; i < count; i++) {
        _Data.Add(new BlockedUser(CAPI.ovr_BlockedUserArray_GetElement(a, (UIntPtr)i)));
      }

      _NextUrl = CAPI.ovr_BlockedUserArray_GetNextUrl(a);
    }

  }
}
