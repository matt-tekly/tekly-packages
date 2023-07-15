using Tekly.Favorites.Gui;
using UnityEditor;
using UnityEngine;

namespace Tekly.Favorites
{
	public class FavoritesWindow : EditorWindow
	{
		[SerializeField] private FavoritesWindowSettings m_settings;

		private static FavoritesWindow s_instance;

		private bool m_isPopup;
		private bool m_moveToMouse;

		private FavoritesWindowGui m_favoritesWindowGui;

		public bool IsPopup => m_isPopup;
		
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

			var x = (Screen.currentResolution.width - s_instance.m_settings.Width) / 2;
			var y = (Screen.currentResolution.height - s_instance.m_settings.Height) / 2;

			s_instance.position = new Rect(x, y, s_instance.m_settings.Width, s_instance.m_settings.Height);
			s_instance.m_isPopup = true;
			s_instance.m_moveToMouse = true;
			s_instance.ShowPopup();
			s_instance.Focus();
		}

		public void HideIfPopup()
		{
			if (s_instance != null && s_instance.m_isPopup) {
				s_instance.Close();
			}
		}

		private void OnEnable()
		{
			wantsMouseMove = true;
			s_instance = this;
			titleContent = m_settings.WindowTitleContent;

			FavoritesData.Instance.TryToUpdateIcons();
		}

		private void OnGUI()
		{
			if (m_favoritesWindowGui == null) {
				m_favoritesWindowGui = new FavoritesWindowGui(FavoritesData.Instance, m_settings, this);
			}

			m_favoritesWindowGui.OnGUI(new Rect(0, 0, position.width, position.height), focusedWindow == this);

			if (m_moveToMouse && Event.current != null) {
				var mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
				var x = mousePos.x;
				var y = mousePos.y;

				position = new Rect(x, y, s_instance.m_settings.Width, s_instance.m_settings.Height);

				m_moveToMouse = false;
			}

			Repaint();
		}

		private void OnDestroy()
		{
			if (s_instance == this) {
				s_instance = null;
			}
		}
	}
}