// Mandarin's "Trim Audio" window: the clip tool's model, with what the small window adds — "selected" text, trimming
// silent ends from the waveform, and playing just the chosen range.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Filee.App.ViewModels;

public sealed partial class ClipWindowViewModel
{
    /// <summary>Quieter than this (of the loudest step) counts as silence at either end.</summary>
    internal const float SilentEnd = 0.04f;

    /// <summary>A little sound kept either side of where the silence stops, so a word's first breath isn't clipped.</summary>
    internal const double SilenceMargin = 0.08;

    /// <summary>The window is Mandarin's Trim Audio (the whole sound starts chosen).</summary>
    internal bool IsTrimAudio => _tool.Id == Filee.Core.Presets.BuiltInData.TrimAudioId;

    /// <summary>"0:41.74 selected".</summary>
    public string SelectedText => _loc.Format("trim.selected", LengthText);

    /// <summary>"Start 0:00.00".</summary>
    public string TrimStartText => _loc.Format("trim.start", StartText);

    /// <summary>"End 0:41.74".</summary>
    public string TrimEndText => _loc.Format("trim.end", EndText);

    /// <summary>Moves the ends in to where the sound starts and stops (and back out when turned off).</summary>
    [ObservableProperty] private bool _trimSilentEnds;

    partial void OnTrimSilentEndsChanged(bool value)
    {
        if (value)
            ApplySilentEnds();
        else if (CanEdit)
        {
            MoveHandle(ClipHandle.Start, 0);
            MoveHandle(ClipHandle.End, Duration);
        }
    }

    partial void OnLoudnessChanged(IReadOnlyList<float> value)
    {
        // The waveform is read after the window opens: a tick made before it came applies now.
        if (TrimSilentEnds)
            ApplySilentEnds();
    }

    partial void OnLengthChanged(double value) => OnPropertyChanged(nameof(SelectedText));

    partial void OnStartChanged(double value) => OnPropertyChanged(nameof(TrimStartText));

    partial void OnEndChanged(double value) => OnPropertyChanged(nameof(TrimEndText));

    private void ApplySilentEnds()
    {
        if (SoundBounds(Loudness, Duration) is not { } bounds)
            return;
        MoveHandle(ClipHandle.End, bounds.End);
        MoveHandle(ClipHandle.Start, bounds.Start);
    }

    /// <summary>
    /// Where the sound in <paramref name="loudness"/> (steps over <paramref name="duration"/> seconds, 0 to 1) starts and
    /// stops, with <see cref="SilenceMargin"/> either side; null when it is all silence or not read yet.
    /// </summary>
    internal static (double Start, double End)? SoundBounds(IReadOnlyList<float> loudness, double duration)
    {
        if (loudness.Count == 0 || !(duration > 0))
            return null;
        var first = -1;
        var last = -1;
        for (var i = 0; i < loudness.Count; i++)
        {
            if (loudness[i] < SilentEnd)
                continue;
            if (first < 0)
                first = i;
            last = i;
        }
        if (first < 0)
            return null;
        var step = duration / loudness.Count;
        return (Math.Max(0, first * step - SilenceMargin), Math.Min(duration, (last + 1) * step + SilenceMargin));
    }

    /// <summary>Plays the chosen range from its start; stops when it is playing.</summary>
    [RelayCommand]
    private void PlaySelection()
    {
        if (IsPlaying)
        {
            StopPlaying();
            return;
        }
        Playhead = Start;
        PlayPause();
    }
}
