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

		[Tooltip("Optional. Shown while open, e.g. a collapse icon")]
		[SerializeField] private GameObject m_expandedIcon;

		[Tooltip("Optional. Shown while closed, e.g. an expand icon")]
		[SerializeField] private GameObject m_collapsedIcon;

		private string m_stateKey;

		private void Awake()
		{
			// Awake can run after Initialize when the foldout is created inside an inactive container,
			// so keep whatever label is already set
			m_button.Initialize(Toggle, m_button.Label);
			ShowExpanded(Expanded);
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
				ShowExpanded(expanded);
			}

			return this;
		}

		public void Toggle()
		{
			Expanded = !Expanded;
		}

		private void SetExpanded(bool expanded)
		{
			ShowExpanded(expanded);
			DevBoard.Instance?.State.Set(m_stateKey, expanded);
		}

		/// <summary>
		/// Opens or closes the content and swaps the icons, without saving.
		/// </summary>
		private void ShowExpanded(bool expanded)
		{
			m_content.gameObject.SetActive(expanded);

			if (m_expandedIcon != null) {
				m_expandedIcon.SetActive(expanded);
			}

			if (m_collapsedIcon != null) {
				m_collapsedIcon.SetActive(!expanded);
			}
		}

		/// <summary>
		/// Saves state under exactly key, rather than a key built from where the foldout is. For views that know
		/// their own keys, like rendered nodes.
		/// </summary>
		internal void UseFullStateKey(string key)
		{
			m_stateKey = key;

			if (TryGetSavedExpanded(out var expanded)) {
				ShowExpanded(expanded);
			}
		}

		private void UseStateKey(string key)
		{
			// Widgets inside this foldout get their state keys scoped under it
			StateScope = key;
			UseFullStateKey(WidgetState.KeyFor(this, key));
		}

		private bool TryGetSavedExpanded(out bool expanded)
		{
			expanded = false;
			var state = DevBoard.Instance?.State;
			return state != null && state.TryGet(m_stateKey, out expanded);
		}
	}

	public static class FoldoutWidgetFactory
	{
		public static FoldoutWidget Foldout(this ContainerWidget parent, string label, WidgetPrefab<FoldoutWidget> prefab = default)
		{
			var instance = parent.Create(prefab.Or(WidgetPrefabs.Foldout));
			instance.Initialize(label);

			return instance;
		}
	}
}
