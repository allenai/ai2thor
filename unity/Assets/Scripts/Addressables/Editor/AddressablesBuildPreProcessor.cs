using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Preprocessor which executes actions before any build has been made
/// </summary>
public class AddressablesBuildPreProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }

    /// <summary>
    /// Preprocess builds within headless or batch mode to autogenerate addressables
    /// </summary>
    /// <param name="report"></param>
    public void OnPreprocessBuild(BuildReport report)
    {
        if (IsHeadlessOrBatchMode())
        {
            BuildAddressablesContent();
        }
    }

    private static void BuildAddressablesContent()
    {
        AddressableAssetSettings.CleanPlayerContent(
            AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);
        AddressableAssetSettings.BuildPlayerContent();
    }

    /// <summary>
    /// Determines if the build was triggered through headless mode (ex: Jenkins build)
    /// </summary>
    /// <returns></returns>
    public static bool IsHeadlessOrBatchMode()
    {
        return Application.isBatchMode || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;
    }
}