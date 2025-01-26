using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.Backtick.Desktop
{
    /// <summary>
    /// When this is selected it passes the focus to the Messages ConsoleInputField
    /// </summary>
    public class SelectablePassThrough : Selectable
    {
        public TerminalInputField Messages;

        private int m_queue;
        
        public override void OnSelect(BaseEventData eventData)
        {
            m_queue = 2;
            Messages.Select();
            Messages.ActivateInputField();
        }

        private void Update()
        {
            m_queue--;
            if (m_queue == 0) {
                Messages.caretPosition = 0;
                Messages.ForceLabelUpdate();
            }
        }
    }
}