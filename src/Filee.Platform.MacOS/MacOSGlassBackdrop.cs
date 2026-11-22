// Native glass behind a shape of an Avalonia window (the wheel's disc, a panel's rounded card).
//
// macOS 26 (Tahoe) and later: an NSGlassEffectView — Apple's own Liquid Glass, refracting and blurring what is behind
// the window. Earlier macOS: an NSVisualEffectView (the HUD material, blending behind the window) clipped to the same
// rounded shape: the system's frosted material, not Liquid Glass.
//
// Avalonia draws the whole window into its content view (AvnView), so the native view is placed in the window's frame
// view just below it; the window is transparent, so it shows through wherever Avalonia leaves the window clear or
// translucent. The owner reports the shape in window points (top-left origin) as it moves and animates; Hide takes it
// away (glass is never faded with alpha: Apple notes that an opacity below 1 collapses the refraction).
//
// Every selector is checked with respondsToSelector: first; anything missing leaves the window as it was (Avalonia's own
// drawing then stands alone).

using System.Runtime.InteropServices;

namespace Filee.Platform.MacOS;

/// <summary>A native glass (or, before macOS 26, frosted) view behind a rounded shape of an Avalonia window.</summary>
public sealed class MacOSGlassBackdrop
{
    private const string ObjC = "/usr/lib/libobjc.A.dylib";
    private const nint Below = -1; // NSWindowBelow

    private nint _view;
    private bool _shown;
    private (double X, double Y, double W, double H, double R) _last = (double.NaN, 0, 0, 0, 0);

    private MacOSGlassBackdrop(nint view, bool liquidGlass)
    {
        _view = view;
        IsLiquidGlass = liquidGlass;
    }

    /// <summary>True for Apple's Liquid Glass (macOS 26+), false for the older frosted material.</summary>
    public bool IsLiquidGlass { get; }

    /// <summary>Whether macOS has Liquid Glass (NSGlassEffectView): macOS 26 or later.</summary>
    public static bool LiquidGlassAvailable => OperatingSystem.IsMacOS() && Class("NSGlassEffectView") != 0;

    /// <summary>
    /// Puts a hidden native glass view in <paramref name="nsWindow"/> (an NSWindow handle), or returns null when that
    /// can't be done (not macOS, no frame view, a selector missing).
    /// </summary>
    public static MacOSGlassBackdrop? Attach(nint nsWindow)
    {
        if (!OperatingSystem.IsMacOS() || nsWindow == 0)
            return null;
        try
        {
            var content = Send(nsWindow, Sel("contentView"));
            var frame = content == 0 ? 0 : Send(content, Sel("superview"));
            if (frame == 0 || !Responds(frame, "addSubview:positioned:relativeTo:"))
                return null;

            var liquid = Class("NSGlassEffectView");
            var cls = liquid != 0 ? liquid : Class("NSVisualEffectView");
            if (cls == 0)
                return null;
            var view = Send(Send(cls, Sel("alloc")), Sel("initWithFrame:"), new CGRect(0, 0, 1, 1));
            if (view == 0)
                return null;
            if (liquid != 0)
            {
                // The regular variant: legible over any wallpaper (the clear one is for media-rich backgrounds).
                if (Responds(view, "setStyle:"))
                    SendInt(view, Sel("setStyle:"), 0);
            }
            else
            {
                // NSVisualEffectMaterialHUDWindow (13), behind the window (0), always active (1).
                if (Responds(view, "setMaterial:"))
                    SendInt(view, Sel("setMaterial:"), 13);
                if (Responds(view, "setBlendingMode:"))
                    SendInt(view, Sel("setBlendingMode:"), 0);
                if (Responds(view, "setState:"))
                    SendInt(view, Sel("setState:"), 1);
                SendBool(view, Sel("setWantsLayer:"), true);
            }
            SendBool(view, Sel("setHidden:"), true);
            SendVoid(frame, Sel("addSubview:positioned:relativeTo:"), view, Below, content);
            Send(view, Sel("release")); // the frame view holds it now
            return new MacOSGlassBackdrop(view, liquid != 0);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or MarshalDirectiveException)
        {
            return null;
        }
    }

    /// <summary>
    /// Shows the glass over the rectangle <paramref name="x"/>, <paramref name="y"/>, <paramref name="width"/>,
    /// <paramref name="height"/> of the window's content (points, top-left origin), its corners rounded by
    /// <paramref name="radius"/> (half the shorter side for a disc or a capsule). <paramref name="contentHeight"/> is
    /// the height of the window's content in points (AppKit's y runs up from the bottom).
    /// </summary>
    public void Show(double x, double y, double width, double height, double radius, double contentHeight)
    {
        if (_view == 0)
            return;
        if (width < 2 || height < 2)
        {
            Hide();
            return;
        }
        var shape = (x, y, width, height, radius);
        if (_shown && shape == _last)
            return;
        // The frame view and the content view share the window's coordinates; AppKit's y runs up from the bottom.
        var rect = new CGRect(x, contentHeight - y - height, width, height);
        SendRect(_view, Sel("setFrame:"), rect);
        radius = Math.Min(radius, Math.Min(width, height) / 2);
        if (Responds(_view, "setCornerRadius:"))
            SendDouble(_view, Sel("setCornerRadius:"), radius);
        else
        {
            var layer = Send(_view, Sel("layer"));
            if (layer != 0)
            {
                SendDouble(layer, Sel("setCornerRadius:"), radius);
                SendBool(layer, Sel("setMasksToBounds:"), true);
            }
        }
        if (!_shown)
            SendBool(_view, Sel("setHidden:"), false);
        _shown = true;
        _last = shape;
    }

    /// <summary>Takes the glass away (it stays in the window, hidden, for the next showing).</summary>
    public void Hide()
    {
        if (_view == 0 || !_shown)
            return;
        SendBool(_view, Sel("setHidden:"), true);
        _shown = false;
    }

    /// <summary>Removes the view from the window for good (before the window closes).</summary>
    public void Detach()
    {
        if (_view == 0)
            return;
        SendVoid(_view, Sel("removeFromSuperview"));
        _view = 0;
        _shown = false;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct CGRect(double X, double Y, double Width, double Height);

    private static nint Class(string name) => objc_getClass(name);

    private static nint Sel(string name) => sel_registerName(name);

    private static bool Responds(nint target, string selector) =>
        target != 0 && SendBoolArg(target, Sel("respondsToSelector:"), Sel(selector));

    [DllImport(ObjC)]
    private static extern nint objc_getClass([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjC)]
    private static extern nint sel_registerName([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern nint Send(nint receiver, nint selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern nint Send(nint receiver, nint selector, CGRect rect);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendRect(nint receiver, nint selector, CGRect rect);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendDouble(nint receiver, nint selector, double value);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendInt(nint receiver, nint selector, nint value);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendBool(nint receiver, nint selector, [MarshalAs(UnmanagedType.I1)] bool value);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool SendBoolArg(nint receiver, nint selector, nint argument);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendVoid(nint receiver, nint selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern void SendVoid(nint receiver, nint selector, nint view, nint place, nint relative);
}
