// Mandarin: the wheel's glass progress ring on its own, for work started from a panel (Add BG's Save): a small
// transparent window that floats above everything, shows the job's real progress (or a turning arc until there is
// some), the green check when the file is ready, then closes itself. A failed job is shown in the toast instead, which
// explains what went wrong.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Filee.App.Controls;
using Filee.App.ViewModels;
using Filee.Core.Conversion;

namespace Filee.App.Views;

public sealed class ProgressRingWindow : Window
{
    /// <summary>The window's side, in its own units: the ring and its shadow with room to spare.</summary>
    public const double Side = 168;

    private readonly DonutMenu _ring;
    private JobViewModel? _job;
    private Action<JobViewModel>? _toToast;

    public ProgressRingWindow()
    {
        Width = Side;
        Height = Side;
        CanResize = false;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        WindowDecorations = WindowDecorations.None;
        Background = Brushes.Transparent;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        Title = "Mandarin";
        _ring = new DonutMenu { GlassStyle = true, Items = [] };
        _ring.Bind(DonutMenu.AccentBrushProperty, _ring.GetResourceObservable("AccentBrush"));
        _ring.Bind(DonutMenu.DangerBrushProperty, _ring.GetResourceObservable("DangerBrush"));
        _ring.ProgressCancelRequested += (_, _) => _job?.CancelCommand.Execute(null);
        _ring.ProgressClicked += (_, _) =>
        {
            if (_job is { HasOutputs: true } job)
                job.OpenFolderCommand.Execute(null);
        };
        Content = _ring;
    }

    internal DonutMenu Ring => _ring;

    /// <summary>The job shown, or null once it was handed on or finished.</summary>
    internal JobViewModel? Job => _job;

    /// <summary>
    /// Shows <paramref name="job"/>'s progress. <paramref name="toToast"/> takes the job over (into the toast) when it
    /// fails, or when <see cref="HandOff"/> is called because another job needs the ring.
    /// </summary>
    public void Track(JobViewModel job, Action<JobViewModel> toToast)
    {
        _job = job;
        _toToast = toToast;
        _ring.BeginProgress();
        _ring.SetProgress(job.Progress / 100);
        job.PropertyChanged += OnJobChanged;
        if (!job.IsRunning)
            Finish(job);
    }

    /// <summary>Another job needs the ring: this one goes on in the toast, and the ring closes at once.</summary>
    public void HandOff()
    {
        if (Detach() is { } job)
            _toToast?.Invoke(job);
        Close();
    }

    private void OnJobChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not JobViewModel job || job != _job)
            return;
        _ring.SetProgress(job.Progress / 100);
        if (e.PropertyName == nameof(JobViewModel.IsRunning) && !job.IsRunning)
            Finish(job);
    }

    private void Finish(JobViewModel job)
    {
        Detach();
        var outcome = job.Job.State switch
        {
            JobState.Completed => DonutProgressOutcome.Succeeded,
            JobState.Cancelled => DonutProgressOutcome.Cancelled,
            _ => DonutProgressOutcome.Failed,
        };
        if (outcome == DonutProgressOutcome.Failed)
            _toToast?.Invoke(job);
        _ring.CompleteProgress(outcome, Close);
    }

    private JobViewModel? Detach()
    {
        var job = _job;
        if (job is not null)
            job.PropertyChanged -= OnJobChanged;
        _job = null;
        return job;
    }

    /// <summary>Centres the ring on <paramref name="over"/> (a panel), else on the primary screen.</summary>
    public void PlaceOver(Window? over)
    {
        var screen = over is { IsVisible: true } ? over.Screens.ScreenFromWindow(over) : Screens.Primary;
        var scaling = screen?.Scaling ?? 1;
        var side = (int)Math.Round(Side * scaling);
        PixelRect area = over is { IsVisible: true } panel
            ? new PixelRect(panel.Position, PixelSize.FromSize(panel.ClientSize, scaling))
            : screen?.WorkingArea ?? new PixelRect(0, 0, side, side);
        Position = new PixelPoint(area.X + (area.Width - side) / 2, area.Y + (area.Height - side) / 2);
    }
}
