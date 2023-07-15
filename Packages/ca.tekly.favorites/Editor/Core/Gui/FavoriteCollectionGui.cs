using UnityEditor;
using UnityEngine;

namespace Tekly.Favorites.Gui
{
	public class FavoriteCollectionGui
	{
		private readonly FavoritesData m_data;
		private readonly FavoritesWindowSettings m_settings;

		public FavoriteCollectionGui(FavoritesData data, FavoritesWindowSettings settings)
		{
			m_data = data;
			m_settings = settings;
		}
		
		public void OnGUI(Rect rect, FavoriteCollection collection, int index, bool isSelected)
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
			iconRect.width = 16f;
			iconRect.height = 16f;
			iconRect.yMin += 1;
			GUI.Label(iconRect, m_settings.CollectionContent);

			var labelRect = rect;
			labelRect.xMin = iconRect.xMax + 2f;
			
			GUI.Label(labelRect, collection.Name);

			if (!mouseIsOver) {
				return;
			}

			var deleteButton = rect;
			deleteButton.xMin = deleteButton.xMax - rect.height - 2;
			deleteButton.yMin += 1;
			deleteButton.height = rect.height - 2;
			deleteButton.width = rect.height;

			if (GuiUtils.DeleteButton(deleteButton)) {
				FavoritesData.Instance.RemoveCollection(collection);
			}
			
			var e = Event.current;

			if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform) {
				if (IsValidDragTarget(collection)) {
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

					if (e.type == EventType.DragPerform) {
						AcceptDrag(index);
					}

					e.Use();
				}
			}

			if (e.type == EventType.MouseDrag) {
				DragAndDrop.PrepareStartDrag();
				DragAndDrop.SetGenericData(FavoritesWindowGui.DragDataType, collection);
				DragAndDrop.StartDrag(collection.Name);

				Event.current.Use();
			}

			if (e.button == 0 && e.type == EventType.MouseDown) {
				FavoritesData.Instance.SetActiveCollection(collection);
			}
		}

		private void AcceptDrag(int index)
		{
			var genericData = DragAndDrop.GetGenericData(FavoritesWindowGui.DragDataType);

			if (genericData != null) {
				var collection = genericData as FavoriteCollection;
				FavoritesData.Instance.ReorderCollection(collection, index);
				DragAndDrop.AcceptDrag();
				DragAndDrop.SetGenericData(FavoritesWindowGui.DragDataType, null);
			}
		}

		private bool IsValidDragTarget(FavoriteCollection collection)
		{
			var genericData = DragAndDrop.GetGenericData(FavoritesWindowGui.DragDataType);

			if (genericData != null) {
				return genericData != collection;
			}

			return false;
		}
	}
}