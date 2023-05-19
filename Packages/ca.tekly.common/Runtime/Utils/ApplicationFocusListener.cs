using Tekly.Common.LifeCycles;
using Tekly.Common.Observables;
using UnityEngine;

namespace Tekly.Common.Utils
{
	public class ApplicationFocusListener : Singleton<ApplicationFocusListener>
	{
		public readonly Triggerable<Unit> Suspended = new Triggerable<Unit>();
		public readonly Triggerable<Unit> Focused = new Triggerable<Unit>();

		public ApplicationFocusListener()
		{
			LifeCycle.Instance.Focus += OnFocus;
			LifeCycle.Instance.Pause += OnPause;
			LifeCycle.Instance.Quit += OnQuit;
		}

		public void Dispose()
		{
			LifeCycle.Instance.Focus -= OnFocus;
			LifeCycle.Instance.Pause -= OnPause;
			LifeCycle.Instance.Quit -= OnQuit;
		}

		private void OnQuit()
		{
			PossiblyQuitting();
			Dispose();
		}

		private void OnPause(bool paused)
		{
			if (ShouldUsePause()) {
				if (paused) {
					PossiblyQuitting();
				} else {
					Refocused();
				}
			}
		}

		private void OnFocus(bool hasFocus)
		{
			if (ShouldUseFocus()) {
				if (!hasFocus) {
					PossiblyQuitting();
				} else {
					Refocused();
				}
			}
		}

		private void PossiblyQuitting()
		{
			Suspended.Emit(Unit.Default);
		}

		private void Refocused()
		{
			Focused.Emit(Unit.Default);
		}

		private bool ShouldUseFocus()
		{
			return Application.platform is RuntimePlatform.tvOS or RuntimePlatform.IPhonePlayer;
		}

		private bool ShouldUsePause()
		{
			return Application.platform is RuntimePlatform.Android;
		}
	}
}