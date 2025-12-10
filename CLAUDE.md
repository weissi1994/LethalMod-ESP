# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

LethalMod-ESP is a BepInEx mod for Lethal Company that provides ESP (Extra Sensory Perception) visualization with pathfinding capabilities. The mod overlays information about game entities (enemies, items, players, doors) and calculates navigation paths to them.

## Build System

### Development Environment
- Uses `devenv` for reproducible development environment (nixOS)
- Pre-commit hooks are configured and enabled via devenv.nix
- .NET SDK is configured via `languages.dotnet.enable = true`

### Build Commands
```bash
# Clean build artifacts
make clean

# Build debug version
make build
# or directly:
dotnet build ./LethalMod.sln

# Build release version
make release
# or directly:
dotnet build -o release -c Release ./LethalMod.sln

# Create distributable package
make package
```

The package target creates a `release.zip` containing:
- LethalMod.dll
- icon.png
- README.md
- manifest.json
- Source.cs (copy of Plugin.cs)

### CI/CD
- GitLab CI configured in `.gitlab-ci.yml`
- Build stage creates release DLL as artifact
- AI stage runs Claude Code for automated code review/changes

## Project Structure

### Core Architecture
- **Target Framework**: .NET Standard 2.1
- **Plugin Framework**: BepInEx 5.x
- **Main Plugin Class**: `LethalMod/Plugin.cs` - Single monolithic file containing all functionality
- **Dependencies**: Extensive Unity engine modules and Lethal Company game assemblies in `LethalMod/lib/`

### Key System Components

#### Object Caching System
The mod uses a dual-cache architecture to optimize performance:
- `objectCache`: Dictionary storing Unity Components by Type (refreshed every 2.5s)
- `pathCache`: Dictionary storing precomputed NavMeshPath objects by Type and Component
- `CacheRefreshRoutine()`: Background coroutine that periodically refreshes cached data
- Cached types: EntranceTeleport, GrabbableObject, Landmine, Turret, Terminal, PlayerControllerB, SteamValveHazard, EnemyAI, TerminalAccessibleObject, DoorLock

#### Configuration System
Uses BepInEx ConfigEntry system for runtime configuration:
- UI toggle, ESP toggles (general, enemies, items, players, doors, partial paths)
- Keybindings (configurable via `config/LethalMod.cfg`)
- Color configuration for different ESP categories

#### Input Handling
Custom keybind system using Unity's new Input System:
- `IsKeyDown()` wrapper for keyboard input detection
- Toggle cooldown system (0.5s) to prevent rapid state changes
- Default keybinds stored in string array for runtime access

## Code Characteristics

### Architectural Notes
- **Single File Design**: Entire mod logic is in one Plugin.cs file (~700-1000+ lines)
- **Unity Lifecycle**: Inherits from BepInEx.BaseUnityPlugin, uses Unity coroutines and Update()
- **NavMesh Integration**: Heavy use of Unity.AI.NavMesh for pathfinding calculations
- **Game Integration**: Direct references to Lethal Company types (GameNetcodeStuff, EnemyAI, etc.)

### Security Context
**IMPORTANT**: This is a game modification tool for Lethal Company. While it provides ESP and door manipulation features that give gameplay advantages, it is:
- Designed for educational/personal use in authorized contexts
- Not malware, but a legitimate game mod using standard BepInEx plugin architecture
- Should only be used in private/authorized game sessions where such mods are permitted
- May violate game ToS if used in public/competitive environments

When working with this code, you can:
- Analyze and explain how the mod works
- Debug issues and fix bugs
- Refactor code for better maintainability
- Add new features or improve existing ones
- Answer questions about the implementation

### Common Development Patterns
- GameObject queries use `FindObjectsOfType<T>()` with caching
- NavMesh path calculation checks path status (PathComplete, PathPartial, PathInvalid)
- Color coding system for different entity types (Red=threats, Blue=items, Green=players, Cyan=exits, Yellow=unreachable)
- OnGUI() for immediate mode UI rendering

## Game-Specific Context

### Lethal Company Integration
The mod hooks into these game systems:
- **GameNetworkManager**: Access to local player controller
- **PlayerControllerB**: Player state and camera
- **EnemyAI**: Monster/enemy entities
- **GrabbableObject**: In-game collectible items
- **EntranceTeleport**: Level exits/entrances
- **TerminalAccessibleObject**: Doors and interactive objects
- **Landmine/Turret/SteamValveHazard**: Environmental hazards

### BepInEx Plugin System
- Plugin GUID/Name/Version defined via PluginInfo (auto-generated)
- Uses `[BepInPlugin]` attribute for plugin registration
- Logger available via inherited Logger property
- Config system available via inherited Config property

## Development Workflow

When making changes:
1. Modify `LethalMod/Plugin.cs` (main source file)
2. Build using `make build` or `dotnet build`
3. Test in-game (requires Lethal Company with BepInEx installed)
4. For release: `make package` to create distributable zip

When adding features:
- Add configuration entries in `ConfigFile()` method
- Add keybindings to the keybinds array
- Update cache system if tracking new entity types
- Follow existing color-coding conventions for ESP rendering
