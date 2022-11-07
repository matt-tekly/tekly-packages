using Tekly.Favorites.Common;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tekly.Favorites
{
    public class CollectionsListView : VisualElement
    {
        public const string DragDataType = "CollectionDrag";

        public new class UxmlFactory : UxmlFactory<CollectionsListView, UxmlTraits> { }
        
        private ListView m_listView;
        private DropTargetElement m_dropTargetElement;

        private bool m_keyDown;

        public CollectionsListView()
        {
            var tree = CommonUtils.Uxml("Editor/Core/CollectionsList/CollectionsListView.uxml");
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
            m_listView.itemsSource = FavoritesData.Instance.Collections;
            m_listView.selectedIndex = FavoritesData.Instance.ActiveCollectionIndex;
            m_listView.Rebuild();

            this.Q<ToolbarButton>("add").clicked += FavoritesData.Instance.AddNewCollection;
            this.Q<ToolbarButton>("delete").clicked += FavoritesData.Instance.RemoveActiveCollection;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            RegisterCallback<KeyDownEvent>(HandleKeyDownEvent);
            RegisterCallback<KeyUpEvent>(HandleKeyUpEvent);
        }

        private void OnActiveCollectionChanged(FavoriteCollection obj)
        {
            m_listView.itemsSource = FavoritesData.Instance.Collections;
            m_listView.selectedIndex = FavoritesData.Instance.ActiveCollectionIndex;
            m_listView.Rebuild();
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (evt.destinationPanel == null) {
                return;
            }

            Undo.undoRedoPerformed += OnUndo;

            FavoritesData.Instance.ActiveCollectionChanged += OnActiveCollectionChanged;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (evt.originPanel == null) {
                return;
            }

            Undo.undoRedoPerformed -= OnUndo;

            FavoritesData.Instance.ActiveCollectionChanged -= OnActiveCollectionChanged;
        }

        private void BindListViewItem(VisualElement element, int index)
        {
            try { 
                CollectionElement collectionElement = element as CollectionElement;
                var collection = FavoritesData.Instance.Collections[index];
                collectionElement.WireData(collection, index);
            } catch (System.Exception ex) {
                Debug.LogError("Error at index " + index);
                Debug.LogException(ex);
            }
        }

        private VisualElement MakeListItem()
        {
            return new CollectionElement();
        }

        private void HandleKeyUpEvent(KeyUpEvent evt)
        {
            m_keyDown = false;
        }

        private void HandleKeyDownEvent(KeyDownEvent evt)
        {
            if (m_keyDown) {
                return;
            }

            if (evt.keyCode == KeyCode.Delete && m_listView.selectedIndex > -1) {
                FavoritesData.Instance.RemoveCollection(m_listView.selectedIndex);
                evt.StopPropagation();
            }
        }

        private void OnUndo()
        {
            m_listView.Rebuild();
        }

        private void OnDropped()
        {
            // TODO: Reorder collections
            // var genericData = DragAndDrop.GetGenericData(FavoriteElement.DragDataType);

            // if (genericData != null) {
            //     FavoriteAsset fa = genericData as FavoriteAsset;
            //     FavoritesData.Instance.ReorderFavorite(fa, FavoritesData.Instance.ActiveCollection.Favorites.Count - 1);
            //     DragAndDrop.AcceptDrag();
            // } else {
            //     FavoritesData.Instance.AddFavorites(DragAndDrop.objectReferences);
            //     DragAndDrop.AcceptDrag();
            // }
        }

        private bool IsValidDropTarget()
        {
            return DragAndDrop.GetGenericData(DragDataType) != null;
        }
    }
}
