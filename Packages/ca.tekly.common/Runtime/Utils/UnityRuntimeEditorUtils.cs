using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Tekly.Common.Utils
{
    public static class UnityRuntimeEditorUtils
    {
        private static readonly List<Action> s_onEnterActions = new List<Action>();
        private static readonly List<Action> s_onExitActions = new List<Action>();
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
        [InitializeOnEnterPlayMode]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
            PlayModeStateChanged(PlayModeStateChange.ExitingEditMode);
        }

        public static void MockPlayModeStart()
        {
            PlayModeStateChanged(PlayModeStateChange.ExitingEditMode);
        }

        public static void MockPlayModeEnd()
        {
            PlayModeStateChanged(PlayModeStateChange.EnteredEditMode);
        }

        private static void PlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode) {
                s_currentlyRunning = true;
                RunActions(s_onEnterActions);
            } else if (change == PlayModeStateChange.EnteredEditMode) {
                s_currentlyRunning = false;
                EditorApplication.playModeStateChanged -= PlayModeStateChanged;
                RunActions(s_onExitActions);
            }
        }

        private static void RunActions(List<Action> actions)
        {
            foreach (var a in actions) {
                try {
                    a.Invoke();
                } catch (Exception exception) {
                    Debug.LogException(exception);
                }
            }
        }
#endif
    }
}