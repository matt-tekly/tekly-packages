using UnityEngine;

namespace Tekly.Common.Presentables
{
	public enum PresentableState
	{
		Hidden,
		Showing,
		Shown,
		Hiding,
	}

	public class Presentable : MonoBehaviour
	{
		public PresentableState State {
			get => m_state;
			private set {
				if (value == m_state) {
					return;
				}

				m_state = value;
				OnStateChanged();
			}
		}

		public bool IsAnimating => State == PresentableState.Showing || State == PresentableState.Hiding;

		private PresentableState m_state;

		public void Show()
		{
			if (State == PresentableState.Shown || State == PresentableState.Showing) {
				return;
			}

			State = PresentableState.Showing;
			gameObject.SetActive(true);
			OnShow();
		}

		public void Hide()
		{
			if (State == PresentableState.Hidden || State == PresentableState.Hiding) {
				return;
			}

			State = PresentableState.Hiding;
			OnHide();
		}

		protected virtual void OnShow()
		{
			CompleteShow();
		}

		protected virtual void OnHide()
		{
			CompleteHide();
		}

		public void CompleteShow()
		{
			State = PresentableState.Shown;
		}

		public void CompleteHide()
		{
			State = PresentableState.Hidden;
			gameObject.SetActive(false);
		}

		protected virtual void OnStateChanged()
		{
			
		}
	}
}