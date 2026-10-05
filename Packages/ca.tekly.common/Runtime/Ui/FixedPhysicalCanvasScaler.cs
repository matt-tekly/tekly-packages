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

			// GameView.m_ZoomArea (ZoomableArea) -> ZoomableArea.scale
			private static readonly FieldInfo s_zoomAreaField = s_gameViewType?.GetField("m_ZoomArea",
				BindingFlags.Instance | BindingFlags.NonPublic);

			private static readonly PropertyInfo s_zoomScaleProperty = s_zoomAreaField?.FieldType.GetProperty("scale",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			private static UnityEditor.EditorWindow s_gameView;
			private static double s_nextSearchTime;

			/// <remarks>
			/// The Game view draws its render target (in pixels) converted to points with its own backing scale, then
			/// zooms it, so render pixels map to monitor pixels by exactly the zoom area's scale. Using the zoom
			/// directly avoids EditorGUIUtility.pixelsPerPoint, which belongs to whichever editor window last ran
			/// OnGUI and flips between values on macOS/HiDPI as the mouse causes other windows to repaint.
			/// GameView.targetInView and PlayModeWindow.GetRenderingResolution both depend on that value internally.
			/// </remarks>
			public static float Get()
			{
				if (s_zoomScaleProperty == null) {
					return 1f;
				}

				var gameView = FindGameView();

				if (gameView == null) {
					return 1f;
				}

				var zoomArea = s_zoomAreaField.GetValue(gameView);

				if (zoomArea == null) {
					return 1f;
				}

				var zoom = ((Vector2) s_zoomScaleProperty.GetValue(zoomArea)).x;

				return zoom > 0f ? 1f / zoom : 1f;
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
