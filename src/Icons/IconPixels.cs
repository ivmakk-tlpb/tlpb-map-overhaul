using System;

namespace MapOverhaul
{
    // A color of 8 bits for each channel.
    internal readonly struct Rgb
    {
        internal readonly byte R;
        internal readonly byte G;
        internal readonly byte B;

        internal Rgb(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }
    }

    // A pixel with channels from 0 to 1.
    internal readonly struct Pixel
    {
        internal readonly float R;
        internal readonly float G;
        internal readonly float B;
        internal readonly float A;

        internal Pixel(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    }

    // The pixels of the map icons, row by row from the top: a mask in ink color over a halo, and a dot with an outline.
    internal static class IconPixels
    {
        internal static Pixel[] Icon(float[] mask, int size, Rgb ink, Rgb halo, int haloRadius, float haloAlpha)
        {
            var pixels = new Pixel[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float inkAlpha = mask[y * size + x];
                    float haloCover = Dilate(mask, size, x, y, haloRadius) * haloAlpha;
                    pixels[y * size + x] = Over(ink, inkAlpha, halo, haloCover);
                }
            return pixels;
        }

        // A disc of the fill color with an outline of the given width at its edge, with smooth edges.
        internal static Pixel[] Dot(int size, Rgb fill, Rgb outline, float outlineWidth)
        {
            var pixels = new Pixel[size * size];
            float center = size / 2f;
            float radius = center - 1f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - center, dy = y + 0.5f - center;
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                    float cover = Clamp(radius - distance + 0.5f);
                    float inner = Clamp(radius - outlineWidth - distance + 0.5f);
                    pixels[y * size + x] = new Pixel(
                        Mix(outline.R, fill.R, inner), Mix(outline.G, fill.G, inner), Mix(outline.B, fill.B, inner), cover);
                }
            return pixels;
        }

        private static float Dilate(float[] mask, int size, int x, int y, int radius)
        {
            float most = 0f;
            for (int dy = -radius; dy <= radius; dy++)
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int px = x + dx, py = y + dy;
                    if (dx * dx + dy * dy > radius * radius || px < 0 || py < 0 || px >= size || py >= size) continue;
                    most = Math.Max(most, mask[py * size + px]);
                }
            return most;
        }

        // The ink with its alpha over the halo with its alpha.
        private static Pixel Over(Rgb top, float topAlpha, Rgb under, float underAlpha)
        {
            float alpha = topAlpha + underAlpha * (1f - topAlpha);
            if (alpha <= 0f) return new Pixel(0f, 0f, 0f, 0f);
            float under1 = underAlpha * (1f - topAlpha);
            return new Pixel(
                (top.R * topAlpha + under.R * under1) / alpha / 255f,
                (top.G * topAlpha + under.G * under1) / alpha / 255f,
                (top.B * topAlpha + under.B * under1) / alpha / 255f,
                alpha);
        }

        private static float Mix(byte from, byte to, float t) => (from + (to - from) * t) / 255f;

        private static float Clamp(float value) => Math.Max(0f, Math.Min(1f, value));
    }
}
