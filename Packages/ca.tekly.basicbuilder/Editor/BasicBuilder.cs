using Tekly.Common.Utils;
using Tekly.ZipFile;
using UnityEditor;
using UnityEngine;

namespace Tekly.BasicBuilder
{
    public static class BasicBuilder
    {
        private static string[] FOLDERS_TO_DELETE => new string[] {
            BuildUtility.ApplicationName + "_BurstDebugInformation_DoNotShip",
            BuildUtility.ApplicationName + "_BackUpThisFolder_ButDontShipItWithYourGame",

        };

        public static void Build(bool autoRun, BuildWindowSettings buildWindowSettings)
        {
            if (buildWindowSettings.UseAddressables) {
                BuildUtility.BuildAddressables();    
            }
            
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var buildDirectory = GetBuildDirectory(buildTarget);

            var buildOptions = BuildOptions.None;
            
            if (EditorUserBuildSettings.development) {
                buildOptions |= BuildOptions.Development;
            }
            
            if (EditorUserBuildSettings.allowDebugging) {
                buildOptions |= BuildOptions.AllowDebugging;
            }

            if (EditorUserBuildSettings.connectProfiler) {
                buildOptions |= BuildOptions.ConnectWithProfiler;
            }

            if (autoRun) {
                buildOptions |= BuildOptions.AutoRunPlayer;    
            }
            
            var options = new BuildPlayerOptions {
                scenes = BuildUtility.Scenes,
                locationPathName = $"{buildDirectory}/{BuildUtility.GetLocationPathName(buildTarget)}",
                target = buildTarget,
                options = buildOptions,
            };

            var defines = BuildDefines.Defines;
            if (defines.Length > 0) {
                options.extraScriptingDefines = defines;
            }
            
            var result = BuildPipeline.BuildPlayer(options);
            
            if (result == null) {
                Debug.LogError("Build failed!");
                return;
            }

            // TODO: We're deleting these files so they're not in the zip. I don't think this is a good solution.
            foreach (var folder in FOLDERS_TO_DELETE) {
                FileUtils.SafeDeleteDirectory($"{buildDirectory}/{folder}", true);    
            }

            if (buildTarget == BuildTarget.StandaloneWindows64) {
                System.IO.File.Delete(options.locationPathName + ".zip");
                ZipUtils.CompressDirectory(buildDirectory + ".zip", buildDirectory, 6);
            }
        }
        
        public static string GetBuildDirectory(BuildTarget buildTarget)
        {
            return $"Builds/{BuildUtility.ApplicationName}_{BuildUtility.GetTargetSimpleName(buildTarget)}_{Application.version}";
        }
    }
}