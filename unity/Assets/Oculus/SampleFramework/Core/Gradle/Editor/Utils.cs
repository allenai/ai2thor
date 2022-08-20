using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

namespace Gradle
{
    public static class Configuration
    {
        private static readonly string androidPluginsFolder = "Assets/Plugins/Android/";
        private static readonly string gradleTemplatePath = androidPluginsFolder + "mainTemplate.gradle";
        private static readonly string disabledGradleTemplatePath = gradleTemplatePath + ".DISABLED";
        private static readonly string internalGradleTemplatePath = Path.Combine(Path.Combine(GetBuildToolsDirectory(BuildTarget.Android), "GradleTemplates"), "mainTemplate.gradle");

        private static string GetBuildToolsDirectory(UnityEditor.BuildTarget bt)
        {
            return (string)(typeof(BuildPipeline).GetMethod("GetBuildToolsDirectory", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, new object[] { bt }));
        }

        public static void UseGradle()
        {
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

            // create android plugins directory if it doesn't exist
            if (!Directory.Exists(androidPluginsFolder))
            {
                Directory.CreateDirectory(androidPluginsFolder);
            }

            if (!File.Exists(gradleTemplatePath))
            {
                if (File.Exists(gradleTemplatePath + ".DISABLED"))
                {
                    File.Move(disabledGradleTemplatePath, gradleTemplatePath);
                    File.Move(disabledGradleTemplatePath + ".meta", gradleTemplatePath + ".meta");
                }
                else
                {
                    File.Copy(internalGradleTemplatePath, gradleTemplatePath);
                }
                AssetDatabase.ImportAsset(gradleTemplatePath);
            }
        }

        public static bool IsUsingGradle()
        {
            return EditorUserBuildSettings.androidBuildSystem == AndroidBuildSystem.Gradle 
                && Directory.Exists(androidPluginsFolder) 
                && File.Exists(gradleTemplatePath);
        }

        public static List<string> ReadLines()
        {
            var allText = IsUsingGradle() ? File.ReadAllText(gradleTemplatePath) : "";
            return new List<string>(allText.Split('\n'));
        }

        public static void WriteLines(List<string> lines)
        {
            if (IsUsingGradle())
            {
                File.WriteAllText(gradleTemplatePath, string.Join("\n", lines.ToArray()));
            }
        }

//        // doesn't seem to be needed anymore as of Unity 2018.4.28f LTS
//        public static void VersionFixups(List<string> lines)
//        {
//            // add compileOptions to add Java 1.8 compatibility
//            int android = Gradle.Parsing.GoToSection("android", lines);
//            if (Gradle.Parsing.FindInScope("compileOptions", android + 1, lines) == -1)
//            {
//                int compileOptionsIndex = Gradle.Parsing.GetScopeEnd(android + 1, lines);
//                lines.Insert(compileOptionsIndex, "\t}");
//                lines.Insert(compileOptionsIndex, "\t\ttargetCompatibility JavaVersion.VERSION_1_8");
//                lines.Insert(compileOptionsIndex, "\t\tsourceCompatibility JavaVersion.VERSION_1_8");
//                lines.Insert(compileOptionsIndex, "\tcompileOptions {");
//            }

//            // add sourceSets if Version < 2018.2
//#if !UNITY_2018_2_OR_NEWER
//            if (Gradle.Parsing.FindInScope("sourceSets\\.main\\.java\\.srcDir", android + 1, lines) == -1)
//            {
//                lines.Insert(Gradle.Parsing.GetScopeEnd(android + 1, lines), "\tsourceSets.main.java.srcDir \"" + gradleSourceSetPath + "\"");
//            }
//#endif
//        }
    }

    public static class Dependencies
    {
        public static void AddRepository(string section, string name, List<string> lines)
        {
            int sectionIndex = Gradle.Parsing.GoToSection($"{section}.repositories", lines);
            if (Gradle.Parsing.FindInScope($"{name}\\(\\)", sectionIndex + 1, lines) == -1)
            {
                lines.Insert(Gradle.Parsing.GetScopeEnd(sectionIndex + 1, lines), $"\t\t{name}()");
            }
        }

        public static void AddDependency(string name, string version, List<string> lines)
        {
            int dependencies = Gradle.Parsing.GoToSection("dependencies", lines);
            if (Gradle.Parsing.FindInScope(Regex.Escape(name), dependencies + 1, lines) == -1)
            {
                lines.Insert(Gradle.Parsing.GetScopeEnd(dependencies + 1, lines), $"\tcompile '{name}:{version}'");
            }
        }

        public static void RemoveDependency(string name, List<string> lines)
        {
            int dependencies = Gradle.Parsing.GoToSection("dependencies", lines);
            int target = Gradle.Parsing.FindInScope(Regex.Escape(name), dependencies + 1, lines);
            if (target != -1)
            {
                lines.RemoveAt(target);
            }
        }

        public static void RemoveSourceSet(string name, List<string> lines)
        {
            int android = Gradle.Parsing.GoToSection("android", lines);
            int sourceSets = Gradle.Parsing.FindInScope(Regex.Escape(name), android + 1, lines);
            if (sourceSets != -1)
            {
                lines.RemoveAt(sourceSets);
            }
        }
    }

    public static class Parsing
    {
        public static string GetVersion(string text)
        {
            return new System.Text.RegularExpressions.Regex("com.android.tools.build:gradle:([0-9]+\\.[0-9]+\\.[0-9]+)").Match(text).Groups[1].Value;
        }
        public static int GoToSection(string section, List<string> lines)
        {
            return GoToSection(section, 0, lines);
        }

        public static int GoToSection(string section, int start, List<string> lines)
        {
            var sections = section.Split('.');

            int p = start - 1;
            for (int i = 0; i < sections.Length; i++)
            {
                p = FindInScope("\\s*" + sections[i] + "\\s*\\{\\s*", p + 1, lines);
            }

            return p;
        }

        public static int FindInScope(string search, int start, List<string> lines)
        {
            var regex = new System.Text.RegularExpressions.Regex(search);

            int depth = 0;

            for (int i = start; i < lines.Count; i++)
            {
                if (depth == 0 && regex.IsMatch(lines[i]))
                {
                    return i;
                }

                // count the number of open and close braces. If we leave the current scope, break
                if (lines[i].Contains("{"))
                {
                    depth++;
                }
                if (lines[i].Contains("}"))
                {
                    depth--;
                }
                if (depth < 0)
                {
                    break;
                }
            }
            return -1;
        }

        public static int GetScopeEnd(int start, List<string> lines)
        {
            int depth = 0;
            for (int i = start; i < lines.Count; i++)
            {
                // count the number of open and close braces. If we leave the current scope, break
                if (lines[i].Contains("{"))
                {
                    depth++;
                }
                if (lines[i].Contains("}"))
                {
                    depth--;
                }
                if (depth < 0)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
