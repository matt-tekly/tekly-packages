using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Tekly.DebugKit.Widgets
{
	public class MenuController
	{
		public readonly VisualElement Root;
		private List<Menu> m_menus = new List<Menu>();

		private int m_currentMenu;
		private bool m_enabled;

		public MenuController(VisualElement root)
		{
			Root = new VisualElement();
			root.Add(Root);
			
			Enable(false);
		}

		public Menu Create(string name, string classNames)
		{
			var menu = new Menu(name, this, classNames);
			m_menus.Add(menu);

			return menu;
		}

		public void Toggle()
		{
			Enable(!m_enabled);
		}
		
		public void Enable(bool enabled)
		{
			m_enabled = enabled;
			Root.style.display = m_enabled ? DisplayStyle.Flex : DisplayStyle.None;

			if (m_menus.Count > 0) {
				m_menus[m_currentMenu].Enabled = enabled;
			}
		}
		
		public void Update()
		{
			if (m_enabled && m_menus.Count > 0) {
				m_menus[m_currentMenu].Update();
			}
		}

		public void LeftMenu()
		{
			m_menus[m_currentMenu].Enabled = false;
			m_currentMenu = WrapIndex(m_menus, m_currentMenu, -1);
			m_menus[m_currentMenu].Enabled = true;
		}
		
		public void RightMenu()
		{
			m_menus[m_currentMenu].Enabled = false;
			m_currentMenu = WrapIndex(m_menus, m_currentMenu, 1);
			m_menus[m_currentMenu].Enabled = true;
		}

		public static int WrapIndex<T>(IList<T> list, int index, int increment)
		{
			if (list.Count == 0) {
				return 0;
			}

			var newIndex = (index + increment) % list.Count;

			if (newIndex < 0) {
				newIndex += list.Count;
			}

			return newIndex;
		}
	}

	public class Menu : Container
	{
		public readonly string Name;

		public Menu(string name, MenuController menuController, string classNames = null) : base(menuController.Root, "dk-root", classNames)
		{
			Name = name;
			
			Row("spaced items-center", row => {
				row.Heading(name);
				row.Row("items-center button-group", left => {
					left.Button("<", "small left", menuController.LeftMenu);
					left.Button(">", "small right", menuController.RightMenu);
				});
			});

			Separator();

			Enabled = false;
		}
	}
}