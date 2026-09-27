using System;
using UnityEngine;

namespace Tekly.Leaf.Elements.Animators
{
	/// <summary>
	/// The pointer/press interaction state of an element. Selection, disabled and on are tracked
	/// separately on <see cref="LeafElementState"/> so they can be combined with any mode.
	/// </summary>
	public enum LeafElementMode
	{
		/// No pointer interaction.
		Normal,
		/// The pointer is over the element.
		Highlighted,
		/// The element is being pressed (pointer down, submit or a simulated press).
		Pressed,
	}

	/// <summary>
	/// Extra state that can be combined with any <see cref="LeafElementMode"/>.
	/// </summary>
	[Flags]
	public enum LeafElementFlags
	{
		None = 0,
		/// The element is the EventSystem's current selection.
		Selected = 1 << 0,
		/// The element isn't interactable.
		Disabled = 1 << 1,
		/// The element is toggled on (toggles, radio options).
		On = 1 << 2,
	}

	/// <summary>
	/// Full visual state of a Leaf element. Unlike Unity's SelectionState an element can be
	/// Selected and Highlighted/Pressed at the same time.
	/// </summary>
	public readonly struct LeafElementState : IEquatable<LeafElementState>
	{
		public static readonly LeafElementState Default = new LeafElementState(LeafElementMode.Normal, LeafElementFlags.None);

		public readonly LeafElementMode Mode;
		public readonly LeafElementFlags Flags;

		public LeafElementState(LeafElementMode mode, LeafElementFlags flags)
		{
			Mode = mode;
			Flags = flags;
		}

		public bool IsHighlighted => Mode == LeafElementMode.Highlighted;
		public bool IsPressed => Mode == LeafElementMode.Pressed;
		public bool IsSelected => (Flags & LeafElementFlags.Selected) != 0;
		public bool IsDisabled => (Flags & LeafElementFlags.Disabled) != 0;
		public bool IsOn => (Flags & LeafElementFlags.On) != 0;

		/// <summary>
		/// True if every flag in <paramref name="flags"/> is set. <see cref="LeafElementFlags.None"/> always matches.
		/// </summary>
		public bool HasAll(LeafElementFlags flags) => (Flags & flags) == flags;

		/// <summary>
		/// True if at least one flag in <paramref name="flags"/> is set.
		/// </summary>
		public bool HasAny(LeafElementFlags flags) => (Flags & flags) != 0;

		public LeafElementState WithMode(LeafElementMode mode) => new LeafElementState(mode, Flags);

		public LeafElementState WithFlags(LeafElementFlags flags, bool value)
		{
			return new LeafElementState(Mode, value ? Flags | flags : Flags & ~flags);
		}

		public bool Equals(LeafElementState other) => Mode == other.Mode && Flags == other.Flags;
		public override bool Equals(object obj) => obj is LeafElementState other && Equals(other);
		public override int GetHashCode() => ((int) Mode << 8) | (int) Flags;

		public static bool operator ==(LeafElementState left, LeafElementState right) => left.Equals(right);
		public static bool operator !=(LeafElementState left, LeafElementState right) => !left.Equals(right);

		public override string ToString() => Flags == LeafElementFlags.None ? Mode.ToString() : $"{Mode} [{Flags}]";
	}

	public abstract class LeafAnimator : MonoBehaviour
	{
		public LeafElementState CurrentState => m_previousState;

		/// <summary>
		/// True once a state has been handled, before that <see cref="CurrentState"/> is just the default.
		/// </summary>
		protected bool HasState => m_hasStateSet;

		private LeafElementState m_previousState = LeafElementState.Default;
		private bool m_hasStateSet;

		/// <summary>
		/// Applies <paramref name="state"/>. Repeated states are skipped unless <paramref name="instant"/>:
		/// instant requests (OnEnable, OnValidate) always re-apply, because the visuals can be reset
		/// without the state changing, e.g. the editor rebuilds graphics when a scene is saved.
		/// </summary>
		public void HandleState(LeafElementState state, bool instant)
		{
			if (m_hasStateSet && !instant && state == m_previousState) {
				return;
			}

			OnHandleState(state, m_previousState, instant);
			m_previousState = state;

			m_hasStateSet = true;
		}

		protected abstract void OnHandleState(LeafElementState current, LeafElementState previous, bool instant);
	}
}
