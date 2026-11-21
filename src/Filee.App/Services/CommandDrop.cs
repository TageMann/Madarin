// macOS: lets a file dragged with ⌘ still held drop onto the wheel (and every other drop target).
// MacOSCommandDrop wraps Avalonia's native drag methods; this tells it, after each managed drag event, whether the
// window under the pointer chose an effect (it saw the event last, after the window's own handlers).

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Filee.Platform.MacOS;
using Microsoft.Extensions.Logging;

namespace Filee.App.Services;

internal static class CommandDrop
{
    public static void Install(ILogger log)
    {
        foreach (var routed in new[] { DragDrop.DragEnterEvent, DragDrop.DragOverEvent, DragDrop.DropEvent })
            routed.AddClassHandler<TopLevel>(Note, RoutingStrategies.Bubble, handledEventsToo: true);
        Controls.DonutMenu.SolidGlass = MacOSAccessibility.ReduceTransparency();
        if (MacOSCommandDrop.Install())
            log.LogInformation("⌘-held drops accepted");
        else
            log.LogWarning("Could not wrap the native drag methods: release ⌘ before dropping");
    }

    private static void Note(TopLevel _, DragEventArgs e) =>
        MacOSCommandDrop.DropWanted = e.DragEffects != DragDropEffects.None;
}
