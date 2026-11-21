using Filee.Platform.MacOS;

namespace Filee.Platform.Tests;

/// <summary>What the ⌘-drop wrapper answers AppKit (see MacOSCommandDrop).</summary>
public sealed class MacOSCommandDropTests
{
    private const nuint Copy = 1, Generic = 4, Move = 16, AllFinder = 1 | 2 | 4 | 16 | 32;

    [Fact]
    public void Command_held_and_the_wheel_wants_the_drop_answers_generic() =>
        Assert.Equal(Generic, MacOSCommandDrop.Decide(0, Generic, wanted: true));

    [Fact]
    public void Command_held_over_a_gap_still_refuses() =>
        Assert.Equal((nuint)0, MacOSCommandDrop.Decide(0, Generic, wanted: false));

    [Fact]
    public void Without_command_avalonia_answer_passes_through() =>
        Assert.Equal(Copy, MacOSCommandDrop.Decide(Copy, AllFinder, wanted: true));

    [Fact]
    public void A_refusal_without_generic_on_offer_stays_a_refusal() =>
        Assert.Equal((nuint)0, MacOSCommandDrop.Decide(0, Move, wanted: true));

    [Fact]
    public void Off_macos_nothing_is_installed()
    {
        if (OperatingSystem.IsMacOS())
            return;
        Assert.False(MacOSCommandDrop.Install());
    }
}
