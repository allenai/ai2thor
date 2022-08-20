/************************************************************************************
Filename    :   OVRLipSyncTool.cs
Content     :   Editor tool for generating lip sync assets
Created     :   May 17th, 2018
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
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;

class OVRLipSyncToolLoader
{
    public static List<AudioClip> clipQueue;
    public static IEnumerator processor;

    // To show progress we use the total seconds of clip
    public static float totalLengthOfClips;
    public static float totalLengthOfClipsProcessed;

    public static IEnumerator ProcessClips(bool useOfflineModel)
    {
        if (clipQueue == null || clipQueue.Count == 0)
        {
            yield break;
        }

        while (clipQueue.Count > 0)
        {
            // Pop a clip off the list
            AudioClip clip = clipQueue[0];
            clipQueue.RemoveAt(0);

            if (clip.loadType != AudioClipLoadType.DecompressOnLoad)
            {
                Debug.LogError(clip.name +
                    ": Cannot process phonemes from an audio clip unless " +
                    "its load type is set to DecompressOnLoad.");
                continue;
            }

            // Update progress
            if (totalLengthOfClips > 0.0f)
            {
                EditorUtility.DisplayProgressBar("Generating Lip Sync Assets...", "Processing clip " + clip.name + "...",
                    totalLengthOfClipsProcessed / totalLengthOfClips);
            }

            if (!clip.preloadAudioData)
            {
                clip.LoadAudioData();

                Debug.LogWarning(clip.name +
                    ": Audio data is not pre-loaded. Data will be loaded then" +
                    "unloaded on completion.");

                while (clip.loadState != AudioDataLoadState.Loaded)
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }

            var sequence =
                OVRLipSyncSequence.CreateSequenceFromAudioClip(clip, useOfflineModel);
            if (sequence != null)
            {
                var path = AssetDatabase.GetAssetPath(clip);
                var newPath = path.Replace(Path.GetExtension(path), "_lipSync.asset");
                var existingSequence = AssetDatabase.LoadAssetAtPath<OVRLipSyncSequence>(newPath);
                if (existingSequence != null)
                {
                    EditorUtility.CopySerialized(sequence, existingSequence);
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    AssetDatabase.CreateAsset(sequence, newPath);

                }
            }
            AssetDatabase.Refresh();

            if (!clip.preloadAudioData)
            {
                clip.UnloadAudioData();
            }

            totalLengthOfClipsProcessed += clip.length;
        }

        EditorUtility.ClearProgressBar();
    }

    static OVRLipSyncToolLoader()
    {
        processor = null;
        EditorApplication.update += Update;
    }
    static void Update()
    {
        if (processor != null)
        {
            processor.MoveNext();
        }
    }
}

class OVRLipSyncTool
{
    [MenuItem("Oculus/Lip Sync/Generate Lip Sync Assets", false, 2000000)]
    static void GenerateLipSyncAssets()
    {
        GenerateLipSyncAssetsInternal(false);
    }

    [MenuItem("Oculus/Lip Sync/Generate Lip Sync Assets With Offline Model", false, 2500000)]
    static void GenerateLipSyncAssetsOffline()
    {
        GenerateLipSyncAssetsInternal(true);
    }

    private static void GenerateLipSyncAssetsInternal(bool useOfflineModel)

    {

        if (OVRLipSyncToolLoader.clipQueue == null)
        {
            OVRLipSyncToolLoader.clipQueue = new List<AudioClip>();
        }

        OVRLipSyncToolLoader.totalLengthOfClips = 0.0f;
        OVRLipSyncToolLoader.totalLengthOfClipsProcessed = 0.0f;

        for (int i = 0; i < Selection.objects.Length; ++i)
        {
            Object obj = Selection.objects[i];
            if (obj is AudioClip)
            {
                AudioClip clip = (AudioClip)obj;

                OVRLipSyncToolLoader.clipQueue.Add(clip);

                OVRLipSyncToolLoader.totalLengthOfClips += clip.length;
            }
        }

        OVRLipSyncToolLoader.processor = OVRLipSyncToolLoader.ProcessClips(useOfflineModel);
    }
}
