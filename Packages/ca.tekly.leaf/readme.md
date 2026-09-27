## Features

### Navigation Scope
- Keeps the navigation within a part of the hierarchy
- TODO:
	- Navigation Scopes could navigate to other scopes when navigating in a direction that doesn't find a next Selectable

### Element State
- `LeafElementMode` is only the interaction: `Normal`, `Highlighted`, `Pressed`
- `LeafElementState` combines the mode with `LeafElementFlags` (`Selected`, `Disabled`, `On`), so a selected element can still be highlighted or pressed (Unity's `SelectionState` can't express that)
- Check flags with `IsSelected`/`IsDisabled`/`IsOn` or `HasAll`/`HasAny` rather than `Enum.HasFlag`, which can allocate on Mono
- `LeafAnimatorColors` gives each Graphic one `LeafColorTarget`: base colors per mode, plus layers that apply when their flags match, in order (e.g. Selected, On, then Disabled last). A layer can override or multiply the color so far
- Fades are run by the animator with unscaled time from a tracked color, so they keep going while a Graphic is hidden and are repaired when the animator is enabled, when an Active Target shows a Graphic, and after a scene save or undo in the editor. Nothing runs per frame while idle apart from a cheap check in LateUpdate. Set the Selectable's Transition to None, and don't make a Toggle's own Graphic a color target
- Showing/hiding GameObjects by flags is a separate list (`LeafActiveTarget`). A face that fades in can often stay active and use alpha in its layers instead
- Disabled elements report `Normal` mode but keep `IsSelected`
