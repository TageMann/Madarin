// Mandarin's Trim Audio: where "Trim silent ends automatically" puts the two ends.

using Filee.App.ViewModels;

namespace Filee.App.Tests;

public class TrimAudioTests
{
    [Fact]
    public void Silent_ends_are_trimmed_with_a_small_margin()
    {
        // 10 steps over 10 s: silence for 2 s, sound to 8 s, silence after.
        float[] loudness = [0, 0.01f, 0.3f, 0.9f, 1, 0.6f, 0.2f, 0.05f, 0.01f, 0];
        var bounds = ClipWindowViewModel.SoundBounds(loudness, 10);
        Assert.NotNull(bounds);
        Assert.Equal(2 - ClipWindowViewModel.SilenceMargin, bounds!.Value.Start, 3);
        Assert.Equal(8 + ClipWindowViewModel.SilenceMargin, bounds.Value.End, 3);
    }

    [Fact]
    public void Sound_from_the_first_to_the_last_step_keeps_the_whole_file()
    {
        float[] loudness = [0.5f, 1, 0.5f];
        Assert.Equal((0.0, 3.0), ClipWindowViewModel.SoundBounds(loudness, 3));
    }

    [Fact]
    public void All_silence_or_no_waveform_moves_nothing()
    {
        Assert.Null(ClipWindowViewModel.SoundBounds([0, 0.01f, 0], 3));
        Assert.Null(ClipWindowViewModel.SoundBounds([], 3));
        Assert.Null(ClipWindowViewModel.SoundBounds([1], 0));
    }
}
