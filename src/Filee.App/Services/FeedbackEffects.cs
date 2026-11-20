// Custom build: sound effects and trackpad haptics.
//
// The platform plays them (IPlatformServices.PlayFeedbackSound / PerformHapticTick; macOS only for now). Settings →
// General turns each on or off (AppSettings.SoundEffects, SoundVolume, TrackpadHaptics). A sound of one's own replaces a
// built-in one: put a file named after the moment in the "sounds" folder of Filee's data folder
// (~/Library/Application Support/Filee/sounds on macOS):
//
//   open.*    the donut opens             hover.*   another slice is under the pointer
//   drop.*    files dropped on a slice    done.*    a conversion finished
//   failed.*  a conversion failed
//
// with .wav, .aiff, .aif, .caf, .m4a or .mp3 as the extension.

using System.Runtime.InteropServices;
using Filee.Core.Platform;
using Filee.Core.Settings;

namespace Filee.App.Services;

internal sealed class FeedbackEffects
{
    private readonly IPlatformServices platform;
    private readonly UserDataStore store;

    public FeedbackEffects(IPlatformServices platform, UserDataStore store)
    {
        this.platform = platform;
        this.store = store;
        Shared ??= this;
    }

    /// <summary>The effects the app's windows use (the first made, by the app's own services); null in tests.</summary>
    internal static FeedbackEffects? Shared { get; private set; }

    private static readonly string[] Extensions = [".wav", ".aiff", ".aif", ".caf", ".m4a", ".mp3"];

    /// <summary>Hover ticks closer together than this are one: sweeping across the ring stays a soft patter.</summary>
    private const long MinHoverGapMs = 35;

    private long _lastHover;
    private bool _broken;

    /// <summary>Plays the sound for <paramref name="sound"/> when sound effects are on. UI thread.</summary>
    public void Play(FeedbackSound sound)
    {
        if (_broken || !platform.SupportsFeedbackEffects || store.Settings is not { SoundEffects: true } settings)
            return;
        if (sound == FeedbackSound.Hover)
        {
            var now = Environment.TickCount64;
            if (now - _lastHover < MinHoverGapMs)
                return;
            _lastHover = now;
        }
        Guard(() => platform.PlayFeedbackSound(sound, Math.Clamp(settings.SoundVolume, 0, 1), CustomFile(sound)));
    }

    /// <summary>A light trackpad tap when haptics are on. UI thread.</summary>
    public void Tick()
    {
        if (_broken || !platform.SupportsFeedbackEffects || store.Settings is not { TrackpadHaptics: true })
            return;
        Guard(platform.PerformHapticTick);
    }

    /// <summary>
    /// The slice under the pointer changed to <paramref name="index"/> (or <paramref name="sub"/> of a group's ring):
    /// a tick and a tap when it is a slice, nothing when the pointer left the ring.
    /// </summary>
    public void SliceChanged(int index, int sub)
    {
        if (index < 0 && sub < 0)
            return;
        Tick();
        Play(FeedbackSound.Hover);
    }

    /// <summary>
    /// The file for <paramref name="sound"/>: one's own in the data folder's "sounds", else Mandarin's own (the Sounds
    /// folder next to the program), else none (the platform's sound).
    /// </summary>
    private string? CustomFile(FeedbackSound sound)
    {
        var name = sound.ToString().ToLowerInvariant();
        return FileIn(Path.Combine(store.Directory, "sounds"), name) ?? FileIn(BundledFolder, name);
    }

    /// <summary>Mandarin's own sounds, shipped next to the program.</summary>
    internal static string BundledFolder => Path.Combine(AppContext.BaseDirectory, "Sounds");

    private static string? FileIn(string folder, string name)
    {
        try
        {
            if (!Directory.Exists(folder))
                return null;
            return Extensions.Select(ext => Path.Combine(folder, name + ext)).FirstOrDefault(File.Exists);
        }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }

    /// <summary>A sound or a tap that fails never stops Filee: the effects just stay off until it restarts.</summary>
    private void Guard(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or MarshalDirectiveException
                                       or InvalidOperationException or PlatformNotSupportedException)
        {
            _broken = true;
        }
    }
}
