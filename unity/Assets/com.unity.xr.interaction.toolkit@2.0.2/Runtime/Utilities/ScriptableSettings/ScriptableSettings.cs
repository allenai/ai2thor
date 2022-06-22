using System;
using System.IO;
using System.Linq;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.XR.Interaction.Toolkit.Utilities
{
    /// <inheritdoc />
    /// <summary>
    /// Based off of Unity's Internal ScriptableSingleton with UnityEditorInternal bits removed
    /// </summary>
    /// <typeparam name="T">The class being created</typeparam>
    abstract class ScriptableSettings<T> : Internal.ScriptableSettingsBase<T> where T : ScriptableObject
    {
        const string k_CustomSavePathFormat = "{0}Resources/{1}.asset";
        const string k_SavePathFormat = "{0}Resources/ScriptableSettings/{1}.asset";
        const string k_LoadPathFormat = "ScriptableSettings/{0}";

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

        internal static T CreateAndLoad()
        {
            Debug.Assert(s_Instance == null);

            // Try to load the singleton
            var path = k_HasCustomPath ? GetFilePath() : string.Format(k_LoadPathFormat, GetFilePath());
            s_Instance = Resources.Load(path) as T;

            // Create it if it doesn't exist
            if (s_Instance == null)
            {
                s_Instance = CreateInstance<T>();

                // And save it back out if appropriate
                Save(k_HasCustomPath ? k_CustomSavePathFormat : k_SavePathFormat);
            }

            Debug.Assert(s_Instance != null);

            return s_Instance;
        }
    }
}

namespace UnityEngine.XR.Interaction.Toolkit.Utilities.Internal
{
    /// <inheritdoc />
    /// <summary>
    /// Internal base for all scriptable settings that is easier to look up via-reflection.
    /// DO NOT USE THIS CLASS DIRECTLY - Use the generic version - ScriptableSettingsBase<T /> above
    /// </summary>
    abstract class ScriptableSettingsBase : ScriptableObject
    {
        const string k_AbsolutePathMessage = "Path cannot be absolute";

        /// <summary>
        /// Message to display when path is invalid
        /// </summary>
        protected const string k_PathExceptionMessage = "Exception caught trying to create path.";

        internal const string nullPathMessage = "Path cannot be null";
        internal const string pathWithPeriodMessage = "Path cannot contain the character '.' before or after" +
            " a directory separator";
        internal const string pathWithInvalidCharacterMessage = "Paths on Windows cannot contain the following " +
            "characters: ':', '*', '?', '\"', '<', '>', '|'";

        static readonly char[] k_PathTrimChars =
        {
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar,
            ' '
        };
        // These characters are invalid in Windows paths, and are not contained in Path.InvalidPathChars on OS X
        static readonly char[] k_InvalidCharacters = { ':', '*', '?', '"', '<', '>', '|', '\\' };
        static readonly string[] k_InvalidStrings = { "\\.", "/.", ".\\", "./" };

        /// <summary>
        /// Looks up the static 'instance' property of the given ScriptableSettings
        /// </summary>
        /// <param name="settingsType">The type that refers to a singleton class, which implements an 'instance' property</param>
        /// <returns>The actual singleton instance of the specified class</returns>
        public static ScriptableSettingsBase GetInstanceByType(Type settingsType)
        {
            var instanceProperty = settingsType.GetProperty("instance",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.FlattenHierarchy);

            return (ScriptableSettingsBase)instanceProperty.GetValue(null, null);
        }

        // Awake and OnEnable can potentially have bad behavior in the editor during asset import, so we
        // don't allow implementors of ScriptableSettings to use these functions at all
        void Awake() { }

        void OnEnable()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                OnLoaded();
#else
            OnLoaded();
#endif
        }

#if UNITY_EDITOR
        internal void LoadInEditor()
        {
            OnLoaded();
        }
#endif

        /// <summary>
        /// Function called when all scriptable settings are loaded and ready for use
        /// </summary>
        protected virtual void OnLoaded()
        {
        }

        internal static bool ValidatePath(string path, out string cleanedPath)
        {
            cleanedPath = path;

            if (cleanedPath == null)
            {
                Debug.LogWarning(nullPathMessage);
                return false;
            }

            foreach (var invalidCharacter in k_InvalidCharacters)
            {
                if (cleanedPath.Contains(invalidCharacter))
                {
                    Debug.LogWarning(pathWithInvalidCharacterMessage);
                    return false;
                }
            }

            foreach (var str in k_InvalidStrings)
            {
                if (cleanedPath.Contains(str))
                {
                    Debug.LogWarning(pathWithPeriodMessage);
                    return false;
                }
            }

            try
            {
                if (Path.IsPathRooted(cleanedPath))
                {
                    Debug.LogWarning(k_AbsolutePathMessage);
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarningFormat("{0}\n{1}", k_PathExceptionMessage, e);
                return false;
            }

            cleanedPath = cleanedPath.Trim(k_PathTrimChars);

            var consecutiveSeparators = 0;
            for (var i = cleanedPath.Length - 1; i >= 0; --i)
            {
                if (cleanedPath[i] == '\\' || cleanedPath[i] == '/')
                {
                    consecutiveSeparators++;
                }
                else if (consecutiveSeparators > 0)
                {
                    cleanedPath = cleanedPath.Remove(i + 1, consecutiveSeparators - 1);
                    consecutiveSeparators = 0;
                }
            }

            if (cleanedPath != "")
                cleanedPath = string.Concat(cleanedPath, "/");

            return true;
        }
    }

    /// <summary>
    /// Base class for ScriptableSettings
    /// </summary>
    /// <typeparam name="T">The implementing type of ScriptableSettings</typeparam>
    abstract class ScriptableSettingsBase<T> : ScriptableSettingsBase where T : ScriptableObject
    {
        protected static readonly bool k_HasCustomPath = typeof(T).IsDefined(typeof(ScriptableSettingsPathAttribute), true);

        /// <summary>
        /// Singleton instance field
        /// </summary>
        protected static T s_Instance;

        /// <summary>
        /// Initialize a new ScriptableSettingsBase
        /// </summary>
        protected ScriptableSettingsBase()
        {
            if (s_Instance != null)
            {
                Debug.LogError(string.Format("ScriptableSingleton {0} already exists. This can happen if there are " +
                    "two copies of the asset or if you query the singleton in a constructor.", typeof(T)), s_Instance);
            }
        }

        /// <summary>
        /// Save this ScriptableSettings to an asset
        /// </summary>
        /// <param name="savePathFormat">Format string for creating the path of the asset</param>
        protected static void Save(string savePathFormat)
        {
            // We only save in the editor during edit mode
#if UNITY_EDITOR
            if (Application.isPlaying || !Application.isEditor)
            {
                // This is expected behavior so no log necessary here
                return;
            }

            if (s_Instance == null)
            {
                Debug.Log("Cannot save ScriptableSettings: no instance!");
                return;
            }

            var generatePath = true;
            string savePath = null;
            if (k_HasCustomPath)
            {
                var pathAttribute = typeof(T).GetAttribute<ScriptableSettingsPathAttribute>(true);
                string path;
                if (ValidatePath(pathAttribute.path, out path))
                {
                    generatePath = false;
                    savePath = string.Format(savePathFormat, path, GetFilePath());
                    try
                    {
                        CreateInstanceAsset(savePath);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"{k_PathExceptionMessage}\n{e}");
                        generatePath = true;
                    }
                }

                if (generatePath)
                    Debug.LogWarning($"The path '{pathAttribute.path}' is invalid. Generating a path instead.");
            }

            if (generatePath)
            {
                // We get the script path, and from there generate the save path.
                // This way settings will stick with packages/repositories they were created with
                var scriptData = MonoScript.FromScriptableObject(s_Instance);
                if (scriptData == null)
                {
                    Debug.LogWarning(
                        string.Format("Error saving {0}. Could not get a MonoScript from the instance", s_Instance),
                        s_Instance);

                    return;
                }
                var assetPath = AssetDatabase.GetAssetPath(scriptData);

                // Get the first folder above 'assets' or 'packages/com.package.name'
                var lower = assetPath.ToLowerInvariant();
                var folderEnd = 0;
                const int assetsLength = 7; // "Assets/".Length
                const int packagesLength = 9; // "Packages/".Length
                if (lower.StartsWith("assets/"))
                {
                    lower = lower.Substring(assetsLength);
                    folderEnd = lower.IndexOf('/') + assetsLength;
                }
                else if (lower.StartsWith("packages/"))
                {
                    lower = assetPath.Substring(packagesLength);
                    folderEnd = lower.IndexOf('/') + 1;
                    folderEnd += lower.Substring(folderEnd).IndexOf('/') + packagesLength;
                }

                var specializationPath = string.Concat(assetPath.Substring(0, folderEnd), "/");
                savePath = string.Format(savePathFormat, specializationPath, GetFilePath());
                CreateInstanceAsset(savePath);
            }

            if (!Application.isBatchMode)
                AssetDatabase.SaveAssets();
#endif
        }

        /// <summary>
        /// Get the filename for this ScriptableSettings
        /// </summary>
        /// <returns>The filename</returns>
        protected static string GetFilePath()
        {
            var type = typeof(T);
            return type.Name;
        }

#if UNITY_EDITOR
        static void CreateInstanceAsset(string savePath)
        {
            var folderPath = Path.GetDirectoryName(savePath);
            if (folderPath == null)
                throw new ArgumentException(string.Format("Path.GetDirectoryName returns null for {0}", savePath));

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            AssetDatabase.CreateAsset(s_Instance, savePath);
        }
#endif
    }
}
