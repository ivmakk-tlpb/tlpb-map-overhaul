using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace MapOverhaul.Game
{
    // The sprites of the POIs, and their look on the pooled icon objects of the map. A POI icon comes from the pool of
    // the marker icons, so an object that goes back to the pool can carry a POI sprite: the game icon that takes it next
    // gets the sprite and the color of the pool back.
    internal static class IconSprites
    {
        private const int IconSize = 64;
        private const int DotSize = 32;
        private const int HaloRadius = 4;
        private const float HaloAlpha = 0.85f;
        private const float Outline = 3f;
        private const float SilverRing = 6f;

        private static readonly Rgb Ink = new Rgb(52, 38, 24);
        private static readonly Rgb Halo = new Rgb(226, 205, 160);
        private static readonly Rgb DotOutline = new Rgb(40, 28, 18);
        private static readonly Rgb[] MetalColors = { new Rgb(47, 79, 79), new Rgb(214, 120, 50), new Rgb(150, 45, 35), new Rgb(245, 245, 250) };

        private static readonly Sprite[] sprites = new Sprite[6];

        // The mod id whose sprite each icon object carries now. A game icon or another mod id that takes the object
        // changes or removes the entry, so a reused object is styled again.
        private static readonly Dictionary<IntPtr, int> owner = new Dictionary<IntPtr, int>();
        private static Sprite poolSprite;
        private static Color poolColor;
        private static bool havePool;

        // Unity can unload a sprite with a world, so a sprite is drawn again when it is gone.
        internal static Sprite Get(PoiKind kind)
        {
            var sprite = sprites[(int)kind];
            if (sprite != null) return sprite;
            sprite = kind == PoiKind.Cabin || kind == PoiKind.Cave ? Icon(kind == PoiKind.Cabin ? "cabin-64.png" : "cave-64.png") : Dot(kind);
            sprites[(int)kind] = sprite;
            return sprite;
        }

        // Gives the icon object of a mod id the sprite of its kind, unless the object carries the sprite of that id now.
        internal static void StyleMod(int id, PoiKind kind, GameObject icon)
        {
            if (owner.TryGetValue(icon.Pointer, out var current) && current == id) return;
            var image = icon.GetComponent<Image>();
            if (!havePool && !owner.ContainsKey(icon.Pointer))
            {
                poolSprite = image.sprite;
                poolColor = image.color;
                havePool = true;
            }
            image.sprite = Get(kind);
            image.color = Color.white;
            owner[icon.Pointer] = id;
            Plugin.Debug("New mod icon: " + kind + " " + id);
        }

        // Puts the look of the pool back on a game icon whose object carried a mod sprite.
        internal static void RestoreGame(GameObject icon)
        {
            if (owner.Count == 0 || !owner.Remove(icon.Pointer)) return;
            var image = icon.GetComponent<Image>();
            image.sprite = poolSprite;
            image.color = poolColor;
            Plugin.Debug("Game icon look restored");
        }

        // Forgets the icon objects at a world unload. The map window and its pools go with the world.
        internal static void Forget()
        {
            owner.Clear();
            havePool = false;
        }

        internal static void Reset()
        {
            Forget();
            Array.Clear(sprites, 0, sprites.Length);
        }

        private static Sprite Icon(string resource)
        {
            var source = new Texture2D(2, 2);
            ImageConversion.LoadImage(source, Resource(resource));
            int size = source.width;
            var mask = new float[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    mask[y * size + x] = source.GetPixel(x, size - 1 - y).a;
            return Make(IconPixels.Icon(mask, size, Ink, Halo, HaloRadius, HaloAlpha), size);
        }

        private static Sprite Dot(PoiKind kind)
        {
            float outline = kind == PoiKind.Silver ? SilverRing : Outline;
            return Make(IconPixels.Dot(DotSize, MetalColors[kind - PoiKind.Tin], DotOutline, outline), DotSize);
        }

        private static Sprite Make(Pixel[] pixels, int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var p = pixels[y * size + x];
                    texture.SetPixel(x, size - 1 - y, new Color(p.R, p.G, p.B, p.A));
                }
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static byte[] Resource(string name)
        {
            using var stream = typeof(IconSprites).Assembly.GetManifestResourceStream("MapOverhaul." + name);
            using var bytes = new MemoryStream();
            stream.CopyTo(bytes);
            return bytes.ToArray();
        }
    }
}
