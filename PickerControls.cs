using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace SerialMonitor;

// =========================================================================
// Horizontal gradient slider (brightness / R / G / B)
// =========================================================================
public sealed class GradientSlider : Control
{
    private AppTheme _t = AppTheme.AI;
    private int _value;
    private bool _drag;

    public string Caption { get; set; } = "";
    public string Suffix { get; set; } = "";
    public int Minimum { get; set; } = 0;
    public int Maximum { get; set; } = 100;
    public Color ColorLeft { get; set; } = Color.Black;
    public Color ColorRight { get; set; } = Color.White;
    public event Action<int>? ValueChanged;     // user drags only

    public AppTheme Skin
    {
        get => _t;
        set { _t = value; BackColor = value.Card; Invalidate(); }
    }

    public int Value
    {
        get => _value;
        set { _value = Math.Clamp(value, Minimum, Maximum); Invalidate(); }
    }

    public GradientSlider()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = _t.Card;
        Height = 44;
    }

    private RectangleF Track()
    {
        float h = 12f;
        return new RectangleF(10, Height - h - 8, Width - 20, h);
    }

    private void SetFromX(float x)
    {
        var tr = Track();
        float f = Math.Clamp((x - tr.X) / tr.Width, 0f, 1f);
        int v = Minimum + (int)Math.Round(f * (Maximum - Minimum));
        if (v == _value) return;
        _value = v;
        Invalidate();
        ValueChanged?.Invoke(_value);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!Enabled || e.Button != MouseButtons.Left) return;
        _drag = true;
        SetFromX(e.X);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Cursor = Enabled ? Cursors.Hand : Cursors.Default;
        if (_drag) SetFromX(e.X);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _drag = false;
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);
        var t = _t;
        var tr = Track();
        if (tr.Width < 10) return;

        // caption + value
        using (var f = new Font(t.HeadFont, 9f, FontStyle.Bold))
        {
            using var cb = new SolidBrush(Enabled ? t.TitleColor : t.Disabled);
            g.DrawString(Caption, f, cb, 8, 2);
            string v = _value + Suffix;
            var sz = g.MeasureString(v, f);
            using var vb = new SolidBrush(Enabled ? t.Text : t.Disabled);
            g.DrawString(v, f, vb, Width - sz.Width - 8, 2);
        }

        // gradient track
        using (var p = Gfx.RoundRect(tr, tr.Height / 2f))
        {
            Color l = Enabled ? ColorLeft : Gfx.Mix(ColorLeft, t.Card, 0.6f);
            Color r = Enabled ? ColorRight : Gfx.Mix(ColorRight, t.Card, 0.6f);
            using var lg = new LinearGradientBrush(new PointF(tr.X, 0), new PointF(tr.Right, 0), l, r);
            g.FillPath(lg, p);
            using var bp = new Pen(Color.FromArgb(150, t.CardBorder), 1.2f);
            g.DrawPath(bp, p);
        }

        // knob
        float f01 = Maximum == Minimum ? 0f : (_value - Minimum) / (float)(Maximum - Minimum);
        float kx = tr.X + tr.Width * f01, ky = tr.Y + tr.Height / 2f;
        float kr = 9f;
        using (var sh = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
            g.FillEllipse(sh, kx - kr, ky - kr + 1.5f, kr * 2, kr * 2);
        using (var kb = new SolidBrush(Enabled ? Color.White : t.Disabled))
            g.FillEllipse(kb, kx - kr, ky - kr, kr * 2, kr * 2);
        using (var kp = new Pen(Enabled ? t.Accent : t.Disabled, 2.5f))
            g.DrawEllipse(kp, kx - kr, ky - kr, kr * 2, kr * 2);
    }
}

// =========================================================================
// Hue / saturation colour wheel
// =========================================================================
public sealed class ColorWheel : Control
{
    private AppTheme _t = AppTheme.AI;
    private Bitmap? _bmp;
    private float _hue, _sat;
    private bool _drag;
    private int _bmpSize;

    public event Action<Color>? ColorPicked;   // user picks only

    public AppTheme Skin
    {
        get => _t;
        set { _t = value; BackColor = value.Card; Invalidate(); }
    }

    public ColorWheel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = _t.Card;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _bmp?.Dispose();
        base.Dispose(disposing);
    }

    // ---- HSV helpers ----
    public static Color FromHsv(float h, float s, float v)
    {
        h = ((h % 360f) + 360f) % 360f;
        float c = v * s, x = c * (1 - MathF.Abs(h / 60f % 2 - 1)), m = v - c;
        (float r, float g, float b) = (int)(h / 60f) switch
        {
            0 => (c, x, 0f),
            1 => (x, c, 0f),
            2 => (0f, c, x),
            3 => (0f, x, c),
            4 => (x, 0f, c),
            _ => (c, 0f, x)
        };
        return Color.FromArgb((int)Math.Round((r + m) * 255), (int)Math.Round((g + m) * 255), (int)Math.Round((b + m) * 255));
    }

    public void SetColor(Color c)
    {
        float max = Math.Max(c.R, Math.Max(c.G, c.B)) / 255f;
        float min = Math.Min(c.R, Math.Min(c.G, c.B)) / 255f;
        _sat = max <= 0f ? 0f : (max - min) / max;
        if (max - min > 0.0001f)
            _hue = c.GetHue();
        Invalidate();
    }

    private float Radius() => Math.Min(Width, Height) / 2f - 8f;

    private void EnsureBitmap()
    {
        int size = (int)(Radius() * 2);
        if (size < 10) return;
        if (_bmp != null && _bmpSize == size) return;
        _bmp?.Dispose();
        _bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        float r = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                float d = MathF.Sqrt(dx * dx + dy * dy);
                if (d > r) { _bmp.SetPixel(x, y, Color.Transparent); continue; }
                float hue = MathF.Atan2(dy, dx) * 180f / MathF.PI;
                var col = FromHsv(hue, Math.Min(d / r, 1f), 1f);
                int alpha = d > r - 1.2f ? (int)(255 * Math.Clamp(r - d, 0f, 1.2f) / 1.2f) : 255;
                _bmp.SetPixel(x, y, Color.FromArgb(alpha, col));
            }
        _bmpSize = size;
    }

    private void Pick(Point p)
    {
        float r = Radius();
        float cx = Width / 2f, cy = Height / 2f;
        float dx = p.X - cx, dy = p.Y - cy;
        float d = MathF.Sqrt(dx * dx + dy * dy);
        _sat = Math.Min(d / r, 1f);
        _hue = (MathF.Atan2(dy, dx) * 180f / MathF.PI + 360f) % 360f;
        Invalidate();
        ColorPicked?.Invoke(FromHsv(_hue, _sat, 1f));
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!Enabled || e.Button != MouseButtons.Left) return;
        _drag = true;
        Pick(e.Location);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Cursor = Enabled ? Cursors.Cross : Cursors.Default;
        if (_drag) Pick(e.Location);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _drag = false;
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);
        EnsureBitmap();
        if (_bmp == null) return;

        float r = Radius();
        float cx = Width / 2f, cy = Height / 2f;
        using (var ring = new Pen(Enabled ? _t.CardBorder : _t.Disabled, 2f))
            g.DrawEllipse(ring, cx - r - 1, cy - r - 1, r * 2 + 2, r * 2 + 2);

        if (Enabled)
            g.DrawImage(_bmp, cx - _bmp.Width / 2f, cy - _bmp.Height / 2f, _bmp.Width, _bmp.Height);
        else
        {
            var cm = new ColorMatrix { Matrix33 = 0.35f };
            using var ia = new ImageAttributes();
            ia.SetColorMatrix(cm);
            g.DrawImage(_bmp, new Rectangle((int)(cx - _bmp.Width / 2f), (int)(cy - _bmp.Height / 2f), _bmp.Width, _bmp.Height),
                0, 0, _bmp.Width, _bmp.Height, GraphicsUnit.Pixel, ia);
        }

        // selector
        float a = _hue * MathF.PI / 180f;
        float mx = cx + MathF.Cos(a) * _sat * r, my = cy + MathF.Sin(a) * _sat * r;
        using (var sp = new Pen(Color.Black, 3.5f)) g.DrawEllipse(sp, mx - 7, my - 7, 14, 14);
        using (var sp = new Pen(Color.White, 2f)) g.DrawEllipse(sp, mx - 7, my - 7, 14, 14);
    }
}
