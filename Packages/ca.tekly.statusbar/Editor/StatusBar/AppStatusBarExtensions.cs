using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tekly.Favorites
{
    [InitializeOnLoad]
    public static class StatusBarExtensions
    {
        private static readonly Type s_appStatusBarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.AppStatusBar");

        private static readonly PropertyInfo s_viewVisualTree = s_appStatusBarType.GetProperty("visualTree", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        private static ScriptableObject s_currentAppStatusBar;

        static StatusBarExtensions()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            if (s_currentAppStatusBar == null) {
                var statusBars = Resources.FindObjectsOfTypeAll(s_appStatusBarType);
                s_currentAppStatusBar = statusBars.Length > 0 ? (ScriptableObject) statusBars[0] : null;

                if (s_currentAppStatusBar != null) {
                    var visualTree = (VisualElement) s_viewVisualTree.GetValue(s_currentAppStatusBar, null);
                    visualTree.Add(new StatusBar());
                }
            }
        }
    }
}