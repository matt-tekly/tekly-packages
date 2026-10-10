using System;
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

		[Tooltip("Shows the On state through the animator, e.g. the current choice in a list or the current tab. Clicking doesn't change it")]
		[SerializeField] private bool m_isOn;

		public ButtonClickedEvent OnClicked {
			get => m_onClick;
			set => m_onClick = value;
		}

		/// <summary>
		/// Whether the animator shows the On state. Unlike a toggle, clicking never changes it: whatever owns
		/// the button decides what's on.
		/// </summary>
		public bool IsOn {
			get => m_isOn;
			set => SetIsOn(value, false);
		}

		protected override bool IsOnState => m_isOn;

		private bool m_isPressPending;
		private IDisposable m_disableInputScope;

		/// <summary>
		/// Sets the On state. instant skips the animator's transition, e.g. when a pooled row is reused.
		/// </summary>
		public void SetIsOn(bool isOn, bool instant)
		{
			if (m_isOn == isOn) {
				return;
			}

			m_isOn = isOn;
			DoStateTransition(currentSelectionState, instant);
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
			if (eventData.button != PointerEventData.InputButton.Left || this.IsLeafInputDisabled()) {
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
			if (this.IsLeafInputDisabled()) {
				return;
			}

			SimulatePress();
		}

		/// <summary>
		/// Shows the pressed state, then presses after the press delay. Ignored while a press is already pending.
		/// </summary>
		public void SimulatePress()
		{
			// Also keeps a quick double submit from pressing twice
			if (!IsActive() || !IsInteractable() || m_isPressPending) {
				return;
			}

			m_isPressPending = true;
			SetPressSimulated(true);
			StartCoroutine(PressDelayCoroutine(m_pressDelay));
		}

		protected override void OnDisable()
		{
			// Disabling stops the coroutine without running the rest of it, so release here
			m_isPressPending = false;
			ReleaseInput();

			base.OnDisable();
		}

		private IEnumerator PressDelayCoroutine(float delay)
		{
			m_disableInputScope = LeafCore.Instance.DisableInputScope(this);

			var elapsedTime = 0f;
			while (elapsedTime < delay) {
				elapsedTime += Time.unscaledDeltaTime;
				yield return null;
			}

			m_isPressPending = false;
			ReleaseInput();

			SetPressSimulated(false);
			Press();
		}

		private void ReleaseInput()
		{
			m_disableInputScope?.Dispose();
			m_disableInputScope = null;
		}
	}
}
