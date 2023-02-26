using Tekly.Favorites.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tekly.Favorites
{
	public class FavoritesWindow : EditorWindow
	{
		private const int WIDTH = 250;
		private const int HEIGHT = 190;

		private static FavoritesWindow s_instance;

		private bool m_isPopup;
		private bool m_moveToMouse;

		public static void Present()
		{
			if (s_instance != null) {
				if (s_instance.m_isPopup) {
					s_instance.Close();
				} else {
					GetWindow<FavoritesWindow>();
				}

				return;
			}

			s_instance = CreateInstance<FavoritesWindow>();

			int x = (Screen.currentResolution.width - WIDTH) / 2;
			int y = (Screen.currentResolution.height - HEIGHT) / 2;

			s_instance.position = new Rect(x, y, WIDTH, HEIGHT);
			s_instance.m_isPopup = true;
			s_instance.m_moveToMouse = true;
			s_instance.ShowPopup();
		}

		public static void HideIfPopup()
		{
			if (s_instance != null && s_instance.m_isPopup) {
				s_instance.Close();
			}
		}

		private void OnEnable()
		{
			s_instance = this;
			var texture = CommonUtils.Texture("Editor/Core/Assets/heart.png");
			titleContent = new GUIContent("Favorites", texture);

			var xmlAsset = CommonUtils.Uxml("Editor/Core/FavoritesWindow.uxml");
			xmlAsset.CloneTree(rootVisualElement);
			rootVisualElement.viewDataKey = "tekly/favoriteswindow";
			rootVisualElement.focusable = true;
			rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);
		}

		private void OnGUI()
		{
			if (m_moveToMouse && Event.current != null) {
				var mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
				var x = mousePos.x;
				var y = mousePos.y;

				position = new Rect(x, y, WIDTH, HEIGHT);

				m_moveToMouse = false;
			}
		}

		private void OnDestroy()
		{
			if (s_instance == this) {
				s_instance = null;
			}
		}

		private void OnKeyDown(KeyDownEvent evt)
		{
			if (evt.keyCode is >= KeyCode.Alpha0 and <= KeyCode.Alpha9) {
				evt.StopPropagation();
				if (FavoritesData.Instance.HandleShortcut(evt.keyCode, evt.shiftKey || evt.ctrlKey) && m_isPopup) {
					Close();
				}
			}

			if (evt.keyCode == KeyCode.Escape && m_isPopup) {
				evt.StopPropagation();
				Close();
			}
		}
	}
}