// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]

namespace Oculus.Platform
{
  using UnityEngine;
  using System;
  using System.Collections;
  using System.Collections.Generic;

  public static class PlatformInternal
  {
    // Keep this enum in sync with ovrMessageTypeInternal in OVR_Platform_Internal.h
    public enum MessageTypeInternal : uint { //TODO - rename this to type; it's already in Message class
      AbuseReport_LaunchAdvancedReportFlow   = 0x4CB13A6E,
      Application_ExecuteCoordinatedLaunch   = 0x267DB4F4,
      Application_GetInstalledApplications   = 0x520F744C,
      Avatar_UpdateMetaData                  = 0x7BCFD98E,
      Cal_FinalizeApplication                = 0x1DA9CBD5,
      Cal_GetSuggestedApplications           = 0x56707015,
      Cal_ProposeApplication                 = 0x4E83F2DD,
      Colocation_GetCurrentMapUuid           = 0x34557EB2,
      Colocation_RequestMap                  = 0x3215666D,
      Colocation_ShareMap                    = 0x186DC4DD,
      GraphAPI_Get                           = 0x30FF006E,
      GraphAPI_Post                          = 0x76A5A7C4,
      HTTP_Get                               = 0x6FB63223,
      HTTP_GetToFile                         = 0x4E81DC59,
      HTTP_MultiPartPost                     = 0x5842D210,
      HTTP_Post                              = 0x6B36A54F,
      Livestreaming_IsAllowedForApplication  = 0x0B6D8D76,
      Livestreaming_StartPartyStream         = 0x7B2F5CDC,
      Livestreaming_StartStream              = 0x501AC7BE,
      Livestreaming_StopPartyStream          = 0x27670F58,
      Livestreaming_StopStream               = 0x44E40DCA,
      Livestreaming_UpdateMicStatus          = 0x1C577D87,
      NetSync_Connect                        = 0x646D855F,
      NetSync_Disconnect                     = 0x1569FEB5,
      NetSync_GetSessions                    = 0x6ED60A35,
      NetSync_GetVoipAttenuation             = 0x112ACA17,
      NetSync_GetVoipAttenuationDefault      = 0x577BA8A0,
      NetSync_SetVoipAttenuation             = 0x3497D7F6,
      NetSync_SetVoipAttenuationModel        = 0x6A94AD8E,
      NetSync_SetVoipChannelCfg              = 0x5C95A4F3,
      NetSync_SetVoipGroup                   = 0x58129C8E,
      NetSync_SetVoipListentoChannels        = 0x5ED0EA32,
      NetSync_SetVoipMicSource               = 0x3302F770,
      NetSync_SetVoipSessionMuted            = 0x5585FF0A,
      NetSync_SetVoipSpeaktoChannels         = 0x2DAFCDD5,
      NetSync_SetVoipStreamMode              = 0x67E19D37,
      Party_Create                           = 0x1AD31B4F,
      Party_GatherInApplication              = 0x7287C183,
      Party_Get                              = 0x5E8953BD,
      Party_GetCurrentForUser                = 0x58CBFF2A,
      Party_Invite                           = 0x35B5C4E3,
      Party_Join                             = 0x68027C73,
      Party_Leave                            = 0x329206D1,
      RichPresence_SetDestination            = 0x4F32E10D,
      RichPresence_SetIsJoinable             = 0x3E9B1F61,
      RichPresence_SetLobbySession           = 0x71010917,
      RichPresence_SetMatchSession           = 0x63DFFC8E,
      Room_CreateOrUpdateAndJoinNamed        = 0x7C8E0A91,
      Room_GetNamedRooms                     = 0x077D6E8C,
      Room_GetSocialRooms                    = 0x61881D76,
      User_CancelRecordingForReportFlow      = 0x03E0D149,
      User_GetLinkedAccounts                 = 0x5793F456,
      User_GetUserCapabilities               = 0x121C317C,
      User_LaunchReportFlow                  = 0x5662A011,
      User_LaunchReportFlow2                 = 0x7F835863,
      User_NewEntitledTestUser               = 0x11741F03,
      User_NewTestUser                       = 0x36E84F8C,
      User_NewTestUserFriends                = 0x1ED726C7,
      User_StartRecordingForReportFlow       = 0x6C6E33E3,
      User_StopRecordingAndLaunchReportFlow  = 0x60788C8B,
      User_StopRecordingAndLaunchReportFlow2 = 0x19C2B32B,
      User_TestUserCreateDeviceManifest      = 0x6570B2BD
    };

    public static void CrashApplication() {
      CAPI.ovr_CrashApplication();
    }

    internal static Message ParseMessageHandle(IntPtr messageHandle, Message.MessageType messageType)
    {
      Message message = null;
      switch ((PlatformInternal.MessageTypeInternal)messageType)
      {
        case MessageTypeInternal.User_StartRecordingForReportFlow:
          message = new MessageWithAbuseReportRecording(messageHandle);
          break;

        case MessageTypeInternal.Cal_FinalizeApplication:
          message = new MessageWithCalApplicationFinalized(messageHandle);
          break;

        case MessageTypeInternal.Cal_GetSuggestedApplications:
          message = new MessageWithCalApplicationSuggestionList(messageHandle);
          break;

        case MessageTypeInternal.Application_ExecuteCoordinatedLaunch:
        case MessageTypeInternal.Cal_ProposeApplication:
        case MessageTypeInternal.Colocation_RequestMap:
        case MessageTypeInternal.Colocation_ShareMap:
        case MessageTypeInternal.Livestreaming_StopPartyStream:
        case MessageTypeInternal.Livestreaming_UpdateMicStatus:
        case MessageTypeInternal.NetSync_SetVoipAttenuation:
        case MessageTypeInternal.NetSync_SetVoipAttenuationModel:
        case MessageTypeInternal.NetSync_SetVoipChannelCfg:
        case MessageTypeInternal.NetSync_SetVoipGroup:
        case MessageTypeInternal.NetSync_SetVoipListentoChannels:
        case MessageTypeInternal.NetSync_SetVoipMicSource:
        case MessageTypeInternal.NetSync_SetVoipSpeaktoChannels:
        case MessageTypeInternal.Party_Leave:
        case MessageTypeInternal.RichPresence_SetDestination:
        case MessageTypeInternal.RichPresence_SetIsJoinable:
        case MessageTypeInternal.RichPresence_SetLobbySession:
        case MessageTypeInternal.RichPresence_SetMatchSession:
        case MessageTypeInternal.User_CancelRecordingForReportFlow:
        case MessageTypeInternal.User_TestUserCreateDeviceManifest:
          message = new Message(messageHandle);
          break;

        case MessageTypeInternal.Application_GetInstalledApplications:
          message = new MessageWithInstalledApplicationList(messageHandle);
          break;

        case MessageTypeInternal.AbuseReport_LaunchAdvancedReportFlow:
        case MessageTypeInternal.User_LaunchReportFlow2:
          message = new MessageWithLaunchReportFlowResult(messageHandle);
          break;

        case MessageTypeInternal.User_GetLinkedAccounts:
          message = new MessageWithLinkedAccountList(messageHandle);
          break;

        case MessageTypeInternal.Livestreaming_IsAllowedForApplication:
          message = new MessageWithLivestreamingApplicationStatus(messageHandle);
          break;

        case MessageTypeInternal.Livestreaming_StartPartyStream:
        case MessageTypeInternal.Livestreaming_StartStream:
          message = new MessageWithLivestreamingStartResult(messageHandle);
          break;

        case MessageTypeInternal.Livestreaming_StopStream:
          message = new MessageWithLivestreamingVideoStats(messageHandle);
          break;

        case MessageTypeInternal.NetSync_Connect:
        case MessageTypeInternal.NetSync_Disconnect:
          message = new MessageWithNetSyncConnection(messageHandle);
          break;

        case MessageTypeInternal.NetSync_GetSessions:
          message = new MessageWithNetSyncSessionList(messageHandle);
          break;

        case MessageTypeInternal.NetSync_SetVoipSessionMuted:
        case MessageTypeInternal.NetSync_SetVoipStreamMode:
          message = new MessageWithNetSyncSetSessionPropertyResult(messageHandle);
          break;

        case MessageTypeInternal.NetSync_GetVoipAttenuation:
        case MessageTypeInternal.NetSync_GetVoipAttenuationDefault:
          message = new MessageWithNetSyncVoipAttenuationValueList(messageHandle);
          break;

        case MessageTypeInternal.Party_Get:
          message = new MessageWithParty(messageHandle);
          break;

        case MessageTypeInternal.Party_GetCurrentForUser:
          message = new MessageWithPartyUnderCurrentParty(messageHandle);
          break;

        case MessageTypeInternal.Party_Create:
        case MessageTypeInternal.Party_GatherInApplication:
        case MessageTypeInternal.Party_Invite:
        case MessageTypeInternal.Party_Join:
          message = new MessageWithPartyID(messageHandle);
          break;

        case MessageTypeInternal.Room_CreateOrUpdateAndJoinNamed:
          message = new MessageWithRoomUnderViewerRoom(messageHandle);
          break;

        case MessageTypeInternal.Room_GetNamedRooms:
        case MessageTypeInternal.Room_GetSocialRooms:
          message = new MessageWithRoomList(messageHandle);
          break;

        case MessageTypeInternal.Avatar_UpdateMetaData:
        case MessageTypeInternal.Colocation_GetCurrentMapUuid:
        case MessageTypeInternal.GraphAPI_Get:
        case MessageTypeInternal.GraphAPI_Post:
        case MessageTypeInternal.HTTP_Get:
        case MessageTypeInternal.HTTP_GetToFile:
        case MessageTypeInternal.HTTP_MultiPartPost:
        case MessageTypeInternal.HTTP_Post:
        case MessageTypeInternal.User_NewEntitledTestUser:
        case MessageTypeInternal.User_NewTestUser:
        case MessageTypeInternal.User_NewTestUserFriends:
          message = new MessageWithString(messageHandle);
          break;

        case MessageTypeInternal.User_GetUserCapabilities:
          message = new MessageWithUserCapabilityList(messageHandle);
          break;

        case MessageTypeInternal.User_LaunchReportFlow:
        case MessageTypeInternal.User_StopRecordingAndLaunchReportFlow:
        case MessageTypeInternal.User_StopRecordingAndLaunchReportFlow2:
          message = new MessageWithUserReportID(messageHandle);
          break;

      }
      return message;
    }

    public static class HTTP
    {
      public static void SetHttpTransferUpdateCallback(Message<Models.HttpTransferUpdate>.Callback callback)
      {
        Callback.SetNotificationCallback(
          Message.MessageType.Notification_HTTP_Transfer,
          callback
        );
      }
    }

    public static Request<Models.PlatformInitialize> InitializeStandaloneAsync(ulong appID, string accessToken)
    {
      var platform = new StandalonePlatform();
      var initRequest = platform.AsyncInitialize(appID, accessToken);

      if (initRequest == null)
      {
        throw new UnityException("Oculus Platform failed to initialize.");
      }

      // This function is not named well.  Actually means that we have called platform init.
      // Async initialization may not have finished at this point.
      Platform.Core.ForceInitialized();
      (new GameObject("Oculus.Platform.CallbackRunner")).AddComponent<CallbackRunner>();
      return initRequest;
    }

    public static class Users
    {
      public static Request<Models.LinkedAccountList> GetLinkedAccounts(ServiceProvider[] providers)
      {
        if (Core.IsInitialized())
        {
          UserOptions userOpts = new UserOptions();
          foreach (ServiceProvider provider in providers)
          {
            userOpts.AddServiceProvider(provider);
          }
          return new Request<Models.LinkedAccountList>(CAPI.ovr_User_GetLinkedAccounts((IntPtr)userOpts));
        }

        return null;
      }
    }
  }
}
