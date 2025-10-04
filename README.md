# LethalMod

An ESP (Extra Sensory Perception) mod for the game Lethal Company, with pathfinding capabilities.

## Purpose

LethalMod enhances the gameplay experience by providing visual cues for various in-game entities. This includes highlighting enemies, players, items, and exits, making them easier to locate. The mod also includes pathfinding to help players navigate the environment, as well as several quality-of-life features such as the ability to open and close doors with hotkeys.

## Features

- **ESP**:
    - **Enemies**: Monsters, landmines, and turrets are highlighted in red.
    - **Items**: Grabbable items are highlighted in blue.
    - **Players**: Other players are highlighted in green.
    - **Doors/Exits**: Exits are highlighted in cyan.
    - **Pathfinding**: Draws a path to certain objects, such as exits and valuable items. Yellow paths indicate that the target is unreachable.
- **Door Control**:
    - Open the nearest big door.
    - Close all big doors.
    - Open and unlock all doors.
- **Customizable UI**:
    - Toggle the entire UI on or off.
    - Toggle individual ESP features on or off.

## Configuration

All settings can be configured in the `config/LethalMod.cfg` file, which is generated after running the game with the mod for the first time.

### Controls

- `p`: Toggle the UI. (Default: Enabled)
- `3`: Toggle ESP in general. (Default: Enabled)
- `4`: Toggle ESP for enemies. (Default: Enabled)
- `5`: Toggle ESP for players. (Default: Enabled)
- `6`: Toggle ESP for doors. (Default: Enabled)
- `7` Toggle ESP for items. (Default: Enabled)
- `8`: Toggle rendering of incomplete paths. (Default: Disabled)
- `f`: Open the big door closest to the player.
- `x`: Close all big doors.
- `c`: Open/Unlock all doors.

## Building from Source

To build this mod from the source code, you will need to have the .NET SDK installed.

1.  Clone the repository:
    ```bash
    git clone https://github.com/weissi1994/LethalMod-ESP
    ```
2.  Navigate to the project directory:
    ```bash
    cd LethalMod-ESP
    ```
3.  Build the project:
    ```bash
    dotnet build ./LethalMod.sln
    ```
    The compiled `.dll` file will be located in the `LethalMod/bin/Debug/netstandard2.1` directory.

## Troubleshooting

If you have issues with the mod not loading properly, please try also installing the [BepInEx_GraphicsSettings](https://thunderstore.io/c/lethal-company/p/BepInEx_Unofficial/BepInEx_GraphicsSettings/) mod and its dependency, [BepInEx_ConfigurationManager](https://thunderstore.io/c/lethal-company/p/BepInEx_Unofficial/BepInEx_ConfigurationManager/).

As pointed out by @yumm-dev in [#8](https://github.com/weissi1994/LethalMod-ESP/issues/8).

## Inspiration

This mod was inspired by the following projects:
- https://github.com/KaylinOwO/Project-Apparatus
- https://github.com/pseuxide/lethal_to_company
- https://www.unknowncheats.me/forum/other-fps-games/612099-lethal-company-esp.html