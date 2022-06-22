using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Utilities.Internal;

namespace UnityEditor.XR.Interaction.Toolkit.Utilities.Internal
{
    /// <inheritdoc />
    /// <summary>
    /// Based off of Unity's Internal ScriptableSingleton with UnityEditorInternal bits removed
    /// </summary>
    /// <typeparam name="T">The class being created</typeparam>
    abstract class EditorScriptableSettings<T> : ScriptableSettingsBase<T> where T : ScriptableObject
    {
        const string k_CustomSavePathFormat = "{0}{1}.asset";
        const string k_SavePathFormat = "{0}ScriptableSettings/{1}.asset";

        /// <summary>
        /// Retrieves a reference to the given settings class. Will load and initialize once, and cache for all future access.
        /// </summary>
        public static T instance
        {
            get
            {
                if (s_Instance == null)
                    CreateAndLoad();

                return s_Instance;
            }
        }

        static void CreateAndLoad()
        {
            Debug.Assert(s_Instance == null);

            // Try to load the singleton
            const string filter = "t:{0}";
            var settingsType = typeof(T);
            foreach (var guid in AssetDatabase.FindAssets(string.Format(filter, settingsType.Name)))
            {
                s_Instance = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (s_Instance != null)
                    break;
            }

            // Asset Database search can fail if the type was moved to a different namespace, so as a backup try searching for the asset file.
            // https://issuetracker.unity3d.com/issues/cannot-find-scriptableobject-asset-by-search-in-project-window-when-namespace-of-corresponding-script-is-changed
            if (s_Instance == null)
            {
                var assetsPath = Application.dataPath;
                var filename = $"{GetFilePath()}.asset";
                foreach (var path in Directory.EnumerateFiles(assetsPath, filename, SearchOption.AllDirectories))
                {
                    var pathInProject = $"Assets{path.Substring(assetsPath.Length)}";
                    s_Instance = AssetDatabase.LoadAssetAtPath<T>(pathInProject);
                    if (s_Instance != null)
                    {
                        // Manually reimport the asset so the Asset Database search does not fail again
                        AssetDatabase.ImportAsset(pathInProject, ImportAssetOptions.ForceUpdate);
                        break;
                    }
                }

                if (s_Instance == null)
                    FindAssetInPackages(filename);
            }

            // Create it if it doesn't exist
            if (s_Instance == null)
            {
                s_Instance = CreateInstance<T>();

                // And save it back out if appropriate
                Save(k_HasCustomPath ? k_CustomSavePathFormat : k_SavePathFormat);
            }

            Debug.Assert(s_Instance != null);
        }

        static void FindAssetInPackages(string filename)
        {
            var projectPath = Directory.GetCurrentDirectory();
            var packageCache = Path.Combine(projectPath, "Library", "PackageCache");
            if (Directory.Exists(packageCache))
            {
                foreach (var path in Directory.EnumerateFiles(packageCache, filename, SearchOption.AllDirectories))
                {
                    var pathInProject = $"Packages{path.Substring(packageCache.Length)}";
                    s_Instance = AssetDatabase.LoadAssetAtPath<T>(pathInProject);
                    if (s_Instance != null)
                    {
                        // Manually reimport the asset so the Asset Database search does not fail again
                        AssetDatabase.ImportAsset(pathInProject, ImportAssetOptions.ForceUpdate);
                        return;
                    }
                }
            }

            var packagesPath = Path.Combine(projectPath, "Packages");
            var manifestPath = Path.Combine(packagesPath, "manifest.json");
            if (!File.Exists(manifestPath))
                return;

            var manifestText = File.ReadAllText(manifestPath);

            // Match package name and file path for local packages
            var matches = Regex.Matches(manifestText, "^.*(\")(.*)(\".*:).*(file:)(.*)\"", RegexOptions.Multiline);
            var count = matches.Count;
            for (var i = 0; i < count; i++)
            {
                var match = matches[i];
                var packageName = match.Groups[2].Value;
                var relativePath = match.Groups[5].Value;
                var absolutePath = Path.Combine(packagesPath, relativePath);
                if (!Directory.Exists(absolutePath))
                    continue;

                foreach (var path in Directory.EnumerateFiles(absolutePath, filename, SearchOption.AllDirectories))
                {
                    var pathInProject = $"Packages/{packageName}/{path.Substring(absolutePath.Length)}";
                    s_Instance = AssetDatabase.LoadAssetAtPath<T>(pathInProject);
                    if (s_Instance != null)
                    {
                        // Manually reimport the asset so the Asset Database search does not fail again
                        AssetDatabase.ImportAsset(pathInProject, ImportAssetOptions.ForceUpdate);
                        return;
                    }
                }
            }
        }
    }
}