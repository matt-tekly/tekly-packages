using System;
using System.Reflection;
using UnityEngine;
using Screen = UnityEngine.Device.Screen;

namespace Tekly.DebugKit.Utils
{
	/// <summary>
	/// In the editor we want to be able to know how big the screen is we're rendering to.
	/// This class helps us know how to scale the UI to keep a fixed size in the editor.
	/// </summary>
	public static class DebugKitScreen
	{
		public static int Width => (int) (Screen.width / ViewScale());
		public static int Height => (int) (Screen.height / ViewScale());
        
#if UNITY_EDITOR
		static DebugKitScreen()
		{
			s_gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
			s_targetInView = s_gameViewType.GetProperty("targetInView", BindingFlags.Instance | BindingFlags.NonPublic);
		}
        
		private static readonly Type s_gameViewType;
		private static readonly PropertyInfo s_targetInView;
        
		private static UnityEditor.EditorWindow s_gameViewWindow;
		
		public static float ViewScale()
		{
			UnityEditor.PlayModeWindow.GetRenderingResolution(out var width, out var height);
			if (width > height)
			{
				var x = ViewAreaSize().x;
				return Mathf.Max(1f, width / x);    
			}
            
			var y = ViewAreaSize().y;
			return Mathf.Max(height / y, 1);
		}

		private static Vector2 ViewAreaSize()
		{
			if (s_gameViewWindow == null)
			{
				s_gameViewWindow = UnityEditor.EditorWindow.GetWindow(s_gameViewType);
			}
            
			if (s_gameViewWindow == null)
			{
				return Vector2.one;
			}
            
			var rect = (Rect) s_targetInView.GetValue(s_gameViewWindow, null);
			return rect.size;
		}
#else
        public static float ViewScale()
        {
           return 1;
        }
#endif
	}
}