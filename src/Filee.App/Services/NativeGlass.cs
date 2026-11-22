// Mandarin, macOS: puts the system's own glass behind a shape of a window — the wheel's disc, the progress ring, a
// floating panel's card. On macOS 26 and later that is Apple's Liquid Glass (NSGlassEffectView); before, the frosted
// HUD material (NSVisualEffectView). Mandarin's drawn glass sits on top of it, lighter, so the real blur and refraction
// of what is behind the window come through. Off macOS, with Settings > General > "Use macOS glass" off, or when the
// native view can't be added, nothing changes: the drawn glass stands alone.
//
// A window that has it carries the class "native-glass", so its styles can thin the drawn surface over it.

using Avalonia;
using Avalonia.Controls;
using Filee.Core.Settings;
using Filee.Platform.MacOS;

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

    /// <summary>Settings > General > "Use macOS glass" (on unless the user turned it off).</summary>
    public static bool Wanted => OperatingSystem.IsMacOS() && AppHost.TryGet<UserDataStore>()?.Settings.NativeGlass != false;

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

    public void Hide() => _backdrop?.Hide();

    /// <summary>
    /// Keeps the glass under the disc <paramref name="donut"/> reports (the wheel's plate, the progress ring), and tells
    /// it whether the glass is there so it draws its own surfaces lighter.
    /// </summary>
    public void UnderDisc(Controls.DonutMenu donut)
    {
        donut.GlassDiscChanged += (center, radius) =>
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
        };
    }

    /// <summary>
    /// Keeps the glass behind <paramref name="surface"/> (a panel's card) as it lays out and the window resizes, its
    /// corners those of the card.
    /// </summary>
    public static NativeGlass? Behind(Window window, Border surface)
    {
        if (For(window) is not { } glass)
            return null;
        void Place()
        {
            if (!surface.IsVisible || surface.TranslatePoint(new Point(0, 0), window) is not { } at)
            {
                glass.Hide();
                return;
            }
            glass.Show(new Rect(at, surface.Bounds.Size), surface.CornerRadius.TopLeft);
        }
        window.Opened += (_, _) => Place();
        surface.LayoutUpdated += (_, _) => Place();
        Place();
        return glass;
    }
}
