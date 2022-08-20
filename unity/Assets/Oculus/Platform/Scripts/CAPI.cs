// This file was @generated with LibOVRPlatform/codegen/main. Do not modify it!

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable 414
namespace Oculus.Platform
{
  public class CAPI
  {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
  #if UNITY_64 || UNITY_EDITOR_64
    public const string DLL_NAME = "LibOVRPlatform64_1";
  #else
    public const string DLL_NAME = "LibOVRPlatform32_1";
  #endif
#elif UNITY_EDITOR || UNITY_EDITOR_64
    public const string DLL_NAME = "ovrplatform";
#elif UNITY_ANDROID && OVR_STANDALONE_PLATFORM
    public const string DLL_NAME = "ovrplatform_standalone";
#else
    public const string DLL_NAME = "ovrplatformloader";
#endif

    private static UTF8Encoding nativeStringEncoding = new UTF8Encoding(false);

    [StructLayout(LayoutKind.Sequential)]
    public struct ovrKeyValuePair {
      public ovrKeyValuePair(string key, string value) {
        key_ = key;
        valueType_ = KeyValuePairType.String;
        stringValue_ = value;

        intValue_ = 0;
        doubleValue_ = 0.0;
      }

      public ovrKeyValuePair(string key, int value) {
        key_ = key;
        valueType_ = KeyValuePairType.Int;
        intValue_ = value;

        stringValue_ = null;
        doubleValue_ = 0.0;
      }

      public ovrKeyValuePair(string key, double value) {
        key_ = key;
        valueType_ = KeyValuePairType.Double;
        doubleValue_ = value;

        stringValue_ = null;
        intValue_ = 0;
      }

      public string key_;
      KeyValuePairType valueType_;

      public string stringValue_;
      public int intValue_;
      public double doubleValue_;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct ovrNetSyncVec3 {
      public float x;
      public float y;
      public float z;
    }

    public static IntPtr ArrayOfStructsToIntPtr(Array ar)
    {
      int totalSize = 0;
      for(int i=0; i<ar.Length; i++) {
        totalSize += Marshal.SizeOf(ar.GetValue(i));
      }

      IntPtr childrenPtr = Marshal.AllocHGlobal(totalSize);
      IntPtr curr = childrenPtr;
      for(int i=0; i<ar.Length; i++) {
        Marshal.StructureToPtr(ar.GetValue(i), curr, false);
        curr = (IntPtr)((long)curr + Marshal.SizeOf(ar.GetValue(i)));
      }
      return childrenPtr;
    }

    public static CAPI.ovrKeyValuePair[] DictionaryToOVRKeyValuePairs(Dictionary<string, object> dict)
    {
      if(dict == null || dict.Count == 0)
      {
        return null;
      }

      var nativeCustomData = new CAPI.ovrKeyValuePair[dict.Count];

      int i = 0;
      foreach(var item in dict)
      {
        if(item.Value.GetType() == typeof(int))
        {
          nativeCustomData[i] = new CAPI.ovrKeyValuePair(item.Key, (int)item.Value);
        }
        else if(item.Value.GetType() == typeof(string))
        {
          nativeCustomData[i] = new CAPI.ovrKeyValuePair(item.Key, (string)item.Value);
        }
        else if(item.Value.GetType() == typeof(double))
        {
          nativeCustomData[i] = new CAPI.ovrKeyValuePair(item.Key, (double)item.Value);
        }
        else
        {
          throw new Exception("Only int, double or string are allowed types in CustomQuery.data");
        }
        i++;
      }
      return nativeCustomData;
    }

    public static byte[] IntPtrToByteArray(IntPtr data, ulong size)
    {
      byte[] outArray = new byte[size];
      Marshal.Copy(data, outArray, 0, (int)size);
      return outArray;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ovrMatchmakingCriterion {
      public ovrMatchmakingCriterion(string key, MatchmakingCriterionImportance importance)
      {
        key_ = key;
        importance_ = importance;

        parameterArray = IntPtr.Zero;
        parameterArrayCount = 0;
      }

      public string key_;
      public MatchmakingCriterionImportance importance_;

      public IntPtr parameterArray;
      public uint parameterArrayCount;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct ovrMatchmakingCustomQueryData {
      public IntPtr dataArray;
      public uint dataArrayCount;

      public IntPtr criterionArray;
      public uint criterionArrayCount;
    };

    public static Dictionary<string, string> DataStoreFromNative(IntPtr pointer) {
      var d = new Dictionary<string, string>();
      var size = (int)CAPI.ovr_DataStore_GetNumKeys(pointer);
      for (var i = 0; i < size; i++) {
        string key = CAPI.ovr_DataStore_GetKey(pointer, i);
        d[key] = CAPI.ovr_DataStore_GetValue(pointer, key);
      }
      return d;
    }

    public static string StringFromNative(IntPtr pointer) {
      if (pointer == IntPtr.Zero) {
        return null;
      }
      var l = GetNativeStringLengthNotIncludingNullTerminator(pointer);
      var data = new byte[l];
      Marshal.Copy(pointer, data, 0, l);
      return nativeStringEncoding.GetString(data);
    }

    public static int GetNativeStringLengthNotIncludingNullTerminator(IntPtr pointer) {
      var l = 0;
      while (true) {
        if (Marshal.ReadByte(pointer, l) == 0) {
          return l;
        }
        l++;
      }
    }

    public static DateTime DateTimeFromNative(ulong seconds_since_the_one_true_epoch) {
      var dt = new DateTime(1970, 1, 1, 0, 0, 0, 0);
      return dt.AddSeconds(seconds_since_the_one_true_epoch).ToLocalTime();
    }

    public static ulong DateTimeToNative(DateTime dt) {
      var universal = (dt.Kind != DateTimeKind.Utc) ? dt.ToUniversalTime() : dt;
      var epochStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
      return (ulong) (universal - epochStart).TotalSeconds;
    }

    public static byte[] BlobFromNative(uint size, IntPtr pointer) {
      var a = new byte[(int)size];
      for (int i = 0; i < (int)size; i++) {
        a[i] = Marshal.ReadByte(pointer, i);
      }
      return a;
    }

    public static byte[] FiledataFromNative(uint size, IntPtr pointer) {
      var data = new byte[(int)size];
      Marshal.Copy(pointer, data, 0, (int)size);
      return data;
    }

    public static IntPtr StringToNative(string s) {
      if (s == null) {
        throw new Exception("StringFromNative: null argument");
      }
      var l = nativeStringEncoding.GetByteCount(s);
      var data = new byte[l + 1];
      nativeStringEncoding.GetBytes(s, 0, s.Length, data, 0);
      var pointer = Marshal.AllocCoTaskMem(l + 1);
      Marshal.Copy(data, 0, pointer, l + 1);
      return pointer;
    }

    // Initialization
    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_UnityInitWrapper(string appId);

    // Initializes just the global variables to use the Unity api without calling the init logic
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ovr_UnityInitGlobals(IntPtr loggingCB);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_UnityInitWrapperAsynchronous(string appId);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_UnityInitWrapperStandalone(string accessToken, IntPtr loggingCB);

    [StructLayout(LayoutKind.Sequential)]
    public struct OculusInitParams
    {
      public int sType;
      public string email;
      public string password;
      public UInt64 appId;
      public string uriPrefixOverride;
    }

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong ovr_Platform_InitializeStandaloneOculus(ref OculusInitParams init);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong ovr_PlatformInitializeWithAccessToken(UInt64 appId, string accessToken);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_UnityInitWrapperWindows(string appId, IntPtr loggingCB);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_UnityInitWrapperWindowsAsynchronous(string appId, IntPtr loggingCB);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_SetDeveloperAccessToken(string accessToken);

    public static string ovr_GetLoggedInUserLocale() {
      var result = StringFromNative(ovr_GetLoggedInUserLocale_Native());
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_GetLoggedInUserLocale")]
    private static extern IntPtr ovr_GetLoggedInUserLocale_Native();


    // Message queue access

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_PopMessage();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_FreeMessage(IntPtr message);


    // VOIP

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Voip_CreateEncoder();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_Voip_DestroyEncoder(IntPtr encoder);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Voip_CreateDecoder();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_Voip_DestroyDecoder(IntPtr decoder);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_VoipDecoder_Decode(IntPtr obj, byte[] compressedData, ulong compressedSize);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Microphone_Create();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_Microphone_Destroy(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_Voip_SetSystemVoipPassthrough(bool passthrough);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_Voip_SetSystemVoipMicrophoneMuted(VoipMuteState muted);

    // Misc

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_UnityResetTestPlatform();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_HTTP_GetWithMessageType(string url, int messageType);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_CrashApplication();

    public const int VoipFilterBufferSize = 480;
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void FilterCallback([MarshalAs(UnmanagedType.LPArray, SizeConst = VoipFilterBufferSize), In, Out] short[] pcmData, UIntPtr pcmDataLength, int frequency, int numChannels);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ovr_Voip_SetMicrophoneFilterCallback(FilterCallback cb);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ovr_Voip_SetMicrophoneFilterCallbackWithFixedSizeBuffer(FilterCallback cb, UIntPtr bufferSizeElements);


    // Logging

    public static void LogNewEvent(string eventName, Dictionary<string, string> values) {
      var eventNameNative = StringToNative(eventName);

      var count = values == null ? 0 : values.Count;

      IntPtr[] valuesNative = new IntPtr[count * 2];

      if (count > 0) {
        int i = 0;
        foreach(var item in values) {
          valuesNative[i * 2 + 0] = StringToNative(item.Key);
          valuesNative[i * 2 + 1] = StringToNative(item.Value);
          i++;
        }
      }

      ovr_Log_NewEvent(eventNameNative, valuesNative, (UIntPtr)count);

      Marshal.FreeCoTaskMem(eventNameNative);
      foreach (var nativeItem in valuesNative) {
        Marshal.FreeCoTaskMem(nativeItem);
      }
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_Log_NewEvent(IntPtr eventName, IntPtr[] values, UIntPtr length);


    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_ApplicationLifecycle_GetLaunchDetails();

    public static void ovr_ApplicationLifecycle_LogDeeplinkResult(string trackingID, LaunchResult result) {
      IntPtr trackingID_native = StringToNative(trackingID);
      ovr_ApplicationLifecycle_LogDeeplinkResult_Native(trackingID_native, result);
      Marshal.FreeCoTaskMem(trackingID_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_ApplicationLifecycle_LogDeeplinkResult")]
    private static extern void ovr_ApplicationLifecycle_LogDeeplinkResult_Native(IntPtr trackingID, LaunchResult result);

    public static ulong ovr_HTTP_StartTransfer(string url, ovrKeyValuePair[] headers) {
      IntPtr url_native = StringToNative(url);
      UIntPtr headers_length = (UIntPtr)headers.Length;
      var result = (ovr_HTTP_StartTransfer_Native(url_native, headers, headers_length));
      Marshal.FreeCoTaskMem(url_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_HTTP_StartTransfer")]
    private static extern ulong ovr_HTTP_StartTransfer_Native(IntPtr url, ovrKeyValuePair[] headers, UIntPtr numItems);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_HTTP_Write(ulong transferId, byte[] bytes, UIntPtr length);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_HTTP_WriteEOM(ulong transferId);

    public static string ovr_Message_GetStringForJavascript(IntPtr message) {
      var result = StringFromNative(ovr_Message_GetStringForJavascript_Native(message));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Message_GetStringForJavascript")]
    private static extern IntPtr ovr_Message_GetStringForJavascript_Native(IntPtr message);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_NetSync_GetAmbisonicFloatPCM(long connection_id, float[] outputBuffer, UIntPtr outputBufferNumElements);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_NetSync_GetAmbisonicInt16PCM(long connection_id, Int16[] outputBuffer, UIntPtr outputBufferNumElements);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_NetSync_GetAmbisonicInterleavedFloatPCM(long connection_id, float[] outputBuffer, UIntPtr outputBufferNumElements);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_NetSync_GetAmbisonicInterleavedInt16PCM(long connection_id, Int16[] outputBuffer, UIntPtr outputBufferNumElements);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_NetSync_GetListenerPosition(long connection_id, UInt64 sessionId, ref ovrNetSyncVec3 position);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_NetSync_GetMonostreamFloatPCM(long connection_id, UInt64 sessionId, float[] outputBuffer, UIntPtr outputBufferNumElements);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_NetSync_GetMonostreamInt16PCM(long connection_id, UInt64 session_id, Int16[] outputBuffer, UIntPtr outputBufferNumElements);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_NetSync_GetPcmBufferMaxSamples();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_NetSync_GetVoipAmplitude(long connection_id, UInt64 sessionId, ref float amplitude);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_NetSync_SetListenerPosition(long connection_id, ref ovrNetSyncVec3 position);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_Net_Accept(UInt64 peerID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_Net_AcceptForCurrentRoom();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_Net_Close(UInt64 peerID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_Net_CloseForCurrentRoom();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_Net_Connect(UInt64 peerID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_Net_IsConnected(UInt64 peerID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Net_Ping(UInt64 peerID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Net_ReadPacket();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_Net_SendPacket(UInt64 userID, UIntPtr length, byte[] bytes, SendPolicy policy);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_Net_SendPacketToCurrentRoom(UIntPtr length, byte[] bytes, SendPolicy policy);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern int ovr_Party_PluginGetSharedMemHandle();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern VoipMuteState ovr_Party_PluginGetVoipMicrophoneMuted();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_Party_PluginGetVoipPassthrough();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern SystemVoipStatus ovr_Party_PluginGetVoipStatus();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_Voip_Accept(UInt64 userID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern VoipDtxState ovr_Voip_GetIsConnectionUsingDtx(UInt64 peerID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern VoipBitrate ovr_Voip_GetLocalBitrate(UInt64 peerID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_Voip_GetOutputBufferMaxSize();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_Voip_GetPCM(UInt64 senderID, Int16[] outputBuffer, UIntPtr outputBufferNumElements);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_Voip_GetPCMFloat(UInt64 senderID, float[] outputBuffer, UIntPtr outputBufferNumElements);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_Voip_GetPCMSize(UInt64 senderID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_Voip_GetPCMWithTimestamp(UInt64 senderID, Int16[] outputBuffer, UIntPtr outputBufferNumElements, UInt32[] timestamp);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_Voip_GetPCMWithTimestampFloat(UInt64 senderID, float[] outputBuffer, UIntPtr outputBufferNumElements, UInt32[] timestamp);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern VoipBitrate ovr_Voip_GetRemoteBitrate(UInt64 peerID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt32 ovr_Voip_GetSyncTimestamp(UInt64 userID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern long ovr_Voip_GetSyncTimestampDifference(UInt32 lhs, UInt32 rhs);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern VoipMuteState ovr_Voip_GetSystemVoipMicrophoneMuted();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern SystemVoipStatus ovr_Voip_GetSystemVoipStatus();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_Voip_SetMicrophoneMuted(VoipMuteState state);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_Voip_SetNewConnectionOptions(IntPtr voipOptions);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_Voip_SetOutputSampleRate(VoipSampleRate rate);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_Voip_Start(UInt64 userID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_Voip_Stop(UInt64 userID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_AbuseReport_LaunchAdvancedReportFlow(UInt64 content_id, IntPtr abuse_report_options);

    public static ulong ovr_Achievements_AddCount(string name, ulong count) {
      IntPtr name_native = StringToNative(name);
      var result = (ovr_Achievements_AddCount_Native(name_native, count));
      Marshal.FreeCoTaskMem(name_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Achievements_AddCount")]
    private static extern ulong ovr_Achievements_AddCount_Native(IntPtr name, ulong count);

    public static ulong ovr_Achievements_AddFields(string name, string fields) {
      IntPtr name_native = StringToNative(name);
      IntPtr fields_native = StringToNative(fields);
      var result = (ovr_Achievements_AddFields_Native(name_native, fields_native));
      Marshal.FreeCoTaskMem(name_native);
      Marshal.FreeCoTaskMem(fields_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Achievements_AddFields")]
    private static extern ulong ovr_Achievements_AddFields_Native(IntPtr name, IntPtr fields);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Achievements_GetAllDefinitions();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Achievements_GetAllProgress();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Achievements_GetDefinitionsByName(string[] names, int count);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Achievements_GetProgressByName(string[] names, int count);

    public static ulong ovr_Achievements_Unlock(string name) {
      IntPtr name_native = StringToNative(name);
      var result = (ovr_Achievements_Unlock_Native(name_native));
      Marshal.FreeCoTaskMem(name_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Achievements_Unlock")]
    private static extern ulong ovr_Achievements_Unlock_Native(IntPtr name);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Application_ExecuteCoordinatedLaunch(ulong appID, ulong roomID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Application_GetInstalledApplications();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Application_GetVersion();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Application_LaunchOtherApp(UInt64 appID, IntPtr deeplink_options);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_ApplicationLifecycle_GetRegisteredPIDs();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_ApplicationLifecycle_GetSessionKey();

    public static ulong ovr_ApplicationLifecycle_RegisterSessionKey(string sessionKey) {
      IntPtr sessionKey_native = StringToNative(sessionKey);
      var result = (ovr_ApplicationLifecycle_RegisterSessionKey_Native(sessionKey_native));
      Marshal.FreeCoTaskMem(sessionKey_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_ApplicationLifecycle_RegisterSessionKey")]
    private static extern ulong ovr_ApplicationLifecycle_RegisterSessionKey_Native(IntPtr sessionKey);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_AssetFile_Delete(UInt64 assetFileID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_AssetFile_DeleteById(UInt64 assetFileID);

    public static ulong ovr_AssetFile_DeleteByName(string assetFileName) {
      IntPtr assetFileName_native = StringToNative(assetFileName);
      var result = (ovr_AssetFile_DeleteByName_Native(assetFileName_native));
      Marshal.FreeCoTaskMem(assetFileName_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_AssetFile_DeleteByName")]
    private static extern ulong ovr_AssetFile_DeleteByName_Native(IntPtr assetFileName);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_AssetFile_Download(UInt64 assetFileID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_AssetFile_DownloadById(UInt64 assetFileID);

    public static ulong ovr_AssetFile_DownloadByName(string assetFileName) {
      IntPtr assetFileName_native = StringToNative(assetFileName);
      var result = (ovr_AssetFile_DownloadByName_Native(assetFileName_native));
      Marshal.FreeCoTaskMem(assetFileName_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_AssetFile_DownloadByName")]
    private static extern ulong ovr_AssetFile_DownloadByName_Native(IntPtr assetFileName);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_AssetFile_DownloadCancel(UInt64 assetFileID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_AssetFile_DownloadCancelById(UInt64 assetFileID);

    public static ulong ovr_AssetFile_DownloadCancelByName(string assetFileName) {
      IntPtr assetFileName_native = StringToNative(assetFileName);
      var result = (ovr_AssetFile_DownloadCancelByName_Native(assetFileName_native));
      Marshal.FreeCoTaskMem(assetFileName_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_AssetFile_DownloadCancelByName")]
    private static extern ulong ovr_AssetFile_DownloadCancelByName_Native(IntPtr assetFileName);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_AssetFile_GetList();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_AssetFile_Status(UInt64 assetFileID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_AssetFile_StatusById(UInt64 assetFileID);

    public static ulong ovr_AssetFile_StatusByName(string assetFileName) {
      IntPtr assetFileName_native = StringToNative(assetFileName);
      var result = (ovr_AssetFile_StatusByName_Native(assetFileName_native));
      Marshal.FreeCoTaskMem(assetFileName_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_AssetFile_StatusByName")]
    private static extern ulong ovr_AssetFile_StatusByName_Native(IntPtr assetFileName);

    public static ulong ovr_Avatar_UpdateMetaData(string avatarMetaData, string imageFilePath) {
      IntPtr avatarMetaData_native = StringToNative(avatarMetaData);
      IntPtr imageFilePath_native = StringToNative(imageFilePath);
      var result = (ovr_Avatar_UpdateMetaData_Native(avatarMetaData_native, imageFilePath_native));
      Marshal.FreeCoTaskMem(avatarMetaData_native);
      Marshal.FreeCoTaskMem(imageFilePath_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Avatar_UpdateMetaData")]
    private static extern ulong ovr_Avatar_UpdateMetaData_Native(IntPtr avatarMetaData, IntPtr imageFilePath);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Cal_FinalizeApplication(UInt64 groupingObject, UInt64[] userIDs, int numUserIDs, UInt64 finalized_application_ID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Cal_GetSuggestedApplications(UInt64 groupingObject, UInt64[] userIDs, int numUserIDs);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Cal_ProposeApplication(UInt64 groupingObject, UInt64[] userIDs, int numUserIDs, UInt64 proposed_application_ID);

    public static ulong ovr_Challenges_Create(string leaderboardName, IntPtr challengeOptions) {
      IntPtr leaderboardName_native = StringToNative(leaderboardName);
      var result = (ovr_Challenges_Create_Native(leaderboardName_native, challengeOptions));
      Marshal.FreeCoTaskMem(leaderboardName_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Challenges_Create")]
    private static extern ulong ovr_Challenges_Create_Native(IntPtr leaderboardName, IntPtr challengeOptions);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Challenges_DeclineInvite(UInt64 challengeID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Challenges_Delete(UInt64 challengeID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Challenges_Get(UInt64 challengeID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Challenges_GetEntries(UInt64 challengeID, int limit, LeaderboardFilterType filter, LeaderboardStartAt startAt);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Challenges_GetEntriesAfterRank(UInt64 challengeID, int limit, ulong afterRank);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Challenges_GetEntriesByIds(UInt64 challengeID, int limit, LeaderboardStartAt startAt, UInt64[] userIDs, uint userIDLength);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Challenges_GetList(IntPtr challengeOptions, int limit);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Challenges_GetNextChallenges(IntPtr handle);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Challenges_GetNextEntries(IntPtr handle);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Challenges_GetPreviousChallenges(IntPtr handle);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Challenges_GetPreviousEntries(IntPtr handle);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Challenges_Join(UInt64 challengeID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Challenges_Leave(UInt64 challengeID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Challenges_UpdateInfo(UInt64 challengeID, IntPtr challengeOptions);

    public static ulong ovr_CloudStorage_Delete(string bucket, string key) {
      IntPtr bucket_native = StringToNative(bucket);
      IntPtr key_native = StringToNative(key);
      var result = (ovr_CloudStorage_Delete_Native(bucket_native, key_native));
      Marshal.FreeCoTaskMem(bucket_native);
      Marshal.FreeCoTaskMem(key_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_CloudStorage_Delete")]
    private static extern ulong ovr_CloudStorage_Delete_Native(IntPtr bucket, IntPtr key);

    public static ulong ovr_CloudStorage_Load(string bucket, string key) {
      IntPtr bucket_native = StringToNative(bucket);
      IntPtr key_native = StringToNative(key);
      var result = (ovr_CloudStorage_Load_Native(bucket_native, key_native));
      Marshal.FreeCoTaskMem(bucket_native);
      Marshal.FreeCoTaskMem(key_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_CloudStorage_Load")]
    private static extern ulong ovr_CloudStorage_Load_Native(IntPtr bucket, IntPtr key);

    public static ulong ovr_CloudStorage_LoadBucketMetadata(string bucket) {
      IntPtr bucket_native = StringToNative(bucket);
      var result = (ovr_CloudStorage_LoadBucketMetadata_Native(bucket_native));
      Marshal.FreeCoTaskMem(bucket_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_CloudStorage_LoadBucketMetadata")]
    private static extern ulong ovr_CloudStorage_LoadBucketMetadata_Native(IntPtr bucket);

    public static ulong ovr_CloudStorage_LoadConflictMetadata(string bucket, string key) {
      IntPtr bucket_native = StringToNative(bucket);
      IntPtr key_native = StringToNative(key);
      var result = (ovr_CloudStorage_LoadConflictMetadata_Native(bucket_native, key_native));
      Marshal.FreeCoTaskMem(bucket_native);
      Marshal.FreeCoTaskMem(key_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_CloudStorage_LoadConflictMetadata")]
    private static extern ulong ovr_CloudStorage_LoadConflictMetadata_Native(IntPtr bucket, IntPtr key);

    public static ulong ovr_CloudStorage_LoadHandle(string handle) {
      IntPtr handle_native = StringToNative(handle);
      var result = (ovr_CloudStorage_LoadHandle_Native(handle_native));
      Marshal.FreeCoTaskMem(handle_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_CloudStorage_LoadHandle")]
    private static extern ulong ovr_CloudStorage_LoadHandle_Native(IntPtr handle);

    public static ulong ovr_CloudStorage_LoadMetadata(string bucket, string key) {
      IntPtr bucket_native = StringToNative(bucket);
      IntPtr key_native = StringToNative(key);
      var result = (ovr_CloudStorage_LoadMetadata_Native(bucket_native, key_native));
      Marshal.FreeCoTaskMem(bucket_native);
      Marshal.FreeCoTaskMem(key_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_CloudStorage_LoadMetadata")]
    private static extern ulong ovr_CloudStorage_LoadMetadata_Native(IntPtr bucket, IntPtr key);

    public static ulong ovr_CloudStorage_ResolveKeepLocal(string bucket, string key, string remoteHandle) {
      IntPtr bucket_native = StringToNative(bucket);
      IntPtr key_native = StringToNative(key);
      IntPtr remoteHandle_native = StringToNative(remoteHandle);
      var result = (ovr_CloudStorage_ResolveKeepLocal_Native(bucket_native, key_native, remoteHandle_native));
      Marshal.FreeCoTaskMem(bucket_native);
      Marshal.FreeCoTaskMem(key_native);
      Marshal.FreeCoTaskMem(remoteHandle_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_CloudStorage_ResolveKeepLocal")]
    private static extern ulong ovr_CloudStorage_ResolveKeepLocal_Native(IntPtr bucket, IntPtr key, IntPtr remoteHandle);

    public static ulong ovr_CloudStorage_ResolveKeepRemote(string bucket, string key, string remoteHandle) {
      IntPtr bucket_native = StringToNative(bucket);
      IntPtr key_native = StringToNative(key);
      IntPtr remoteHandle_native = StringToNative(remoteHandle);
      var result = (ovr_CloudStorage_ResolveKeepRemote_Native(bucket_native, key_native, remoteHandle_native));
      Marshal.FreeCoTaskMem(bucket_native);
      Marshal.FreeCoTaskMem(key_native);
      Marshal.FreeCoTaskMem(remoteHandle_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_CloudStorage_ResolveKeepRemote")]
    private static extern ulong ovr_CloudStorage_ResolveKeepRemote_Native(IntPtr bucket, IntPtr key, IntPtr remoteHandle);

    public static ulong ovr_CloudStorage_Save(string bucket, string key, byte[] data, uint dataSize, long counter, string extraData) {
      IntPtr bucket_native = StringToNative(bucket);
      IntPtr key_native = StringToNative(key);
      IntPtr extraData_native = StringToNative(extraData);
      var result = (ovr_CloudStorage_Save_Native(bucket_native, key_native, data, dataSize, counter, extraData_native));
      Marshal.FreeCoTaskMem(bucket_native);
      Marshal.FreeCoTaskMem(key_native);
      Marshal.FreeCoTaskMem(extraData_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_CloudStorage_Save")]
    private static extern ulong ovr_CloudStorage_Save_Native(IntPtr bucket, IntPtr key, byte[] data, uint dataSize, long counter, IntPtr extraData);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_CloudStorage2_GetUserDirectoryPath();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Colocation_GetCurrentMapUuid();

    public static ulong ovr_Colocation_RequestMap(string uuid) {
      IntPtr uuid_native = StringToNative(uuid);
      var result = (ovr_Colocation_RequestMap_Native(uuid_native));
      Marshal.FreeCoTaskMem(uuid_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Colocation_RequestMap")]
    private static extern ulong ovr_Colocation_RequestMap_Native(IntPtr uuid);

    public static ulong ovr_Colocation_ShareMap(string uuid) {
      IntPtr uuid_native = StringToNative(uuid);
      var result = (ovr_Colocation_ShareMap_Native(uuid_native));
      Marshal.FreeCoTaskMem(uuid_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Colocation_ShareMap")]
    private static extern ulong ovr_Colocation_ShareMap_Native(IntPtr uuid);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Entitlement_GetIsViewerEntitled();

    public static ulong ovr_GraphAPI_Get(string url) {
      IntPtr url_native = StringToNative(url);
      var result = (ovr_GraphAPI_Get_Native(url_native));
      Marshal.FreeCoTaskMem(url_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_GraphAPI_Get")]
    private static extern ulong ovr_GraphAPI_Get_Native(IntPtr url);

    public static ulong ovr_GraphAPI_Post(string url) {
      IntPtr url_native = StringToNative(url);
      var result = (ovr_GraphAPI_Post_Native(url_native));
      Marshal.FreeCoTaskMem(url_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_GraphAPI_Post")]
    private static extern ulong ovr_GraphAPI_Post_Native(IntPtr url);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_GroupPresence_Clear();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_GroupPresence_GetInvitableUsers(IntPtr options);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_GroupPresence_GetSentInvites();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_GroupPresence_LaunchInvitePanel(IntPtr options);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_GroupPresence_LaunchMultiplayerErrorDialog(IntPtr options);

    public static ulong ovr_GroupPresence_LaunchRejoinDialog(string lobby_session_id, string match_session_id, string destination_api_name) {
      IntPtr lobby_session_id_native = StringToNative(lobby_session_id);
      IntPtr match_session_id_native = StringToNative(match_session_id);
      IntPtr destination_api_name_native = StringToNative(destination_api_name);
      var result = (ovr_GroupPresence_LaunchRejoinDialog_Native(lobby_session_id_native, match_session_id_native, destination_api_name_native));
      Marshal.FreeCoTaskMem(lobby_session_id_native);
      Marshal.FreeCoTaskMem(match_session_id_native);
      Marshal.FreeCoTaskMem(destination_api_name_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_GroupPresence_LaunchRejoinDialog")]
    private static extern ulong ovr_GroupPresence_LaunchRejoinDialog_Native(IntPtr lobby_session_id, IntPtr match_session_id, IntPtr destination_api_name);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_GroupPresence_LaunchRosterPanel(IntPtr options);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_GroupPresence_SendInvites(UInt64[] userIDs, uint userIDLength);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_GroupPresence_Set(IntPtr groupPresenceOptions);

    public static ulong ovr_GroupPresence_SetDestination(string api_name) {
      IntPtr api_name_native = StringToNative(api_name);
      var result = (ovr_GroupPresence_SetDestination_Native(api_name_native));
      Marshal.FreeCoTaskMem(api_name_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_GroupPresence_SetDestination")]
    private static extern ulong ovr_GroupPresence_SetDestination_Native(IntPtr api_name);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_GroupPresence_SetIsJoinable(bool is_joinable);

    public static ulong ovr_GroupPresence_SetLobbySession(string id) {
      IntPtr id_native = StringToNative(id);
      var result = (ovr_GroupPresence_SetLobbySession_Native(id_native));
      Marshal.FreeCoTaskMem(id_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_GroupPresence_SetLobbySession")]
    private static extern ulong ovr_GroupPresence_SetLobbySession_Native(IntPtr id);

    public static ulong ovr_GroupPresence_SetMatchSession(string id) {
      IntPtr id_native = StringToNative(id);
      var result = (ovr_GroupPresence_SetMatchSession_Native(id_native));
      Marshal.FreeCoTaskMem(id_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_GroupPresence_SetMatchSession")]
    private static extern ulong ovr_GroupPresence_SetMatchSession_Native(IntPtr id);

    public static ulong ovr_HTTP_Get(string url) {
      IntPtr url_native = StringToNative(url);
      var result = (ovr_HTTP_Get_Native(url_native));
      Marshal.FreeCoTaskMem(url_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_HTTP_Get")]
    private static extern ulong ovr_HTTP_Get_Native(IntPtr url);

    public static ulong ovr_HTTP_GetToFile(string url, string diskFile) {
      IntPtr url_native = StringToNative(url);
      IntPtr diskFile_native = StringToNative(diskFile);
      var result = (ovr_HTTP_GetToFile_Native(url_native, diskFile_native));
      Marshal.FreeCoTaskMem(url_native);
      Marshal.FreeCoTaskMem(diskFile_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_HTTP_GetToFile")]
    private static extern ulong ovr_HTTP_GetToFile_Native(IntPtr url, IntPtr diskFile);

    public static ulong ovr_HTTP_MultiPartPost(string url, string filepath_param_name, string filepath, string access_token, ovrKeyValuePair[] post_params) {
      IntPtr url_native = StringToNative(url);
      IntPtr filepath_param_name_native = StringToNative(filepath_param_name);
      IntPtr filepath_native = StringToNative(filepath);
      IntPtr access_token_native = StringToNative(access_token);
      UIntPtr post_params_length = (UIntPtr)post_params.Length;
      var result = (ovr_HTTP_MultiPartPost_Native(url_native, filepath_param_name_native, filepath_native, access_token_native, post_params, post_params_length));
      Marshal.FreeCoTaskMem(url_native);
      Marshal.FreeCoTaskMem(filepath_param_name_native);
      Marshal.FreeCoTaskMem(filepath_native);
      Marshal.FreeCoTaskMem(access_token_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_HTTP_MultiPartPost")]
    private static extern ulong ovr_HTTP_MultiPartPost_Native(IntPtr url, IntPtr filepath_param_name, IntPtr filepath, IntPtr access_token, ovrKeyValuePair[] post_params, UIntPtr numItems);

    public static ulong ovr_HTTP_Post(string url) {
      IntPtr url_native = StringToNative(url);
      var result = (ovr_HTTP_Post_Native(url_native));
      Marshal.FreeCoTaskMem(url_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_HTTP_Post")]
    private static extern ulong ovr_HTTP_Post_Native(IntPtr url);

    public static ulong ovr_IAP_ConsumePurchase(string sku) {
      IntPtr sku_native = StringToNative(sku);
      var result = (ovr_IAP_ConsumePurchase_Native(sku_native));
      Marshal.FreeCoTaskMem(sku_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_IAP_ConsumePurchase")]
    private static extern ulong ovr_IAP_ConsumePurchase_Native(IntPtr sku);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_IAP_GetProductsBySKU(string[] skus, int count);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_IAP_GetViewerPurchases();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_IAP_GetViewerPurchasesDurableCache();

    public static ulong ovr_IAP_LaunchCheckoutFlow(string sku) {
      IntPtr sku_native = StringToNative(sku);
      var result = (ovr_IAP_LaunchCheckoutFlow_Native(sku_native));
      Marshal.FreeCoTaskMem(sku_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_IAP_LaunchCheckoutFlow")]
    private static extern ulong ovr_IAP_LaunchCheckoutFlow_Native(IntPtr sku);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_LanguagePack_GetCurrent();

    public static ulong ovr_LanguagePack_SetCurrent(string tag) {
      IntPtr tag_native = StringToNative(tag);
      var result = (ovr_LanguagePack_SetCurrent_Native(tag_native));
      Marshal.FreeCoTaskMem(tag_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_LanguagePack_SetCurrent")]
    private static extern ulong ovr_LanguagePack_SetCurrent_Native(IntPtr tag);

    public static ulong ovr_Leaderboard_Get(string leaderboardName) {
      IntPtr leaderboardName_native = StringToNative(leaderboardName);
      var result = (ovr_Leaderboard_Get_Native(leaderboardName_native));
      Marshal.FreeCoTaskMem(leaderboardName_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Leaderboard_Get")]
    private static extern ulong ovr_Leaderboard_Get_Native(IntPtr leaderboardName);

    public static ulong ovr_Leaderboard_GetEntries(string leaderboardName, int limit, LeaderboardFilterType filter, LeaderboardStartAt startAt) {
      IntPtr leaderboardName_native = StringToNative(leaderboardName);
      var result = (ovr_Leaderboard_GetEntries_Native(leaderboardName_native, limit, filter, startAt));
      Marshal.FreeCoTaskMem(leaderboardName_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Leaderboard_GetEntries")]
    private static extern ulong ovr_Leaderboard_GetEntries_Native(IntPtr leaderboardName, int limit, LeaderboardFilterType filter, LeaderboardStartAt startAt);

    public static ulong ovr_Leaderboard_GetEntriesAfterRank(string leaderboardName, int limit, ulong afterRank) {
      IntPtr leaderboardName_native = StringToNative(leaderboardName);
      var result = (ovr_Leaderboard_GetEntriesAfterRank_Native(leaderboardName_native, limit, afterRank));
      Marshal.FreeCoTaskMem(leaderboardName_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Leaderboard_GetEntriesAfterRank")]
    private static extern ulong ovr_Leaderboard_GetEntriesAfterRank_Native(IntPtr leaderboardName, int limit, ulong afterRank);

    public static ulong ovr_Leaderboard_GetEntriesByIds(string leaderboardName, int limit, LeaderboardStartAt startAt, UInt64[] userIDs, uint userIDLength) {
      IntPtr leaderboardName_native = StringToNative(leaderboardName);
      var result = (ovr_Leaderboard_GetEntriesByIds_Native(leaderboardName_native, limit, startAt, userIDs, userIDLength));
      Marshal.FreeCoTaskMem(leaderboardName_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Leaderboard_GetEntriesByIds")]
    private static extern ulong ovr_Leaderboard_GetEntriesByIds_Native(IntPtr leaderboardName, int limit, LeaderboardStartAt startAt, UInt64[] userIDs, uint userIDLength);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Leaderboard_GetNextEntries(IntPtr handle);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Leaderboard_GetPreviousEntries(IntPtr handle);

    public static ulong ovr_Leaderboard_WriteEntry(string leaderboardName, long score, byte[] extraData, uint extraDataLength, bool forceUpdate) {
      IntPtr leaderboardName_native = StringToNative(leaderboardName);
      var result = (ovr_Leaderboard_WriteEntry_Native(leaderboardName_native, score, extraData, extraDataLength, forceUpdate));
      Marshal.FreeCoTaskMem(leaderboardName_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Leaderboard_WriteEntry")]
    private static extern ulong ovr_Leaderboard_WriteEntry_Native(IntPtr leaderboardName, long score, byte[] extraData, uint extraDataLength, bool forceUpdate);

    public static ulong ovr_Leaderboard_WriteEntryWithSupplementaryMetric(string leaderboardName, long score, long supplementaryMetric, byte[] extraData, uint extraDataLength, bool forceUpdate) {
      IntPtr leaderboardName_native = StringToNative(leaderboardName);
      var result = (ovr_Leaderboard_WriteEntryWithSupplementaryMetric_Native(leaderboardName_native, score, supplementaryMetric, extraData, extraDataLength, forceUpdate));
      Marshal.FreeCoTaskMem(leaderboardName_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Leaderboard_WriteEntryWithSupplementaryMetric")]
    private static extern ulong ovr_Leaderboard_WriteEntryWithSupplementaryMetric_Native(IntPtr leaderboardName, long score, long supplementaryMetric, byte[] extraData, uint extraDataLength, bool forceUpdate);

    public static ulong ovr_Livestreaming_IsAllowedForApplication(string packageName) {
      IntPtr packageName_native = StringToNative(packageName);
      var result = (ovr_Livestreaming_IsAllowedForApplication_Native(packageName_native));
      Marshal.FreeCoTaskMem(packageName_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Livestreaming_IsAllowedForApplication")]
    private static extern ulong ovr_Livestreaming_IsAllowedForApplication_Native(IntPtr packageName);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Livestreaming_StartPartyStream();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Livestreaming_StartStream(LivestreamingAudience audience, LivestreamingMicrophoneStatus micStatus);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Livestreaming_StopPartyStream();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Livestreaming_StopStream();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Livestreaming_UpdateMicStatus(LivestreamingMicrophoneStatus micStatus);

    public static ulong ovr_Matchmaking_Browse(string pool, IntPtr customQueryData) {
      IntPtr pool_native = StringToNative(pool);
      var result = (ovr_Matchmaking_Browse_Native(pool_native, customQueryData));
      Marshal.FreeCoTaskMem(pool_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Matchmaking_Browse")]
    private static extern ulong ovr_Matchmaking_Browse_Native(IntPtr pool, IntPtr customQueryData);

    public static ulong ovr_Matchmaking_Browse2(string pool, IntPtr matchmakingOptions) {
      IntPtr pool_native = StringToNative(pool);
      var result = (ovr_Matchmaking_Browse2_Native(pool_native, matchmakingOptions));
      Marshal.FreeCoTaskMem(pool_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Matchmaking_Browse2")]
    private static extern ulong ovr_Matchmaking_Browse2_Native(IntPtr pool, IntPtr matchmakingOptions);

    public static ulong ovr_Matchmaking_Cancel(string pool, string requestHash) {
      IntPtr pool_native = StringToNative(pool);
      IntPtr requestHash_native = StringToNative(requestHash);
      var result = (ovr_Matchmaking_Cancel_Native(pool_native, requestHash_native));
      Marshal.FreeCoTaskMem(pool_native);
      Marshal.FreeCoTaskMem(requestHash_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Matchmaking_Cancel")]
    private static extern ulong ovr_Matchmaking_Cancel_Native(IntPtr pool, IntPtr requestHash);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Matchmaking_Cancel2();

    public static ulong ovr_Matchmaking_CreateAndEnqueueRoom(string pool, uint maxUsers, bool subscribeToUpdates, IntPtr customQueryData) {
      IntPtr pool_native = StringToNative(pool);
      var result = (ovr_Matchmaking_CreateAndEnqueueRoom_Native(pool_native, maxUsers, subscribeToUpdates, customQueryData));
      Marshal.FreeCoTaskMem(pool_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Matchmaking_CreateAndEnqueueRoom")]
    private static extern ulong ovr_Matchmaking_CreateAndEnqueueRoom_Native(IntPtr pool, uint maxUsers, bool subscribeToUpdates, IntPtr customQueryData);

    public static ulong ovr_Matchmaking_CreateAndEnqueueRoom2(string pool, IntPtr matchmakingOptions) {
      IntPtr pool_native = StringToNative(pool);
      var result = (ovr_Matchmaking_CreateAndEnqueueRoom2_Native(pool_native, matchmakingOptions));
      Marshal.FreeCoTaskMem(pool_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Matchmaking_CreateAndEnqueueRoom2")]
    private static extern ulong ovr_Matchmaking_CreateAndEnqueueRoom2_Native(IntPtr pool, IntPtr matchmakingOptions);

    public static ulong ovr_Matchmaking_CreateRoom(string pool, uint maxUsers, bool subscribeToUpdates) {
      IntPtr pool_native = StringToNative(pool);
      var result = (ovr_Matchmaking_CreateRoom_Native(pool_native, maxUsers, subscribeToUpdates));
      Marshal.FreeCoTaskMem(pool_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Matchmaking_CreateRoom")]
    private static extern ulong ovr_Matchmaking_CreateRoom_Native(IntPtr pool, uint maxUsers, bool subscribeToUpdates);

    public static ulong ovr_Matchmaking_CreateRoom2(string pool, IntPtr matchmakingOptions) {
      IntPtr pool_native = StringToNative(pool);
      var result = (ovr_Matchmaking_CreateRoom2_Native(pool_native, matchmakingOptions));
      Marshal.FreeCoTaskMem(pool_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Matchmaking_CreateRoom2")]
    private static extern ulong ovr_Matchmaking_CreateRoom2_Native(IntPtr pool, IntPtr matchmakingOptions);

    public static ulong ovr_Matchmaking_Enqueue(string pool, IntPtr customQueryData) {
      IntPtr pool_native = StringToNative(pool);
      var result = (ovr_Matchmaking_Enqueue_Native(pool_native, customQueryData));
      Marshal.FreeCoTaskMem(pool_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Matchmaking_Enqueue")]
    private static extern ulong ovr_Matchmaking_Enqueue_Native(IntPtr pool, IntPtr customQueryData);

    public static ulong ovr_Matchmaking_Enqueue2(string pool, IntPtr matchmakingOptions) {
      IntPtr pool_native = StringToNative(pool);
      var result = (ovr_Matchmaking_Enqueue2_Native(pool_native, matchmakingOptions));
      Marshal.FreeCoTaskMem(pool_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Matchmaking_Enqueue2")]
    private static extern ulong ovr_Matchmaking_Enqueue2_Native(IntPtr pool, IntPtr matchmakingOptions);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Matchmaking_EnqueueRoom(UInt64 roomID, IntPtr customQueryData);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Matchmaking_EnqueueRoom2(UInt64 roomID, IntPtr matchmakingOptions);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Matchmaking_GetAdminSnapshot();

    public static ulong ovr_Matchmaking_GetStats(string pool, uint maxLevel, MatchmakingStatApproach approach) {
      IntPtr pool_native = StringToNative(pool);
      var result = (ovr_Matchmaking_GetStats_Native(pool_native, maxLevel, approach));
      Marshal.FreeCoTaskMem(pool_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Matchmaking_GetStats")]
    private static extern ulong ovr_Matchmaking_GetStats_Native(IntPtr pool, uint maxLevel, MatchmakingStatApproach approach);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Matchmaking_JoinRoom(UInt64 roomID, bool subscribeToUpdates);

    public static ulong ovr_Matchmaking_ReportResultInsecure(UInt64 roomID, ovrKeyValuePair[] data) {
      UIntPtr data_length = (UIntPtr)data.Length;
      var result = (ovr_Matchmaking_ReportResultInsecure_Native(roomID, data, data_length));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Matchmaking_ReportResultInsecure")]
    private static extern ulong ovr_Matchmaking_ReportResultInsecure_Native(UInt64 roomID, ovrKeyValuePair[] data, UIntPtr numItems);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Matchmaking_StartMatch(UInt64 roomID);

    public static ulong ovr_Media_ShareToFacebook(string postTextSuggestion, string filePath, MediaContentType contentType) {
      IntPtr postTextSuggestion_native = StringToNative(postTextSuggestion);
      IntPtr filePath_native = StringToNative(filePath);
      var result = (ovr_Media_ShareToFacebook_Native(postTextSuggestion_native, filePath_native, contentType));
      Marshal.FreeCoTaskMem(postTextSuggestion_native);
      Marshal.FreeCoTaskMem(filePath_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Media_ShareToFacebook")]
    private static extern ulong ovr_Media_ShareToFacebook_Native(IntPtr postTextSuggestion, IntPtr filePath, MediaContentType contentType);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_NetSync_Connect(IntPtr connect_options);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_NetSync_Disconnect(long connection_id);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_NetSync_GetSessions(long connection_id);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_NetSync_GetVoipAttenuation(long connection_id);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_NetSync_GetVoipAttenuationDefault();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_NetSync_SetVoipAttenuation(long connection_id, float[] distances, float[] decibels, UIntPtr count);

    public static ulong ovr_NetSync_SetVoipAttenuationModel(long connection_id, string name, float[] distances, float[] decibels, UIntPtr count) {
      IntPtr name_native = StringToNative(name);
      var result = (ovr_NetSync_SetVoipAttenuationModel_Native(connection_id, name_native, distances, decibels, count));
      Marshal.FreeCoTaskMem(name_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_NetSync_SetVoipAttenuationModel")]
    private static extern ulong ovr_NetSync_SetVoipAttenuationModel_Native(long connection_id, IntPtr name, float[] distances, float[] decibels, UIntPtr count);

    public static ulong ovr_NetSync_SetVoipChannelCfg(long connection_id, string channel_name, string attnmodel, bool disable_spatialization) {
      IntPtr channel_name_native = StringToNative(channel_name);
      IntPtr attnmodel_native = StringToNative(attnmodel);
      var result = (ovr_NetSync_SetVoipChannelCfg_Native(connection_id, channel_name_native, attnmodel_native, disable_spatialization));
      Marshal.FreeCoTaskMem(channel_name_native);
      Marshal.FreeCoTaskMem(attnmodel_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_NetSync_SetVoipChannelCfg")]
    private static extern ulong ovr_NetSync_SetVoipChannelCfg_Native(long connection_id, IntPtr channel_name, IntPtr attnmodel, bool disable_spatialization);

    public static ulong ovr_NetSync_SetVoipGroup(long connection_id, string group_id) {
      IntPtr group_id_native = StringToNative(group_id);
      var result = (ovr_NetSync_SetVoipGroup_Native(connection_id, group_id_native));
      Marshal.FreeCoTaskMem(group_id_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_NetSync_SetVoipGroup")]
    private static extern ulong ovr_NetSync_SetVoipGroup_Native(long connection_id, IntPtr group_id);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_NetSync_SetVoipListentoChannels(long connection_id, string[] listento_channels, UIntPtr count);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_NetSync_SetVoipMicSource(long connection_id, NetSyncVoipMicSource mic_source);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_NetSync_SetVoipSessionMuted(long connection_id, UInt64 session_id, bool muted);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_NetSync_SetVoipSpeaktoChannels(long connection_id, string[] speakto_channels, UIntPtr count);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_NetSync_SetVoipStreamMode(long connection_id, UInt64 sessionId, NetSyncVoipStreamMode streamMode);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Notification_GetRoomInvites();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Notification_MarkAsRead(UInt64 notificationID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Party_Create();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Party_GatherInApplication(UInt64 partyID, UInt64 appID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Party_Get(UInt64 partyID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Party_GetCurrent();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Party_GetCurrentForUser(UInt64 userID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Party_Invite(UInt64 partyID, UInt64 userID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Party_Join(UInt64 partyID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Party_Leave(UInt64 partyID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_RichPresence_Clear();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_RichPresence_GetDestinations();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_RichPresence_Set(IntPtr richPresenceOptions);

    public static ulong ovr_RichPresence_SetDestination(string api_name) {
      IntPtr api_name_native = StringToNative(api_name);
      var result = (ovr_RichPresence_SetDestination_Native(api_name_native));
      Marshal.FreeCoTaskMem(api_name_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_RichPresence_SetDestination")]
    private static extern ulong ovr_RichPresence_SetDestination_Native(IntPtr api_name);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_RichPresence_SetIsJoinable(bool is_joinable);

    public static ulong ovr_RichPresence_SetLobbySession(string id) {
      IntPtr id_native = StringToNative(id);
      var result = (ovr_RichPresence_SetLobbySession_Native(id_native));
      Marshal.FreeCoTaskMem(id_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_RichPresence_SetLobbySession")]
    private static extern ulong ovr_RichPresence_SetLobbySession_Native(IntPtr id);

    public static ulong ovr_RichPresence_SetMatchSession(string id) {
      IntPtr id_native = StringToNative(id);
      var result = (ovr_RichPresence_SetMatchSession_Native(id_native));
      Marshal.FreeCoTaskMem(id_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_RichPresence_SetMatchSession")]
    private static extern ulong ovr_RichPresence_SetMatchSession_Native(IntPtr id);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Room_CreateAndJoinPrivate(RoomJoinPolicy joinPolicy, uint maxUsers, bool subscribeToUpdates);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Room_CreateAndJoinPrivate2(RoomJoinPolicy joinPolicy, uint maxUsers, IntPtr roomOptions);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Room_Get(UInt64 roomID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Room_GetCurrent();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Room_GetCurrentForUser(UInt64 userID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Room_GetInvitableUsers();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Room_GetInvitableUsers2(IntPtr roomOptions);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Room_GetModeratedRooms();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Room_GetSocialRooms(UInt64 appID);

    public static ulong ovr_Room_InviteUser(UInt64 roomID, string inviteToken) {
      IntPtr inviteToken_native = StringToNative(inviteToken);
      var result = (ovr_Room_InviteUser_Native(roomID, inviteToken_native));
      Marshal.FreeCoTaskMem(inviteToken_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Room_InviteUser")]
    private static extern ulong ovr_Room_InviteUser_Native(UInt64 roomID, IntPtr inviteToken);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Room_Join(UInt64 roomID, bool subscribeToUpdates);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Room_Join2(UInt64 roomID, IntPtr roomOptions);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Room_KickUser(UInt64 roomID, UInt64 userID, int kickDurationSeconds);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Room_LaunchInvitableUserFlow(UInt64 roomID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Room_Leave(UInt64 roomID);

    public static ulong ovr_Room_SetDescription(UInt64 roomID, string description) {
      IntPtr description_native = StringToNative(description);
      var result = (ovr_Room_SetDescription_Native(roomID, description_native));
      Marshal.FreeCoTaskMem(description_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Room_SetDescription")]
    private static extern ulong ovr_Room_SetDescription_Native(UInt64 roomID, IntPtr description);

    public static ulong ovr_Room_UpdateDataStore(UInt64 roomID, ovrKeyValuePair[] data) {
      UIntPtr data_length = (UIntPtr)data.Length;
      var result = (ovr_Room_UpdateDataStore_Native(roomID, data, data_length));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Room_UpdateDataStore")]
    private static extern ulong ovr_Room_UpdateDataStore_Native(UInt64 roomID, ovrKeyValuePair[] data, UIntPtr numItems);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Room_UpdateMembershipLockStatus(UInt64 roomID, RoomMembershipLockStatus membershipLockStatus);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Room_UpdateOwner(UInt64 roomID, UInt64 userID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Room_UpdatePrivateRoomJoinPolicy(UInt64 roomID, RoomJoinPolicy newJoinPolicy);

    public static ulong ovr_User_CancelRecordingForReportFlow(string recordingUUID) {
      IntPtr recordingUUID_native = StringToNative(recordingUUID);
      var result = (ovr_User_CancelRecordingForReportFlow_Native(recordingUUID_native));
      Marshal.FreeCoTaskMem(recordingUUID_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_User_CancelRecordingForReportFlow")]
    private static extern ulong ovr_User_CancelRecordingForReportFlow_Native(IntPtr recordingUUID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_User_Get(UInt64 userID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_User_GetAccessToken();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_User_GetBlockedUsers();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_User_GetLinkedAccounts(IntPtr userOptions);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_User_GetLoggedInUser();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_User_GetLoggedInUserFriends();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_User_GetLoggedInUserFriendsAndRooms();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_User_GetLoggedInUserRecentlyMetUsersAndRooms(IntPtr userOptions);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_User_GetOrgScopedID(UInt64 userID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_User_GetSdkAccounts();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_User_GetUserCapabilities();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_User_GetUserProof();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_User_LaunchBlockFlow(UInt64 userID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_User_LaunchFriendRequestFlow(UInt64 userID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_User_LaunchReportFlow(UInt64 userID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_User_LaunchReportFlow2(UInt64 optionalUserID, IntPtr abuseReportOptions);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_User_LaunchUnblockFlow(UInt64 userID);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_User_NewEntitledTestUser();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_User_NewTestUser();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_User_NewTestUserFriends();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_User_StartRecordingForReportFlow();

    public static ulong ovr_User_StopRecordingAndLaunchReportFlow(UInt64 optionalUserID, string optionalRecordingUUID) {
      IntPtr optionalRecordingUUID_native = StringToNative(optionalRecordingUUID);
      var result = (ovr_User_StopRecordingAndLaunchReportFlow_Native(optionalUserID, optionalRecordingUUID_native));
      Marshal.FreeCoTaskMem(optionalRecordingUUID_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_User_StopRecordingAndLaunchReportFlow")]
    private static extern ulong ovr_User_StopRecordingAndLaunchReportFlow_Native(UInt64 optionalUserID, IntPtr optionalRecordingUUID);

    public static ulong ovr_User_StopRecordingAndLaunchReportFlow2(UInt64 optionalUserID, string optionalRecordingUUID, IntPtr abuseReportOptions) {
      IntPtr optionalRecordingUUID_native = StringToNative(optionalRecordingUUID);
      var result = (ovr_User_StopRecordingAndLaunchReportFlow2_Native(optionalUserID, optionalRecordingUUID_native, abuseReportOptions));
      Marshal.FreeCoTaskMem(optionalRecordingUUID_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_User_StopRecordingAndLaunchReportFlow2")]
    private static extern ulong ovr_User_StopRecordingAndLaunchReportFlow2_Native(UInt64 optionalUserID, IntPtr optionalRecordingUUID, IntPtr abuseReportOptions);

    public static ulong ovr_User_TestUserCreateDeviceManifest(string deviceID, UInt64[] appIDs, int numAppIDs) {
      IntPtr deviceID_native = StringToNative(deviceID);
      var result = (ovr_User_TestUserCreateDeviceManifest_Native(deviceID_native, appIDs, numAppIDs));
      Marshal.FreeCoTaskMem(deviceID_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_User_TestUserCreateDeviceManifest")]
    private static extern ulong ovr_User_TestUserCreateDeviceManifest_Native(IntPtr deviceID, UInt64[] appIDs, int numAppIDs);

    public static ulong ovr_UserDataStore_PrivateDeleteEntryByKey(UInt64 userID, string key) {
      IntPtr key_native = StringToNative(key);
      var result = (ovr_UserDataStore_PrivateDeleteEntryByKey_Native(userID, key_native));
      Marshal.FreeCoTaskMem(key_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_UserDataStore_PrivateDeleteEntryByKey")]
    private static extern ulong ovr_UserDataStore_PrivateDeleteEntryByKey_Native(UInt64 userID, IntPtr key);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_UserDataStore_PrivateGetEntries(UInt64 userID);

    public static ulong ovr_UserDataStore_PrivateGetEntryByKey(UInt64 userID, string key) {
      IntPtr key_native = StringToNative(key);
      var result = (ovr_UserDataStore_PrivateGetEntryByKey_Native(userID, key_native));
      Marshal.FreeCoTaskMem(key_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_UserDataStore_PrivateGetEntryByKey")]
    private static extern ulong ovr_UserDataStore_PrivateGetEntryByKey_Native(UInt64 userID, IntPtr key);

    public static ulong ovr_UserDataStore_PrivateWriteEntry(UInt64 userID, string key, string value) {
      IntPtr key_native = StringToNative(key);
      IntPtr value_native = StringToNative(value);
      var result = (ovr_UserDataStore_PrivateWriteEntry_Native(userID, key_native, value_native));
      Marshal.FreeCoTaskMem(key_native);
      Marshal.FreeCoTaskMem(value_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_UserDataStore_PrivateWriteEntry")]
    private static extern ulong ovr_UserDataStore_PrivateWriteEntry_Native(UInt64 userID, IntPtr key, IntPtr value);

    public static ulong ovr_UserDataStore_PublicDeleteEntryByKey(UInt64 userID, string key) {
      IntPtr key_native = StringToNative(key);
      var result = (ovr_UserDataStore_PublicDeleteEntryByKey_Native(userID, key_native));
      Marshal.FreeCoTaskMem(key_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_UserDataStore_PublicDeleteEntryByKey")]
    private static extern ulong ovr_UserDataStore_PublicDeleteEntryByKey_Native(UInt64 userID, IntPtr key);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_UserDataStore_PublicGetEntries(UInt64 userID);

    public static ulong ovr_UserDataStore_PublicGetEntryByKey(UInt64 userID, string key) {
      IntPtr key_native = StringToNative(key);
      var result = (ovr_UserDataStore_PublicGetEntryByKey_Native(userID, key_native));
      Marshal.FreeCoTaskMem(key_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_UserDataStore_PublicGetEntryByKey")]
    private static extern ulong ovr_UserDataStore_PublicGetEntryByKey_Native(UInt64 userID, IntPtr key);

    public static ulong ovr_UserDataStore_PublicWriteEntry(UInt64 userID, string key, string value) {
      IntPtr key_native = StringToNative(key);
      IntPtr value_native = StringToNative(value);
      var result = (ovr_UserDataStore_PublicWriteEntry_Native(userID, key_native, value_native));
      Marshal.FreeCoTaskMem(key_native);
      Marshal.FreeCoTaskMem(value_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_UserDataStore_PublicWriteEntry")]
    private static extern ulong ovr_UserDataStore_PublicWriteEntry_Native(UInt64 userID, IntPtr key, IntPtr value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Voip_GetMicrophoneAvailability();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Voip_SetSystemVoipSuppressed(bool suppressed);

    public static string ovr_AbuseReportRecording_GetRecordingUuid(IntPtr obj) {
      var result = StringFromNative(ovr_AbuseReportRecording_GetRecordingUuid_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_AbuseReportRecording_GetRecordingUuid")]
    private static extern IntPtr ovr_AbuseReportRecording_GetRecordingUuid_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern uint ovr_AchievementDefinition_GetBitfieldLength(IntPtr obj);

    public static string ovr_AchievementDefinition_GetName(IntPtr obj) {
      var result = StringFromNative(ovr_AchievementDefinition_GetName_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_AchievementDefinition_GetName")]
    private static extern IntPtr ovr_AchievementDefinition_GetName_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_AchievementDefinition_GetTarget(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern AchievementType ovr_AchievementDefinition_GetType(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_AchievementDefinitionArray_GetElement(IntPtr obj, UIntPtr index);

    public static string ovr_AchievementDefinitionArray_GetNextUrl(IntPtr obj) {
      var result = StringFromNative(ovr_AchievementDefinitionArray_GetNextUrl_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_AchievementDefinitionArray_GetNextUrl")]
    private static extern IntPtr ovr_AchievementDefinitionArray_GetNextUrl_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_AchievementDefinitionArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_AchievementDefinitionArray_HasNextPage(IntPtr obj);

    public static string ovr_AchievementProgress_GetBitfield(IntPtr obj) {
      var result = StringFromNative(ovr_AchievementProgress_GetBitfield_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_AchievementProgress_GetBitfield")]
    private static extern IntPtr ovr_AchievementProgress_GetBitfield_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_AchievementProgress_GetCount(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_AchievementProgress_GetIsUnlocked(IntPtr obj);

    public static string ovr_AchievementProgress_GetName(IntPtr obj) {
      var result = StringFromNative(ovr_AchievementProgress_GetName_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_AchievementProgress_GetName")]
    private static extern IntPtr ovr_AchievementProgress_GetName_Native(IntPtr obj);

    public static DateTime ovr_AchievementProgress_GetUnlockTime(IntPtr obj) {
      var result = DateTimeFromNative(ovr_AchievementProgress_GetUnlockTime_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_AchievementProgress_GetUnlockTime")]
    private static extern ulong ovr_AchievementProgress_GetUnlockTime_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_AchievementProgressArray_GetElement(IntPtr obj, UIntPtr index);

    public static string ovr_AchievementProgressArray_GetNextUrl(IntPtr obj) {
      var result = StringFromNative(ovr_AchievementProgressArray_GetNextUrl_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_AchievementProgressArray_GetNextUrl")]
    private static extern IntPtr ovr_AchievementProgressArray_GetNextUrl_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_AchievementProgressArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_AchievementProgressArray_HasNextPage(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_AchievementUpdate_GetJustUnlocked(IntPtr obj);

    public static string ovr_AchievementUpdate_GetName(IntPtr obj) {
      var result = StringFromNative(ovr_AchievementUpdate_GetName_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_AchievementUpdate_GetName")]
    private static extern IntPtr ovr_AchievementUpdate_GetName_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_Application_GetID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_ApplicationInvite_GetDestination(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_ApplicationInvite_GetID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_ApplicationInvite_GetIsActive(IntPtr obj);

    public static string ovr_ApplicationInvite_GetLobbySessionId(IntPtr obj) {
      var result = StringFromNative(ovr_ApplicationInvite_GetLobbySessionId_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_ApplicationInvite_GetLobbySessionId")]
    private static extern IntPtr ovr_ApplicationInvite_GetLobbySessionId_Native(IntPtr obj);

    public static string ovr_ApplicationInvite_GetMatchSessionId(IntPtr obj) {
      var result = StringFromNative(ovr_ApplicationInvite_GetMatchSessionId_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_ApplicationInvite_GetMatchSessionId")]
    private static extern IntPtr ovr_ApplicationInvite_GetMatchSessionId_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_ApplicationInvite_GetRecipient(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_ApplicationInviteArray_GetElement(IntPtr obj, UIntPtr index);

    public static string ovr_ApplicationInviteArray_GetNextUrl(IntPtr obj) {
      var result = StringFromNative(ovr_ApplicationInviteArray_GetNextUrl_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_ApplicationInviteArray_GetNextUrl")]
    private static extern IntPtr ovr_ApplicationInviteArray_GetNextUrl_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_ApplicationInviteArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_ApplicationInviteArray_HasNextPage(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern int ovr_ApplicationVersion_GetCurrentCode(IntPtr obj);

    public static string ovr_ApplicationVersion_GetCurrentName(IntPtr obj) {
      var result = StringFromNative(ovr_ApplicationVersion_GetCurrentName_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_ApplicationVersion_GetCurrentName")]
    private static extern IntPtr ovr_ApplicationVersion_GetCurrentName_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern int ovr_ApplicationVersion_GetLatestCode(IntPtr obj);

    public static string ovr_ApplicationVersion_GetLatestName(IntPtr obj) {
      var result = StringFromNative(ovr_ApplicationVersion_GetLatestName_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_ApplicationVersion_GetLatestName")]
    private static extern IntPtr ovr_ApplicationVersion_GetLatestName_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_AssetDetails_GetAssetId(IntPtr obj);

    public static string ovr_AssetDetails_GetAssetType(IntPtr obj) {
      var result = StringFromNative(ovr_AssetDetails_GetAssetType_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_AssetDetails_GetAssetType")]
    private static extern IntPtr ovr_AssetDetails_GetAssetType_Native(IntPtr obj);

    public static string ovr_AssetDetails_GetDownloadStatus(IntPtr obj) {
      var result = StringFromNative(ovr_AssetDetails_GetDownloadStatus_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_AssetDetails_GetDownloadStatus")]
    private static extern IntPtr ovr_AssetDetails_GetDownloadStatus_Native(IntPtr obj);

    public static string ovr_AssetDetails_GetFilepath(IntPtr obj) {
      var result = StringFromNative(ovr_AssetDetails_GetFilepath_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_AssetDetails_GetFilepath")]
    private static extern IntPtr ovr_AssetDetails_GetFilepath_Native(IntPtr obj);

    public static string ovr_AssetDetails_GetIapStatus(IntPtr obj) {
      var result = StringFromNative(ovr_AssetDetails_GetIapStatus_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_AssetDetails_GetIapStatus")]
    private static extern IntPtr ovr_AssetDetails_GetIapStatus_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_AssetDetails_GetLanguage(IntPtr obj);

    public static string ovr_AssetDetails_GetMetadata(IntPtr obj) {
      var result = StringFromNative(ovr_AssetDetails_GetMetadata_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_AssetDetails_GetMetadata")]
    private static extern IntPtr ovr_AssetDetails_GetMetadata_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_AssetDetailsArray_GetElement(IntPtr obj, UIntPtr index);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_AssetDetailsArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_AssetFileDeleteResult_GetAssetFileId(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_AssetFileDeleteResult_GetAssetId(IntPtr obj);

    public static string ovr_AssetFileDeleteResult_GetFilepath(IntPtr obj) {
      var result = StringFromNative(ovr_AssetFileDeleteResult_GetFilepath_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_AssetFileDeleteResult_GetFilepath")]
    private static extern IntPtr ovr_AssetFileDeleteResult_GetFilepath_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_AssetFileDeleteResult_GetSuccess(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_AssetFileDownloadCancelResult_GetAssetFileId(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_AssetFileDownloadCancelResult_GetAssetId(IntPtr obj);

    public static string ovr_AssetFileDownloadCancelResult_GetFilepath(IntPtr obj) {
      var result = StringFromNative(ovr_AssetFileDownloadCancelResult_GetFilepath_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_AssetFileDownloadCancelResult_GetFilepath")]
    private static extern IntPtr ovr_AssetFileDownloadCancelResult_GetFilepath_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_AssetFileDownloadCancelResult_GetSuccess(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_AssetFileDownloadResult_GetAssetId(IntPtr obj);

    public static string ovr_AssetFileDownloadResult_GetFilepath(IntPtr obj) {
      var result = StringFromNative(ovr_AssetFileDownloadResult_GetFilepath_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_AssetFileDownloadResult_GetFilepath")]
    private static extern IntPtr ovr_AssetFileDownloadResult_GetFilepath_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_AssetFileDownloadUpdate_GetAssetFileId(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_AssetFileDownloadUpdate_GetAssetId(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern uint ovr_AssetFileDownloadUpdate_GetBytesTotal(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_AssetFileDownloadUpdate_GetBytesTotalLong(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern int ovr_AssetFileDownloadUpdate_GetBytesTransferred(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern long ovr_AssetFileDownloadUpdate_GetBytesTransferredLong(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_AssetFileDownloadUpdate_GetCompleted(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_BlockedUser_GetId(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_BlockedUserArray_GetElement(IntPtr obj, UIntPtr index);

    public static string ovr_BlockedUserArray_GetNextUrl(IntPtr obj) {
      var result = StringFromNative(ovr_BlockedUserArray_GetNextUrl_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_BlockedUserArray_GetNextUrl")]
    private static extern IntPtr ovr_BlockedUserArray_GetNextUrl_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_BlockedUserArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_BlockedUserArray_HasNextPage(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern int ovr_CalApplicationFinalized_GetCountdownMS(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_CalApplicationFinalized_GetID(IntPtr obj);

    public static string ovr_CalApplicationFinalized_GetLaunchDetails(IntPtr obj) {
      var result = StringFromNative(ovr_CalApplicationFinalized_GetLaunchDetails_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_CalApplicationFinalized_GetLaunchDetails")]
    private static extern IntPtr ovr_CalApplicationFinalized_GetLaunchDetails_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_CalApplicationProposed_GetID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_CalApplicationSuggestion_GetID(IntPtr obj);

    public static string ovr_CalApplicationSuggestion_GetSocialContext(IntPtr obj) {
      var result = StringFromNative(ovr_CalApplicationSuggestion_GetSocialContext_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_CalApplicationSuggestion_GetSocialContext")]
    private static extern IntPtr ovr_CalApplicationSuggestion_GetSocialContext_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_CalApplicationSuggestionArray_GetElement(IntPtr obj, UIntPtr index);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_CalApplicationSuggestionArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ChallengeCreationType ovr_Challenge_GetCreationType(IntPtr obj);

    public static string ovr_Challenge_GetDescription(IntPtr obj) {
      var result = StringFromNative(ovr_Challenge_GetDescription_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Challenge_GetDescription")]
    private static extern IntPtr ovr_Challenge_GetDescription_Native(IntPtr obj);

    public static DateTime ovr_Challenge_GetEndDate(IntPtr obj) {
      var result = DateTimeFromNative(ovr_Challenge_GetEndDate_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Challenge_GetEndDate")]
    private static extern ulong ovr_Challenge_GetEndDate_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_Challenge_GetID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Challenge_GetInvitedUsers(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Challenge_GetLeaderboard(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Challenge_GetParticipants(IntPtr obj);

    public static DateTime ovr_Challenge_GetStartDate(IntPtr obj) {
      var result = DateTimeFromNative(ovr_Challenge_GetStartDate_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Challenge_GetStartDate")]
    private static extern ulong ovr_Challenge_GetStartDate_Native(IntPtr obj);

    public static string ovr_Challenge_GetTitle(IntPtr obj) {
      var result = StringFromNative(ovr_Challenge_GetTitle_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Challenge_GetTitle")]
    private static extern IntPtr ovr_Challenge_GetTitle_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ChallengeVisibility ovr_Challenge_GetVisibility(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_ChallengeArray_GetElement(IntPtr obj, UIntPtr index);

    public static string ovr_ChallengeArray_GetNextUrl(IntPtr obj) {
      var result = StringFromNative(ovr_ChallengeArray_GetNextUrl_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_ChallengeArray_GetNextUrl")]
    private static extern IntPtr ovr_ChallengeArray_GetNextUrl_Native(IntPtr obj);

    public static string ovr_ChallengeArray_GetPreviousUrl(IntPtr obj) {
      var result = StringFromNative(ovr_ChallengeArray_GetPreviousUrl_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_ChallengeArray_GetPreviousUrl")]
    private static extern IntPtr ovr_ChallengeArray_GetPreviousUrl_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_ChallengeArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_ChallengeArray_GetTotalCount(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_ChallengeArray_HasNextPage(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_ChallengeArray_HasPreviousPage(IntPtr obj);

    public static string ovr_ChallengeEntry_GetDisplayScore(IntPtr obj) {
      var result = StringFromNative(ovr_ChallengeEntry_GetDisplayScore_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_ChallengeEntry_GetDisplayScore")]
    private static extern IntPtr ovr_ChallengeEntry_GetDisplayScore_Native(IntPtr obj);

    public static byte[] ovr_ChallengeEntry_GetExtraData(IntPtr obj) {
      var result = BlobFromNative(ovr_LeaderboardEntry_GetExtraDataLength(obj), ovr_ChallengeEntry_GetExtraData_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_ChallengeEntry_GetExtraData")]
    private static extern IntPtr ovr_ChallengeEntry_GetExtraData_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern uint ovr_ChallengeEntry_GetExtraDataLength(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_ChallengeEntry_GetID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern int ovr_ChallengeEntry_GetRank(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern long ovr_ChallengeEntry_GetScore(IntPtr obj);

    public static DateTime ovr_ChallengeEntry_GetTimestamp(IntPtr obj) {
      var result = DateTimeFromNative(ovr_ChallengeEntry_GetTimestamp_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_ChallengeEntry_GetTimestamp")]
    private static extern ulong ovr_ChallengeEntry_GetTimestamp_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_ChallengeEntry_GetUser(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_ChallengeEntryArray_GetElement(IntPtr obj, UIntPtr index);

    public static string ovr_ChallengeEntryArray_GetNextUrl(IntPtr obj) {
      var result = StringFromNative(ovr_ChallengeEntryArray_GetNextUrl_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_ChallengeEntryArray_GetNextUrl")]
    private static extern IntPtr ovr_ChallengeEntryArray_GetNextUrl_Native(IntPtr obj);

    public static string ovr_ChallengeEntryArray_GetPreviousUrl(IntPtr obj) {
      var result = StringFromNative(ovr_ChallengeEntryArray_GetPreviousUrl_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_ChallengeEntryArray_GetPreviousUrl")]
    private static extern IntPtr ovr_ChallengeEntryArray_GetPreviousUrl_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_ChallengeEntryArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_ChallengeEntryArray_GetTotalCount(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_ChallengeEntryArray_HasNextPage(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_ChallengeEntryArray_HasPreviousPage(IntPtr obj);

    public static string ovr_CloudStorage2UserDirectoryPathResponse_GetPath(IntPtr obj) {
      var result = StringFromNative(ovr_CloudStorage2UserDirectoryPathResponse_GetPath_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_CloudStorage2UserDirectoryPathResponse_GetPath")]
    private static extern IntPtr ovr_CloudStorage2UserDirectoryPathResponse_GetPath_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_CloudStorageConflictMetadata_GetLocal(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_CloudStorageConflictMetadata_GetRemote(IntPtr obj);

    public static string ovr_CloudStorageData_GetBucket(IntPtr obj) {
      var result = StringFromNative(ovr_CloudStorageData_GetBucket_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_CloudStorageData_GetBucket")]
    private static extern IntPtr ovr_CloudStorageData_GetBucket_Native(IntPtr obj);

    public static byte[] ovr_CloudStorageData_GetData(IntPtr obj) {
      var result = FiledataFromNative(ovr_CloudStorageData_GetDataSize(obj), ovr_CloudStorageData_GetData_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_CloudStorageData_GetData")]
    private static extern IntPtr ovr_CloudStorageData_GetData_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern uint ovr_CloudStorageData_GetDataSize(IntPtr obj);

    public static string ovr_CloudStorageData_GetKey(IntPtr obj) {
      var result = StringFromNative(ovr_CloudStorageData_GetKey_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_CloudStorageData_GetKey")]
    private static extern IntPtr ovr_CloudStorageData_GetKey_Native(IntPtr obj);

    public static string ovr_CloudStorageMetadata_GetBucket(IntPtr obj) {
      var result = StringFromNative(ovr_CloudStorageMetadata_GetBucket_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_CloudStorageMetadata_GetBucket")]
    private static extern IntPtr ovr_CloudStorageMetadata_GetBucket_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern long ovr_CloudStorageMetadata_GetCounter(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern uint ovr_CloudStorageMetadata_GetDataSize(IntPtr obj);

    public static string ovr_CloudStorageMetadata_GetExtraData(IntPtr obj) {
      var result = StringFromNative(ovr_CloudStorageMetadata_GetExtraData_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_CloudStorageMetadata_GetExtraData")]
    private static extern IntPtr ovr_CloudStorageMetadata_GetExtraData_Native(IntPtr obj);

    public static string ovr_CloudStorageMetadata_GetKey(IntPtr obj) {
      var result = StringFromNative(ovr_CloudStorageMetadata_GetKey_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_CloudStorageMetadata_GetKey")]
    private static extern IntPtr ovr_CloudStorageMetadata_GetKey_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_CloudStorageMetadata_GetSaveTime(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern CloudStorageDataStatus ovr_CloudStorageMetadata_GetStatus(IntPtr obj);

    public static string ovr_CloudStorageMetadata_GetVersionHandle(IntPtr obj) {
      var result = StringFromNative(ovr_CloudStorageMetadata_GetVersionHandle_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_CloudStorageMetadata_GetVersionHandle")]
    private static extern IntPtr ovr_CloudStorageMetadata_GetVersionHandle_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_CloudStorageMetadataArray_GetElement(IntPtr obj, UIntPtr index);

    public static string ovr_CloudStorageMetadataArray_GetNextUrl(IntPtr obj) {
      var result = StringFromNative(ovr_CloudStorageMetadataArray_GetNextUrl_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_CloudStorageMetadataArray_GetNextUrl")]
    private static extern IntPtr ovr_CloudStorageMetadataArray_GetNextUrl_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_CloudStorageMetadataArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_CloudStorageMetadataArray_HasNextPage(IntPtr obj);

    public static string ovr_CloudStorageUpdateResponse_GetBucket(IntPtr obj) {
      var result = StringFromNative(ovr_CloudStorageUpdateResponse_GetBucket_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_CloudStorageUpdateResponse_GetBucket")]
    private static extern IntPtr ovr_CloudStorageUpdateResponse_GetBucket_Native(IntPtr obj);

    public static string ovr_CloudStorageUpdateResponse_GetKey(IntPtr obj) {
      var result = StringFromNative(ovr_CloudStorageUpdateResponse_GetKey_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_CloudStorageUpdateResponse_GetKey")]
    private static extern IntPtr ovr_CloudStorageUpdateResponse_GetKey_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern CloudStorageUpdateStatus ovr_CloudStorageUpdateResponse_GetStatus(IntPtr obj);

    public static string ovr_CloudStorageUpdateResponse_GetVersionHandle(IntPtr obj) {
      var result = StringFromNative(ovr_CloudStorageUpdateResponse_GetVersionHandle_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_CloudStorageUpdateResponse_GetVersionHandle")]
    private static extern IntPtr ovr_CloudStorageUpdateResponse_GetVersionHandle_Native(IntPtr obj);

    public static uint ovr_DataStore_Contains(IntPtr obj, string key) {
      IntPtr key_native = StringToNative(key);
      var result = (ovr_DataStore_Contains_Native(obj, key_native));
      Marshal.FreeCoTaskMem(key_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_DataStore_Contains")]
    private static extern uint ovr_DataStore_Contains_Native(IntPtr obj, IntPtr key);

    public static string ovr_DataStore_GetKey(IntPtr obj, int index) {
      var result = StringFromNative(ovr_DataStore_GetKey_Native(obj, index));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_DataStore_GetKey")]
    private static extern IntPtr ovr_DataStore_GetKey_Native(IntPtr obj, int index);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_DataStore_GetNumKeys(IntPtr obj);

    public static string ovr_DataStore_GetValue(IntPtr obj, string key) {
      IntPtr key_native = StringToNative(key);
      var result = StringFromNative(ovr_DataStore_GetValue_Native(obj, key_native));
      Marshal.FreeCoTaskMem(key_native);
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_DataStore_GetValue")]
    private static extern IntPtr ovr_DataStore_GetValue_Native(IntPtr obj, IntPtr key);

    public static string ovr_Destination_GetApiName(IntPtr obj) {
      var result = StringFromNative(ovr_Destination_GetApiName_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Destination_GetApiName")]
    private static extern IntPtr ovr_Destination_GetApiName_Native(IntPtr obj);

    public static string ovr_Destination_GetDeeplinkMessage(IntPtr obj) {
      var result = StringFromNative(ovr_Destination_GetDeeplinkMessage_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Destination_GetDeeplinkMessage")]
    private static extern IntPtr ovr_Destination_GetDeeplinkMessage_Native(IntPtr obj);

    public static string ovr_Destination_GetDisplayName(IntPtr obj) {
      var result = StringFromNative(ovr_Destination_GetDisplayName_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Destination_GetDisplayName")]
    private static extern IntPtr ovr_Destination_GetDisplayName_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_DestinationArray_GetElement(IntPtr obj, UIntPtr index);

    public static string ovr_DestinationArray_GetNextUrl(IntPtr obj) {
      var result = StringFromNative(ovr_DestinationArray_GetNextUrl_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_DestinationArray_GetNextUrl")]
    private static extern IntPtr ovr_DestinationArray_GetNextUrl_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_DestinationArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_DestinationArray_HasNextPage(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern int ovr_Error_GetCode(IntPtr obj);

    public static string ovr_Error_GetDisplayableMessage(IntPtr obj) {
      var result = StringFromNative(ovr_Error_GetDisplayableMessage_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Error_GetDisplayableMessage")]
    private static extern IntPtr ovr_Error_GetDisplayableMessage_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern int ovr_Error_GetHttpCode(IntPtr obj);

    public static string ovr_Error_GetMessage(IntPtr obj) {
      var result = StringFromNative(ovr_Error_GetMessage_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Error_GetMessage")]
    private static extern IntPtr ovr_Error_GetMessage_Native(IntPtr obj);

    public static string ovr_GroupPresenceJoinIntent_GetDeeplinkMessage(IntPtr obj) {
      var result = StringFromNative(ovr_GroupPresenceJoinIntent_GetDeeplinkMessage_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_GroupPresenceJoinIntent_GetDeeplinkMessage")]
    private static extern IntPtr ovr_GroupPresenceJoinIntent_GetDeeplinkMessage_Native(IntPtr obj);

    public static string ovr_GroupPresenceJoinIntent_GetDestinationApiName(IntPtr obj) {
      var result = StringFromNative(ovr_GroupPresenceJoinIntent_GetDestinationApiName_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_GroupPresenceJoinIntent_GetDestinationApiName")]
    private static extern IntPtr ovr_GroupPresenceJoinIntent_GetDestinationApiName_Native(IntPtr obj);

    public static string ovr_GroupPresenceJoinIntent_GetLobbySessionId(IntPtr obj) {
      var result = StringFromNative(ovr_GroupPresenceJoinIntent_GetLobbySessionId_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_GroupPresenceJoinIntent_GetLobbySessionId")]
    private static extern IntPtr ovr_GroupPresenceJoinIntent_GetLobbySessionId_Native(IntPtr obj);

    public static string ovr_GroupPresenceJoinIntent_GetMatchSessionId(IntPtr obj) {
      var result = StringFromNative(ovr_GroupPresenceJoinIntent_GetMatchSessionId_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_GroupPresenceJoinIntent_GetMatchSessionId")]
    private static extern IntPtr ovr_GroupPresenceJoinIntent_GetMatchSessionId_Native(IntPtr obj);

    public static string ovr_GroupPresenceLeaveIntent_GetDestinationApiName(IntPtr obj) {
      var result = StringFromNative(ovr_GroupPresenceLeaveIntent_GetDestinationApiName_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_GroupPresenceLeaveIntent_GetDestinationApiName")]
    private static extern IntPtr ovr_GroupPresenceLeaveIntent_GetDestinationApiName_Native(IntPtr obj);

    public static string ovr_GroupPresenceLeaveIntent_GetLobbySessionId(IntPtr obj) {
      var result = StringFromNative(ovr_GroupPresenceLeaveIntent_GetLobbySessionId_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_GroupPresenceLeaveIntent_GetLobbySessionId")]
    private static extern IntPtr ovr_GroupPresenceLeaveIntent_GetLobbySessionId_Native(IntPtr obj);

    public static string ovr_GroupPresenceLeaveIntent_GetMatchSessionId(IntPtr obj) {
      var result = StringFromNative(ovr_GroupPresenceLeaveIntent_GetMatchSessionId_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_GroupPresenceLeaveIntent_GetMatchSessionId")]
    private static extern IntPtr ovr_GroupPresenceLeaveIntent_GetMatchSessionId_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_HttpTransferUpdate_GetBytes(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_HttpTransferUpdate_GetID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_HttpTransferUpdate_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_HttpTransferUpdate_IsCompleted(IntPtr obj);

    public static string ovr_InstalledApplication_GetApplicationId(IntPtr obj) {
      var result = StringFromNative(ovr_InstalledApplication_GetApplicationId_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_InstalledApplication_GetApplicationId")]
    private static extern IntPtr ovr_InstalledApplication_GetApplicationId_Native(IntPtr obj);

    public static string ovr_InstalledApplication_GetPackageName(IntPtr obj) {
      var result = StringFromNative(ovr_InstalledApplication_GetPackageName_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_InstalledApplication_GetPackageName")]
    private static extern IntPtr ovr_InstalledApplication_GetPackageName_Native(IntPtr obj);

    public static string ovr_InstalledApplication_GetStatus(IntPtr obj) {
      var result = StringFromNative(ovr_InstalledApplication_GetStatus_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_InstalledApplication_GetStatus")]
    private static extern IntPtr ovr_InstalledApplication_GetStatus_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern int ovr_InstalledApplication_GetVersionCode(IntPtr obj);

    public static string ovr_InstalledApplication_GetVersionName(IntPtr obj) {
      var result = StringFromNative(ovr_InstalledApplication_GetVersionName_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_InstalledApplication_GetVersionName")]
    private static extern IntPtr ovr_InstalledApplication_GetVersionName_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_InstalledApplicationArray_GetElement(IntPtr obj, UIntPtr index);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_InstalledApplicationArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_InvitePanelResultInfo_GetInvitesSent(IntPtr obj);

    public static string ovr_LanguagePackInfo_GetEnglishName(IntPtr obj) {
      var result = StringFromNative(ovr_LanguagePackInfo_GetEnglishName_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_LanguagePackInfo_GetEnglishName")]
    private static extern IntPtr ovr_LanguagePackInfo_GetEnglishName_Native(IntPtr obj);

    public static string ovr_LanguagePackInfo_GetNativeName(IntPtr obj) {
      var result = StringFromNative(ovr_LanguagePackInfo_GetNativeName_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_LanguagePackInfo_GetNativeName")]
    private static extern IntPtr ovr_LanguagePackInfo_GetNativeName_Native(IntPtr obj);

    public static string ovr_LanguagePackInfo_GetTag(IntPtr obj) {
      var result = StringFromNative(ovr_LanguagePackInfo_GetTag_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_LanguagePackInfo_GetTag")]
    private static extern IntPtr ovr_LanguagePackInfo_GetTag_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_LaunchBlockFlowResult_GetDidBlock(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_LaunchBlockFlowResult_GetDidCancel(IntPtr obj);

    public static string ovr_LaunchDetails_GetDeeplinkMessage(IntPtr obj) {
      var result = StringFromNative(ovr_LaunchDetails_GetDeeplinkMessage_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_LaunchDetails_GetDeeplinkMessage")]
    private static extern IntPtr ovr_LaunchDetails_GetDeeplinkMessage_Native(IntPtr obj);

    public static string ovr_LaunchDetails_GetDestinationApiName(IntPtr obj) {
      var result = StringFromNative(ovr_LaunchDetails_GetDestinationApiName_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_LaunchDetails_GetDestinationApiName")]
    private static extern IntPtr ovr_LaunchDetails_GetDestinationApiName_Native(IntPtr obj);

    public static string ovr_LaunchDetails_GetLaunchSource(IntPtr obj) {
      var result = StringFromNative(ovr_LaunchDetails_GetLaunchSource_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_LaunchDetails_GetLaunchSource")]
    private static extern IntPtr ovr_LaunchDetails_GetLaunchSource_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern LaunchType ovr_LaunchDetails_GetLaunchType(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_LaunchDetails_GetRoomID(IntPtr obj);

    public static string ovr_LaunchDetails_GetTrackingID(IntPtr obj) {
      var result = StringFromNative(ovr_LaunchDetails_GetTrackingID_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_LaunchDetails_GetTrackingID")]
    private static extern IntPtr ovr_LaunchDetails_GetTrackingID_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_LaunchDetails_GetUsers(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_LaunchFriendRequestFlowResult_GetDidCancel(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_LaunchFriendRequestFlowResult_GetDidSendRequest(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_LaunchInvitePanelFlowResult_GetInvitedUsers(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_LaunchReportFlowResult_GetDidCancel(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_LaunchReportFlowResult_GetUserReportId(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_LaunchUnblockFlowResult_GetDidCancel(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_LaunchUnblockFlowResult_GetDidUnblock(IntPtr obj);

    public static string ovr_Leaderboard_GetApiName(IntPtr obj) {
      var result = StringFromNative(ovr_Leaderboard_GetApiName_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Leaderboard_GetApiName")]
    private static extern IntPtr ovr_Leaderboard_GetApiName_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Leaderboard_GetDestination(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_Leaderboard_GetID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_LeaderboardArray_GetElement(IntPtr obj, UIntPtr index);

    public static string ovr_LeaderboardArray_GetNextUrl(IntPtr obj) {
      var result = StringFromNative(ovr_LeaderboardArray_GetNextUrl_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_LeaderboardArray_GetNextUrl")]
    private static extern IntPtr ovr_LeaderboardArray_GetNextUrl_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_LeaderboardArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_LeaderboardArray_HasNextPage(IntPtr obj);

    public static string ovr_LeaderboardEntry_GetDisplayScore(IntPtr obj) {
      var result = StringFromNative(ovr_LeaderboardEntry_GetDisplayScore_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_LeaderboardEntry_GetDisplayScore")]
    private static extern IntPtr ovr_LeaderboardEntry_GetDisplayScore_Native(IntPtr obj);

    public static byte[] ovr_LeaderboardEntry_GetExtraData(IntPtr obj) {
      var result = BlobFromNative(ovr_LeaderboardEntry_GetExtraDataLength(obj), ovr_LeaderboardEntry_GetExtraData_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_LeaderboardEntry_GetExtraData")]
    private static extern IntPtr ovr_LeaderboardEntry_GetExtraData_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern uint ovr_LeaderboardEntry_GetExtraDataLength(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_LeaderboardEntry_GetID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern int ovr_LeaderboardEntry_GetRank(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern long ovr_LeaderboardEntry_GetScore(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_LeaderboardEntry_GetSupplementaryMetric(IntPtr obj);

    public static DateTime ovr_LeaderboardEntry_GetTimestamp(IntPtr obj) {
      var result = DateTimeFromNative(ovr_LeaderboardEntry_GetTimestamp_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_LeaderboardEntry_GetTimestamp")]
    private static extern ulong ovr_LeaderboardEntry_GetTimestamp_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_LeaderboardEntry_GetUser(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_LeaderboardEntryArray_GetElement(IntPtr obj, UIntPtr index);

    public static string ovr_LeaderboardEntryArray_GetNextUrl(IntPtr obj) {
      var result = StringFromNative(ovr_LeaderboardEntryArray_GetNextUrl_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_LeaderboardEntryArray_GetNextUrl")]
    private static extern IntPtr ovr_LeaderboardEntryArray_GetNextUrl_Native(IntPtr obj);

    public static string ovr_LeaderboardEntryArray_GetPreviousUrl(IntPtr obj) {
      var result = StringFromNative(ovr_LeaderboardEntryArray_GetPreviousUrl_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_LeaderboardEntryArray_GetPreviousUrl")]
    private static extern IntPtr ovr_LeaderboardEntryArray_GetPreviousUrl_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_LeaderboardEntryArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_LeaderboardEntryArray_GetTotalCount(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_LeaderboardEntryArray_HasNextPage(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_LeaderboardEntryArray_HasPreviousPage(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_LeaderboardUpdateStatus_GetDidUpdate(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_LeaderboardUpdateStatus_GetUpdatedChallengeId(IntPtr obj, uint index);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern uint ovr_LeaderboardUpdateStatus_GetUpdatedChallengeIdsSize(IntPtr obj);

    public static string ovr_LinkedAccount_GetAccessToken(IntPtr obj) {
      var result = StringFromNative(ovr_LinkedAccount_GetAccessToken_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_LinkedAccount_GetAccessToken")]
    private static extern IntPtr ovr_LinkedAccount_GetAccessToken_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ServiceProvider ovr_LinkedAccount_GetServiceProvider(IntPtr obj);

    public static string ovr_LinkedAccount_GetUserId(IntPtr obj) {
      var result = StringFromNative(ovr_LinkedAccount_GetUserId_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_LinkedAccount_GetUserId")]
    private static extern IntPtr ovr_LinkedAccount_GetUserId_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_LinkedAccountArray_GetElement(IntPtr obj, UIntPtr index);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_LinkedAccountArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_LivestreamingApplicationStatus_GetStreamingEnabled(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern LivestreamingStartStatus ovr_LivestreamingStartResult_GetStreamingResult(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_LivestreamingStatus_GetCommentsVisible(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_LivestreamingStatus_GetIsPaused(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_LivestreamingStatus_GetLivestreamingEnabled(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern int ovr_LivestreamingStatus_GetLivestreamingType(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_LivestreamingStatus_GetMicEnabled(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern int ovr_LivestreamingVideoStats_GetCommentCount(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern int ovr_LivestreamingVideoStats_GetReactionCount(IntPtr obj);

    public static string ovr_LivestreamingVideoStats_GetTotalViews(IntPtr obj) {
      var result = StringFromNative(ovr_LivestreamingVideoStats_GetTotalViews_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_LivestreamingVideoStats_GetTotalViews")]
    private static extern IntPtr ovr_LivestreamingVideoStats_GetTotalViews_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_MatchmakingAdminSnapshot_GetCandidates(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern double ovr_MatchmakingAdminSnapshot_GetMyCurrentThreshold(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_MatchmakingAdminSnapshotCandidate_GetCanMatch(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern double ovr_MatchmakingAdminSnapshotCandidate_GetMyTotalScore(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern double ovr_MatchmakingAdminSnapshotCandidate_GetTheirCurrentThreshold(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern double ovr_MatchmakingAdminSnapshotCandidate_GetTheirTotalScore(IntPtr obj);

    public static string ovr_MatchmakingAdminSnapshotCandidate_GetTraceId(IntPtr obj) {
      var result = StringFromNative(ovr_MatchmakingAdminSnapshotCandidate_GetTraceId_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_MatchmakingAdminSnapshotCandidate_GetTraceId")]
    private static extern IntPtr ovr_MatchmakingAdminSnapshotCandidate_GetTraceId_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_MatchmakingAdminSnapshotCandidateArray_GetElement(IntPtr obj, UIntPtr index);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_MatchmakingAdminSnapshotCandidateArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_MatchmakingBrowseResult_GetEnqueueResult(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_MatchmakingBrowseResult_GetRooms(IntPtr obj);

    public static string ovr_MatchmakingCandidate_GetEntryHash(IntPtr obj) {
      var result = StringFromNative(ovr_MatchmakingCandidate_GetEntryHash_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_MatchmakingCandidate_GetEntryHash")]
    private static extern IntPtr ovr_MatchmakingCandidate_GetEntryHash_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_MatchmakingCandidate_GetUserId(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_MatchmakingCandidateArray_GetElement(IntPtr obj, UIntPtr index);

    public static string ovr_MatchmakingCandidateArray_GetNextUrl(IntPtr obj) {
      var result = StringFromNative(ovr_MatchmakingCandidateArray_GetNextUrl_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_MatchmakingCandidateArray_GetNextUrl")]
    private static extern IntPtr ovr_MatchmakingCandidateArray_GetNextUrl_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_MatchmakingCandidateArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_MatchmakingCandidateArray_HasNextPage(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_MatchmakingEnqueueResult_GetAdminSnapshot(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern uint ovr_MatchmakingEnqueueResult_GetAverageWait(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern uint ovr_MatchmakingEnqueueResult_GetMatchesInLastHourCount(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern uint ovr_MatchmakingEnqueueResult_GetMaxExpectedWait(IntPtr obj);

    public static string ovr_MatchmakingEnqueueResult_GetPool(IntPtr obj) {
      var result = StringFromNative(ovr_MatchmakingEnqueueResult_GetPool_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_MatchmakingEnqueueResult_GetPool")]
    private static extern IntPtr ovr_MatchmakingEnqueueResult_GetPool_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern uint ovr_MatchmakingEnqueueResult_GetRecentMatchPercentage(IntPtr obj);

    public static string ovr_MatchmakingEnqueueResult_GetRequestHash(IntPtr obj) {
      var result = StringFromNative(ovr_MatchmakingEnqueueResult_GetRequestHash_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_MatchmakingEnqueueResult_GetRequestHash")]
    private static extern IntPtr ovr_MatchmakingEnqueueResult_GetRequestHash_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_MatchmakingEnqueueResultAndRoom_GetMatchmakingEnqueueResult(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_MatchmakingEnqueueResultAndRoom_GetRoom(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_MatchmakingEnqueuedUser_GetAdditionalUserID(IntPtr obj, uint index);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern uint ovr_MatchmakingEnqueuedUser_GetAdditionalUserIDsSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_MatchmakingEnqueuedUser_GetCustomData(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_MatchmakingEnqueuedUser_GetUser(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_MatchmakingEnqueuedUserArray_GetElement(IntPtr obj, UIntPtr index);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_MatchmakingEnqueuedUserArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_MatchmakingNotification_GetAddedByUserId(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_MatchmakingNotification_GetRoom(IntPtr obj);

    public static string ovr_MatchmakingNotification_GetTraceId(IntPtr obj) {
      var result = StringFromNative(ovr_MatchmakingNotification_GetTraceId_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_MatchmakingNotification_GetTraceId")]
    private static extern IntPtr ovr_MatchmakingNotification_GetTraceId_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern uint ovr_MatchmakingRoom_GetPingTime(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_MatchmakingRoom_GetRoom(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_MatchmakingRoom_HasPingTime(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_MatchmakingRoomArray_GetElement(IntPtr obj, UIntPtr index);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_MatchmakingRoomArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern uint ovr_MatchmakingStats_GetDrawCount(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern uint ovr_MatchmakingStats_GetLossCount(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern uint ovr_MatchmakingStats_GetSkillLevel(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern double ovr_MatchmakingStats_GetSkillMean(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern double ovr_MatchmakingStats_GetSkillStandardDeviation(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern uint ovr_MatchmakingStats_GetWinCount(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetAbuseReportRecording(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetAchievementDefinitionArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetAchievementProgressArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetAchievementUpdate(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetApplicationInviteArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetApplicationVersion(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetAssetDetails(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetAssetDetailsArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetAssetFileDeleteResult(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetAssetFileDownloadCancelResult(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetAssetFileDownloadResult(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetAssetFileDownloadUpdate(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetBlockedUserArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetCalApplicationFinalized(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetCalApplicationProposed(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetCalApplicationSuggestionArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetChallenge(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetChallengeArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetChallengeEntryArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetCloudStorageConflictMetadata(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetCloudStorageData(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetCloudStorageMetadata(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetCloudStorageMetadataArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetCloudStorageUpdateResponse(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetDataStore(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetDestinationArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetError(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetGroupPresenceJoinIntent(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetGroupPresenceLeaveIntent(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetHttpTransferUpdate(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetInstalledApplicationArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetInvitePanelResultInfo(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetLaunchBlockFlowResult(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetLaunchFriendRequestFlowResult(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetLaunchInvitePanelFlowResult(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetLaunchReportFlowResult(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetLaunchUnblockFlowResult(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetLeaderboardArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetLeaderboardEntryArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetLeaderboardUpdateStatus(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetLinkedAccountArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetLivestreamingApplicationStatus(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetLivestreamingStartResult(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetLivestreamingStatus(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetLivestreamingVideoStats(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetMatchmakingAdminSnapshot(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetMatchmakingBrowseResult(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetMatchmakingEnqueueResult(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetMatchmakingEnqueueResultAndRoom(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetMatchmakingRoomArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetMatchmakingStats(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetMicrophoneAvailabilityState(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetNativeMessage(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetNetSyncConnection(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetNetSyncSessionArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetNetSyncSessionsChangedNotification(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetNetSyncSetSessionPropertyResult(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetNetSyncVoipAttenuationValueArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetNetworkingPeer(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetOrgScopedID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetParty(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetPartyID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetPartyUpdateNotification(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetPidArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetPingResult(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetPlatformInitialize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetProductArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetPurchase(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetPurchaseArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetRejoinDialogResult(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_Message_GetRequestID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetRoom(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetRoomArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetRoomInviteNotification(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetRoomInviteNotificationArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetSdkAccountArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetSendInvitesResult(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetShareMediaResult(IntPtr obj);

    public static string ovr_Message_GetString(IntPtr obj) {
      var result = StringFromNative(ovr_Message_GetString_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Message_GetString")]
    private static extern IntPtr ovr_Message_GetString_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetSystemVoipState(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern Message.MessageType ovr_Message_GetType(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetUser(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetUserAndRoomArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetUserArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetUserCapabilityArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetUserDataStoreUpdateResponse(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetUserProof(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Message_GetUserReportID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_Message_IsError(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_Microphone_GetNumSamplesAvailable(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_Microphone_GetOutputBufferMaxSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_Microphone_GetPCM(IntPtr obj, Int16[] outputBuffer, UIntPtr outputBufferNumElements);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_Microphone_GetPCMFloat(IntPtr obj, float[] outputBuffer, UIntPtr outputBufferNumElements);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_Microphone_ReadData(IntPtr obj, float[] outputBuffer, UIntPtr outputBufferSize);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_Microphone_SetAcceptableRecordingDelayHint(IntPtr obj, UIntPtr delayMs);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_Microphone_Start(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_Microphone_Stop(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_MicrophoneAvailabilityState_GetMicrophoneAvailable(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern long ovr_NetSyncConnection_GetConnectionId(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern NetSyncDisconnectReason ovr_NetSyncConnection_GetDisconnectReason(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_NetSyncConnection_GetSessionId(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern NetSyncConnectionStatus ovr_NetSyncConnection_GetStatus(IntPtr obj);

    public static string ovr_NetSyncConnection_GetZoneId(IntPtr obj) {
      var result = StringFromNative(ovr_NetSyncConnection_GetZoneId_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_NetSyncConnection_GetZoneId")]
    private static extern IntPtr ovr_NetSyncConnection_GetZoneId_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern long ovr_NetSyncSession_GetConnectionId(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_NetSyncSession_GetMuted(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_NetSyncSession_GetSessionId(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_NetSyncSession_GetUserId(IntPtr obj);

    public static string ovr_NetSyncSession_GetVoipGroup(IntPtr obj) {
      var result = StringFromNative(ovr_NetSyncSession_GetVoipGroup_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_NetSyncSession_GetVoipGroup")]
    private static extern IntPtr ovr_NetSyncSession_GetVoipGroup_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_NetSyncSessionArray_GetElement(IntPtr obj, UIntPtr index);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_NetSyncSessionArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern long ovr_NetSyncSessionsChangedNotification_GetConnectionId(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_NetSyncSessionsChangedNotification_GetSessions(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_NetSyncSetSessionPropertyResult_GetSession(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern float ovr_NetSyncVoipAttenuationValue_GetDecibels(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern float ovr_NetSyncVoipAttenuationValue_GetDistance(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_NetSyncVoipAttenuationValueArray_GetElement(IntPtr obj, UIntPtr index);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_NetSyncVoipAttenuationValueArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_NetworkingPeer_GetID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern PeerConnectionState ovr_NetworkingPeer_GetState(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_OrgScopedID_GetID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_Packet_Free(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Packet_GetBytes(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern SendPolicy ovr_Packet_GetSendPolicy(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_Packet_GetSenderID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_Packet_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_Party_GetID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Party_GetInvitedUsers(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Party_GetLeader(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Party_GetRoom(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Party_GetUsers(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_PartyID_GetID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern PartyUpdateAction ovr_PartyUpdateNotification_GetAction(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_PartyUpdateNotification_GetPartyId(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_PartyUpdateNotification_GetSenderId(IntPtr obj);

    public static string ovr_PartyUpdateNotification_GetUpdateTimestamp(IntPtr obj) {
      var result = StringFromNative(ovr_PartyUpdateNotification_GetUpdateTimestamp_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_PartyUpdateNotification_GetUpdateTimestamp")]
    private static extern IntPtr ovr_PartyUpdateNotification_GetUpdateTimestamp_Native(IntPtr obj);

    public static string ovr_PartyUpdateNotification_GetUserAlias(IntPtr obj) {
      var result = StringFromNative(ovr_PartyUpdateNotification_GetUserAlias_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_PartyUpdateNotification_GetUserAlias")]
    private static extern IntPtr ovr_PartyUpdateNotification_GetUserAlias_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_PartyUpdateNotification_GetUserId(IntPtr obj);

    public static string ovr_PartyUpdateNotification_GetUserName(IntPtr obj) {
      var result = StringFromNative(ovr_PartyUpdateNotification_GetUserName_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_PartyUpdateNotification_GetUserName")]
    private static extern IntPtr ovr_PartyUpdateNotification_GetUserName_Native(IntPtr obj);

    public static string ovr_Pid_GetId(IntPtr obj) {
      var result = StringFromNative(ovr_Pid_GetId_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Pid_GetId")]
    private static extern IntPtr ovr_Pid_GetId_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_PidArray_GetElement(IntPtr obj, UIntPtr index);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_PidArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_PingResult_GetID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ulong ovr_PingResult_GetPingTimeUsec(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_PingResult_IsTimeout(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern PlatformInitializeResult ovr_PlatformInitialize_GetResult(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern uint ovr_Price_GetAmountInHundredths(IntPtr obj);

    public static string ovr_Price_GetCurrency(IntPtr obj) {
      var result = StringFromNative(ovr_Price_GetCurrency_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Price_GetCurrency")]
    private static extern IntPtr ovr_Price_GetCurrency_Native(IntPtr obj);

    public static string ovr_Price_GetFormatted(IntPtr obj) {
      var result = StringFromNative(ovr_Price_GetFormatted_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Price_GetFormatted")]
    private static extern IntPtr ovr_Price_GetFormatted_Native(IntPtr obj);

    public static string ovr_Product_GetDescription(IntPtr obj) {
      var result = StringFromNative(ovr_Product_GetDescription_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Product_GetDescription")]
    private static extern IntPtr ovr_Product_GetDescription_Native(IntPtr obj);

    public static string ovr_Product_GetFormattedPrice(IntPtr obj) {
      var result = StringFromNative(ovr_Product_GetFormattedPrice_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Product_GetFormattedPrice")]
    private static extern IntPtr ovr_Product_GetFormattedPrice_Native(IntPtr obj);

    public static string ovr_Product_GetName(IntPtr obj) {
      var result = StringFromNative(ovr_Product_GetName_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Product_GetName")]
    private static extern IntPtr ovr_Product_GetName_Native(IntPtr obj);

    public static string ovr_Product_GetSKU(IntPtr obj) {
      var result = StringFromNative(ovr_Product_GetSKU_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Product_GetSKU")]
    private static extern IntPtr ovr_Product_GetSKU_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_ProductArray_GetElement(IntPtr obj, UIntPtr index);

    public static string ovr_ProductArray_GetNextUrl(IntPtr obj) {
      var result = StringFromNative(ovr_ProductArray_GetNextUrl_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_ProductArray_GetNextUrl")]
    private static extern IntPtr ovr_ProductArray_GetNextUrl_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_ProductArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_ProductArray_HasNextPage(IntPtr obj);

    public static DateTime ovr_Purchase_GetExpirationTime(IntPtr obj) {
      var result = DateTimeFromNative(ovr_Purchase_GetExpirationTime_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Purchase_GetExpirationTime")]
    private static extern ulong ovr_Purchase_GetExpirationTime_Native(IntPtr obj);

    public static DateTime ovr_Purchase_GetGrantTime(IntPtr obj) {
      var result = DateTimeFromNative(ovr_Purchase_GetGrantTime_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Purchase_GetGrantTime")]
    private static extern ulong ovr_Purchase_GetGrantTime_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_Purchase_GetPurchaseID(IntPtr obj);

    public static string ovr_Purchase_GetPurchaseStrID(IntPtr obj) {
      var result = StringFromNative(ovr_Purchase_GetPurchaseStrID_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Purchase_GetPurchaseStrID")]
    private static extern IntPtr ovr_Purchase_GetPurchaseStrID_Native(IntPtr obj);

    public static string ovr_Purchase_GetSKU(IntPtr obj) {
      var result = StringFromNative(ovr_Purchase_GetSKU_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Purchase_GetSKU")]
    private static extern IntPtr ovr_Purchase_GetSKU_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_PurchaseArray_GetElement(IntPtr obj, UIntPtr index);

    public static string ovr_PurchaseArray_GetNextUrl(IntPtr obj) {
      var result = StringFromNative(ovr_PurchaseArray_GetNextUrl_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_PurchaseArray_GetNextUrl")]
    private static extern IntPtr ovr_PurchaseArray_GetNextUrl_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_PurchaseArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_PurchaseArray_HasNextPage(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_RejoinDialogResult_GetRejoinSelected(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_Room_GetApplicationID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Room_GetDataStore(IntPtr obj);

    public static string ovr_Room_GetDescription(IntPtr obj) {
      var result = StringFromNative(ovr_Room_GetDescription_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Room_GetDescription")]
    private static extern IntPtr ovr_Room_GetDescription_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_Room_GetID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Room_GetInvitedUsers(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_Room_GetIsMembershipLocked(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern RoomJoinPolicy ovr_Room_GetJoinPolicy(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern RoomJoinability ovr_Room_GetJoinability(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Room_GetMatchedUsers(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern uint ovr_Room_GetMaxUsers(IntPtr obj);

    public static string ovr_Room_GetName(IntPtr obj) {
      var result = StringFromNative(ovr_Room_GetName_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Room_GetName")]
    private static extern IntPtr ovr_Room_GetName_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Room_GetOwner(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Room_GetTeams(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern RoomType ovr_Room_GetType(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Room_GetUsers(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern uint ovr_Room_GetVersion(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_RoomArray_GetElement(IntPtr obj, UIntPtr index);

    public static string ovr_RoomArray_GetNextUrl(IntPtr obj) {
      var result = StringFromNative(ovr_RoomArray_GetNextUrl_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_RoomArray_GetNextUrl")]
    private static extern IntPtr ovr_RoomArray_GetNextUrl_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_RoomArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_RoomArray_HasNextPage(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_RoomInviteNotification_GetID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_RoomInviteNotification_GetRoomID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_RoomInviteNotification_GetSenderID(IntPtr obj);

    public static DateTime ovr_RoomInviteNotification_GetSentTime(IntPtr obj) {
      var result = DateTimeFromNative(ovr_RoomInviteNotification_GetSentTime_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_RoomInviteNotification_GetSentTime")]
    private static extern ulong ovr_RoomInviteNotification_GetSentTime_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_RoomInviteNotificationArray_GetElement(IntPtr obj, UIntPtr index);

    public static string ovr_RoomInviteNotificationArray_GetNextUrl(IntPtr obj) {
      var result = StringFromNative(ovr_RoomInviteNotificationArray_GetNextUrl_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_RoomInviteNotificationArray_GetNextUrl")]
    private static extern IntPtr ovr_RoomInviteNotificationArray_GetNextUrl_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_RoomInviteNotificationArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_RoomInviteNotificationArray_HasNextPage(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern SdkAccountType ovr_SdkAccount_GetAccountType(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_SdkAccount_GetUserId(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_SdkAccountArray_GetElement(IntPtr obj, UIntPtr index);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_SdkAccountArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_SendInvitesResult_GetInvites(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern ShareMediaStatus ovr_ShareMediaResult_GetStatus(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_SupplementaryMetric_GetID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern long ovr_SupplementaryMetric_GetMetric(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern VoipMuteState ovr_SystemVoipState_GetMicrophoneMuted(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern SystemVoipStatus ovr_SystemVoipState_GetStatus(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_Team_GetAssignedUsers(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern int ovr_Team_GetMaxUsers(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern int ovr_Team_GetMinUsers(IntPtr obj);

    public static string ovr_Team_GetName(IntPtr obj) {
      var result = StringFromNative(ovr_Team_GetName_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_Team_GetName")]
    private static extern IntPtr ovr_Team_GetName_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_TeamArray_GetElement(IntPtr obj, UIntPtr index);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_TeamArray_GetSize(IntPtr obj);

    public static string ovr_TestUser_GetAccessToken(IntPtr obj) {
      var result = StringFromNative(ovr_TestUser_GetAccessToken_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_TestUser_GetAccessToken")]
    private static extern IntPtr ovr_TestUser_GetAccessToken_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_TestUser_GetAppAccessArray(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_TestUser_GetFbAppAccessArray(IntPtr obj);

    public static string ovr_TestUser_GetFriendAccessToken(IntPtr obj) {
      var result = StringFromNative(ovr_TestUser_GetFriendAccessToken_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_TestUser_GetFriendAccessToken")]
    private static extern IntPtr ovr_TestUser_GetFriendAccessToken_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_TestUser_GetFriendAppAccessArray(IntPtr obj);

    public static string ovr_TestUser_GetUserAlias(IntPtr obj) {
      var result = StringFromNative(ovr_TestUser_GetUserAlias_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_TestUser_GetUserAlias")]
    private static extern IntPtr ovr_TestUser_GetUserAlias_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_TestUser_GetUserFbid(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_TestUser_GetUserId(IntPtr obj);

    public static string ovr_TestUserAppAccess_GetAccessToken(IntPtr obj) {
      var result = StringFromNative(ovr_TestUserAppAccess_GetAccessToken_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_TestUserAppAccess_GetAccessToken")]
    private static extern IntPtr ovr_TestUserAppAccess_GetAccessToken_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_TestUserAppAccess_GetAppId(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_TestUserAppAccess_GetUserId(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_TestUserAppAccessArray_GetElement(IntPtr obj, UIntPtr index);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_TestUserAppAccessArray_GetSize(IntPtr obj);

    public static string ovr_User_GetDisplayName(IntPtr obj) {
      var result = StringFromNative(ovr_User_GetDisplayName_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_User_GetDisplayName")]
    private static extern IntPtr ovr_User_GetDisplayName_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_User_GetID(IntPtr obj);

    public static string ovr_User_GetImageUrl(IntPtr obj) {
      var result = StringFromNative(ovr_User_GetImageUrl_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_User_GetImageUrl")]
    private static extern IntPtr ovr_User_GetImageUrl_Native(IntPtr obj);

    public static string ovr_User_GetInviteToken(IntPtr obj) {
      var result = StringFromNative(ovr_User_GetInviteToken_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_User_GetInviteToken")]
    private static extern IntPtr ovr_User_GetInviteToken_Native(IntPtr obj);

    public static string ovr_User_GetOculusID(IntPtr obj) {
      var result = StringFromNative(ovr_User_GetOculusID_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_User_GetOculusID")]
    private static extern IntPtr ovr_User_GetOculusID_Native(IntPtr obj);

    public static string ovr_User_GetPresence(IntPtr obj) {
      var result = StringFromNative(ovr_User_GetPresence_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_User_GetPresence")]
    private static extern IntPtr ovr_User_GetPresence_Native(IntPtr obj);

    public static string ovr_User_GetPresenceDeeplinkMessage(IntPtr obj) {
      var result = StringFromNative(ovr_User_GetPresenceDeeplinkMessage_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_User_GetPresenceDeeplinkMessage")]
    private static extern IntPtr ovr_User_GetPresenceDeeplinkMessage_Native(IntPtr obj);

    public static string ovr_User_GetPresenceDestinationApiName(IntPtr obj) {
      var result = StringFromNative(ovr_User_GetPresenceDestinationApiName_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_User_GetPresenceDestinationApiName")]
    private static extern IntPtr ovr_User_GetPresenceDestinationApiName_Native(IntPtr obj);

    public static string ovr_User_GetPresenceLobbySessionId(IntPtr obj) {
      var result = StringFromNative(ovr_User_GetPresenceLobbySessionId_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_User_GetPresenceLobbySessionId")]
    private static extern IntPtr ovr_User_GetPresenceLobbySessionId_Native(IntPtr obj);

    public static string ovr_User_GetPresenceMatchSessionId(IntPtr obj) {
      var result = StringFromNative(ovr_User_GetPresenceMatchSessionId_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_User_GetPresenceMatchSessionId")]
    private static extern IntPtr ovr_User_GetPresenceMatchSessionId_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UserPresenceStatus ovr_User_GetPresenceStatus(IntPtr obj);

    public static string ovr_User_GetSmallImageUrl(IntPtr obj) {
      var result = StringFromNative(ovr_User_GetSmallImageUrl_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_User_GetSmallImageUrl")]
    private static extern IntPtr ovr_User_GetSmallImageUrl_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_UserAndRoom_GetRoom(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_UserAndRoom_GetUser(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_UserAndRoomArray_GetElement(IntPtr obj, UIntPtr index);

    public static string ovr_UserAndRoomArray_GetNextUrl(IntPtr obj) {
      var result = StringFromNative(ovr_UserAndRoomArray_GetNextUrl_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_UserAndRoomArray_GetNextUrl")]
    private static extern IntPtr ovr_UserAndRoomArray_GetNextUrl_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_UserAndRoomArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_UserAndRoomArray_HasNextPage(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_UserArray_GetElement(IntPtr obj, UIntPtr index);

    public static string ovr_UserArray_GetNextUrl(IntPtr obj) {
      var result = StringFromNative(ovr_UserArray_GetNextUrl_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_UserArray_GetNextUrl")]
    private static extern IntPtr ovr_UserArray_GetNextUrl_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_UserArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_UserArray_HasNextPage(IntPtr obj);

    public static string ovr_UserCapability_GetDescription(IntPtr obj) {
      var result = StringFromNative(ovr_UserCapability_GetDescription_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_UserCapability_GetDescription")]
    private static extern IntPtr ovr_UserCapability_GetDescription_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_UserCapability_GetIsEnabled(IntPtr obj);

    public static string ovr_UserCapability_GetName(IntPtr obj) {
      var result = StringFromNative(ovr_UserCapability_GetName_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_UserCapability_GetName")]
    private static extern IntPtr ovr_UserCapability_GetName_Native(IntPtr obj);

    public static string ovr_UserCapability_GetReasonCode(IntPtr obj) {
      var result = StringFromNative(ovr_UserCapability_GetReasonCode_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_UserCapability_GetReasonCode")]
    private static extern IntPtr ovr_UserCapability_GetReasonCode_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_UserCapabilityArray_GetElement(IntPtr obj, UIntPtr index);

    public static string ovr_UserCapabilityArray_GetNextUrl(IntPtr obj) {
      var result = StringFromNative(ovr_UserCapabilityArray_GetNextUrl_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_UserCapabilityArray_GetNextUrl")]
    private static extern IntPtr ovr_UserCapabilityArray_GetNextUrl_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_UserCapabilityArray_GetSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_UserCapabilityArray_HasNextPage(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_UserDataStoreUpdateResponse_GetSuccess(IntPtr obj);

    public static string ovr_UserProof_GetNonce(IntPtr obj) {
      var result = StringFromNative(ovr_UserProof_GetNonce_Native(obj));
      return result;
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_UserProof_GetNonce")]
    private static extern IntPtr ovr_UserProof_GetNonce_Native(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern bool ovr_UserReportID_GetDidCancel(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UInt64 ovr_UserReportID_GetID(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_VoipDecoder_Decode(IntPtr obj, byte[] compressedData, UIntPtr compressedSize);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_VoipDecoder_GetDecodedPCM(IntPtr obj, float[] outputBuffer, UIntPtr outputBufferSize);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_VoipEncoder_AddPCM(IntPtr obj, float[] inputData, uint inputSize);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_VoipEncoder_GetCompressedData(IntPtr obj, byte[] outputBuffer, UIntPtr intputSize);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern UIntPtr ovr_VoipEncoder_GetCompressedDataSize(IntPtr obj);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_AbuseReportOptions_Create();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_AbuseReportOptions_Destroy(IntPtr handle);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_AbuseReportOptions_SetPreventPeopleChooser(IntPtr handle, bool value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_AbuseReportOptions_SetReportType(IntPtr handle, AbuseReportType value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_AdvancedAbuseReportOptions_Create();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_AdvancedAbuseReportOptions_Destroy(IntPtr handle);

    public static void ovr_AdvancedAbuseReportOptions_SetDeveloperDefinedContextString(IntPtr handle, string key, string value) {
      IntPtr key_native = StringToNative(key);
      IntPtr value_native = StringToNative(value);
      ovr_AdvancedAbuseReportOptions_SetDeveloperDefinedContextString_Native(handle, key_native, value_native);
      Marshal.FreeCoTaskMem(key_native);
      Marshal.FreeCoTaskMem(value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_AdvancedAbuseReportOptions_SetDeveloperDefinedContextString")]
    private static extern void ovr_AdvancedAbuseReportOptions_SetDeveloperDefinedContextString_Native(IntPtr handle, IntPtr key, IntPtr value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_AdvancedAbuseReportOptions_ClearDeveloperDefinedContext(IntPtr handle);

    public static void ovr_AdvancedAbuseReportOptions_SetObjectType(IntPtr handle, string value) {
      IntPtr value_native = StringToNative(value);
      ovr_AdvancedAbuseReportOptions_SetObjectType_Native(handle, value_native);
      Marshal.FreeCoTaskMem(value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_AdvancedAbuseReportOptions_SetObjectType")]
    private static extern void ovr_AdvancedAbuseReportOptions_SetObjectType_Native(IntPtr handle, IntPtr value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_AdvancedAbuseReportOptions_SetReportType(IntPtr handle, AbuseReportType value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_AdvancedAbuseReportOptions_SetVideoMode(IntPtr handle, AbuseReportVideoMode value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_ApplicationOptions_Create();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_ApplicationOptions_Destroy(IntPtr handle);

    public static void ovr_ApplicationOptions_SetDeeplinkMessage(IntPtr handle, string value) {
      IntPtr value_native = StringToNative(value);
      ovr_ApplicationOptions_SetDeeplinkMessage_Native(handle, value_native);
      Marshal.FreeCoTaskMem(value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_ApplicationOptions_SetDeeplinkMessage")]
    private static extern void ovr_ApplicationOptions_SetDeeplinkMessage_Native(IntPtr handle, IntPtr value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_ChallengeOptions_Create();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_ChallengeOptions_Destroy(IntPtr handle);

    public static void ovr_ChallengeOptions_SetDescription(IntPtr handle, string value) {
      IntPtr value_native = StringToNative(value);
      ovr_ChallengeOptions_SetDescription_Native(handle, value_native);
      Marshal.FreeCoTaskMem(value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_ChallengeOptions_SetDescription")]
    private static extern void ovr_ChallengeOptions_SetDescription_Native(IntPtr handle, IntPtr value);

    public static void ovr_ChallengeOptions_SetEndDate(IntPtr handle, DateTime value) {
      ulong value_native = DateTimeToNative(value);
      ovr_ChallengeOptions_SetEndDate_Native(handle, value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_ChallengeOptions_SetEndDate")]
    private static extern void ovr_ChallengeOptions_SetEndDate_Native(IntPtr handle, ulong value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_ChallengeOptions_SetIncludeActiveChallenges(IntPtr handle, bool value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_ChallengeOptions_SetIncludeFutureChallenges(IntPtr handle, bool value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_ChallengeOptions_SetIncludePastChallenges(IntPtr handle, bool value);

    public static void ovr_ChallengeOptions_SetLeaderboardName(IntPtr handle, string value) {
      IntPtr value_native = StringToNative(value);
      ovr_ChallengeOptions_SetLeaderboardName_Native(handle, value_native);
      Marshal.FreeCoTaskMem(value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_ChallengeOptions_SetLeaderboardName")]
    private static extern void ovr_ChallengeOptions_SetLeaderboardName_Native(IntPtr handle, IntPtr value);

    public static void ovr_ChallengeOptions_SetStartDate(IntPtr handle, DateTime value) {
      ulong value_native = DateTimeToNative(value);
      ovr_ChallengeOptions_SetStartDate_Native(handle, value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_ChallengeOptions_SetStartDate")]
    private static extern void ovr_ChallengeOptions_SetStartDate_Native(IntPtr handle, ulong value);

    public static void ovr_ChallengeOptions_SetTitle(IntPtr handle, string value) {
      IntPtr value_native = StringToNative(value);
      ovr_ChallengeOptions_SetTitle_Native(handle, value_native);
      Marshal.FreeCoTaskMem(value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_ChallengeOptions_SetTitle")]
    private static extern void ovr_ChallengeOptions_SetTitle_Native(IntPtr handle, IntPtr value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_ChallengeOptions_SetViewerFilter(IntPtr handle, ChallengeViewerFilter value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_ChallengeOptions_SetVisibility(IntPtr handle, ChallengeVisibility value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_GroupPresenceOptions_Create();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_GroupPresenceOptions_Destroy(IntPtr handle);

    public static void ovr_GroupPresenceOptions_SetDestinationApiName(IntPtr handle, string value) {
      IntPtr value_native = StringToNative(value);
      ovr_GroupPresenceOptions_SetDestinationApiName_Native(handle, value_native);
      Marshal.FreeCoTaskMem(value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_GroupPresenceOptions_SetDestinationApiName")]
    private static extern void ovr_GroupPresenceOptions_SetDestinationApiName_Native(IntPtr handle, IntPtr value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_GroupPresenceOptions_SetIsJoinable(IntPtr handle, bool value);

    public static void ovr_GroupPresenceOptions_SetLobbySessionId(IntPtr handle, string value) {
      IntPtr value_native = StringToNative(value);
      ovr_GroupPresenceOptions_SetLobbySessionId_Native(handle, value_native);
      Marshal.FreeCoTaskMem(value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_GroupPresenceOptions_SetLobbySessionId")]
    private static extern void ovr_GroupPresenceOptions_SetLobbySessionId_Native(IntPtr handle, IntPtr value);

    public static void ovr_GroupPresenceOptions_SetMatchSessionId(IntPtr handle, string value) {
      IntPtr value_native = StringToNative(value);
      ovr_GroupPresenceOptions_SetMatchSessionId_Native(handle, value_native);
      Marshal.FreeCoTaskMem(value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_GroupPresenceOptions_SetMatchSessionId")]
    private static extern void ovr_GroupPresenceOptions_SetMatchSessionId_Native(IntPtr handle, IntPtr value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_InviteOptions_Create();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_InviteOptions_Destroy(IntPtr handle);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_InviteOptions_AddSuggestedUser(IntPtr handle, UInt64 value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_InviteOptions_ClearSuggestedUsers(IntPtr handle);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_MatchmakingOptions_Create();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_MatchmakingOptions_Destroy(IntPtr handle);

    public static void ovr_MatchmakingOptions_SetCreateRoomDataStoreString(IntPtr handle, string key, string value) {
      IntPtr key_native = StringToNative(key);
      IntPtr value_native = StringToNative(value);
      ovr_MatchmakingOptions_SetCreateRoomDataStoreString_Native(handle, key_native, value_native);
      Marshal.FreeCoTaskMem(key_native);
      Marshal.FreeCoTaskMem(value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_MatchmakingOptions_SetCreateRoomDataStoreString")]
    private static extern void ovr_MatchmakingOptions_SetCreateRoomDataStoreString_Native(IntPtr handle, IntPtr key, IntPtr value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_MatchmakingOptions_ClearCreateRoomDataStore(IntPtr handle);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_MatchmakingOptions_SetCreateRoomJoinPolicy(IntPtr handle, RoomJoinPolicy value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_MatchmakingOptions_SetCreateRoomMaxUsers(IntPtr handle, uint value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_MatchmakingOptions_AddEnqueueAdditionalUser(IntPtr handle, UInt64 value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_MatchmakingOptions_ClearEnqueueAdditionalUsers(IntPtr handle);

    public static void ovr_MatchmakingOptions_SetEnqueueDataSettingsInt(IntPtr handle, string key, int value) {
      IntPtr key_native = StringToNative(key);
      ovr_MatchmakingOptions_SetEnqueueDataSettingsInt_Native(handle, key_native, value);
      Marshal.FreeCoTaskMem(key_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_MatchmakingOptions_SetEnqueueDataSettingsInt")]
    private static extern void ovr_MatchmakingOptions_SetEnqueueDataSettingsInt_Native(IntPtr handle, IntPtr key, int value);

    public static void ovr_MatchmakingOptions_SetEnqueueDataSettingsDouble(IntPtr handle, string key, double value) {
      IntPtr key_native = StringToNative(key);
      ovr_MatchmakingOptions_SetEnqueueDataSettingsDouble_Native(handle, key_native, value);
      Marshal.FreeCoTaskMem(key_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_MatchmakingOptions_SetEnqueueDataSettingsDouble")]
    private static extern void ovr_MatchmakingOptions_SetEnqueueDataSettingsDouble_Native(IntPtr handle, IntPtr key, double value);

    public static void ovr_MatchmakingOptions_SetEnqueueDataSettingsString(IntPtr handle, string key, string value) {
      IntPtr key_native = StringToNative(key);
      IntPtr value_native = StringToNative(value);
      ovr_MatchmakingOptions_SetEnqueueDataSettingsString_Native(handle, key_native, value_native);
      Marshal.FreeCoTaskMem(key_native);
      Marshal.FreeCoTaskMem(value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_MatchmakingOptions_SetEnqueueDataSettingsString")]
    private static extern void ovr_MatchmakingOptions_SetEnqueueDataSettingsString_Native(IntPtr handle, IntPtr key, IntPtr value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_MatchmakingOptions_ClearEnqueueDataSettings(IntPtr handle);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_MatchmakingOptions_SetEnqueueIsDebug(IntPtr handle, bool value);

    public static void ovr_MatchmakingOptions_SetEnqueueQueryKey(IntPtr handle, string value) {
      IntPtr value_native = StringToNative(value);
      ovr_MatchmakingOptions_SetEnqueueQueryKey_Native(handle, value_native);
      Marshal.FreeCoTaskMem(value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_MatchmakingOptions_SetEnqueueQueryKey")]
    private static extern void ovr_MatchmakingOptions_SetEnqueueQueryKey_Native(IntPtr handle, IntPtr value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_MultiplayerErrorOptions_Create();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_MultiplayerErrorOptions_Destroy(IntPtr handle);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_MultiplayerErrorOptions_SetErrorKey(IntPtr handle, MultiplayerErrorErrorKey value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_NetSyncOptions_Create();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_NetSyncOptions_Destroy(IntPtr handle);

    public static void ovr_NetSyncOptions_SetVoipGroup(IntPtr handle, string value) {
      IntPtr value_native = StringToNative(value);
      ovr_NetSyncOptions_SetVoipGroup_Native(handle, value_native);
      Marshal.FreeCoTaskMem(value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_NetSyncOptions_SetVoipGroup")]
    private static extern void ovr_NetSyncOptions_SetVoipGroup_Native(IntPtr handle, IntPtr value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_NetSyncOptions_SetVoipStreamDefault(IntPtr handle, NetSyncVoipStreamMode value);

    public static void ovr_NetSyncOptions_SetZoneId(IntPtr handle, string value) {
      IntPtr value_native = StringToNative(value);
      ovr_NetSyncOptions_SetZoneId_Native(handle, value_native);
      Marshal.FreeCoTaskMem(value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_NetSyncOptions_SetZoneId")]
    private static extern void ovr_NetSyncOptions_SetZoneId_Native(IntPtr handle, IntPtr value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_RichPresenceOptions_Create();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_RichPresenceOptions_Destroy(IntPtr handle);

    public static void ovr_RichPresenceOptions_SetApiName(IntPtr handle, string value) {
      IntPtr value_native = StringToNative(value);
      ovr_RichPresenceOptions_SetApiName_Native(handle, value_native);
      Marshal.FreeCoTaskMem(value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_RichPresenceOptions_SetApiName")]
    private static extern void ovr_RichPresenceOptions_SetApiName_Native(IntPtr handle, IntPtr value);

    public static void ovr_RichPresenceOptions_SetArgsString(IntPtr handle, string key, string value) {
      IntPtr key_native = StringToNative(key);
      IntPtr value_native = StringToNative(value);
      ovr_RichPresenceOptions_SetArgsString_Native(handle, key_native, value_native);
      Marshal.FreeCoTaskMem(key_native);
      Marshal.FreeCoTaskMem(value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_RichPresenceOptions_SetArgsString")]
    private static extern void ovr_RichPresenceOptions_SetArgsString_Native(IntPtr handle, IntPtr key, IntPtr value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_RichPresenceOptions_ClearArgs(IntPtr handle);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_RichPresenceOptions_SetCurrentCapacity(IntPtr handle, uint value);

    public static void ovr_RichPresenceOptions_SetDeeplinkMessageOverride(IntPtr handle, string value) {
      IntPtr value_native = StringToNative(value);
      ovr_RichPresenceOptions_SetDeeplinkMessageOverride_Native(handle, value_native);
      Marshal.FreeCoTaskMem(value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_RichPresenceOptions_SetDeeplinkMessageOverride")]
    private static extern void ovr_RichPresenceOptions_SetDeeplinkMessageOverride_Native(IntPtr handle, IntPtr value);

    public static void ovr_RichPresenceOptions_SetEndTime(IntPtr handle, DateTime value) {
      ulong value_native = DateTimeToNative(value);
      ovr_RichPresenceOptions_SetEndTime_Native(handle, value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_RichPresenceOptions_SetEndTime")]
    private static extern void ovr_RichPresenceOptions_SetEndTime_Native(IntPtr handle, ulong value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_RichPresenceOptions_SetExtraContext(IntPtr handle, RichPresenceExtraContext value);

    public static void ovr_RichPresenceOptions_SetInstanceId(IntPtr handle, string value) {
      IntPtr value_native = StringToNative(value);
      ovr_RichPresenceOptions_SetInstanceId_Native(handle, value_native);
      Marshal.FreeCoTaskMem(value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_RichPresenceOptions_SetInstanceId")]
    private static extern void ovr_RichPresenceOptions_SetInstanceId_Native(IntPtr handle, IntPtr value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_RichPresenceOptions_SetIsIdle(IntPtr handle, bool value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_RichPresenceOptions_SetIsJoinable(IntPtr handle, bool value);

    public static void ovr_RichPresenceOptions_SetJoinableId(IntPtr handle, string value) {
      IntPtr value_native = StringToNative(value);
      ovr_RichPresenceOptions_SetJoinableId_Native(handle, value_native);
      Marshal.FreeCoTaskMem(value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_RichPresenceOptions_SetJoinableId")]
    private static extern void ovr_RichPresenceOptions_SetJoinableId_Native(IntPtr handle, IntPtr value);

    public static void ovr_RichPresenceOptions_SetLobbySessionId(IntPtr handle, string value) {
      IntPtr value_native = StringToNative(value);
      ovr_RichPresenceOptions_SetLobbySessionId_Native(handle, value_native);
      Marshal.FreeCoTaskMem(value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_RichPresenceOptions_SetLobbySessionId")]
    private static extern void ovr_RichPresenceOptions_SetLobbySessionId_Native(IntPtr handle, IntPtr value);

    public static void ovr_RichPresenceOptions_SetMatchSessionId(IntPtr handle, string value) {
      IntPtr value_native = StringToNative(value);
      ovr_RichPresenceOptions_SetMatchSessionId_Native(handle, value_native);
      Marshal.FreeCoTaskMem(value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_RichPresenceOptions_SetMatchSessionId")]
    private static extern void ovr_RichPresenceOptions_SetMatchSessionId_Native(IntPtr handle, IntPtr value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_RichPresenceOptions_SetMaxCapacity(IntPtr handle, uint value);

    public static void ovr_RichPresenceOptions_SetStartTime(IntPtr handle, DateTime value) {
      ulong value_native = DateTimeToNative(value);
      ovr_RichPresenceOptions_SetStartTime_Native(handle, value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_RichPresenceOptions_SetStartTime")]
    private static extern void ovr_RichPresenceOptions_SetStartTime_Native(IntPtr handle, ulong value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_RoomOptions_Create();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_RoomOptions_Destroy(IntPtr handle);

    public static void ovr_RoomOptions_SetDataStoreString(IntPtr handle, string key, string value) {
      IntPtr key_native = StringToNative(key);
      IntPtr value_native = StringToNative(value);
      ovr_RoomOptions_SetDataStoreString_Native(handle, key_native, value_native);
      Marshal.FreeCoTaskMem(key_native);
      Marshal.FreeCoTaskMem(value_native);
    }

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl, EntryPoint="ovr_RoomOptions_SetDataStoreString")]
    private static extern void ovr_RoomOptions_SetDataStoreString_Native(IntPtr handle, IntPtr key, IntPtr value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_RoomOptions_ClearDataStore(IntPtr handle);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_RoomOptions_SetExcludeRecentlyMet(IntPtr handle, bool value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_RoomOptions_SetMaxUserResults(IntPtr handle, uint value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_RoomOptions_SetOrdering(IntPtr handle, UserOrdering value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_RoomOptions_SetRecentlyMetTimeWindow(IntPtr handle, TimeWindow value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_RoomOptions_SetRoomId(IntPtr handle, UInt64 value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_RoomOptions_SetTurnOffUpdates(IntPtr handle, bool value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_RosterOptions_Create();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_RosterOptions_Destroy(IntPtr handle);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_RosterOptions_AddSuggestedUser(IntPtr handle, UInt64 value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_RosterOptions_ClearSuggestedUsers(IntPtr handle);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_UserOptions_Create();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_UserOptions_Destroy(IntPtr handle);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_UserOptions_SetMaxUsers(IntPtr handle, uint value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_UserOptions_AddServiceProvider(IntPtr handle, ServiceProvider value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_UserOptions_ClearServiceProviders(IntPtr handle);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_UserOptions_SetTimeWindow(IntPtr handle, TimeWindow value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ovr_VoipOptions_Create();

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_VoipOptions_Destroy(IntPtr handle);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_VoipOptions_SetBitrateForNewConnections(IntPtr handle, VoipBitrate value);

    [DllImport(DLL_NAME, CallingConvention=CallingConvention.Cdecl)]
    public static extern void ovr_VoipOptions_SetCreateNewConnectionUseDtx(IntPtr handle, VoipDtxState value);
  }
}
