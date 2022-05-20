using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.Common.Terminal
{
    public interface IInputFilter
    {
        bool HandleKeyCode(TerminalInputField input, Event keyEvent);
    }

    public class TerminalInputField : InputField
    {
        public IInputFilter InputFilter;

        private readonly Event m_processingEvent = new Event();

        protected override void Awake()
        {
            base.Awake();
        }
        
        public override void OnUpdateSelected(BaseEventData eventData)
        {
            if (!isFocused) {
                return;
            }

            var consumedEvent = false;
            while (Event.PopEvent(m_processingEvent)) {
                if (m_processingEvent.rawType == EventType.KeyDown) {
                    consumedEvent = true;

                    if (InputFilter.HandleKeyCode(this, m_processingEvent)) {
                        break;
                    }

                    if (m_processingEvent.keyCode == KeyCode.C && (m_processingEvent.control || m_processingEvent.command)) {
                        HandleCopyText();
                        break;
                    }

                    var shouldContinue = KeyPressed(m_processingEvent);
                    if (shouldContinue == EditState.Finish) {
                        if (!wasCanceled) {
                            SendOnSubmit();
                        }

                        DeactivateInputField();
                        break;
                    }
                }

                switch (m_processingEvent.type) {
                    case EventType.ValidateCommand:
                    case EventType.ExecuteCommand:
                        switch (m_processingEvent.commandName) {
                            case "SelectAll":
                                SelectAll();
                                consumedEvent = true;
                                break;
                        }

                        break;
                }
            }

            if (consumedEvent) {
                UpdateLabel();
            }

            eventData.Use();
        }

        private void HandleCopyText()
        {
            if (caretPositionInternal == caretSelectPositionInternal) {
                return;
            }

            var startPos = caretPositionInternal;
            var endPos = caretSelectPositionInternal;

            // Ensure startPos is always less then endPos to make the code simpler
            if (startPos > endPos) {
                (startPos, endPos) = (endPos, startPos);
            }

            var selectedText = text.Substring(startPos, endPos - startPos);
            var strippedText = RemoveRichText(selectedText);
            GUIUtility.systemCopyBuffer = strippedText;
        }

        private static string RemoveRichText(string input)
        {
            input = RemoveRichTextDynamicTag(input, "color");
            input = RemoveRichTextDynamicTag(input, "size");

            input = RemoveRichTextTag(input, "b");
            input = RemoveRichTextTag(input, "i");
            
            return input;
        }
        
        private static string RemoveRichTextDynamicTag(string input, string tag)
        {
            while (true) {
                var index = input.IndexOf($"<{tag}=", StringComparison.Ordinal);
                
                if (index != -1) {
                    var endIndex = input.Substring(index, input.Length - index).IndexOf('>');
                    if (endIndex > 0) {
                        input = input.Remove(index, endIndex + 1);
                    }

                    continue;
                }

                input = RemoveRichTextTag(input, tag, false);
                return input;
            }
        }

        private static string RemoveRichTextTag(string input, string tag, bool isStart = true)
        {
            while (true) {
                var index = input.IndexOf(isStart ? $"<{tag}>" : $"</{tag}>", StringComparison.Ordinal);
                if (index != -1) {
                    input = input.Remove(index, 2 + tag.Length + (!isStart).GetHashCode());
                    continue;
                }

                if (isStart) {
                    input = RemoveRichTextTag(input, tag, false);
                }

                return input;
            }
        }
    }
}