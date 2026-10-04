using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Common.Ui
{
	/// <summary>
	/// A CanvasScaler that, in Constant Pixel Size mode, compensates for Game view scaling in the editor
	/// so the UI stays the same physical size on the monitor. Behaves exactly like CanvasScaler in builds.
	/// </summary>
	[AddComponentMenu("Layout/Fixed Physical Canvas Scaler")]
	public class FixedPhysicalCanvasScaler : CanvasScaler
	{
		[Tooltip("In the editor, scale against the Game view's zoom so the UI stays the same size on the monitor")]
		[SerializeField] private bool m_compensateGameViewScale = true;

		/// <summary>
		/// In the editor, scale against the Game view's zoom so the UI stays the same size on the monitor.
		/// Has no effect in builds.
		/// </summary>
		public bool CompensateGameViewScale {
			get => m_compensateGameViewScale;
			set => m_compensateGameViewScale = value;
		}

		protected override void HandleConstantPixelSize()
		{
			var gameViewScale = m_compensateGameViewScale ? GameViewScale.Get() : 1f;
			SetScaleFactor(m_ScaleFactor * gameViewScale);
			SetReferencePixelsPerUnit(m_ReferencePixelsPerUnit);
		}

		/// <summary>
		/// Ratio of the rendering resolution to the physical pixels the Game view actually displays it in.
		/// 2 means the game is shown at half size on the monitor. Always 1 outside the editor.
		/// </summary>
		private static class GameViewScale
		{
#if UNITY_EDITOR
			private const double SEARCH_INTERVAL = 1d;

			private static readonly Type s_gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");

			private static readonly PropertyInfo s_targetInView = s_gameViewType?.GetProperty("targetInView",
				BindingFlags.Instance | BindingFlags.NonPublic);

			private static UnityEditor.EditorWindow s_gameView;
			private static double s_nextSearchTime;

			public static float Get()
			{
				if (s_targetInView == null) {
					return 1f;
				}

				var gameView = FindGameView();

				if (gameView == null) {
					return 1f;
				}

				var viewRect = (Rect) s_targetInView.GetValue(gameView);
				var viewWidth = viewRect.width * UnityEditor.EditorGUIUtility.pixelsPerPoint;

				if (viewWidth <= 0f) {
					return 1f;
				}

				UnityEditor.PlayModeWindow.GetRenderingResolution(out var width, out _);

				return width / viewWidth;
			}

			private static UnityEditor.EditorWindow FindGameView()
			{
				// Prefer the Game view being interacted with when several are open
				var focused = UnityEditor.EditorWindow.focusedWindow;

				if (focused != null && focused.GetType() == s_gameViewType) {
					s_gameView = focused;
					return s_gameView;
				}

				// Avoid EditorWindow.GetWindow: it creates and focuses a Game view if none exists.
				// FindObjectsOfTypeAll is slow, so only search periodically when the cached view is gone.
				if (s_gameView == null && UnityEditor.EditorApplication.timeSinceStartup >= s_nextSearchTime) {
					s_nextSearchTime = UnityEditor.EditorApplication.timeSinceStartup + SEARCH_INTERVAL;

					var views = Resources.FindObjectsOfTypeAll(s_gameViewType);
					s_gameView = views.Length > 0 ? (UnityEditor.EditorWindow) views[0] : null;
				}

				return s_gameView;
			}
#else
			public static float Get()
			{
				return 1f;
			}
#endif
		}
	}
}
