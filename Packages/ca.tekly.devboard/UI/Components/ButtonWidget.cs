using System;
using Tekly.Leaf.Elements;
using Tekly.Leaf.Elements.Animators;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.DevBoard.Components
{
	/// <summary>
	/// A button that runs an action when clicked. It hooks itself up to the LeafButton on the same GameObject,
	/// so prefabs don't wire the click to it, and a subclass can replace it on a prefab without losing the click.
	///
	/// What it shows follows what it's given: the label hides while it's empty, and the icon shows once one is
	/// set. WithTone recolors it through the LeafAnimatorColors on the same GameObject.
	/// </summary>
	public class ButtonWidget : Widget
	{
		public string Label {
			get => m_label != null ? m_label.Text : null;
			set => SetLabel(value);
		}

		/// <summary>
		/// The label widget, for subclasses that measure or style it. Null when the prefab has none.
		/// </summary>
		protected LabelWidget LabelComponent => m_label;

		/// <summary>
		/// The LeafButton this widget runs on. Null when the GameObject has none.
		/// </summary>
		protected LeafButton Button => m_button;

		/// <summary>
		/// Whether the button takes clicks, as of the last time WithInteractable's check was polled.
		/// </summary>
		public bool IsInteractable => m_isInteractable;

		[SerializeField] private LabelWidget m_label;

		[Tooltip("Shows the icon set with WithIcon. Optional")]
		[SerializeField] private Image m_icon;

		[Tooltip("Recolored by WithTone, and hidden by a plain button until it's hovered. Optional")]
		[SerializeField] private Graphic m_background;

		private Action m_onActivate;
		private LeafButton m_button;
		private Func<bool> m_interactable;
		private bool m_isInteractable = true;
		private Sprite m_prefabIcon;
		private bool m_hasPrefabIcon;
		private bool m_prefabIconShown;
		private bool m_labelHidden;

		protected virtual void Awake()
		{
			if (TryGetComponent(out m_button)) {
				m_button.OnClicked.AddListener(Activate);
			}

			// Initialize can run before Awake when the button is created inside an inactive container
			ApplyInteractable();
		}

		protected virtual void OnDestroy()
		{
			if (m_button != null) {
				m_button.OnClicked.RemoveListener(Activate);
			}
		}

		public void Initialize(Action onActivate, string label)
		{
			m_onActivate = onActivate;
			SetLabel(label);
		}

		/// <summary>
		/// Sets what the button does, keeping its label. For buttons that are part of a prefab.
		/// </summary>
		public ButtonWidget WithAction(Action onActivate)
		{
			m_onActivate = onActivate;
			return this;
		}

		/// <summary>
		/// Greys the button out and ignores clicks while interactable returns false. Polled as the panel refreshes.
		/// Null for always interactable.
		/// </summary>
		public ButtonWidget WithInteractable(Func<bool> interactable)
		{
			m_interactable = interactable;
			m_isInteractable = true;

			try {
				Tick();
			} catch (Exception) {
				// Don't break the code building the board. Tick retries, and logs the error if it keeps throwing
				m_isInteractable = false;
			}

			ApplyInteractable();
			return this;
		}

		/// <summary>
		/// Hides the label even when it has text, e.g. for a button that shows only its icon.
		/// </summary>
		public ButtonWidget WithLabelHidden(bool hidden)
		{
			m_labelHidden = hidden;
			SetLabel(Label);
			return this;
		}

		/// <summary>
		/// Colors the button with a tone's colors. A solid button takes them on its background, label and icon; a
		/// plain one has no background until hovered and carries the tone in its label and icon. Null, or the
		/// neutral tone on a solid button, keeps the prefab's own colors. The prefab's Disabled colors still apply.
		/// </summary>
		public ButtonWidget WithTone(ButtonToneColors tone, bool plain)
		{
			var animator = GetComponent<LeafAnimatorColors>();
			var text = LabelComponent != null ? LabelComponent.TextComponent : null;
			var keepsPrefabColors = tone == null || (!plain && tone.Tone == Nodes.ButtonTone.Neutral);

			if (keepsPrefabColors) {
				if (animator != null) {
					animator.ClearColorOverride(m_background);
					animator.ClearColorOverride(text);
					animator.ClearColorOverride(m_icon);
				}

				return this;
			}

			var ink = plain ? tone.Ink : tone.Text;

			if (animator != null) {
				animator.OverrideColors(m_background, plain ? PlainBackground(tone.Background) : tone.Background);
				animator.OverrideColors(text, ink);
				animator.OverrideColors(m_icon, ink);
			}

			return this;
		}

		/// <summary>
		/// Shows sprite in the prefab's icon, if it has one, e.g. before a text button's label, where the icon is
		/// hidden until one is set. Null puts back the prefab's own icon, and hides it again if the prefab does.
		/// </summary>
		public ButtonWidget WithIcon(Sprite sprite)
		{
			if (m_icon == null) {
				return this;
			}

			if (!m_hasPrefabIcon) {
				m_prefabIcon = m_icon.sprite;
				m_prefabIconShown = m_icon.gameObject.activeSelf;
				m_hasPrefabIcon = true;
			}

			m_icon.sprite = sprite != null ? sprite : m_prefabIcon;
			m_icon.gameObject.SetActive(sprite != null || m_prefabIconShown);
			return this;
		}

		public void Activate()
		{
			if (m_isInteractable) {
				m_onActivate?.Invoke();
			}
		}

		protected override void Tick()
		{
			if (m_interactable == null) {
				return;
			}

			var interactable = m_interactable();

			if (interactable != m_isInteractable) {
				m_isInteractable = interactable;
				ApplyInteractable();
			}
		}

		protected override void OnFaulted(Exception exception)
		{
			// A check that throws counts as not interactable, as it does for the palette and NodeAccess
			m_isInteractable = false;
			ApplyInteractable();
		}

		private void ApplyInteractable()
		{
			// The prefab's color animator shows the disabled look, label and icon included, from the selectable's state
			if (m_button != null && m_button.interactable != m_isInteractable) {
				m_button.interactable = m_isInteractable;
			}
		}

		private void SetLabel(string label)
		{
			if (m_label == null) {
				return;
			}

			m_label.Text = label;
			m_label.gameObject.SetActive(!m_labelHidden && !string.IsNullOrEmpty(label));
		}

		/// <summary>
		/// Clear until the pointer is over the button, then a faint wash of the tone's background.
		/// </summary>
		private static LeafColorBlock PlainBackground(LeafColorBlock background)
		{
			return new LeafColorBlock(Fade(background.Normal, 0f), Fade(background.Highlighted, 0.35f),
				Fade(background.Pressed, 0.55f));
		}

		private static Color Fade(Color color, float alpha)
		{
			color.a = alpha;
			return color;
		}
	}

	public static class ButtonWidgetFactory
	{
		public static ButtonWidget Button(this ContainerWidget parent, string label, Action action, WidgetPrefab<ButtonWidget> prefab = default)
		{
			var instance = parent.Create(prefab.Or(WidgetPrefabs.Button));
			instance.Initialize(action, label);

			return instance;
		}
	}
}
