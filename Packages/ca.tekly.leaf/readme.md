## Features

### Navigation Scope
- Keeps the navigation within a part of the hierarchy
- Every active, interactable Selectable below the scope takes part, with no extra component. A Navigation mode of None leaves one out (e.g. a ScrollRect's scrollbars)
- `ContainNavigation` (on by default) makes the scope a boundary: arrows and Tab stay inside it, and scopes above don't see its Selectables
- With it off, the scope only decides where focus lands. Navigation is handled by the nearest containing scope above, which moves in and out of it freely, e.g. tab panels or side by side columns inside a board. If no containing scope is above, it navigates itself
- `Entry` decides where focus lands when the scope is enabled, or when navigation enters it from outside:
	- `First`: `m_firstSelection`, or the first Selectable in tab order
	- `Remembered`: the Selectable last focused in the scope if it's still there, otherwise like First
	- `Nearest`: arrows land wherever the spatial search picks, e.g. the same row of a column beside this one. When enabled, like First
- Tab only follows Entry into a scope with a remembered selection; otherwise it enters at the first Selectable in order (the last with Shift+Tab)
- Leaf elements call `LeafNavigationScope.TryNavigateFrom` in `OnMove`, which uses `FindNavigationScope`: the nearest containing scope above them
- Arrow keys move spatially (`FindNext`), based on Android's FocusFinder. Whole rects are compared in the scope's space: a candidate has to be past the current element in that direction, ones that line up with it beat ones that don't, and the rest are scored by edge gap weighted well above sideways offset
- `SidewaysStaysInRow` (on by default): Left and Right only move to Selectables in the same row (overlapping vertically). Off, they can move diagonally to the nearest Selectable when the row has nothing that way, e.g. for staggered grids
- With wrapping on, a direction with nothing left wraps to the far side, e.g. Right at the end of a row goes to the start of that row. Sideways wrapping always stays in the row
- The Selectables are collected from the hierarchy on each key press rather than registered
- A containing scope selects its entry whenever it's enabled. A non-containing one only does when nothing visible is focused in the scope that navigates it, so switching tab panels moves focus into the new panel, but a column shown along with its board doesn't take focus from it
- If there's nothing to select yet, it keeps trying until something is selected, since widgets are often added after the scope. A selection on a hidden object (e.g. in the tab panel just switched away from) counts as none
- `SelectOnEnable` off stops the scope selecting anything when it's enabled, for when something else decides, e.g. `LeafTabPanels`. `TakeFocus()` selects the entry now, or keeps trying until something selectable shows up, stopping if something visible gets focus first

### Selection
- `LeafCore.Instance.Selection.Current` is the EventSystem's selected GameObject as an observable. Unity doesn't tell parents when a child is selected, so subscribe here instead of polling `currentSelectedGameObject`
- Refreshed every LateUpdate (via `LifeCycle.LateUpdate`), after the EventSystem has processed input. Call `Refresh()` after changing the selection to notify right away

### Scroll To Selection
- `LeafScrollToSelection` on a ScrollRect scrolls the least distance that shows a newly selected object inside its content, plus `m_padding`. Nothing moves if it's already fully visible
- Forces a canvas update before measuring, so a widget added and selected in the same frame is measured after layout
- Never scrolls past the ends of the content, so Elastic scroll views don't spring back
- `m_duration` glides there (SmoothDamp, unscaled time); 0 jumps. A new selection while moving is measured from where the content is heading, so quick or held arrow keys stay one smooth motion. Dragging or the scroll wheel cancels it, and the goal is kept in range if the content resizes while moving
- Only updates while moving: it subscribes to `LifeCycle.LateUpdate` for the duration of a scroll
- The first selection after the component is enabled (e.g. a scope's entry when a screen opens) jumps instead of scrolling in
- `ScrollIntoView(target, instant)` scrolls from code; `StopAnimating()` stops where it is

### Tab Navigation
- The UI input modules never send Tab, so `LeafCore` reads Tab/Shift+Tab every LateUpdate (`LeafTabInput`) and passes it to the scope that navigates the selection
- Tab follows hierarchy order, depth first (`FindNextInTabOrder`), and wraps at the ends of the scope. If the order looks wrong, reorder the children to match the screen
- The selected object gets `ILeafTabHandler.OnTab` first; calling `Use()` on the event data stops the scope's navigation
- `LeafInputField` hands Tab back to the scope while editing, so tabbing out ends the edit like clicking away. `TabNavigates` off lets a multi-line field type tabs instead
- If the selection was cleared (e.g. by clicking the background) or is on a hidden object, Tab reselects where it was in the scope that was used last
- `LeafInputField.EnterMovesNext` makes Enter end the edit and select the next element in tab order (e.g. username to password). Leave it off on a form's last field and wire `onSubmit` to the form's action. The move happens a frame later so the same Enter can't also press the next element
- `LeafNavigationScope.TrySelectNextFrom` selects the next element in tab order from code

### Pressing From Code
- `LeafCore.Instance.Click(target)` clicks a GameObject through the EventSystem as a left click would, e.g. for a tutorial step. Works for any element that handles clicks, Unity's own included. It sends a click rather than Submit because unselectable elements don't handle Submit
- Returns false when there's no EventSystem, or the target is inactive or doesn't handle clicks, but true for a non-interactable element (its handler runs and does nothing)
- To make sure a radio option ends up on, set `IsOn` instead, since a click on the current option turns it off when its group allows none

### Radio Groups
- `LeafRadioGroup` keeps one of the options below it on. It's never selectable itself, and it owns which option is on: options only show it, so nothing can get out of sync
- Two kinds of option, mixed as needed:
	- `LeafRadioOption` takes keyboard focus like any Selectable, for choices in a form
	- `LeafRadioOptionUnselectable` never takes focus (built on `LeafButtonUnselectable`), for tabs that switch pages while focus stays in the content. Reach them from the keyboard with `SelectNext`, e.g. from shoulder buttons. Needs the Input System UI module and no Selectable above it
- Keyboard, for `LeafRadioOption`s: arrows along the group's layout axis move between its options (wrapping if `m_wrap`), other arrows and Tab leave it, and arrowing or tabbing into the group lands on the current option
- `SelectionFollowsFocus`: arrow moves inside the group also turn the option on. Implies `SingleTabStop`, where Tab only lands on the current option, so tabbing past can't change the choice
- `Select`/`SelectIndex` fire `OnChanged` (index, -1 for none) and the options' `OnValueChanged`; `SetWithoutNotify`/`SetIndexWithoutNotify` don't, for pushing in model values
- `CurrentChanged` (C# event) is raised on every change of `Current`, including the first option becoming current and `SetWithoutNotify`, for anything that has to mirror the group, e.g. `LeafTabPanels`
- Which option starts on is authored on the group (`InitialOption`), never on the options, so two options can't both claim to be on. Pick it from the group's Initial Option list, or tick On in an option's inspector (which sets the group's Initial Option). Options run in edit mode, so the change shows straight away
- Without an Initial Option (or if it's hidden or disabled), the first option in hierarchy order starts on, or none with `AllowNone`. This is decided once for all options, counting ones that haven't been enabled yet, since Unity doesn't enable siblings in hierarchy order. No events fire, as binders read the value
- `ResetToInitial()` goes back to the initial option without events. `InitialOption` doesn't follow `Current`; in play mode the group's inspector also shows Current, and changing it selects through the group
- `AllowNone` lets clicking the current option turn it off. Without it, disabling the current option moves to the next available one, unless the whole group is being hidden
- `Interactable` on the group disables all of its options
- The group's own `LeafAnimator` gets the Selected flag while one of its options has focus (like CSS `:focus-within`), and Disabled when it isn't interactable, e.g. to show an outline around the whole group
- An option without a group toggles on and off by itself
- `LeafRadioOptionBinder` binds a bool model to either kind of option

### Tab Panels
- Add `LeafTab` beside an option (usually a `LeafRadioOptionUnselectable`) and set its Panel. Several tabs can share a panel, and an option without `LeafTab` just doesn't drive one
- `LeafTabPanels` references a `LeafRadioGroup` (filled from its own GameObject or parents when empty), shows the current option's panel and hides the panels of the group's other tabs, hidden tabs included
- Follows `CurrentChanged`, so the starting tab and model-driven changes (`SetWithoutNotify`) show the right panel too. With `AllowNone` and nothing on, every panel is hidden. It can sit anywhere, e.g. on the panels' container, but only follows the group while it's enabled, so keep it somewhere that stays active while the tabs can change
- Kept separate from the options' animators: styling a tab never decides which panel is visible
- Panels aren't switched in edit mode; show or hide them by hand while laying them out, and `LeafTabPanels` syncs them when enabled
- The old panel hides before the new one shows. On a swap (any change after the first panel is shown since `LeafTabPanels` was enabled), a `LeafNavigationScope` on the new panel's root gets `TakeFocus()`, waiting for something selectable if the panel is still being filled. The starting panel doesn't take focus
- Typical setup: tabs are `LeafRadioOptionUnselectable`s with a `LeafTab` under the group, each panel has a `LeafNavigationScope` with `ContainNavigation` off, `SelectOnEnable` off and `Entry` set to `Remembered` (back to where you were in that tab) or `First`, all under the screen's containing scope
- Switch tabs from a gamepad or keyboard with `LeafRadioGroup.SelectNext`, e.g. from shoulder buttons

### Element State
- `LeafElementMode` is only the interaction: `Normal`, `Highlighted`, `Pressed`
- `LeafElementState` combines the mode with `LeafElementFlags` (`Selected`, `Disabled`, `On`), so a selected element can still be highlighted or pressed (Unity's `SelectionState` can't express that)
- Check flags with `IsSelected`/`IsDisabled`/`IsOn` or `HasAll`/`HasAny` rather than `Enum.HasFlag`, which can allocate on Mono
- `LeafAnimatorColors` gives each Graphic one `LeafColorTarget`: base colors per mode, plus layers that apply when their flags match, in order (e.g. Selected, On, then Disabled last). A layer can override or multiply the color so far
- Fades are run by the animator with unscaled time from a tracked color, so they keep going while a Graphic is hidden and are repaired when the animator is enabled, when an Active Target shows a Graphic, and after a scene save or undo in the editor. Nothing runs per frame while idle apart from a cheap check in LateUpdate. Set the Selectable's Transition to None, and don't make a Toggle's own Graphic a color target
- Showing/hiding GameObjects by flags is a separate list (`LeafActiveTarget`). A face that fades in can often stay active and use alpha in its layers instead
- Disabled elements report `Normal` mode but keep `IsSelected`
