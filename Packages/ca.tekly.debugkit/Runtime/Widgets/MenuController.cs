using System.Collections.Generic;
using Tekly.DebugKit.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tekly.DebugKit.Widgets
{
	public class MenuController
	{
		public readonly VisualElement Root;

		private int m_currentMenu;
		private bool m_enabled;

		private readonly List<string> m_menus = new List<string>();
		private readonly List<Container> m_containers = new List<Container>();
		private readonly DropdownField m_menuDropdown;
		private readonly Container m_rootContainer;

		public MenuController(VisualElement root)
		{
			Root = root;

			m_rootContainer = new Container(root, "dk-root");
			Enable(false);

			m_menuDropdown = new DropdownField();
			m_menuDropdown.AddClassNames("dk-dropdown");
			m_menuDropdown.RegisterValueChangedCallback(evt => SetMenu(m_menus.IndexOf(evt.newValue)));
			m_rootContainer
				.Raw(m_menuDropdown)
				.Separator();
		}

		public Container Create(string name, string classNames)
		{
			var childContainer = m_rootContainer.ChildContainer(classNames);
			m_containers.Add(childContainer);

			m_menus.Add(name);
			m_menuDropdown.choices.Add(name);

			if (m_menuDropdown.index == -1) {
				m_menuDropdown.index = 0;
			} else {
				childContainer.Enabled = false;
			}

			return childContainer;
		}

		public void Toggle()
		{
			Enable(!m_enabled);
		}

		public void Enable(bool enabled)
		{
			m_enabled = enabled;
			Root.style.display = m_enabled ? DisplayStyle.Flex : DisplayStyle.None;

			if (m_containers.Count > 0) {
				m_containers[m_currentMenu].Enabled = enabled;
			}
		}

		public void Update()
		{
			if (m_enabled && m_containers.Count > 0) {
				m_containers[m_currentMenu].Update();
			}
		}

		public void SetMenu(int index)
		{
			m_containers[m_currentMenu].Enabled = false;
			m_currentMenu = index;
			m_containers[m_currentMenu].Enabled = true;
		}
	}
}