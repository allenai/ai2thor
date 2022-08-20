// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]

namespace Oculus.Platform
{
  using UnityEngine;
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Runtime.InteropServices;

  public sealed class Core {
    private static bool IsPlatformInitialized = false;
    public static bool IsInitialized()
    {
      return IsPlatformInitialized;
    }

    // If LogMessages is true, then the contents of each request response
    // will be printed using Debug.Log. This allocates a lot of heap memory,
    // and so should not be called outside of testing and debugging.
    public static bool LogMessages = false;

    public static string PlatformUninitializedError = "This function requires an initialized Oculus Platform. Run Oculus.Platform.Core.[Initialize|AsyncInitialize] and try again.";

    internal static void ForceInitialized()
    {
      IsPlatformInitialized = true;
    }

    private static string getAppID(string appId = null) {
      string configAppID = GetAppIDFromConfig();
      if (String.IsNullOrEmpty(appId))
      {
        if (String.IsNullOrEmpty(configAppID))
        {
          throw new UnityException("Update your app id by selecting 'Oculus Platform' -> 'Edit Settings'");
        }
        appId = configAppID;
      }
      else
      {
        if (!String.IsNullOrEmpty(configAppID))
        {
          Debug.LogWarningFormat("The 'Oculus App Id ({0})' field in 'Oculus Platform/Edit Settings' is being overridden by the App Id ({1}) that you passed in to Platform.Core.Initialize.  You should only specify this in one place.  We recommend the menu location.", configAppID, appId);
        }
      }
      return appId;
    }

    // Asynchronously Initialize Platform SDK. The result will be put on the message
    // queue with the message type: ovrMessage_PlatformInitializeAndroidAsynchronous
    //
    // While the platform is in an initializing state, it's not fully functional.
    // [Requests]: will queue up and run once platform is initialized.
    //    For example: ovr_User_GetLoggedInUser() can be called immediately after
    //    asynchronous init and once platform is initialized, this request will run
    // [Synchronous Methods]: will return the default value;
    //    For example: ovr_GetLoggedInUserID() will return 0 until platform is
    //    fully initialized
    public static Request<Models.PlatformInitialize> AsyncInitialize(string appId = null) {
      appId = getAppID(appId);

      Request<Models.PlatformInitialize> request;
      if (UnityEngine.Application.isEditor && PlatformSettings.UseStandalonePlatform) {
        var platform = new StandalonePlatform();
        request = platform.InitializeInEditor();
      }
      else if (UnityEngine.Application.platform == RuntimePlatform.WindowsEditor ||
               UnityEngine.Application.platform == RuntimePlatform.WindowsPlayer) {
        var platform = new WindowsPlatform();
        request = platform.AsyncInitialize(appId);
      }
      else if (UnityEngine.Application.platform == RuntimePlatform.Android) {
        var platform = new AndroidPlatform();
        request = platform.AsyncInitialize(appId);
      }
      else {
        throw new NotImplementedException("Oculus platform is not implemented on this platform yet.");
      }

      IsPlatformInitialized = (request != null);

      if (!IsPlatformInitialized)
      {
        throw new UnityException("Oculus Platform failed to initialize.");
      }

      if (LogMessages) {
        Debug.LogWarning("Oculus.Platform.Core.LogMessages is set to true. This will cause extra heap allocations, and should not be used outside of testing and debugging.");
      }

      // Create the GameObject that will run the callbacks
      (new GameObject("Oculus.Platform.CallbackRunner")).AddComponent<CallbackRunner>();
      return request;
    }


    public static void Initialize(string appId = null)
    {
      appId = getAppID(appId);

      if (UnityEngine.Application.isEditor && PlatformSettings.UseStandalonePlatform) {
        var platform = new StandalonePlatform();
        IsPlatformInitialized = platform.InitializeInEditor() != null;
      }
      else if (UnityEngine.Application.platform == RuntimePlatform.WindowsEditor ||
               UnityEngine.Application.platform == RuntimePlatform.WindowsPlayer) {
        var platform = new WindowsPlatform();
        IsPlatformInitialized = platform.Initialize(appId);
      }
      else if (UnityEngine.Application.platform == RuntimePlatform.Android) {
        var platform = new AndroidPlatform();
        IsPlatformInitialized = platform.Initialize(appId);
      }
      else {
        throw new NotImplementedException("Oculus platform is not implemented on this platform yet.");
      }

      if (!IsPlatformInitialized)
      {
        throw new UnityException("Oculus Platform failed to initialize.");
      }

      if (LogMessages) {
        Debug.LogWarning("Oculus.Platform.Core.LogMessages is set to true. This will cause extra heap allocations, and should not be used outside of testing and debugging.");
      }

      // Create the GameObject that will run the callbacks
      (new GameObject("Oculus.Platform.CallbackRunner")).AddComponent<CallbackRunner>();
    }

    private static string GetAppIDFromConfig()
    {
      if (UnityEngine.Application.platform == RuntimePlatform.Android)
      {
        return PlatformSettings.MobileAppID;
      }
      else
      {
        return PlatformSettings.AppID;
      }
    }
  }

  public static partial class ApplicationLifecycle
  {
    public static Models.LaunchDetails GetLaunchDetails() {
      return new Models.LaunchDetails(CAPI.ovr_ApplicationLifecycle_GetLaunchDetails());
    }
    public static void LogDeeplinkResult(string trackingID, LaunchResult result) {
      CAPI.ovr_ApplicationLifecycle_LogDeeplinkResult(trackingID, result);
    }
  }

  public static partial class Rooms
  {

    public static Request<Models.Room> UpdateDataStore(UInt64 roomID, Dictionary<string, string> data)
    {
      if (Core.IsInitialized())
      {
        CAPI.ovrKeyValuePair[] kvps = new CAPI.ovrKeyValuePair[data.Count];
        int i=0;
        foreach(var item in data)
        {
          kvps[i++] = new CAPI.ovrKeyValuePair(item.Key, item.Value);
        }

        return new Request<Models.Room>(CAPI.ovr_Room_UpdateDataStore(roomID, kvps));
      }
      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    [Obsolete("Deprecated in favor of SetRoomInviteAcceptedNotificationCallback")]
    public static void SetRoomInviteNotificationCallback(Message<string>.Callback callback)
    {
      SetRoomInviteAcceptedNotificationCallback(callback);
    }

  }

  public static partial class Matchmaking
  {
    public class CustomQuery
    {
      public Dictionary<string, object> data;
      public Criterion[] criteria;

      public struct Criterion
      {
        public Criterion(string key_, MatchmakingCriterionImportance importance_)
        {
          key = key_;
          importance = importance_;

          parameters = null;
        }

        public string key;
        public MatchmakingCriterionImportance importance;
        public Dictionary<string, object> parameters;
      }

      public IntPtr ToUnmanaged()
      {
        var customQueryUnmanaged = new CAPI.ovrMatchmakingCustomQueryData();

        if(criteria != null && criteria.Length > 0)
        {
          customQueryUnmanaged.criterionArrayCount = (uint)criteria.Length;
          var temp = new CAPI.ovrMatchmakingCriterion[criteria.Length];

          for(int i=0; i<criteria.Length; i++)
          {
            temp[i].importance_ = criteria[i].importance;
            temp[i].key_ = criteria[i].key;

            if(criteria[i].parameters != null && criteria[i].parameters.Count > 0)
            {
              temp[i].parameterArrayCount = (uint)criteria[i].parameters.Count;
              temp[i].parameterArray = CAPI.ArrayOfStructsToIntPtr(CAPI.DictionaryToOVRKeyValuePairs(criteria[i].parameters));
            }
            else
            {
              temp[i].parameterArrayCount = 0;
              temp[i].parameterArray = IntPtr.Zero;
            }
          }

          customQueryUnmanaged.criterionArray = CAPI.ArrayOfStructsToIntPtr(temp);
        }
        else
        {
          customQueryUnmanaged.criterionArrayCount = 0;
          customQueryUnmanaged.criterionArray = IntPtr.Zero;
        }


        if(data != null && data.Count > 0)
        {
          customQueryUnmanaged.dataArrayCount = (uint)data.Count;
          customQueryUnmanaged.dataArray = CAPI.ArrayOfStructsToIntPtr(CAPI.DictionaryToOVRKeyValuePairs(data));
        }
        else
        {
          customQueryUnmanaged.dataArrayCount = 0;
          customQueryUnmanaged.dataArray = IntPtr.Zero;
        }

        IntPtr res = Marshal.AllocHGlobal(Marshal.SizeOf(customQueryUnmanaged));
        Marshal.StructureToPtr(customQueryUnmanaged, res, true);
        return res;
      }
    }

    public static Request ReportResultsInsecure(UInt64 roomID, Dictionary<string, int> data)
    {
      if(Core.IsInitialized())
      {
        CAPI.ovrKeyValuePair[] kvps = new CAPI.ovrKeyValuePair[data.Count];
        int i=0;
        foreach(var item in data)
        {
          kvps[i++] = new CAPI.ovrKeyValuePair(item.Key, item.Value);
        }

        return new Request(CAPI.ovr_Matchmaking_ReportResultInsecure(roomID, kvps));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    public static Request<Models.MatchmakingStats> GetStats(string pool, uint maxLevel, MatchmakingStatApproach approach = MatchmakingStatApproach.Trailing)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.MatchmakingStats>(CAPI.ovr_Matchmaking_GetStats(pool, maxLevel, approach));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }
  }

  public static partial class Net
  {
    public static Packet ReadPacket()
    {
      if (!Core.IsInitialized())
      {
        Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
        return null;
      }

      var packetHandle = CAPI.ovr_Net_ReadPacket();

      if(packetHandle == IntPtr.Zero)
      {
        return null;
      }

      return new Packet(packetHandle);
    }

    public static bool SendPacket(UInt64 userID, byte[] bytes, SendPolicy policy)
    {
      if(Core.IsInitialized())
      {
        return CAPI.ovr_Net_SendPacket(userID, (UIntPtr)bytes.Length, bytes, policy);
      }

      return false;
    }

    public static void Connect(UInt64 userID)
    {
      if (Core.IsInitialized())
      {
        CAPI.ovr_Net_Connect(userID);
      }
    }

    public static void Accept(UInt64 userID)
    {
      if(Core.IsInitialized())
      {
        CAPI.ovr_Net_Accept(userID);
      }
    }

    public static void Close(UInt64 userID)
    {
      if(Core.IsInitialized())
      {
        CAPI.ovr_Net_Close(userID);
      }
    }

    public static bool IsConnected(UInt64 userID)
    {
      return Core.IsInitialized() && CAPI.ovr_Net_IsConnected(userID);
    }

    public static bool SendPacketToCurrentRoom(byte[] bytes, SendPolicy policy)
    {
      if (Core.IsInitialized())
      {
        return CAPI.ovr_Net_SendPacketToCurrentRoom((UIntPtr)bytes.Length, bytes, policy);
      }

      return false;
    }

    public static bool AcceptForCurrentRoom()
    {
      if (Core.IsInitialized())
      {
        return CAPI.ovr_Net_AcceptForCurrentRoom();
      }

      return false;
    }

    public static void CloseForCurrentRoom()
    {
      if (Core.IsInitialized())
      {
        CAPI.ovr_Net_CloseForCurrentRoom();
      }
    }

    public static Request<Models.PingResult> Ping(UInt64 userID)
    {
      if(Core.IsInitialized())
      {
        return new Request<Models.PingResult>(CAPI.ovr_Net_Ping(userID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }
  }

  public static partial class Leaderboards
  {
    public static Request<Models.LeaderboardEntryList> GetNextEntries(Models.LeaderboardEntryList list)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.LeaderboardEntryList>(CAPI.ovr_HTTP_GetWithMessageType(list.NextUrl, (int)Message.MessageType.Leaderboard_GetNextEntries));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    public static Request<Models.LeaderboardEntryList> GetPreviousEntries(Models.LeaderboardEntryList list)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.LeaderboardEntryList>(CAPI.ovr_HTTP_GetWithMessageType(list.PreviousUrl, (int)Message.MessageType.Leaderboard_GetPreviousEntries));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }
  }

  public static partial class Challenges
  {
    public static Request<Models.ChallengeEntryList> GetNextEntries(Models.ChallengeEntryList list)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ChallengeEntryList>(CAPI.ovr_HTTP_GetWithMessageType(list.NextUrl, (int)Message.MessageType.Challenges_GetNextEntries));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    public static Request<Models.ChallengeEntryList> GetPreviousEntries(Models.ChallengeEntryList list)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ChallengeEntryList>(CAPI.ovr_HTTP_GetWithMessageType(list.PreviousUrl, (int)Message.MessageType.Challenges_GetPreviousEntries));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    public static Request<Models.ChallengeList> GetNextChallenges(Models.ChallengeList list)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ChallengeList>(CAPI.ovr_HTTP_GetWithMessageType(list.NextUrl, (int)Message.MessageType.Challenges_GetNextChallenges));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    public static Request<Models.ChallengeList> GetPreviousChallenges(Models.ChallengeList list)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ChallengeList>(CAPI.ovr_HTTP_GetWithMessageType(list.PreviousUrl, (int)Message.MessageType.Challenges_GetPreviousChallenges));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }
  }

  public static partial class Voip
  {
    public static void Start(UInt64 userID)
    {
      if (Core.IsInitialized())
      {
        CAPI.ovr_Voip_Start(userID);
      }
    }

    public static void Accept(UInt64 userID)
    {
      if (Core.IsInitialized())
      {
        CAPI.ovr_Voip_Accept(userID);
      }
    }

    public static void Stop(UInt64 userID)
    {
      if (Core.IsInitialized())
      {
        CAPI.ovr_Voip_Stop(userID);
      }
    }

    public static void SetMicrophoneFilterCallback(CAPI.FilterCallback callback)
    {
      if (Core.IsInitialized())
      {
        CAPI.ovr_Voip_SetMicrophoneFilterCallbackWithFixedSizeBuffer(callback, (UIntPtr)CAPI.VoipFilterBufferSize);
      }
    }

    public static void SetMicrophoneMuted(VoipMuteState state)
    {
      if (Core.IsInitialized())
      {
        CAPI.ovr_Voip_SetMicrophoneMuted(state);
      }
    }

    public static VoipMuteState GetSystemVoipMicrophoneMuted()
    {
      if (Core.IsInitialized())
      {
        return CAPI.ovr_Voip_GetSystemVoipMicrophoneMuted();
      }
      return VoipMuteState.Unknown;
    }

    public static SystemVoipStatus GetSystemVoipStatus()
    {
      if (Core.IsInitialized())
      {
        return CAPI.ovr_Voip_GetSystemVoipStatus();
      }
      return SystemVoipStatus.Unknown;
    }

    public static Oculus.Platform.VoipDtxState GetIsConnectionUsingDtx(UInt64 peerID)
    {
      if (Core.IsInitialized())
      {
        return CAPI.ovr_Voip_GetIsConnectionUsingDtx(peerID);
      }
      return Oculus.Platform.VoipDtxState.Unknown;
    }

    public static Oculus.Platform.VoipBitrate GetLocalBitrate(UInt64 peerID)
    {
      if (Core.IsInitialized())
      {
        return CAPI.ovr_Voip_GetLocalBitrate(peerID);
      }
      return Oculus.Platform.VoipBitrate.Unknown;
    }

    public static Oculus.Platform.VoipBitrate GetRemoteBitrate(UInt64 peerID)
    {
      if (Core.IsInitialized())
      {
        return CAPI.ovr_Voip_GetRemoteBitrate(peerID);
      }
      return Oculus.Platform.VoipBitrate.Unknown;
    }

    public static void SetNewConnectionOptions(VoipOptions voipOptions)
    {
      if (Core.IsInitialized())
      {
        CAPI.ovr_Voip_SetNewConnectionOptions((IntPtr)voipOptions);
      }
    }
  }

  public static partial class Users
  {
    public static string GetLoggedInUserLocale()
    {
      if (Core.IsInitialized())
      {
        return CAPI.ovr_GetLoggedInUserLocale();
      }
      return "";
    }
  }

  public static partial class Achievements
  {
    /// Add 'count' to the achievement with the given name. This must be a COUNT
    /// achievement. The largest number that is supported by this method is the max
    /// value of a signed 64-bit integer. If the number is larger than that, it is
    /// clamped to that max value before being passed to the servers.
    ///
    public static Request<Models.AchievementUpdate> AddCount(string name, ulong count)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AchievementUpdate>(CAPI.ovr_Achievements_AddCount(name, count));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Unlock fields of a BITFIELD achievement.
    /// \param name The name of the achievement to unlock
    /// \param fields A string containing either '0' or '1' characters. Every '1' will unlock the field in the corresponding position.
    ///
    public static Request<Models.AchievementUpdate> AddFields(string name, string fields)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AchievementUpdate>(CAPI.ovr_Achievements_AddFields(name, fields));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Request all achievement definitions for the app.
    ///
    public static Request<Models.AchievementDefinitionList> GetAllDefinitions()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AchievementDefinitionList>(CAPI.ovr_Achievements_GetAllDefinitions());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Request the progress for the user on all achievements in the app.
    ///
    public static Request<Models.AchievementProgressList> GetAllProgress()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AchievementProgressList>(CAPI.ovr_Achievements_GetAllProgress());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Request the achievement definitions that match the specified names.
    ///
    public static Request<Models.AchievementDefinitionList> GetDefinitionsByName(string[] names)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AchievementDefinitionList>(CAPI.ovr_Achievements_GetDefinitionsByName(names, (names != null ? names.Length : 0)));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Request the user's progress on the specified achievements.
    ///
    public static Request<Models.AchievementProgressList> GetProgressByName(string[] names)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AchievementProgressList>(CAPI.ovr_Achievements_GetProgressByName(names, (names != null ? names.Length : 0)));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Unlock the achievement with the given name. This can be of any achievement
    /// type.
    ///
    public static Request<Models.AchievementUpdate> Unlock(string name)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AchievementUpdate>(CAPI.ovr_Achievements_Unlock(name));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class Application
  {
    /// Requests version information, including the currently installed and latest
    /// available version name and version code.
    ///
    public static Request<Models.ApplicationVersion> GetVersion()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ApplicationVersion>(CAPI.ovr_Application_GetVersion());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Launches a different application in the user's library. If the user does
    /// not have that application installed, they will be taken to that app's page
    /// in the Oculus Store
    /// \param appID The ID of the app to launch
    /// \param deeplink_options Additional configuration for this requests. Optional.
    ///
    public static Request<string> LaunchOtherApp(UInt64 appID, ApplicationOptions deeplink_options = null)
    {
      if (Core.IsInitialized())
      {
        return new Request<string>(CAPI.ovr_Application_LaunchOtherApp(appID, (IntPtr)deeplink_options));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class ApplicationLifecycle
  {
    /// Sent when a launch intent is received (for both cold and warm starts). The
    /// payload is the type of the intent. ApplicationLifecycle.GetLaunchDetails()
    /// should be called to get the other details.
    ///
    public static void SetLaunchIntentChangedNotificationCallback(Message<string>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_ApplicationLifecycle_LaunchIntentChanged,
        callback
      );
    }

  }

  public static partial class AssetFile
  {
    /// DEPRECATED. Use AssetFile.DeleteById()
    ///
    public static Request<Models.AssetFileDeleteResult> Delete(UInt64 assetFileID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetFileDeleteResult>(CAPI.ovr_AssetFile_Delete(assetFileID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Removes an previously installed asset file from the device by its ID.
    /// Returns an object containing the asset ID and file name, and a success
    /// flag.
    /// \param assetFileID The asset file ID
    ///
    public static Request<Models.AssetFileDeleteResult> DeleteById(UInt64 assetFileID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetFileDeleteResult>(CAPI.ovr_AssetFile_DeleteById(assetFileID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Removes an previously installed asset file from the device by its name.
    /// Returns an object containing the asset ID and file name, and a success
    /// flag.
    /// \param assetFileName The asset file name
    ///
    public static Request<Models.AssetFileDeleteResult> DeleteByName(string assetFileName)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetFileDeleteResult>(CAPI.ovr_AssetFile_DeleteByName(assetFileName));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// DEPRECATED. Use AssetFile.DownloadById()
    ///
    public static Request<Models.AssetFileDownloadResult> Download(UInt64 assetFileID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetFileDownloadResult>(CAPI.ovr_AssetFile_Download(assetFileID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Downloads an asset file by its ID on demand. Returns an object containing
    /// the asset ID and filepath. Sends periodic
    /// MessageType.Notification_AssetFile_DownloadUpdate to track the downloads.
    /// \param assetFileID The asset file ID
    ///
    public static Request<Models.AssetFileDownloadResult> DownloadById(UInt64 assetFileID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetFileDownloadResult>(CAPI.ovr_AssetFile_DownloadById(assetFileID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Downloads an asset file by its name on demand. Returns an object containing
    /// the asset ID and filepath. Sends periodic
    /// {notifications.asset_file.download_update}} to track the downloads.
    /// \param assetFileName The asset file name
    ///
    public static Request<Models.AssetFileDownloadResult> DownloadByName(string assetFileName)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetFileDownloadResult>(CAPI.ovr_AssetFile_DownloadByName(assetFileName));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// DEPRECATED. Use AssetFile.DownloadCancelById()
    ///
    public static Request<Models.AssetFileDownloadCancelResult> DownloadCancel(UInt64 assetFileID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetFileDownloadCancelResult>(CAPI.ovr_AssetFile_DownloadCancel(assetFileID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Cancels a previously spawned download request for an asset file by its ID.
    /// Returns an object containing the asset ID and file path, and a success
    /// flag.
    /// \param assetFileID The asset file ID
    ///
    public static Request<Models.AssetFileDownloadCancelResult> DownloadCancelById(UInt64 assetFileID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetFileDownloadCancelResult>(CAPI.ovr_AssetFile_DownloadCancelById(assetFileID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Cancels a previously spawned download request for an asset file by its
    /// name. Returns an object containing the asset ID and file path, and a
    /// success flag.
    /// \param assetFileName The asset file name
    ///
    public static Request<Models.AssetFileDownloadCancelResult> DownloadCancelByName(string assetFileName)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetFileDownloadCancelResult>(CAPI.ovr_AssetFile_DownloadCancelByName(assetFileName));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Returns an array of objects with asset file names and their associated IDs,
    /// and and whether it's currently installed.
    ///
    public static Request<Models.AssetDetailsList> GetList()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetDetailsList>(CAPI.ovr_AssetFile_GetList());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// DEPRECATED. Use AssetFile.StatusById()
    ///
    public static Request<Models.AssetDetails> Status(UInt64 assetFileID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetDetails>(CAPI.ovr_AssetFile_Status(assetFileID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Returns the details on a single asset: ID, file name, and whether it's
    /// currently installed
    /// \param assetFileID The asset file ID
    ///
    public static Request<Models.AssetDetails> StatusById(UInt64 assetFileID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetDetails>(CAPI.ovr_AssetFile_StatusById(assetFileID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Returns the details on a single asset: ID, file name, and whether it's
    /// currently installed
    /// \param assetFileName The asset file name
    ///
    public static Request<Models.AssetDetails> StatusByName(string assetFileName)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetDetails>(CAPI.ovr_AssetFile_StatusByName(assetFileName));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Sent to indicate download progress for asset files.
    ///
    public static void SetDownloadUpdateNotificationCallback(Message<Models.AssetFileDownloadUpdate>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_AssetFile_DownloadUpdate,
        callback
      );
    }

  }

  public static partial class Cal
  {
  }

  public static partial class Challenges
  {
    /// DEPRECATED. Use server-to-server API call instead.
    ///
    public static Request<Models.Challenge> Create(string leaderboardName, ChallengeOptions challengeOptions)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Challenge>(CAPI.ovr_Challenges_Create(leaderboardName, (IntPtr)challengeOptions));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// If the current user has an invite to the challenge, decline the invite
    ///
    public static Request<Models.Challenge> DeclineInvite(UInt64 challengeID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Challenge>(CAPI.ovr_Challenges_DeclineInvite(challengeID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// DEPRECATED. Use server-to-server API call instead.
    ///
    public static Request Delete(UInt64 challengeID)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_Challenges_Delete(challengeID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Gets the information for a single challenge
    /// \param challengeID The id of the challenge whose entries to return.
    ///
    public static Request<Models.Challenge> Get(UInt64 challengeID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Challenge>(CAPI.ovr_Challenges_Get(challengeID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Requests a block of challenge entries.
    /// \param challengeID The id of the challenge whose entries to return.
    /// \param limit Defines the maximum number of entries to return.
    /// \param filter Allows you to restrict the returned values by friends.
    /// \param startAt Defines whether to center the query on the user or start at the top of the challenge.
    ///
    public static Request<Models.ChallengeEntryList> GetEntries(UInt64 challengeID, int limit, LeaderboardFilterType filter, LeaderboardStartAt startAt)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ChallengeEntryList>(CAPI.ovr_Challenges_GetEntries(challengeID, limit, filter, startAt));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Requests a block of challenge entries.
    /// \param challengeID The id of the challenge whose entries to return.
    /// \param limit The maximum number of entries to return.
    /// \param afterRank The position after which to start.  For example, 10 returns challenge results starting with the 11th user.
    ///
    public static Request<Models.ChallengeEntryList> GetEntriesAfterRank(UInt64 challengeID, int limit, ulong afterRank)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ChallengeEntryList>(CAPI.ovr_Challenges_GetEntriesAfterRank(challengeID, limit, afterRank));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Requests a block of challenge entries. Will return only entries matching
    /// the user IDs passed in.
    /// \param challengeID The id of the challenge whose entries to return.
    /// \param limit Defines the maximum number of entries to return.
    /// \param startAt Defines whether to center the query on the user or start at the top of the challenge. If this is LeaderboardStartAt.CenteredOnViewer or LeaderboardStartAt.CenteredOnViewerOrTop, then the current user's ID will be automatically added to the query.
    /// \param userIDs Defines a list of user ids to get entries for.
    ///
    public static Request<Models.ChallengeEntryList> GetEntriesByIds(UInt64 challengeID, int limit, LeaderboardStartAt startAt, UInt64[] userIDs)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ChallengeEntryList>(CAPI.ovr_Challenges_GetEntriesByIds(challengeID, limit, startAt, userIDs, (uint)(userIDs != null ? userIDs.Length : 0)));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Requests for a list of challenge
    ///
    public static Request<Models.ChallengeList> GetList(ChallengeOptions challengeOptions, int limit)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ChallengeList>(CAPI.ovr_Challenges_GetList((IntPtr)challengeOptions, limit));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// If the current user has permission, join the challenge
    ///
    public static Request<Models.Challenge> Join(UInt64 challengeID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Challenge>(CAPI.ovr_Challenges_Join(challengeID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// If the current user has permission, leave the challenge
    ///
    public static Request<Models.Challenge> Leave(UInt64 challengeID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Challenge>(CAPI.ovr_Challenges_Leave(challengeID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// DEPRECATED. Use server-to-server API call instead.
    ///
    public static Request<Models.Challenge> UpdateInfo(UInt64 challengeID, ChallengeOptions challengeOptions)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Challenge>(CAPI.ovr_Challenges_UpdateInfo(challengeID, (IntPtr)challengeOptions));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class CloudStorage
  {
    /// Deletes the specified save data buffer. Conflicts are handled just like
    /// Saves.
    /// \param bucket The name of the storage bucket.
    /// \param key The name for this saved data.
    ///
    public static Request<Models.CloudStorageUpdateResponse> Delete(string bucket, string key)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.CloudStorageUpdateResponse>(CAPI.ovr_CloudStorage_Delete(bucket, key));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Loads the saved entry for the specified bucket and key. If a conflict
    /// exists with the key then an error message is returned.
    /// \param bucket The name of the storage bucket.
    /// \param key The name for this saved data.
    ///
    public static Request<Models.CloudStorageData> Load(string bucket, string key)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.CloudStorageData>(CAPI.ovr_CloudStorage_Load(bucket, key));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Loads all the metadata for the saves in the specified bucket, including
    /// conflicts.
    /// \param bucket The name of the storage bucket.
    ///
    public static Request<Models.CloudStorageMetadataList> LoadBucketMetadata(string bucket)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.CloudStorageMetadataList>(CAPI.ovr_CloudStorage_LoadBucketMetadata(bucket));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Loads the metadata for this bucket-key combination that need to be manually
    /// resolved.
    /// \param bucket The name of the storage bucket
    /// \param key The key for this saved data.
    ///
    public static Request<Models.CloudStorageConflictMetadata> LoadConflictMetadata(string bucket, string key)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.CloudStorageConflictMetadata>(CAPI.ovr_CloudStorage_LoadConflictMetadata(bucket, key));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Loads the data specified by the storage handle.
    ///
    public static Request<Models.CloudStorageData> LoadHandle(string handle)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.CloudStorageData>(CAPI.ovr_CloudStorage_LoadHandle(handle));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// load the metadata for the specified key
    /// \param bucket The name of the storage bucket.
    /// \param key The name for this saved data.
    ///
    public static Request<Models.CloudStorageMetadata> LoadMetadata(string bucket, string key)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.CloudStorageMetadata>(CAPI.ovr_CloudStorage_LoadMetadata(bucket, key));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Selects the local save for manual conflict resolution.
    /// \param bucket The name of the storage bucket.
    /// \param key The name for this saved data.
    /// \param remoteHandle The handle of the remote that the local file was resolved against.
    ///
    public static Request<Models.CloudStorageUpdateResponse> ResolveKeepLocal(string bucket, string key, string remoteHandle)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.CloudStorageUpdateResponse>(CAPI.ovr_CloudStorage_ResolveKeepLocal(bucket, key, remoteHandle));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Selects the remote save for manual conflict resolution.
    /// \param bucket The name of the storage bucket.
    /// \param key The name for this saved data.
    /// \param remoteHandle The handle of the remote.
    ///
    public static Request<Models.CloudStorageUpdateResponse> ResolveKeepRemote(string bucket, string key, string remoteHandle)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.CloudStorageUpdateResponse>(CAPI.ovr_CloudStorage_ResolveKeepRemote(bucket, key, remoteHandle));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Note: Cloud Storage is only available for Rift apps.
    ///
    /// Send a save data buffer to the platform. CloudStorage.Save() passes a
    /// pointer to your data in an async call. You need to maintain the save data
    /// until you receive the message indicating that the save was successful.
    ///
    /// If the data is destroyed or modified prior to receiving that message the
    /// data will not be saved.
    /// \param bucket The name of the storage bucket.
    /// \param key The name for this saved data.
    /// \param data Start of the data block.
    /// \param counter Optional. Counter used for user data or auto-deconfliction.
    /// \param extraData Optional. String data that isn't used by the platform.
    ///
    /// <b>Error codes</b>
    /// - \b 100: The stored version has a later timestamp than the data provided. This cloud storage bucket's conflict resolution policy is configured to use the latest timestamp, which is configurable in the developer dashboard.
    ///
    public static Request<Models.CloudStorageUpdateResponse> Save(string bucket, string key, byte[] data, long counter, string extraData)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.CloudStorageUpdateResponse>(CAPI.ovr_CloudStorage_Save(bucket, key, data, (uint)(data != null ? data.Length : 0), counter, extraData));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class CloudStorage2
  {
    /// Get the directory path for the current user/app pair that will be used
    /// during cloud storage synchronization
    ///
    public static Request<string> GetUserDirectoryPath()
    {
      if (Core.IsInitialized())
      {
        return new Request<string>(CAPI.ovr_CloudStorage2_GetUserDirectoryPath());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class Entitlements
  {
    /// Returns whether the current user is entitled to the current app.
    ///
    public static Request IsUserEntitledToApplication()
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_Entitlement_GetIsViewerEntitled());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class GroupPresence
  {
    /// Clear group presence for running app
    ///
    public static Request Clear()
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_GroupPresence_Clear());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Returns a list of users that can be invited to your current lobby. These
    /// are pulled from your friends and recently met lists.
    ///
    public static Request<Models.UserList> GetInvitableUsers(InviteOptions options)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.UserList>(CAPI.ovr_GroupPresence_GetInvitableUsers((IntPtr)options));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Returns a list of users that can be invited to your current lobby. These
    /// are pulled from your friends and recently met lists.
    ///
    public static Request<Models.ApplicationInviteList> GetSentInvites()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ApplicationInviteList>(CAPI.ovr_GroupPresence_GetSentInvites());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Launch the flow to allow the user to invite others to their current
    /// session. This can only be used if the user is in a joinable session.
    ///
    public static Request<Models.InvitePanelResultInfo> LaunchInvitePanel(InviteOptions options)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.InvitePanelResultInfo>(CAPI.ovr_GroupPresence_LaunchInvitePanel((IntPtr)options));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Launch an error dialog with predefined messages for common multiplayer
    /// errors.
    ///
    public static Request LaunchMultiplayerErrorDialog(MultiplayerErrorOptions options)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_GroupPresence_LaunchMultiplayerErrorDialog((IntPtr)options));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Launch the dialog which will allow the user to rejoin a previous
    /// lobby/match. Either the lobby_session_id or the match_session_id, or both,
    /// must be populated.
    ///
    public static Request<Models.RejoinDialogResult> LaunchRejoinDialog(string lobby_session_id, string match_session_id, string destination_api_name)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.RejoinDialogResult>(CAPI.ovr_GroupPresence_LaunchRejoinDialog(lobby_session_id, match_session_id, destination_api_name));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Launch the panel which displays the current users in the roster. Users with
    /// the same lobby and match session id as part of their presence will show up
    /// here.
    ///
    public static Request LaunchRosterPanel(RosterOptions options)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_GroupPresence_LaunchRosterPanel((IntPtr)options));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Returns a list of users that can be invited to your current lobby. These
    /// are pulled from your friends and recently met lists.
    ///
    public static Request<Models.SendInvitesResult> SendInvites(UInt64[] userIDs)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.SendInvitesResult>(CAPI.ovr_GroupPresence_SendInvites(userIDs, (uint)(userIDs != null ? userIDs.Length : 0)));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Set group presence for running app
    ///
    public static Request Set(GroupPresenceOptions groupPresenceOptions)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_GroupPresence_Set((IntPtr)groupPresenceOptions));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Replaces the user's current destination for the provided one. All other
    /// existing group presence parameters will remain the same.
    ///
    public static Request SetDestination(string api_name)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_GroupPresence_SetDestination(api_name));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Set if the current user's destination and session is joinable while keeping
    /// the other group presence parameters the same. If the destination or session
    /// ids of the user is not set, they cannot be set to joinable.
    ///
    public static Request SetIsJoinable(bool is_joinable)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_GroupPresence_SetIsJoinable(is_joinable));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Replaces the user's current lobby session id for the provided one. All
    /// other existing group presence parameters will remain the same.
    ///
    public static Request SetLobbySession(string id)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_GroupPresence_SetLobbySession(id));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Replaces the user's current match session id for the provided one. All
    /// other existing group presence parameters will remain the same.
    ///
    public static Request SetMatchSession(string id)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_GroupPresence_SetMatchSession(id));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Sent when the user is finished using the invite panel to send out
    /// invitations. Contains a list of invitees.
    ///
    public static void SetInvitationsSentNotificationCallback(Message<Models.LaunchInvitePanelFlowResult>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_GroupPresence_InvitationsSent,
        callback
      );
    }

    /// Sent when a user has chosen to join the destination/lobby/match. Read all
    /// the fields to figure out where the user wants to go and take the
    /// appropriate actions to bring them there. If the user is unable to go there,
    /// provide adequate messaging to the user on why they cannot go there. These
    /// notifications should be responded to immediately.
    ///
    public static void SetJoinIntentReceivedNotificationCallback(Message<Models.GroupPresenceJoinIntent>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_GroupPresence_JoinIntentReceived,
        callback
      );
    }

    /// Sent when the user has chosen to leave the destination/lobby/match from the
    /// Oculus menu. Read the specific fields to check the user is currently from
    /// the destination/lobby/match and take the appropriate actions to remove
    /// them. Update the user's presence clearing the appropriate fields to
    /// indicate the user has left.
    ///
    public static void SetLeaveIntentReceivedNotificationCallback(Message<Models.GroupPresenceLeaveIntent>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_GroupPresence_LeaveIntentReceived,
        callback
      );
    }

  }

  public static partial class HTTP
  {
  }

  public static partial class IAP
  {
    /// Allow the consumable IAP product to be purchased again. Conceptually, this
    /// indicates that the item was used or consumed.
    ///
    public static Request ConsumePurchase(string sku)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_IAP_ConsumePurchase(sku));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Retrieve a list of IAP products that can be purchased.
    /// \param skus The SKUs of the products to retrieve.
    ///
    public static Request<Models.ProductList> GetProductsBySKU(string[] skus)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ProductList>(CAPI.ovr_IAP_GetProductsBySKU(skus, (skus != null ? skus.Length : 0)));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Retrieve a list of Purchase that the Logged-In-User has made. This list
    /// will also contain consumable purchases that have not been consumed.
    ///
    public static Request<Models.PurchaseList> GetViewerPurchases()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.PurchaseList>(CAPI.ovr_IAP_GetViewerPurchases());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Retrieve a list of Purchase that the Logged-In-User has made. This list
    /// will only contain durable purchase (non-consumable) and is populated from a
    /// device cache. It is recommended in all cases to use
    /// ovr_User_GetViewerPurchases first and only check the cache if that fails.
    ///
    public static Request<Models.PurchaseList> GetViewerPurchasesDurableCache()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.PurchaseList>(CAPI.ovr_IAP_GetViewerPurchasesDurableCache());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Launch the checkout flow to purchase the existing product. Oculus Home
    /// tries handle and fix as many errors as possible. Home returns the
    /// appropriate error message and how to resolveit, if possible. Returns a
    /// purchase on success, empty purchase on cancel, and an error on error.
    /// \param sku IAP sku for the item the user wishes to purchase.
    ///
    public static Request<Models.Purchase> LaunchCheckoutFlow(string sku)
    {
      if (Core.IsInitialized())
      {
        if (UnityEngine.Application.isEditor) {
          throw new NotImplementedException("LaunchCheckoutFlow() is not implemented in the editor yet.");
        }

        return new Request<Models.Purchase>(CAPI.ovr_IAP_LaunchCheckoutFlow(sku));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class LanguagePack
  {
    /// Returns currently installed and selected language pack for an app in the
    /// view of the `asset_details`. Use `language` field to extract neeeded
    /// language info. A particular language can be download and installed by a
    /// user from the Oculus app on the application page.
    ///
    public static Request<Models.AssetDetails> GetCurrent()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetDetails>(CAPI.ovr_LanguagePack_GetCurrent());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Sets the current language to specified. The parameter is the BCP47 language
    /// tag. If a language pack is not downloaded yet, spawns automatically the
    /// AssetFile.DownloadByName() request, and sends periodic
    /// MessageType.Notification_AssetFile_DownloadUpdate to track the downloads.
    /// Once the language asset file is downloaded, call LanguagePack.GetCurrent()
    /// to retrive the data, and use the language at runtime.
    /// \param tag BCP47 language tag
    ///
    public static Request<Models.AssetFileDownloadResult> SetCurrent(string tag)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.AssetFileDownloadResult>(CAPI.ovr_LanguagePack_SetCurrent(tag));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class Leaderboards
  {
    /// Gets the information for a single leaderboard
    /// \param leaderboardName The name of the leaderboard to return.
    ///
    public static Request<Models.LeaderboardList> Get(string leaderboardName)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.LeaderboardList>(CAPI.ovr_Leaderboard_Get(leaderboardName));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Requests a block of leaderboard entries.
    /// \param leaderboardName The name of the leaderboard whose entries to return.
    /// \param limit Defines the maximum number of entries to return.
    /// \param filter Allows you to restrict the returned values by friends.
    /// \param startAt Defines whether to center the query on the user or start at the top of the leaderboard.
    ///
    /// <b>Error codes</b>
    /// - \b 100: Parameter {parameter}: invalid user id: {user_id}
    /// - \b 100: Something went wrong.
    /// - \b 12074: You're not yet ranked on this leaderboard.
    ///
    public static Request<Models.LeaderboardEntryList> GetEntries(string leaderboardName, int limit, LeaderboardFilterType filter, LeaderboardStartAt startAt)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.LeaderboardEntryList>(CAPI.ovr_Leaderboard_GetEntries(leaderboardName, limit, filter, startAt));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Requests a block of leaderboard entries.
    /// \param leaderboardName The name of the leaderboard.
    /// \param limit The maximum number of entries to return.
    /// \param afterRank The position after which to start.  For example, 10 returns leaderboard results starting with the 11th user.
    ///
    public static Request<Models.LeaderboardEntryList> GetEntriesAfterRank(string leaderboardName, int limit, ulong afterRank)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.LeaderboardEntryList>(CAPI.ovr_Leaderboard_GetEntriesAfterRank(leaderboardName, limit, afterRank));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Requests a block of leaderboard entries. Will return only entries matching
    /// the user IDs passed in.
    /// \param leaderboardName The name of the leaderboard whose entries to return.
    /// \param limit Defines the maximum number of entries to return.
    /// \param startAt Defines whether to center the query on the user or start at the top of the leaderboard. If this is LeaderboardStartAt.CenteredOnViewer or LeaderboardStartAt.CenteredOnViewerOrTop, then the current user's ID will be automatically added to the query.
    /// \param userIDs Defines a list of user ids to get entries for.
    ///
    public static Request<Models.LeaderboardEntryList> GetEntriesByIds(string leaderboardName, int limit, LeaderboardStartAt startAt, UInt64[] userIDs)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.LeaderboardEntryList>(CAPI.ovr_Leaderboard_GetEntriesByIds(leaderboardName, limit, startAt, userIDs, (uint)(userIDs != null ? userIDs.Length : 0)));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Writes a single entry to a leaderboard.
    /// \param leaderboardName The leaderboard for which to write the entry.
    /// \param score The score to write.
    /// \param extraData A 2KB custom data field that is associated with the leaderboard entry. This can be a game replay or anything that provides more detail about the entry to the viewer.
    /// \param forceUpdate If true, the score always updates.  This happens even if it is not the user's best score.
    ///
    /// <b>Error codes</b>
    /// - \b 100: Parameter {parameter}: invalid user id: {user_id}
    /// - \b 100: Something went wrong.
    /// - \b 100: This leaderboard entry is too late for the leaderboard's allowed time window.
    ///
    public static Request<bool> WriteEntry(string leaderboardName, long score, byte[] extraData = null, bool forceUpdate = false)
    {
      if (Core.IsInitialized())
      {
        return new Request<bool>(CAPI.ovr_Leaderboard_WriteEntry(leaderboardName, score, extraData, (uint)(extraData != null ? extraData.Length : 0), forceUpdate));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Writes a single entry to a leaderboard, can include supplementary metrics
    /// \param leaderboardName The leaderboard for which to write the entry.
    /// \param score The score to write.
    /// \param supplementaryMetric A metric that can be used for tiebreakers.
    /// \param extraData A 2KB custom data field that is associated with the leaderboard entry. This can be a game replay or anything that provides more detail about the entry to the viewer.
    /// \param forceUpdate If true, the score always updates. This happens ecen if it is not the user's best score.
    ///
    /// <b>Error codes</b>
    /// - \b 100: Parameter {parameter}: invalid user id: {user_id}
    /// - \b 100: Something went wrong.
    /// - \b 100: This leaderboard entry is too late for the leaderboard's allowed time window.
    ///
    public static Request<bool> WriteEntryWithSupplementaryMetric(string leaderboardName, long score, long supplementaryMetric, byte[] extraData = null, bool forceUpdate = false)
    {
      if (Core.IsInitialized())
      {
        return new Request<bool>(CAPI.ovr_Leaderboard_WriteEntryWithSupplementaryMetric(leaderboardName, score, supplementaryMetric, extraData, (uint)(extraData != null ? extraData.Length : 0), forceUpdate));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class Livestreaming
  {
    /// Indicates that the livestreaming session has been updated. You can use this
    /// information to throttle your game performance or increase CPU/GPU
    /// performance. Use Message.GetLivestreamingStatus() to extract the updated
    /// livestreaming status.
    ///
    public static void SetStatusUpdateNotificationCallback(Message<Models.LivestreamingStatus>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_Livestreaming_StatusChange,
        callback
      );
    }

  }

  public static partial class Matchmaking
  {
    /// DEPRECATED. Use Browse2.
    /// \param pool A BROWSE type matchmaking pool.
    /// \param customQueryData Optional. Custom query data.
    ///
    /// <b>Error codes</b>
    /// - \b 100: Pool {pool_key} does not contain custom data key {key}. You can configure matchmaking custom data at https://dashboard.oculus.com/application/&lt;app_id&gt;/matchmaking
    /// - \b 12072: Unknown pool: {pool_key}. You can configure matchmaking pools at https://dashboard.oculus.com/application/&lt;app_id&gt;/matchmaking
    ///
    public static Request<Models.MatchmakingBrowseResult> Browse(string pool, CustomQuery customQueryData = null)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.MatchmakingBrowseResult>(CAPI.ovr_Matchmaking_Browse(pool, customQueryData != null ? customQueryData.ToUnmanaged() : IntPtr.Zero));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Modes: BROWSE
    ///
    /// See overview documentation above.
    ///
    /// Return a list of matchmaking rooms in the current pool filtered by skill
    /// and ping (if enabled). This also enqueues the user in the matchmaking
    /// queue. When the user has made a selection, call Rooms.Join2() on one of the
    /// rooms that was returned. If the user stops browsing, call
    /// Matchmaking.Cancel().
    ///
    /// In addition to the list of rooms, enqueue results are also returned. Call
    /// MatchmakingBrowseResult.GetEnqueueResult() to obtain them. See
    /// OVR_MatchmakingEnqueueResult.h for details.
    /// \param pool A BROWSE type matchmaking pool.
    /// \param matchmakingOptions Additional matchmaking configuration for this request. Optional.
    ///
    /// <b>Error codes</b>
    /// - \b 100: Pool {pool_key} does not contain custom data key {key}. You can configure matchmaking custom data at https://dashboard.oculus.com/application/&lt;app_id&gt;/matchmaking
    /// - \b 12072: Unknown pool: {pool_key}. You can configure matchmaking pools at https://dashboard.oculus.com/application/&lt;app_id&gt;/matchmaking
    ///
    public static Request<Models.MatchmakingBrowseResult> Browse2(string pool, MatchmakingOptions matchmakingOptions = null)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.MatchmakingBrowseResult>(CAPI.ovr_Matchmaking_Browse2(pool, (IntPtr)matchmakingOptions));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// DEPRECATED. Use Cancel2.
    /// \param pool The pool in question.
    /// \param requestHash Used to find your entry in a queue.
    ///
    /// <b>Error codes</b>
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is currently in another room (perhaps on another device), and thus is no longer in this room. Users can only be in one room at a time. If they are active on two different devices at once, there will be undefined behavior.
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is not in the room (or any room). Perhaps they already left, or they stopped heartbeating. If this is a test environment, make sure you are not using the deprecated initialization methods ovr_PlatformInitializeStandaloneAccessToken (C++)/StandalonePlatform.Initialize(accessToken) (C#).
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is not the owner of the room.
    /// - \b 100: Invalid room_id: {room_id}. Either the ID is not a valid room or the user does not have permission to see or act on the room.
    ///
    public static Request Cancel(string pool, string requestHash)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_Matchmaking_Cancel(pool, requestHash));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Modes: QUICKMATCH, BROWSE
    ///
    /// Makes a best effort to cancel a previous Enqueue request before a match
    /// occurs. Typically triggered when a user gives up waiting. For BROWSE mode,
    /// call this when a user gives up looking through the room list or when the
    /// host of a room wants to stop receiving new users. If you don't cancel but
    /// the user goes offline, the user/room will be timed out of the queue within
    /// 30 seconds.
    ///
    /// <b>Error codes</b>
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is currently in another room (perhaps on another device), and thus is no longer in this room. Users can only be in one room at a time. If they are active on two different devices at once, there will be undefined behavior.
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is not in the room (or any room). Perhaps they already left, or they stopped heartbeating. If this is a test environment, make sure you are not using the deprecated initialization methods ovr_PlatformInitializeStandaloneAccessToken (C++)/StandalonePlatform.Initialize(accessToken) (C#).
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is not the owner of the room.
    /// - \b 100: Invalid room_id: {room_id}. Either the ID is not a valid room or the user does not have permission to see or act on the room.
    ///
    public static Request Cancel()
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_Matchmaking_Cancel2());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// DEPRECATED. Use CreateAndEnqueueRoom2.
    /// \param pool The matchmaking pool to use, which is defined for the app.
    /// \param maxUsers Overrides the Max Users value, which is configured in pool settings of the Developer Dashboard.
    /// \param subscribeToUpdates If true, sends a message with type MessageType.Notification_Room_RoomUpdate when the room data changes, such as when users join or leave.
    /// \param customQueryData Optional.  See "Custom criteria" section above.
    ///
    /// <b>Error codes</b>
    /// - \b 100: Pool {pool_key} does not contain custom data key {key}. You can configure matchmaking custom data at https://dashboard.oculus.com/application/&lt;app_id&gt;/matchmaking
    /// - \b 12051: Pool '{pool_key}' is configured for Quickmatch mode. In Quickmatch mode, rooms are created on users' behalf when a match is found. Specify Advanced Quickmatch or Browse mode to use this feature.
    /// - \b 12072: Unknown pool: {pool_key}. You can configure matchmaking pools at https://dashboard.oculus.com/application/&lt;app_id&gt;/matchmaking
    /// - \b 12089: You have asked to enqueue {num_users} users together, but this must be less than the maximum number of users in a room, {max_users}.
    ///
    public static Request<Models.MatchmakingEnqueueResultAndRoom> CreateAndEnqueueRoom(string pool, uint maxUsers, bool subscribeToUpdates = false, CustomQuery customQueryData = null)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.MatchmakingEnqueueResultAndRoom>(CAPI.ovr_Matchmaking_CreateAndEnqueueRoom(pool, maxUsers, subscribeToUpdates, customQueryData != null ? customQueryData.ToUnmanaged() : IntPtr.Zero));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Modes: BROWSE, QUICKMATCH (Advanced; Can Users Create Rooms = true)
    ///
    /// See overview documentation above.
    ///
    /// Create a matchmaking room, join it, and enqueue it. This is the preferred
    /// method. But, if you do not wish to automatically enqueue the room, you can
    /// call CreateRoom2 instead.
    ///
    /// Visit https://dashboard.oculus.com/application/[YOUR_APP_ID]/matchmaking to
    /// set up pools and queries
    /// \param pool The matchmaking pool to use, which is defined for the app.
    /// \param matchmakingOptions Additional matchmaking configuration for this request. Optional.
    ///
    /// <b>Error codes</b>
    /// - \b 100: Pool {pool_key} does not contain custom data key {key}. You can configure matchmaking custom data at https://dashboard.oculus.com/application/&lt;app_id&gt;/matchmaking
    /// - \b 12051: Pool '{pool_key}' is configured for Quickmatch mode. In Quickmatch mode, rooms are created on users' behalf when a match is found. Specify Advanced Quickmatch or Browse mode to use this feature.
    /// - \b 12072: Unknown pool: {pool_key}. You can configure matchmaking pools at https://dashboard.oculus.com/application/&lt;app_id&gt;/matchmaking
    /// - \b 12089: You have asked to enqueue {num_users} users together, but this must be less than the maximum number of users in a room, {max_users}.
    ///
    public static Request<Models.MatchmakingEnqueueResultAndRoom> CreateAndEnqueueRoom2(string pool, MatchmakingOptions matchmakingOptions = null)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.MatchmakingEnqueueResultAndRoom>(CAPI.ovr_Matchmaking_CreateAndEnqueueRoom2(pool, (IntPtr)matchmakingOptions));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// DEPRECATED. Use CreateRoom2.
    /// \param pool The matchmaking pool to use, which is defined for the app.
    /// \param maxUsers Overrides the Max Users value, which is configured in pool settings of the Developer Dashboard.
    /// \param subscribeToUpdates If true, sends a message with type MessageType.Notification_Room_RoomUpdate when room data changes, such as when users join or leave.
    ///
    public static Request<Models.Room> CreateRoom(string pool, uint maxUsers, bool subscribeToUpdates = false)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Room>(CAPI.ovr_Matchmaking_CreateRoom(pool, maxUsers, subscribeToUpdates));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Create a matchmaking room and join it, but do not enqueue the room. After
    /// creation, you can call EnqueueRoom2. However, Oculus recommends using
    /// CreateAndEnqueueRoom2 instead.
    ///
    /// Modes: BROWSE, QUICKMATCH (Advanced; Can Users Create Rooms = true)
    ///
    /// Create a matchmaking room and join it, but do not enqueue the room. After
    /// creation, you can call EnqueueRoom. Consider using CreateAndEnqueueRoom
    /// instead.
    ///
    /// Visit https://dashboard.oculus.com/application/[YOUR_APP_ID]/matchmaking to
    /// set up pools and queries
    /// \param pool The matchmaking pool to use, which is defined for the app.
    /// \param matchmakingOptions Additional matchmaking configuration for this request. Optional.
    ///
    public static Request<Models.Room> CreateRoom2(string pool, MatchmakingOptions matchmakingOptions = null)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Room>(CAPI.ovr_Matchmaking_CreateRoom2(pool, (IntPtr)matchmakingOptions));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// DEPRECATED. Use Enqueue2.
    /// \param pool The pool to enqueue in.
    /// \param customQueryData Optional.  See "Custom criteria" section above.
    ///
    /// <b>Error codes</b>
    /// - \b 100: Pool {pool_key} does not contain custom data key {key}. You can configure matchmaking custom data at https://dashboard.oculus.com/application/&lt;app_id&gt;/matchmaking
    /// - \b 12072: Unknown pool: {pool_key}. You can configure matchmaking pools at https://dashboard.oculus.com/application/&lt;app_id&gt;/matchmaking
    ///
    public static Request<Models.MatchmakingEnqueueResult> Enqueue(string pool, CustomQuery customQueryData = null)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.MatchmakingEnqueueResult>(CAPI.ovr_Matchmaking_Enqueue(pool, customQueryData != null ? customQueryData.ToUnmanaged() : IntPtr.Zero));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Modes: QUICKMATCH
    ///
    /// See overview documentation above.
    ///
    /// Enqueue yourself to await an available matchmaking room. The platform
    /// returns a MessageType.Notification_Matchmaking_MatchFound message when a
    /// match is found. Call Rooms.Join2() on the returned room. The response
    /// contains useful information to display to the user to set expectations for
    /// how long it will take to get a match.
    ///
    /// If the user stops waiting, call Matchmaking.Cancel().
    /// \param pool The pool to enqueue in.
    /// \param matchmakingOptions Additional matchmaking configuration for this request. Optional.
    ///
    /// <b>Error codes</b>
    /// - \b 100: Pool {pool_key} does not contain custom data key {key}. You can configure matchmaking custom data at https://dashboard.oculus.com/application/&lt;app_id&gt;/matchmaking
    /// - \b 12072: Unknown pool: {pool_key}. You can configure matchmaking pools at https://dashboard.oculus.com/application/&lt;app_id&gt;/matchmaking
    ///
    public static Request<Models.MatchmakingEnqueueResult> Enqueue2(string pool, MatchmakingOptions matchmakingOptions = null)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.MatchmakingEnqueueResult>(CAPI.ovr_Matchmaking_Enqueue2(pool, (IntPtr)matchmakingOptions));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// DEPRECATED. Please use Matchmaking.EnqueueRoom2() instead.
    /// \param roomID Returned either from MessageType.Notification_Matchmaking_MatchFound or from Matchmaking.CreateRoom().
    /// \param customQueryData Optional.  See the "Custom criteria" section above.
    ///
    /// <b>Error codes</b>
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is currently in another room (perhaps on another device), and thus is no longer in this room. Users can only be in one room at a time. If they are active on two different devices at once, there will be undefined behavior.
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is not in the room (or any room). Perhaps they already left, or they stopped heartbeating. If this is a test environment, make sure you are not using the deprecated initialization methods ovr_PlatformInitializeStandaloneAccessToken (C++)/StandalonePlatform.Initialize(accessToken) (C#).
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is not the owner of the room.
    /// - \b 100: Invalid room_id: {room_id}. Either the ID is not a valid room or the user does not have permission to see or act on the room.
    /// - \b 12051: Pool '{pool_key}' is configured for Quickmatch mode. In Quickmatch mode, rooms are created on users' behalf when a match is found. Specify Advanced Quickmatch or Browse mode to use this feature.
    ///
    public static Request<Models.MatchmakingEnqueueResult> EnqueueRoom(UInt64 roomID, CustomQuery customQueryData = null)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.MatchmakingEnqueueResult>(CAPI.ovr_Matchmaking_EnqueueRoom(roomID, customQueryData != null ? customQueryData.ToUnmanaged() : IntPtr.Zero));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Modes: BROWSE (for Rooms only), ROOM
    ///
    /// See the overview documentation above. Enqueue yourself to await an
    /// available matchmaking room. MessageType.Notification_Matchmaking_MatchFound
    /// gets enqueued when a match is found.
    ///
    /// The response contains useful information to display to the user to set
    /// expectations for how long it will take to get a match.
    ///
    /// If the user stops waiting, call Matchmaking.Cancel().
    /// \param roomID Returned either from MessageType.Notification_Matchmaking_MatchFound or from Matchmaking.CreateRoom().
    /// \param matchmakingOptions Additional matchmaking configuration for this request. Optional.
    ///
    /// <b>Error codes</b>
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is currently in another room (perhaps on another device), and thus is no longer in this room. Users can only be in one room at a time. If they are active on two different devices at once, there will be undefined behavior.
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is not in the room (or any room). Perhaps they already left, or they stopped heartbeating. If this is a test environment, make sure you are not using the deprecated initialization methods ovr_PlatformInitializeStandaloneAccessToken (C++)/StandalonePlatform.Initialize(accessToken) (C#).
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is not the owner of the room.
    /// - \b 100: Invalid room_id: {room_id}. Either the ID is not a valid room or the user does not have permission to see or act on the room.
    /// - \b 12051: Pool '{pool_key}' is configured for Quickmatch mode. In Quickmatch mode, rooms are created on users' behalf when a match is found. Specify Advanced Quickmatch or Browse mode to use this feature.
    ///
    public static Request<Models.MatchmakingEnqueueResult> EnqueueRoom2(UInt64 roomID, MatchmakingOptions matchmakingOptions = null)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.MatchmakingEnqueueResult>(CAPI.ovr_Matchmaking_EnqueueRoom2(roomID, (IntPtr)matchmakingOptions));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Modes: QUICKMATCH, BROWSE
    ///
    /// Used to debug the state of the current matchmaking pool queue. This is not
    /// intended to be used in production.
    ///
    public static Request<Models.MatchmakingAdminSnapshot> GetAdminSnapshot()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.MatchmakingAdminSnapshot>(CAPI.ovr_Matchmaking_GetAdminSnapshot());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// DEPRECATED. Use ovr_Room_Join2.
    /// \param roomID ID of a room previously returned from MessageType.Notification_Matchmaking_MatchFound or Matchmaking.Browse().
    /// \param subscribeToUpdates If true, sends a message with type MessageType.Notification_Room_RoomUpdate when room data changes, such as when users join or leave.
    ///
    public static Request<Models.Room> JoinRoom(UInt64 roomID, bool subscribeToUpdates = false)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Room>(CAPI.ovr_Matchmaking_JoinRoom(roomID, subscribeToUpdates));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Modes: QUICKMATCH, BROWSE (+ Skill Pool)
    ///
    /// For pools with skill-based matching. See overview documentation above.
    ///
    /// Call after calling Rooms.Join2() when the players are present to begin a
    /// rated match for which you plan to report the results (using
    /// Matchmaking.ReportResultInsecure()).
    ///
    /// <b>Error codes</b>
    /// - \b 100: There is no active match associated with the room {room_id}.
    /// - \b 100: You can only start matches, report matches, and track skill ratings in matchmaking rooms. {room_id} is a room, but it is not a matchmaking room.
    ///
    public static Request StartMatch(UInt64 roomID)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_Matchmaking_StartMatch(roomID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Indicates that a match has been found, for example after calling
    /// Matchmaking.Enqueue(). Use Message.GetRoom() to extract the matchmaking
    /// room.
    ///
    public static void SetMatchFoundNotificationCallback(Message<Models.Room>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_Matchmaking_MatchFound,
        callback
      );
    }

  }

  public static partial class Media
  {
    /// Launch the Share to Facebook modal via a deeplink to Home on Gear VR,
    /// allowing users to share local media files to Facebook. Accepts a
    /// postTextSuggestion string for the default text of the Facebook post.
    /// Requires a filePath string as the path to the image to be shared to
    /// Facebook. This image should be located in your app's internal storage
    /// directory. Requires a contentType indicating the type of media to be shared
    /// (only 'photo' is currently supported.)
    /// \param postTextSuggestion this text will prepopulate the facebook status text-input box within the share modal
    /// \param filePath path to the file to be shared to facebook
    /// \param contentType content type of the media to be shared
    ///
    public static Request<Models.ShareMediaResult> ShareToFacebook(string postTextSuggestion, string filePath, MediaContentType contentType)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.ShareMediaResult>(CAPI.ovr_Media_ShareToFacebook(postTextSuggestion, filePath, contentType));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class NetSync
  {
    /// Sent when the status of a connection has changed.
    ///
    public static void SetConnectionStatusChangedNotificationCallback(Message<Models.NetSyncConnection>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_NetSync_ConnectionStatusChanged,
        callback
      );
    }

    /// Sent when the list of known connected sessions has changed. Contains the
    /// new list of sessions.
    ///
    public static void SetSessionsChangedNotificationCallback(Message<Models.NetSyncSessionsChangedNotification>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_NetSync_SessionsChanged,
        callback
      );
    }

  }

  public static partial class Net
  {
    /// Indicates that a connection has been established or there's been an error.
    /// Use NetworkingPeer.GetState() to get the result; as above,
    /// NetworkingPeer.GetID() returns the ID of the peer this message is for.
    ///
    public static void SetConnectionStateChangedCallback(Message<Models.NetworkingPeer>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_Networking_ConnectionStateChange,
        callback
      );
    }

    /// Indicates that another user is attempting to establish a P2P connection
    /// with us. Use NetworkingPeer.GetID() to extract the ID of the peer.
    ///
    public static void SetPeerConnectRequestCallback(Message<Models.NetworkingPeer>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_Networking_PeerConnectRequest,
        callback
      );
    }

    /// Generated in response to Net.Ping(). Either contains ping time in
    /// microseconds or indicates that there was a timeout.
    ///
    public static void SetPingResultNotificationCallback(Message<Models.PingResult>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_Networking_PingResult,
        callback
      );
    }

  }

  public static partial class Notifications
  {
    /// Retrieve a list of all pending room invites for your application (for
    /// example, notifications that may have been sent before the user launched
    /// your game). You can also get push notifications with
    /// MessageType.Notification_Room_InviteReceived.
    ///
    public static Request<Models.RoomInviteNotificationList> GetRoomInviteNotifications()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.RoomInviteNotificationList>(CAPI.ovr_Notification_GetRoomInvites());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Mark a notification as read. This causes it to disappear from the Universal
    /// Menu, the Oculus App, Oculus Home, and in-app retrieval.
    ///
    public static Request MarkAsRead(UInt64 notificationID)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_Notification_MarkAsRead(notificationID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class Parties
  {
    /// Load the party the current user is in.
    ///
    public static Request<Models.Party> GetCurrent()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Party>(CAPI.ovr_Party_GetCurrent());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Indicates that party has been updated
    ///
    public static void SetPartyUpdateNotificationCallback(Message<Models.PartyUpdateNotification>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_Party_PartyUpdate,
        callback
      );
    }

  }

  public static partial class RichPresence
  {
    /// DEPRECATED. Use the clear method in group presence
    ///
    public static Request Clear()
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_RichPresence_Clear());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Gets all the destinations that the presence can be set to
    ///
    public static Request<Models.DestinationList> GetDestinations()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.DestinationList>(CAPI.ovr_RichPresence_GetDestinations());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// DEPRECATED. Use GroupPresence.Set().
    ///
    public static Request Set(RichPresenceOptions richPresenceOptions)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_RichPresence_Set((IntPtr)richPresenceOptions));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class Rooms
  {
    /// DEPRECATED. Use CreateAndJoinPrivate2.
    /// \param joinPolicy Specifies who can join the room without an invite.
    /// \param maxUsers The maximum number of users allowed in the room, including the creator.
    /// \param subscribeToUpdates If true, sends a message with type MessageType.Notification_Room_RoomUpdate when room data changes, such as when users join or leave.
    ///
    /// <b>Error codes</b>
    /// - \b 100: Something went wrong.
    /// - \b 12037: Rooms cannot allow more than {limit} users to join. Please set the max users to a lower amount.
    ///
    public static Request<Models.Room> CreateAndJoinPrivate(RoomJoinPolicy joinPolicy, uint maxUsers, bool subscribeToUpdates = false)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Room>(CAPI.ovr_Room_CreateAndJoinPrivate(joinPolicy, maxUsers, subscribeToUpdates));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Creates a new private (client controlled) room and adds the caller to it.
    /// This type of room is good for matches where the user wants to play with
    /// friends, as they're primarially discoverable by examining which rooms your
    /// friends are in.
    /// \param joinPolicy Specifies who can join the room without an invite.
    /// \param maxUsers The maximum number of users allowed in the room, including the creator.
    /// \param roomOptions Additional room configuration for this request. Optional.
    ///
    /// <b>Error codes</b>
    /// - \b 100: Something went wrong.
    /// - \b 12037: Rooms cannot allow more than {limit} users to join. Please set the max users to a lower amount.
    ///
    public static Request<Models.Room> CreateAndJoinPrivate2(RoomJoinPolicy joinPolicy, uint maxUsers, RoomOptions roomOptions)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Room>(CAPI.ovr_Room_CreateAndJoinPrivate2(joinPolicy, maxUsers, (IntPtr)roomOptions));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Allows arbitrary rooms for the application to be loaded.
    /// \param roomID The room to load.
    ///
    public static Request<Models.Room> Get(UInt64 roomID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Room>(CAPI.ovr_Room_Get(roomID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Easy loading of the room you're currently in. If you don't want live
    /// updates on your current room (by using subscribeToUpdates), you can use
    /// this to refresh the data.
    ///
    public static Request<Models.Room> GetCurrent()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Room>(CAPI.ovr_Room_GetCurrent());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Allows the current room for a given user to be loaded. Remember that the
    /// user's privacy settings may not allow their room to be loaded. Because of
    /// this, it's often possible to load the users in a room, but not to take
    /// those users and load their room.
    /// \param userID ID of the user for which to load the room.
    ///
    public static Request<Models.Room> GetCurrentForUser(UInt64 userID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Room>(CAPI.ovr_Room_GetCurrentForUser(userID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// DEPRECATED. Use GetInvitableUsers2.
    ///
    public static Request<Models.UserList> GetInvitableUsers()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.UserList>(CAPI.ovr_Room_GetInvitableUsers());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Loads a list of users you can invite to a room. These are pulled from your
    /// friends list and recently met lists and filtered for relevance and
    /// interest. If the room cannot be joined, this list will be empty. By
    /// default, the invitable users returned will be for the user's current room.
    ///
    /// If your application grouping was created after September 9 2017, recently
    /// met users will be included by default. If your application grouping was
    /// created before then, you can go to edit the setting in the "Rooms and
    /// Matchmaking" section of Platform Services at dashboard.oculus.com
    ///
    /// Customization can be done via RoomOptions. Create this object with
    /// RoomOptions(). The params that could be used are:
    ///
    /// 1. RoomOptions.SetRoomId()- will return the invitable users for this room
    /// (instead of the current room).
    ///
    /// 2. RoomOptions.SetOrdering() - returns the list of users in the provided
    /// ordering (see UserOrdering enum).
    ///
    /// 3. RoomOptions.SetRecentlyMetTimeWindow() - how long long ago should we
    /// include users you've recently met in the results?
    ///
    /// 4. RoomOptions.SetMaxUserResults() - we will limit the number of results
    /// returned. By default, the number is unlimited, but the server may choose to
    /// limit results for performance reasons.
    ///
    /// 5. RoomOptions.SetExcludeRecentlyMet() - Don't include users recently in
    /// rooms with this user in the result. Also, see the above comment.
    ///
    /// Example custom C++ usage:
    ///
    ///   auto roomOptions = ovr_RoomOptions_Create();
    ///   ovr_RoomOptions_SetOrdering(roomOptions, ovrUserOrdering_PresenceAlphabetical);
    ///   ovr_RoomOptions_SetRoomId(roomOptions, roomID);
    ///   ovr_Room_GetInvitableUsers2(roomOptions);
    ///   ovr_RoomOptions_Destroy(roomOptions);
    /// \param roomOptions Additional configuration for this request. Optional.
    ///
    public static Request<Models.UserList> GetInvitableUsers2(RoomOptions roomOptions = null)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.UserList>(CAPI.ovr_Room_GetInvitableUsers2((IntPtr)roomOptions));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Fetches the list of moderated rooms created for the application.
    ///
    public static Request<Models.RoomList> GetModeratedRooms()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.RoomList>(CAPI.ovr_Room_GetModeratedRooms());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Invites a user to the specified room. They will receive a notification via
    /// MessageType.Notification_Room_InviteReceived if they are in your game,
    /// and/or they can poll for room invites using
    /// Notifications.GetRoomInviteNotifications().
    /// \param roomID The ID of your current room.
    /// \param inviteToken A user's invite token, returned by Rooms.GetInvitableUsers().
    ///
    /// <b>Error codes</b>
    /// - \b 100: The invite token has expired, the user will need to be reinvited to the room.
    /// - \b 100: The target user cannot join you in your current experience
    /// - \b 100: You cannot send an invite to a room you are not in
    ///
    public static Request<Models.Room> InviteUser(UInt64 roomID, string inviteToken)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Room>(CAPI.ovr_Room_InviteUser(roomID, inviteToken));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Joins the target room (leaving the one you're currently in).
    /// \param roomID The room to join.
    /// \param subscribeToUpdates If true, sends a message with type MessageType.Notification_Room_RoomUpdate when room data changes, such as when users join or leave.
    ///
    /// <b>Error codes</b>
    /// - \b 10: The room you're attempting to join is currently locked. Please try again later.
    /// - \b 10: You don't have permission to enter this room. You may need to be invited first.
    /// - \b 100: Invalid room_id: {room_id}. Either the ID is not a valid room or the user does not have permission to see or act on the room.
    /// - \b 100: The room you're attempting to join is full. Please try again later.
    /// - \b 100: This game isn't available. If it already started or was canceled, you can host a new game at any point.
    ///
    public static Request<Models.Room> Join(UInt64 roomID, bool subscribeToUpdates = false)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Room>(CAPI.ovr_Room_Join(roomID, subscribeToUpdates));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Joins the target room (leaving the one you're currently in).
    /// \param roomID The room to join.
    /// \param roomOptions Additional room configuration for this request. Optional.
    ///
    /// <b>Error codes</b>
    /// - \b 10: The room you're attempting to join is currently locked. Please try again later.
    /// - \b 10: You don't have permission to enter this room. You may need to be invited first.
    /// - \b 100: Invalid room_id: {room_id}. Either the ID is not a valid room or the user does not have permission to see or act on the room.
    /// - \b 100: The room you're attempting to join is full. Please try again later.
    /// - \b 100: This game isn't available. If it already started or was canceled, you can host a new game at any point.
    ///
    public static Request<Models.Room> Join2(UInt64 roomID, RoomOptions roomOptions)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Room>(CAPI.ovr_Room_Join2(roomID, (IntPtr)roomOptions));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Allows the room owner to kick a user out of the current room.
    /// \param roomID The room that you currently own (check Room.GetOwner()).
    /// \param userID The user to be kicked (cannot be yourself).
    /// \param kickDurationSeconds Length of the ban, in seconds.
    ///
    /// <b>Error codes</b>
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is not in the room (or any room). Perhaps they already left, or they stopped heartbeating. If this is a test environment, make sure you are not using the deprecated initialization methods ovr_PlatformInitializeStandaloneAccessToken (C++)/StandalonePlatform.Initialize(accessToken) (C#).
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is not the owner of the room.
    /// - \b 100: You cannot remove yourself from room {room_id}
    ///
    public static Request<Models.Room> KickUser(UInt64 roomID, UInt64 userID, int kickDurationSeconds)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Room>(CAPI.ovr_Room_KickUser(roomID, userID, kickDurationSeconds));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Launch the invitable user flow to invite to the logged in user's current
    /// room. This is intended to be a nice shortcut for developers not wanting to
    /// build out their own Invite UI although it has the same rules as if you
    /// build it yourself.
    ///
    public static Request LaunchInvitableUserFlow(UInt64 roomID)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_Room_LaunchInvitableUserFlow(roomID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Removes you from your current room. Returns the solo room you are now in if
    /// it succeeds
    /// \param roomID The room you're currently in.
    ///
    /// <b>Error codes</b>
    /// - \b 100: Something went wrong.
    ///
    public static Request<Models.Room> Leave(UInt64 roomID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Room>(CAPI.ovr_Room_Leave(roomID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Allows the room owner to set the description of their room.
    /// \param roomID The room that you currently own (check Room.GetOwner()).
    /// \param description The new name of the room.
    ///
    /// <b>Error codes</b>
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is currently in another room (perhaps on another device), and thus is no longer in this room. Users can only be in one room at a time. If they are active on two different devices at once, there will be undefined behavior.
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is not in the room (or any room). Perhaps they already left, or they stopped heartbeating. If this is a test environment, make sure you are not using the deprecated initialization methods ovr_PlatformInitializeStandaloneAccessToken (C++)/StandalonePlatform.Initialize(accessToken) (C#).
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is not the owner of the room.
    ///
    public static Request<Models.Room> SetDescription(UInt64 roomID, string description)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Room>(CAPI.ovr_Room_SetDescription(roomID, description));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Disallow new members from being able to join the room. This will prevent
    /// joins from Rooms.Join(), invites, 'Join From Home', etc. Users that are in
    /// the room at the time of lockdown WILL be able to rejoin.
    /// \param roomID The room whose membership you want to lock or unlock.
    /// \param membershipLockStatus The new LockStatus for the room
    ///
    /// <b>Error codes</b>
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is currently in another room (perhaps on another device), and thus is no longer in this room. Users can only be in one room at a time. If they are active on two different devices at once, there will be undefined behavior.
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is not in the room (or any room). Perhaps they already left, or they stopped heartbeating. If this is a test environment, make sure you are not using the deprecated initialization methods ovr_PlatformInitializeStandaloneAccessToken (C++)/StandalonePlatform.Initialize(accessToken) (C#).
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is not the owner of the room.
    /// - \b 100: Invalid room_id: {room_id}. Either the ID is not a valid room or the user does not have permission to see or act on the room.
    ///
    public static Request<Models.Room> UpdateMembershipLockStatus(UInt64 roomID, RoomMembershipLockStatus membershipLockStatus)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Room>(CAPI.ovr_Room_UpdateMembershipLockStatus(roomID, membershipLockStatus));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Allows the room owner to transfer ownership to someone else.
    /// \param roomID The room that the user owns (check Room.GetOwner()).
    /// \param userID The new user to make an owner; the user must be in the room.
    ///
    /// <b>Error codes</b>
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is currently in another room (perhaps on another device), and thus is no longer in this room. Users can only be in one room at a time. If they are active on two different devices at once, there will be undefined behavior.
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is not in the room (or any room). Perhaps they already left, or they stopped heartbeating. If this is a test environment, make sure you are not using the deprecated initialization methods ovr_PlatformInitializeStandaloneAccessToken (C++)/StandalonePlatform.Initialize(accessToken) (C#).
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is not the owner of the room.
    /// - \b 100: Parameter {parameter}: invalid user id: {user_id}
    ///
    public static Request UpdateOwner(UInt64 roomID, UInt64 userID)
    {
      if (Core.IsInitialized())
      {
        return new Request(CAPI.ovr_Room_UpdateOwner(roomID, userID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Sets the join policy of the user's private room.
    /// \param roomID The room ID that the user owns (check Room.GetOwner()).
    /// \param newJoinPolicy The new join policy for the room.
    ///
    /// <b>Error codes</b>
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is currently in another room (perhaps on another device), and thus is no longer in this room. Users can only be in one room at a time. If they are active on two different devices at once, there will be undefined behavior.
    /// - \b 10: Room {room_id}: The user does not have permission to {cannot_action} because the user is not in the room (or any room). Perhaps they already left, or they stopped heartbeating. If this is a test environment, make sure you are not using the deprecated initialization methods ovr_PlatformInitializeStandaloneAccessToken (C++)/StandalonePlatform.Initialize(accessToken) (C#).
    ///
    public static Request<Models.Room> UpdatePrivateRoomJoinPolicy(UInt64 roomID, RoomJoinPolicy newJoinPolicy)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.Room>(CAPI.ovr_Room_UpdatePrivateRoomJoinPolicy(roomID, newJoinPolicy));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Indicates that the user has accepted an invitation, for example in Oculus
    /// Home. Use Message.GetString() to extract the ID of the room that the user
    /// has been inivted to as a string. Then call ovrID_FromString() to parse it
    /// into an ovrID.
    ///
    /// Note that you must call Rooms.Join() if you want to actually join the room.
    ///
    public static void SetRoomInviteAcceptedNotificationCallback(Message<string>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_Room_InviteAccepted,
        callback
      );
    }

    /// Handle this to notify the user when they've received an invitation to join
    /// a room in your game. You can use this in lieu of, or in addition to,
    /// polling for room invitations via
    /// Notifications.GetRoomInviteNotifications().
    ///
    public static void SetRoomInviteReceivedNotificationCallback(Message<Models.RoomInviteNotification>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_Room_InviteReceived,
        callback
      );
    }

    /// Indicates that the current room has been updated. Use Message.GetRoom() to
    /// extract the updated room.
    ///
    public static void SetUpdateNotificationCallback(Message<Models.Room>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_Room_RoomUpdate,
        callback
      );
    }

  }

  public static partial class Session
  {
  }

  public static partial class Users
  {
    /// Retrieve the user with the given ID. This might fail if the ID is invalid
    /// or the user is blocked.
    ///
    /// NOTE: Users will have a unique ID per application.
    /// \param userID User ID retrieved with this application.
    ///
    public static Request<Models.User> Get(UInt64 userID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.User>(CAPI.ovr_User_Get(userID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Return an access token for this user, suitable for making REST calls
    /// against graph.oculus.com.
    ///
    public static Request<string> GetAccessToken()
    {
      if (Core.IsInitialized())
      {
        return new Request<string>(CAPI.ovr_User_GetAccessToken());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Return the IDs of users entitled to use the current app that are blocked by
    /// the specified user
    ///
    public static Request<Models.BlockedUserList> GetBlockedUsers()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.BlockedUserList>(CAPI.ovr_User_GetBlockedUsers());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Retrieve the currently signed in user. This call is available offline.
    ///
    /// NOTE: This will not return the user's presence as it should always be
    /// 'online' in your application.
    ///
    /// NOTE: Users will have a unique ID per application.
    ///
    /// <b>Error codes</b>
    /// - \b 100: Something went wrong.
    ///
    public static Request<Models.User> GetLoggedInUser()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.User>(CAPI.ovr_User_GetLoggedInUser());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Retrieve a list of the logged in user's friends.
    ///
    public static Request<Models.UserList> GetLoggedInUserFriends()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.UserList>(CAPI.ovr_User_GetLoggedInUserFriends());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Retrieve a list of the logged in user's friends and any rooms they might be
    /// in.
    ///
    public static Request<Models.UserAndRoomList> GetLoggedInUserFriendsAndRooms()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.UserAndRoomList>(CAPI.ovr_User_GetLoggedInUserFriendsAndRooms());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Returns a list of users that the logged in user was in a room with
    /// recently, sorted by relevance, along with any rooms they might be in. All
    /// you need to do to use this method is to use our Rooms API, and we will
    /// track the number of times users are together, their most recent encounter,
    /// and the amount of time they spend together.
    ///
    /// Customization can be done via UserOptions. Create this object with
    /// UserOptions(). The params that could be used are:
    ///
    /// 1. UserOptions.SetTimeWindow() - how recently should the users have played?
    /// The default is TimeWindow.ThirtyDays.
    ///
    /// 2. UserOptions.SetMaxUsers() - we will limit the number of results
    /// returned. By default, the number is unlimited, but the server may choose to
    /// limit results for performance reasons.
    /// \param userOptions Additional configuration for this request. Optional.
    ///
    public static Request<Models.UserAndRoomList> GetLoggedInUserRecentlyMetUsersAndRooms(UserOptions userOptions = null)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.UserAndRoomList>(CAPI.ovr_User_GetLoggedInUserRecentlyMetUsersAndRooms((IntPtr)userOptions));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// returns an ovrID which is unique per org. allows different apps within the
    /// same org to identify the user.
    /// \param userID to load the org scoped id of
    ///
    public static Request<Models.OrgScopedID> GetOrgScopedID(UInt64 userID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.OrgScopedID>(CAPI.ovr_User_GetOrgScopedID(userID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Returns all accounts belonging to this user. Accounts are the Oculus user
    /// and x-users that are linked to this user.
    ///
    public static Request<Models.SdkAccountList> GetSdkAccounts()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.SdkAccountList>(CAPI.ovr_User_GetSdkAccounts());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Part of the scheme to confirm the identity of a particular user in your
    /// backend. You can pass the result of Users.GetUserProof() and a user ID from
    /// Users.Get() to your your backend. Your server can then use our api to
    /// verify identity. 'https://graph.oculus.com/user_nonce_validate?nonce=USER_P
    /// ROOF&user_id=USER_ID&access_token=ACCESS_TOKEN'
    ///
    /// NOTE: The nonce is only good for one check and then it is invalidated.
    ///
    public static Request<Models.UserProof> GetUserProof()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.UserProof>(CAPI.ovr_User_GetUserProof());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Launch the flow for blocking the given user. You can't be friended,
    /// invited, or searched by a blocked user, for example. You can remove the
    /// block via ovr_User_LaunchUnblockFlow.
    /// \param userID User ID of user being blocked
    ///
    public static Request<Models.LaunchBlockFlowResult> LaunchBlockFlow(UInt64 userID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.LaunchBlockFlowResult>(CAPI.ovr_User_LaunchBlockFlow(userID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Launch the flow for sending a friend request to a user.
    /// \param userID User ID of user to send a friend request to
    ///
    public static Request<Models.LaunchFriendRequestFlowResult> LaunchFriendRequestFlow(UInt64 userID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.LaunchFriendRequestFlowResult>(CAPI.ovr_User_LaunchFriendRequestFlow(userID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Launch the flow for unblocking a user that the viewer has blocked.
    /// \param userID User ID of user to unblock
    ///
    public static Request<Models.LaunchUnblockFlowResult> LaunchUnblockFlow(UInt64 userID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.LaunchUnblockFlowResult>(CAPI.ovr_User_LaunchUnblockFlow(userID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class UserDataStore
  {
    /// Delete an entry by a key from a private user data store.
    /// \param userID The ID of the user who owns this private user data store.
    /// \param key The key of entry.
    ///
    public static Request<Models.UserDataStoreUpdateResponse> PrivateDeleteEntryByKey(UInt64 userID, string key)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.UserDataStoreUpdateResponse>(CAPI.ovr_UserDataStore_PrivateDeleteEntryByKey(userID, key));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Get entries from a private user data store.
    /// \param userID The ID of the user who owns this private user data store.
    ///
    public static Request<Dictionary<string, string>> PrivateGetEntries(UInt64 userID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Dictionary<string, string>>(CAPI.ovr_UserDataStore_PrivateGetEntries(userID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Get an entry by a key from a private user data store.
    /// \param userID The ID of the user who owns this private user data store.
    /// \param key The key of entry.
    ///
    public static Request<Dictionary<string, string>> PrivateGetEntryByKey(UInt64 userID, string key)
    {
      if (Core.IsInitialized())
      {
        return new Request<Dictionary<string, string>>(CAPI.ovr_UserDataStore_PrivateGetEntryByKey(userID, key));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Write a single entry to a private user data store.
    /// \param userID The ID of the user who owns this private user data store.
    /// \param key The key of entry.
    /// \param value The value of entry.
    ///
    public static Request<Models.UserDataStoreUpdateResponse> PrivateWriteEntry(UInt64 userID, string key, string value)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.UserDataStoreUpdateResponse>(CAPI.ovr_UserDataStore_PrivateWriteEntry(userID, key, value));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Delete an entry by a key from a public user data store.
    /// \param userID The ID of the user who owns this public user data store.
    /// \param key The key of entry.
    ///
    public static Request<Models.UserDataStoreUpdateResponse> PublicDeleteEntryByKey(UInt64 userID, string key)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.UserDataStoreUpdateResponse>(CAPI.ovr_UserDataStore_PublicDeleteEntryByKey(userID, key));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Get entries from a public user data store.
    /// \param userID The ID of the user who owns this public user data store.
    ///
    public static Request<Dictionary<string, string>> PublicGetEntries(UInt64 userID)
    {
      if (Core.IsInitialized())
      {
        return new Request<Dictionary<string, string>>(CAPI.ovr_UserDataStore_PublicGetEntries(userID));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Get an entry by a key from a public user data store.
    /// \param userID The ID of the user who owns this public user data store.
    /// \param key The key of entry.
    ///
    public static Request<Dictionary<string, string>> PublicGetEntryByKey(UInt64 userID, string key)
    {
      if (Core.IsInitialized())
      {
        return new Request<Dictionary<string, string>>(CAPI.ovr_UserDataStore_PublicGetEntryByKey(userID, key));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Write a single entry to a public user data store.
    /// \param userID The ID of the user who owns this public user data store.
    /// \param key The key of entry.
    /// \param value The value of entry.
    ///
    public static Request<Models.UserDataStoreUpdateResponse> PublicWriteEntry(UInt64 userID, string key, string value)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.UserDataStoreUpdateResponse>(CAPI.ovr_UserDataStore_PublicWriteEntry(userID, key, value));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class Voip
  {
    /// Gets whether the microphone is currently available to the app. This can be
    /// used to show if the user's voice is able to be heard by other users.
    ///
    public static Request<Models.MicrophoneAvailabilityState> GetMicrophoneAvailability()
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.MicrophoneAvailabilityState>(CAPI.ovr_Voip_GetMicrophoneAvailability());
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Sets whether SystemVoip should be suppressed so that this app's Voip can
    /// use the mic and play incoming Voip audio. Once microphone switching
    /// functionality for the user is released, this function will no longer work.
    /// You can use get_microphone_availability to see if the user has allowed the
    /// app access to the microphone.
    ///
    public static Request<Models.SystemVoipState> SetSystemVoipSuppressed(bool suppressed)
    {
      if (Core.IsInitialized())
      {
        return new Request<Models.SystemVoipState>(CAPI.ovr_Voip_SetSystemVoipSuppressed(suppressed));
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    /// Sent when another user is attempting to establish a VoIP connection. Use
    /// Message.GetNetworkingPeer() to extract information about the user, and
    /// Voip.Accept() to accept the connection.
    ///
    public static void SetVoipConnectRequestCallback(Message<Models.NetworkingPeer>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_Voip_ConnectRequest,
        callback
      );
    }

    /// Indicates that the current microphone availability state has been updated.
    /// Use Voip.GetMicrophoneAvailability() to extract the microphone availability
    /// state.
    ///
    public static void SetMicrophoneAvailabilityStateUpdateNotificationCallback(Message<string>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_Voip_MicrophoneAvailabilityStateUpdate,
        callback
      );
    }

    /// Sent to indicate that the state of the VoIP connection changed. Use
    /// Message.GetNetworkingPeer() and NetworkingPeer.GetState() to extract the
    /// current state.
    ///
    public static void SetVoipStateChangeCallback(Message<Models.NetworkingPeer>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_Voip_StateChange,
        callback
      );
    }

    /// Sent to indicate that some part of the overall state of SystemVoip has
    /// changed. Use Message.GetSystemVoipState() and the properties of
    /// SystemVoipState to extract the state that triggered the notification.
    ///
    /// Note that the state may have changed further since the notification was
    /// generated, and that you may call the `GetSystemVoip...()` family of
    /// functions at any time to get the current state directly.
    ///
    public static void SetSystemVoipStateNotificationCallback(Message<Models.SystemVoipState>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_Voip_SystemVoipState,
        callback
      );
    }

  }

  public static partial class Vrcamera
  {
    /// Get vr camera related webrtc data channel messages for update.
    ///
    public static void SetGetDataChannelMessageUpdateNotificationCallback(Message<string>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_Vrcamera_GetDataChannelMessageUpdate,
        callback
      );
    }

    /// Get surface and update action from platform webrtc for update.
    ///
    public static void SetGetSurfaceUpdateNotificationCallback(Message<string>.Callback callback)
    {
      Callback.SetNotificationCallback(
        Message.MessageType.Notification_Vrcamera_GetSurfaceUpdate,
        callback
      );
    }

  }


  public static partial class Achievements {
    public static Request<Models.AchievementDefinitionList> GetNextAchievementDefinitionListPage(Models.AchievementDefinitionList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextAchievementDefinitionListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.AchievementDefinitionList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.Achievements_GetNextAchievementDefinitionArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    public static Request<Models.AchievementProgressList> GetNextAchievementProgressListPage(Models.AchievementProgressList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextAchievementProgressListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.AchievementProgressList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.Achievements_GetNextAchievementProgressArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class CloudStorage {
    public static Request<Models.CloudStorageMetadataList> GetNextCloudStorageMetadataListPage(Models.CloudStorageMetadataList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextCloudStorageMetadataListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.CloudStorageMetadataList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.CloudStorage_GetNextCloudStorageMetadataArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class GroupPresence {
    public static Request<Models.ApplicationInviteList> GetNextApplicationInviteListPage(Models.ApplicationInviteList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextApplicationInviteListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.ApplicationInviteList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.GroupPresence_GetNextApplicationInviteArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class IAP {
    public static Request<Models.ProductList> GetNextProductListPage(Models.ProductList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextProductListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.ProductList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.IAP_GetNextProductArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    public static Request<Models.PurchaseList> GetNextPurchaseListPage(Models.PurchaseList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextPurchaseListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.PurchaseList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.IAP_GetNextPurchaseArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class Leaderboards {
    public static Request<Models.LeaderboardList> GetNextLeaderboardListPage(Models.LeaderboardList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextLeaderboardListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.LeaderboardList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.Leaderboard_GetNextLeaderboardArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class Notifications {
    public static Request<Models.RoomInviteNotificationList> GetNextRoomInviteNotificationListPage(Models.RoomInviteNotificationList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextRoomInviteNotificationListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.RoomInviteNotificationList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.Notification_GetNextRoomInviteNotificationArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class RichPresence {
    public static Request<Models.DestinationList> GetNextDestinationListPage(Models.DestinationList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextDestinationListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.DestinationList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.RichPresence_GetNextDestinationArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class Rooms {
    public static Request<Models.RoomList> GetNextRoomListPage(Models.RoomList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextRoomListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.RoomList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.Room_GetNextRoomArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }

  public static partial class Users {
    public static Request<Models.BlockedUserList> GetNextBlockedUserListPage(Models.BlockedUserList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextBlockedUserListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.BlockedUserList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.User_GetNextBlockedUserArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    public static Request<Models.UserAndRoomList> GetNextUserAndRoomListPage(Models.UserAndRoomList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextUserAndRoomListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.UserAndRoomList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.User_GetNextUserAndRoomArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    public static Request<Models.UserList> GetNextUserListPage(Models.UserList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextUserListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.UserList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.User_GetNextUserArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

    public static Request<Models.UserCapabilityList> GetNextUserCapabilityListPage(Models.UserCapabilityList list) {
      if (!list.HasNextPage)
      {
        Debug.LogWarning("Oculus.Platform.GetNextUserCapabilityListPage: List has no next page");
        return null;
      }

      if (Core.IsInitialized())
      {
        return new Request<Models.UserCapabilityList>(
          CAPI.ovr_HTTP_GetWithMessageType(
            list.NextUrl,
            (int)Message.MessageType.User_GetNextUserCapabilityArrayPage
          )
        );
      }

      Debug.LogError(Oculus.Platform.Core.PlatformUninitializedError);
      return null;
    }

  }


}
