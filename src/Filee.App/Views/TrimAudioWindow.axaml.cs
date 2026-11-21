using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Filee.App.Services;
using Filee.App.ViewModels;

namespace Filee.App.Views;

/// <summary>
/// Mandarin's "Trim Audio" panel (see TrimAudioWindow.axaml): the timeline's handle drags go to the clip model; Space
/// plays the chosen range, Enter applies, Esc stops playing or closes; the title bar moves the panel.
/// </summary>
public partial class TrimAudioWindow : Window
{
    private ClipWindowViewModel? _vm;

    public TrimAudioWindow()
    {
        InitializeComponent();
        Motion.Track(this);
        UiZoom.Track(this);
        DataContextChanged += (_, _) =>
        {
            if (DataContext is not ClipWindowViewModel vm || ReferenceEquals(vm, _vm))
                return;
            _vm = vm;
            vm.CloseRequested += Close;
            Timeline.HandleMoved += vm.MoveHandle;
            Timeline.PlayheadMoved += vm.MovePlayhead;
            Timeline.ClipMoved += vm.MoveClip;
            Closed += (_, _) => vm.Dispose();
        };
        TitleBar.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed
                && (e.Source as Visual)?.FindAncestorOfType<Button>(includeSelf: true) is null)
                BeginMoveDrag(e);
        };
        AddHandler(KeyDownEvent, OnKeyDownFirst, RoutingStrategies.Tunnel);
    }

    internal Controls.ClipTimeline ClipTimeline => Timeline;

    private void OnKeyDownFirst(object? sender, KeyEventArgs e)
    {
        if (_vm is not { } vm || e.KeyModifiers != KeyModifiers.None)
            return;
        switch (e.Key)
        {
            case Key.Space when e.Source is not Button and not CheckBox:
                vm.PlaySelectionCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Enter when e.Source is not Button:
                vm.SaveCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Escape:
                if (vm.IsPlaying)
                    vm.StopPlaying();
                else
                    vm.CancelCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }
}
