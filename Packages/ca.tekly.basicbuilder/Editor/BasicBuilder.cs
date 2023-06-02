using Tekly.Common.Utils;
using Tekly.ZipFile;
using UnityEditor;
using UnityEngine;

namespace Tekly.BasicBuilder
{
    public static class BasicBuilder
    {
        private static string FOLDER_TO_DELETE => BuildUtility.ApplicationName + "_BurstDebugInformation_DoNotShip";
        
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

            FileUtils.SafeDeleteDirectory($"{buildDirectory}/{FOLDER_TO_DELETE}", true);

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