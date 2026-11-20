// Mandarin, Edit Photo: the light adjustments move the tones the way they say, and leave a picture alone at 0.

using Filee.Core.Presets;
using Filee.Engines.Magick;
using ImageMagick;

namespace Filee.Engines.Tests;

public class LightAdjusterTests
{
    private static double Mean(MagickImage image) => image.Statistics().GetChannel(PixelChannel.Red)!.Mean;

    private static MagickImage Grey(byte level = 100)
    {
        var image = new MagickImage(new MagickColor(level, level, level), 32, 32);
        return image;
    }

    [Fact]
    public void Nothing_set_leaves_the_picture_as_it_is()
    {
        using var image = Grey();
        var before = Mean(image);
        LightAdjuster.Apply(image, new ImageOptions());
        Assert.Equal(before, Mean(image));
        Assert.False(new ImageOptions().AdjustsLight);
        Assert.False(new ImageOptions().EditsPicture);
    }

    [Theory]
    [InlineData(1.0, 0, 0, 0, 0, true)]
    [InlineData(-1.0, 0, 0, 0, 0, false)]
    [InlineData(0, 40, 0, 0, 0, true)]
    [InlineData(0, -40, 0, 0, 0, false)]
    [InlineData(0, 0, 0, 0, 60, true)]
    [InlineData(0, 0, 0, 0, -60, false)]
    public void Each_adjustment_moves_the_tones_its_way(double exposure, int brightness, int contrast, int highlights, int shadows, bool lighter)
    {
        using var image = Grey();
        var before = Mean(image);
        var options = new ImageOptions { Exposure = exposure, Brightness = brightness, Contrast = contrast, Highlights = highlights, Shadows = shadows };
        Assert.True(options.AdjustsLight);
        Assert.True(options.EditsPicture);

        LightAdjuster.Apply(image, options);

        Assert.True(lighter ? Mean(image) > before : Mean(image) < before, $"{before} → {Mean(image)}");
    }
}
