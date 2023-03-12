using System.Collections;
using Tekly.Common.LifeCycles;
using TeklySample.Clipboard.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace TeklySample
{
	public static class CaptureScreenshot
	{
		[MenuItem("Tools/Tekly/Screenshot/Scene to clipboard")]
		public static void CaptureSceneView()
		{
			CaptureWindow(EditorWindow.GetWindow<SceneView>());
		}

		[MenuItem("Tools/Tekly/Screenshot/Game to clipboard")]
		public static void CaptureGameView()
		{
			if (Application.isPlaying) {
				LifeCycle.Instance.StartCoroutine(CaptureGameCoroutine());
			} else {
				var assembly = typeof(EditorWindow).Assembly;
				var type = assembly.GetType("UnityEditor.GameView");
				var gameView = EditorWindow.GetWindow(type);
				CaptureWindow(gameView);
			}
		}

		[MenuItem("Tools/Tekly/Screenshot/Active to clipboard")]
		public static void CaptureActiveView()
		{
			CaptureWindow(EditorWindow.focusedWindow);
		}

		private static void CaptureWindow(EditorWindow window)
		{
			// Get screen position and sizes
			var vec2Position = window.position.position;
			var sizeX = window.position.width;
			var sizeY = window.position.height;

			// Take Screenshot at given position sizes
			var colors = InternalEditorUtility.ReadScreenPixel(vec2Position, (int) sizeX, (int) sizeY);

			// write result Color[] data into a temporal Texture2D
			var texture = new Texture2D((int) sizeX, (int) sizeY);
			texture.SetPixels(colors);
			TeklyClipboard.CopyToClipboard(texture);
		}

		private static IEnumerator CaptureGameCoroutine()
		{
			yield return new WaitForEndOfFrame();
			var texture = ScreenCapture.CaptureScreenshotAsTexture();
			TeklyClipboard.CopyToClipboard(texture);
		}
	}
}