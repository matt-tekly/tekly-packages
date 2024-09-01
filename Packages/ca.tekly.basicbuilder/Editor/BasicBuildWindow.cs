using System;
using System.IO;
using Tekly.Common.Git;
using Tekly.Common.Gui;
using Tekly.EditorUtils.Gui;
using UnityEditor;
using UnityEngine;

namespace Tekly.BasicBuilder
{
    public class BasicBuildWindow : EditorWindow
    {
        [MenuItem("Tools/Tekly/Basic Builder", false, 1)]
        private static void OpenWindow()
        {
            GetWindow<BasicBuildWindow>("Builder");
        }
        
        [SerializeField] private BuildWindowSettings m_settings;
        [SerializeField] private bool m_buildAndRun = true;
        
        private GUIContent m_settingsContent;
        private GitBranchInfo m_gitBranchInfo;
        
        private string[] m_addressableProfileNames;
        
        private readonly Color m_outerContainerColor = new Color(0.3f, 0.3f, 0.3f, 1);
        
        private void OnFocus()
        {
            m_settingsContent = EditorGUIUtility.IconContent("Settings");
            m_settingsContent.tooltip = "Open Project Settings";
            
            GitUtility.TryFindBranchInfo(out m_gitBranchInfo);
            m_settings = BuildWindowSettings.GetSettings();

            if (m_settings && m_settings.UseAddressables) {
                m_addressableProfileNames = BuildUtility.GetAddressableProfileNames().ToArray();    
            }
        }

        public void OnGUI()
        {
            if (m_settings == null) {
                EditorGUILayout.HelpBox("No settings found. Please create a Build Window Settings.", MessageType.Error);
                return;
            }
            
            using (EditorGuiExt.LargeContainer(m_outerContainerColor)) {
                Header();

                SettingsSection();
                SceneSection();

                AddressablesSection();
                BuildButtonSection();
                
                if (Application.isPlaying) {
                    EditorGUILayout.HelpBox("Can't build while the game is playing.", MessageType.Warning);
                }
            }
        }

        private void Header()
        {
            using (EditorGuiExt.Horizontal()) {
                var heading = $"{BuildUtility.ApplicationName} - {PlayerSettings.bundleVersion}";
                GUILayout.Label(heading, EditorGuiStyles.Instance.Heading, GUILayout.ExpandWidth(false));

                EditorGUILayout.Space(10, true);
                
                var target = EditorUserBuildSettings.activeBuildTarget;

                for (var index = 0; index < m_settings.Platforms.Length; index++) {
                    var platform = m_settings.Platforms[index];
                    var isActiveGroup = platform.BuildTarget == target;
                    var buttonStyle = EditorStyles.miniButtonMid;

                    if (index == 0) {
                        buttonStyle = EditorStyles.miniButtonLeft;
                    } else if (index == m_settings.Platforms.Length - 1) {
                        buttonStyle = EditorStyles.miniButtonRight;
                    }
                    
                    using (EditorGuiExt.EnabledBlock(!isActiveGroup && !Application.isPlaying)) {
                        using (EditorGuiExt.ContentColorBlock(isActiveGroup ? Color.cyan : GUI.contentColor)) {
                            if (GUILayout.Button(platform.Content, buttonStyle, GUILayout.Width(30))) {
                                SwitchPlatforms(platform.BuildTargetGroup, platform.BuildTarget);
                            }
                        }
                    }
                }
            }
            
            EditorGUILayout.Space(2);
            using (EditorGuiExt.Horizontal()) {
                if (m_gitBranchInfo != null) {
                    GUILayout.Label($"Branch: {m_gitBranchInfo.Branch}");
                }

                EditorGUILayout.Space(10, true);
                if (GUILayout.Button(m_settingsContent, EditorStyles.iconButton, GUILayout.Width(22))) {
                    SettingsService.OpenProjectSettings("Project/Player");
                }
            }
        }
        
        private void AddressablesSection()
        {
            if (!m_settings.UseAddressables) {
                return;
            }
            
            using (Section("Addressables:")) {
                var index = Array.IndexOf(m_addressableProfileNames, BuildUtility.GetActiveAddressablesProfileName());
                var newIndex = EditorGUILayout.Popup("Profile", index, m_addressableProfileNames);

                if (index != newIndex) {
                    BuildUtility.SwitchAddressablesProfile(m_addressableProfileNames[newIndex]);
                }
            }
        }

        private void SceneSection()
        {
            using (Section("Scenes:")) {
                var scenes = BuildUtility.Scenes;
                foreach (var scene in scenes) {
                    EditorGUILayout.LabelField(scene);
                }
            }
        }
        
        private void SettingsSection()
        {
            using (Section("Settings")) {
                EditorUserBuildSettings.development = EditorGUILayout.Toggle("Development", EditorUserBuildSettings.development);
                using (EditorGuiExt.EnabledBlock(EditorUserBuildSettings.development)) {
                    EditorUserBuildSettings.allowDebugging = EditorGUILayout.Toggle("Debugging", EditorUserBuildSettings.allowDebugging);
                    EditorUserBuildSettings.connectProfiler = EditorGUILayout.Toggle("Auto Connect Profiler", EditorUserBuildSettings.connectProfiler);
                }
                
                m_buildAndRun = EditorGUILayout.Toggle("Auto Run", m_buildAndRun);
            }

            using (Section("Defines")) {
                foreach (var define in m_settings.Defines) {
                    var startingValue = BuildDefines.IsSet(define.Define);
                    var content = new GUIContent(define.DisplayName, define.ToolTip);
                    var newValue = EditorGUILayout.Toggle(content, startingValue);

                    if (startingValue != newValue) {
                        BuildDefines.Set(define.Define, newValue);
                    }
                }
            }
        }

        private void DoBuild()
        {
            BasicBuilder.Build(m_buildAndRun, m_settings);
        }

        private void BuildButtonSection()
        {
            using (EditorGuiExt.SmallContainer()) {
                using (EditorGuiExt.Horizontal()) {
                    var buildDirectory = BasicBuilder.GetBuildDirectory(EditorUserBuildSettings.activeBuildTarget);
                    if (EditorGUILayout.LinkButton(buildDirectory)) {
                        if (!Directory.Exists(buildDirectory)) {
                            Directory.CreateDirectory(buildDirectory);
                        }

                        EditorUtility.RevealInFinder(buildDirectory);
                    }

                    EditorGUILayout.Space(10, true);
                    using (EditorGuiExt.EnabledBlock(!Application.isPlaying)) {
                        if (EditorGuiExt.NegativeButton("Build", GUILayout.Width(50))) {
                            EditorApplication.delayCall += DoBuild;
                        }
                    }
                }
            }
        }
        
        private static IDisposable Section(string sectionTitle)
        {
            var disposable = EditorGuiExt.SmallContainer();
            EditorGUILayout.LabelField(sectionTitle, EditorStyles.boldLabel);

            return disposable;
        }

        private void SwitchPlatforms(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
        {
            var message = $"Are you sure you want to switch your platform to: {BuildUtility.GetTargetSimpleName(buildTarget)}";
            
            if (EditorUtility.DisplayDialog("Switch Platforms?", message, "Yes", "No")) {
                EditorApplication.delayCall += () => {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);
                };
            }
        }
    }
}
