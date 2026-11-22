// Mandarin's glass wheel turns over as one disc: eased, never showing squeezed text, and safe to press repeatedly.

using Avalonia.Headless.XUnit;
using Filee.App.Controls;
using Filee.App.Services;

namespace Filee.App.Tests;

[Collection("Motion")]
public class GlassFlipTests
{
    private static DonutMenu Glass()
    {
        DonutItem[] Face(string prefix) => Enumerable.Range(0, 8).Select(i => new DonutItem { Id = prefix + i, Label = prefix + i }).ToArray();
        var donut = new DonutMenu { GlassStyle = true, CanFlip = true, Items = Face("A") };
        var front = true;
        donut.FaceSwapping += (_, _) =>
        {
            front = !front;
            donut.Items = Face(front ? "A" : "B");
        };
        return donut;
    }

    [Fact]
    public void The_easing_rises_smoothly_from_0_to_1_and_answers_at_once()
    {
        Assert.Equal(0, DonutMenu.GlassEase(0));
        Assert.Equal(1, DonutMenu.GlassEase(1));
        var previous = 0.0;
        for (var t = 0.02; t <= 1; t += 0.02)
        {
            var y = DonutMenu.GlassEase(t);
            Assert.True(y >= previous - 1e-9, $"not monotonic at {t}");
            previous = y;
        }
        // Most of the turn is done in the first half of the time: it answers the key at once, then settles.
        Assert.True(DonutMenu.GlassEase(0.5) > 0.8);
        // ...without a jump at the start.
        Assert.True(DonutMenu.GlassEase(0.02) < 0.05);
    }

    [AvaloniaFact]
    public void The_disc_turns_as_one_and_text_is_hidden_while_squeezed()
    {
        TestServices.EnsureInitialized("en");
        var previous = Motion.Enabled;
        try
        {
            Motion.Set(true);
            var donut = Glass();
            Assert.True(donut.Flip());
            for (var t = 0.0; t < DonutMenu.GlassFlipDuration; t += 1 / 120.0)
            {
                // Every slice of a face has the same width: one disc, no wave.
                var widths = Enumerable.Range(0, 8).Select(i => donut.SliceFlipWidth(false, i))
                    .Concat(Enumerable.Range(0, 8).Select(i => donut.SliceFlipWidth(true, i))).Where(w => w > 0).Distinct().ToList();
                Assert.True(widths.Count <= 1);
                // Text shows only on a face wide enough not to look squeezed.
                for (var i = 0; i < 8; i++)
                {
                    foreach (var old in new[] { false, true })
                        if (donut.SliceFlipWidth(old, i) < 0.55)
                            Assert.Equal(0, donut.FlipLabelOpacity(old, i));
                }
                donut.Advance(1 / 120.0);
            }
            donut.Advance(0.05);
            Assert.False(donut.IsFlipping);
            Assert.Equal("B0", donut.Items![0].Id);
            Assert.Equal(1, donut.FlipLabelOpacity(false, 0));
        }
        finally { Motion.Set(previous); }
    }

    [AvaloniaFact]
    public void Rapid_presses_land_on_the_right_face_and_stop()
    {
        TestServices.EnsureInitialized("en");
        var previous = Motion.Enabled;
        try
        {
            Motion.Set(true);
            var donut = Glass();
            // Three presses in quick succession: an odd number, so the back face, and nothing left running.
            donut.Flip();
            donut.Advance(0.03);
            donut.Flip();
            donut.Advance(0.03);
            donut.Flip();
            for (var i = 0; i < 200 && donut.IsFlipping; i++)
                donut.Advance(1 / 60.0);
            Assert.False(donut.IsFlipping);
            Assert.StartsWith("B", donut.Items![0].Id);
        }
        finally { Motion.Set(previous); }
    }
}
