using Tekly.Common.Utils.Tweening;
using UnityEngine;

namespace Tekly.Trellis
{
	/// <summary>
	/// Eases one axis of a LayoutItem's reported size toward its content's size, for
	/// <see cref="LayoutItem.SizeTransition"/>. The first size is taken straight away; later changes ease from
	/// wherever the size is at that moment, so a change mid-transition carries on smoothly. Unscaled time, so it
	/// runs while the game is paused.
	/// </summary>
	internal sealed class SizeTween
	{
		private float m_from;
		private float m_to;
		private float m_startTime;
		private bool m_hasSize;

		public bool IsRunning(float duration)
		{
			return m_hasSize && Time.unscaledTime < m_startTime + duration;
		}

		/// <summary>
		/// The measure to report: the content's, with its preferred size eased. Min is lowered to match while
		/// growing, or the parent couldn't make it smaller than the content. Returns true when a new transition
		/// starts.
		/// </summary>
		public bool Apply(ref LayoutMeasure measure, float duration, Ease ease)
		{
			var now = Time.unscaledTime;
			var target = measure.Preferred;
			var started = false;

			if (!m_hasSize) {
				m_from = target;
				m_to = target;
				m_startTime = now - duration;
				m_hasSize = true;
			} else if (!Mathf.Approximately(target, m_to)) {
				m_from = Evaluate(now, duration, ease);
				m_to = target;
				m_startTime = now;
				started = true;
			}

			var shown = Evaluate(now, duration, ease);
			measure = LayoutMeasure.Create(Mathf.Min(measure.Min, shown), shown, measure.Flexible, measure.Max,
				measure.MarginStart, measure.MarginEnd);

			return started;
		}

		/// <summary>
		/// Forgets the size, so the next one is taken straight away, e.g. after the item was hidden.
		/// </summary>
		public void Reset()
		{
			m_hasSize = false;
		}

		private float Evaluate(float now, float duration, Ease ease)
		{
			var t = duration > 0f ? Mathf.Clamp01((now - m_startTime) / duration) : 1f;

			// Unclamped, so eases that overshoot, like OutBack, can
			return Mathf.LerpUnclamped(m_from, m_to, t >= 1f ? 1f : Easing.Evaluate(t, ease));
		}
	}
}
