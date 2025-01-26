using System;
using System.Collections;
using System.Linq;
using Tekly.Backtick.Commands;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Backtick.Desktop
{
    public class DesktopTerminalView : TerminalView, IInputFilter
    {
        public TerminalInputField Messages;
        public TerminalInputField InputField;

        public CanvasScaler Scaler;
        public RectTransform Transform;
        
        private TerminalRoot m_container;
        private CommandStore m_commandStore;
 
        private int m_previousMessageIndex;
        private CommandSuggestion m_commandSuggestion;

        public override float GetScale()
        {
            return Scaler.scaleFactor;
        }

        public override void SetScale(float scale)
        {
            Scaler.scaleFactor = scale;
        }

        public override float GetSize()
        {
            return 1f - Transform.anchorMin.y;
        }

        public override void SetSize(float size)
        {
            Transform.anchorMin = new Vector2(Transform.anchorMin.x, 1f - size);
        }

        public override void Initialize(TerminalRoot container, CommandStore commandStore)
        {
            m_container = container;
            m_commandStore = commandStore;
            m_commandStore.MessagesChanged += OnMessagesChanged;
            m_previousMessageIndex = m_commandStore.CommandHistory.Count;

            m_commandSuggestion = new CommandSuggestion(commandStore);
            InputField.onValueChanged.AddListener(InputValueChanged);
        }

        private void InputValueChanged(string value)
        {
            m_commandSuggestion.Clear();
        }

        public void Close()
        {
            m_container.SetActive(false);
        }

        private void Awake()
        {
            InputField.InputFilter = this;
            Messages.InputFilter = this;
            
            Messages.text = $"Enter {"help".Gray()} for information.";
            
            ClearInputText();
        }

        private void OnEnable()
        {
            StartCoroutine(SelectInput());
        }
        
        private IEnumerator SelectInput()
        {
            yield return new WaitForEndOfFrame();
            
            InputField.Select();
            InputField.ActivateInputField();

            yield return new WaitForEndOfFrame();

            InputField.caretPosition = InputField.text.Length;
            InputField.ForceLabelUpdate();
        }

        private void ClearInputText()
        {
            InputField.text = "";
        }

        public bool HandleKeyCode(TerminalInputField input, Event keyEvent)
        {
            if (input == Messages) {
                switch (keyEvent.keyCode) {
                    case KeyCode.Escape: {
                        Close();
                        return true;
                    }
                    case KeyCode.BackQuote: {
                        Close();
                        return true;
                    }
                    case KeyCode.Tab: {
                        InputField.Select();
                        InputField.ActivateInputField();
                        return true;
                    }
                }
                
                return false;
            }
            
            switch (keyEvent.keyCode) {
                case KeyCode.UpArrow: {
                    m_previousMessageIndex = Math.Max(m_previousMessageIndex - 1, 0);
                    if (m_commandStore.CommandHistory.Count > 0) {
                        InputField.text = m_commandStore.CommandHistory[m_previousMessageIndex];
                        InputField.caretPosition = InputField.text.Length;
                    }

                    return true;
                }
                case KeyCode.DownArrow: {
                    m_previousMessageIndex =
                        Math.Min(m_previousMessageIndex + 1, m_commandStore.CommandHistory.Count - 1);
                    if (m_commandStore.CommandHistory.Count > 0) {
                        InputField.text = m_commandStore.CommandHistory[m_previousMessageIndex];
                        InputField.caretPosition = InputField.text.Length;
                    }

                    return true;
                }
                case KeyCode.Escape: {
                    if (string.IsNullOrEmpty(InputField.text)) {
                        Close();
                    } else {
                        ClearInputText();
                        m_previousMessageIndex = m_commandStore.CommandHistory.Count;
                    }

                    return true;
                }
                case KeyCode.Return:
                case KeyCode.KeypadEnter: {
                    if (string.IsNullOrWhiteSpace(InputField.text)) {
                        InputField.text = "";
                        return true;
                    }
                    
                    m_commandStore.Execute(InputField.text);
                    ClearInputText();
                    StartCoroutine(SelectInput());
                    m_previousMessageIndex = m_commandStore.CommandHistory.Count;

                    return true;
                }
                case KeyCode.BackQuote: {
                    Close();
                    return true;
                }
                case KeyCode.Tab: {
                    if (!m_commandSuggestion.HasInput) {
                        m_commandSuggestion.SetInput(InputField.text);    
                    } else {
                        if (m_commandSuggestion.HasSuggestions) {
                            if (keyEvent.shift) {
                                m_commandSuggestion.Decrement();
                            } else {
                                m_commandSuggestion.Increment();
                            }
                        }
                    }
                    
                    if (m_commandSuggestion.HasSuggestions) {
                        InputField.SetTextWithoutNotify(m_commandSuggestion.Suggestion.Id);
                        InputField.caretPosition = InputField.text.Length;
                    }
                    
                    return true;
                }
            }

            return false;
        }

        private void OnMessagesChanged()
        {
            Messages.text = string.Join("\n", m_commandStore.Messages.Select(x => x.ToConsoleMessage()));
            Messages.textComponent.text = Messages.text;
        }
    }
}