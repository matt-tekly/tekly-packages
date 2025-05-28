using Tekly.Common.Observables;
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

	/// <summary>
	/// Wraps up showing and hiding a GameObject. This is a base class to reference when you need to generically show
	/// and hide something. 
	/// </summary>
	public class Presentable : MonoBehaviour
	{
		public PresentableState State {
			get => m_state;
			private set {
				if (value == m_state) {
					return;
				}

				m_state = value;
				m_stateChanged.Emit(m_state);
				OnStateChanged();
			}
		}

		public bool IsAnimating => State == PresentableState.Showing || State == PresentableState.Hiding;
		public ITriggerable<PresentableState> StateChanged => m_stateChanged;

		[SerializeField] private bool m_completeHideOnDisable;
		
		
		private readonly Triggerable<PresentableState> m_stateChanged = new Triggerable<PresentableState>();
		private PresentableState m_state;

		/// <summary>
		/// Shows the presentable. You are responsible for implementing OnShow to play an animation.
		/// The default OnShow will just call CompleteShow marking the presentable as Shown 
		/// OnShow is only called if the Presentable isn't already Showing or Shown.
		/// OnShowAttempt will always be called regardless of the state.
		/// </summary>
		public void Show()
		{
			OnShowAttempt();
			
			if (State == PresentableState.Shown || State == PresentableState.Showing) {
				return;
			}

			State = PresentableState.Showing;
			gameObject.SetActive(true);
			OnShow();
		}

		/// <summary>
		/// Hides the presentable if it isn't Hiding or Hidden.
		/// </summary>
		public void Hide()
		{
			if (State == PresentableState.Hidden || State == PresentableState.Hiding) {
				return;
			}

			State = PresentableState.Hiding;
			OnHide();
		}

		public void Present(bool show)
		{
			if (show) {
				Show();
			} else {
				Hide();
			}
		}

		/// <summary>
		/// Called when Show is called regardless of the current state
		/// </summary>
		protected virtual void OnShowAttempt()
		{
			
		}

		/// <summary>
		/// Override this to implement your own showing behaviour. You must call CompleteShow when your behaviour
		/// is done animating.
		/// </summary>
		protected virtual void OnShow()
		{
			CompleteShow();
		}

		/// <summary>
		/// Override this to implement your own hiding behaviour. You must call CompleteShow when your behaviour
		/// is done animating.
		/// </summary>
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

		protected virtual void OnDisable()
		{
			if (m_completeHideOnDisable) {
				State = PresentableState.Hidden;
			}
		}
	}
}