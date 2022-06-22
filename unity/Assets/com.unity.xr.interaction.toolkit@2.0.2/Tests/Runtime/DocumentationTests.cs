using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
#endif

namespace UnityEngine.XR.Interaction.Toolkit.Tests
{
    [TestFixture]
    class DocumentationTests
    {
        /// <summary>
        /// Runtime types that can appear in the Inspector.
        /// These should all have HelpURLs defined.
        /// </summary>
        static readonly Type[] s_RuntimeTypes;

        /// <summary>
        /// Runtime behavior types that can appear in the Inspector.
        /// These should all have AddComponentMenu defined.
        /// </summary>
        static readonly Type[] s_RuntimeBehaviorTypes;

#if UNITY_EDITOR
        /// <summary>
        /// <see cref="PackageInfo"/> for com.unity.xr.interaction.toolkit.
        /// </summary>
        PackageInfo m_PackageInfo;

        /// <summary>
        /// <c>major.minor</c> version of com.unity.xr.interaction.toolkit.
        /// </summary>
        string m_MajorMinorVersion;
#endif

        static DocumentationTests()
        {
            var assembly = Assembly.Load("Unity.XR.Interaction.Toolkit");
            if (assembly == null)
                return;

            s_RuntimeTypes = assembly.GetExportedTypes()
                .Where(type => (type.IsSubclassOf(typeof(MonoBehaviour)) || type.IsSubclassOf(typeof(ScriptableObject))) && !type.IsAbstract)
                .OrderBy(type => type.FullName)
                .ToArray();

            s_RuntimeBehaviorTypes = assembly.GetExportedTypes()
                .Where(type => type.IsSubclassOf(typeof(MonoBehaviour)) && !type.IsAbstract)
                .OrderBy(type => type.FullName)
                .ToArray();
        }

        [OneTimeSetUp]
        public void SetUp()
        {
#if UNITY_EDITOR
            var assembly = Assembly.Load("Unity.XR.Interaction.Toolkit");
            Assert.That(assembly, Is.Not.Null);

            m_PackageInfo = PackageInfo.FindForAssembly(assembly);
            Assert.That(m_PackageInfo, Is.Not.Null);

            // Parse the major.minor version
            Assert.That(m_PackageInfo.version, Is.Not.Null);
            Assert.That(m_PackageInfo.version, Is.Not.Empty);
            var secondDotIndex = m_PackageInfo.version.IndexOf('.', m_PackageInfo.version.IndexOf('.') + 1);
            Assert.That(secondDotIndex, Is.GreaterThan(0));
            m_MajorMinorVersion = m_PackageInfo.version.Substring(0, secondDotIndex);
#endif
        }

        /// <summary>
        /// Tests that all types that can appear in the Inspector have a valid
        /// URL for the (?) button.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of the object to test.</param>
        /// <returns>Returns a coroutine enumerator.</returns>
        [UnityTest]
        // ReSharper disable once InconsistentNaming -- Matches HelpURL attribute
        public IEnumerator HelpURLDefined([ValueSource(nameof(s_RuntimeTypes))] Type type)
        {
            Assert.That(type, Is.Not.Null);

            var attribute = type.GetCustomAttribute<HelpURLAttribute>();
            Assert.That(attribute, Is.Not.Null, $"HelpURL not defined for {type.FullName}.");

            Assert.That(attribute.URL, Is.Not.Null);
            Assert.That(attribute.URL, Is.Not.Empty);
#if UNITY_EDITOR
            Assert.That(attribute.URL, Does.Contain($"{m_PackageInfo.name}@{m_MajorMinorVersion}/"));
#endif
            // Assumes Scripting API reference.
            Assert.That(attribute.URL, Does.EndWith($"{type.FullName}.html"));

            yield return null;
        }

        /// <summary>
        /// Tests that all types that can appear in the Inspector have a specified
        /// path in the Component menu instead of just the default Component > Scripts menu.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of the object to test.</param>
        /// <returns>Returns a coroutine enumerator.</returns>
        [UnityTest]
        public IEnumerator AddComponentMenuDefined([ValueSource(nameof(s_RuntimeBehaviorTypes))] Type type)
        {
            Assert.That(type, Is.Not.Null);

            var attribute = type.GetCustomAttribute<AddComponentMenu>();
            Assert.That(attribute, Is.Not.Null, $"AddComponentMenu not defined for {type.FullName}.");

            yield return null;
        }
    }
}