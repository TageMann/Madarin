// Lets a drag held with ⌘ drop onto Mandarin's windows.
//
// Why it fails without this: while ⌘ is held, AppKit narrows the drag source's operation mask to
// NSDragOperationGeneric (Apple: Control → Link, Option → Copy, Command → Generic). Avalonia's AvnView only maps Copy,
// Move and Link to managed drag effects, so under ⌘ every window answered "no drop": the wheel lit up, but the release
// slid the file back to Finder and the action never ran until ⌘ was let go first.
//
// What this does: it wraps AvnView's four NSDraggingDestination methods. Each first calls Avalonia's own method, which
// raises the managed DragEnter / DragOver / Drop events as before. When Avalonia answers "none" only because the mask
// was Generic, and the managed window did want the drop (DropWanted, set by the app from the effect its handler chose),
// the wrapper answers Generic / YES instead. Nothing else changes: with ⌘ released Avalonia answers Copy itself and the
// wrapper passes that through. Generic tells Finder nothing was moved; Mandarin only reads the paths and writes new
// files, so the source stays where it is.

using System.Runtime.InteropServices;

namespace Filee.Platform.MacOS;

/// <summary>Accepts ⌘-held drags on Avalonia's macOS view (see the file header).</summary>
public static unsafe class MacOSCommandDrop
{
    private const string ObjC = "/usr/lib/libobjc.A.dylib";
    internal const nuint DragNone = 0;
    internal const nuint DragGeneric = 4;

    private static delegate* unmanaged<nint, nint, nint, nuint> _entered;
    private static delegate* unmanaged<nint, nint, nint, nuint> _updated;
    private static delegate* unmanaged<nint, nint, nint, byte> _prepare;
    private static delegate* unmanaged<nint, nint, nint, byte> _perform;
    private static nint _maskSelector;

    /// <summary>
    /// Whether the last managed drag event (enter, over or drop) asked for the drop. The app sets it from a class
    /// handler that sees the effect a window's own handler chose; it is cleared before each event.
    /// </summary>
    public static volatile bool DropWanted;

    /// <summary>Whether the wrappers are in place (false off macOS, or when AvnView was not found).</summary>
    public static bool Installed { get; private set; }

    /// <summary>Installs the wrappers once. Safe to call more than once; does nothing off macOS.</summary>
    public static bool Install()
    {
        if (Installed || !OperatingSystem.IsMacOS())
            return Installed;
        try
        {
            var view = objc_getClass("AvnView");
            if (view == 0)
                return false;
            _maskSelector = sel_registerName("draggingSourceOperationMask");
            _entered = (delegate* unmanaged<nint, nint, nint, nuint>)Swap(view, "draggingEntered:", (nint)(delegate* unmanaged<nint, nint, nint, nuint>)&Entered);
            _updated = (delegate* unmanaged<nint, nint, nint, nuint>)Swap(view, "draggingUpdated:", (nint)(delegate* unmanaged<nint, nint, nint, nuint>)&Updated);
            _prepare = (delegate* unmanaged<nint, nint, nint, byte>)Swap(view, "prepareForDragOperation:", (nint)(delegate* unmanaged<nint, nint, nint, byte>)&Prepare);
            _perform = (delegate* unmanaged<nint, nint, nint, byte>)Swap(view, "performDragOperation:", (nint)(delegate* unmanaged<nint, nint, nint, byte>)&Perform);
            Installed = _entered != null && _updated != null && _prepare != null && _perform != null;
            return Installed;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            return false;
        }
    }

    /// <summary>Puts <paramref name="replacement"/> in place of a method and returns the original, or 0.</summary>
    private static nint Swap(nint cls, string selector, nint replacement)
    {
        var method = class_getInstanceMethod(cls, sel_registerName(selector));
        return method == 0 ? 0 : method_setImplementation(method, replacement);
    }

    /// <summary>
    /// What to answer after Avalonia answered <paramref name="avalonia"/>: its answer when it took the drop; Generic
    /// when it refused only because the source offered Generic alone and the window wanted the drop.
    /// </summary>
    internal static nuint Decide(nuint avalonia, nuint sourceMask, bool wanted) =>
        avalonia != DragNone || !wanted || (sourceMask & DragGeneric) == 0 ? avalonia : DragGeneric;

    private static nuint Mask(nint info) => info == 0 || _maskSelector == 0 ? 0 : SendMask(info, _maskSelector);

    [UnmanagedCallersOnly]
    private static nuint Entered(nint self, nint cmd, nint info)
    {
        DropWanted = false;
        return Decide(_entered(self, cmd, info), Mask(info), DropWanted);
    }

    [UnmanagedCallersOnly]
    private static nuint Updated(nint self, nint cmd, nint info)
    {
        DropWanted = false;
        return Decide(_updated(self, cmd, info), Mask(info), DropWanted);
    }

    [UnmanagedCallersOnly]
    private static byte Prepare(nint self, nint cmd, nint info)
    {
        DropWanted = false;
        var taken = _prepare(self, cmd, info) != 0;
        return Decide(taken ? 1u : DragNone, Mask(info), DropWanted) != DragNone ? (byte)1 : (byte)0;
    }

    [UnmanagedCallersOnly]
    private static byte Perform(nint self, nint cmd, nint info)
    {
        DropWanted = false;
        var taken = _perform(self, cmd, info) != 0;
        return Decide(taken ? 1u : DragNone, Mask(info), DropWanted) != DragNone ? (byte)1 : (byte)0;
    }

    [DllImport(ObjC)]
    private static extern nint objc_getClass([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjC)]
    private static extern nint sel_registerName([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjC)]
    private static extern nint class_getInstanceMethod(nint cls, nint selector);

    [DllImport(ObjC)]
    private static extern nint method_setImplementation(nint method, nint implementation);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern nuint SendMask(nint receiver, nint selector);
}
