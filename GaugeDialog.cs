using System.Globalization;

namespace SerialMonitor;

// =========================================================================
// Small themed dialog to add a custom sensor widget
// =========================================================================
internal sealed class GaugeDialog : Form
{
    private readonly TextBox _key = new() { Text = "SENSOR1" };
    private readonly TextBox _title = new() { Text = "Sensor" };
    private readonly TextBox _unit = new() { Text = "" };
    private readonly TextBox _min = new() { Text = "0" };
    private readonly TextBox _max = new() { Text = "100" };
    private readonly CheckBox _invert = new() { Text = "Low value = danger (battery, distance...)", AutoSize = true };

    public string KeyName => _key.Text.Trim().ToUpperInvariant();
    public string Title2 => _title.Text.Trim();
    public string UnitText => _unit.Text.Trim();
    public double Min { get; private set; }
    public double Max { get; private set; }
    public bool Invert => _invert.Checked;

    public GaugeDialog(AppTheme t)
    {
        Text = "Add sensor widget";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(360, 292);
        BackColor = t.Bg;
        ForeColor = t.Text;
        Font = new Font(t.UiFont, 9.5f);

        var rows = new (string label, Control box)[]
        {
            ("Key (Arduino sends  KEY:value)", _key),
            ("Title", _title),
            ("Unit", _unit),
            ("Min", _min),
            ("Max", _max)
        };
        int y = 14;
        foreach (var (label, box) in rows)
        {
            Controls.Add(new Label { Text = label, Left = 16, Top = y + 4, Width = 170, ForeColor = t.Text });
            box.Left = 190; box.Top = y; box.Width = 150;
            box.BackColor = t.InputBg; box.ForeColor = t.InputText;
            ((TextBox)box).BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(box);
            y += 34;
        }
        _invert.Left = 16; _invert.Top = y + 2; _invert.ForeColor = t.Text;
        Controls.Add(_invert);

        var ok = new Button { Text = "Add", Left = 170, Top = 244, Width = 80, Height = 32, DialogResult = DialogResult.None };
        var cancel = new Button { Text = "Cancel", Left = 260, Top = 244, Width = 80, Height = 32, DialogResult = DialogResult.Cancel };
        foreach (var b in new[] { ok, cancel })
        {
            b.FlatStyle = FlatStyle.Flat;
            b.UseVisualStyleBackColor = false;
            b.FlatAppearance.BorderColor = t.CardBorder;
            b.BackColor = b == ok ? t.Accent : t.InputBg;
            b.ForeColor = b == ok ? t.AccentText : t.InputText;
            Controls.Add(b);
        }
        ok.Click += (_, _) => TryAccept();
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void TryAccept()
    {
        var inv = CultureInfo.InvariantCulture;
        if (KeyName.Length == 0 || !KeyName.All(c => char.IsLetterOrDigit(c) || c == '_') || char.IsDigit(KeyName[0]))
        {
            MessageBox.Show("Key letters / numbers / _ mattum (number-la start aaga koodathu).");
            return;
        }
        if (!double.TryParse(_min.Text, NumberStyles.Float, inv, out double mn) ||
            !double.TryParse(_max.Text, NumberStyles.Float, inv, out double mx) || mx <= mn)
        {
            MessageBox.Show("Min / Max correct-a kudunga (Max > Min).");
            return;
        }
        Min = mn; Max = mx;
        DialogResult = DialogResult.OK;
    }
}
