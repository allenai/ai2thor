namespace Oculus.Platform
{
  using UnityEngine;
  using System;
  using System.Collections.Generic;

  public static class Callback
  {
    #region Notification Callbacks: Exposed through Oculus.Platform.Platform

    internal static void SetNotificationCallback<T>(Message.MessageType type, Message<T>.Callback callback)
    {
      if (callback == null) {
        throw new Exception ("Cannot provide a null notification callback.");
      }

      notificationCallbacks[type] = new RequestCallback<T>(callback);

      if (type == Message.MessageType.Notification_Room_InviteAccepted)
      {
        FlushRoomInviteNotificationQueue();
      }
      else if (type == Message.MessageType.Notification_GroupPresence_JoinIntentReceived)
      {
        FlushJoinIntentNotificationQueue();
      }
    }

    internal static void SetNotificationCallback(Message.MessageType type, Message.Callback callback)
    {
      if (callback == null) {
        throw new Exception ("Cannot provide a null notification callback.");
      }

      notificationCallbacks[type] = new RequestCallback(callback);
    }
    #endregion

    #region Adding and running request handlers
    internal static void AddRequest(Request request)
    {
      if (request.RequestID == 0)
      {
        // An early out error happened in the C SDK. Do not add it to the mapping of callbacks
        Debug.LogError("An unknown error occurred. Request failed.");
        return;
      }
      requestIDsToRequests[request.RequestID] = request;
    }

    internal static void RunCallbacks()
    {
      while (true)
      {
        var msg = Platform.Message.PopMessage();
        if (msg == null)
        {
          break;
        }

        HandleMessage(msg);
      }

    }

    internal static void RunLimitedCallbacks(uint limit)
    {
      for (var i = 0; i < limit; ++i)
      {
        var msg = Platform.Message.PopMessage();
        if (msg == null)
        {
          break;
        }

        HandleMessage(msg);
      }
    }

    internal static void OnApplicationQuit()
    {
      // Clear out all outstanding callbacks
      requestIDsToRequests.Clear();
      notificationCallbacks.Clear();
    }

  #endregion

  #region Callback Internals
  private static Dictionary<ulong, Request> requestIDsToRequests = new Dictionary<ulong, Request>();
    private static Dictionary<Message.MessageType, RequestCallback> notificationCallbacks = new Dictionary<Message.MessageType, RequestCallback>();

    private static bool hasRegisteredRoomInviteNotificationHandler = false;
    private static List<Message> pendingRoomInviteNotifications = new List<Message>();
    private static void FlushRoomInviteNotificationQueue() {
        hasRegisteredRoomInviteNotificationHandler = true;
        foreach (Message msg in pendingRoomInviteNotifications) {
            HandleMessage(msg);
        }
        pendingRoomInviteNotifications.Clear();
    }

    private static bool hasRegisteredJoinIntentNotificationHandler = false;
    private static Message latestPendingJoinIntentNotifications;
    private static void FlushJoinIntentNotificationQueue() {
        hasRegisteredJoinIntentNotificationHandler = true;
        if (latestPendingJoinIntentNotifications != null) {
          HandleMessage(latestPendingJoinIntentNotifications);
        }
        latestPendingJoinIntentNotifications = null;
    }

    private class RequestCallback
    {
      private Message.Callback messageCallback;

      public RequestCallback() { }

      public RequestCallback(Message.Callback callback)
      {
        this.messageCallback = callback;
      }

      public virtual void HandleMessage(Message msg)
      {
        if (messageCallback != null)
        {
          messageCallback(msg);
        }
      }
    }

    private sealed class RequestCallback<T> : RequestCallback
    {
      private Message<T>.Callback callback;
      public RequestCallback(Message<T>.Callback callback)
      {
        this.callback = callback;
      }

      public override void HandleMessage(Message msg)
      {
        if (callback != null)
        {
          if (msg is Message<T>)
          {
            callback((Message<T>)msg);
          }
          else
          {
            Debug.LogError("Unable to handle message: " + msg.GetType());
          }
        }
      }
    }

    internal static void HandleMessage(Message msg)
    {
      Request request;
      if (msg.RequestID != 0 && requestIDsToRequests.TryGetValue(msg.RequestID, out request)) {
        try {
          request.HandleMessage(msg);
        } finally {
          requestIDsToRequests.Remove(msg.RequestID);
        }
        return;
      }

      RequestCallback callbackHolder;
      if (notificationCallbacks.TryGetValue(msg.Type, out callbackHolder))
      {
        callbackHolder.HandleMessage(msg);
      }
      // We need to queue up Join Intents because the callback runner will be called before a handler has beeen set.
      else if (!hasRegisteredJoinIntentNotificationHandler && msg.Type == Message.MessageType.Notification_GroupPresence_JoinIntentReceived)
      {
        latestPendingJoinIntentNotifications = msg;
      }
      // We need to queue up GameInvites because the callback runner will be called before a handler has beeen set.
      else if (!hasRegisteredRoomInviteNotificationHandler && msg.Type == Message.MessageType.Notification_Room_InviteAccepted)
      {
        pendingRoomInviteNotifications.Add(msg);
      }
    }

    #endregion
  }
}
