# LethalMod

ESP Mod with pathfinding capabilities.

> If you have issues with the mod not loading properly, please try also installing
> [BepInEx_GraphicsSettings](https://thunderstore.io/c/lethal-company/p/BepInEx_Unofficial/BepInEx_GraphicsSettings/) mod and its dependency, [BepInEx_ConfigurationManager](https://thunderstore.io/c/lethal-company/p/BepInEx_Unofficial/BepInEx_ConfigurationManager/).
>
> As pointed out by @yumm-dev in [#8](https://github.com/weissi1994/LethalMod-ESP/issues/8)

**Controls:**

> All keybindings can be configured in `config/LethalMod.cfg` under the `[Keybindings]` section.
>
> **To unbind a key:** Set its value to an empty string or whitespace in the config file. For example:
> ```ini
> [Keybindings]
> Toggle UI =
> ```
>
> **Font Size Configuration:** Adjust the font size in `config/LethalMod.cfg` under the `[UI]` section:
> ```ini
> [UI]
> ## Font size for ESP labels (default: 12, recommended for 4K: 18-24)
> Font Size = 12
> ```
> For 4K displays, values between 18-24 are recommended for better readability.

- Hit `p` to Toggle UI for ESP (default: Enabled)
- Hit `3` to Toggle ESP in general (default: Enabled)
- Hit `4` to Toggle ESP for Enemies (default: Enabled)
- Hit `5` to Toggle ESP for Players (default: Enabled)
- Hit `6` to Toggle ESP for Doors (default: Enabled)
- Hit `7` to Toggle ESP for Items (default: Enabled)
- Hit `8` to Toggle Rendering of incomplete paths (default: Disabled)
- Hit `F` to Open big door closest to the player
- Hit `X` to Close all big doors
- Hit `C` to Open/Unlock all doors

**Features:**

- Monsters/Landmines/Turrets (Red by default)
- Items (Blue by default)
- Players (Green by default)
- Exits (Cyan by default)
- Terminals (Magenta by default)
- Partial/Incomplete paths (Yellow by default)

**Customizable Colors:**

All ESP colors can be customized in `config/LethalMod.cfg` under the `[Colors]` section using RGB values (0-255). This is especially helpful for colorblind users or for better visibility.

Example configuration:
```ini
[Colors]
# Enemy/Hazard Colors (Landmines, Turrets, Enemies)
Enemy Red = 255
Enemy Green = 0
Enemy Blue = 0

# Item Colors
Item Red = 0
Item Green = 0
Item Blue = 255

# Player Colors
Player Red = 0
Player Green = 255
Player Blue = 0

# Exit Colors
Exit Red = 0
Exit Green = 255
Exit Blue = 255

# Hazard Colors (Landmines, Turrets, Steam Valves)
Hazard Red = 255
Hazard Green = 0
Hazard Blue = 0

# Terminal Colors
Terminal Red = 255
Terminal Green = 0
Terminal Blue = 255

# Partial Path Colors (incomplete/unreachable paths)
Partial Path Red = 255
Partial Path Green = 255
Partial Path Blue = 0
```

> **Note:** To make colors more visible for colorblindness, try:
> - Protanopia/Deuteranopia: Use blue (0, 0, 255) and yellow (255, 255, 0) instead of red and green
> - Tritanopia: Use red (255, 0, 0) and cyan (0, 255, 255) with higher saturation

## Tested with:

- https://thunderstore.io/c/lethal-company/p/LethalMinors/Multiplayer_Modpack/

---

Inspired by:

- https://github.com/KaylinOwO/Project-Apparatus
- https://github.com/pseuxide/lethal_to_company
- https://www.unknowncheats.me/forum/other-fps-games/612099-lethal-company-esp.html
