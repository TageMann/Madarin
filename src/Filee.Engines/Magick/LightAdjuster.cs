// Mandarin, Edit Photo: the Light group (exposure, brightness, contrast, highlights, shadows), applied to the picture
// as it is converted and to the window's live preview alike, so the preview is what the file gets.

using Filee.Core.Presets;
using ImageMagick;

namespace Filee.Engines.Magick;

public static class LightAdjuster
{
    /// <summary>
    /// Adjusts <paramref name="image"/>'s light as <paramref name="options"/> say; leaves it as it is when none is set.
    /// Exposure multiplies the light in stops; brightness and contrast are ImageMagick's own; highlights move the white
    /// end and shadows the black end of the tones, so the middle stays where it is.
    /// </summary>
    public static void Apply(IMagickImage<byte> image, ImageOptions options)
    {
        if (!options.AdjustsLight)
            return;
        var exposure = Math.Clamp(options.Exposure, -3, 3);
        if (Math.Abs(exposure) > 0.001)
            image.Evaluate(Channels.RGB, EvaluateOperator.Multiply, Math.Pow(2, exposure));

        int brightness = Math.Clamp(options.Brightness, -100, 100), contrast = Math.Clamp(options.Contrast, -100, 100);
        if (brightness != 0 || contrast != 0)
            image.BrightnessContrast(new Percentage(brightness * 0.5), new Percentage(contrast * 0.5));

        var highlights = Math.Clamp(options.Highlights, -100, 100);
        if (highlights > 0)
            image.Level(new Percentage(0), new Percentage(100 - highlights * 0.25), Channels.RGB);
        else if (highlights < 0)
            image.InverseLevel(new Percentage(0), new Percentage(100 + highlights * 0.25), Channels.RGB);

        var shadows = Math.Clamp(options.Shadows, -100, 100);
        if (shadows > 0)
            image.InverseLevel(new Percentage(shadows * 0.25), new Percentage(100), Channels.RGB);
        else if (shadows < 0)
            image.Level(new Percentage(-shadows * 0.25), new Percentage(100), Channels.RGB);
    }
}
