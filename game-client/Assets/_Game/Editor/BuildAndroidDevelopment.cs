using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Kingdom.Editor
{
    public static class BuildAndroidDevelopment
    {
        private const string OutputPath = "Builds/Android/KingdomOne-dev.apk";

        [MenuItem("Kingdom/Build/Build Android Development APK")]
        public static void Build()
        {
            KingdomProjectSetup.ConfigureAll();

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Could not switch the Unity build target to Android.");

            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.kingdomone.frontier");
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;

            string absoluteOutput = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputPath));
            Directory.CreateDirectory(Path.GetDirectoryName(absoluteOutput) ?? throw new InvalidOperationException());

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = absoluteOutput,
                target = BuildTarget.Android,
                options = BuildOptions.Development,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Android build failed: {report.summary.result}.");

            Debug.Log($"Android development APK ready: {absoluteOutput}");
        }
    }
}
