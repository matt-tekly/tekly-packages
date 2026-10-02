## Features

### Navigation Scope
- Keeps the navigation within a part of the hierarchy
- TODO:
	- Navigation Scopes could navigate to other scopes when navigating in a direction that doesn't find a next Selectable

### Tab Navigation
- The UI input modules never send Tab, so the scope that owns the current selection polls Tab/Shift+Tab (`LeafTabInput`)
- Arrow keys stay spatial (`FindNext`). Tab follows a fixed order instead (`FindNextInTabOrder`) and wraps at the ends of the scope
- Tab order is hierarchy order by default. Elements need a `LeafNavigationElement` to be in it, and `IsTabStop` can leave one out
- `LeafNavigationGroup` makes its elements one block of the order: Tab goes through the whole group before moving on, entering at its first element (last with Shift+Tab). Its `Order` sorts the block by hierarchy, `LeftToRight` or `TopToBottom`, with a nested group placed by its own rect. Put one on the scope to set the order of the top level
- The selected object gets `ILeafTabHandler.OnTab` first; calling `Use()` on the event data stops the scope's navigation
- `LeafInputField` hands Tab back to the scope while editing, so tabbing out ends the edit like clicking away. `TabNavigates` off lets a multi-line field type tabs instead
- If the selection was cleared (e.g. by clicking the background), Tab reselects in the scope that was used last

### Element State
- `LeafElementMode` is only the interaction: `Normal`, `Highlighted`, `Pressed`
- `LeafElementState` combines the mode with `LeafElementFlags` (`Selected`, `Disabled`, `On`), so a selected element can still be highlighted or pressed (Unity's `SelectionState` can't express that)
- Check flags with `IsSelected`/`IsDisabled`/`IsOn` or `HasAll`/`HasAny` rather than `Enum.HasFlag`, which can allocate on Mono
- `LeafAnimatorColors` gives each Graphic one `LeafColorTarget`: base colors per mode, plus layers that apply when their flags match, in order (e.g. Selected, On, then Disabled last). A layer can override or multiply the color so far
- Fades are run by the animator with unscaled time from a tracked color, so they keep going while a Graphic is hidden and are repaired when the animator is enabled, when an Active Target shows a Graphic, and after a scene save or undo in the editor. Nothing runs per frame while idle apart from a cheap check in LateUpdate. Set the Selectable's Transition to None, and don't make a Toggle's own Graphic a color target
- Showing/hiding GameObjects by flags is a separate list (`LeafActiveTarget`). A face that fades in can often stay active and use alpha in its layers instead
- Disabled elements report `Normal` mode but keep `IsSelected`
