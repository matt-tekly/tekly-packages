using System;
using Tekly.Leaf.Elements;
using UnityEngine;

namespace Tekly.DevBoard.Components
{
	public class ToggleWidget : Widget
	{
		[SerializeField] private LeafToggle m_toggle;
		[SerializeField] private LabelWidget m_label;

		private Binding<bool> m_binding;
		private BindingWatch<bool> m_watch;

		private void Awake()
		{
			m_toggle.onValueChanged.AddListener(OnValueChanged);
		}

		private void OnDestroy()
		{
			m_toggle.onValueChanged.RemoveListener(OnValueChanged);
		}

		private void OnValueChanged(bool isOn)
		{
			// Only user clicks get here: refreshes use SetIsOnWithoutNotify
			m_binding?.Write(isOn);

			// The setter may have rejected the change, so the next tick puts the toggle back to the real value
			m_watch.Invalidate();
		}

		public void Initialize(string label, Binding<bool> binding)
		{
			m_label.Text = label;
			m_binding = binding;
			m_watch.Invalidate();

			try {
				Tick();
			} catch (Exception) {
				// Don't break the code building the board. Tick retries, and logs the error if it keeps throwing
			}
		}

		protected override void Tick()
		{
			// Without notify, so a change made by the game isn't pushed back through the setter
			if (m_binding != null && m_watch.Poll(m_binding, out var value)) {
				m_toggle.SetIsOnWithoutNotify(value);
			}
		}
	}

	public static class ToggleWidgetFactory
	{
		public static ToggleWidget Toggle(this ContainerWidget parent, string label, Func<bool> getValue, Action<bool> setValue,
			WidgetPrefab<ToggleWidget> prefab = default)
		{
			return parent.Toggle(label, new Binding<bool>(getValue, setValue), prefab);
		}

		public static ToggleWidget Toggle(this ContainerWidget parent, string label, Binding<bool> binding,
			WidgetPrefab<ToggleWidget> prefab = default)
		{
			var instance = parent.Create(prefab.Or(WidgetPrefabs.Toggle));
			instance.Initialize(label, binding);

			return instance;
		}
	}
}
