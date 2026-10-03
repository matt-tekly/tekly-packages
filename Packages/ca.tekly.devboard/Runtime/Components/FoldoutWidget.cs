using UnityEngine;

namespace Tekly.DevBoard.Components
{
	/// <summary>
	/// A container that opens and closes. Whether it's open is saved in DevBoard.State under its label (or the key
	/// set with WithStateKey), so a rebuilt foldout reopens the way the user left it.
	/// </summary>
	public class FoldoutWidget : ContainerWidget
	{
		public bool Expanded {
			get => m_content.gameObject.activeSelf;
			set => SetExpanded(value);
		}

		public ContainerWidget InfoContainer => m_infoContainer;

		[SerializeField] private ContainerWidget m_infoContainer;
		[SerializeField] private ButtonWidget m_button;

		private string m_stateKey;

		private void Awake()
		{
			m_button.Initialize(Toggle, "Toggle");
		}

		public void Initialize(string label)
		{
			m_button.Label = label;
			UseStateKey(label);
		}

		/// <summary>
		/// Saves state under key instead of the label. Use it when two foldouts in the same container share a label.
		/// Call it before adding widgets to the foldout, since their keys are scoped under this one.
		/// </summary>
		public FoldoutWidget WithStateKey(string key)
		{
			UseStateKey(key);
			return this;
		}

		/// <summary>
		/// Sets whether the foldout starts open. Ignored when there's saved state from an earlier build.
		/// </summary>
		public FoldoutWidget StartExpanded(bool expanded)
		{
			if (!TryGetSavedExpanded(out _)) {
				m_content.gameObject.SetActive(expanded);
			}

			return this;
		}

		public void Toggle()
		{
			Expanded = !Expanded;
		}

		private void SetExpanded(bool expanded)
		{
			m_content.gameObject.SetActive(expanded);
			DevBoard.Instance?.State.Set(m_stateKey, expanded);
		}

		private void UseStateKey(string key)
		{
			// Widgets inside this foldout get their state keys scoped under it
			StateScope = key;
			m_stateKey = DevBoardState.KeyFor(this, key);

			if (TryGetSavedExpanded(out var expanded)) {
				m_content.gameObject.SetActive(expanded);
			}
		}

		private bool TryGetSavedExpanded(out bool expanded)
		{
			expanded = false;
			var state = DevBoard.Instance?.State;
			return state != null && state.TryGet(m_stateKey, out expanded);
		}
	}
}
