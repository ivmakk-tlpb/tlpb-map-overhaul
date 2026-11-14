# Images

- `content.json` - the text, the icons, and the crops of the thumbnail and the page background. The `nexus-mod-images` tool renders both images from it.
- `01-thumb.jpg` - 1920x1080, the first gallery image, so Nexus uses it as the thumbnail in the mods grid. The title ("Map" in the accent, "Overhaul" in white), two captions: "Cabins and caves / on the game Map" with the mod's own cabin icon (`MapOverhaul/cabin`), and "Ore deposits / tin, copper, iron, silver" with the game's Copper-rich Rock texture (`resources.assets/Texture2D/copper_rock_resource_1`). Behind them, the Map of `src/map-on.jpg` at 1:1, crop `[300, 820, 1920, 1080]`, with the cabins, the caves, the ore dots, and the edge of the fog of war.

The cabin icon is the mod's own image, so the tool cannot extract it from the game files. Before a render, copy it into the gitignored icon folder of the game repo: `assets/cabin.png` to `icons/MapOverhaul/cabin.png`.
- `02-map-without-mod.jpg` - 1920x1080, gallery. Without the mod: the Map of the game. From `src/map-off.jpg`, scaled down.
- `03-map-with-mod.jpg` - 1920x1080, gallery. With the mod, the same Map view: the cabin and cave icons, the ore dots, the fog of war, and the legend. From `src/map-on.jpg`, scaled down.
- `04-cave-hover.jpg` - 1920x1080, gallery. The hover text of a cave: "Cave", "Copper Ore (1)", "Iron Ore (7)". The legend shows Silver turned off. The full screenshot `src/cave-hover.jpg`, scaled down.
- `05-deposit-hover.jpg` - 1920x1080, gallery. The hover text of an iron dot: "Iron deposit (2)". The full screenshot `src/deposit-hover.jpg`, scaled down.
- `banner.jpg` - 1300x372, the page background. The explored part of the Map of `src/map-on.jpg`, crop `[1180, 1250, 1420, 406]`, with the Map across the whole width, darker at the left and the bottom, so the mod name reads.
- `src/` - the screenshots, taken on 2026-11-14 at 3840x2160 on game version 0.12.7 in game driver sessions on the fixture `map-overhaul`: `map-off.jpg` with no mod (session `20261114-164218-709-session`), `map-on.jpg` with Map Overhaul 1.0.0 (session `20261114-164341-722-session`). Stored as JPG at quality 95. `cave-hover.jpg` and `deposit-hover.jpg` are the user's own screenshots at 3840x2160, stored as taken.

The description has no inline image. If one is added, upload the file to the Nexus images of the mod first and use the hosted URL.
