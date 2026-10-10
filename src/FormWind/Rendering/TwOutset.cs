using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FormWind;

/// <summary>Bir gölge katmanı (CSS box-shadow): dikey kayma, bulanıklık, yayılma ve renk. Değerler pikseldir.</summary>
internal readonly struct TwShadowLayer : IEquatable<TwShadowLayer>
{
    public TwShadowLayer(float offsetY, float blur, float spread, Color color)
    {
        OffsetY = offsetY;
        Blur = blur;
        Spread = spread;
        Color = color;
    }

    public float OffsetY { get; }
    public float Blur { get; }
    public float Spread { get; }
    public Color Color { get; }

    public TwShadowLayer Scale(float scale) => new TwShadowLayer(OffsetY * scale, Blur * scale, Spread * scale, Color);

    /// <summary>Tailwind v3 gölge ön ayarları; <paramref name="color"/> verilirse katman renklerinin yerine geçer.</summary>
    public static TwShadowLayer[] Preset(TwShadowSize size, Color? color)
    {
        Color C(double alpha) => color ?? Color.FromArgb((int)Math.Round(alpha * 255), 0, 0, 0);
        return size switch
        {
            TwShadowSize.Sm => new[] { new TwShadowLayer(1, 2, 0, C(0.05)) },
            TwShadowSize.Base => new[] { new TwShadowLayer(1, 3, 0, C(0.1)), new TwShadowLayer(1, 2, -1, C(0.1)) },
            TwShadowSize.Md => new[] { new TwShadowLayer(4, 6, -1, C(0.1)), new TwShadowLayer(2, 4, -2, C(0.1)) },
            TwShadowSize.Lg => new[] { new TwShadowLayer(10, 15, -3, C(0.1)), new TwShadowLayer(4, 6, -4, C(0.1)) },
            TwShadowSize.Xl => new[] { new TwShadowLayer(20, 25, -5, C(0.1)), new TwShadowLayer(8, 10, -6, C(0.1)) },
            TwShadowSize.Xl2 => new[] { new TwShadowLayer(25, 50, -12, C(0.25)) },
            _ => Array.Empty<TwShadowLayer>(),
        };
    }

    public bool Equals(TwShadowLayer other) =>
        OffsetY == other.OffsetY && Blur == other.Blur && Spread == other.Spread && Color.ToArgb() == other.Color.ToArgb();

    public override bool Equals(object? obj) => obj is TwShadowLayer other && Equals(other);

    public override int GetHashCode() => OffsetY.GetHashCode() ^ (Blur.GetHashCode() * 7) ^ Color.ToArgb();
}

/// <summary>
/// Kontrolün dışına taşan süslemeler: gölge katmanları, focus halkası (ring) ve native kontrollerin çerçevesi
/// (<see cref="Frame"/>: kendi kenarlığını çizemeyen TextBox'ların kenarlığı ve köşesi).
/// </summary>
internal sealed class TwOutsetSpec : IEquatable<TwOutsetSpec>
{
    public static readonly Color DefaultRingColor = Color.FromArgb(128, 59, 130, 246); // Tailwind: blue-500 / %50

    public TwOutsetSpec(float radius, IReadOnlyList<TwShadowLayer> shadows, float ringWidth, Color ringColor, float ringOffset, Color ringOffsetColor)
    {
        Radius = radius;
        Shadows = shadows;
        RingWidth = ringWidth;
        RingColor = ringColor;
        RingOffset = ringOffset;
        RingOffsetColor = ringOffsetColor;
    }

    public float Radius { get; }
    public IReadOnlyList<TwShadowLayer> Shadows { get; }
    public float RingWidth { get; }
    public Color RingColor { get; }
    public float RingOffset { get; }
    public Color RingOffsetColor { get; }

    /// <summary>Kontrolün etrafına, zemin rengiyle doldurulup çizilen kenarlık; <c>null</c> = çerçeve yok.</summary>
    public TwChrome? Frame { get; private set; }

    /// <summary>Çerçeve ile kontrol arasındaki boşluk (piksel).</summary>
    public float FrameGap { get; private set; }

    public bool IsEmpty => Shadows.Count == 0 && RingWidth <= 0 && Frame is null;

    /// <summary>Çerçevenin kontrol sınırlarından taştığı mesafe (kenar başına).</summary>
    public Padding FrameInset => Frame is TwChrome frame
        ? new Padding(
            (int)Math.Ceiling(FrameGap + frame.BorderLeft), (int)Math.Ceiling(FrameGap + frame.BorderTop),
            (int)Math.Ceiling(FrameGap + frame.BorderRight), (int)Math.Ceiling(FrameGap + frame.BorderBottom))
        : Padding.Empty;

    /// <summary>Aynı gölge ve halkayla, verilen çerçeveyi de çizen bir kopya.</summary>
    public TwOutsetSpec WithFrame(TwChrome frame, float gap) =>
        new TwOutsetSpec(Radius, Shadows, RingWidth, RingColor, RingOffset, RingOffsetColor) { Frame = frame, FrameGap = gap };

    /// <summary>Gölge ya da halkanın kontrol sınırlarının dışına taştığı en büyük mesafe.</summary>
    public int Extent
    {
        get
        {
            var inset = FrameInset;
            float frame = Math.Max(Math.Max(inset.Left, inset.Right), Math.Max(inset.Top, inset.Bottom));
            float extent = frame + RingWidth + RingOffset;
            foreach (var layer in Shadows)
                extent = Math.Max(extent, frame + layer.Blur + Math.Max(0, layer.Spread) + Math.Abs(layer.OffsetY));
            return (int)Math.Ceiling(extent) + 2;
        }
    }

    public static TwOutsetSpec FromStyle(TwStyle style, float scale)
    {
        var layers = TwShadowLayer.Preset(style.Shadow ?? TwShadowSize.None, style.ShadowColor);
        for (int i = 0; i < layers.Length; i++) layers[i] = layers[i].Scale(scale);
        return new TwOutsetSpec(
            (style.Radius ?? 0) * scale,
            layers,
            (style.RingWidth ?? 0) * scale,
            style.RingColor ?? DefaultRingColor,
            (style.RingOffset ?? 0) * scale,
            style.RingOffsetColor ?? Color.White);
    }

    public bool Equals(TwOutsetSpec? other)
    {
        if (other is null) return false;
        if (Radius != other.Radius || RingWidth != other.RingWidth || RingOffset != other.RingOffset
            || RingColor.ToArgb() != other.RingColor.ToArgb() || RingOffsetColor.ToArgb() != other.RingOffsetColor.ToArgb()
            || Shadows.Count != other.Shadows.Count
            || !Nullable.Equals(Frame, other.Frame) || FrameGap != other.FrameGap)
            return false;
        for (int i = 0; i < Shadows.Count; i++)
            if (!Shadows[i].Equals(other.Shadows[i])) return false;
        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as TwOutsetSpec);

    public override int GetHashCode() => Radius.GetHashCode() ^ RingWidth.GetHashCode() ^ Shadows.Count;
}

/// <summary>
/// Gölge ve halkayı kontrolün <b>ebeveyninde</b> çizer: kontroller kendi sınırlarının dışına çizim yapamaz.
/// Ebeveynin Paint olayı çocuk pencerelerden önce çizildiği için gölge doğal olarak kontrolün altında kalır.
/// Form, Panel ve TwPanel gibi Paint olayı tetikleyen her ebeveynde çalışır.
/// </summary>
internal sealed class TwOutset
{
    private static readonly ConditionalWeakTable<Control, TwOutset> Table = new ConditionalWeakTable<Control, TwOutset>();
    private static readonly ConditionalWeakTable<Control, ParentPainter> Painters = new ConditionalWeakTable<Control, ParentPainter>();

    private readonly Control _control;
    private Control? _parent;
    private Rectangle _paintedBounds;

    private TwOutset(Control control)
    {
        _control = control;
        _control.ParentChanged += (_, _) => Reattach();
        _control.LocationChanged += (_, _) => InvalidateParent();
        _control.SizeChanged += (_, _) => InvalidateParent();
        _control.VisibleChanged += (_, _) => InvalidateParent();
        // Çerçevenin zemini kontrolün o anki rengidir (geçiş animasyonları dahil).
        _control.BackColorChanged += (_, _) => { if (Spec?.Frame != null) InvalidateParent(); };
        Reattach();
    }

    public TwOutsetSpec? Spec { get; private set; }

    public static bool IsAttached(Control control) => Table.TryGetValue(control, out _);

    public static TwOutset For(Control control) => Table.GetValue(control, c => new TwOutset(c));

    public void Update(TwOutsetSpec spec)
    {
        if (Spec != null && Spec.Equals(spec)) return;
        Spec = spec.IsEmpty ? null : spec;
        InvalidateParent();
    }

    internal void Paint(Graphics graphics)
    {
        var spec = Spec;
        if (spec is null || !_control.Visible) return;

        var bounds = _control.Bounds;
        _paintedBounds = Rectangle.Inflate(bounds, spec.Extent, spec.Extent);

        // Çerçeve varsa gölge ve halka onun dışına çizilir.
        var inset = spec.FrameInset;
        bounds = Rectangle.FromLTRB(bounds.Left - inset.Left, bounds.Top - inset.Top, bounds.Right + inset.Right, bounds.Bottom + inset.Bottom);

        // Gölgeler, en geniş katman önce.
        for (int i = spec.Shadows.Count - 1; i >= 0; i--)
            TwShadowRenderer.Draw(graphics, bounds, spec.Radius, spec.Shadows[i]);

        if (spec.Frame is TwChrome frame)
        {
            TwPainter.FillRounded(graphics, bounds, frame.Radius, _control.BackColor);
            TwPainter.StrokeChrome(graphics, bounds, frame);
        }

        if (spec.RingWidth > 0)
        {
            var radius = TwPainter.EffectiveRadius(spec.Radius, bounds.Size);
            if (spec.RingOffset > 0)
            {
                var offsetBounds = Rectangle.Inflate(bounds, (int)Math.Ceiling(spec.RingOffset), (int)Math.Ceiling(spec.RingOffset));
                TwPainter.StrokeRounded(graphics, offsetBounds, radius + spec.RingOffset, spec.RingOffset, spec.RingOffsetColor);
            }
            var grow = (int)Math.Ceiling(spec.RingOffset + spec.RingWidth);
            var ringBounds = Rectangle.Inflate(bounds, grow, grow);
            TwPainter.StrokeRounded(graphics, ringBounds, radius + grow, spec.RingWidth, spec.RingColor);
        }
    }

    private void Reattach()
    {
        var parent = _control.Parent;
        if (ReferenceEquals(parent, _parent)) return;
        if (_parent != null && Painters.TryGetValue(_parent, out var oldPainter))
        {
            oldPainter.Remove(this);
            _parent.Invalidate(_paintedBounds);
        }
        _parent = parent;
        if (parent != null) Painters.GetValue(parent, p => new ParentPainter(p)).Add(this);
        InvalidateParent();
    }

    private void InvalidateParent()
    {
        if (_parent is null) return;
        var extent = Spec?.Extent ?? 0;
        var area = Rectangle.Inflate(_control.Bounds, extent, extent);
        _parent.Invalidate(Rectangle.Union(area, _paintedBounds.IsEmpty ? area : _paintedBounds));
        _paintedBounds = area;
    }

    private sealed class ParentPainter
    {
        private readonly List<WeakReference<TwOutset>> _children = new List<WeakReference<TwOutset>>();

        public ParentPainter(Control parent) => parent.Paint += OnPaint;

        public void Add(TwOutset outset) => _children.Add(new WeakReference<TwOutset>(outset));

        public void Remove(TwOutset outset) =>
            _children.RemoveAll(r => !r.TryGetTarget(out var target) || ReferenceEquals(target, outset));

        private void OnPaint(object? sender, PaintEventArgs e)
        {
            // Controls sırasının tersi: alttaki (sonra eklenen) kontrolün gölgesi önce çizilir.
            for (int i = _children.Count - 1; i >= 0; i--)
            {
                if (_children[i].TryGetTarget(out var outset)) outset.Paint(e.Graphics);
                else _children.RemoveAt(i);
            }
        }
    }
}

/// <summary>Bulanık gölge bitmap'lerini üretir ve önbelleğe alır (3 geçişli kutu bulanıklığı ≈ Gauss).</summary>
internal static class TwShadowRenderer
{
    private const int CacheLimit = 128;
    private static readonly Dictionary<(int W, int H, int Radius, int Blur, int Color), Bitmap> Cache =
        new Dictionary<(int W, int H, int Radius, int Blur, int Color), Bitmap>();

    public static void Draw(Graphics graphics, Rectangle bounds, float radius, TwShadowLayer layer)
    {
        if (layer.Color.A == 0) return;
        var spread = layer.Spread;
        var rect = RectangleF.Inflate(bounds, spread, spread);
        rect.Offset(0, layer.OffsetY);
        if (rect.Width <= 0 || rect.Height <= 0) return;

        int blur = (int)Math.Ceiling(layer.Blur);
        int margin = blur + 2;
        var effectiveRadius = Math.Max(0, TwPainter.EffectiveRadius(radius, bounds.Size) + spread);
        var bitmap = Get((int)Math.Ceiling(rect.Width), (int)Math.Ceiling(rect.Height), (int)Math.Round(effectiveRadius), blur, layer.Color);
        graphics.DrawImageUnscaled(bitmap, (int)Math.Round(rect.X) - margin, (int)Math.Round(rect.Y) - margin);
    }

    private static Bitmap Get(int width, int height, int radius, int blur, Color color)
    {
        var key = (width, height, radius, blur, color.ToArgb());
        lock (Cache)
        {
            if (Cache.TryGetValue(key, out var cached)) return cached;
            if (Cache.Count >= CacheLimit)
            {
                foreach (var bitmap in Cache.Values) bitmap.Dispose();
                Cache.Clear();
            }
            var created = Create(width, height, radius, blur, color);
            Cache[key] = created;
            return created;
        }
    }

    private static Bitmap Create(int width, int height, int radius, int blur, Color color)
    {
        int margin = blur + 2;
        int w = width + margin * 2, h = height + margin * 2;
        var bitmap = new Bitmap(w, h, PixelFormat.Format32bppArgb);

        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(Color.Black);
            using var path = TwPainter.CreateRoundedRect(new RectangleF(margin, margin, width, height), radius);
            g.FillPath(brush, path);
        }

        var data = bitmap.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        try
        {
            var pixels = new byte[data.Stride * h];
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);

            var alpha = new byte[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    alpha[y * w + x] = pixels[y * data.Stride + x * 4 + 3];

            // CSS blur yarıçapı B ≈ Gauss σ = B/2; üç kutu geçişi için kutu yarıçapı ≈ B/2.
            int boxRadius = blur > 0 ? Math.Max(1, (int)Math.Round(blur / 2.0)) : 0;
            for (int pass = 0; pass < 3 && boxRadius > 0; pass++)
            {
                BoxBlurHorizontal(alpha, w, h, boxRadius);
                BoxBlurVertical(alpha, w, h, boxRadius);
            }

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * data.Stride + x * 4;
                    pixels[i] = color.B;
                    pixels[i + 1] = color.G;
                    pixels[i + 2] = color.R;
                    pixels[i + 3] = (byte)(alpha[y * w + x] * color.A / 255);
                }
            }
            Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
        return bitmap;
    }

    private static void BoxBlurHorizontal(byte[] values, int w, int h, int r)
    {
        var row = new byte[w];
        int size = r * 2 + 1;
        for (int y = 0; y < h; y++)
        {
            int offset = y * w, sum = 0;
            for (int x = -r; x <= r; x++) sum += values[offset + Clamp(x, w)];
            for (int x = 0; x < w; x++)
            {
                row[x] = (byte)(sum / size);
                sum += values[offset + Clamp(x + r + 1, w)] - values[offset + Clamp(x - r, w)];
            }
            Buffer.BlockCopy(row, 0, values, offset, w);
        }
    }

    private static void BoxBlurVertical(byte[] values, int w, int h, int r)
    {
        var column = new byte[h];
        int size = r * 2 + 1;
        for (int x = 0; x < w; x++)
        {
            int sum = 0;
            for (int y = -r; y <= r; y++) sum += values[Clamp(y, h) * w + x];
            for (int y = 0; y < h; y++)
            {
                column[y] = (byte)(sum / size);
                sum += values[Clamp(y + r + 1, h) * w + x] - values[Clamp(y - r, h) * w + x];
            }
            for (int y = 0; y < h; y++) values[y * w + x] = column[y];
        }
    }

    private static int Clamp(int value, int length) => value < 0 ? 0 : value >= length ? length - 1 : value;
}
