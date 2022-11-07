using System;
using Tekly.Favorites.Common;
using UnityEditor;
using UnityEngine.UIElements;

namespace Tekly.Favorites
{
    public class CollectionElement : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<CollectionElement, UxmlTraits> { }

        private bool m_gotMouseDown;

        private string id = Guid.NewGuid().ToString();

        private EditableLabel m_label;
        private Label m_indexLabel;
        private Image m_icon;

        private FavoriteCollection m_collection;
        private int m_index;

        public CollectionElement()
        {
            var tree = CommonUtils.Uxml("Editor/Core/CollectionsList/CollectionElement.uxml");
            tree.CloneTree(this);

            m_icon = this.Q<Image>("asset-icon");
            m_label = this.Q<EditableLabel>("asset-name");
            m_label.Text = id;

            m_indexLabel = this.Q<Label>("favorite-index");
            m_label.TextChanged += OnCollectionRenamed;

            this.Q<Button>().clicked += OnDeleteClicked;
            RegisterCallback<MouseDownEvent>(OnPointerDownEvent);
            RegisterCallback<MouseMoveEvent>(OnPointerMoveEvent);
            RegisterCallback<MouseUpEvent>(OnPointerUpEvent);
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
        }

        private void OnCollectionRenamed(string text)
        {
            FavoritesData.Instance.RenameCollection(text, m_collection);
            m_label.Text = text;
        }

        public void WireData(FavoriteCollection collection, int index)
        {
            m_index = index;

            if (m_index < 9) {
                m_indexLabel.text = (m_index + 1).ToString();
            } else {
                m_indexLabel.text = string.Empty;
            }
            
            m_collection = collection;
            m_label.Text = m_collection.Name;
        }

        private void OnDeleteClicked()
        {
            FavoritesData.Instance.RemoveCollection(m_collection);
        }

        private void OnPointerDownEvent(MouseDownEvent e)
        {
            m_gotMouseDown = true;
            e.StopPropagation();
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
                FavoritesData.Instance.SetActiveCollection(m_collection);
                e.StopPropagation();
            }  
        }

        private void StartDragging()
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData(CollectionsListView.DragDataType, m_collection);
            DragAndDrop.StartDrag(id);
        }

        private void OnDragPerformEvent(DragPerformEvent e)
        {
            var genericData = DragAndDrop.GetGenericData(CollectionsListView.DragDataType);

            if (genericData != null) {
                FavoriteCollection collection = genericData as FavoriteCollection;
                FavoritesData.Instance.ReorderCollection(collection, m_index);
                DragAndDrop.AcceptDrag();
                e.StopPropagation();
            }
        }
    }
}