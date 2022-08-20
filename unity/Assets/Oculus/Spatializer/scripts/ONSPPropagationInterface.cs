/************************************************************************************
Filename    :   ONSPPropagationInterface.cs
Content     :   Interface into the Oculus Audio propagation functions
Copyright   :   Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Licensed under the Oculus SDK Version 3.5 (the "License");
you may not use the Oculus SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developer.oculus.com/licenses/sdk-3.5/

Unless required by applicable law or agreed to in writing, the Oculus SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
************************************************************************************/
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Oculus.Spatializer.Propagation;

namespace Oculus
{
    namespace Spatializer
    {
        namespace Propagation
        {
            /***********************************************************************************/
            // ENUMS and STRUCTS
            /***********************************************************************************/
            public enum FaceType : uint
            {
                TRIANGLES = 0,
                QUADS
            }

            public enum MaterialProperty : uint
            {
                ABSORPTION = 0,
                TRANSMISSION,
                SCATTERING
            }

            // Matches internal mesh layout
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct MeshGroup
            {
                public UIntPtr indexOffset;
                public UIntPtr faceCount;
                [MarshalAs(UnmanagedType.U4)]
                public FaceType faceType;
                public IntPtr material;
            }
        }
    }
}

class ONSPPropagation
{
    static PropagationInterface CachedInterface;
    public static PropagationInterface Interface { get { if (CachedInterface == null) CachedInterface = FindInterface(); return CachedInterface; } }

    static PropagationInterface FindInterface()
    {
        IntPtr temp;
        try
        {
            WwisePluginInterface.ovrAudio_GetPluginContext(out temp, ClientType.OVRA_CLIENT_TYPE_WWISE_UNKNOWN);
            Debug.Log("Propagation initialized with Wwise Oculus Spatializer plugin");
            return new WwisePluginInterface();
        }
        catch(System.DllNotFoundException)
        {
            // this is fine
        }
        try
        {
            FMODPluginInterface.ovrAudio_GetPluginContext(out temp, ClientType.OVRA_CLIENT_TYPE_FMOD);
            Debug.Log("Propagation initialized with FMOD Oculus Spatializer plugin");
            return new FMODPluginInterface();
        }
        catch (System.DllNotFoundException)
        {
            // this is fine
        }

        Debug.Log("Propagation initialized with Unity Oculus Spatializer plugin");
        return new UnityNativeInterface();
    }

    public enum ovrAudioScalarType : uint
    {
        Int8,
        UInt8,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Float16,
        Float32,
        Float64
    }

    public class ClientType
    {
        // Copied from AudioSDK\OVRAudio\OVR_Audio_Internal.h
        public const uint OVRA_CLIENT_TYPE_NATIVE = 0;
        public const uint OVRA_CLIENT_TYPE_WWISE_2016 = 1;
        public const uint OVRA_CLIENT_TYPE_WWISE_2017_1 = 2;
        public const uint OVRA_CLIENT_TYPE_WWISE_2017_2 = 3;
        public const uint OVRA_CLIENT_TYPE_WWISE_2018_1 = 4;
        public const uint OVRA_CLIENT_TYPE_FMOD = 5;
        public const uint OVRA_CLIENT_TYPE_UNITY = 6;
        public const uint OVRA_CLIENT_TYPE_UE4 = 7;
        public const uint OVRA_CLIENT_TYPE_VST = 8;
        public const uint OVRA_CLIENT_TYPE_AAX = 9;
        public const uint OVRA_CLIENT_TYPE_TEST = 10;
        public const uint OVRA_CLIENT_TYPE_OTHER = 11;
        public const uint OVRA_CLIENT_TYPE_WWISE_UNKNOWN = 12;
        public const uint OVRA_CLIENT_TYPE_WWISE_2019_1 = 13;
        public const uint OVRA_CLIENT_TYPE_WWISE_2019_2 = 14;
        public const uint OVRA_CLIENT_TYPE_WWISE_2021_1 = 15;
    }

    public interface PropagationInterface
    {
        /***********************************************************************************/
        // Settings API
        int SetPropagationQuality(float quality);
        int SetPropagationThreadAffinity(UInt64 cpuMask);

        /***********************************************************************************/
        // Geometry API
        int CreateAudioGeometry(out IntPtr geometry);
        int DestroyAudioGeometry(IntPtr geometry);
        int AudioGeometryUploadMeshArrays(IntPtr geometry,
                                                        float[] vertices, int vertexCount,
                                                        int[] indices, int indexCount,
                                                        MeshGroup[] groups, int groupCount);
        int AudioGeometrySetTransform(IntPtr geometry, float[] matrix4x4);
        int AudioGeometryGetTransform(IntPtr geometry, out float[] matrix4x4);
        int AudioGeometryWriteMeshFile(IntPtr geometry, string filePath);
        int AudioGeometryReadMeshFile(IntPtr geometry, string filePath);
        int AudioGeometryWriteMeshFileObj(IntPtr geometry, string filePath);

        /***********************************************************************************/
        // Material API
        int AudioMaterialGetFrequency(IntPtr material, MaterialProperty property, float frequency, out float value);
        int CreateAudioMaterial(out IntPtr material);
        int DestroyAudioMaterial(IntPtr material);
        int AudioMaterialSetFrequency(IntPtr material, MaterialProperty property, float frequency, float value);
        int AudioMaterialReset(IntPtr material, MaterialProperty property);
    }

    /***********************************************************************************/
    // UNITY NATIVE
    /***********************************************************************************/
    public class UnityNativeInterface : PropagationInterface
    {
        // The name used for the plugin DLL.
        public const string strOSPS = "AudioPluginOculusSpatializer";

        /***********************************************************************************/
        // Context API: Required to create internal context if it does not exist yet
        IntPtr context_ = IntPtr.Zero;
        IntPtr context { get { if (context_ == IntPtr.Zero) { ovrAudio_GetPluginContext(out context_, ClientType.OVRA_CLIENT_TYPE_UNITY); } return context_; } }

        [DllImport(strOSPS)]
        public static extern int ovrAudio_GetPluginContext(out IntPtr context, uint clientType);

        /***********************************************************************************/
        // Settings API
        [DllImport(strOSPS)]
        private static extern int ovrAudio_SetPropagationQuality(IntPtr context, float quality);
        public int SetPropagationQuality(float quality)
        {
            return ovrAudio_SetPropagationQuality(context, quality);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_SetPropagationThreadAffinity(IntPtr context, UInt64 cpuMask);
        public int SetPropagationThreadAffinity(UInt64 cpuMask)
        {
            return ovrAudio_SetPropagationThreadAffinity(context, cpuMask);
        }

        /***********************************************************************************/
        // Geometry API
        [DllImport(strOSPS)]
        private static extern int ovrAudio_CreateAudioGeometry(IntPtr context, out IntPtr geometry);
        public int CreateAudioGeometry(out IntPtr geometry)
        {
            return ovrAudio_CreateAudioGeometry(context, out geometry);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_DestroyAudioGeometry(IntPtr geometry);
        public int DestroyAudioGeometry(IntPtr geometry)
        {
            return ovrAudio_DestroyAudioGeometry(geometry);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioGeometryUploadMeshArrays(IntPtr geometry,
                                                                        float[] vertices, UIntPtr verticesBytesOffset, UIntPtr vertexCount, UIntPtr vertexStride, ovrAudioScalarType vertexType,
                                                                        int[] indices, UIntPtr indicesByteOffset, UIntPtr indexCount, ovrAudioScalarType indexType,
                                                                        MeshGroup[] groups, UIntPtr groupCount);

        public int AudioGeometryUploadMeshArrays(IntPtr geometry,
                                                        float[] vertices, int vertexCount,
                                                        int[] indices, int indexCount,
                                                        MeshGroup[] groups, int groupCount)
        {
            return ovrAudio_AudioGeometryUploadMeshArrays(geometry,
                vertices, UIntPtr.Zero, (UIntPtr)vertexCount, UIntPtr.Zero, ovrAudioScalarType.Float32,
                indices, UIntPtr.Zero, (UIntPtr)indexCount, ovrAudioScalarType.UInt32,
                groups, (UIntPtr)groupCount);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioGeometrySetTransform(IntPtr geometry, float[] matrix4x4);
        public int AudioGeometrySetTransform(IntPtr geometry, float[] matrix4x4)
        {
            return ovrAudio_AudioGeometrySetTransform(geometry, matrix4x4);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioGeometryGetTransform(IntPtr geometry, out float[] matrix4x4);
        public int AudioGeometryGetTransform(IntPtr geometry, out float[] matrix4x4)
        {
            return ovrAudio_AudioGeometryGetTransform(geometry, out matrix4x4);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioGeometryWriteMeshFile(IntPtr geometry, string filePath);
        public int AudioGeometryWriteMeshFile(IntPtr geometry, string filePath)
        {
            return ovrAudio_AudioGeometryWriteMeshFile(geometry, filePath);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioGeometryReadMeshFile(IntPtr geometry, string filePath);
        public int AudioGeometryReadMeshFile(IntPtr geometry, string filePath)
        {
            return ovrAudio_AudioGeometryReadMeshFile(geometry, filePath);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioGeometryWriteMeshFileObj(IntPtr geometry, string filePath);
        public int AudioGeometryWriteMeshFileObj(IntPtr geometry, string filePath)
        {
            return ovrAudio_AudioGeometryWriteMeshFileObj(geometry, filePath);
        }

        /***********************************************************************************/
        // Material API
        [DllImport(strOSPS)]
        private static extern int ovrAudio_CreateAudioMaterial(IntPtr context, out IntPtr material);
        public int CreateAudioMaterial(out IntPtr material)
        {
            return ovrAudio_CreateAudioMaterial(context, out material);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_DestroyAudioMaterial(IntPtr material);
        public int DestroyAudioMaterial(IntPtr material)
        {
            return ovrAudio_DestroyAudioMaterial(material);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioMaterialSetFrequency(IntPtr material, MaterialProperty property, float frequency, float value);
        public int AudioMaterialSetFrequency(IntPtr material, MaterialProperty property, float frequency, float value)
        {
            return ovrAudio_AudioMaterialSetFrequency(material, property, frequency, value);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioMaterialGetFrequency(IntPtr material, MaterialProperty property, float frequency, out float value);
        public int AudioMaterialGetFrequency(IntPtr material, MaterialProperty property, float frequency, out float value)
        {
            return ovrAudio_AudioMaterialGetFrequency(material, property, frequency, out value);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioMaterialReset(IntPtr material, MaterialProperty property);
        public int AudioMaterialReset(IntPtr material, MaterialProperty property)
        {
            return ovrAudio_AudioMaterialReset(material, property);
        }
    }

    /***********************************************************************************/
    // WWISE
    /***********************************************************************************/
    public class WwisePluginInterface : PropagationInterface
    {
        // The name used for the plugin DLL.
        public const string strOSPS = "OculusSpatializerWwise";

        /***********************************************************************************/
        // Context API: Required to create internal context if it does not exist yet
        IntPtr context_ = IntPtr.Zero;
        IntPtr context { get { if (context_ == IntPtr.Zero) { ovrAudio_GetPluginContext(out context_, ClientType.OVRA_CLIENT_TYPE_WWISE_UNKNOWN); } return context_; } }

        [DllImport(strOSPS)]
        public static extern int ovrAudio_GetPluginContext(out IntPtr context, uint clientType);

        /***********************************************************************************/
        // Settings API
        [DllImport(strOSPS)]
        private static extern int ovrAudio_SetPropagationQuality(IntPtr context, float quality);
        public int SetPropagationQuality(float quality)
        {
            return ovrAudio_SetPropagationQuality(context, quality);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_SetPropagationThreadAffinity(IntPtr context, UInt64 cpuMask);
        public int SetPropagationThreadAffinity(UInt64 cpuMask)
        {
            return ovrAudio_SetPropagationThreadAffinity(context, cpuMask);
        }

        /***********************************************************************************/
        // Geometry API
        [DllImport(strOSPS)]
        private static extern int ovrAudio_CreateAudioGeometry(IntPtr context, out IntPtr geometry);
        public int CreateAudioGeometry(out IntPtr geometry)
        {
            return ovrAudio_CreateAudioGeometry(context, out geometry);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_DestroyAudioGeometry(IntPtr geometry);
        public int DestroyAudioGeometry(IntPtr geometry)
        {
            return ovrAudio_DestroyAudioGeometry(geometry);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioGeometryUploadMeshArrays(IntPtr geometry,
                                                                        float[] vertices, UIntPtr verticesBytesOffset, UIntPtr vertexCount, UIntPtr vertexStride, ovrAudioScalarType vertexType,
                                                                        int[] indices, UIntPtr indicesByteOffset, UIntPtr indexCount, ovrAudioScalarType indexType,
                                                                        MeshGroup[] groups, UIntPtr groupCount);

        public int AudioGeometryUploadMeshArrays(IntPtr geometry,
                                                        float[] vertices, int vertexCount,
                                                        int[] indices, int indexCount,
                                                        MeshGroup[] groups, int groupCount)
        {
            return ovrAudio_AudioGeometryUploadMeshArrays(geometry,
                vertices, UIntPtr.Zero, (UIntPtr)vertexCount, UIntPtr.Zero, ovrAudioScalarType.Float32,
                indices, UIntPtr.Zero, (UIntPtr)indexCount, ovrAudioScalarType.UInt32,
                groups, (UIntPtr)groupCount);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioGeometrySetTransform(IntPtr geometry, float[] matrix4x4);
        public int AudioGeometrySetTransform(IntPtr geometry, float[] matrix4x4)
        {
            return ovrAudio_AudioGeometrySetTransform(geometry, matrix4x4);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioGeometryGetTransform(IntPtr geometry, out float[] matrix4x4);
        public int AudioGeometryGetTransform(IntPtr geometry, out float[] matrix4x4)
        {
            return ovrAudio_AudioGeometryGetTransform(geometry, out matrix4x4);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioGeometryWriteMeshFile(IntPtr geometry, string filePath);
        public int AudioGeometryWriteMeshFile(IntPtr geometry, string filePath)
        {
            return ovrAudio_AudioGeometryWriteMeshFile(geometry, filePath);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioGeometryReadMeshFile(IntPtr geometry, string filePath);
        public int AudioGeometryReadMeshFile(IntPtr geometry, string filePath)
        {
            return ovrAudio_AudioGeometryReadMeshFile(geometry, filePath);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioGeometryWriteMeshFileObj(IntPtr geometry, string filePath);
        public int AudioGeometryWriteMeshFileObj(IntPtr geometry, string filePath)
        {
            return ovrAudio_AudioGeometryWriteMeshFileObj(geometry, filePath);
        }

        /***********************************************************************************/
        // Material API
        [DllImport(strOSPS)]
        private static extern int ovrAudio_CreateAudioMaterial(IntPtr context, out IntPtr material);
        public int CreateAudioMaterial(out IntPtr material)
        {
            return ovrAudio_CreateAudioMaterial(context, out material);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_DestroyAudioMaterial(IntPtr material);
        public int DestroyAudioMaterial(IntPtr material)
        {
            return ovrAudio_DestroyAudioMaterial(material);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioMaterialSetFrequency(IntPtr material, MaterialProperty property, float frequency, float value);
        public int AudioMaterialSetFrequency(IntPtr material, MaterialProperty property, float frequency, float value)
        {
            return ovrAudio_AudioMaterialSetFrequency(material, property, frequency, value);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioMaterialGetFrequency(IntPtr material, MaterialProperty property, float frequency, out float value);
        public int AudioMaterialGetFrequency(IntPtr material, MaterialProperty property, float frequency, out float value)
        {
            return ovrAudio_AudioMaterialGetFrequency(material, property, frequency, out value);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioMaterialReset(IntPtr material, MaterialProperty property);
        public int AudioMaterialReset(IntPtr material, MaterialProperty property)
        {
            return ovrAudio_AudioMaterialReset(material, property);
        }
    }

    /***********************************************************************************/
    // FMOD
    /***********************************************************************************/
    public class FMODPluginInterface : PropagationInterface
    {
        // The name used for the plugin DLL.
        public const string strOSPS = "OculusSpatializerFMOD";

        /***********************************************************************************/
        // Context API: Required to create internal context if it does not exist yet
        IntPtr context_ = IntPtr.Zero;
        IntPtr context { get { if (context_ == IntPtr.Zero) { ovrAudio_GetPluginContext(out context_, ClientType.OVRA_CLIENT_TYPE_FMOD); } return context_; } }

        [DllImport(strOSPS)]
        public static extern int ovrAudio_GetPluginContext(out IntPtr context, uint clientType);

        /***********************************************************************************/
        // Settings API
        [DllImport(strOSPS)]
        private static extern int ovrAudio_SetPropagationQuality(IntPtr context, float quality);
        public int SetPropagationQuality(float quality)
        {
            return ovrAudio_SetPropagationQuality(context, quality);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_SetPropagationThreadAffinity(IntPtr context, UInt64 cpuMask);
        public int SetPropagationThreadAffinity(UInt64 cpuMask)
        {
            return ovrAudio_SetPropagationThreadAffinity(context, cpuMask);
        }

        /***********************************************************************************/
        // Geometry API
        [DllImport(strOSPS)]
        private static extern int ovrAudio_CreateAudioGeometry(IntPtr context, out IntPtr geometry);
        public int CreateAudioGeometry(out IntPtr geometry)
        {
            return ovrAudio_CreateAudioGeometry(context, out geometry);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_DestroyAudioGeometry(IntPtr geometry);
        public int DestroyAudioGeometry(IntPtr geometry)
        {
            return ovrAudio_DestroyAudioGeometry(geometry);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioGeometryUploadMeshArrays(IntPtr geometry,
                                                                        float[] vertices, UIntPtr verticesBytesOffset, UIntPtr vertexCount, UIntPtr vertexStride, ovrAudioScalarType vertexType,
                                                                        int[] indices, UIntPtr indicesByteOffset, UIntPtr indexCount, ovrAudioScalarType indexType,
                                                                        MeshGroup[] groups, UIntPtr groupCount);

        public int AudioGeometryUploadMeshArrays(IntPtr geometry,
                                                        float[] vertices, int vertexCount,
                                                        int[] indices, int indexCount,
                                                        MeshGroup[] groups, int groupCount)
        {
            return ovrAudio_AudioGeometryUploadMeshArrays(geometry,
                vertices, UIntPtr.Zero, (UIntPtr)vertexCount, UIntPtr.Zero, ovrAudioScalarType.Float32,
                indices, UIntPtr.Zero, (UIntPtr)indexCount, ovrAudioScalarType.UInt32,
                groups, (UIntPtr)groupCount);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioGeometrySetTransform(IntPtr geometry, float[] matrix4x4);
        public int AudioGeometrySetTransform(IntPtr geometry, float[] matrix4x4)
        {
            return ovrAudio_AudioGeometrySetTransform(geometry, matrix4x4);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioGeometryGetTransform(IntPtr geometry, out float[] matrix4x4);
        public int AudioGeometryGetTransform(IntPtr geometry, out float[] matrix4x4)
        {
            return ovrAudio_AudioGeometryGetTransform(geometry, out matrix4x4);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioGeometryWriteMeshFile(IntPtr geometry, string filePath);
        public int AudioGeometryWriteMeshFile(IntPtr geometry, string filePath)
        {
            return ovrAudio_AudioGeometryWriteMeshFile(geometry, filePath);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioGeometryReadMeshFile(IntPtr geometry, string filePath);
        public int AudioGeometryReadMeshFile(IntPtr geometry, string filePath)
        {
            return ovrAudio_AudioGeometryReadMeshFile(geometry, filePath);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioGeometryWriteMeshFileObj(IntPtr geometry, string filePath);
        public int AudioGeometryWriteMeshFileObj(IntPtr geometry, string filePath)
        {
            return ovrAudio_AudioGeometryWriteMeshFileObj(geometry, filePath);
        }

        /***********************************************************************************/
        // Material API
        [DllImport(strOSPS)]
        private static extern int ovrAudio_CreateAudioMaterial(IntPtr context, out IntPtr material);
        public int CreateAudioMaterial(out IntPtr material)
        {
            return ovrAudio_CreateAudioMaterial(context, out material);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_DestroyAudioMaterial(IntPtr material);
        public int DestroyAudioMaterial(IntPtr material)
        {
            return ovrAudio_DestroyAudioMaterial(material);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioMaterialSetFrequency(IntPtr material, MaterialProperty property, float frequency, float value);
        public int AudioMaterialSetFrequency(IntPtr material, MaterialProperty property, float frequency, float value)
        {
            return ovrAudio_AudioMaterialSetFrequency(material, property, frequency, value);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioMaterialGetFrequency(IntPtr material, MaterialProperty property, float frequency, out float value);
        public int AudioMaterialGetFrequency(IntPtr material, MaterialProperty property, float frequency, out float value)
        {
            return ovrAudio_AudioMaterialGetFrequency(material, property, frequency, out value);
        }

        [DllImport(strOSPS)]
        private static extern int ovrAudio_AudioMaterialReset(IntPtr material, MaterialProperty property);
        public int AudioMaterialReset(IntPtr material, MaterialProperty property)
        {
            return ovrAudio_AudioMaterialReset(material, property);
        }
    }
}
