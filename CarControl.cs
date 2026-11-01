using System.Drawing.Drawing2D;

namespace SerialMonitor;

public enum CarDir { Stop, Forward, Back, Left, Right }

// =========================================================================
// Animated top-down car: scrolling road, headlight beams, tail lights, steering tilt
// =========================================================================
public sealed class CarControl : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 30 };
    private AppTheme _t = AppTheme.AI;
    private CarDir _dir = CarDir.Stop;
    private float _road, _phase, _tilt, _tiltTarget;

    public AppTheme Skin
    {
        get => _t;
        set { _t = value; BackColor = value.Card; Invalidate(); }
    }

    public CarDir Dir
    {
        get => _dir;
        set
        {
            _dir = value;
            _tiltTarget = value == CarDir.Left ? -16f : value == CarDir.Right ? 16f : 0f;
            Invalidate();
        }
    }

    public CarControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = _t.Card;
        _timer.Tick += (_, _) =>
        {
            _phase += 0.2f;
            bool moving = _dir != CarDir.Stop;
            if (moving)
                _road += _dir == CarDir.Back ? -7f : 7f;
            float dt = _tiltTarget - _tilt;
            if (Math.Abs(dt) > 0.05f) _tilt += dt * 0.18f; else _tilt = _tiltTarget;
            if (moving || Math.Abs(dt) > 0.05f) Invalidate();
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

        float u = Math.Min(Width / 100f, Height / 110f);
        if (u <= 0) return;
        float cx = Width / 2f, cy = Height / 2f;
        var t = _t;
        bool moving = _dir != CarDir.Stop;

        // ---- road ----
        var road = new RectangleF(cx - 40 * u, 0, 80 * u, Height);
        using (var rb = new SolidBrush(Gfx.Mix(t.Card, Color.Black, 0.35f))) g.FillRectangle(rb, road);
        using (var edge = new Pen(t.GuardLine, Math.Max(1.5f, 2 * u)))
        {
            g.DrawLine(edge, road.Left, 0, road.Left, Height);
            g.DrawLine(edge, road.Right, 0, road.Right, Height);
        }
        float period = 26 * u;
        float off = ((_road * u * 0.5f % period) + period) % period;
        using (var dash = new Pen(Color.FromArgb(moving ? 200 : 90, t.IconOn2), Math.Max(2f, 3 * u)))
        {
            for (float y = -period + off; y < Height; y += period)
                g.DrawLine(dash, cx, y, cx, y + 13 * u);
        }

        // ---- car (rotated by steering tilt) ----
        var st = g.Save();
        g.TranslateTransform(cx, cy);
        g.RotateTransform(_tilt);

        Color bodyC = moving ? t.IconOn1 : Gfx.Mix(t.IconOff, t.IconOn1, 0.35f);
        Color roofC = moving ? Gfx.Mix(t.IconOn1, Color.Black, 0.25f) : t.IconOff;
        Color glassC = Gfx.Mix(t.Card, Color.Black, 0.55f);

        // headlight beams
        if (_dir == CarDir.Forward || _dir == CarDir.Left || _dir == CarDir.Right)
        {
            float flick = 40 + 25 * (MathF.Sin(_phase) + 1f) / 2f;
            using var beam = new SolidBrush(Color.FromArgb((int)flick, t.IconGlow));
            g.FillPolygon(beam, new[]
            {
                new PointF(-11 * u, -27 * u), new PointF(-20 * u, -62 * u), new PointF(-3 * u, -62 * u)
            });
            g.FillPolygon(beam, new[]
            {
                new PointF(11 * u, -27 * u), new PointF(3 * u, -62 * u), new PointF(20 * u, -62 * u)
            });
        }

        // wheels
        using (var wb = new SolidBrush(Color.FromArgb(235, 18, 20, 24)))
        {
            foreach (float wx in new[] { -19f, 14f })
                foreach (float wy in new[] { -20f, 10f })
                {
                    using var wp = Gfx.RoundRect(new RectangleF(wx * u, wy * u, 5 * u, 13 * u), 2 * u);
                    g.FillPath(wb, wp);
                }
        }

        using var body = Gfx.RoundRect(new RectangleF(-14 * u, -27 * u, 28 * u, 54 * u), 8 * u);
        using var roof = Gfx.RoundRect(new RectangleF(-10 * u, -6 * u, 20 * u, 20 * u), 4 * u);
        using var wind = Gfx.RoundRect(new RectangleF(-10 * u, -19 * u, 20 * u, 11 * u), 3 * u);
        using var rear = Gfx.RoundRect(new RectangleF(-9 * u, 15 * u, 18 * u, 7 * u), 2 * u);

        if (t.Neon)
        {
            Gfx.Neon(g, body, bodyC, u, moving);
            Gfx.Neon(g, roof, roofC, u, false);
        }
        else
        {
            using (var b = new SolidBrush(bodyC)) g.FillPath(b, body);
            using (var b = new SolidBrush(roofC)) g.FillPath(b, roof);
        }
        using (var b = new SolidBrush(glassC))
        {
            g.FillPath(b, wind);
            g.FillPath(b, rear);
        }

        // head lights
        Color head = moving && _dir != CarDir.Back ? Gfx.Mix(t.IconGlow, Color.White, 0.6f) : Gfx.Mix(t.IconOff, Color.White, 0.2f);
        using (var hb = new SolidBrush(head))
        {
            g.FillEllipse(hb, -12 * u, -27 * u, 6 * u, 4 * u);
            g.FillEllipse(hb, 6 * u, -27 * u, 6 * u, 4 * u);
        }
        // tail lights (bright while reversing / stopped brake)
        bool tailOn = _dir == CarDir.Back || _dir == CarDir.Stop;
        Color tail = tailOn ? t.OffCol : Gfx.Mix(t.OffCol, t.Card, 0.6f);
        using (var tb = new SolidBrush(tail))
        {
            g.FillRectangle(tb, -12 * u, 23 * u, 6 * u, 3 * u);
            g.FillRectangle(tb, 6 * u, 23 * u, 6 * u, 3 * u);
        }
        g.Restore(st);

        // ---- direction label ----
        string arrow = _dir switch
        {
            CarDir.Forward => "▲ FWD",
            CarDir.Back => "▼ REV",
            CarDir.Left => "◀ LEFT",
            CarDir.Right => "RIGHT ▶",
            _ => "■ STOP"
        };
        using var f = new Font(t.HeadFont, Math.Max(8f, 9f * u), FontStyle.Bold);
        using var tbr = new SolidBrush(moving ? t.OnCol : t.OffCol);
        g.DrawString(arrow, f, tbr, 6, 4);

        Gfx.Decor(g, t, Width, Height, cx, cy, 44 * u, 50 * u, Width - 26 * u, Height - 22 * u, u * 0.9f, _phase, moving);
    }
}
