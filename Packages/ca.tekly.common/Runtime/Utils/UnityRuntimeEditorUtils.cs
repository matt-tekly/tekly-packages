using System;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Tekly.Common.Utils
{
    public static class UnityRuntimeEditorUtils
    {
        private static readonly List<Action> s_onEnterActions = new();
        private static readonly List<Action> s_onExitActions = new();
        private static bool s_currentlyRunning;

        public static void OnEnterPlayMode(Action action)
        {
#if UNITY_EDITOR
            if (s_currentlyRunning) {
                action?.Invoke();
            }

            s_onEnterActions.Add(action);
#else
            action?.Invoke();
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void OnExitPlayMode(Action action)
        {
            s_onExitActions.Add(action);
        }
        
#if UNITY_EDITOR
        [UnityEditor.InitializeOnEnterPlayMode]
        private static void Initialize()
        {
            UnityEditor.EditorApplication.playModeStateChanged += PlayModeStateChanged;
            s_currentlyRunning = true;
            RunAllTheThings(s_onEnterActions);
        }

        private static void PlayModeStateChanged(UnityEditor.PlayModeStateChange change)
        {
            if (change != UnityEditor.PlayModeStateChange.EnteredEditMode) {
                return;
            }
            
            UnityEditor.EditorApplication.delayCall += () => {
                s_currentlyRunning = false;
                UnityEditor.EditorApplication.playModeStateChanged -= PlayModeStateChanged;
                RunAllTheThings(s_onExitActions);
            };
        }

        private static void RunAllTheThings(List<Action> allTheThings)
        {
            foreach (var thing in allTheThings) {
                try {
                    thing();
                } catch (Exception exception) {
                    Debug.LogException(exception);
                }
            }

            s_onExitActions.Clear();
        }
#endif
    }
}