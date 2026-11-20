// Mandarin: the wheel as a piece of glass floating above Finder.
//
// With GlassStyle on (the live wheel and its previews), the donut is drawn as:
//   • a glass plate: a translucent ring with a polished rim, behind the slices;
//   • each slice a rounded glass button inset in the plate, a near-white peach tint with a light rim at the top;
//   • the slice under the pointer a warm orange gradient with a white label;
//   • a clear hole in the middle, so the file being dragged stays visible behind it, and in it only a small glass
//     pill with the files' size ("2.2 MB", CenterPill).
// The colour of the glass is near white in light mode and a warm smoke in dark mode, never the accent: as with
// liquid glass, the colour comes from what is behind it, and only the lit slice is orange. The slices are opaque
// enough that their labels read on any wallpaper.
//
// GlassStyle off draws the donut exactly as before (tests and the Theme page's small preview use it that way).

using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;

namespace Filee.App.Controls;

public sealed partial class DonutMenu
{
    /// <summary>Draws the donut as Mandarin's glass wheel (see the top of DonutMenu.Glass.cs).</summary>
    public static readonly StyledProperty<bool> GlassStyleProperty =
        AvaloniaProperty.Register<DonutMenu, bool>(nameof(GlassStyle));

    /// <summary>
    /// With <see cref="GlassStyle"/>: the only thing the hub shows, in a small glass pill ("2.2 MB"); null keeps the hub's
    /// usual title and hints.
    /// </summary>
    public static readonly StyledProperty<string?> CenterPillProperty =
        AvaloniaProperty.Register<DonutMenu, string?>(nameof(CenterPill));

    public bool GlassStyle { get => GetValue(GlassStyleProperty); set => SetValue(GlassStyleProperty, value); }
    public string? CenterPill { get => GetValue(CenterPillProperty); set => SetValue(CenterPillProperty, value); }

    /// <summary>How far the glass slices sit inside the plate's rim, in pixels at this size.</summary>
    internal double GlassInset => GlassStyle ? 7 * DonutScale : 0;

    /// <summary>The gap between slices: wider for glass, so each reads as a button of its own.</summary>
    private double SliceGap => GlassStyle ? 7 : GapPixels;

    private bool IsDarkGlass => ActualThemeVariant == ThemeVariant.Dark;

    // Immutable brushes: shared by every donut and free of any thread (a mutable brush belongs to the one that made it).
    private static IImmutableBrush Linear(params (uint Argb, double Offset)[] stops) =>
        new ImmutableLinearGradientBrush(
            [.. stops.Select(s => new ImmutableGradientStop(s.Offset, Color.FromUInt32(s.Argb)))],
            startPoint: new RelativePoint(0.15, 0, RelativeUnit.Relative),
            endPoint: new RelativePoint(0.85, 1, RelativeUnit.Relative));

    // ── The glass, light and dark ──
    private static readonly IImmutableBrush LightSlice = Linear((0xF5FFFAF5, 0), (0xEBFFE9DA, 0.55), (0xE6FFDCC6, 1));
    private static readonly IImmutableBrush DarkSlice = Linear((0xE84A3A31, 0), (0xE23A2C25, 0.55), (0xDE2C211C, 1));
    private static readonly IImmutableBrush LightPlate = Linear((0x70FFFFFF, 0), (0x4DFFF0E6, 0.5), (0x40FFE2D0, 1));
    private static readonly IImmutableBrush DarkPlate = Linear((0x5C3A2C25, 0), (0x4D2A201B, 1));
    private static readonly IImmutableBrush LightRim = Linear((0xF2FFFFFF, 0), (0x80FFFFFF, 0.45), (0x40FFFFFF, 1));
    private static readonly IImmutableBrush DarkRim = Linear((0x73FFFFFF, 0), (0x26FFFFFF, 0.5), (0x14FFFFFF, 1));
    private static readonly IImmutableBrush Lit = Linear((0xFFFFA24A, 0), (0xFFFF7A1E, 0.5), (0xFFE5480C, 1));
    private static readonly IImmutableBrush LitRim = Linear((0xB3FFFFFF, 0), (0x33FFFFFF, 0.5), (0x1AFFFFFF, 1));
    private static readonly IImmutableBrush LightPill = Linear((0xF2FFFFFF, 0), (0xE6FFF3EA, 1));
    private static readonly IImmutableBrush DarkPill = Linear((0xEB4A3A31, 0), (0xE62C211C, 1));
    private static readonly IImmutableBrush HoleTint = new ImmutableSolidColorBrush(Color.FromUInt32(0x14FFFFFF));
    private static readonly IImmutableSolidColorBrush Hairline = new ImmutableSolidColorBrush(Color.FromUInt32(0x1F000000));
    private static readonly IImmutableSolidColorBrush GlassLabel = new ImmutableSolidColorBrush(Color.FromUInt32(0xFF2B1B12));
    private static readonly IImmutableSolidColorBrush GlassLabelDark = new ImmutableSolidColorBrush(Color.FromUInt32(0xFFF6EDE6));

    /// <summary>The label of the lit slice: white on the orange glass, the theme's own otherwise.</summary>
    private IBrush? HotLabelBrush => GlassStyle ? Brushes.White : OnAccentBrush;

    /// <summary>The label of an ordinary slice: a warm near-black on light glass, warm white on dark.</summary>
    private IBrush? SliceLabelBrush => GlassStyle ? IsDarkGlass ? GlassLabelDark : GlassLabel : Foreground;

    /// <summary>
    /// The glass plate the slices sit in: a translucent ring from the hole to the outer edge with a polished rim,
    /// squeezed sideways with the wheel while it flips (<paramref name="width"/> 0 to 1).
    /// </summary>
    private void DrawGlassPlate(DrawingContext context, Point center, double r1, double r2, double width)
    {
        if (width <= 0.001)
            return;
        var opening = _open.Length == 0 ? 1 : Math.Clamp(_open.Average(), 0, 1);
        if (opening <= 0.001)
            return;
        var scale = 0.86 + 0.14 * opening;
        var lift = 1 + FlipLift * 0.04;
        using var transform = context.PushTransform(
            Matrix.CreateTranslation(-center.X, -center.Y) * Matrix.CreateScale(scale * width * lift, scale * lift) *
            Matrix.CreateTranslation(center.X, center.Y));
        using var fade = context.PushOpacity(opening);
        var ring = CreateRing(center, r1, r2);
        context.DrawGeometry(IsDarkGlass ? DarkPlate : LightPlate, null, ring);
        // A hairline outside the rim keeps the wheel's edge on a white window; the rim lights its top.
        context.DrawEllipse(null, new Pen(Hairline, 1), center, r2 + 0.5, r2 + 0.5);
        context.DrawEllipse(null, new Pen(IsDarkGlass ? DarkRim : LightRim, 1.4), center, r2 - 0.7, r2 - 0.7);
        context.DrawEllipse(null, new Pen(IsDarkGlass ? DarkRim : LightRim, 1.1), center, r1 + 0.6, r1 + 0.6);
    }

    /// <summary>
    /// A slice's surface: glass (lit orange by <paramref name="emphasis"/>) with <see cref="GlassStyle"/>, else the
    /// theme's flat slice with the accent over it, as before.
    /// </summary>
    private void DrawSliceSurface(DrawingContext context, Geometry wedge, bool enabled, double emphasis)
    {
        emphasis = enabled ? Math.Clamp(emphasis, 0, 1) : 0;
        if (!GlassStyle)
        {
            context.DrawGeometry(WithOpacity(SliceBrush, SliceOpacity * (enabled ? 1 : 0.55)), BorderPen(), wedge);
            if (emphasis > 0.01)
                context.DrawGeometry(WithOpacity(AccentBrush, 0.82 * emphasis), null, wedge);
            return;
        }

        using (context.PushOpacity(SliceOpacity * (enabled ? 1 : 0.5)))
            context.DrawGeometry(IsDarkGlass ? DarkSlice : LightSlice, null, wedge);
        if (emphasis > 0.01)
        {
            // A soft orange glow just outside the lit slice gives it depth, then the orange glass itself.
            using (context.PushOpacity(0.22 * emphasis))
                context.DrawGeometry(null, new Pen(Lit, 6, lineJoin: PenLineJoin.Round), wedge);
            using (context.PushOpacity(emphasis))
                context.DrawGeometry(Lit, null, wedge);
        }
        var rim = emphasis > 0.5 ? LitRim : IsDarkGlass ? DarkRim : LightRim;
        context.DrawGeometry(null, new Pen(rim, 1.1, lineJoin: PenLineJoin.Round), wedge);
    }

    /// <summary>
    /// The hub with <see cref="GlassStyle"/> and a <see cref="CenterPill"/>: clear, so the file being dragged shows
    /// through, with the size in a glass pill in its middle. False when the hub should be drawn as usual (no pill).
    /// </summary>
    private bool DrawGlassCenter(DrawingContext context, Point center, double r1)
    {
        if (!GlassStyle || string.IsNullOrEmpty(CenterPill))
            return false;
        var pulse = 1 + _centerPulse.Value * 0.06;
        var radius = (r1 - 2) * pulse;
        context.DrawEllipse(HoleTint, null, center, radius, radius);
        if (_externalCenterHot)
            context.DrawEllipse(WithOpacity(DangerBrush, 0.22), null, center, radius, radius);

        var scale = DonutScale;
        var typeface = new Typeface(GetValue(Avalonia.Controls.Documents.TextElement.FontFamilyProperty), FontStyle.Normal, FontWeight.SemiBold);
        var text = CachedText(CenterPill!, typeface, 14 * scale, SliceLabelBrush, radius * 1.8);
        var width = Math.Min(text.Width + 30 * scale, radius * 1.9);
        var height = text.Height + 14 * scale;
        var pill = new Rect(center.X - width / 2, center.Y - height / 2, width, height);
        var corner = height / 2;
        // A faint shadow under the pill lifts it off whatever is behind the hole.
        context.DrawRectangle(new ImmutableSolidColorBrush(Color.FromUInt32(0x1A000000)), null,
            pill.Translate(new Vector(0, 1.5 * scale)).Inflate(0.5), corner, corner);
        context.DrawRectangle(IsDarkGlass ? DarkPill : LightPill, new Pen(IsDarkGlass ? DarkRim : LightRim, 1), pill, corner, corner);
        context.DrawText(text, new Point(center.X - text.Width / 2, center.Y - text.Height / 2));
        return true;
    }

    /// <summary>
    /// How far a flip has come, 0 to 1 and back: the wheel rises a little as it turns over, like an object picked up.
    /// </summary>
    private double FlipLift => IsFlipping ? Math.Sin(Math.PI * Math.Clamp(_flipClock / FlipDuration, 0, 1)) : 0;

    /// <summary>How wide the plate is while the wheel flips: as wide as its widest slice.</summary>
    private double PlateFlipWidth()
    {
        if (!IsFlipping)
            return 1;
        var widest = 0.0;
        for (var i = 0; i < Count; i++)
            widest = Math.Max(widest, SliceFlipWidth(false, i));
        if (_oldFace is { } old)
            for (var i = 0; i < old.Length; i++)
                widest = Math.Max(widest, SliceFlipWidth(true, i));
        return widest;
    }
}
