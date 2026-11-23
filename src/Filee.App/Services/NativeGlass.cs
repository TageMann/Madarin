// Mandarin, macOS: puts the system's own glass behind a shape of a window — the wheel's disc, the progress ring, a
// floating panel's card. On macOS 26 and later that is Apple's Liquid Glass (NSGlassEffectView); before, the frosted
// HUD material (NSVisualEffectView). Mandarin's drawn glass sits on top of it, lighter, so the real blur and refraction
// of what is behind the window come through. Off macOS, with Settings > General > "Use macOS glass" off, or when the
// native view can't be added, nothing changes: the drawn glass stands alone.
//
// A window that has it carries the class "native-glass", so its styles can thin the drawn surface over it.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Filee.Core.Settings;
using Filee.Platform.MacOS;
using Microsoft.Extensions.Logging;

namespace Filee.App.Services;

internal sealed class NativeGlass
{
    /// <summary>The window class set while native glass is behind its surface.</summary>
    public const string WindowClass = "native-glass";

    private readonly Window _window;
    private MacOSGlassBackdrop? _backdrop;
    private bool _tried;

    private NativeGlass(Window window)
    {
        _window = window;
        window.Closing += (_, _) =>
        {
            _backdrop?.Detach();
            _backdrop = null;
        };
    }

    /// <summary>Settings > General > "Use macOS glass" (on unless the user turned it off), and not given up on.</summary>
    public static bool Wanted => !_brokenThisSession && OperatingSystem.IsMacOS() && AppHost.TryGet<UserDataStore>()?.Settings.NativeGlass != false;

    // Set when anything about the native glass failed: it is left off until Mandarin starts again, and nothing that
    // failed can take the app down with it.
    private static bool _brokenThisSession;

    /// <summary>
    /// Runs <paramref name="work"/> on the UI thread after the current frame (never inside a render pass), and turns
    /// the native glass off for this session if it throws.
    /// </summary>
    private void Later(Action work) => Dispatcher.UIThread.Post(() =>
    {
        if (_brokenThisSession)
            return;
        try
        {
            work();
        }
        catch (Exception ex)
        {
            GiveUp(ex);
        }
    }, DispatcherPriority.Background);

    private void GiveUp(Exception ex)
    {
        _brokenThisSession = true;
        AppHost.TryGet<ILogger<NativeGlass>>()?.LogWarning(ex, "macOS glass failed; Mandarin's drawn glass is used until it restarts");
        try
        {
            _backdrop?.Hide();
            _backdrop?.Detach();
        }
        catch (Exception)
        {
            // already broken: nothing more to undo
        }
        _backdrop = null;
        _window.Classes.Set(WindowClass, false);
    }

    /// <summary>Native glass for <paramref name="window"/>, or null where there is none to have.</summary>
    public static NativeGlass? For(Window window) => Wanted ? new NativeGlass(window) : null;

    /// <summary>Whether the native view is in place (after the window first showed).</summary>
    public bool IsActive => _backdrop is not null;

    /// <summary>Apple's Liquid Glass (macOS 26+) rather than the older frosted material.</summary>
    public bool IsLiquidGlass => _backdrop?.IsLiquidGlass == true;

    private MacOSGlassBackdrop? Backdrop()
    {
        if (_backdrop is not null || _tried)
            return _backdrop;
        if (_window.TryGetPlatformHandle()?.Handle is not { } handle || handle == 0)
            return null; // not shown yet: try again next time
        _tried = true;
        _backdrop = MacOSGlassBackdrop.Attach(handle);
        _window.Classes.Set(WindowClass, _backdrop is not null);
        return _backdrop;
    }

    /// <summary>Shows the glass behind <paramref name="rect"/> (window coordinates), corners rounded by <paramref name="radius"/>.</summary>
    public void Show(Rect rect, double radius)
    {
        if (Backdrop() is not { } backdrop)
            return;
        backdrop.Show(rect.X, rect.Y, rect.Width, rect.Height, radius, _window.ClientSize.Height);
    }

    public void Hide()
    {
        try
        {
            _backdrop?.Hide();
        }
        catch (Exception ex)
        {
            GiveUp(ex);
        }
    }

    /// <summary>
    /// Keeps the glass under the disc <paramref name="donut"/> reports (the wheel's plate, the progress ring), and tells
    /// it whether the glass is there so it draws its own surfaces lighter.
    /// </summary>
    public void UnderDisc(Controls.DonutMenu donut)
    {
        // The wheel reports its disc while it draws: the native view is moved after the frame, never during it.
        donut.GlassDiscChanged += (center, radius) => Later(() =>
        {
            if (radius <= 0 || !_window.IsVisible || donut.TranslatePoint(center, _window) is not { } at)
                Hide();
            else
                Show(new Rect(at.X - radius, at.Y - radius, radius * 2, radius * 2), radius);
            if (donut.NativeGlassBehind != IsActive)
            {
                donut.NativeGlassBehind = IsActive;
                donut.InvalidateVisual();
            }
        });
    }

    /// <summary>
    /// Keeps the glass behind <paramref name="surface"/> (a panel's card) as it lays out and the window resizes, its
    /// corners those of the card.
    /// </summary>
    public static NativeGlass? Behind(Window window, Border surface)
    {
        if (For(window) is not { } glass)
            return null;
        void Place() => glass.Later(() =>
        {
            if (!surface.IsVisible || surface.TranslatePoint(new Point(0, 0), window) is not { } at)
            {
                glass.Hide();
                return;
            }
            glass.Show(new Rect(at, surface.Bounds.Size), surface.CornerRadius.TopLeft);
        });
        window.Opened += (_, _) => Place();
        surface.LayoutUpdated += (_, _) => Place();
        Place();
        return glass;
    }
}
