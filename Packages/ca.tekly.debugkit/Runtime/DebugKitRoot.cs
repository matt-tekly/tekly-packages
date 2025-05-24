using Tekly.Common.Observables;
using Tekly.DebugKit.Widgets;
using UnityEngine.UIElements;

namespace Tekly.DebugKit
{
	/// <summary>
	/// Represents the Root UI Menu
	/// </summary>
	public class DebugKitRoot
	{
		public IObservableValue<bool> EnabledObservable => m_enabled;
		
		private readonly ObservableValue<bool> m_enabled = new ObservableValue<bool>();
		private readonly Container m_root;
		
		private MenuController m_menuController;
		private Container m_preferencesContainer;

		private bool m_showingPreferences;

		public bool Enabled {
			get => m_root.Enabled;
			set {
				m_enabled.Value = value;
				
				m_root.Enabled = value;
				m_menuController.Enable(!m_showingPreferences && value);
				m_preferencesContainer.Enabled = m_showingPreferences;
			}
		}

		public bool ShowPreferences {
			get => m_showingPreferences;
			set {
				m_showingPreferences = value;
				
				m_menuController.Enable(!m_showingPreferences);
				m_preferencesContainer.Enabled = m_showingPreferences;
			}
		}

		public DebugKitRoot(VisualElement root)
		{
			m_root = new Container(root, "dk-root");
			m_root.Root.name = "DebugKitRoot";
			m_root.Root.focusable = true;
			
			m_menuController = new MenuController(m_root.Root, "debugkit.menu.selected", (buttons) => {
				buttons.ButtonOptions(() => ShowPreferences = !ShowPreferences);
				buttons.VerticalSpace();
			});
			
			m_root.Column(column => {
				m_preferencesContainer = column;
			});
			
			var preferences = DebugKit.Instance.Preferences;
			m_preferencesContainer
				.Row(row => {
					row.ButtonOptions(() => ShowPreferences = !ShowPreferences);
					row.VerticalSpace();
					row.Heading("Preferences");
				})
#if UNITY_EDITOR
				.Checkbox("Scale In Editor", preferences.ScaleInEditor)
#endif
				.Checkbox("Scale Override", preferences.ScaleOverrideEnabled)
				.CardColumn(subColumn => {
					subColumn.FloatField("Scale", () => preferences.ScaleOverride.Value,
							v => preferences.ScaleOverride.Value = v)
						.SliderFloat(null, 0.25f, 3f, () => preferences.ScaleOverride.Value,
							v => preferences.ScaleOverride.Value = v);
				});
		}

		public Menu Menu(string name, string classNames = null)
		{
			return m_menuController.Create(name, classNames);
		}

		public void RemoveMenu(Menu menu)
		{
			m_menuController.Remove(menu);
		}

		public void Toggle()
		{
			Enabled = !Enabled;
		}

		public void Update()
		{
			m_root.Update();
			m_menuController.Update();
		}

		public void Focus()
		{
			m_root.Root.panel.visualTree.Q("DebugKitRoot").Focus();
		}
	}
}