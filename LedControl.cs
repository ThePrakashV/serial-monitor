using System.Drawing.Drawing2D;

namespace SerialMonitor;

// =========================================================================
// Animated LED bulb: color + brightness (0-100) -> glow radius / intensity, soft pulse
// =========================================================================
public sealed class LedControl : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 30 };
    private AppTheme _t = AppTheme.AI;
    private Color _color = Color.White;
    private int _brightness = 50;
    private float _shown;      // smoothed brightness 0..1
    private float _phase;

    public AppTheme Skin
    {
        get => _t;
        set { _t = value; BackColor = value.Card; Invalidate(); }
    }

    public Color LedColor
    {
        get => _color;
        set { _color = value; Invalidate(); }
    }

    public int Brightness
    {
        get => _brightness;
        set { _brightness = Math.Clamp(value, 0, 100); Invalidate(); }
    }

    public LedControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = _t.Card;
        _timer.Tick += (_, _) =>
        {
            float target = _brightness / 100f;
            if (Math.Abs(target - _shown) > 0.002f) _shown += (target - _shown) * 0.2f; else _shown = target;
            if (_shown > 0.01f) _phase += 0.1f;
            Invalidate();
        };
        _timer.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        float u = Math.Min(Width, Height) / 100f;
        if (u <= 0) return;
        float cx = Width / 2f, cy = Height / 2f - 6 * u;
        float b = _shown;
        float pulse = (MathF.Sin(_phase) + 1f) / 2f;
        var t = _t;

        // outer glow (grows with brightness)
        if (b > 0.01f)
        {
            float gr = (26 + 30 * b + pulse * 3 * b) * u;
            using var gp = new GraphicsPath();
            gp.AddEllipse(cx - gr, cy - gr, gr * 2, gr * 2);
            using var pg = new PathGradientBrush(gp)
            {
                CenterColor = Color.FromArgb((int)(200 * b), _color),
                SurroundColors = new[] { Color.FromArgb(0, _color) }
            };
            g.FillPath(pg, gp);

            // rays
            using var rp = new Pen(Color.FromArgb((int)(210 * b), _color), Math.Max(2f, 2.6f * u))
            { StartCap = LineCap.Round, EndCap = LineCap.Round };
            for (int k = 0; k < 12; k++)
            {
                float a = (k * 30f + _phase * 6f) * MathF.PI / 180f;
                float r1 = 25 * u, r2 = (28 + 10 * b + pulse * 3) * u;
                g.DrawLine(rp, cx + MathF.Cos(a) * r1, cy + MathF.Sin(a) * r1,
                               cx + MathF.Cos(a) * r2, cy + MathF.Sin(a) * r2);
            }
        }

        // base / socket
        using (var sb = new SolidBrush(t.IconOff))
        {
            using var sp = Gfx.RoundRect(new RectangleF(cx - 11 * u, cy + 21 * u, 22 * u, 14 * u), 3 * u);
            g.FillPath(sb, sp);
        }
        using (var lp = new Pen(Gfx.Mix(t.IconOff, Color.Black, 0.4f), Math.Max(1.2f, 1.5f * u)))
        {
            g.DrawLine(lp, cx - 10 * u, cy + 26 * u, cx + 10 * u, cy + 26 * u);
            g.DrawLine(lp, cx - 10 * u, cy + 30 * u, cx + 10 * u, cy + 30 * u);
        }

        // bulb
        Color off = Gfx.Mix(t.IconOff, t.Card, 0.25f);
        Color core = Gfx.Mix(off, _color, b);
        using var bulb = new GraphicsPath();
        bulb.AddEllipse(cx - 21 * u, cy - 24 * u, 42 * u, 42 * u);
        bulb.AddRectangle(new RectangleF(cx - 11 * u, cy + 10 * u, 22 * u, 12 * u));
        if (t.Neon)
            Gfx.Neon(g, bulb, core, u, b > 0.05f);
        else
        {
            using var bb = new SolidBrush(core);
            g.FillPath(bb, bulb);
        }
        // highlight
        using (var hb = new SolidBrush(Color.FromArgb((int)(40 + 110 * b), Color.White)))
            g.FillEllipse(hb, cx - 13 * u, cy - 17 * u, 12 * u, 16 * u);

        // percent
        using var f = new Font(t.HeadFont, Math.Max(9f, 11f * u), FontStyle.Bold);
        using var tb = new SolidBrush(_brightness > 0 ? Gfx.Mix(t.Text, _color, 0.5f) : t.Disabled);
        string s = _brightness + "%";
        var sz = g.MeasureString(s, f);
        g.DrawString(s, f, tb, cx - sz.Width / 2f, Height - sz.Height - 2 * u);
    }
}
