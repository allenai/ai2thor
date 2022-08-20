using UnityEngine;
using System.Collections;
using System;

namespace Oculus.Platform
{
  public interface IMicrophone
  {
    void Start();

    void Stop();

    float[] Update();
  }
}
