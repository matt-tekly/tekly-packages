using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tekly.DevBoard
{
	/// <summary>
	/// Leaves DevBoard's UI out of player builds that don't include DevBoard (see DevBoard.Enabled): removes every
	/// DevBoardInit from the scenes being built. Nothing then refers to DevBoard's widget prefabs and fonts, so
	/// Unity leaves them out too, and the builds don't log missing scripts for a UI assembly that isn't there.
	///
	/// Decides from the same defines the build compiles with: the platform's Player Settings, the active Build
	/// Profile, and the build's own options (its Development flag and extra defines). Logs what it decided, and
	/// why, once per build.
	/// </summary>
	public class DevBoardBuildStripper : BuildPlayerProcessor, IProcessSceneWithReport, IPostprocessBuildWithReport
	{
		private const string ENABLED = "DEVBOARD_ENABLED";
		private const string DISABLED = "DEVBOARD_DISABLED";

		public override int callbackOrder => 0;

		// The options of the build in progress, from PrepareForBuild. Unity makes a separate instance for each kind
		// of callback, so they're shared statically
		private static BuildPlayerOptions? s_options;
		private static bool? s_includes;

		public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
		{
			s_options = buildPlayerContext.BuildPlayerOptions;
			s_includes = null;
		}

		public void OnProcessScene(Scene scene, BuildReport report)
		{
			// Entering play mode in the editor processes scenes too, without a report. DevBoard stays then
			if (report == null || IncludesDevBoard(report)) {
				return;
			}

			var removed = 0;

			foreach (var root in scene.GetRootGameObjects()) {
				foreach (var init in root.GetComponentsInChildren<DevBoardInit>(true)) {
					// Usually a prefab instance, which can't lose parts of its prefab. The scene being built is a copy,
					// so unpacking it changes nothing in the project
					if (PrefabUtility.IsPartOfPrefabInstance(init)) {
						PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(init), PrefabUnpackMode.Completely,
							InteractionMode.AutomatedAction);
					}

					Object.DestroyImmediate(init);
					removed++;
				}
			}

			if (removed > 0) {
				Debug.Log($"[DevBoard] Removed {removed} DevBoardInit from scene '{scene.name}'");
			}
		}

		public void OnPostprocessBuild(BuildReport report)
		{
			s_options = null;
			s_includes = null;
		}

		/// <summary>
		/// The same rule as the UI assembly's define constraints and DevBoard.Enabled, for a player build:
		/// a development build or DEVBOARD_ENABLED, and not DEVBOARD_DISABLED. Worked out once per build.
		/// </summary>
		public static bool IncludesDevBoard(BuildReport report)
		{
			if (s_includes.HasValue) {
				return s_includes.Value;
			}

			var options = s_options?.options ?? report.summary.options;
			var development = (options & BuildOptions.Development) != 0;
			var sources = BuildDefines(report);

			var enabled = sources.TryGetValue(ENABLED, out var enabledFrom);
			var disabled = sources.TryGetValue(DISABLED, out var disabledFrom);
			var includes = (development || enabled) && !disabled;

			string reason;

			if (disabled) {
				reason = $"{DISABLED} from {disabledFrom}";
			} else if (enabled) {
				reason = $"{ENABLED} from {enabledFrom}";
			} else {
				reason = development ? "a development build" : $"not a development build, and no {ENABLED}";
			}

			Debug.Log(includes ? $"[DevBoard] In this build: {reason}" : $"[DevBoard] Not in this build: {reason}");

			s_includes = includes;
			return includes;
		}

		/// <summary>
		/// Every define the build compiles with, each with where it first came from.
		/// </summary>
		private static Dictionary<string, string> BuildDefines(BuildReport report)
		{
			var sources = new Dictionary<string, string>();

			var target = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(report.summary.platform));
			PlayerSettings.GetScriptingDefineSymbols(target, out var playerDefines);
			Add(playerDefines, "Player Settings", sources);

			var profile = BuildProfile.GetActiveBuildProfile();

			if (profile != null) {
				Add(profile.scriptingDefines, $"build profile '{profile.name}'", sources);
			}

			if (s_options.HasValue) {
				Add(s_options.Value.extraScriptingDefines, "the build's extra defines", sources);
			}

			return sources;
		}

		private static void Add(string[] symbols, string source, Dictionary<string, string> sources)
		{
			if (symbols == null) {
				return;
			}

			foreach (var symbol in symbols) {
				if (string.IsNullOrWhiteSpace(symbol)) {
					continue;
				}

				sources.TryAdd(symbol.Trim(), source);
			}
		}
	}
}
