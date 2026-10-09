using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CampusLife.EditorTools
{
    /// <summary>
    /// Platform builds from the menu or the command line, e.g.
    /// Unity -batchmode -quit -projectPath CampusLifeAfrica -executeMethod CampusLife.EditorTools.BuildScript.BuildWebGL
    /// </summary>
    public static class BuildScript
    {
        [MenuItem("Campus Life Africa/Build/WebGL")]
        public static void BuildWebGL() => Build(BuildTarget.WebGL, "Builds/WebGL");

        [MenuItem("Campus Life Africa/Build/Windows")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/CampusLifeAfrica.exe");

        [MenuItem("Campus Life Africa/Build/Android (APK)")]
        public static void BuildAndroid() => Build(BuildTarget.Android, "Builds/Android/CampusLifeAfrica.apk");

        [MenuItem("Campus Life Africa/Build/iOS (Xcode project)")]
        public static void BuildIOS() => Build(BuildTarget.iOS, "Builds/iOS");

        static void Build(BuildTarget target, string path)
        {
            ProjectSetup.SetUpAll();
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.ScenePath },
                locationPathName = path,
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[Campus Life Africa] {target} build {summary.result}: {summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} errors, {summary.totalTime}");
            if (summary.result != BuildResult.Succeeded)
            {
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw new Exception($"{target} build failed: {summary.result}");
            }
        }
    }
}
