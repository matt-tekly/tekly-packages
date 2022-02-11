using System;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Tekly.Common.Utils
{
    public static class UnityRuntimeEditorUtils
    {
        private static readonly List<Action> s_onExitActions = new List<Action>();
        
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
        }

        private static void PlayModeStateChanged(UnityEditor.PlayModeStateChange change)
        {
            if (change != UnityEditor.PlayModeStateChange.EnteredEditMode) {
                return;
            }
            
            UnityEditor.EditorApplication.delayCall += () => {
                UnityEditor.EditorApplication.playModeStateChanged -= PlayModeStateChanged;
                foreach (var exitAction in s_onExitActions) {
                    try {
                        exitAction();
                    } catch (Exception exception) {
                        Debug.LogException(exception);
                    }
                }
            
                s_onExitActions.Clear();
            };
        }
#endif
    }
}