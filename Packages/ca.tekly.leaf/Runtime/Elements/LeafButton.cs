using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Tekly.Leaf.Elements
{
	public class LeafButton : LeafSelectable, IPointerClickHandler, ISubmitHandler
	{
		[Tooltip("Add delay to when the press or submit is processed")]
		[SerializeField] private float m_pressDelay;
		
		[Serializable]
		public class ButtonClickedEvent : UnityEvent { }

		[SerializeField] private ButtonClickedEvent m_onClick = new();

		public ButtonClickedEvent OnClick {
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

			if (m_pressDelay > 0) {
				StartCoroutine(PressDelayCoroutine(m_pressDelay));
			} else {
				Press();	
			}
		}

		public virtual void OnSubmit(BaseEventData eventData)
		{
			DoStateTransition(SelectionState.Pressed, false);
			StartCoroutine(PressDelayCoroutine(3f));
		}

		private IEnumerator PressDelayCoroutine(float delay)
		{
			yield return null;
			
			using (LeafCore.Instance.DisableEventSystemScope(this)) {
				var fadeTime = delay;
				var elapsedTime = 0f;

				while (elapsedTime < fadeTime) {
					elapsedTime += Time.unscaledDeltaTime;
					yield return null;
				}
			}
			
			DoStateTransition(currentSelectionState, false);
			
			Press();
		}
	}
}