# Favorites
- A simple tool window for quickly accessing commonly used assets or GameObjects
- Open it with `Ctrl+G` or `CMD+G`
  - The window will open at your mouse cursor position
  - You can open a dockable window by open the window from the menu `Tools/Favorites`
- Drag assets from the Project Window or GameObjects from hierarchy onto the window
  - GameObjects will only be accessible if the scene or prefab they're from is open
- You can drag items out of the Favorites window just as you would from Project or Hierarchy window
- You can drag items in the Favorites window to reorder them

## Shortcuts
- You can use the number keys to activate a favorite in the actively selected collection
- Hold shift to bring up the collections and use the number keys to swap between them

## Collections
- Favorites supports having collections that you can quickly swap between
- You can use collections to group related assets together or to make a quick access collection of things you use constantly
- Hit the Edit button to add, remove and rename collections

## FavoriteAction Assets
- Actions are essentially functions you want to invoke when activating a Favorite in the window
- Actions are implemented as ScriptableObjects. So you just create an instance of the ScriptableObject and add it to the window
  - When double clicking or using the number shortcut the window will activate the FavoriteAction
- You are responsible for implementing useful actions
- To create a FavoriteAction right click on a directory in the Project window
  - `Favorites/Menu Action`: Enter a menu and it will activated when this action is invoked
  - `Favorites/Scripted Action`: Allows you to easily implement an action. 
    - You can select any `IFavoriteActionScript` from the dropdown in this object
    - Deriving from `IFavoriteActionScript` will automatically have your class show up here
    - See `FavoriteActionDisableRaycastTarget` for a good example. It disables `Raycast Target` on the currently select GameObjects
  