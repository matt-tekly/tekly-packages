using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Tekly.Leaf.Elements
{
	public class LeafButton : LeafSelectable, IPointerClickHandler, ISubmitHandler
    {
        [Serializable]
        public class ButtonClickedEvent : UnityEvent {}

        [SerializeField] private ButtonClickedEvent m_onClick = new();
        
        public ButtonClickedEvent OnClick
        {
            get => m_onClick;
            set => m_onClick = value;
        }

        protected virtual void Press()
        {
	        if (!IsActive() || !IsInteractable()) {
		        return;
	        }
	        
            UISystemProfilerApi.AddMarker("Button.OnPress", this);
            OnPress();
        }
        
        protected virtual void OnPress()
        {
	        m_onClick.Invoke();
        }
        
        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) {
	            return;
            }

            Press();
        }
        public virtual void OnSubmit(BaseEventData eventData)
        {
            Press();

            // if we get set disabled during the press
            // don't run the coroutine.
            if (!IsActive() || !IsInteractable()) {
	            return;
            }

            DoStateTransition(SelectionState.Pressed, false);
            StartCoroutine(OnFinishSubmit());
        }

        private IEnumerator OnFinishSubmit()
        {
            var fadeTime = colors.fadeDuration;
            var elapsedTime = 0f;

            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            DoStateTransition(currentSelectionState, false);
        }
	}
}