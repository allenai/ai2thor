#if OVR_PLATFORM_ASYNC_MESSAGES
using System.Threading.Tasks;
#endif
using UnityEngine;

namespace Oculus.Platform
{
  public sealed class Request<T> : Request
  {
#if OVR_PLATFORM_ASYNC_MESSAGES
    private TaskCompletionSource<Message<T>> tcs_ = null;
#endif
    private Message<T>.Callback callback_ = null;

    public Request(ulong requestID) : base (requestID) { }

    public Request<T> OnComplete(Message<T>.Callback callback)
    {
      if (callback_ != null)
      {
        throw new UnityException("Attempted to attach multiple handlers to a Request.  This is not allowed.");
      }

#if OVR_PLATFORM_ASYNC_MESSAGES
      if (tcs_ != null)
      {
        throw new UnityException("Attempted to attach multiple handlers to a Request.  This is not allowed.");
      }
#endif

      callback_ = callback;
      Callback.AddRequest(this);
      return this;
    }

#if OVR_PLATFORM_ASYNC_MESSAGES
    new public async Task<Message<T>> Gen()
    {
      if (callback_ != null || tcs_ != null)
      {
        throw new UnityException("Attempted to attach multiple handlers to a Request.  This is not allowed.");
      }

      tcs_ = new TaskCompletionSource<Message<T>>();
      Callback.AddRequest(this);
      return await tcs_.Task;
    }
#endif

    override public void HandleMessage(Message msg)
    {
      if (! (msg is Message<T>))
      {
        Debug.LogError("Unable to handle message: " + msg.GetType());
        return;
      }

#if OVR_PLATFORM_ASYNC_MESSAGES
      if (tcs_ != null)
      {
        tcs_.SetResult( (Message<T>)msg);
        return;
      }
#endif

      if (callback_ != null)
      {
        callback_( (Message<T>)msg);
        return;
      }

      throw new UnityException("Request with no handler.  This should never happen.");
    }
  }

  public class Request
  {
#if OVR_PLATFORM_ASYNC_MESSAGES
    private TaskCompletionSource<Message> tcs_;
#endif
    private Message.Callback callback_;

    public Request(ulong requestID) {this.RequestID = requestID;}
    public ulong RequestID {get; set;}

    public Request OnComplete(Message.Callback callback)
    {
      callback_ = callback;
      Callback.AddRequest(this);
      return this;
    }

#if OVR_PLATFORM_ASYNC_MESSAGES
    public async Task<Message> Gen() {
      tcs_ = new TaskCompletionSource<Message>();
      Callback.AddRequest(this);
      return await tcs_.Task;
    }
#endif

    virtual public void HandleMessage(Message msg)
    {
#if OVR_PLATFORM_ASYNC_MESSAGES
      if (tcs_ != null)
      {
        tcs_.SetResult(msg);
        return;
      }
#endif

      if (callback_ != null)
      {
        callback_(msg);
        return;
      }

      throw new UnityException("Request with no handler.  This should never happen.");
    }

    /**
     * This will run callbacks on all messages that returned from the server.
     * If too many message are coming back at once, then a limit can be passed in
     * as an arg to limit the number of messages to run callbacks on at a time
     */
    public static void RunCallbacks(uint limit = 0)
    {
      // default of 0 will run callbacks on all messages on the queue
      if (limit == 0)
      {
        Callback.RunCallbacks();
      }
      else
      {
        Callback.RunLimitedCallbacks(limit);
      }
    }
  }
}
