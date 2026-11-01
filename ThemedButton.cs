namespace SerialMonitor;

// =========================================================================
// Button that keeps theme colours when DISABLED.
// (WinForms ignores ForeColor for disabled Flat buttons and draws system gray/black text.)
// =========================================================================
public class ThemedButton : Button
{
    protected override void OnPaint(PaintEventArgs e)
    {
        if (Enabled)
        {
            base.OnPaint(e);
            return;
        }

        var g = e.Graphics;
        using (var bg = new SolidBrush(BackColor)) g.FillRectangle(bg, ClientRectangle);

        int bs = FlatAppearance.BorderSize;
        if (bs > 0)
        {
            using var pen = new Pen(FlatAppearance.BorderColor.IsEmpty ? ForeColor : FlatAppearance.BorderColor, bs);
            g.DrawRectangle(pen, bs / 2f, bs / 2f, Width - bs, Height - bs);
        }

        // theme text colour, just dimmed so it still looks "disabled"
        Color txt = Gfx.Mix(ForeColor, BackColor, 0.40f);
        TextRenderer.DrawText(g, Text, Font, ClientRectangle, txt,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
    }
}
