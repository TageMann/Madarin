// Mandarin: the wheel as a piece of glass floating above Finder.
//
// With GlassStyle on (the live wheel and its previews), the donut is drawn as:
//   • a glass plate: a translucent ring with a polished rim, behind the slices;
//   • each slice a rounded glass button inset in the plate: clear, near-white (graphite in dark mode), with a sheen
//     across its top and a polished rim;
//   • the slice under the pointer filled with the accent colour (Mandarin orange by default) as colour inside the glass;
//   • a clear hole in the middle, so the file being dragged stays visible behind it, and in it only a small glass
//     pill with the files' size ("2.2 MB", CenterPill).
// The glass itself is colourless, never the accent: as with liquid glass, the colour comes from what is behind it, and
// only the lit slice takes the accent. The slices are opaque
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

    /// <summary>Mandarin: slice names in capitals ("ADD BG"), as on the design's wheel.</summary>
    public static readonly StyledProperty<bool> UppercaseLabelsProperty =
        AvaloniaProperty.Register<DonutMenu, bool>(nameof(UppercaseLabels));

    public bool UppercaseLabels { get => GetValue(UppercaseLabelsProperty); set => SetValue(UppercaseLabelsProperty, value); }

    /// <summary>How far the glass slices sit inside the plate's rim, in pixels at this size.</summary>
    internal double GlassInset => GlassStyle ? 7 * DonutScale : 0;

    /// <summary>The gap between slices: wider for glass, so each reads as a button of its own.</summary>
    private double SliceGap => GlassStyle ? 7 : GapPixels;

    private bool IsDarkGlass => ActualThemeVariant == ThemeVariant.Dark;

    // The radius of the native glass disc the frame being drawn wants under it (0: none).
    private double _glassDisc;
    private (Point Center, double Radius) _reportedDisc;

    /// <summary>
    /// The disc of native glass the wheel wants under it changed: its centre and radius in the control's coordinates
    /// (radius 0: none). The owner puts the system's glass there (NativeGlass), so it follows the open, turn and close.
    /// </summary>
    public event Action<Point, double>? GlassDiscChanged;

    /// <summary>The disc last reported (for tests).</summary>
    internal (Point Center, double Radius) GlassDisc => _reportedDisc;

    /// <summary>
    /// macOS native glass sits under the wheel: the drawn plate and slices are made lighter so the real blur shows
    /// through, while the rims, sheen and labels stay as drawn.
    /// </summary>
    public bool NativeGlassBehind { get; set; }

    private void ReportGlassDisc(Point center, double radius)
    {
        if (!GlassStyle)
            radius = 0;
        radius = Math.Round(radius * 2) / 2;
        if (_reportedDisc == (center, radius))
            return;
        _reportedDisc = (center, radius);
        GlassDiscChanged?.Invoke(center, radius);
    }

    /// <summary>The wheel is closed or hidden: no native glass under it.</summary>
    internal void ClearGlassDisc() => ReportGlassDisc(Center, 0);

    /// <summary>
    /// macOS Reduce transparency: the glass is drawn solid (read when the app starts, see App). Slices, plate and pill
    /// keep their shapes and rims.
    /// </summary>
    public static bool SolidGlass { get; set; }

    private static readonly IImmutableBrush LightSliceSolid = Linear((0xFFFFFFFF, 0), (0xFFF5F5F7, 1));
    private static readonly IImmutableBrush DarkSliceSolid = Linear((0xFF3A3A3F, 0), (0xFF2C2C30, 1));
    private static readonly IImmutableBrush LightPlateSolid = Linear((0xFFF2F2F5, 0), (0xFFE8E8EC, 1));
    private static readonly IImmutableBrush DarkPlateSolid = Linear((0xFF232327, 0), (0xFF1C1C1F, 1));

    // Over macOS's own glass: the drawn surfaces are a thin frost on it, so its blur and refraction read through.
    private static readonly IImmutableBrush LightSliceOverGlass = Linear((0x8CFFFFFF, 0), (0x66FFFFFF, 0.55), (0x59FAFAFC, 1));
    private static readonly IImmutableBrush DarkSliceOverGlass = Linear((0x66505056, 0), (0x4D3A3A40, 1));
    private static readonly IImmutableBrush LightPlateOverGlass = Linear((0x26FFFFFF, 0), (0x14FFFFFF, 1));
    private static readonly IImmutableBrush DarkPlateOverGlass = Linear((0x1F000000, 0), (0x14000000, 1));

    private IImmutableBrush SliceFill => SolidGlass ? IsDarkGlass ? DarkSliceSolid : LightSliceSolid
        : NativeGlassBehind ? IsDarkGlass ? DarkSliceOverGlass : LightSliceOverGlass
        : IsDarkGlass ? DarkSlice : LightSlice;
    private IImmutableBrush PlateFill => SolidGlass ? IsDarkGlass ? DarkPlateSolid : LightPlateSolid
        : NativeGlassBehind ? IsDarkGlass ? DarkPlateOverGlass : LightPlateOverGlass
        : IsDarkGlass ? DarkPlate : LightPlate;

    // Immutable brushes: shared by every donut and free of any thread (a mutable brush belongs to the one that made it).
    private static IImmutableBrush Linear(params (uint Argb, double Offset)[] stops) =>
        new ImmutableLinearGradientBrush(
            [.. stops.Select(s => new ImmutableGradientStop(s.Offset, Color.FromUInt32(s.Argb)))],
            startPoint: new RelativePoint(0.15, 0, RelativeUnit.Relative),
            endPoint: new RelativePoint(0.85, 1, RelativeUnit.Relative));

    // ── The glass, light and dark: colourless, as with liquid glass; colour comes from behind and from the lit slice ──
    private static readonly IImmutableBrush LightSlice = Linear((0xD9FFFFFF, 0), (0xC7FBFBFD, 0.55), (0xBDF2F3F7, 1));
    private static readonly IImmutableBrush DarkSlice = Linear((0xB33C3C42, 0), (0xA6303036, 0.55), (0x9E26262B, 1));
    private static readonly IImmutableBrush LightPlate = Linear((0x66FFFFFF, 0), (0x47F7F8FB, 0.5), (0x38EEF0F5, 1));
    private static readonly IImmutableBrush DarkPlate = Linear((0x592E2E33, 0), (0x47222226, 1));
    private static readonly IImmutableBrush LightRim = Linear((0xFFFFFFFF, 0), (0x8CFFFFFF, 0.45), (0x4DFFFFFF, 1));
    private static readonly IImmutableBrush DarkRim = Linear((0x80FFFFFF, 0), (0x2EFFFFFF, 0.5), (0x17FFFFFF, 1));
    private static readonly IImmutableBrush LitRim = Linear((0xCCFFFFFF, 0), (0x40FFFFFF, 0.5), (0x1FFFFFFF, 1));
    private static readonly IImmutableBrush LightPill = Linear((0xF2FFFFFF, 0), (0xE0F6F7FA, 1));
    private static readonly IImmutableBrush DarkPill = Linear((0xE6404046, 0), (0xDE2A2A2F, 1));
    private static readonly IImmutableBrush HoleTint = new ImmutableSolidColorBrush(Color.FromUInt32(0x14FFFFFF));
    private static readonly IImmutableSolidColorBrush Hairline = new ImmutableSolidColorBrush(Color.FromUInt32(0x1F000000));
    private static readonly IImmutableSolidColorBrush GlassLabel = new ImmutableSolidColorBrush(Color.FromUInt32(0xFF1D1D1F));
    private static readonly IImmutableSolidColorBrush GlassLabelDark = new ImmutableSolidColorBrush(Color.FromUInt32(0xFFF5F5F7));

    /// <summary>
    /// The light inside each glass button: a sheen across its top that fades out by the middle, and a faint shade at
    /// its bottom, so the button reads as a lens with depth rather than a flat tint.
    /// </summary>
    private static readonly IImmutableBrush Sheen = new ImmutableLinearGradientBrush(
        [new ImmutableGradientStop(0, Color.FromUInt32(0x73FFFFFF)), new ImmutableGradientStop(0.42, Color.FromUInt32(0x00FFFFFF)),
         new ImmutableGradientStop(0.78, Color.FromUInt32(0x00000000)), new ImmutableGradientStop(1, Color.FromUInt32(0x14000000))],
        startPoint: new RelativePoint(0.5, 0, RelativeUnit.Relative), endPoint: new RelativePoint(0.5, 1, RelativeUnit.Relative));

    private Color _litFor;
    private IImmutableBrush? _lit;
    private IImmutableBrush? _litLabel;

    /// <summary>The accent the lit slice takes (the theme's, Mandarin orange unless the user picked another).</summary>
    private Color AccentColor => (AccentBrush as ISolidColorBrush)?.Color ?? Color.FromUInt32(0xFFF27A1E);

    /// <summary>
    /// The lit slice: the accent as colour inside the glass, a little lighter at the top where the light comes in and
    /// deeper at the bottom, not quite opaque.
    /// </summary>
    private IImmutableBrush Lit
    {
        get
        {
            var accent = AccentColor;
            if (_lit is null || accent != _litFor)
            {
                _litFor = accent;
                _lit = Linear((Mix(accent, Colors.White, 0.28, 0xF0), 0), (Mix(accent, Colors.White, 0, 0xEB), 0.5),
                    (Mix(accent, Colors.Black, 0.14, 0xF2), 1));
                _litLabel = new ImmutableSolidColorBrush(Luminance(accent) > 0.62 ? Color.FromUInt32(0xFF1D1D1F) : Colors.White);
            }
            return _lit;
        }
    }

    private static uint Mix(Color a, Color b, double t, byte alpha)
    {
        byte C(byte x, byte y) => (byte)Math.Round(x + (y - x) * t);
        return (uint)(alpha << 24 | C(a.R, b.R) << 16 | C(a.G, b.G) << 8 | C(a.B, b.B));
    }

    private static double Luminance(Color c) => (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255;

    /// <summary>The label of the lit slice: white on the accent glass (near-black on a pale accent), the theme's own otherwise.</summary>
    private IBrush? HotLabelBrush => GlassStyle ? LitLabel : OnAccentBrush;

    private IBrush LitLabel
    {
        get
        {
            _ = Lit;
            return _litLabel ?? Brushes.White;
        }
    }

    /// <summary>The label of an ordinary slice: near-black on light glass, near-white on dark.</summary>
    private IBrush? SliceLabelBrush => GlassStyle ? IsDarkGlass ? GlassLabelDark : GlassLabel : Foreground;

    /// <summary>
    /// The light catching the glass as it turns: a soft white band that sweeps across the disc, strongest when the disc
    /// is angled (a quarter and three quarters of the way round) and gone when it lies flat or stands edge-on.
    /// </summary>
    private void DrawFlipGlint(DrawingContext context, Point center, double r1, double r2)
    {
        var angle = GlassAngle;
        var strength = Math.Pow(Math.Sin(2 * angle), 2) * (SolidGlass ? 0.08 : 0.2);
        var width = PlateFlipWidth();
        if (strength < 0.01 || width < EdgeOn)
            return;
        var at = Math.Clamp(angle / Math.PI, 0, 1);
        var band = new ImmutableLinearGradientBrush(
            [
                new ImmutableGradientStop(Math.Clamp(at - 0.28, 0, 1), Color.FromArgb(0, 255, 255, 255)),
                new ImmutableGradientStop(at, Color.FromArgb((byte)(255 * strength), 255, 255, 255)),
                new ImmutableGradientStop(Math.Clamp(at + 0.28, 0, 1), Color.FromArgb(0, 255, 255, 255)),
            ],
            startPoint: new RelativePoint(0, 0.2, RelativeUnit.Relative), endPoint: new RelativePoint(1, 0.8, RelativeUnit.Relative));
        var depth = 1 - 0.05 * FlipLift;
        var turn = Matrix.CreateTranslation(-center.X, -center.Y) * Matrix.CreateScale(width * depth, depth) *
                   Matrix.CreateTranslation(center.X, center.Y);
        using (context.PushTransform(turn))
            context.DrawGeometry(band, null, CreateRing(center, r1 + GlassInset, r2 - GlassInset));
    }

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
        var lift = 1 - 0.05 * FlipLift; // eases back with the slices as the disc turns
        // The native glass under the plate: a disc no wider than the plate as drawn (round while it turns).
        if (opening > 0.6 && !_closing)
            _glassDisc = r2 * scale * lift * Math.Min(1, width);
        using var transform = context.PushTransform(
            Matrix.CreateTranslation(-center.X, -center.Y) * Matrix.CreateScale(scale * width * lift, scale * lift) *
            Matrix.CreateTranslation(center.X, center.Y));
        using var fade = context.PushOpacity(opening);
        var ring = CreateRing(center, r1, r2);
        context.DrawGeometry(PlateFill, null, ring);
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
            context.DrawGeometry(SliceFill, null, wedge);
        if (emphasis > 0.01)
        {
            // The accent fills the glass, with a soft glow of it just outside the slice.
            var lit = Lit;
            using (context.PushOpacity(0.16 * emphasis))
                context.DrawGeometry(null, new Pen(lit, 8, lineJoin: PenLineJoin.Round), wedge);
            using (context.PushOpacity(emphasis))
            {
                context.DrawGeometry(lit, null, wedge);
                // Light gathering inside the coloured glass, near its top: colour in the material, not paint on it.
                context.DrawGeometry(LitGlow, null, wedge);
            }
        }
        using (context.PushOpacity(enabled ? 1 : 0.6))
            context.DrawGeometry(Sheen, null, wedge);
        var rim = emphasis > 0.5 ? LitRim : IsDarkGlass ? DarkRim : LightRim;
        context.DrawGeometry(null, new Pen(rim, 1.2, lineJoin: PenLineJoin.Round), wedge);
        // A faint shade along the lower edge: the rim catches light at the top and falls into shadow below.
        context.DrawGeometry(null, new Pen(IsDarkGlass ? DarkUnderRim : LightUnderRim, 1, lineJoin: PenLineJoin.Round), wedge);
    }

    private static readonly IImmutableBrush LitGlow = new ImmutableRadialGradientBrush(
        [new ImmutableGradientStop(0, Color.FromUInt32(0x52FFFFFF)), new ImmutableGradientStop(1, Color.FromUInt32(0x00FFFFFF))],
        center: new RelativePoint(0.5, 0.22, RelativeUnit.Relative), gradientOrigin: new RelativePoint(0.5, 0.22, RelativeUnit.Relative), radius: 0.62);

    private static readonly IImmutableBrush LightUnderRim = Linear((0x00000000, 0), (0x00000000, 0.55), (0x24000000, 1));
    private static readonly IImmutableBrush DarkUnderRim = Linear((0x00000000, 0), (0x00000000, 0.55), (0x4D000000, 1));

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
        var text = new FormattedText(CenterPill!, System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            typeface, 14 * scale, SliceLabelBrush);
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
    private double FlipLift => IsFlipping ? Math.Sin(GlassAngle) : 0;

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
