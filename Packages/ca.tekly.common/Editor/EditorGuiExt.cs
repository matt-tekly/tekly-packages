using System;
using UnityEngine;

namespace Tekly.Common
{
    public static class EditorGuiExt 
    {
        public static IDisposable GuiEnabledBlock(bool enabled)
        {
            return new GuiEnabledTracker(enabled);
        }
        
        public static IDisposable GuiColorBlock(Color color)
        {
            return new GuiColorTracker(color);
        }
        
        public static IDisposable Horizontal(GUIStyle style = null)
        {
            return new GuiLayoutTracker(true, style ?? GUIStyle.none);
        }
        
        public static IDisposable Horizontal(Color color, GUIStyle style = null)
        {
            return new GuiLayoutTracker(true, color, style ?? GUIStyle.none);
        }
        
        public static IDisposable Vertical(GUIStyle style = null)
        {
            return new GuiLayoutTracker(false, style ?? GUIStyle.none);
        }
        
        public static IDisposable Vertical(Color color, GUIStyle style = null)
        {
            return new GuiLayoutTracker(false, color, style ?? GUIStyle.none);
        }
        
        public static IDisposable LargeContainer(Color color)
        {
            return new GuiLayoutTracker(false, color, EditorGuiStyles.Instance.LargeContainer);
        }
        
        public static IDisposable LargeContainer()
        {
            return new GuiLayoutTracker(false, GUI.backgroundColor, EditorGuiStyles.Instance.LargeContainer);
        }
        
        public static IDisposable SmallContainer(Color color)
        {
            return new GuiLayoutTracker(false, color, EditorGuiStyles.Instance.SmallContainer);
        }
        
        public static IDisposable SmallContainer()
        {
            return new GuiLayoutTracker(false, GUI.backgroundColor, EditorGuiStyles.Instance.SmallContainer);
        }
        
        public static bool PositiveButton(string text, params GUILayoutOption[] options)
        {
            return ColorButton(text, Color.green, options);
        }
        
        public static bool NegativeButton(string text, params GUILayoutOption[] options)
        {
            return ColorButton(text, Color.red, options);
        }

        public static bool ColorButton(string text, Color color, params GUILayoutOption[] options)
        {
            var currentColor = GUI.backgroundColor;
            
            GUI.backgroundColor = color;
            var result = GUILayout.Button(text, options);
            GUI.backgroundColor = currentColor;
            
            return result;
        }
    }
}
