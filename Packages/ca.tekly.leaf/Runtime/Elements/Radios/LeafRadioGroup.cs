using System;
using System.Collections.Generic;
using Tekly.Common.Utils;
using Tekly.Leaf.Elements.Animators;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tekly.Leaf.Elements.Radios
{
	/// <summary>
	/// Keeps one of the <see cref="ILeafRadioOption"/>s below it on. The group is never selectable itself, so it works
	/// with options that take keyboard focus (<see cref="LeafRadioOption"/>) and ones that don't
	/// (<see cref="LeafRadioOptionUnselectable"/>). Options are ordered by hierarchy.
	///
	/// Keyboard, for focusable options: arrows along <see cref="m_layoutAxis"/> move between the options, other
	/// arrows and Tab leave the group, and arrowing or tabbing into the group lands on the current option.
	/// The group's animator gets the Selected flag while one of its options has focus, e.g. for an outline.
	/// </summary>
	[DisallowMultipleComponent]
	public class LeafRadioGroup : MonoBehaviour
	{
		/// <summary>
		/// The option that's on, or null. An option destroyed while the group was hidden reads as null.
		/// </summary>
		public ILeafRadioOption Current => IsAlive(m_current) ? m_current : null;
		public int CurrentIndex => IndexOf(Current);
		public UnityEvent<int> OnChanged => m_onChanged;

		/// <summary>
		/// True while one of this group's options has keyboard focus.
		/// </summary>
		public bool HasFocus => m_hasFocus;

		public bool AllowNone => m_allowNone;
		public bool SelectionFollowsFocus => m_selectionFollowsFocus;

		/// <summary>
		/// Tab only lands on the current option, so the group is one stop. Always true when selection follows
		/// focus, or tabbing past would change the choice.
		/// </summary>
		public bool SingleTabStop => m_singleTabStop || m_selectionFollowsFocus;

		public bool Interactable {
			get => m_interactable;
			set {
				if (m_interactable == value) {
					return;
				}

				m_interactable = value;
				RefreshOptions();
				UpdateAnimator(false);
			}
		}

		[Tooltip("Clicking the current option turns it off, and no option has to be on")]
		[FormerlySerializedAs("m_allowNoOption")]
		[FormerlySerializedAs("_allowNoOption")]
		[SerializeField] private bool m_allowNone;

		[SerializeField] private bool m_interactable = true;

		[Tooltip("The direction the options are laid out in. Arrows along it move between the options")]
		[SerializeField] private LayoutAxis m_layoutAxis;

		[Tooltip("Arrow keys past the last option go back to the first")]
		[SerializeField] private bool m_wrap = true;

		[Tooltip("Moving between options with the arrow keys also turns them on")]
		[SerializeField] private bool m_selectionFollowsFocus;

		[Tooltip("Tab only lands on the current option. Always on when selection follows focus")]
		[SerializeField] private bool m_singleTabStop = true;

		[Tooltip("The index of the option turned on, -1 for none")]
		[SerializeField] private UnityEvent<int> m_onChanged = new();

		[Tooltip("Shows the group as a whole: Selected while one of its options has focus, Disabled when not interactable")]
		[SerializeField] private LeafAnimator m_animator;

		private ILeafRadioOption m_current;
		private IDisposable m_selectionSubscription;
		private bool m_hasFocus;

		private static readonly List<ILeafRadioOption> s_options = new();

		/// <summary>
		/// Turns option on and the current one off, then fires <see cref="OnChanged"/>. Null turns everything off,
		/// if the group allows none.
		/// </summary>
		public void Select(ILeafRadioOption option)
		{
			if (CanSelect(option)) {
				Apply(option, true);
			}
		}

		/// <summary>
		/// <see cref="Select"/> without any events, for pushing a value in from a model.
		/// </summary>
		public void SetWithoutNotify(ILeafRadioOption option)
		{
			if (CanSelect(option)) {
				Apply(option, false);
			}
		}

		public void SelectIndex(int index)
		{
			Select(GetOption(index));
		}

		public void SetIndexWithoutNotify(int index)
		{
			SetWithoutNotify(GetOption(index));
		}

		/// <summary>
		/// Turns on the next (or previous) option that's active and interactable, e.g. for shoulder buttons or
		/// Ctrl+Tab on a tab bar. Returns false when there's no other option to go to.
		/// </summary>
		public bool SelectNext(bool isReverse)
		{
			var next = FindAvailable(Current, isReverse ? -1 : 1, false);
			if (next == null) {
				return false;
			}

			Select(next);
			return true;
		}

		public ILeafRadioOption GetOption(int index)
		{
			CollectOptions(s_options);
			var option = index >= 0 && index < s_options.Count ? s_options[index] : null;
			s_options.Clear();

			return option;
		}

		public int IndexOf(ILeafRadioOption option)
		{
			if (option == null) {
				return -1;
			}

			CollectOptions(s_options);
			var index = s_options.IndexOf(option);
			s_options.Clear();

			return index;
		}

		internal void OnOptionPressed(ILeafRadioOption option)
		{
			if (!m_interactable) {
				return;
			}

			if (option == Current) {
				if (m_allowNone) {
					Select(null);
				}

				return;
			}

			Select(option);
		}

		internal void OnOptionEnabled(ILeafRadioOption option)
		{
			// The first option to show up becomes current when one is required. No event: binders read the value
			if (Current == null && !m_allowNone) {
				Apply(option, false);
			}
		}

		internal void OnOptionDisabled(ILeafRadioOption option)
		{
			if (option != Current) {
				return;
			}

			// The whole group is being hidden, keep the choice for when it comes back
			if (!gameObject.activeInHierarchy) {
				return;
			}

			Apply(m_allowNone ? null : FindAvailable(null, 1, false), true);
		}

		/// <summary>
		/// For a focusable option's OnMove: moves focus to the next option along the layout axis.
		/// Returns false for other directions, or at the end of the options without wrapping.
		/// </summary>
		internal bool TryMove(ILeafRadioOption from, MoveDirection direction)
		{
			var step = m_layoutAxis == LayoutAxis.Horizontal
				? direction == MoveDirection.Left ? -1 : direction == MoveDirection.Right ? 1 : 0
				: direction == MoveDirection.Up ? -1 : direction == MoveDirection.Down ? 1 : 0;

			if (step == 0) {
				return false;
			}

			var next = FindAvailable(from, step, true);
			var eventSystem = EventSystem.current;

			if (next == null || eventSystem == null) {
				return false;
			}

			eventSystem.SetSelectedGameObject(next.transform.gameObject);

			// Only arrow moves change the choice: tabbing or arrowing into the group lands on the current option
			if (m_selectionFollowsFocus) {
				Select(next);
			}

			return true;
		}

		/// <summary>
		/// Where keyboard focus lands when entering the group: the current option, or the first focusable one.
		/// </summary>
		internal Selectable GetEntrySelectable()
		{
			if (Current is Selectable current && LeafNavigationScope.IsNavigable(current)) {
				return current;
			}

			var next = FindAvailable(null, 1, true);
			return next as Selectable;
		}

		private bool CanSelect(ILeafRadioOption option)
		{
			if (option == null) {
				if (!m_allowNone) {
					Debug.LogWarning($"Radio group [{name}] can't turn every option off unless it allows none", this);
					return false;
				}

				return true;
			}

			if (option.Group != this) {
				Debug.LogWarning($"Radio group [{name}] was given an option from another group", this);
				return false;
			}

			return true;
		}

		private void Apply(ILeafRadioOption option, bool notify)
		{
			var previous = Current;
			if (option == previous) {
				return;
			}

			m_current = option;

			previous?.RefreshState(notify);
			option?.RefreshState(notify);

			if (notify) {
				m_onChanged.Invoke(IndexOf(option));
			}
		}

		private ILeafRadioOption FindAvailable(ILeafRadioOption from, int step, bool focusableOnly)
		{
			CollectOptions(s_options);

			var count = s_options.Count;
			var index = from != null ? s_options.IndexOf(from) : -1;

			if (index < 0) {
				index = step > 0 ? -1 : count;
			}

			ILeafRadioOption result = null;

			for (var i = 1; i <= count; i++) {
				var position = index + step * i;

				if (!m_wrap && (position < 0 || position >= count)) {
					break;
				}

				var candidate = s_options[(position % count + count) % count];

				if (candidate != from && IsAvailable(candidate, focusableOnly)) {
					result = candidate;
					break;
				}
			}

			s_options.Clear();
			return result;
		}

		private static bool IsAlive(ILeafRadioOption option)
		{
			// Unity's null check, which an interface reference skips
			return option is Object unityObject ? unityObject != null : option != null;
		}

		private static bool IsAvailable(ILeafRadioOption option, bool focusableOnly)
		{
			if (!option.isActiveAndEnabled || !option.IsInteractable()) {
				return false;
			}

			return !focusableOnly || option is Selectable selectable && LeafNavigationScope.IsNavigable(selectable);
		}

		private void RefreshOptions()
		{
			CollectOptions(s_options);

			// Copied first: refreshing can run code that uses the shared list
			var options = s_options.ToArray();
			s_options.Clear();

			for (var i = 0; i < options.Length; i++) {
				options[i].RefreshState(false);
			}
		}

		/// <summary>
		/// The active options of this group in hierarchy order. Options of nested groups are left out.
		/// </summary>
		private void OnSelectionChanged(GameObject selected)
		{
			var hasFocus = selected != null && selected.TryGetComponent(out ILeafRadioOption option) && option.Group == this;
			if (hasFocus == m_hasFocus) {
				return;
			}

			m_hasFocus = hasFocus;
			UpdateAnimator(false);
		}

		private void UpdateAnimator(bool instant)
		{
			if (m_animator == null) {
				return;
			}

			var flags = m_hasFocus ? LeafElementFlags.Selected : LeafElementFlags.None;
			if (!m_interactable) {
				flags |= LeafElementFlags.Disabled;
			}

			m_animator.HandleState(new LeafElementState(LeafElementMode.Normal, flags), instant);
		}

		private void CollectOptions(List<ILeafRadioOption> output)
		{
			GetComponentsInChildren(false, output);

			for (var i = output.Count - 1; i >= 0; i--) {
				if (output[i].Group != this) {
					output.RemoveAt(i);
				}
			}
		}

		private void OnEnable()
		{
			m_selectionSubscription = LeafCore.Instance.Selection.Current.Subscribe(OnSelectionChanged);
			UpdateAnimator(true);
		}

		private void OnDisable()
		{
			m_selectionSubscription?.Dispose();
			m_selectionSubscription = null;
			m_hasFocus = false;
		}
	}
}
