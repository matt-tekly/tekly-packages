using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tekly.Leaf.Elements
{
	public class LeafButton : LeafSelectable, IPointerClickHandler, ISubmitHandler, ILeafButton
	{
		[Tooltip("Add delay to when the press or submit is processed")]
		[SerializeField] private float m_pressDelay;
		
		[SerializeField] private ButtonClickedEvent m_onClick = new();

		public ButtonClickedEvent OnClicked {
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
				SimulatePress();
			} else {
				Press();	
			}
		}

		public virtual void OnSubmit(BaseEventData eventData)
		{
			SimulatePress();
		}

		public void SimulatePress()
		{
			DoStateTransition(SelectionState.Pressed, false);
			StartCoroutine(PressDelayCoroutine(m_pressDelay));
		}

		private IEnumerator PressDelayCoroutine(float delay)
		{
			using (LeafCore.Instance.DisableInputScope(this)) {
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