using Tekly.Common.Gui;
using UnityEditor;
using UnityEngine;

namespace Tekly.EditorUtils.Gui
{
    public static class EditorGuiExt 
    {
        public static Color BackgroundColor {
            get {
                var backgroundIntensity = EditorGUIUtility.isProSkin ? 0.22f : 0.76f;
                return new Color(backgroundIntensity, backgroundIntensity, backgroundIntensity, 1f);
            }
        }
        
        public static GuiEnabledTracker EnabledBlock(bool enabled)
        {
            return new GuiEnabledTracker(enabled);
        }
        
        public static GuiColorTracker ColorBlock(Color color)
        {
            return new GuiColorTracker(color);
        }
        
        public static GuiBackgroundColorTracker BackgroundColorBlock(Color color)
        {
            return new GuiBackgroundColorTracker(color);
        }
        
        public static GuiContentColorTracker ContentColorBlock(Color color)
        {
            return new GuiContentColorTracker(color);
        }
        
        public static GuiLayoutTracker Horizontal(GUIStyle style = null)
        {
            return new GuiLayoutTracker(true, style ?? GUIStyle.none);
        }
        
        public static GuiLayoutTracker Horizontal(Color color, GUIStyle style = null)
        {
            return new GuiLayoutTracker(true, color, style ?? GUIStyle.none);
        }
        
        public static GuiLayoutTracker Vertical(GUIStyle style = null)
        {
            return new GuiLayoutTracker(false, style ?? GUIStyle.none);
        }
        
        public static GuiLayoutTracker Vertical(Color color, GUIStyle style = null)
        {
            return new GuiLayoutTracker(false, color, style ?? GUIStyle.none);
        }
        
        public static GuiLayoutTracker LargeContainer(Color color, bool horizontal = false)
        {
            return new GuiLayoutTracker(horizontal, color, EditorGuiStyles.Instance.LargeContainer);
        }
        
        public static GuiLayoutTracker LargeContainer(bool horizontal = false)
        {
            return new GuiLayoutTracker(horizontal, GUI.backgroundColor, EditorGuiStyles.Instance.LargeContainer);
        }
        
        public static GuiLayoutTracker SmallContainer(Color color, bool horizontal = false)
        {
            return new GuiLayoutTracker(horizontal, color, EditorGuiStyles.Instance.SmallContainer);
        }
        
        public static GuiLayoutTracker SmallContainer(bool horizontal = false)
        {
            return new GuiLayoutTracker(horizontal, GUI.backgroundColor, EditorGuiStyles.Instance.SmallContainer);
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
        
        public static void DrawSprite(Rect containerRect, Sprite sprite)
        {
            var uv = sprite.rect;
            uv.x /= sprite.texture.width;
            uv.width /= sprite.texture.width;
			
            uv.y /= sprite.texture.height;
            uv.height /= sprite.texture.height;
			
            var rect = FitTargetIntoContainer(sprite.textureRect, containerRect);
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
        }
        
        public static Rect FitTargetIntoContainer(Rect target, Rect container)
        {
            var scaleFactor = Mathf.Min(container.width / target.width, container.height / target.height);
            var scaledWidth = target.width * scaleFactor;
            var scaledHeight = target.height * scaleFactor;
            var offsetX = (container.width - scaledWidth) * 0.5f;
            var offsetY = (container.height - scaledHeight) * 0.5f;

            return new Rect(container.x + offsetX, container.y + offsetY, scaledWidth, scaledHeight);
        }
    }
}
