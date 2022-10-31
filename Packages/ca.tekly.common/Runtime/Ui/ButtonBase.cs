using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Tekly.Common.Ui
{
    public enum ButtonState
    {
        Down,
        Up,
        Disabled
    }

    [ExecuteAlways]
    [SelectionBase]
    [DisallowMultipleComponent]
    public class ButtonBase : UIBehaviour,
        IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler, IPointerClickHandler
    {
        [Serializable]
        public class ClickEvent : UnityEvent<ButtonBase> { }

         public bool Interactable {
            get => m_interactable;
            set {
                if (m_interactable != value) {
                    m_interactable = value;
                    
                    m_state = DetermineState();
                    DoStateTransition(m_state, false);
                }
            }
        }

        public ClickEvent OnClick => m_onClick;
        public ClickEvent OnClickDisabled => m_onClickDisabled;

        [SerializeField] private bool m_interactable = true;
        [SerializeField] private ClickEvent m_onClick;
        [SerializeField] private ClickEvent m_onClickDisabled;

        protected ButtonState m_state = ButtonState.Up;

        private bool m_groupsAllowInteraction = true;
        private bool m_isPointerDown;
        private bool m_enableCalled;

        private static readonly List<CanvasGroup> s_canvasGroupCache = new List<CanvasGroup>();

        protected override void OnEnable()
        {
            // Check to avoid multiple OnEnable()
            if (m_enableCalled) {
                return;
            }

            base.OnEnable();

            m_isPointerDown = false;
            m_enableCalled = true;

            m_state = DetermineState();
            DoStateTransition(m_state, true);
        }

        protected override void OnDisable()
        {
            // Check to avoid multiple OnDisable()
            if (!m_enableCalled) {
                return;
            }

            base.OnDisable();
            m_isPointerDown = false;
            m_enableCalled = false;
        }

        public virtual bool IsInteractable()
        {
            return m_groupsAllowInteraction && m_interactable;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) {
                return;
            }

            Press();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) {
                return;
            }

            if (EventSystem.current != null) {
                EventSystem.current.SetSelectedGameObject(gameObject, eventData);
            }

            m_isPointerDown = true;
            EvaluateAndTransitionToSelectionState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) {
                return;
            }

            m_isPointerDown = false;
            EvaluateAndTransitionToSelectionState();
        }

        public void OnSelect(BaseEventData eventData) { }

        public void OnDeselect(BaseEventData eventData) { }
        
        protected virtual void DoStateTransition(ButtonState state, bool instant) { }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();

            // If our parent changes figure out if we are under a new CanvasGroup.
            OnCanvasGroupChanged();
        }

        protected override void OnCanvasGroupChanged()
        {
            // Figure out if parent groups allow interaction
            // If no interaction is allowed then we need to not do that
            var groupAllowInteraction = true;
            var t = transform;

            while (t != null) {
                t.GetComponents(s_canvasGroupCache);
                var shouldBreak = false;
                for (var i = 0; i < s_canvasGroupCache.Count; i++) {
                    // Ff the parent group does not allow interaction we need to break
                    if (!s_canvasGroupCache[i].interactable) {
                        groupAllowInteraction = false;
                        shouldBreak = true;
                    }

                    // If this is a 'fresh' group, then break as we should not consider parents
                    if (s_canvasGroupCache[i].ignoreParentGroups) {
                        shouldBreak = true;
                    }
                }

                if (shouldBreak) {
                    break;
                }

                t = t.parent;
            }

            if (groupAllowInteraction != m_groupsAllowInteraction) {
                m_groupsAllowInteraction = groupAllowInteraction;
                OnSetProperty();
            }
        }
        
        private void EvaluateAndTransitionToSelectionState()
        {
            if (!IsActive() || !IsInteractable()) {
                return;
            }

            var state = DetermineState();

            if (state != m_state) {
                m_state = state;
                DoStateTransition(m_state, false);
            }
        }

        private ButtonState DetermineState()
        {
            if (!IsInteractable()) {
                return ButtonState.Disabled;
            }

            if (m_isPointerDown) {
                return ButtonState.Down;
            }

            return ButtonState.Up;
        }
        
        private void Press()
        {
            if (!IsActive()) {
                return;
            }

            if (IsInteractable()) {
                UISystemProfilerApi.AddMarker("BaseButton.OnClick", this);
                OnClick.Invoke(this);
            } else {
                UISystemProfilerApi.AddMarker("BaseButton.OnClickDisabled", this);
                OnClickDisabled.Invoke(this);
            }
        }

        private void OnSetProperty()
        {
            var state = DetermineState();
            m_state = state;
            DoStateTransition(m_state, !Application.isPlaying);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            // OnValidate can be called before OnEnable, this makes it unsafe to access other components
            // since they might not have been initialized yet.
            // OnSetProperty potentially access Animator or Graphics. (case 618186)
            if (isActiveAndEnabled) {
                if (!m_interactable && EventSystem.current != null &&
                    EventSystem.current.currentSelectedGameObject == gameObject) {
                    EventSystem.current.SetSelectedGameObject(null);
                }

                OnSetProperty();
            }
        }
#endif
    }
}