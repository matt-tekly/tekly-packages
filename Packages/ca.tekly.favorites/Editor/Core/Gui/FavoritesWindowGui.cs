using UnityEditor;
using UnityEngine;

namespace Tekly.Favorites.Gui
{
	public class FavoritesWindowGui
	{
		public static string DragDataType = "FavoriteElement";

		private readonly float m_headerHeight = 20f;
		private readonly float m_rowHeight = 18f;

		private readonly FavoritesData m_data;
		private readonly FavoritesWindowSettings m_settings;
		private readonly FavoritesWindow m_window;

		private readonly FavoriteCollectionGui m_collectionGui;
		private readonly FavoriteCollectionEditGui m_collectionEditGui;
		private readonly FavoriteAssetGui m_favoriteGui;

		private bool m_edit;
		private bool m_shift;

		public FavoritesWindowGui(FavoritesData data, FavoritesWindowSettings settings, FavoritesWindow window)
		{
			m_data = data;
			m_settings = settings;
			m_window = window;

			m_collectionGui = new FavoriteCollectionGui(data, settings);
			m_collectionEditGui = new FavoriteCollectionEditGui(data, settings);
			m_favoriteGui = new FavoriteAssetGui(data, settings, window);
		}

		public void OnGUI(Rect rect, bool hasFocus)
		{
			DrawHeader(rect);

			var listRect = rect;
			listRect.yMin = m_headerHeight;

			DrawGutter(listRect);

			if (m_edit) {
				HandleEditCollections(listRect);
			} else if (Event.current.shift && hasFocus) {
				HandleCollections(listRect);
			} else {
				HandleFavorites(listRect, m_data.ActiveCollection);
			}

			if (hasFocus) {
				var lineRect = rect;
				lineRect.yMax = lineRect.yMin + 2;
				EditorGUI.DrawRect(lineRect, m_settings.FocusedColor);
			}

			HandleDrag();
			HandleKeys();
		}

		private void DrawHeader(Rect rect)
		{
			var collection = m_data.ActiveCollection;

			var headerRect = rect;
			headerRect.height = m_headerHeight;

			var iconRect = headerRect;
			iconRect.xMin += 2;
			iconRect.yMin += 2;
			iconRect.width = 16;
			iconRect.height = 16;

			GUI.Label(iconRect, m_settings.CollectionContent);

			headerRect.xMin = iconRect.xMax + 2;
			GUI.Label(headerRect, collection.Name);

			headerRect.xMin = headerRect.xMax - 28f;
			headerRect.xMax += 1;
			m_edit = GUI.Toggle(headerRect, m_edit, "Edit", EditorStyles.toolbarButton);

			var separatorRect = rect;
			separatorRect.yMin = m_headerHeight - 1;
			separatorRect.yMax = m_headerHeight;
			EditorGUI.DrawRect(separatorRect, new Color(0f, 0f, 0f, .3f));
		}

		private void DrawGutter(Rect rect)
		{
			var gutterRect = rect;
			gutterRect.width = 13f;

			EditorGUI.DrawRect(gutterRect, new Color(0f, 0f, 0f, .3f));

			var lineRect = gutterRect;
			lineRect.xMin = 12f;
			lineRect.xMax = 13f;
			EditorGUI.DrawRect(lineRect, new Color(0f, 0f, 0f, .3f));
		}

		private void HandleFavorites(Rect rect, FavoriteCollection collection)
		{
			for (var index = 0; index < collection.Favorites.Count; index++) {
				var favorite = collection.Favorites[index];

				var favoriteRect = new Rect(0, m_headerHeight + index * m_rowHeight, rect.width, m_rowHeight);
				m_favoriteGui.OnGUI(favoriteRect, favorite, index, m_data.FavoriteIndex == index);
			}
		}

		private void HandleCollections(Rect rect)
		{
			var plusButtonRect = rect;
			plusButtonRect.xMin = plusButtonRect.xMax - 22;
			plusButtonRect.yMin = plusButtonRect.yMax - 20;
			plusButtonRect.height = 18f;
			plusButtonRect.width = 20f;

			if (GuiUtils.AddButton(plusButtonRect)) {
				m_data.AddNewCollection();
			}

			for (var index = 0; index < m_data.Collections.Count; index++) {
				var collection = m_data.Collections[index];

				var favoriteRect = new Rect(0, m_headerHeight + index * m_rowHeight, rect.width, m_rowHeight);
				m_collectionGui.OnGUI(favoriteRect, collection, index, m_data.ActiveCollectionIndex == index);
			}
		}
		
		private void HandleEditCollections(Rect rect)
		{
			var plusButtonRect = rect;
			plusButtonRect.xMin = plusButtonRect.xMax - 22;
			plusButtonRect.yMin = plusButtonRect.yMax - 22;
			plusButtonRect.height = 20f;
			plusButtonRect.width = 20f;

			if (GuiUtils.AddButton(plusButtonRect)) {
				m_data.AddNewCollection();
			}

			for (var index = 0; index < m_data.Collections.Count; index++) {
				var collection = m_data.Collections[index];

				var favoriteRect = new Rect(0, m_headerHeight + index * m_rowHeight, rect.width, m_rowHeight);
				m_collectionEditGui.OnGUI(favoriteRect, collection, index);
			}
		}

		private void HandleDrag()
		{
			var e = Event.current;

			if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

				if (e.type == EventType.DragPerform) {
					AcceptDrag();
				}

				e.Use();
			}
		}

		private static void AcceptDrag()
		{
			var genericData = DragAndDrop.GetGenericData(DragDataType);

			if (genericData != null) {
				if (genericData is FavoriteAsset fa) {
					FavoritesData.Instance.OrderFavoriteToEnd(fa);
					DragAndDrop.AcceptDrag();
					DragAndDrop.SetGenericData(DragDataType, null);
				} else if (genericData is FavoriteCollection collection) {
					FavoritesData.Instance.OrderCollectionToEnd(collection);
					DragAndDrop.AcceptDrag();
					DragAndDrop.SetGenericData(DragDataType, null);
				}
			} else {
				FavoritesData.Instance.AddFavoritesToEnd(DragAndDrop.objectReferences);
				DragAndDrop.AcceptDrag();
			}
		}
		
		private void HandleKeys()
		{
			var evt = Event.current;
			
			// Shift has to be cached rom Repaint because it is unreliably reported in key event
			if (evt.type == EventType.Repaint) {
				m_shift = evt.shift || evt.modifiers == EventModifiers.Shift;
			}
			
			if (evt.type != EventType.KeyDown) {
				return;
			}
			
			if (FavoritesData.Instance.HandleShortcut(evt.keyCode, m_shift || m_edit, evt)) {
				m_window.Close();
			}

			if (evt.keyCode == KeyCode.Escape && m_window.IsPopup) {
				m_window.Close();
				evt.Use();
			}

			if (evt.keyCode == KeyCode.K) {
				Selection.activeObject = FavoritesData.Instance;
			}
		}
	}
}