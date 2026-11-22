// Mandarin's look, rendered headlessly for review (bin/.../screenshots/mandarin-*.png): the glass wheel with a lit
// slice and its size pill, its converter face, the Add BG panel with a built-in picture behind on 9:16, and Edit Photo
// with its light adjusted, in light and dark.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Filee.App.Services;
using Filee.App.ViewModels;
using Filee.App.Views;
using Filee.Core.Localization;
using Filee.Core.Presets;
using Filee.Core.Settings;
using Filee.App.Controls;
using Filee.Engines.Media;
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
            // A wallpaper behind the window, as the desktop is behind the wheel on a Mac (the screenshot has no blur).
            var window = new RadialWindow { DataContext = vm, Background = Wallpaper(dark) };
            window.Show();
            var donut = window.DonutControl;
            Assert.True(donut.GlassStyle);
            donut.SetExternalHighlight(2);
            Wait(() => false, 1);
            RenderTests.Save(window, $"mandarin-wheel-{suffix}.png");

            donut.Flip();
            donut.Advance(0.045);
            RenderTests.Save(window, $"mandarin-flip-a-{suffix}.png");
            donut.Advance(0.03);
            RenderTests.Save(window, $"mandarin-flip-b-{suffix}.png");
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
            var window = new BackgroundWindow { DataContext = vm, Width = 1180, Height = 820, Background = Wallpaper(dark) };
            window.Show();
            // At its usual size the controls are on the left and the picture fills the right.
            Assert.True(window.IsWideLayout);
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
            var window = new ImageEditWindow { DataContext = vm, Background = Wallpaper(dark) };
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

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_progress_ring_is_glass_and_finishes_with_a_green_check(bool dark)
    {
        TestServices.EnsureInitialized("en");
        var theme = AppHost.Get<ThemeService>();
        var suffix = dark ? "dark" : "light";
        try
        {
            theme.Apply(new ThemeSettings { Mode = dark ? ThemeMode.Dark : ThemeMode.Light }, "en");
            var donut = new DonutMenu
            {
                GlassStyle = true,
                CenterPill = "2.2 MB",
                Items = new[] { "JPG", "WEBP", "HEIC", "TIFF", "AVIF", "BMP", "PDF", "DOCX" }.Select(l => new DonutItem { Id = l, Label = l }).ToList(),
            };
            donut.Bind(DonutMenu.AccentBrushProperty, donut.GetResourceObservable("AccentBrush"));
            var window = new Avalonia.Controls.Window
            {
                Content = donut, Width = 460, Height = 460,
                Background = Avalonia.Media.Brush.Parse(dark ? "#3A2A3A" : "#F2B58A"),
            };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            void Run(double seconds)
            {
                for (var t = 0.0; t < seconds; t += 1 / 60.0)
                    donut.Advance(1 / 60.0);
                Dispatcher.UIThread.RunJobs();
            }

            donut.BeginProgress();
            Run(0.9);
            // Nothing known yet: an arc goes round, no percentage is made up.
            Assert.True(donut.IsProgressIndeterminate);
            RenderTests.Save(window, $"mandarin-progress-waiting-{suffix}.png");
            donut.SetProgress(0.62);
            Run(1);
            Assert.False(donut.IsProgressIndeterminate);
            RenderTests.Save(window, $"mandarin-progress-{suffix}.png");
            var finished = false;
            donut.CompleteProgress(DonutProgressOutcome.Succeeded, () => finished = true);
            for (var t = 0.0; donut.ShownOutcome is null && t < 3; t += 1 / 60.0)
                donut.Advance(1 / 60.0);
            Run(0.4);
            Assert.Equal(DonutProgressOutcome.Succeeded, donut.ShownOutcome);
            RenderTests.Save(window, $"mandarin-progress-done-{suffix}.png");
            Run(3);
            Assert.True(finished);
            window.Close();
        }
        finally
        {
            theme.Apply(new ThemeSettings(), "en");
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Trim_audio_shows_the_waveform_between_two_handles(bool dark)
    {
        TestServices.EnsureInitialized("en");
        var theme = AppHost.Get<ThemeService>();
        var suffix = dark ? "dark" : "light";
        var folder = Path.Combine(Path.GetTempPath(), "mandarin-look-" + Guid.NewGuid().ToString("N"));
        try
        {
            theme.Apply(new ThemeSettings { Mode = dark ? ThemeMode.Dark : ThemeMode.Light }, "en");
            Directory.CreateDirectory(folder);
            var memo = Path.Combine(folder, "voice memo.m4a");
            File.WriteAllBytes(memo, new byte[64]);
            var exports = new List<(string Path, Preset Preset)>();
            var vm = new ClipWindowViewModel([memo], BuiltInData.TrimAudioPreset(), AppHost.Get<ILocalizer>(),
                AppHost.Get<UserDataStore>(), new Sound(41.74), _ => "m4a", (path, preset) => exports.Add((path, preset)));
            var window = new TrimAudioWindow { DataContext = vm, Background = Wallpaper(dark) };
            window.Show();
            Wait(() => vm.Loudness.Count > 0 && vm.Duration > 0);
            Assert.True(vm.IsSoundOnly);
            Assert.Equal(41.74, vm.End, 2);

            vm.TrimSilentEnds = true;
            Wait(() => false, 1);
            Assert.True(vm.Start > 0);
            Assert.True(vm.End < vm.Duration);
            Assert.Contains("selected", vm.SelectedText);
            RenderTests.Save(window, $"mandarin-trim-audio-{suffix}.png");
            window.Close();
        }
        finally
        {
            theme.Apply(new ThemeSettings(), "en");
            try { Directory.Delete(folder, true); } catch (IOException) { }
        }
    }

    /// <summary>A sound of <paramref name="duration"/> seconds with a second of silence at either end.</summary>
    private sealed class Sound(double duration) : IVideoFrameSource
    {
        public Task<VideoSummary> ProbeAsync(string path, CancellationToken cancellationToken) =>
            Task.FromResult(new VideoSummary(duration, 0, 0, 0));

        public Task<bool> WriteFrameAsync(string path, double seconds, string output, int maxHeight, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<float[]> ReadLoudnessAsync(string path, double start, double end, int count, string workFolder, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Range(0, count).Select(i =>
            {
                var t = start + (end - start) * i / count;
                if (t < 1 || t > duration - 1)
                    return 0f;
                return (float)Math.Clamp(0.45 + 0.35 * Math.Sin(i / 2.0) * Math.Sin(i / 23.0), 0.08, 1);
            }).ToArray());

        public Task<double> ReadSoundLengthAsync(string path, CancellationToken cancellationToken) => Task.FromResult(duration);

        public Task<IReadOnlyList<ClipRange>> FindSilencesAsync(string path, double start, double end, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ClipRange>>([]);

        public Task PlayAsync(string path, ClipEdit clip, int height, Action<byte[], int, int, double> onFrame, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    /// <summary>A desktop picture like the user's: warm orange to violet (light), deep plum to navy (dark).</summary>
    internal static Avalonia.Media.IBrush Wallpaper(bool dark) => new Avalonia.Media.LinearGradientBrush
    {
        StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
        EndPoint = new Avalonia.RelativePoint(1, 1, Avalonia.RelativeUnit.Relative),
        GradientStops =
        {
            new Avalonia.Media.GradientStop(Avalonia.Media.Color.Parse(dark ? "#3B1D2E" : "#F7A13A"), 0),
            new Avalonia.Media.GradientStop(Avalonia.Media.Color.Parse(dark ? "#2A1F3D" : "#F06A4D"), 0.5),
            new Avalonia.Media.GradientStop(Avalonia.Media.Color.Parse(dark ? "#141A2E" : "#A98BD0"), 1),
        },
    };
}
