## Features

### Navigation Scope
- Keeps the navigation within a part of the hierarchy
- Every active, interactable Selectable below the scope takes part, with no extra component. Selectables inside a nested scope belong to that scope, and a Navigation mode of None leaves one out (e.g. a ScrollRect's scrollbars)
- Leaf elements call `LeafNavigationScope.TryNavigateFrom` in `OnMove`, which uses the nearest scope above them
- Arrow keys move spatially (`FindNext`), based on Android's FocusFinder. Whole rects are compared in the scope's space: a candidate has to be past the current element in that direction, ones that line up with it beat ones that don't (sideways, staying in the row always wins), and the rest are scored by edge gap weighted well above sideways offset
- With wrapping on, a direction with nothing left wraps to the far side, e.g. Right at the end of a row goes to the start of that row
- The Selectables are collected from the hierarchy on each key press rather than registered
- When enabled, the scope selects `m_firstSelection`, or the first Selectable in tab order. If there's nothing to select yet, it keeps trying until something is selected, since widgets are often added after the scope
- TODO:
	- Navigation Scopes could navigate to other scopes when navigating in a direction that doesn't find a next Selectable

### Selection
- `LeafCore.Instance.Selection.Current` is the EventSystem's selected GameObject as an observable. Unity doesn't tell parents when a child is selected, so subscribe here instead of polling `currentSelectedGameObject`
- Refreshed every LateUpdate (via `LifeCycle.LateUpdate`), after the EventSystem has processed input. Call `Refresh()` after changing the selection to notify right away

### Scroll To Selection
- `LeafScrollToSelection` on a ScrollRect scrolls the least distance that shows a newly selected object inside its content, plus `m_padding`. Nothing moves if it's already fully visible
- Forces a canvas update before measuring, so a widget added and selected in the same frame is measured after layout
- Clamps to the ends of the content afterwards, so Elastic scroll views don't spring back

### Tab Navigation
- The UI input modules never send Tab, so `LeafCore` reads Tab/Shift+Tab every LateUpdate (`LeafTabInput`) and passes it to the nearest scope above the selection
- Tab follows hierarchy order, depth first (`FindNextInTabOrder`), and wraps at the ends of the scope. If the order looks wrong, reorder the children to match the screen
- The selected object gets `ILeafTabHandler.OnTab` first; calling `Use()` on the event data stops the scope's navigation
- `LeafInputField` hands Tab back to the scope while editing, so tabbing out ends the edit like clicking away. `TabNavigates` off lets a multi-line field type tabs instead
- If the selection was cleared (e.g. by clicking the background), Tab reselects in the scope that was used last
- `LeafInputField.EnterMovesNext` makes Enter end the edit and select the next element in tab order (e.g. username to password). Leave it off on a form's last field and wire `onSubmit` to the form's action. The move happens a frame later so the same Enter can't also press the next element
- `LeafNavigationScope.TrySelectNextFrom` selects the next element in tab order from code

### Radio Groups
- `LeafRadioGroup` keeps one of the options below it on. It's never selectable itself, and it owns which option is on: options only show it, so nothing can get out of sync
- Two kinds of option, mixed as needed:
	- `LeafRadioOption` takes keyboard focus like any Selectable, for choices in a form
	- `LeafRadioOptionUnselectable` never takes focus (built on `LeafButtonUnselectable`), for tabs that switch pages while focus stays in the content. Reach them from the keyboard with `SelectNext`, e.g. from shoulder buttons. Needs the Input System UI module and no Selectable above it
- Keyboard, for `LeafRadioOption`s: arrows along the group's layout axis move between its options (wrapping if `m_wrap`), other arrows and Tab leave it, and arrowing or tabbing into the group lands on the current option
- `SelectionFollowsFocus`: arrow moves inside the group also turn the option on. Implies `SingleTabStop`, where Tab only lands on the current option, so tabbing past can't change the choice
- `Select`/`SelectIndex` fire `OnChanged` (index, -1 for none) and the options' `OnValueChanged`; `SetWithoutNotify`/`SetIndexWithoutNotify` don't, for pushing in model values
- `AllowNone` lets clicking the current option turn it off. Without it, the first option to be enabled becomes current (without events), and disabling the current option moves to the next available one, unless the whole group is being hidden
- `Interactable` on the group disables all of its options
- The group's own `LeafAnimator` gets the Selected flag while one of its options has focus (like CSS `:focus-within`), and Disabled when it isn't interactable, e.g. to show an outline around the whole group
- An option without a group toggles on and off by itself
- `LeafRadioOptionBinder` binds a bool model to either kind of option

### Element State
- `LeafElementMode` is only the interaction: `Normal`, `Highlighted`, `Pressed`
- `LeafElementState` combines the mode with `LeafElementFlags` (`Selected`, `Disabled`, `On`), so a selected element can still be highlighted or pressed (Unity's `SelectionState` can't express that)
- Check flags with `IsSelected`/`IsDisabled`/`IsOn` or `HasAll`/`HasAny` rather than `Enum.HasFlag`, which can allocate on Mono
- `LeafAnimatorColors` gives each Graphic one `LeafColorTarget`: base colors per mode, plus layers that apply when their flags match, in order (e.g. Selected, On, then Disabled last). A layer can override or multiply the color so far
- Fades are run by the animator with unscaled time from a tracked color, so they keep going while a Graphic is hidden and are repaired when the animator is enabled, when an Active Target shows a Graphic, and after a scene save or undo in the editor. Nothing runs per frame while idle apart from a cheap check in LateUpdate. Set the Selectable's Transition to None, and don't make a Toggle's own Graphic a color target
- Showing/hiding GameObjects by flags is a separate list (`LeafActiveTarget`). A face that fades in can often stay active and use alpha in its layers instead
- Disabled elements report `Normal` mode but keep `IsSelected`
