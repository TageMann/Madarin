using System.Runtime.InteropServices;

namespace Filee.Platform.MacOS;

/// <summary>macOS accessibility display settings the glass looks follow.</summary>
public static class MacOSAccessibility
{
    private const string ObjC = "/usr/lib/libobjc.A.dylib";

    /// <summary>
    /// System Settings > Accessibility > Display > Reduce transparency: glass is drawn solid. False off macOS or when
    /// it can't be read.
    /// </summary>
    public static bool ReduceTransparency()
    {
        if (!OperatingSystem.IsMacOS())
            return false;
        try
        {
            var workspace = Send(objc_getClass("NSWorkspace"), sel_registerName("sharedWorkspace"));
            return workspace != 0 && SendBool(workspace, sel_registerName("accessibilityDisplayShouldReduceTransparency")) != 0;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            return false;
        }
    }

    [DllImport(ObjC)]
    private static extern nint objc_getClass([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjC)]
    private static extern nint sel_registerName([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern nint Send(nint receiver, nint selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern byte SendBool(nint receiver, nint selector);
}
