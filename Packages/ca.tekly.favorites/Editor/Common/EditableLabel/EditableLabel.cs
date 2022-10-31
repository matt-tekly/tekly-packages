using System;
using Tekly.Favorites.Common;
using UnityEngine.UIElements;

namespace Tekly.Favorites
{
    public class EditableLabel : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<EditableLabel, UxmlTraits> { }

        private bool m_isEditing;

        private Label m_label;
        private TextField m_textField;

        public string Text {
            set {
                m_label.text = value;
                m_textField.value = value;
            }            
        }

        public event Action<string> TextChanged;

        public EditableLabel()
        {
            var tree = CommonUtils.Uxml("Editor/Common/EditableLabel/EditableLabel.uxml");
            tree.CloneTree(this);

            m_label = this.Q<Label>();
            m_textField = this.Q<TextField>();
            m_textField.style.display = DisplayStyle.None;

            m_textField.RegisterCallback<BlurEvent>(OnBlur);

            RegisterCallback<PointerDownEvent>(OnPointerDownEvent);
        }

        private void OnBlur(BlurEvent evt)
        {
            m_textField.style.display = DisplayStyle.None;
            m_label.style.display = DisplayStyle.Flex;

            TextChanged?.Invoke(m_textField.text);
        }

        private void OnPointerDownEvent(PointerDownEvent e)
        {
            if (e.isPrimary && e.button == 0 && e.clickCount == 2) {
                m_textField.style.display = DisplayStyle.Flex;
                m_label.style.display = DisplayStyle.None;
                m_textField.value = m_label.text;
            
                schedule.Execute(() => {
                    m_textField.Q("unity-text-input").Focus();
                });
            }
        }
    }
}