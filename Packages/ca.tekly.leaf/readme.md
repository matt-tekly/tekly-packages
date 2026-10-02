## Features

### Navigation Scope
- Keeps the navigation within a part of the hierarchy
- Every active, interactable Selectable below the scope takes part, with no extra component. Selectables inside a nested scope belong to that scope, and a Navigation mode of None leaves one out (e.g. a ScrollRect's scrollbars)
- Leaf elements call `LeafNavigationScope.TryNavigateFrom` in `OnMove`, which uses the nearest scope above them
- Arrow keys move spatially (`FindNext`). The Selectables are collected from the hierarchy on each key press rather than registered
- When enabled, the scope selects `m_firstSelection`, or the first Selectable in tab order. If there's nothing to select yet, it keeps trying until something is selected, since widgets are often added after the scope
- TODO:
	- Navigation Scopes could navigate to other scopes when navigating in a direction that doesn't find a next Selectable

### Tab Navigation
- The UI input modules never send Tab, so the scope that owns the current selection polls Tab/Shift+Tab (`LeafTabInput`)
- Tab follows hierarchy order, depth first (`FindNextInTabOrder`), and wraps at the ends of the scope. If the order looks wrong, reorder the children to match the screen
- The selected object gets `ILeafTabHandler.OnTab` first; calling `Use()` on the event data stops the scope's navigation
- `LeafInputField` hands Tab back to the scope while editing, so tabbing out ends the edit like clicking away. `TabNavigates` off lets a multi-line field type tabs instead
- If the selection was cleared (e.g. by clicking the background), Tab reselects in the scope that was used last
- `LeafInputField.EnterMovesNext` makes Enter end the edit and select the next element in tab order (e.g. username to password). Leave it off on a form's last field and wire `onSubmit` to the form's action. The move happens a frame later so the same Enter can't also press the next element
- `LeafNavigationScope.TrySelectNextFrom` selects the next element in tab order from code

### Element State
- `LeafElementMode` is only the interaction: `Normal`, `Highlighted`, `Pressed`
- `LeafElementState` combines the mode with `LeafElementFlags` (`Selected`, `Disabled`, `On`), so a selected element can still be highlighted or pressed (Unity's `SelectionState` can't express that)
- Check flags with `IsSelected`/`IsDisabled`/`IsOn` or `HasAll`/`HasAny` rather than `Enum.HasFlag`, which can allocate on Mono
- `LeafAnimatorColors` gives each Graphic one `LeafColorTarget`: base colors per mode, plus layers that apply when their flags match, in order (e.g. Selected, On, then Disabled last). A layer can override or multiply the color so far
- Fades are run by the animator with unscaled time from a tracked color, so they keep going while a Graphic is hidden and are repaired when the animator is enabled, when an Active Target shows a Graphic, and after a scene save or undo in the editor. Nothing runs per frame while idle apart from a cheap check in LateUpdate. Set the Selectable's Transition to None, and don't make a Toggle's own Graphic a color target
- Showing/hiding GameObjects by flags is a separate list (`LeafActiveTarget`). A face that fades in can often stay active and use alpha in its layers instead
- Disabled elements report `Normal` mode but keep `IsSelected`
