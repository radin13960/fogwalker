using System.IO;
using FogWalker.Core;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FogWalker.EditorTools
{
    /// <summary>
    /// ساخت خودکار اندروید: هم از منو و هم از خط فرمان (CI/دستور یک‌خطی).
    /// مثال CLI:
    ///   Unity -batchmode -quit -projectPath . -executeMethod FogWalker.EditorTools.BuildScript.BuildAndroidApk
    /// </summary>
    public static class BuildScript
    {
        private const string ApkPath = "Build/Android/FogWalker.apk";
        private const string AabPath = "Build/Android/FogWalker.aab";

        [MenuItem("FogWalker/Build/APK توسعه (Development)")]
        public static void BuildAndroidApkMenu() => BuildAndroidApk(development: true);

        [MenuItem("FogWalker/Build/AAB انتشار (Release)")]
        public static void BuildAndroidAabMenu() => BuildAndroidAab();

        /// <summary>ساخت APK توسعه برای نصب سریع روی دستگاه (بدون Keystore — با کلید Debug).</summary>
        public static void BuildAndroidApk() => BuildAndroidApk(development: false);

        /// <summary>ساخت AAB برای Google Play.</summary>
        public static void BuildAndroidAab()
        {
            SetupFactory.ApplyPlayerSettings();
            SetupFactory.AddScenesToBuildSettings();
            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.buildApkPerCpuArchitecture = false;
            RunBuild(AabPath, BuildOptions.None);
        }

        private static void BuildAndroidApk(bool development)
        {
            SetupFactory.ApplyPlayerSettings();
            SetupFactory.AddScenesToBuildSettings();
            EditorUserBuildSettings.buildAppBundle = false;
            var options = development ? (BuildOptions.Development | BuildOptions.AllowDebugging) : BuildOptions.None;
            RunBuild(ApkPath, options);
        }

        private static void RunBuild(string location, BuildOptions options)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(location));

            var scenes = new System.Collections.Generic.List<string>();
            foreach (var s in EditorBuildSettings.scenes)
                if (s.enabled) scenes.Add(s.path);

            if (scenes.Count == 0)
            {
                Debug.LogError("[Build] هیچ صحنه‌ای در Build Settings نیست!");
                return;
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = location,
                target = BuildTarget.Android,
                options = options,
            });

            if (report.summary.result == BuildResult.Succeeded)
                Debug.Log($"✅ [Build] موفق: {location} ({report.summary.totalSize / (1024f * 1024f):0.0} MB در {report.summary.totalTime.Minutes}m{report.summary.totalTime.Seconds}s)");
            else
                Debug.LogError($"❌ [Build] ناموفق: {report.summary.result}");
        }
    }
}
