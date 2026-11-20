// Custom build: sound effects (NSSound) and trackpad haptics (NSHapticFeedbackManager) for the donut and conversions.
//
// Sounds are macOS's own (/System/Library/Sounds), so nothing has to be bundled; a file of one's own takes a sound's
// place (see FeedbackEffects in Filee.App). Haptics are felt only on a Force Touch trackpad while a finger touches it,
// which it does during a drag: a mouse or an older trackpad simply feels nothing.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Filee.Core.Platform;

namespace Filee.Platform.MacOS;

[SupportedOSPlatform("macos")]
internal static class MacOSFeedback
{
    private const string ObjC = "/usr/lib/libobjc.A.dylib";

    // NSHapticFeedbackPatternAlignment: the light "snap" macOS uses when something lines up with a guide.
    private const nint AlignmentPattern = 1;
    // NSHapticFeedbackPerformanceTimeNow.
    private const nuint PerformNow = 1;

    private static readonly Lock Gate = new();
    private static readonly Dictionary<string, nint> Sounds = new(StringComparer.Ordinal);
    private static readonly HashSet<string> Unplayable = new(StringComparer.Ordinal);
    private static nint _performer;
    private static bool _noHaptics;

    // AppKit has to be loaded before its classes can be found by name; NativeMethods loads it too.
    private static readonly nint AppKit = NativeLibrary.Load("/System/Library/Frameworks/AppKit.framework/AppKit");

    /// <summary>The built-in macOS sound for each moment.</summary>
    private static string SystemSoundFor(FeedbackSound sound) => sound switch
    {
        FeedbackSound.Open => "Pop",
        FeedbackSound.Hover => "Tink",
        FeedbackSound.Drop => "Bottle",
        FeedbackSound.Done => "Glass",
        FeedbackSound.Failed => "Basso",
        FeedbackSound.Flip => "Pop",
        FeedbackSound.Panel => "Pop",
        FeedbackSound.Select => "Tink",
        _ => "Tink",
    };

    /// <summary>
    /// How loud each built-in sound is next to the others: the hover tick plays often, so it is the quietest.
    /// </summary>
    private static double Loudness(FeedbackSound sound) => sound switch
    {
        FeedbackSound.Hover => 0.45,
        FeedbackSound.Open => 0.5,
        FeedbackSound.Drop => 0.75,
        FeedbackSound.Flip => 0.7,
        FeedbackSound.Panel => 0.45,
        FeedbackSound.Select => 0.5,
        _ => 1.0,
    };

    internal static void Play(FeedbackSound sound, double volume, string? file)
    {
        GC.KeepAlive(AppKit);
        var level = (float)Math.Clamp(volume * Loudness(sound), 0, 1);
        if (level <= 0)
            return;
        using var pool = new AutoreleasePool();
        var player = file is not null ? SoundFromFile(file) : 0;
        if (player == 0)
            player = NamedSound(SystemSoundFor(sound));
        if (player == 0)
            return;
        // A sound still playing (the hover tick, moved over slices quickly) starts again rather than being skipped.
        if (SendBool(player, Sel("isPlaying")))
            SendVoid(player, Sel("stop"));
        SendFloat(player, Sel("setVolume:"), level);
        SendBool(player, Sel("play"));
    }

    internal static void HapticTick()
    {
        GC.KeepAlive(AppKit);
        if (_noHaptics)
            return;
        using var pool = new AutoreleasePool();
        if (_performer == 0)
        {
            var manager = objc_getClass("NSHapticFeedbackManager");
            _performer = manager == 0 ? 0 : Send(manager, Sel("defaultPerformer"));
            if (_performer == 0)
            {
                _noHaptics = true;
                return;
            }
            Send(_performer, Sel("retain"));
        }
        SendHaptic(_performer, Sel("performFeedbackPattern:performanceTime:"), AlignmentPattern, PerformNow);
    }

    /// <summary>A macOS sound by its name ("Tink"), kept for the next time; 0 when there is none.</summary>
    private static nint NamedSound(string name)
    {
        lock (Gate)
        {
            if (Sounds.TryGetValue("system:" + name, out var known))
                return known;
            var sound = Send(objc_getClass("NSSound"), Sel("soundNamed:"), NSString(name));
            if (sound != 0)
                Send(sound, Sel("retain"));
            Sounds["system:" + name] = sound;
            return sound;
        }
    }

    /// <summary>A sound file of one's own, kept for the next time; 0 when it can't be played.</summary>
    private static nint SoundFromFile(string path)
    {
        string key;
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists)
                return 0;
            // A file replaced with another sound is read again.
            key = $"file:{path}|{info.Length}|{info.LastWriteTimeUtc.Ticks}";
        }
        catch (IOException) { return 0; }
        catch (UnauthorizedAccessException) { return 0; }

        lock (Gate)
        {
            if (Unplayable.Contains(key))
                return 0;
            if (Sounds.TryGetValue(key, out var known))
                return known;
            var allocated = Send(objc_getClass("NSSound"), Sel("alloc"));
            var sound = allocated == 0 ? 0 : SendInit(allocated, Sel("initWithContentsOfFile:byReference:"), NSString(path), false);
            if (sound == 0)
            {
                Unplayable.Add(key);
                return 0;
            }
            Sounds[key] = sound; // owned by the cache (alloc + init)
            return sound;
        }
    }

    private static nint Sel(string name) => sel_registerName(name);

    private static nint NSString(string value)
    {
        var utf8 = Marshal.StringToCoTaskMemUTF8(value);
        try
        {
            return Send(objc_getClass("NSString"), Sel("stringWithUTF8String:"), utf8);
        }
        finally
        {
            Marshal.FreeCoTaskMem(utf8);
        }
    }

    private readonly struct AutoreleasePool : IDisposable
    {
        private readonly nint _pool;
        public AutoreleasePool() => _pool = Send(Send(objc_getClass("NSAutoreleasePool"), Sel("alloc")), Sel("init"));
        public void Dispose()
        {
            if (_pool != 0)
                SendVoid(_pool, Sel("drain"));
        }
    }

    [DllImport(ObjC)]
    private static extern nint objc_getClass([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjC)]
    private static extern nint sel_registerName([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern nint Send(nint receiver, nint selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern nint Send(nint receiver, nint selector, nint argument);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern nint SendInit(nint receiver, nint selector, nint path, [MarshalAs(UnmanagedType.I1)] bool byReference);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendVoid(nint receiver, nint selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendFloat(nint receiver, nint selector, float value);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendHaptic(nint receiver, nint selector, nint pattern, nuint performanceTime);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool SendBool(nint receiver, nint selector);
}
