# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-11-14

### Added

- The Map shows a cabin icon at each cabin in the land that the game generated.
- The Map shows a cave icon at the entrance of each cave of the region.
- The Map shows the ore deposits as small dots: tin dark grey, copper orange, iron dark red, and silver white with a dark ring. Rocks of one metal less than 30 m apart make one dot.
- Hover texts: "Cabin", "Iron deposit (4)", and "Cave" with one line for each ore inside the cave, such as "Iron Ore (4)".
- New land adds its cabins and ore rocks at the next opening of the Map, and a mined-out rock drops off.
- Fog of war: the land that the game has not generated yet shows darker.
- A legend in the top-right corner of the Map turns Cabins, Caves, Tin, Copper, Iron, Silver, and Fog of war on or off, and the config keeps the choice. A click on its title folds it into a small button for the rest of the session.
- The first load of a save reads it in the background, with "Loading POIs... 45%" in the legend. Later loads use the cache in `BepInEx\cache\MapOverhaul`.
- Works on game version 0.12.7 and on the beta branch `new-save-system` (0.12.8b3) with its new save format. After a switch between game versions, the first load reads the save again.
