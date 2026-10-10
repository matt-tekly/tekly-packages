using System;
using Tekly.Leaf.Elements;
using UnityEngine;

namespace Tekly.DevBoard.Components
{
	/// <summary>
	/// A button that runs an action when clicked. It hooks itself up to the LeafButton on the same GameObject,
	/// so prefabs don't wire the click to it, and a subclass can replace it on a prefab without losing the click.
	/// </summary>
	public class ButtonWidget : Widget
	{
		public string Label {
			get => m_label != null ? m_label.Text : null;
			set {
				if (m_label != null) {
					m_label.Text = value;
				}
			}
		}
		
		/// <summary>
		/// The label widget, for subclasses that measure or style it. Null when the prefab has none.
		/// </summary>
		protected LabelWidget LabelComponent => m_label;

		/// <summary>
		/// The LeafButton this widget runs on. Null when the GameObject has none.
		/// </summary>
		protected LeafButton Button => m_button;

		[SerializeField] private LabelWidget m_label;
		
		private Action m_onActivate;
		private LeafButton m_button;

		protected virtual void Awake()
		{
			if (TryGetComponent(out m_button)) {
				m_button.OnClicked.AddListener(Activate);
			}
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
			
			if (m_label != null) {
				m_label.Text = label;
			}
		}
		
		/// <summary>
		/// Sets what the button does, keeping its label. For buttons that are part of a prefab.
		/// </summary>
		public ButtonWidget WithAction(Action onActivate)
		{
			m_onActivate = onActivate;
			return this;
		}

		public void Activate()
		{
			m_onActivate?.Invoke();
		}
	}
}