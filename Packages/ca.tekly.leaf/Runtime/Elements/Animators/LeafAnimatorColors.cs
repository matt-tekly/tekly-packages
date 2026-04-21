using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Leaf.Elements.Animators
{
	[Serializable]
	public class LeafAnimatorColorTarget
	{
		[SerializeField] private Graphic m_target;
		[SerializeField] private ColorBlock m_colors;
		[SerializeField] private bool m_requiredToBeOn;

		public void HandleMode(LeafElementMode mode, bool on, bool instant)
		{
			var isEnabled = !m_requiredToBeOn || on;
			m_target.gameObject.SetActive(isEnabled);
			var color = mode switch {
				LeafElementMode.Normal => m_colors.normalColor,
				LeafElementMode.Highlighted => m_colors.highlightedColor,
				LeafElementMode.Pressed => m_colors.pressedColor,
				LeafElementMode.Selected => m_colors.selectedColor,
				LeafElementMode.Disabled => m_colors.disabledColor,
				_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
			};

			var duration = instant ? 0f : m_colors.fadeDuration;
			if (duration > 0f) {
				m_target.CrossFadeColor(color, duration, false, true);	
			} else {
				m_target.canvasRenderer.SetColor(color);
				m_target.canvasRenderer.SetAlpha(color.a);
			}
		}
	}

	public class LeafAnimatorColors : LeafAnimator
	{
		[SerializeField] private LeafAnimatorColorTarget[] m_targets;

		public override void HandleMode(LeafElementMode mode, bool on, bool instant)
		{
			foreach (var target in m_targets) {
				target.HandleMode(mode, on, instant);
			}
		}
	}
}