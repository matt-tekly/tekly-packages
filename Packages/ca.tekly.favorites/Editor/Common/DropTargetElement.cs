using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Tekly.Favorites
{
    public class DropTargetElement : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<DropTargetElement, UxmlTraits> { }

        public event Action Dropped;

        private const string DragClass = "dragging";

        private Func<bool> m_validator;

        public DropTargetElement()
        {
            RegisterCallback<DragEnterEvent>(OnDragEnterEvent);
            RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            RegisterCallback<DragExitedEvent>(OnDragExitedEvent);
        }

        public void SetDragDataValidator(Func<bool> validator)
        {
            m_validator = validator;
        }

        private void OnDragEnterEvent(DragEnterEvent e)
        {
            if (m_validator == null || !m_validator.Invoke()) {
                return;
            }

            SetDragging(true);
        }

        private void OnDragLeaveEvent(DragLeaveEvent e)
        {
            if (m_validator == null || !m_validator.Invoke()) {
                return;
            }

            SetDragging(false);
        }

        private void OnDragUpdatedEvent(DragUpdatedEvent e)
        {
            if (m_validator == null || !m_validator.Invoke()) {
                return;
            }

            SetDragging(true);
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        }

        private void OnDragPerformEvent(DragPerformEvent e)
        {
            if (m_validator == null || !m_validator.Invoke()) {
                return;
            }

            Dropped?.Invoke();

            DragAndDrop.AcceptDrag();
        }

        private void OnDragExitedEvent(DragExitedEvent e)
        {
            SetDragging(false);
        }

        private void SetDragging(bool isDragging)
        {
            EnableInClassList(DragClass, isDragging);
        }
    }
}
