using System.Drawing.Drawing2D;
using System.Globalization;

namespace SerialMonitor;

// =========================================================================
// Animated arc gauge widget. Fed by lines like  TEMP:28.5  from the serial port.
// =========================================================================
public sealed class GaugeControl : Control
{
    private static readonly Color Warn = Color.FromArgb(250, 204, 21);

    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 30 };
    private AppTheme _t = AppTheme.AI;
    private double _value, _shown;
    private bool _has;
    private float _phase;

    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public string Unit { get; set; } = "";
    public double Minimum { get; set; } = 0;
    public double Maximum { get; set; } = 100;
    public bool Invert { get; set; }          // true = LOW values are the dangerous ones (battery, distance)
    public bool HasData => _has;

    public AppTheme Skin
    {
        get => _t;
        set { _t = value; BackColor = value.Card; Invalidate(); }
    }

    public void SetValue(double v)
    {
        _value = v;
        if (!_has) { _has = true; _shown = v; }
        Invalidate();
    }

    public void ClearData()
    {
        _has = false;
        Invalidate();
    }

    public GaugeControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = _t.Card;
        Size = new Size(134, 142);
        _timer.Tick += (_, _) =>
        {
            if (!_has) return;
            double range = Math.Max(1e-9, Maximum - Minimum);
            bool moving = Math.Abs(_value - _shown) > range * 0.0008;
            if (moving) _shown += (_value - _shown) * 0.2; else _shown = _value;
            _phase += 0.2f;
            if (moving || Danger() > 0.85f) Invalidate();
        };
        _timer.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }

    private float Frac()
    {
        double range = Maximum - Minimum;
        if (range <= 0) return 0f;
        return (float)Math.Clamp((_shown - Minimum) / range, 0.0, 1.0);
    }

    private float Danger() => Invert ? 1f - Frac() : Frac();

    private string Fmt(double v)
        => (Math.Abs(v - Math.Round(v)) > 0.05 && Math.Abs(v) < 1000 ? v.ToString("0.0", CultureInfo.InvariantCulture)
                                                                    : Math.Round(v).ToString("0", CultureInfo.InvariantCulture));

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(BackColor);

        float u = Math.Min(Width / 134f, Height / 142f);
        if (u <= 0) return;
        var t = _t;
        float cx = Width / 2f;
        float R = Math.Min(Width / 2f - 14 * u, Height * 0.34f);
        float cy = Height * 0.54f;
        var box = new RectangleF(cx - R, cy - R, R * 2, R * 2);
        var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        // title
        using (var tf = new Font(t.HeadFont, Math.Max(7f, 8.5f * u), FontStyle.Bold))
        using (var tb = new SolidBrush(t.TitleColor))
            g.DrawString(Title, tf, tb, new RectangleF(0, 3 * u, Width, 16 * u), center);

        // background arc
        float pw = Math.Max(5f, 10f * u);
        using (var bg = new Pen(Gfx.Mix(t.Card, Color.White, 0.14f), pw) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawArc(bg, box, 150, 240);

        float f = Frac();
        float danger = Danger();
        Color col = !_has ? t.Disabled : danger < 0.6f ? t.OnCol : danger < 0.85f ? Warn : t.OffCol;

        if (_has && f > 0.002f)
        {
            float pulse = danger > 0.85f ? (MathF.Sin(_phase) + 1f) / 2f : 0.4f;
            using (var glow = new Pen(Color.FromArgb((int)(30 + 60 * pulse), col), pw * 1.9f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawArc(glow, box, 150, 240 * f);
            using (var vp = new Pen(col, pw) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawArc(vp, box, 150, 240 * f);

            float a = (150 + 240 * f) * MathF.PI / 180f;
            float tx = cx + MathF.Cos(a) * R, ty = cy + MathF.Sin(a) * R;
            using var dot = new SolidBrush(Color.White);
            g.FillEllipse(dot, tx - 3.2f * u, ty - 3.2f * u, 6.4f * u, 6.4f * u);
        }

        // min / max labels
        using (var sf = new Font(t.UiFont, Math.Max(6.5f, 7f * u)))
        using (var sb = new SolidBrush(Gfx.Mix(t.Text, t.Card, 0.45f)))
        {
            float ly = cy + R * 0.5f + 12 * u;
            g.DrawString(Fmt(Minimum), sf, sb, new RectangleF(cx - R - 12 * u, ly, 34 * u, 12 * u), center);
            g.DrawString(Fmt(Maximum), sf, sb, new RectangleF(cx + R - 22 * u, ly, 34 * u, 12 * u), center);
        }

        // value + unit
        using (var vf = new Font(t.LogFont, Math.Max(10f, 15f * u), FontStyle.Bold))
        using (var vb = new SolidBrush(_has ? t.Text : t.Disabled))
            g.DrawString(_has ? Fmt(_shown) : "--", vf, vb, new RectangleF(cx - R, cy - 16 * u, R * 2, 24 * u), center);

        using (var uf = new Font(t.UiFont, Math.Max(7f, 8f * u)))
        using (var ub = new SolidBrush(_has ? col : t.Disabled))
            g.DrawString(_has ? Unit : "no data", uf, ub, new RectangleF(cx - R, cy + 8 * u, R * 2, 14 * u), center);

        // key tag at the bottom
        using (var kf = new Font(t.LogFont, Math.Max(6.5f, 7.5f * u)))
        using (var kb = new SolidBrush(Gfx.Mix(t.Text, t.Card, 0.55f)))
            g.DrawString(Key + ":", kf, kb, new RectangleF(0, Height - 15 * u, Width, 13 * u), center);
    }
}
