using System.Drawing.Drawing2D;
using System.Globalization;

namespace SerialMonitor;

// =========================================================================
// Animated GPS radar: sweeping beam, pulsing position marker, trail, lat/long readout
// =========================================================================
public sealed class GpsControl : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 30 };
    private readonly List<(double lat, double lon)> _trail = new();
    private AppTheme _t = AppTheme.AI;
    private float _sweep, _pulse;
    private bool _fix;
    private double _lat, _lon;
    private DateTime _lastUpdate = DateTime.MinValue;

    public AppTheme Skin
    {
        get => _t;
        set { _t = value; BackColor = value.Card; Invalidate(); }
    }

    public bool HasFix => _fix;

    public void SetPosition(double lat, double lon)
    {
        _lat = lat; _lon = lon; _fix = true;
        _lastUpdate = DateTime.Now;
        if (_trail.Count == 0 || _trail[^1] != (lat, lon)) _trail.Add((lat, lon));
        if (_trail.Count > 60) _trail.RemoveAt(0);
    }

    public void Reset()
    {
        _fix = false;
        _trail.Clear();
        Invalidate();
    }

    public GpsControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = _t.Card;
        _timer.Tick += (_, _) =>
        {
            _sweep = (_sweep + 3.2f) % 360f;
            _pulse = (_pulse + 0.035f) % 1f;
            Invalidate();
        };
        _timer.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }

    private static string Fmt(double v, string pos, string neg)
        => Math.Abs(v).ToString("0.000000", CultureInfo.InvariantCulture) + "° " + (v >= 0 ? pos : neg);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);
        var t = _t;

        float textH = Math.Max(54f, Height * 0.30f);
        float areaH = Height - textH;
        float R = Math.Min(Width, areaH) / 2f - 8f;
        if (R < 10) return;
        float cx = Width / 2f, cy = areaH / 2f + 2f;
        Color ring = Color.FromArgb(120, t.IconOn1);

        // dish
        using (var fill = new SolidBrush(Gfx.Mix(t.Card, Color.Black, 0.30f)))
            g.FillEllipse(fill, cx - R, cy - R, R * 2, R * 2);
        using (var rp = new Pen(ring, 1.2f))
        {
            for (int k = 1; k <= 3; k++)
            {
                float rr = R * k / 3f;
                g.DrawEllipse(rp, cx - rr, cy - rr, rr * 2, rr * 2);
            }
            g.DrawLine(rp, cx - R, cy, cx + R, cy);
            g.DrawLine(rp, cx, cy - R, cx, cy + R);
        }
        using (var outer = new Pen(t.IconOn1, 2f)) g.DrawEllipse(outer, cx - R, cy - R, R * 2, R * 2);

        // sweep with fading tail
        for (int k = 0; k < 14; k++)
        {
            using var sb = new SolidBrush(Color.FromArgb(Math.Max(0, 85 - k * 6), t.IconOn1));
            g.FillPie(sb, cx - R, cy - R, R * 2, R * 2, _sweep - k * 4f, 4.5f);
        }
        float a = _sweep * MathF.PI / 180f;
        using (var lp = new Pen(t.IconOn1, 2f))
            g.DrawLine(lp, cx, cy, cx + MathF.Cos(a) * R, cy + MathF.Sin(a) * R);

        // satellites (orbiting dots around dish, show when fix)
        for (int k = 0; k < 4; k++)
        {
            float sa = (_sweep * 0.4f + k * 90f) * MathF.PI / 180f;
            float sr = R + 1f;
            float sx = cx + MathF.Cos(sa) * sr, sy = cy + MathF.Sin(sa) * sr;
            using var sb = new SolidBrush(_fix ? (k % 2 == 0 ? t.IconOn1 : t.IconOn2) : t.IconOff);
            g.FillEllipse(sb, sx - 3.5f, sy - 3.5f, 7f, 7f);
        }

        if (_fix)
        {
            // auto-scaled trail
            double lat0 = _trail[0].lat, lon0 = _trail[0].lon;
            double maxDev = 1e-5;
            foreach (var p in _trail)
                maxDev = Math.Max(maxDev, Math.Max(Math.Abs(p.lat - lat0), Math.Abs(p.lon - lon0)));
            double scale = R * 0.75 / maxDev;
            PointF ToPt((double lat, double lon) p) =>
                new(cx + (float)((p.lon - lon0) * scale), cy - (float)((p.lat - lat0) * scale));

            if (_trail.Count > 1)
            {
                var pts = _trail.Select(ToPt).ToArray();
                using var tp = new Pen(Color.FromArgb(160, t.IconOn2), 2f) { LineJoin = LineJoin.Round };
                g.DrawLines(tp, pts);
            }
            var cur = ToPt((_lat, _lon));

            // pulsing rings
            for (int k = 0; k < 3; k++)
            {
                float ph = (_pulse + k / 3f) % 1f;
                float rr = 5f + ph * 26f;
                using var pp = new Pen(Color.FromArgb((int)(200 * (1f - ph)), t.OnCol), 2f);
                g.DrawEllipse(pp, cur.X - rr, cur.Y - rr, rr * 2, rr * 2);
            }
            using (var mb = new SolidBrush(t.OnCol)) g.FillEllipse(mb, cur.X - 5, cur.Y - 5, 10, 10);
            using (var mw = new Pen(Color.White, 1.5f)) g.DrawEllipse(mw, cur.X - 5, cur.Y - 5, 10, 10);
        }

        // readout
        using var hf = new Font(t.HeadFont, 9f, FontStyle.Bold);
        using var vf = new Font(t.LogFont, Math.Max(11f, Math.Min(15f, Width / 22f)), FontStyle.Bold);
        using var tb = new SolidBrush(t.TitleColor);
        using var vb = new SolidBrush(_fix ? t.Text : t.Disabled);
        float ty = areaH + 4f;

        if (_fix)
        {
            g.DrawString("LATITUDE", hf, tb, 10, ty);
            g.DrawString(Fmt(_lat, "N", "S"), vf, vb, 10, ty + 16f);
            g.DrawString("LONGITUDE", hf, tb, Width / 2f + 6, ty);
            g.DrawString(Fmt(_lon, "E", "W"), vf, vb, Width / 2f + 6, ty + 16f);
            double age = (DateTime.Now - _lastUpdate).TotalSeconds;
            string st = age < 5 ? "● LIVE" : $"● {age:0}s ago";
            using var sb2 = new SolidBrush(age < 5 ? t.OnCol : t.OffCol);
            g.DrawString(st, hf, sb2, 10, ty + 16f + vf.Height + 2f);
        }
        else
        {
            bool blink = ((int)(_sweep / 60f)) % 2 == 0;
            using var sb2 = new SolidBrush(blink ? t.OffCol : Gfx.Mix(t.OffCol, t.Card, 0.6f));
            g.DrawString("● SEARCHING FOR SIGNAL...", hf, sb2, 10, ty);
            g.DrawString("LAT  --.------°", vf, vb, 10, ty + 18f);
            g.DrawString("LON  --.------°", vf, vb, Width / 2f + 6, ty + 18f);
        }
    }
}
