# Basic Builder
A simple window for making a player build

This presents several basic options

- Swapping between platforms
- Configuring Development or Release builds
- Turning on and off defines
- Building Addressables before the Player Build

When you open the window for the first time it will create a settings object at `Assets/Editor/Builder/BuildWindowSettings`

You can configure several things here:

- What platforms are supported
- Should Addressables be built before the build
- What defines are available to be turned on and off

![basic_builder_window.png](Assets%2Fbasic_builder_window.png)

## Future Considerations
- Making the list of scenes editable here
- Adding profiles for the settings
