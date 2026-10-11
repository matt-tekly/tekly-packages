using System;
using System.Collections.Generic;
using Tekly.DevBoard.Components;
using Tekly.DevBoard.Pages;
using Tekly.Leaf.Elements;
using Tekly.Leaf.Elements.Radios;
using UnityEngine;

namespace Tekly.DevBoard.Panels
{
	/// <summary>
	/// A panel's header: the breadcrumb and collapse toggle, the "..." toggle that swaps them for the settings row
	/// (pop out, dock slot, overlay, show when hidden, close), and the handle that replaces the whole header in
	/// overlay mode. Turns clicks into calls on the panel and keeps the controls showing the panel's state.
	///
	/// Any of the parts can be missing from the prefab.
	/// </summary>
	internal sealed class PanelHeader
	{
		/// <summary>
		/// The objects and controls that make up a header, from the panel prefab.
		/// </summary>
		public struct Parts
		{
			public GameObject Header;
			public BreadcrumbWidget Breadcrumb;
			public LeafToggle CollapseToggle;
			public LeafToggle ActionsToggle;
			public GameObject HeaderNormal;
			public ButtonWidget OverlayHandle;
			public GameObject HeaderSettings;
			public ButtonWidget PopButton;
			public LeafRadioGroup DockGroup;
			public LeafToggle OverlayToggle;
			public LeafToggle ShowWhenHiddenToggle;
			public ButtonWidget CloseButton;
		}

		private readonly DevBoardPanel m_panel;
		private readonly Parts m_parts;

		public PanelHeader(DevBoardPanel panel, Parts parts)
		{
			m_panel = panel;
			m_parts = parts;

			Wire(parts.OverlayHandle, () => m_panel.Defer(() => m_panel.SetOverlay(false)));
			Wire(parts.PopButton, MenuAction(() => m_panel.PopOut()));
			Wire(parts.CloseButton, MenuAction(m_panel.Close));

			if (parts.ActionsToggle != null) {
				parts.ActionsToggle.onValueChanged.AddListener(OnActionsToggleChanged);
			}

			if (parts.CollapseToggle != null) {
				parts.CollapseToggle.onValueChanged.AddListener(OnCollapseToggleChanged);
			}

			if (parts.DockGroup != null) {
				parts.DockGroup.OnChanged.AddListener(OnDockOptionChanged);
			}

			if (parts.ShowWhenHiddenToggle != null) {
				parts.ShowWhenHiddenToggle.onValueChanged.AddListener(OnShowWhenHiddenToggleChanged);
			}

			if (parts.OverlayToggle != null) {
				parts.OverlayToggle.onValueChanged.AddListener(OnOverlayToggleChanged);
			}

			SetSettingsOpen(false);
		}

		/// <summary>
		/// Shows page in the breadcrumb and on the overlay handle. Only text changes here: which header objects are
		/// active is set by SetSettingsOpen and ApplyOverlay, so nothing here fights the toggle.
		/// </summary>
		public void ShowPage(PageNode page)
		{
			if (m_parts.OverlayHandle != null) {
				m_parts.OverlayHandle.Label = page?.Title ?? PageTree.ROOT_TITLE;
			}

			if (m_parts.Breadcrumb != null) {
				var crumbs = new List<Crumb>();

				for (var node = page; node != null; node = node.Parent) {
					crumbs.Insert(0, new Crumb(node.Title, node.Path));
				}

				m_parts.Breadcrumb.Set(crumbs, path => m_panel.Defer(() => m_panel.Open(path)));
			}
		}

		/// <summary>
		/// In overlay mode the header gives way to a small handle, if the prefab has one, and settings close with it.
		/// Without a handle the header stays as it is, settings included.
		/// </summary>
		public void ApplyOverlay(bool overlay)
		{
			if (m_parts.OverlayToggle != null) {
				m_parts.OverlayToggle.SetIsOnWithoutNotify(overlay);
			}

			var showHandle = overlay && m_parts.OverlayHandle != null;

			if (m_parts.Header != null) {
				m_parts.Header.SetActive(!showHandle);
			}

			if (m_parts.OverlayHandle != null) {
				m_parts.OverlayHandle.gameObject.SetActive(showHandle);
			}

			if (showHandle) {
				SetSettingsOpen(false);
			}
		}

		public void ShowCollapsed(bool collapsed)
		{
			if (m_parts.CollapseToggle != null) {
				m_parts.CollapseToggle.SetIsOnWithoutNotify(collapsed);
			}
		}

		public void ShowShowWhenHidden(bool showWhenHidden)
		{
			if (m_parts.ShowWhenHiddenToggle != null) {
				m_parts.ShowWhenHiddenToggle.SetIsOnWithoutNotify(showWhenHidden);
			}
		}

		/// <summary>
		/// Turns on the option for the panel's dock slot, without events.
		/// </summary>
		public void ShowDock()
		{
			var group = m_parts.DockGroup;

			if (group == null || !group.isActiveAndEnabled) {
				return;
			}

			for (var i = 0; ; i++) {
				var option = group.GetOption(i);

				if (option == null) {
					return;
				}

				if (option is Component component && component.TryGetComponent(out DockSlotOption slot) && slot.Slot == m_panel.Dock) {
					group.SetWithoutNotify(option);
					return;
				}
			}
		}

		public void Teardown()
		{
			if (m_parts.ActionsToggle != null) {
				m_parts.ActionsToggle.onValueChanged.RemoveListener(OnActionsToggleChanged);
			}

			if (m_parts.CollapseToggle != null) {
				m_parts.CollapseToggle.onValueChanged.RemoveListener(OnCollapseToggleChanged);
			}

			if (m_parts.DockGroup != null) {
				m_parts.DockGroup.OnChanged.RemoveListener(OnDockOptionChanged);
			}

			if (m_parts.ShowWhenHiddenToggle != null) {
				m_parts.ShowWhenHiddenToggle.onValueChanged.RemoveListener(OnShowWhenHiddenToggleChanged);
			}

			if (m_parts.OverlayToggle != null) {
				m_parts.OverlayToggle.onValueChanged.RemoveListener(OnOverlayToggleChanged);
			}
		}

		/// <summary>
		/// The toggle is the only state for settings: it just swaps the normal and settings header objects. Runs
		/// immediately, since the toggle sits outside both and nothing is rebuilt.
		/// </summary>
		private void OnActionsToggleChanged(bool isOn)
		{
			ApplySettingsOpen(isOn);
		}

		/// <summary>
		/// Deferred like the buttons, since collapsing changes the layout around the toggle that was clicked.
		/// </summary>
		private void OnCollapseToggleChanged(bool isOn)
		{
			m_panel.Defer(() => m_panel.SetCollapsed(isOn));
		}

		/// <summary>
		/// Settings stay open, like the dock options, so it's easy to see the new state.
		/// </summary>
		private void OnShowWhenHiddenToggleChanged(bool isOn)
		{
			m_panel.Defer(() => m_panel.SetShowWhenHidden(isOn));
		}

		private void OnOverlayToggleChanged(bool isOn)
		{
			m_panel.Defer(() => m_panel.SetOverlay(isOn));
		}

		/// <summary>
		/// Deferred, since docking moves the panel, and with it the option that was clicked. Settings stay open
		/// so the panel can be moved again straight away.
		/// </summary>
		private void OnDockOptionChanged(int index)
		{
			if (m_parts.DockGroup.GetOption(index) is Component option && option.TryGetComponent(out DockSlotOption slot)) {
				var dock = slot.Slot;
				m_panel.Defer(() => m_panel.SetDock(dock));
			}
		}

		private void SetSettingsOpen(bool open)
		{
			if (m_parts.ActionsToggle != null) {
				m_parts.ActionsToggle.SetIsOnWithoutNotify(open);
			}

			ApplySettingsOpen(open);
		}

		private void ApplySettingsOpen(bool open)
		{
			if (m_parts.HeaderNormal != null) {
				m_parts.HeaderNormal.SetActive(!open);
			}

			if (m_parts.HeaderSettings != null) {
				m_parts.HeaderSettings.SetActive(open);
			}

			// The options only count once they're active, so sync after showing them
			if (open) {
				ShowDock();
			}
		}

		/// <summary>
		/// A button from the settings row: closes settings, then runs, after the click. Most panel actions rebuild
		/// or hide the very button that was clicked.
		/// </summary>
		private Action MenuAction(Action action)
		{
			return () => m_panel.Defer(() => {
				SetSettingsOpen(false);
				action();
			});
		}

		private static void Wire(ButtonWidget button, Action action)
		{
			if (button != null) {
				button.WithAction(action);
			}
		}
	}
}
