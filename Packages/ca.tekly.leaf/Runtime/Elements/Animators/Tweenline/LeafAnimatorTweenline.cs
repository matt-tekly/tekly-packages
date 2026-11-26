using System;
using Tekly.Tweenlines;
using UnityEngine;

namespace Tekly.Leaf.Elements.Animators
{
	/// <summary>
	/// The modes a <see cref="LeafTweenlineRule"/> applies in. Everything matches any mode.
	/// </summary>
	[Flags]
	public enum LeafElementModes
	{
		Normal = 1 << 0,
		Highlighted = 1 << 1,
		Pressed = 1 << 2,
	}

	/// <summary>
	/// Plays an animation while the element is in one of <see cref="Modes"/> with all of <see cref="Flags"/> set.
	/// </summary>
	[Serializable]
	public class LeafTweenlineRule
	{
		public LeafElementModes Modes => m_modes;
		public LeafElementFlags Flags => m_flags;
		public string Animation => m_animation;

		[Tooltip("The rule is used in any of these modes")]
		[SerializeField] private LeafElementModes m_modes = LeafElementModes.Normal | LeafElementModes.Highlighted | LeafElementModes.Pressed;

		[Tooltip("The rule is used while all of these flags are set. Nothing matches any flags")]
		[SerializeField] private LeafElementFlags m_flags;

		[Tooltip("The Tweenline animation to play")]
		[SerializeField] private string m_animation;

		public bool Matches(LeafElementState state)
		{
			var mode = (LeafElementModes) (1 << (int) state.Mode);
			return (m_modes & mode) != 0 && state.HasAll(m_flags);
		}
	}

	/// <summary>
	/// Plays a Tweenline animation for each state. Rules are checked in order and the first match plays, so
	/// put the most specific ones first (e.g. Disabled, then Pressed, then Highlighted, then Selected) and end
	/// with one that matches everything. Author each animation as "go to this look" with its clips starting
	/// from Current, so any state can follow any other and an interrupted change carries on smoothly.
	/// Only plays in play mode, so a state shown in the editor is never saved into the scene.
	/// </summary>
	public class LeafAnimatorTweenline : LeafAnimator
	{
		public Tweenline Tweenline => m_tweenline;

		[Tooltip("Plays the animations. Set it to Unscaled Time so buttons animate while the game is paused, and leave Stagger at 0")]
		[SerializeField] private Tweenline m_tweenline;

		[Tooltip("Checked in order, the first match plays. A state no rule matches leaves the current animation alone")]
		[SerializeField] private LeafTweenlineRule[] m_rules = Array.Empty<LeafTweenlineRule>();

		protected override void OnHandleState(LeafElementState current, LeafElementState previous, bool instant)
		{
			if (!Application.isPlaying || m_tweenline == null) {
				return;
			}

			var animationName = FindAnimation(current);

			if (string.IsNullOrEmpty(animationName)) {
				return;
			}

			var animation = m_tweenline.Find(animationName);

			if (animation == null) {
				Debug.LogWarning($"[{nameof(LeafAnimatorTweenline)}] Tweenline [{m_tweenline.name}] has no animation named [{animationName}]", this);
				return;
			}

			if (instant) {
				m_tweenline.Seek(animation, animation.Duration);
				return;
			}

			// A change that keeps the same look, like gaining a flag no rule cares about, lets it carry on
			if (m_tweenline.Current == animation) {
				return;
			}

			m_tweenline.Play(animation, false);
		}

		private string FindAnimation(LeafElementState state)
		{
			for (var i = 0; i < m_rules.Length; i++) {
				var rule = m_rules[i];
				if (rule != null && rule.Matches(state)) {
					return rule.Animation;
				}
			}

			return null;
		}

		private void Reset()
		{
			m_tweenline = GetComponent<Tweenline>();
		}
	}
}
