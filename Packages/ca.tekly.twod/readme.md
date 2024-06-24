# TwoD

- Custom format for importing animated sprites and tiles from Aseprite.
- The exporter only works on mac for now

# Setup

## Exporting from Aseprite

### Scripts
There is an accompanying Aseprite script that exports sprites in the right format.

- They are in the Scripts directory of this package
- You need to copy them to your Aseprite Scripts Folder 
- You can find this in Aseprite by going to File/Scripts/Open Scripts Folder

### Rogue Export
- This exports the Aseprite file as animations
- In Unity this will be a CellSprite

### Tile Export
- This exports the Aseprite file to be used in tilemaps

### Configuration
- The scripts expect a config file named `.aseconfig` to be in a parent directory of your Aseprite file
- This file should only contain a relative path from the config file that you want assets exported to in your Unity project
    - **THIS FILE MUST BE A SINGLE LINE. DO NOT HAVE A NEW LINE ENDING THE FILE**

A common set up would be to have something like

#### .aseconfig
```
../game_unity_project/Assets/Content
```

#### Directory Structure

```
- RepositoryRoot
  - Art
    - .aseconfig
    - Sprites
      - Enemies
      - Heroes
    - Tiles
      - Forest
  - game_unity_project
    - Assets
      - Content
        - Sprites
          - Enemies
          - Heroes
        - Tiles
          - Forest
```

## Runtime
- CellRenderer
  - Attach to a SpriteRenderer to render sprite animations
- CellImage
  - Attach to UI Image to animation sprites in UI