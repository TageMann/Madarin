// Mandarin's look, rendered headlessly for review (bin/.../screenshots/mandarin-*.png): the glass wheel with a lit
// slice and its size pill, its converter face, the Add BG panel with a built-in picture behind on 9:16, and Edit Photo
// with its light adjusted, in light and dark.

using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Filee.App.Services;
using Filee.App.ViewModels;
using Filee.App.Views;
using Filee.Core.Localization;
using Filee.Core.Presets;
using Filee.Core.Settings;
using ImageMagick;

namespace Filee.App.Tests;

public class MandarinLookTests
{
    private static string Photo(string folder, string name, uint width = 1200, uint height = 800)
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, name);
        using var image = new MagickImage("gradient:#FFB347-#2F80ED", width, height);
        new ImageMagick.Drawing.Drawables().FillColor(new MagickColor("#FF7A1E"))
            .Circle(width * 0.35, height * 0.45, width * 0.35 + height * 0.2, height * 0.45).Draw(image);
        image.Write(path, MagickFormat.Png);
        return path;
    }

    private static void Wait(Func<bool> done, int seconds = 8)
    {
        for (var i = 0; i < seconds * 200 && !done(); i++)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_glass_wheel_shows_its_lit_slice_and_only_the_size(bool dark)
    {
        TestServices.EnsureInitialized("en");
        var theme = AppHost.Get<ThemeService>();
        var store = AppHost.Get<UserDataStore>();
        var folder = Path.Combine(Path.GetTempPath(), "mandarin-look-" + Guid.NewGuid().ToString("N"));
        var images = store.Profiles.First(p => p.Id == "images");
        var (front, back) = (images.PresetIds, images.BackPresetIds);
        var suffix = dark ? "dark" : "light";
        try
        {
            images.PresetIds = [.. UserDataStore.MandarinImageActions];
            images.BackPresetIds = [.. UserDataStore.MandarinImageFormats];
            theme.Apply(new ThemeSettings { Mode = dark ? ThemeMode.Dark : ThemeMode.Light }, "en");
            var vm = AppHost.Get<RadialViewModel>();
            vm.Load([Photo(folder, "tangerine tree.png")], clickMode: true);
            vm.FitToThisDonut();
            Assert.False(string.IsNullOrEmpty(vm.CenterPill));
            Assert.EndsWith("B", vm.CenterPill);
            var window = new RadialWindow { DataContext = vm };
            window.Show();
            var donut = window.DonutControl;
            Assert.True(donut.GlassStyle);
            donut.SetExternalHighlight(2);
            Wait(() => false, 1);
            RenderTests.Save(window, $"mandarin-wheel-{suffix}.png");

            donut.Flip();
            Wait(() => !donut.IsFlipping, 2);
            Wait(() => false, 1);
            donut.SetExternalHighlight(1);
            Wait(() => false, 1);
            RenderTests.Save(window, $"mandarin-converter-{suffix}.png");
            window.Close();
        }
        finally
        {
            (images.PresetIds, images.BackPresetIds) = (front, back);
            theme.Apply(new ThemeSettings(), "en");
            try { Directory.Delete(folder, true); } catch (IOException) { }
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Add_bg_puts_a_picture_on_a_built_in_background_for_a_story(bool dark)
    {
        TestServices.EnsureInitialized("en");
        var theme = AppHost.Get<ThemeService>();
        var folder = Path.Combine(Path.GetTempPath(), "mandarin-look-" + Guid.NewGuid().ToString("N"));
        var suffix = dark ? "dark" : "light";
        try
        {
            theme.Apply(new ThemeSettings { Mode = dark ? ThemeMode.Dark : ThemeMode.Light }, "en");
            var store = AppHost.Get<UserDataStore>();
            var tool = (store.FindPreset(BuiltInData.BackgroundId) ?? BuiltInData.BackgroundPresets().Single()).Clone();
            tool.Image.BackdropLook = null;
            var vm = new BackgroundViewModel([Photo(folder, "screenshot.png", 1000, 640)], tool, AppHost.Get<ILocalizer>(), store, (_, _) => { });
            var window = new BackgroundWindow { DataContext = vm, Width = 800, Height = 900 };
            window.Show();
            Wait(() => vm.IsPreviewCurrent && !vm.IsBusy);
            // Mandarin's own pictures ship next to the program.
            Assert.NotEmpty(vm.Pictures);
            vm.Pictures[2].ChooseCommand.Execute(null);
            vm.SetCanvasCommand.Execute(BackdropCanvas.Story);
            vm.PictureScale = 80;
            vm.OffsetY = -40;
            Wait(() => vm.IsPreviewCurrent && !vm.IsBusy);
            Wait(() => false, 1);
            Assert.Equal(BackdropKind.Picture, vm.Look.Kind);
            Assert.Equal(BackdropCanvas.Story, vm.Look.Canvas);
            RenderTests.Save(window, $"mandarin-addbg-{suffix}.png");
            window.Close();
        }
        finally
        {
            theme.Apply(new ThemeSettings(), "en");
            try { Directory.Delete(folder, true); } catch (IOException) { }
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Edit_photo_previews_its_light(bool dark)
    {
        TestServices.EnsureInitialized("en");
        var theme = AppHost.Get<ThemeService>();
        var folder = Path.Combine(Path.GetTempPath(), "mandarin-look-" + Guid.NewGuid().ToString("N"));
        var suffix = dark ? "dark" : "light";
        try
        {
            theme.Apply(new ThemeSettings { Mode = dark ? ThemeMode.Dark : ThemeMode.Light }, "en");
            var tool = BuiltInData.ImageEditPresets().Single();
            var starts = new List<(IReadOnlyList<string> Paths, Preset Preset)>();
            var vm = new ImageEditViewModel([Photo(folder, "flowers.png", 800, 1200)], tool, AppHost.Get<ILocalizer>(),
                AppHost.Get<UserDataStore>(), (_, chosen) => chosen, (paths, preset) => starts.Add((paths, preset)), WindowService.PictureSize);
            var window = new ImageEditWindow { DataContext = vm };
            window.Show();
            Wait(() => vm.PreviewImage is not null);
            vm.Exposure = 0.5;
            vm.Contrast = 20;
            vm.Shadows = 15;
            Wait(() => false, 1);
            Assert.NotNull(vm.PreviewImage);
            Assert.Equal("+0.50 EV", vm.ExposureText);
            RenderTests.Save(window, $"mandarin-edit-photo-{suffix}.png");

            // Convert carries the light into the conversion.
            vm.Convert();
            var conversion = Assert.Single(starts).Preset;
            Assert.Equal(0.5, conversion.Image.Exposure);
            Assert.Equal(20, conversion.Image.Contrast);
            window.Close();
        }
        finally
        {
            theme.Apply(new ThemeSettings(), "en");
            try { Directory.Delete(folder, true); } catch (IOException) { }
        }
    }
}
