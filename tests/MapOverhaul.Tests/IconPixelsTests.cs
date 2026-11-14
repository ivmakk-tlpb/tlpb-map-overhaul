using MapOverhaul;
using Xunit;

public class IconPixelsTests
{
    private static readonly Rgb Ink = new Rgb(52, 38, 24);
    private static readonly Rgb Halo = new Rgb(226, 205, 160);

    // A mask of 9 by 9 pixels with one pixel at its center.
    private static float[] Point()
    {
        var alpha = new float[81];
        alpha[4 * 9 + 4] = 1f;
        return alpha;
    }

    [Fact]
    public void A_mask_pixel_has_the_ink_color_at_full_alpha()
    {
        var pixels = IconPixels.Icon(Point(), 9, Ink, Halo, 2, 0.85f);

        var center = pixels[4 * 9 + 4];
        Assert.Equal((52f / 255, 38f / 255, 24f / 255, 1f), (center.R, center.G, center.B, center.A));
    }

    [Fact]
    public void The_halo_has_the_halo_color_within_its_radius()
    {
        var pixels = IconPixels.Icon(Point(), 9, Ink, Halo, 2, 0.85f);

        var near = pixels[4 * 9 + 6];
        Assert.Equal(226f / 255, near.R, 4);
        Assert.Equal(0.85f, near.A, 4);
    }

    [Fact]
    public void Outside_the_halo_is_clear()
    {
        var pixels = IconPixels.Icon(Point(), 9, Ink, Halo, 2, 0.85f);

        Assert.Equal(0f, pixels[4 * 9 + 7].A);
        Assert.Equal(0f, pixels[0].A);
    }

    [Fact]
    public void A_part_mask_pixel_lies_ink_over_the_halo()
    {
        var alpha = new float[9];
        alpha[4] = 0.5f;

        var pixel = IconPixels.Icon(alpha, 3, Ink, Halo, 1, 1f)[4];

        // The halo has alpha 0.5 under the ink of alpha 0.5: alpha 0.5 + 0.5 x 0.5, color (52 x 0.5 + 226 x 0.25) / 0.75.
        Assert.Equal(0.75f, pixel.A, 4);
        Assert.Equal(110f / 255, pixel.R, 4);
    }

    [Fact]
    public void A_dot_has_the_fill_at_its_center_the_outline_at_its_edge_and_is_clear_outside()
    {
        var fill = new Rgb(150, 45, 35);
        var outline = new Rgb(40, 28, 18);

        var pixels = IconPixels.Dot(32, fill, outline, 2f);

        Assert.Equal((150f / 255, 1f), (pixels[16 * 32 + 16].R, pixels[16 * 32 + 16].A));
        Assert.Equal(40f / 255, pixels[16 * 32 + 30].R, 4);
        Assert.Equal(0f, pixels[0].A);
    }

    [Fact]
    public void A_wider_outline_reaches_further_in()
    {
        var fill = new Rgb(255, 255, 255);
        var outline = new Rgb(40, 28, 18);

        var thin = IconPixels.Dot(32, fill, outline, 2f);
        var ring = IconPixels.Dot(32, fill, outline, 5f);

        Assert.Equal(1f, thin[16 * 32 + 27].R, 4);
        Assert.Equal(40f / 255, ring[16 * 32 + 27].R, 4);
    }
}
