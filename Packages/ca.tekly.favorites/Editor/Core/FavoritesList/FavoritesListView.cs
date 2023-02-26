using System;
using Tekly.Favorites.Common;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tekly.Favorites
{
	public class FavoritesListView : VisualElement
	{
		public new class UxmlFactory : UxmlFactory<FavoritesListView, UxmlTraits> { }

		private ListView m_listView;
		private DropTargetElement m_dropTargetElement;
		private ToolbarMenu m_toolbarMenu;
		private EditableLabel m_editableLabel;

		private bool m_keyDown;

		public FavoritesListView()
		{
			var tree = CommonUtils.Uxml("Editor/Core/FavoritesList/FavoritesListView.uxml");
			tree.CloneTree(this);

			m_dropTargetElement = this.Q<DropTargetElement>();
			m_dropTargetElement.StretchToParentSize();

			m_dropTargetElement.SetDragDataValidator(IsValidDropTarget);
			m_dropTargetElement.Dropped += OnDropped;

			m_listView = this.Q<ListView>();
			m_dropTargetElement.Add(m_listView);

			m_listView.StretchToParentSize();
			m_listView.fixedItemHeight = 18;

			m_listView.selectionType = SelectionType.Single;
			m_listView.bindItem = BindListViewItem;
			m_listView.makeItem = MakeListItem;
			m_listView.itemsSource = FavoritesData.Instance.ActiveCollection.Favorites;
			m_listView.selectedIndex = FavoritesData.Instance.FavoriteIndex;
			m_listView.focusable = true;

			m_editableLabel = this.Q<EditableLabel>();
			m_editableLabel.TextChanged += FavoritesData.Instance.RenameActiveCollection;
			m_toolbarMenu = this.Q<ToolbarMenu>();

			SetupToolbar();

			RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
			RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

			RegisterCallback<KeyDownEvent>(HandleKeyDownEvent);
			RegisterCallback<KeyUpEvent>(HandleKeyUpEvent);
		}

		private void OnActiveCollectionChanged(FavoriteCollection obj)
		{
			OnFavoritesChanged();

			m_toolbarMenu.text = obj.Name;
			m_editableLabel.Text = obj.Name;
		}

		private void OnFavoritesChanged()
		{
			m_listView.itemsSource = FavoritesData.Instance.ActiveCollection.Favorites;
			m_listView.Rebuild();
			m_listView.selectedIndex = -1;

			SetupToolbar();
		}

		private void OnFavoriteIndexChanged(int index)
		{
			m_listView.selectedIndex = index;
			m_listView.Rebuild();
		}

		private void OnAttachToPanel(AttachToPanelEvent evt)
		{
			if (evt.destinationPanel == null) {
				return;
			}

			Undo.undoRedoPerformed += OnUndo;

			FavoritesData.Instance.FavoriteIndexChanged += OnFavoriteIndexChanged;
			FavoritesData.Instance.ActiveCollectionChanged += OnActiveCollectionChanged;
			FavoritesData.Instance.FavoritesChanged += OnFavoritesChanged;
			FavoritesData.Instance.AssetRenamed += OnAssetRenamed;
			m_listView.Focus();
		}

		private void OnDetachFromPanel(DetachFromPanelEvent evt)
		{
			if (evt.originPanel == null) {
				return;
			}

			Undo.undoRedoPerformed -= OnUndo;

			FavoritesData.Instance.FavoriteIndexChanged -= OnFavoriteIndexChanged;
			FavoritesData.Instance.ActiveCollectionChanged -= OnActiveCollectionChanged;
			FavoritesData.Instance.FavoritesChanged -= OnFavoritesChanged;
			FavoritesData.Instance.AssetRenamed -= OnAssetRenamed;
		}

		private void OnAssetRenamed()
		{
			m_listView.Rebuild();
		}

		private void BindListViewItem(VisualElement element, int index)
		{
			try {
				FavoriteElement favoriteElement = (FavoriteElement)element;
				favoriteElement.WireData(FavoritesData.Instance.ActiveCollection.Favorites[index], index);
			} catch (Exception ex) {
				Debug.LogError("Error at index " + index);
				Debug.LogException(ex);
			}
		}

		private VisualElement MakeListItem()
		{
			return new FavoriteElement();
		}

		private void HandleKeyUpEvent(KeyUpEvent evt)
		{
			m_keyDown = false;

			if (evt.keyCode == KeyCode.Escape) {
				FavoritesWindow.HideIfPopup();
			}

			if (evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Return) {
				evt.StopPropagation();
				if (FavoritesData.Instance.HandleShortcut(m_listView.selectedIndex + KeyCode.Alpha1, evt.shiftKey || evt.ctrlKey)) {
					FavoritesWindow.HideIfPopup();
				}
			}
		}

		private void HandleKeyDownEvent(KeyDownEvent evt)
		{
			if (m_keyDown) {
				return;
			}

			if (evt.keyCode == KeyCode.Delete && m_listView.selectedIndex > -1) {
				evt.StopPropagation();
				FavoritesData.Instance.RemoveFavorite(m_listView.selectedIndex);
				m_listView.selectedIndex = FavoritesData.Instance.FavoriteIndex;
			}
		}

		private void OnUndo()
		{
			m_listView.selectedIndex = FavoritesData.Instance.FavoriteIndex;
			m_listView.Rebuild();
		}

		private void OnDropped()
		{
			var genericData = DragAndDrop.GetGenericData(FavoriteElement.DragDataType);

			if (genericData != null) {
				FavoriteAsset fa = genericData as FavoriteAsset;
				FavoritesData.Instance.ReorderFavorite(fa, FavoritesData.Instance.ActiveCollection.Favorites.Count - 1);
				DragAndDrop.AcceptDrag();

				DragAndDrop.SetGenericData(FavoriteElement.DragDataType, null);
			} else {
				FavoritesData.Instance.AddFavorites(DragAndDrop.objectReferences);
				DragAndDrop.AcceptDrag();
			}
		}

		private bool IsValidDropTarget()
		{
			return true;
		}

		private void SetupToolbar()
		{
			m_toolbarMenu.text = FavoritesData.Instance.ActiveCollection.Name;
			m_editableLabel.Text = FavoritesData.Instance.ActiveCollection.Name;
			m_toolbarMenu.menu.MenuItems().Clear();

			for (int i = 0; i < FavoritesData.Instance.Collections.Count; i++) {
				FavoriteCollection collection = FavoritesData.Instance.Collections[i];
				m_toolbarMenu.menu.AppendAction($"{i + 1}. {collection.Name}", OnCollectionMenuSelected, OnCollectionMenuStatus, collection);
			}

			m_toolbarMenu.menu.AppendSeparator();
			m_toolbarMenu.menu.AppendAction("Add new collection", _ => FavoritesData.Instance.AddNewCollection());
			m_toolbarMenu.menu.AppendAction("Remove collection", _ => FavoritesData.Instance.RemoveCollection(FavoritesData.Instance.ActiveCollection));
		}

		private void OnCollectionMenuSelected(DropdownMenuAction action)
		{
			FavoriteCollection collection = action.userData as FavoriteCollection;
			FavoritesData.Instance.SetActiveCollection(collection);
		}

		private DropdownMenuAction.Status OnCollectionMenuStatus(DropdownMenuAction action) => DropdownMenuAction.Status.Normal;
	}
}