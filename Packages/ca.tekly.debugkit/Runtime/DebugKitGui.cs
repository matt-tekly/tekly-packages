using Tekly.Common.Observables;
using Tekly.DebugKit.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Tekly.DebugKit
{
	public class DebugKitGui : MonoBehaviour
	{
		public VisualElement Root => m_document.rootVisualElement;

		public IObservableValue<bool> Focused => _focused;
		private readonly ObservableValue<bool> _focused = new ObservableValue<bool>(true);

		public float Scale {
			get => m_document.panelSettings.scale;
			set => m_document.panelSettings.scale = value;
		}

		private UIDocument m_document;
		private DebugKit m_debugKit;

		public void Initialize(DebugKit debugKit)
		{
			m_debugKit = debugKit;
			m_document = gameObject.AddComponent<UIDocument>();
			m_document.panelSettings = Instantiate(debugKit.Settings.PanelSettings);

			if (debugKit.Settings.StyleSheets != null) {
				foreach (var sheet in debugKit.Settings.StyleSheets) {
					m_document.rootVisualElement.styleSheets.Add(sheet);
				}
			}

			if (debugKit.Settings.RootClassNames != null) {
				foreach (var classNames in debugKit.Settings.RootClassNames) {
					m_document.rootVisualElement.AddClassNames(classNames);
				}
			}
		}

		private void Update()
		{
			m_debugKit.Update();
			
			var isElementFocused = m_document.rootVisualElement.panel.focusController.focusedElement != null;
			var isPanelFocused = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == m_document.gameObject;
			_focused.Value = isElementFocused || isPanelFocused;
		}
	}
}