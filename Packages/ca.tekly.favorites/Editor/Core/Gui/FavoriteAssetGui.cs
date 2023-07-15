using UnityEditor;
using UnityEngine;

namespace Tekly.Favorites.Gui
{
	public class FavoriteAssetGui
	{
		private readonly FavoritesData m_data;
		private readonly FavoritesWindowSettings m_settings;
		private readonly FavoritesWindow m_window;

		public FavoriteAssetGui(FavoritesData data, FavoritesWindowSettings settings, FavoritesWindow window)
		{
			m_data = data;
			m_settings = settings;
			m_window = window;
		}

		public void OnGUI(Rect rect, FavoriteAsset favoriteAsset, int index, bool isSelected)
		{
			var mouseIsOver = rect.Contains(Event.current.mousePosition);

			if (isSelected) {
				EditorGUI.DrawRect(rect, m_settings.SelectedColor);
			} else if (mouseIsOver) {
				EditorGUI.DrawRect(rect, m_settings.HoverColor);
			}

			var indexRect = rect;
			indexRect.width = 12f;
			GUI.Label(indexRect, (index + 1).ToString());

			var iconRect = rect;
			iconRect.xMin = indexRect.xMax + 2f;
			iconRect.width = 18f;
			iconRect.yMin += 1;
			GUI.Label(iconRect, favoriteAsset.Icon);

			var labelRect = rect;
			labelRect.xMin = iconRect.xMax + 2f;

			GUI.Label(labelRect, favoriteAsset.DisplayName);

			if (!mouseIsOver) {
				return;
			}

			var deleteButton = rect;
			deleteButton.xMin = deleteButton.xMax - rect.height - 2;
			deleteButton.yMin += 1;
			deleteButton.height = rect.height - 2;
			deleteButton.width = rect.height;

			if (GuiUtils.DeleteButton(deleteButton)) {
				m_data.RemoveFavorite(favoriteAsset);
			}

			var e = Event.current;

			if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

				if (e.type == EventType.DragPerform) {
					AcceptDrag(index);
				}

				e.Use();
			}

			if (e.type == EventType.MouseDrag) {
				DragAndDrop.PrepareStartDrag();
				DragAndDrop.objectReferences = new[] {favoriteAsset.Asset};
				DragAndDrop.SetGenericData(FavoritesWindowGui.DragDataType, favoriteAsset);
				DragAndDrop.StartDrag(favoriteAsset.DisplayName);

				Event.current.Use();
			}

			if (e.button == 0 && e.type == EventType.MouseDown) {
				if (e.clickCount > 1) {
					if (favoriteAsset.Asset is FavoriteActionAsset fa) {
						fa.Activate();
					} else {
						AssetDatabase.OpenAsset(favoriteAsset.Asset);
					}

					m_window.HideIfPopup();
				} else {
					m_data.SetActiveFavorite(favoriteAsset);
				}
			}
		}

		private void AcceptDrag(int index)
		{
			var genericData = DragAndDrop.GetGenericData(FavoritesWindowGui.DragDataType);

			if (genericData != null) {
				var fa = genericData as FavoriteAsset;
				m_data.ReorderFavorite(fa, index);
				DragAndDrop.AcceptDrag();
				DragAndDrop.SetGenericData(FavoritesWindowGui.DragDataType, null);
			} else {
				m_data.AddFavorites(DragAndDrop.objectReferences, index);
				DragAndDrop.AcceptDrag();
			}
		}
	}
}