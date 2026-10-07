using System.IO;
using System.Linq;
using AuraKnight.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Tests.Integration
{
    /// <summary>Release readiness (plan phase 13): version, build flavours, signing guard and "no keystore in the repo".</summary>
    public sealed class ReleaseConfigTests
    {
        [Test]
        public void TheVersionIsOnePointZeroPointZeroWithVersionCodeOne()
        {
            Assert.AreEqual("1.0.0", ProjectSetup.Version);
            Assert.AreEqual(1, ProjectSetup.BundleVersionCode);
            Assert.AreEqual(ProjectSetup.Version, PlayerSettings.bundleVersion, "run Aura > Setup Project");
            Assert.AreEqual(ProjectSetup.BundleVersionCode, PlayerSettings.Android.bundleVersionCode);
        }

        [Test]
        public void OnlyTheDevelopmentApkIsADevelopmentBuild()
        {
            Assert.IsTrue((BuildScript.DevelopmentOptions & BuildOptions.Development) != 0);
            Assert.IsFalse((BuildScript.ReleaseOptions & BuildOptions.Development) != 0);
            Assert.IsFalse(EditorUserBuildSettings.development, "Development Build is off in the project");
        }

        [Test]
        public void TheReleaseBuildIsIl2cppArm64()
        {
            Assert.AreEqual(ScriptingImplementation.IL2CPP, PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android));
            Assert.AreEqual(AndroidArchitecture.ARM64, PlayerSettings.Android.targetArchitectures);
        }

        [Test]
        public void EveryQualityLevelHasVsyncOffSoTheFrameCapWorksOnAndroid()
        {
            int current = QualitySettings.GetQualityLevel();
            try
            {
                for (int level = 0; level < QualitySettings.names.Length; level++)
                {
                    QualitySettings.SetQualityLevel(level, false);
                    Assert.AreEqual(0, QualitySettings.vSyncCount, QualitySettings.names[level]);
                }
            }
            finally { QualitySettings.SetQualityLevel(current, false); }
        }

        [Test]
        public void SigningIsRefusedUntilKeystoreAndPasswordsAreAllThere()
        {
            Assert.IsNotNull(BuildScript.ReleaseSigningProblem(false, "k.keystore", true, "p", "a", "ap"), "debug key");
            Assert.IsNotNull(BuildScript.ReleaseSigningProblem(true, "", false, "p", "a", "ap"), "no file name");
            Assert.IsNotNull(BuildScript.ReleaseSigningProblem(true, "k.keystore", false, "p", "a", "ap"), "file missing");
            Assert.IsNotNull(BuildScript.ReleaseSigningProblem(true, "k.keystore", true, "", "a", "ap"), "no store password");
            Assert.IsNotNull(BuildScript.ReleaseSigningProblem(true, "k.keystore", true, "p", "", "ap"), "no alias");
            Assert.IsNotNull(BuildScript.ReleaseSigningProblem(true, "k.keystore", true, "p", "a", ""), "no alias password");
            Assert.IsNull(BuildScript.ReleaseSigningProblem(true, "k.keystore", true, "p", "a", "ap"));
        }

        [Test]
        public void NoKeystoreOrApkIsCheckedIntoTheRepository()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var tracked = new[] { "Assets", "ProjectSettings", "Packages", "docs", "tools" }.Select(d => Path.Combine(root, d)).Where(Directory.Exists);
            var offenders = tracked.SelectMany(d => Directory.GetFiles(d, "*", SearchOption.AllDirectories))
                .Where(f => new[] { ".keystore", ".jks", ".apk", ".aab" }.Contains(Path.GetExtension(f).ToLowerInvariant())).ToArray();
            CollectionAssert.IsEmpty(offenders);
        }
    }
}
