using UnityEngine;

namespace Tekly.DevBoard.Components
{
	/// <summary>
	/// A container that opens and closes. With a state key, whether it's open is saved in DevBoard.State, between
	/// sessions too, so a foldout reopens the way the user left it; the renderer gives a foldout node's widget
	/// its key.
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
		}

		/// <summary>
		/// Saves whether the foldout is open in DevBoard.State under key, and opens or closes it to match what's
		/// saved. Without one the foldout forgets its state when it's destroyed.
		/// </summary>
		public FoldoutWidget WithStateKey(string key)
		{
			m_stateKey = key;

			if (TryGetSavedExpanded(out var expanded)) {
				ShowExpanded(expanded);
			}

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

			// Kept between sessions, so a foldout opened once stays open the next time the game runs
			DevBoard.Instance?.State.Set(m_stateKey, expanded, persistent: true);
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
