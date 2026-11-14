![version](https://img.shields.io/badge/version-1.0.0-blue) ![coverage](https://img.shields.io/badge/coverage-98%25-brightgreen)

# Map Overhaul

Map Overhaul is a mod for the Steam game *The Last Plague: Blight*. The game's Map shows the land, the roads, and the villages, but not the cabins you passed, the caves, or the ore rocks you saw, so you set markers by hand to find them again. This mod draws them on the Map for you, darkens the land you have not been near yet, and adds a legend to turn each kind on or off. The idea is close to [Cave markers and more](https://www.nexusmods.com/thelastplagueblight/mods/7), which places the pins with a separate program. This mod runs inside the game.

## Features

- **Cabins.** A cabin icon shows at each cabin in the land that the game generated.
- **Caves.** A cave icon shows at the entrance of each cave of the region, also of caves far from where you have been.
- **Ore deposits.** Cassiterite, Copper-rich Rocks, Iron-rich Rocks, and Silver-rich Rocks show as small dots: tin dark grey, copper orange, iron dark red, and silver white with a dark ring. Rocks of one metal less than 30 m apart make one dot. Rocks inside a cave get no dot.
- **Hover texts.** Point at an icon to see "Cabin", "Iron deposit (4)" with the rock count, or "Cave" with one line for each ore inside it, such as "Iron Ore (4)".
- **Fills in as you explore.** Cabins and ore rocks come from the land that the game generated, which is the land near where you have been. New land adds its cabins and rocks at the next opening of the Map. A rock that you mined until it is gone drops off at the next opening of the Map.
- **Fog of war.** The land that the game has not generated yet shows darker. The land, the roads, the village names, and the icons stay readable under it.
- **Map legend.** A small panel in the top-right corner of the Map lists Cabins, Caves, Tin, Copper, Iron, Silver, and Fog of war. Click a row to hide or show that kind at once. The mod keeps your choices for the next game. Click the title to fold the legend into a small button, so you can see the Map under it. Each save load opens it again.
- **Icon sizes.** The cabin and cave icons have the size of the game's fire pit icon, and all icons and dots keep their size when you zoom.
- **First load.** The first time you load a save with the mod, it reads the save in the background while you play (about 15 to 20 seconds for a large save). The legend shows "Loading POIs... 45%" until it is done, and the Map shows what it found so far. Later loads take less than a second, because the mod keeps what it read in a cache and reads again only the parts of the save that changed. After a switch to another game version, such as the beta branch and back, the first load reads the save again.

## Save safety

The mod writes nothing to the save. You can remove the mod at any time, and your save loads as before. The mod keeps its cache in `BepInEx\cache\MapOverhaul`, outside the save folder. You can delete that folder at any time, and the next load reads the save again.

## Multiplayer

Single player only. In a multiplayer game the mod does nothing, and the Map is as in the game.

## Languages

The ore names in the cave hover texts come from the game, in your game language. The other texts of the mod are in English.

## Requirements

- The Last Plague: Blight 0.12.7 or later. Also tested on the beta branch `new-save-system` (game version 0.12.8b3), which uses the new save format.
- The [BepInEx Pack for The Last Plague: Blight](https://www.nexusmods.com/thelastplagueblight/mods/9), the BepInEx 6 (IL2CPP) build for the game.

## Install

Download the zip from [Nexus Mods](https://www.nexusmods.com/thelastplagueblight/mods/14) or [GitHub Releases](https://github.com/ivmakk-tlpb/tlpb-map-overhaul/releases).

1. Install the [BepInEx Pack for The Last Plague: Blight](https://www.nexusmods.com/thelastplagueblight/mods/9).
2. Extract this mod's zip into the game folder (the folder with the game .exe). The DLL lands in `BepInEx\plugins`. Full path example:
   - Steam: `C:\Program Files (x86)\Steam\steamapps\common\The Last Plague\BepInEx\plugins\MapOverhaul.dll`

## Uninstall

Delete `MapOverhaul.dll` from the `BepInEx\plugins` folder, and the folder `BepInEx\cache\MapOverhaul`. On the next game start, the Map is as in the game.

## Configuration

The legend on the Map writes its rows to `BepInEx\config\com.ivmakk.thelastplagueblight.mapoverhaul.cfg`. You can also edit that file while the game is closed (see [CONFIG.md](CONFIG.md) for every setting).

## Troubleshooting

If a game update renames a part of the game that the mod needs, only the feature that uses it turns off, with one warning in the log. The other features keep working.

Check `BepInEx\LogOutput.log` for the `Map Overhaul loaded.` line, and for warnings or errors from Map Overhaul.

## Credits

The cabin and cave icons were made by the author with ChatGPT.

## Build

Map Overhaul is a BepInEx 6 IL2CPP plugin. Building requires the .NET 8 SDK and a game installation with BepInEx. Start the game once after installing BepInEx to generate the IL2CPP interop assemblies.

```
dotnet build src/MapOverhaul.csproj -c Release
```

`Directory.Build.props` sets `GameDir` to the default Steam installation path. For another location, set the `GameDir` environment variable or pass `-p:GameDir=...` to the build command. The output DLL is `src\bin\Release\MapOverhaul.dll`.

Unit tests cover all files of `src/` and run with no game and no BepInEx, because the test project compiles the patch code against fake game types.

```
dotnet test tests/MapOverhaul.Tests
```

## Package

```
dotnet build src/MapOverhaul.csproj -c Release -p:Package=true
```

The zip is `dist/MapOverhaul-<version>.zip`, laid out as `BepInEx/plugins/MapOverhaul.dll`.

## License

Licensed under the GNU General Public License v3.0. Copyright (C) 2026 ivmakk. See [LICENSE](LICENSE).

You may reuse and modify this mod, but you must keep it open under the same license and give credit. Do not reupload it without credit.
