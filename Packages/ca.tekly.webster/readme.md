# Webster
Webster provides a set of utilities for remotely inspecting, interacting with and debugging your game.

Webster embeds a web server into your game. This provides a remote web interface into your game from a browser. No IDE or Editor is required to use Webster at runtime. This allows anyone on your team to use Webster.

Webster has several goals:

- To make playing and testing on device as easy as possible
- To make it easy to cheat and debug on device builds
- To provide artists, designers, and QA with user friendly interfaces for cheating and debugging
- To provide an easy way to get performance information from device builds
- To provide an easy way to add new cheats and debug features

Webster achieves these goals by providing a web interface into your game that is easy to use and extend. It can also be completely compiled out for your production builds.

See the `Webster/Docs` directory for more info

## Preview of the web interfaces
These are screenshots of a browser connected to an Android device on the same network

### Assets
View assets that are in memory
![assets.jpg](Images%2Fassets.jpg)

### Commands
Execute commands. You can easily add your own.
![commands.jpeg](Images%2Fcommands.jpeg)

### Disk
View and downloads files from device
![disk.jpeg](Images%2Fdisk.jpeg)

### Frameline
View a timeline of user defined events. Great for view the order of events or profiling multi-frame operations
![frameline.jpeg](Images%2Fframeline.jpeg)

### GameObjects
View the GameObject hierarchy and perform basic operations on GameObjects and Components
![gameobjects.jpeg](Images%2Fgameobjects.jpeg)