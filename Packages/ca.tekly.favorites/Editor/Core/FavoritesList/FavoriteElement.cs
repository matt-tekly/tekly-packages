using System;
using Tekly.Favorites.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tekly.Favorites
{
	public class FavoriteElement : VisualElement
	{
		public static string DragDataType = "FavoriteElement";

		public new class UxmlFactory : UxmlFactory<FavoriteElement, UxmlTraits> { }

		private bool m_gotMouseDown;

		private string m_id = Guid.NewGuid().ToString();

		private Label m_label;
		private Label m_indexLabel;
		private Image m_icon;

		private VisualElement m_warning;

		private FavoriteAsset m_favorite;
		private int m_index;

		public FavoriteElement()
		{
			var tree = CommonUtils.Uxml("Editor/Core/FavoritesList/FavoriteElement.uxml");
			tree.CloneTree(this);

			m_icon = this.Q<Image>("asset-icon");
			m_label = this.Q<Label>("asset-name");
			m_label.text = m_id;

			m_indexLabel = this.Q<Label>("favorite-index");
			m_warning = this.Q<VisualElement>("warning-container");

			// this.Q<Button>("delete").clicked += OnDeleteClicked;

			RegisterCallback<MouseDownEvent>(OnPointerDownEvent);
			RegisterCallback<MouseMoveEvent>(OnPointerMoveEvent);
			RegisterCallback<MouseUpEvent>(OnPointerUpEvent);
			RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
		}

		public void WireData(FavoriteAsset favoriteAsset, int index)
		{
			m_index = index;

			if (m_index < 9) {
				m_indexLabel.text = (m_index + 1).ToString();
			} else {
				m_indexLabel.text = string.Empty;
			}

			m_favorite = favoriteAsset;

			m_icon.image = m_favorite.Icon;
			m_label.text = m_favorite.DisplayName;

			if (m_favorite.Asset == null) {
				tooltip = "Favorite target could not be found. It may have been deleted or is part of an unloaded scene.";
			} else {
				tooltip = AssetDatabase.GetAssetPath(m_favorite.Asset);
			}

			m_warning.visible = m_favorite.Asset == null;
		}

		private void OnDeleteClicked()
		{
			FavoritesData.Instance.RemoveFavorite(m_favorite);
		}

		private void OnPointerDownEvent(MouseDownEvent e)
		{
			if (e.clickCount == 2) {
				if (m_favorite.Asset is FavoriteActionAsset fa) {
					fa.Activate();
				} else {
					AssetDatabase.OpenAsset(m_favorite.Asset);
				}

				FavoritesWindow.HideIfPopup();
				e.StopPropagation();
			} else {
				m_gotMouseDown = true;
				e.StopPropagation();
			}
		}

		private void OnPointerMoveEvent(MouseMoveEvent e)
		{
			if (m_gotMouseDown && e.pressedButtons == 1) {
				StartDragging();
				m_gotMouseDown = false;
			}
		}

		private void OnPointerUpEvent(MouseUpEvent e)
		{
			if (m_gotMouseDown) {
				m_gotMouseDown = false;
				FavoritesData.Instance.SetActiveFavorite(m_favorite);

				if (e.button == 1 && e.pressedButtons == 0) {
					EditorUtility.DisplayPopupMenu(new Rect(e.mousePosition.x, e.mousePosition.y, 0, 0), "Assets/", null);
				}

				e.StopPropagation();
			}
		}

		private void StartDragging()
		{
			DragAndDrop.PrepareStartDrag();
			DragAndDrop.objectReferences = new[] { m_favorite.Asset };
			DragAndDrop.SetGenericData(DragDataType, m_favorite);
			DragAndDrop.StartDrag(m_favorite.Asset.name);
		}

		private void OnDragPerformEvent(DragPerformEvent e)
		{
			var genericData = DragAndDrop.GetGenericData(DragDataType);

			if (genericData != null) {
				FavoriteAsset fa = genericData as FavoriteAsset;
				FavoritesData.Instance.ReorderFavorite(fa, m_index);
				DragAndDrop.AcceptDrag();
				e.StopPropagation();
				DragAndDrop.SetGenericData(DragDataType, null);
				//} else if (DragAndDrop.objectReferences.All(AssetDatabase.Contains)) {
			} else {
				FavoritesData.Instance.AddFavorites(DragAndDrop.objectReferences, m_index);
				DragAndDrop.AcceptDrag();
				e.StopPropagation();
			}
		}
	}
}