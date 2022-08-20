/************************************************************************************
Filename    :   OVRLipSync.cs
Content     :   Interface to Oculus Lip Sync engine
Created     :   August 4th, 2015
Copyright   :   Copyright Facebook Technologies, LLC and its affiliates.
                All rights reserved.

Licensed under the Oculus Audio SDK License Version 3.3 (the "License");
you may not use the Oculus Audio SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developer.oculus.com/licenses/audio-3.3/

Unless required by applicable law or agreed to in writing, the Oculus Audio SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
************************************************************************************/
using UnityEngine;
using System;
using System.Runtime.InteropServices;

//-------------------------------------------------------------------------------------
// ***** OVRLipSync
//
/// <summary>
/// OVRLipSync interfaces into the Oculus lip sync engine. This component should be added
/// into the scene once.
///
/// </summary>
public class OVRLipSync : MonoBehaviour
{
    // Error codes that may return from Lip Sync engine
    public enum Result
    {
        Success = 0,
        Unknown = -2200,  //< An unknown error has occurred
        CannotCreateContext = -2201,  //< Unable to create a context
        InvalidParam = -2202,  //< An invalid parameter, e.g. NULL pointer or out of range
        BadSampleRate = -2203,  //< An unsupported sample rate was declared
        MissingDLL = -2204,  //< The DLL or shared library could not be found
        BadVersion = -2205,  //< Mismatched versions between header and libs
        UndefinedFunction = -2206   //< An undefined function
    };

    // Audio buffer data type
    public enum AudioDataType
    {
        // Signed 16-bit integer mono audio stream
        S16_Mono,
        // Signed 16-bit integer stereo audio stream
        S16_Stereo,
        // Signed 32-bit float mono audio stream
        F32_Mono,
        // Signed 32-bit float stereo audio stream
        F32_Stereo
    };

    // Various visemes
    public enum Viseme
    {
        sil,
        PP,
        FF,
        TH,
        DD,
        kk,
        CH,
        SS,
        nn,
        RR,
        aa,
        E,
        ih,
        oh,
        ou
    };

    public static readonly int VisemeCount = Enum.GetNames(typeof(Viseme)).Length;

    // Enum for sending lip-sync engine specific signals
    public enum Signals
    {
        VisemeOn,
        VisemeOff,
        VisemeAmount,
        VisemeSmoothing,
        LaughterAmount
    };

    public static readonly int SignalCount = Enum.GetNames(typeof(Signals)).Length;

    // Enum for provider context to create
    public enum ContextProviders
    {
        Original,
        Enhanced,
        Enhanced_with_Laughter,
    };

    /// NOTE: Opaque typedef for lip-sync context is an unsigned int (uint)

    /// Current phoneme frame results
    [System.Serializable]
    public class Frame
    {
        public void CopyInput(Frame input)
        {
            frameNumber = input.frameNumber;
            frameDelay = input.frameDelay;
            input.Visemes.CopyTo(Visemes, 0);
            laughterScore = input.laughterScore;
        }

        public void Reset()
        {
            frameNumber = 0;
            frameDelay = 0;
            Array.Clear(Visemes, 0, VisemeCount);
            laughterScore = 0;
        }

        public int frameNumber;    // count from start of recognition
        public int frameDelay;     // in ms
        public float[] Visemes = new float[VisemeCount];       // Array of floats for viseme frame. Size of Viseme Count, above
        public float laughterScore; // probability of laughter presence.
    };

    // * * * * * * * * * * * * *
    // Import functions
    #if !UNITY_IOS || UNITY_EDITOR
    public const string strOVRLS = "OVRLipSync";
    #else
    public const string strOVRLS = "__Internal";
    #endif
    [DllImport(strOVRLS)]
    private static extern int ovrLipSyncDll_Initialize(int samplerate, int buffersize);
    [DllImport(strOVRLS)]
    private static extern void ovrLipSyncDll_Shutdown();
    [DllImport(strOVRLS)]
    private static extern IntPtr ovrLipSyncDll_GetVersion(ref int Major,
                                                          ref int Minor,
                                                          ref int Patch);
    [DllImport(strOVRLS)]
    private static extern int ovrLipSyncDll_CreateContextEx(ref uint context,
                                                           ContextProviders provider,
                                                           int sampleRate,
                                                           bool enableAcceleration);

    [DllImport(strOVRLS)]
    private static extern int ovrLipSyncDll_CreateContextWithModelFile(ref uint context,
                                                       ContextProviders provider,
                                                       string modelPath,
                                                       int sampleRate,
                                                       bool enableAcceleration);

    [DllImport(strOVRLS)]
    private static extern int ovrLipSyncDll_DestroyContext(uint context);


    [DllImport(strOVRLS)]
    private static extern int ovrLipSyncDll_ResetContext(uint context);
    [DllImport(strOVRLS)]
    private static extern int ovrLipSyncDll_SendSignal(uint context,
                                                       Signals signal,
                                                       int arg1, int arg2);
    [DllImport(strOVRLS)]
    private static extern int ovrLipSyncDll_ProcessFrameEx(
        uint context,
        IntPtr audioBuffer,
        uint bufferSize,
        AudioDataType dataType,
        ref int frameNumber,
        ref int frameDelay,
        float[] visemes,
        int visemeCount,
        ref float laughterScore,
        float[] laughterCategories,
        int laughterCategoriesLength);

    // * * * * * * * * * * * * *
    // Public members

    // * * * * * * * * * * * * *
    // Static members
    private static Result sInitialized = Result.Unknown;

    // interface through this static member.
    public static OVRLipSync sInstance = null;


    // * * * * * * * * * * * * *
    // MonoBehaviour overrides

    /// <summary>
    /// Awake this instance.
    /// </summary>
    void Awake()
    {
        // We can only have one instance of OVRLipSync in a scene (use this for local property query)
        if (sInstance == null)
        {
            sInstance = this;
        }
        else
        {
            Debug.LogWarning(System.String.Format("OVRLipSync Awake: Only one instance of OVRPLipSync can exist in the scene."));
            return;
        }

        if (IsInitialized() != Result.Success)
        {
            sInitialized = Initialize();

            if (sInitialized != Result.Success)
            {
                Debug.LogWarning(System.String.Format
                ("OvrLipSync Awake: Failed to init Speech Rec library"));
            }
        }

        // Important: Use the touchpad mechanism for input, call Create on the OVRTouchpad helper class
        OVRTouchpad.Create();

    }

    /// <summary>
    /// Raises the destroy event.
    /// </summary>
    void OnDestroy()
    {
        if (sInstance != this)
        {
            Debug.LogWarning(
            "OVRLipSync OnDestroy: This is not the correct OVRLipSync instance.");
            return;
        }

        // Do not shut down at this time
        //      ovrLipSyncDll_Shutdown();
        //      sInitialized = (int)Result.Unknown;
    }


    // * * * * * * * * * * * * *
    // Public Functions

    public static Result Initialize()
    {
        int sampleRate;
        int bufferSize;
        int numbuf;

        // Get the current sample rate
        sampleRate = AudioSettings.outputSampleRate;
        // Get the current buffer size and number of buffers
        AudioSettings.GetDSPBufferSize(out bufferSize, out numbuf);

        String str = System.String.Format
        ("OvrLipSync Awake: Queried SampleRate: {0:F0} BufferSize: {1:F0}", sampleRate, bufferSize);
        Debug.LogWarning(str);

        sInitialized = (Result)ovrLipSyncDll_Initialize(sampleRate, bufferSize);
        return sInitialized;
    }

    public static Result Initialize(int sampleRate, int bufferSize)
    {
        String str = System.String.Format
        ("OvrLipSync Awake: Queried SampleRate: {0:F0} BufferSize: {1:F0}", sampleRate, bufferSize);
        Debug.LogWarning(str);

        sInitialized = (Result)ovrLipSyncDll_Initialize(sampleRate, bufferSize);
        return sInitialized;
    }

    public static void Shutdown()
    {
        ovrLipSyncDll_Shutdown();
        sInitialized = Result.Unknown;
    }

    /// <summary>
    /// Determines if is initialized.
    /// </summary>
    /// <returns><c>true</c> if is initialized; otherwise, <c>false</c>.</returns>
    public static Result IsInitialized()
    {
        return sInitialized;
    }

    /// <summary>
    /// Creates a lip-sync context.
    /// </summary>
    /// <returns>error code</returns>
    /// <param name="context">Context.</param>
    /// <param name="provider">Provider.</param>
    /// <param name="enableAcceleration">Enable DSP Acceleration.</param>
    public static Result CreateContext(
        ref uint context,
        ContextProviders provider,
        int sampleRate = 0,
        bool enableAcceleration = false)
    {
        if (IsInitialized() != Result.Success && Initialize() != Result.Success)
            return Result.CannotCreateContext;

        return (Result)ovrLipSyncDll_CreateContextEx(ref context, provider, sampleRate, enableAcceleration);
    }

    /// <summary>
    /// Creates a lip-sync context with specified model file.
    /// </summary>
    /// <returns>error code</returns>
    /// <param name="context">Context.</param>
    /// <param name="provider">Provider.</param>
    /// <param name="modelPath">Model Dir.</param>
    /// <param name="sampleRate">Sampling Rate.</param>
    /// <param name="enableAcceleration">Enable DSP Acceleration.</param>
    public static Result CreateContextWithModelFile(
        ref uint context,
        ContextProviders provider,
        string modelPath,
        int sampleRate = 0,
        bool enableAcceleration = false)
    {
        if (IsInitialized() != Result.Success && Initialize() != Result.Success)
            return Result.CannotCreateContext;

        return (Result)ovrLipSyncDll_CreateContextWithModelFile(
            ref context,
            provider,
            modelPath,
            sampleRate,
            enableAcceleration);
    }

    /// <summary>
    /// Destroy a lip-sync context.
    /// </summary>
    /// <returns>The context.</returns>
    /// <param name="context">Context.</param>
    public static Result DestroyContext(uint context)
    {
        if (IsInitialized() != Result.Success)
            return Result.Unknown;

        return (Result)ovrLipSyncDll_DestroyContext(context);
    }

    /// <summary>
    /// Resets the context.
    /// </summary>
    /// <returns>error code</returns>
    /// <param name="context">Context.</param>
    public static Result ResetContext(uint context)
    {
        if (IsInitialized() != Result.Success)
            return Result.Unknown;

        return (Result)ovrLipSyncDll_ResetContext(context);
    }

    /// <summary>
    /// Sends a signal to the lip-sync engine.
    /// </summary>
    /// <returns>error code</returns>
    /// <param name="context">Context.</param>
    /// <param name="signal">Signal.</param>
    /// <param name="arg1">Arg1.</param>
    /// <param name="arg2">Arg2.</param>
    public static Result SendSignal(uint context, Signals signal, int arg1, int arg2)
    {
        if (IsInitialized() != Result.Success)
            return Result.Unknown;

        return (Result)ovrLipSyncDll_SendSignal(context, signal, arg1, arg2);
    }

    /// <summary>
    ///  Process float[] audio buffer by lip-sync engine.
    /// </summary>
    /// <returns>error code</returns>
    /// <param name="context">Context.</param>
    /// <param name="audioBuffer"> PCM audio buffer.</param>
    /// <param name="frame">Lip-sync Frame.</param>
    /// <param name="stereo">Whether buffer is part of stereo or mono stream.</param>
    public static Result ProcessFrame(
        uint context, float[] audioBuffer, Frame frame, bool stereo = true)
    {
        if (IsInitialized() != Result.Success)
            return Result.Unknown;

        var dataType = stereo ? AudioDataType.F32_Stereo : AudioDataType.F32_Mono;
        var numSamples = (uint)(stereo ? audioBuffer.Length / 2 : audioBuffer.Length);
        var handle = GCHandle.Alloc(audioBuffer, GCHandleType.Pinned);
        var rc = ovrLipSyncDll_ProcessFrameEx(context,
                                              handle.AddrOfPinnedObject(), numSamples, dataType,
                                              ref frame.frameNumber, ref frame.frameDelay,
                                              frame.Visemes, frame.Visemes.Length,
                                              ref frame.laughterScore,
                                              null, 0
                                              );
        handle.Free();
        return (Result)rc;

    }

    /// <summary>
    ///  Process short[] audio buffer by lip-sync engine.
    /// </summary>
    /// <returns>error code</returns>
    /// <param name="context">Context.</param>
    /// <param name="audioBuffer"> PCM audio buffer.</param>
    /// <param name="frame">Lip-sync Frame.</param>
    /// <param name="stereo">Whether buffer is part of stereo or mono stream.</param>
    public static Result ProcessFrame(
    uint context, short[] audioBuffer, Frame frame, bool stereo = true)
    {
        if (IsInitialized() != Result.Success)
            return Result.Unknown;

        var dataType = stereo ? AudioDataType.S16_Stereo : AudioDataType.S16_Mono;
        var numSamples = (uint)(stereo ? audioBuffer.Length / 2 : audioBuffer.Length);
        var handle = GCHandle.Alloc(audioBuffer, GCHandleType.Pinned);
        var rc = ovrLipSyncDll_ProcessFrameEx(context,
                                              handle.AddrOfPinnedObject(), numSamples, dataType,
                                              ref frame.frameNumber, ref frame.frameDelay,
                                              frame.Visemes, frame.Visemes.Length,
                                              ref frame.laughterScore,
                                              null, 0
                                              );
        handle.Free();
        return (Result)rc;
    }

}
