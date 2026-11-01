using System.Drawing.Drawing2D;

namespace SerialMonitor;

// =========================================================================
// Animated toggle switch (knob slides, glow pulses when ON)
// =========================================================================
public sealed class SwitchControl : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 20 };
    private AppTheme _t = AppTheme.AI;
    private float _pos;      // 0 = off, 1 = on
    private float _phase;
    private bool _on;

    public string Caption { get; set; } = "";
    public event Action<bool>? ToggleRequested;

    public AppTheme Skin
    {
        get => _t;
        set { _t = value; BackColor = value.Card; Invalidate(); }
    }

    public bool IsOn
    {
        get => _on;
        set { _on = value; Invalidate(); }
    }

    public SwitchControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.StandardClick, true);
        BackColor = _t.Card;
        Cursor = Cursors.Hand;
        _timer.Tick += (_, _) =>
        {
            float target = _on ? 1f : 0f;
            bool changed = false;
            if (Math.Abs(target - _pos) > 0.01f) { _pos += (target - _pos) * 0.28f; changed = true; }
            else if (_pos != target) { _pos = target; changed = true; }
            if (_on) { _phase += 0.12f; changed = true; }
            if (changed) Invalidate();
        };
        _timer.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (Enabled && e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location))
            ToggleRequested?.Invoke(!_on);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        float u = Math.Min(Width / 64f, Height / 84f);
        if (u <= 0) return;
        var t = _t;
        float cx = Width / 2f;
        float pulse = (MathF.Sin(_phase) + 1f) / 2f;

        // caption
        using (var cf = new Font(t.HeadFont, Math.Max(7f, 9f * u), FontStyle.Bold))
        using (var cb = new SolidBrush(t.TitleColor))
        {
            var sz = g.MeasureString(Caption, cf);
            g.DrawString(Caption, cf, cb, cx - sz.Width / 2f, 2 * u);
        }

        // track (vertical-ish pill, horizontal slide)
        float tw = 44 * u, th = 22 * u;
        float tx = cx - tw / 2f, ty = Height / 2f - th / 2f + 2 * u;
        Color offC = Enabled ? t.OffCol : t.Disabled;
        Color onC = Enabled ? t.OnCol : t.Disabled;
        Color trackC = Gfx.Mix(Gfx.Mix(offC, t.Card, 0.45f), Gfx.Mix(onC, t.Card, 0.10f), _pos);

        if (_pos > 0.05f && Enabled)
        {
            using var gp = new Pen(Color.FromArgb((int)(30 + 50 * pulse), onC), 7 * u);
            using var gpath = Gfx.RoundRect(new RectangleF(tx, ty, tw, th), th / 2f);
            g.DrawPath(gp, gpath);
        }
        using (var track = Gfx.RoundRect(new RectangleF(tx, ty, tw, th), th / 2f))
        {
            if (t.Neon)
                Gfx.Neon(g, track, Gfx.Mix(offC, onC, _pos), u, _on);
            else
            {
                using var tb = new SolidBrush(trackC);
                g.FillPath(tb, track);
            }
        }

        // knob
        float kr = th / 2f - 2.5f * u;
        float kx = tx + th / 2f + (tw - th) * _pos;
        float ky = ty + th / 2f;
        using (var shadow = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
            g.FillEllipse(shadow, kx - kr, ky - kr + 1.5f * u, kr * 2, kr * 2);
        using (var kb = new SolidBrush(t.Neon ? Gfx.Mix(offC, onC, _pos) : Color.White))
            g.FillEllipse(kb, kx - kr, ky - kr, kr * 2, kr * 2);

        // state text
        using (var sf = new Font(t.HeadFont, Math.Max(7f, 8.5f * u), FontStyle.Bold))
        using (var sb = new SolidBrush(_on ? onC : offC))
        {
            string s = _on ? "ON" : "OFF";
            var sz = g.MeasureString(s, sf);
            g.DrawString(s, sf, sb, cx - sz.Width / 2f, ty + th + 6 * u);
        }
    }
}
