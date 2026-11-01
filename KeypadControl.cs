using System.Drawing.Drawing2D;

namespace SerialMonitor;

// =========================================================================
// Animated 4x4 keypad (hover glow + press ripple)
// =========================================================================
public sealed class KeypadControl : Control
{
    private static readonly string[] Keys16 =
    {
        "1", "2", "3", "A",
        "4", "5", "6", "B",
        "7", "8", "9", "C",
        "*", "0", "#", "D"
    };

    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 20 };
    private readonly float[] _flash = new float[16];
    private readonly float[] _hoverAmt = new float[16];
    private AppTheme _t = AppTheme.AI;
    private int _hover = -1;

    public event Action<string>? KeyPressed;

    public AppTheme Skin
    {
        get => _t;
        set { _t = value; BackColor = value.Card; Invalidate(); }
    }

    public KeypadControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = _t.Card;
        _timer.Tick += (_, _) =>
        {
            bool changed = false;
            for (int i = 0; i < 16; i++)
            {
                if (_flash[i] > 0f) { _flash[i] = Math.Max(0f, _flash[i] - 0.045f); changed = true; }
                float target = i == _hover && Enabled ? 1f : 0f;
                if (Math.Abs(_hoverAmt[i] - target) > 0.01f)
                {
                    _hoverAmt[i] += (target - _hoverAmt[i]) * 0.3f;
                    changed = true;
                }
            }
            if (changed) Invalidate();
        };
        _timer.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }

    private RectangleF Cell(int i)
    {
        float gap = 6f;
        float cw = (Width - gap * 5) / 4f, ch = (Height - gap * 5) / 4f;
        int r = i / 4, c = i % 4;
        return new RectangleF(gap + c * (cw + gap), gap + r * (ch + gap), cw, ch);
    }

    private int HitTest(Point p)
    {
        for (int i = 0; i < 16; i++)
            if (Cell(i).Contains(p)) return i;
        return -1;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _hover = HitTest(e.Location);
        Cursor = _hover >= 0 && Enabled ? Cursors.Hand : Cursors.Default;
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = -1;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!Enabled || e.Button != MouseButtons.Left) return;
        int i = HitTest(e.Location);
        if (i < 0) return;
        _flash[i] = 1f;
        KeyPressed?.Invoke(Keys16[i]);
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

        for (int i = 0; i < 16; i++)
        {
            var r = Cell(i);
            if (r.Width <= 2 || r.Height <= 2) continue;
            float f = _flash[i], h = _hoverAmt[i];
            bool letter = char.IsLetter(Keys16[i][0]);
            Color baseC = letter ? t.IconOn2 : t.IconOn1;
            Color edge = Enabled ? baseC : t.Disabled;

            using var p = Gfx.RoundRect(r, Math.Min(r.Width, r.Height) * 0.2f);
            Color fill = t.Neon
                ? Gfx.Mix(t.InputBg, edge, 0.08f + 0.25f * h + 0.55f * f)
                : Gfx.Mix(Gfx.Mix(t.InputBg, t.Card, 0.4f), edge, 0.14f + 0.25f * h + 0.60f * f);
            using (var fb = new SolidBrush(fill)) g.FillPath(fb, p);

            if (f > 0f)
            {
                using var gl = new Pen(Color.FromArgb((int)(120 * f), edge), 6f);
                g.DrawPath(gl, p);
                // ripple
                float rad = (1f - f) * Math.Max(r.Width, r.Height) * 0.7f;
                float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
                using var rp = new Pen(Color.FromArgb((int)(200 * f), Color.White), 2f);
                var st = g.Save();
                g.SetClip(p, CombineMode.Intersect);
                g.DrawEllipse(rp, cx - rad, cy - rad, rad * 2, rad * 2);
                g.Restore(st);
            }
            using (var bp = new Pen(Color.FromArgb(Enabled ? 200 : 110, edge), 1.5f)) g.DrawPath(bp, p);

            using var font = new Font(t.HeadFont, Math.Max(10f, Math.Min(r.Width, r.Height) * 0.32f), FontStyle.Bold, GraphicsUnit.Pixel);
            Color txt = Enabled ? Gfx.Mix(t.Text, Color.White, f) : t.Disabled;
            using var tb = new SolidBrush(txt);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(Keys16[i], font, tb, r, sf);
        }
    }
}
