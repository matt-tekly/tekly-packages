using System;
using System.Collections.Generic;
using System.Linq;
using Tekly.DebugKit.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tekly.DebugKit.Widgets
{
	public class Menu : Container
	{
		public readonly string Name;
		private readonly MenuController m_menuController;

		public Menu(string name, MenuController menuController, string classNames = null) : base(menuController.Root, classNames)
		{
			Name = name;
			m_menuController = menuController;
		}
		
		public void Select()
		{
			m_menuController.Select(this);
		}
	}

	public class MenuController : Widget
	{
		public VisualElement Root => m_rootContainer.Root;
		public int MenuCount => m_menus.Count;
		
		private int m_currentMenu;

		private bool Enabled {
			get => m_rootContainer.Enabled;
			set => m_rootContainer.Enabled = value;
		}

		private List<Menu> m_menus = new List<Menu>();
		private DropdownField m_menuDropdown;
		private Container m_rootContainer;

		private Menu m_activeMenu;
		
		private StringPref m_lastSelectedMenu;

		public MenuController(VisualElement root, string pref, Action<Container> preButtons)
		{
			Initialize(root, pref, null, preButtons);
		}

		public MenuController(VisualElement root, string pref, string classNames = null)
		{
			Initialize(root, pref, classNames, null);
		}

		private void Initialize(VisualElement root, string pref, string classNames, Action<Container> preButtons)
		{
			m_rootContainer = new Container(root, classNames);
			m_lastSelectedMenu = new StringPref(pref, "");

			Enable(false);

			m_menuDropdown = new DropdownField();
			m_menuDropdown.AddClassNames("dk-dropdown grow");
			m_menuDropdown.RegisterValueChangedCallback(evt => SetMenu(evt.newValue));

			m_rootContainer
				.Row("grow", row => {
					preButtons?.Invoke(row);
					row.Raw(m_menuDropdown);
				}).HorizontalSpace();
		}

		public Menu Create(string name, string classNames = null)
		{
			if (Exists(name)) {
				Debug.LogError($"Trying to create a menu with an existing name [{name}]");
			}
			
			var menu = new Menu(name, this, classNames);
			m_menus.Add(menu);

			m_menuDropdown.choices.Add(name);
			m_menuDropdown.choices.Sort();

			if (m_activeMenu != null) {
				m_menuDropdown.SetValueWithoutNotify(m_activeMenu.Name);
			}

			menu.Enabled = false;
			
			return menu;
		}

		public void Remove(Menu menu)
		{
			m_menus.Remove(menu);
			
			m_menuDropdown.choices.Remove(menu.Name);
			m_menuDropdown.choices.Sort();
			
			if (m_activeMenu == menu) {
				if (m_menus.Count > 0) {
					m_menuDropdown.value = m_menus[0].Name;	
				} else {
					m_menuDropdown.value = null;
				}
			}

			menu.Enabled = false;
			menu.Detach();
		}

		public void Toggle()
		{
			Enable(!Enabled);
		}

		public void Enable(bool enabled)
		{
			Enabled = enabled;
			
			if (enabled) {
				var lastSelectedMenu = m_lastSelectedMenu.Value;
				if (!string.IsNullOrEmpty(lastSelectedMenu) && Exists(lastSelectedMenu)) {
					m_menuDropdown.value = lastSelectedMenu;
				} else if (m_menus.Count > 0) {
					m_menuDropdown.value = m_menus[0].Name;	
				}
				
				m_menuDropdown.Focus();
			}
		}

		public override void Update()
		{
			if (Enabled && m_activeMenu != null) {
				m_activeMenu.Update();
			}
		}

		private void SetMenu(string menu)
		{
			if (m_activeMenu != null) {
				m_activeMenu.Enabled = false;	
			}

			m_activeMenu = m_menus.FirstOrDefault(x => x.Name == menu);
			
			if (m_activeMenu != null) {
				m_activeMenu.Enabled = true;	
			}
			
			m_lastSelectedMenu.Value = menu;
		}
		
		private bool Exists(string menuName)
		{
			return m_menus.FirstOrDefault(x => x.Name == menuName) != null; 
		}

		public void Select(Menu menu)
		{
			m_menuDropdown.value = menu.Name;
		}
	}
}