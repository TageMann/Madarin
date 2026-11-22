// Regression: after a conversion, a plain drag (no ⌘) passing over the spot where the wheel was last used opened the
// wheel there. The window was still up (invisible but for the ring) and took the drag. It must refuse every drag once
// something was dropped, while the ring shows, and after the gesture closed.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Filee.App.Controls;
using Filee.App.Services;
using Filee.App.ViewModels;
using Filee.App.Views;

namespace Filee.App.Tests;

[Collection("Motion")]
public class StrayWheelTests
{
    private static string Picture()
    {
        var folder = Directory.CreateTempSubdirectory("mandarin-stray-").FullName;
        var path = Path.Combine(folder, "tangerine tree.png");
        using (var image = new ImageMagick.MagickImage(ImageMagick.MagickColors.Orange, 40, 40))
            image.Write(path, ImageMagick.MagickFormat.Png);
        return path;
    }

    private static DataTransfer Files(RadialWindow window, string file)
    {
        var data = new DataTransfer();
        data.Add(DataTransferItem.CreateFile(window.StorageProvider.TryGetFileFromPathAsync(file).GetAwaiter().GetResult()!));
        return data;
    }

    private static void Enter(RadialWindow window, DataTransfer data) =>
        window.DragDrop(new Point(window.Bounds.Width / 2 + 60, window.Bounds.Height / 2), RawDragEventType.DragEnter, data,
            DragDropEffects.Copy, RawInputModifiers.None);

    [AvaloniaFact]
    public void A_drag_over_the_window_while_the_ring_shows_does_not_open_the_wheel()
    {
        TestServices.EnsureInitialized("en");
        var radial = AppHost.Get<RadialController>();
        var model = AppHost.Get<RadialViewModel>();
        var motion = Motion.Enabled;
        var file = Picture();
        try
        {
            Motion.Set(false);
            radial.ShowForDrag(400, 400);
            var window = radial.Window!;
            RenderTests.Pump();
            Enter(window, Files(window, file));
            Dispatcher.UIThread.RunJobs();
            Assert.True(model.HasFiles);

            // A conversion started: the ring shows in the window, which stays up.
            window.DonutControl.BeginProgress();
            Assert.False(radial.AcceptsDrops(window));

            // A plain drag of another file passes over that spot: refused, and the wheel keeps its ring.
            var other = Picture();
            Enter(window, Files(window, other));
            Dispatcher.UIThread.RunJobs();
            Assert.True(window.DonutControl.IsInProgressMode);
            Assert.DoesNotContain(other, model.Files);
        }
        finally
        {
            radial.Close(animate: false);
            radial.Window?.DonutControl.LeaveProgressMode();
            radial.Close(animate: false);
            Motion.Set(motion);
        }
    }

    [AvaloniaFact]
    public void A_window_left_up_after_its_gesture_closed_refuses_drags()
    {
        TestServices.EnsureInitialized("en");
        var radial = AppHost.Get<RadialController>();
        var model = AppHost.Get<RadialViewModel>();
        var motion = Motion.Enabled;
        var file = Picture();
        try
        {
            Motion.Set(false);
            radial.ShowForDrag(400, 400);
            var window = radial.Window!;
            RenderTests.Pump();
            radial.Close(animate: false);
            // Shown again by something other than a gesture (as a ring left behind would be): still no wheel.
            window.Show();
            RenderTests.Pump();
            Assert.False(radial.IsVisible);
            Enter(window, Files(window, file));
            Dispatcher.UIThread.RunJobs();
            Assert.False(model.HasFiles && model.Files.Contains(file));
            window.Hide();
        }
        finally
        {
            radial.Close(animate: false);
            Motion.Set(motion);
        }
    }

    [AvaloniaFact]
    public void A_new_gesture_takes_drags_again()
    {
        TestServices.EnsureInitialized("en");
        var radial = AppHost.Get<RadialController>();
        var model = AppHost.Get<RadialViewModel>();
        var motion = Motion.Enabled;
        var file = Picture();
        try
        {
            Motion.Set(false);
            radial.ShowForDrag(400, 400);
            radial.Close(animate: false);
            radial.ShowForDrag(420, 420);
            var window = radial.Window!;
            RenderTests.Pump();
            Assert.True(radial.AcceptsDrops(window));
            Enter(window, Files(window, file));
            Dispatcher.UIThread.RunJobs();
            Assert.Contains(file, model.Files);
        }
        finally
        {
            radial.Close(animate: false);
            Motion.Set(motion);
        }
    }
}
