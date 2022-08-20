using System;
using UnityEngine;
using Oculus.Platform;
using Oculus.Platform.Models;

namespace Oculus.Platform.Samples.EntitlementCheck
{
  public class EntitlementCheck : MonoBehaviour
  {
    // Implements a default behavior for entitlement check failures by simply exiting the app.
    // Set to false if the app wants to provide custom logic to handle entitlement check failures.
    // For example, the app can instead display a modal dialog to the user and exit gracefully.
    public bool exitAppOnFailure = true;

    // The app can optionally subscribe to these events to do custom entitlement check logic.
    public static event Action UserFailedEntitlementCheck;
    public static event Action UserPassedEntitlementCheck;

    void Start()
    {
      try
      {
        // Init the Oculust Platform SDK and send an entitlement check request.
        if (!Oculus.Platform.Core.IsInitialized())
        {
          Oculus.Platform.Core.Initialize();
        }

        Entitlements.IsUserEntitledToApplication().OnComplete(EntitlementCheckCallback);
      }
      catch
      {
        // Treat any potential initialization exceptions as an entitlement check failure.
        HandleEntitlementCheckResult(false);
      }
    }

    // Called when the Oculus Platform completes the async entitlement check request and a result is available.
    void EntitlementCheckCallback(Message msg)
    {
      // If the user passed the entitlement check, msg.IsError will be false.
      // If the user failed the entitlement check, msg.IsError will be true.
      HandleEntitlementCheckResult(msg.IsError == false);
    }

    void HandleEntitlementCheckResult(bool result)
    {
      if (result) // User passed entitlement check
      {
        Debug.Log("Oculus user entitlement check successful.");

        try
        {
          // Raise the user passed entitlement check event if the app subscribed a handler to it.
          if (UserPassedEntitlementCheck != null)
          {
            UserPassedEntitlementCheck();
          }
        }
        catch
        {
          // Suppressing any exceptions to avoid potential exceptions in the app-provided event handler.
          Debug.LogError("Suppressed exception in app-provided UserPassedEntitlementCheck() event handler.");
        }
      }
      else // User failed entitlement check
      {
        try
        {
          // Raise the user failed entitlement check event if the app subscribed a handler to it.
          if (UserFailedEntitlementCheck != null)
          {
            UserFailedEntitlementCheck();
          }
        }
        catch
        {
          // Suppressing any exceptions to avoid potential exceptions in the app-provided event handler.
          // Ensures the default entitlement check behavior will still execute, if enabled.
          Debug.LogError("Suppressed exception in app-provided UserFailedEntitlementCheck() event handler.");
        }

        if (exitAppOnFailure)
        {
          // Implements a default behavior for an entitlement check failure -- log the failure and exit the app.
          Debug.LogError("Oculus user entitlement check failed. Exiting now.");
#if UNITY_EDITOR
          UnityEditor.EditorApplication.isPlaying = false;
#else
          UnityEngine.Application.Quit();
#endif
        }
        else
        {
          Debug.LogError("Oculus user entitlement check failed.");
        }
      }
    }
  }
}
