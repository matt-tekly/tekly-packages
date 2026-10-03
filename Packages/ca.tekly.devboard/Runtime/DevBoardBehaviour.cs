using System;
using UnityEngine;

namespace Tekly.DevBoard
{
	/// <summary>
	/// Ticks every frame, and keeps a throwing Tick from spamming the console. Widget getters often reach into
	/// game objects that can be destroyed or unloaded, so a Tick that throws is logged once, the behaviour is
	/// marked faulted, and Tick is retried at a slower rate until it succeeds again.
	/// </summary>
	public class DevBoardBehaviour : MonoBehaviour
	{
		/// <summary>
		/// True while Tick is throwing.
		/// </summary>
		public bool IsFaulted => m_faulted;

		private const float FAULTED_RETRY_INTERVAL = 0.5f;

		private bool m_faulted;
		private float m_nextRetryTime;

		private void Update()
		{
			if (m_faulted && Time.realtimeSinceStartup < m_nextRetryTime) {
				return;
			}

			try {
				Tick();
			} catch (Exception exception) {
				m_nextRetryTime = Time.realtimeSinceStartup + FAULTED_RETRY_INTERVAL;

				if (!m_faulted) {
					m_faulted = true;
					Debug.LogException(exception, this);
					OnFaulted(exception);
				}

				return;
			}

			if (m_faulted) {
				m_faulted = false;
				OnRecovered();
			}
		}

		protected virtual void Tick()
		{

		}

		/// <summary>
		/// Called once when Tick starts throwing. Use it to show an error state.
		/// </summary>
		protected virtual void OnFaulted(Exception exception)
		{

		}

		/// <summary>
		/// Called once when Tick succeeds again after being faulted.
		/// </summary>
		protected virtual void OnRecovered()
		{

		}
	}
}
