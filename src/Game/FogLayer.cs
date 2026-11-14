using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace MapOverhaul.Game
{
    // The fog of war of the region map: one RawImage over the map tiles, clear over the chunks that the game generated.
    // The game records no explored area, and it generates the chunks near the player.
    internal static class FogLayer
    {
        private const string Name = "MapOverhaulFog";

        // The alpha values of the fog, built at each map open.
        internal static FogTexture Model;

        private static Texture2D texture;
        private static RawImage image;
        private static bool uploaded;
        private static float tileSizeRatio = 1f;

        // Clears each generated chunk of the region, and uploads the texture when a chunk changed.
        internal static void Build(World world, RegionMapData map)
        {
            double start = ScanRunner.Clock();
            tileSizeRatio = map.tileSizeRatio;
            var min = map.minCoordinates;
            var max = map.maxCoordinates;
            if (Model == null || Model.Width != max.x - min.x + 1 || Model.Height != max.y - min.y + 1)
            {
                Model = new FogTexture(min.x, min.y, max.x, max.y);
                uploaded = false;
            }
            int changed = 0;
            foreach (var chunk in ChunkReader.GeneratedChunks(world))
                if (Model.SetGenerated(chunk.X, chunk.Y)) changed++;
            if (changed > 0 || !uploaded) Upload();
            Plugin.Debug(string.Format(CultureInfo.InvariantCulture, "Fog build: {0} x {1}, {2} chunks changed, {3:F1} ms",
                Model.Width, Model.Height, changed, ScanRunner.Clock() - start));
        }

        // Keeps the fog image above the tiles and over their corners, on each frame of the open map.
        internal static void Place(MapUI ui)
        {
            if (Model == null) return;
            if (image == null || image.transform.parent == null || image.transform.parent.Pointer != ui.tilesArea.Pointer) Create(ui.tilesArea);
            image.gameObject.SetActive(Plugin.Settings.FogOfWar);
            image.transform.SetAsLastSibling();

            var tiles = ui.activeTileUIs;
            if (tiles.Count == 0) return;
            float half = ui.tileSize / 2f;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < tiles.Count; i++)
            {
                var position = tiles[i].transform.localPosition;
                minX = Math.Min(minX, position.x - half);
                minY = Math.Min(minY, position.y - half);
                maxX = Math.Max(maxX, position.x + half);
                maxY = Math.Max(maxY, position.y + half);
            }
            // The game pads the region into square tiles: the region starts at the south-west corner of the tiles
            // and fills the share tileSizeRatio of them (MapUI.Update: region size = tiles x tileSize x tileSizeRatio).
            float width = (maxX - minX) * tileSizeRatio;
            float height = (maxY - minY) * tileSizeRatio;
            var rect = image.rectTransform;
            rect.sizeDelta = new Vector2(width, height);
            rect.localPosition = new Vector3(minX + width / 2f, minY + height / 2f, 0f);
        }

        // The game makes a new map window for each save load, and the old fog image goes with the old window.
        // The texture is the mod's own, so the mod frees it.
        internal static void Reset()
        {
            Model = null;
            if (texture != null) UnityEngine.Object.Destroy(texture);
            texture = null;
            image = null;
            uploaded = false;
        }

        private static void Create(Transform tilesArea)
        {
            var fog = new GameObject(Name);
            image = fog.AddComponent<RawImage>();
            image.raycastTarget = false;
            image.texture = texture;
            image.transform.SetParent(tilesArea, false);
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        // Black pixels with the fog alpha, row by row from the south edge, as the texture takes them.
        private static void Upload()
        {
            if (texture == null || texture.width != Model.Width || texture.height != Model.Height)
            {
                texture = new Texture2D(Model.Width, Model.Height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
                if (image != null) image.texture = texture;
            }
            var alpha = Model.Alpha;
            var bytes = new byte[alpha.Length * 4];
            for (int i = 0; i < alpha.Length; i++) bytes[i * 4 + 3] = (byte)Math.Min(255, (int)(alpha[i] * 256f));
            texture.LoadRawTextureData(bytes);
            texture.Apply();
            uploaded = true;
        }
    }
}
