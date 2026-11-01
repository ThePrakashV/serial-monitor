using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;

namespace SerialMonitor;

// =========================================================================
// THEMES  (12 tech domains)
// =========================================================================
public enum DecorStyle
{
    // ambient (around the icon)
    Orbit, Matrix, Scan, Qubit,
    // small animated badge (corner of the icon)
    Signal, Gear, Cloud, Bars, Chain, Chip, Packets, Edge
}

public sealed class AppTheme
{
    public string Name, Title, LightTitle, FanTitle;
    public string UiFont, HeadFont, LogFont;
    public bool OutlineButtons, GlowCards, Neon;
    public DecorStyle Decor;

    public Color Bg, TopBar, TopText, Card, CardBorder, TitleColor, Text;
    public Color InputBg, InputText, OnCol, OffCol, Disabled, Accent, AccentText, BtnText;
    public Color LogBg, LogRx, LogTx, LogSys;
    public Color IconOn1, IconOn2, IconOff, IconGlow, Guard, GuardLine, Hub;

    private static Color C(int r, int g, int b) => Color.FromArgb(r, g, b);
    private static float Lum(Color c) => 0.299f * c.R + 0.587f * c.G + 0.114f * c.B;
    private static Color Contrast(Color c) => Lum(c) > 140 ? C(12, 16, 24) : Color.White;

    // Only the key colors are given; everything else is derived.
    private AppTheme(string name, string title, string lightTitle, string fanTitle,
        bool dark, Color bg, Color top, Color card, Color border, Color i1, Color i2, Color on, Color off,
        DecorStyle decor, bool neon, bool outline, bool glowCards,
        string ui, string head, string log, Color? glow = null)
    {
        Name = name; Title = title; LightTitle = lightTitle; FanTitle = fanTitle;
        UiFont = ui; HeadFont = head; LogFont = log;
        Decor = decor; Neon = neon; OutlineButtons = outline; GlowCards = glowCards;

        Bg = bg; TopBar = top; Card = card; CardBorder = border;
        IconOn1 = i1; IconOn2 = i2; OnCol = on; OffCol = off;
        IconGlow = glow ?? i1;

        Text = dark ? Gfx.Mix(C(228, 234, 246), i1, 0.12f) : C(38, 50, 62);
        TopText = neon ? i1 : (Lum(top) < 140 ? Color.White : C(20, 30, 40));
        TitleColor = neon ? i1 : (dark ? Gfx.Mix(i1, Color.White, 0.35f) : Gfx.Mix(i1, Color.Black, 0.45f));
        InputBg = dark ? Gfx.Mix(bg, Color.Black, 0.35f) : Color.White;
        InputText = dark ? Text : C(20, 30, 40);
        Disabled = dark ? Gfx.Mix(card, Color.White, 0.22f) : C(176, 190, 197);
        Accent = i1;
        AccentText = Contrast(i1);
        BtnText = Contrast(Gfx.Mix(on, off, 0.5f));

        LogBg = dark ? Gfx.Mix(bg, Color.Black, 0.45f) : C(19, 34, 43);
        LogRx = neon ? i1 : (dark ? Gfx.Mix(Color.White, i1, 0.15f) : C(224, 242, 241));
        LogTx = Gfx.Mix(i2, Color.White, 0.25f);
        LogSys = C(120, 135, 150);

        IconOff = dark ? Gfx.Mix(card, Color.White, 0.28f) : C(176, 190, 197);
        Guard = neon ? Gfx.Mix(card, Color.Black, 0.30f)
                     : (dark ? Gfx.Mix(card, Color.White, 0.08f) : Gfx.Mix(card, Color.Black, 0.04f));
        GuardLine = neon ? i1 : (dark ? Gfx.Mix(card, Color.White, 0.40f) : C(96, 125, 139));
        Hub = neon ? bg : (dark ? bg : Gfx.Mix(i1, Color.Black, 0.55f));
    }

    // 1. AI -- indigo + violet / sky-blue, orbiting "neurons"
    public static readonly AppTheme AI = new("AI", "AI Control Console", "Light Agent", "Fan Agent",
        dark: true, bg: C(13, 14, 32), top: C(20, 22, 48), card: C(26, 29, 62), border: C(88, 80, 200),
        i1: C(139, 92, 246), i2: C(56, 189, 248), on: C(52, 211, 153), off: C(251, 113, 133),
        decor: DecorStyle.Orbit, neon: false, outline: false, glowCards: true,
        ui: "Segoe UI", head: "Segoe UI Semibold", log: "Consolas");

    // 2. Cybersecurity -- black + matrix green / alert red, falling code, neon outlines
    public static readonly AppTheme Cyber = new("Cybersecurity", "Cybersecurity // SOC Terminal", "root@light:~#", "root@fan:~#",
        dark: true, bg: C(5, 10, 7), top: C(8, 16, 11), card: C(9, 20, 13), border: C(0, 255, 65),
        i1: C(0, 255, 65), i2: C(255, 40, 60), on: C(0, 255, 65), off: C(255, 40, 60),
        decor: DecorStyle.Matrix, neon: true, outline: true, glowCards: true,
        ui: "Consolas", head: "Consolas", log: "Consolas");

    // 3. IoT -- clean light dashboard, teal + blue, Wi-Fi signal badge
    public static readonly AppTheme IoT = new("IoT", "IoT Device Dashboard", "Smart Light  ·  Node 01", "Smart Fan  ·  Node 02",
        dark: false, bg: C(234, 242, 247), top: C(0, 121, 107), card: Color.White, border: C(200, 218, 228),
        i1: C(0, 150, 199), i2: C(255, 193, 7), on: C(0, 168, 107), off: C(229, 57, 53),
        decor: DecorStyle.Signal, neon: false, outline: false, glowCards: false,
        ui: "Segoe UI", head: "Segoe UI Semibold", log: "Consolas", glow: C(255, 200, 40));

    // 4. Robotics & Automation -- steel gray + safety orange, rotating gear
    public static readonly AppTheme Robotics = new("Robotics", "Robotics & Automation Control Panel", "Automation Light  ·  Cell A", "Cooling Fan  ·  Cell B",
        dark: true, bg: C(30, 32, 36), top: C(22, 24, 27), card: C(44, 47, 53), border: C(255, 152, 0),
        i1: C(255, 152, 0), i2: C(255, 214, 0), on: C(76, 175, 80), off: C(244, 67, 54),
        decor: DecorStyle.Gear, neon: false, outline: false, glowCards: false,
        ui: "Segoe UI", head: "Segoe UI Semibold", log: "Consolas", glow: C(255, 193, 7));

    // 5. Cloud Computing -- sky blue light theme, uploading cloud badge
    public static readonly AppTheme Cloud = new("Cloud", "Cloud Console", "Cloud Light  ·  Instance 1", "Cloud Fan  ·  Instance 2",
        dark: false, bg: C(232, 243, 255), top: C(25, 118, 210), card: Color.White, border: C(187, 222, 251),
        i1: C(33, 150, 243), i2: C(144, 202, 249), on: C(0, 184, 148), off: C(255, 82, 82),
        decor: DecorStyle.Cloud, neon: false, outline: false, glowCards: false,
        ui: "Segoe UI", head: "Segoe UI Semibold", log: "Consolas", glow: C(255, 214, 10));

    // 6. Data Science & Big Data -- slate navy + teal / orange, live bar chart badge
    public static readonly AppTheme Data = new("Data Science", "Data Science Dashboard", "Light  ·  Stream 1", "Fan  ·  Stream 2",
        dark: true, bg: C(15, 23, 42), top: C(10, 16, 32), card: C(22, 33, 62), border: C(45, 212, 191),
        i1: C(45, 212, 191), i2: C(251, 146, 60), on: C(52, 211, 153), off: C(248, 113, 113),
        decor: DecorStyle.Bars, neon: false, outline: false, glowCards: true,
        ui: "Segoe UI", head: "Segoe UI Semibold", log: "Consolas");

    // 7. Blockchain & Web3 -- dark purple + bitcoin orange, chain of blocks
    public static readonly AppTheme Blockchain = new("Blockchain", "Web3 Node Console", "Light Contract  ·  0x01", "Fan Contract  ·  0x02",
        dark: true, bg: C(14, 10, 28), top: C(22, 15, 42), card: C(28, 21, 52), border: C(247, 147, 26),
        i1: C(247, 147, 26), i2: C(130, 71, 229), on: C(34, 197, 94), off: C(239, 68, 68),
        decor: DecorStyle.Chain, neon: false, outline: false, glowCards: true,
        ui: "Segoe UI", head: "Segoe UI Semibold", log: "Consolas", glow: C(255, 190, 70));

    // 8. AR / VR / XR -- holographic cyan + pink, scanning line + viewfinder corners
    public static readonly AppTheme XR = new("AR/VR/XR", "XR Spatial Control", "Holo Light  ·  Layer 1", "Holo Fan  ·  Layer 2",
        dark: true, bg: C(10, 6, 25), top: C(16, 9, 38), card: C(22, 13, 48), border: C(236, 72, 153),
        i1: C(34, 211, 238), i2: C(236, 72, 153), on: C(74, 222, 128), off: C(244, 63, 94),
        decor: DecorStyle.Scan, neon: true, outline: true, glowCards: true,
        ui: "Segoe UI", head: "Segoe UI Semibold", log: "Consolas");

    // 9. Embedded Systems -- PCB green + gold traces, blinking microchip
    public static readonly AppTheme Embedded = new("Embedded", "Embedded // MCU Bench", "GPIO 13  ·  LED", "GPIO 09  ·  MOTOR",
        dark: true, bg: C(9, 32, 22), top: C(6, 24, 16), card: C(14, 50, 34), border: C(212, 175, 55),
        i1: C(230, 200, 80), i2: C(120, 220, 150), on: C(80, 230, 120), off: C(240, 90, 70),
        decor: DecorStyle.Chip, neon: false, outline: false, glowCards: false,
        ui: "Consolas", head: "Consolas", log: "Consolas", glow: C(255, 220, 90));

    // 10. Networking & Telecom -- navy + signal blue / green, packets travelling between nodes
    public static readonly AppTheme Network = new("Networking", "Network Operations Center", "Port 01  ·  Light", "Port 02  ·  Fan",
        dark: true, bg: C(9, 20, 38), top: C(6, 14, 28), card: C(14, 32, 58), border: C(41, 121, 255),
        i1: C(41, 182, 246), i2: C(102, 255, 153), on: C(0, 200, 120), off: C(255, 82, 82),
        decor: DecorStyle.Packets, neon: false, outline: false, glowCards: true,
        ui: "Segoe UI", head: "Segoe UI Semibold", log: "Consolas");

    // 11. Quantum Computing -- deep space + electric cyan / violet, atom orbits with electrons
    public static readonly AppTheme Quantum = new("Quantum", "Quantum Control Lab", "Qubit Q0  ·  Light", "Qubit Q1  ·  Fan",
        dark: true, bg: C(6, 8, 22), top: C(11, 14, 36), card: C(14, 17, 44), border: C(0, 229, 255),
        i1: C(0, 229, 255), i2: C(186, 104, 255), on: C(64, 255, 170), off: C(255, 90, 140),
        decor: DecorStyle.Qubit, neon: false, outline: false, glowCards: true,
        ui: "Segoe UI", head: "Segoe UI Semibold", log: "Consolas", glow: C(120, 200, 255));

    // 12. Edge Computing -- charcoal + lime / sky, edge node cluster
    public static readonly AppTheme Edge = new("Edge", "Edge Computing Gateway", "Edge Node A  ·  Light", "Edge Node B  ·  Fan",
        dark: true, bg: C(17, 20, 24), top: C(11, 13, 16), card: C(28, 33, 40), border: C(163, 230, 53),
        i1: C(163, 230, 53), i2: C(56, 189, 248), on: C(132, 204, 22), off: C(248, 81, 73),
        decor: DecorStyle.Edge, neon: false, outline: false, glowCards: false,
        ui: "Segoe UI", head: "Segoe UI Semibold", log: "Consolas", glow: C(190, 242, 100));

    public static readonly AppTheme[] All =
    {
        AI, Cyber, IoT, Robotics, Cloud, Data, Blockchain, XR, Embedded, Network, Quantum, Edge
    };
}

// =========================================================================
// MAIN FORM
// =========================================================================
public class MainForm : Form
{
    private AppTheme _t = AppTheme.AI;

    // ---------- Serial ----------
    private readonly SerialPort _port = new();
    private readonly StringBuilder _rxBuffer = new();
    private readonly System.Windows.Forms.Timer _flushTimer = new() { Interval = 300 };
    private DateTime _lastRx = DateTime.MinValue;

    // ---------- Connection controls ----------
    private readonly ComboBox _cmbPort = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
    private readonly ThemedButton _btnRefresh = new() { Text = "Refresh", AutoSize = true };
    private readonly ComboBox _cmbBaud = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90 };
    private readonly ThemedButton _btnConnect = new() { Text = "CONNECT", Width = 130, Height = 32 };
    private readonly ComboBox _cmbTheme = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };

    // ---------- Device buttons: [0]=Light ON, [1]=Light OFF, [2]=Fan ON, [3]=Fan OFF ----------
    private readonly ThemedButton[] _btns = new ThemedButton[4];
    private readonly TextBox[] _btnChars = new TextBox[4];
    private readonly LightControl _light = new() { Dock = DockStyle.Fill };
    private readonly FanControl _fan = new() { Dock = DockStyle.Fill };
    private readonly Label _lblLightState = new();
    private readonly Label _lblFanState = new();
    private readonly ToolTip _tip = new();

    // ---------- extra feature pages ----------
    private readonly Panel _pageHost = new() { Dock = DockStyle.Fill };
    private readonly List<Control> _pages = new();
    private readonly List<Button> _tabBtns = new();
    private int _tabIndex;

    // car
    private readonly CarControl _car = new() { Dock = DockStyle.Fill };
    private readonly Label _lblCarState = new();
    private readonly ThemedButton[] _carBtns = new ThemedButton[5];   // 0=Fwd 1=Back 2=Left 3=Right 4=Stop
    private readonly TextBox[] _carChars = new TextBox[5];
    private static readonly string[] CarCmds = { "U", "D", "L", "R", "S" };
    private static readonly string[] CarSyms = { "▲", "▼", "◀", "▶", "■" };
    private static readonly CarDir[] CarDirs = { CarDir.Forward, CarDir.Back, CarDir.Left, CarDir.Right, CarDir.Stop };

    // 10 switches
    private readonly SwitchControl[] _sw = new SwitchControl[10];

    // 4x4 keypad
    private readonly KeypadControl _keypad = new() { Dock = DockStyle.Fill };
    private readonly Label _lblLastKey = new();

    // GPS
    private readonly GpsControl _gps = new() { Dock = DockStyle.Fill };

    // sensor gauge widgets
    private readonly List<GaugeControl> _gauges = new();
    private FlowLayoutPanel _gaugeFlow = null!;
    private readonly Label _lblSensorState = new();
    private readonly Label _sensorHint = new();
    private readonly ThemedButton _btnAddGauge = new() { Text = "＋ Add widget", AutoSize = true };
    private static readonly Regex SensorKv = new(
        @"([A-Za-z_][A-Za-z0-9_]*)\s*[:=]\s*(-?\d+(?:\.\d+)?)", RegexOptions.Compiled);

    // 16-channel servo
    private readonly GradientSlider[] _servo = new GradientSlider[16];
    private readonly int[] _svVal = new int[16];
    private readonly bool[] _svDirty = new bool[16];
    private readonly ThemedButton _btnSvCenter = new() { Text = "Center 90°", AutoSize = false };
    private readonly ThemedButton _btnSvZero = new() { Text = "All 0°", AutoSize = false };
    private readonly ThemedButton _btnSvSweep = new() { Text = "Sweep: OFF", AutoSize = false };
    private readonly Label _gaugeEmpty = new();
    private readonly System.Windows.Forms.Timer _servoTimer = new() { Interval = 80 };
    private bool _svSweep;
    private float _svPhase;
    private int _svNext;

    // LED brightness + RGB picker
    private readonly LedControl _led = new() { Dock = DockStyle.Fill };
    private readonly ColorWheel _wheel = new() { Dock = DockStyle.Fill };
    private readonly GradientSlider _slBright = new() { Dock = DockStyle.Fill, Caption = "Brightness", Suffix = "%", Maximum = 100 };
    private readonly GradientSlider _slR = new() { Dock = DockStyle.Fill, Caption = "R", Maximum = 255 };
    private readonly GradientSlider _slG = new() { Dock = DockStyle.Fill, Caption = "G", Maximum = 255 };
    private readonly GradientSlider _slB = new() { Dock = DockStyle.Fill, Caption = "B", Maximum = 255 };
    private readonly Label _lblLedState = new();
    private readonly System.Windows.Forms.Timer _ledTimer = new() { Interval = 80 };
    private Color _ledColor = Color.FromArgb(255, 255, 255);
    private int _ledBright = 50;
    private bool _ledColorDirty, _ledBrightDirty;

    // ---------- themed containers ----------
    private FlowLayoutPanel _top = null!;
    private readonly List<Label> _topLabels = new();
    private readonly List<CardPanel> _cards = new();
    private readonly List<Label> _cardTitles = new();

    // ---------- Monitor ----------
    private readonly RichTextBox _log = new()
    {
        ReadOnly = true,
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        HideSelection = true,
        WordWrap = false,
        ScrollBars = RichTextBoxScrollBars.Both
    };
    private readonly TextBox _txtSend = new() { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11) };
    private readonly ThemedButton _btnSend = new() { Text = "Send", Dock = DockStyle.Right, Width = 90 };
    private readonly CheckBox _chkAutoScroll = new() { Text = "Autoscroll", Checked = true, AutoSize = true };
    private readonly CheckBox _chkTimestamp = new() { Text = "Show timestamp", Checked = true, AutoSize = true };
    private readonly ComboBox _cmbEnding = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
    private readonly ThemedButton _btnClear = new() { Text = "Clear output", AutoSize = true };

    private enum LogKind { Rx, Tx, System }

    public MainForm()
    {
        Size = new Size(1150, 680);
        MinimumSize = new Size(950, 560);
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();
        WireEvents();

        _cmbBaud.Items.AddRange(new object[] { "300", "1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200", "230400", "250000" });
        _cmbBaud.SelectedItem = "9600";

        _cmbEnding.Items.AddRange(new object[] { "No line ending", "Newline", "Carriage return", "Both NL & CR" });
        _cmbEnding.SelectedIndex = 1;

        for (int n = 1; n <= AppTheme.All.Length; n++) _cmbTheme.Items.Add(n.ToString()); // numbers only
        _cmbTheme.SelectedIndexChanged += (_, _) => ApplyTheme(AppTheme.All[_cmbTheme.SelectedIndex]);
        _cmbTheme.SelectedIndex = 0; // AI -> ApplyTheme

        RefreshPorts();
        UpdateUiState();
    }

    // =====================================================================
    // Theme apply
    // =====================================================================
    private void ApplyTheme(AppTheme t)
    {
        _t = t;
        SuspendLayout();

        Text = "Serial Monitor  -  Theme " + (Array.IndexOf(AppTheme.All, t) + 1);
        BackColor = t.Bg;
        ForeColor = t.Text;
        Font = new Font(t.UiFont, 9.5f);

        _top.BackColor = t.TopBar;
        foreach (var l in _topLabels)
        {
            l.ForeColor = t.TopText;
            l.Font = new Font(t.HeadFont, 10);
        }

        foreach (var c in _cards)
        {
            c.BackColor = t.Card;
            c.BorderColor = t.CardBorder;
            c.Glow = t.GlowCards;
            c.Invalidate();
        }
        foreach (var l in _cardTitles)
        {
            l.ForeColor = t.TitleColor;
            l.Font = new Font(t.HeadFont, 12, FontStyle.Bold);
        }
        _cardTitles[0].Text = t.LightTitle;
        _cardTitles[1].Text = t.FanTitle;

        foreach (var s in new[] { _lblLightState, _lblFanState, _lblCarState, _lblLastKey, _lblLedState })
            s.Font = new Font(t.HeadFont, 10, FontStyle.Bold);
        _lblLastKey.ForeColor = t.Accent;

        foreach (var tb in _btnChars.Concat(_carChars).Append(_txtSend))
        {
            tb.BackColor = t.InputBg;
            tb.ForeColor = t.InputText;
            tb.BorderStyle = BorderStyle.FixedSingle;
        }
        _txtSend.Font = new Font(t.UiFont, 11);

        foreach (var cb in new[] { _cmbPort, _cmbBaud, _cmbEnding, _cmbTheme })
        {
            cb.FlatStyle = FlatStyle.Flat;
            cb.BackColor = t.InputBg;
            cb.ForeColor = t.InputText;
        }

        StyleSmall(_btnRefresh);
        StyleSmall(_btnClear);

        _log.BackColor = t.LogBg;
        _log.ForeColor = t.LogRx;
        _log.Font = new Font(t.LogFont, 10);

        foreach (var b in _btns) b.Font = new Font(t.HeadFont, 16, FontStyle.Bold);
        _btnConnect.Font = new Font(t.HeadFont, 11, FontStyle.Bold);
        _btnSend.Font = new Font(t.HeadFont, 10, FontStyle.Bold);

        _light.Skin = t;
        _fan.Skin = t;
        _car.Skin = t;
        _keypad.Skin = t;
        _gps.Skin = t;
        foreach (var s in _sw) s.Skin = t;
        _led.Skin = t;
        _wheel.Skin = t;
        foreach (var s in new[] { _slBright, _slR, _slG, _slB }) s.Skin = t;
        RefreshLedUi();

        foreach (var gg in _gauges) gg.Skin = t;
        _sensorHint.ForeColor = Gfx.Mix(t.Text, t.Card, 0.4f);
        _sensorHint.Font = new Font(t.UiFont, 9f);
        _gaugeEmpty.BackColor = t.Card;
        _gaugeEmpty.ForeColor = Gfx.Mix(t.Text, t.Card, 0.45f);
        _gaugeEmpty.Font = new Font(t.HeadFont, 12f);
        UpdateSensorState();
        foreach (var s in _servo)
        {
            s.Skin = t;
            s.ColorLeft = Gfx.Mix(t.IconOn2, t.Card, 0.45f);
            s.ColorRight = t.IconOn1;
        }
        StyleSmall(_btnAddGauge);
        StyleSmall(_btnSvCenter);
        StyleSmall(_btnSvZero);
        RefreshSweepBtn();
        foreach (var b in _carBtns) b.Font = new Font("Segoe UI Symbol", 18, FontStyle.Bold);
        StyleTabs();

        ResumeLayout(true);

        UpdateUiState();
        SetLight(_light.IsOn);
        SetFan(_fan.IsOn);
        SetCar(_car.Dir);
    }

    private void StyleSmall(Button b)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.Cursor = Cursors.Hand;
        b.UseVisualStyleBackColor = false;
        b.Font = new Font(_t.HeadFont, 9.5f, FontStyle.Bold);
        b.TextAlign = ContentAlignment.MiddleCenter;
        b.UseCompatibleTextRendering = false;
        if (!b.AutoSize) b.Size = new Size(112, 32);
        b.FlatAppearance.BorderSize = 1;
        if (_t.OutlineButtons)
        {
            b.FlatAppearance.BorderColor = _t.IconOn1;
            b.BackColor = _t.Card;
            b.ForeColor = _t.IconOn1;
            b.FlatAppearance.MouseOverBackColor = Gfx.Mix(_t.Card, _t.IconOn1, 0.22f);
        }
        else
        {
            b.FlatAppearance.BorderColor = _t.CardBorder;
            b.BackColor = _t.InputBg;
            b.ForeColor = _t.InputText;
            b.FlatAppearance.MouseOverBackColor = Gfx.Mix(_t.InputBg, _t.CardBorder, 0.25f);
        }
    }

    // sweep button: same look in every theme, accent-filled while sweeping
    private void RefreshSweepBtn()
    {
        StyleSmall(_btnSvSweep);
        _btnSvSweep.Text = _svSweep ? "Sweep: ON" : "Sweep: OFF";
        if (_svSweep)
        {
            _btnSvSweep.BackColor = _t.Accent;
            _btnSvSweep.ForeColor = _t.AccentText;
            _btnSvSweep.FlatAppearance.BorderColor = _t.Accent;
            _btnSvSweep.FlatAppearance.MouseOverBackColor = Gfx.Mix(_t.Accent, Color.White, 0.2f);
        }
    }

    // big colored buttons: filled or neon outline
    private void PaintBtn(Button b, Color col, bool enabled, Color fillText)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.Cursor = enabled ? Cursors.Hand : Cursors.Default;
        b.UseVisualStyleBackColor = false;
        Color c = enabled ? col : _t.Disabled;

        if (_t.OutlineButtons)
        {
            b.FlatAppearance.BorderSize = 2;
            b.FlatAppearance.BorderColor = c;
            b.BackColor = _t.InputBg;
            b.ForeColor = c;
            b.FlatAppearance.MouseOverBackColor = Gfx.Mix(_t.InputBg, c, 0.22f);
            b.FlatAppearance.MouseDownBackColor = Gfx.Mix(_t.InputBg, c, 0.40f);
        }
        else
        {
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = c;
            b.ForeColor = enabled ? fillText : Color.White;
            b.FlatAppearance.MouseOverBackColor = Gfx.Mix(c, Color.White, 0.15f);
            b.FlatAppearance.MouseDownBackColor = Gfx.Mix(c, Color.Black, 0.15f);
        }
    }

    // =====================================================================
    // Layout
    // =====================================================================
    private void BuildLayout()
    {
        // ---- top bar ----
        _top = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 56,
            Padding = new Padding(14, 12, 10, 0),
            WrapContents = false
        };
        _top.Controls.Add(MakeLabel("Port:"));
        _top.Controls.Add(_cmbPort);
        _btnRefresh.Margin = new Padding(6, 1, 3, 0);
        _top.Controls.Add(_btnRefresh);
        _top.Controls.Add(MakeLabel("    Baud:"));
        _top.Controls.Add(_cmbBaud);
        _top.Controls.Add(new Label { Width = 24 });
        _btnConnect.Margin = new Padding(3, 0, 3, 0);
        _top.Controls.Add(_btnConnect);
        _top.Controls.Add(new Label { Width = 30 });
        _top.Controls.Add(MakeLabel("Theme:"));
        _top.Controls.Add(_cmbTheme);
        _cmbPort.Margin = new Padding(3, 3, 3, 0);
        _cmbBaud.Margin = new Padding(3, 3, 3, 0);
        _cmbTheme.Margin = new Padding(3, 3, 3, 0);

        // ---- body ----
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(14) };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));

        // left: Light card + Fan card
        var left = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        string[] defaults = { "1", "2", "3", "4" };
        for (int i = 0; i < 4; i++)
        {
            bool isOn = i % 2 == 0;
            _btns[i] = new ThemedButton { Text = isOn ? "ON" : "OFF", Dock = DockStyle.Fill, Margin = new Padding(4) };
            _btnChars[i] = new TextBox
            {
                Text = defaults[i],
                TextAlign = HorizontalAlignment.Center,
                Font = new Font("Segoe UI", 10),
                Dock = DockStyle.Fill,
                Margin = new Padding(14, 6, 14, 0)
            };
            _tip.SetToolTip(_btnChars[i], "Indha button click panna Arduino-ku anuppa vendiya char/text");
        }

        left.Controls.Add(MakeDeviceCard(_lblLightState, _light, _btns[0], _btnChars[0], _btns[1], _btnChars[1]), 0, 0);
        left.Controls.Add(MakeDeviceCard(_lblFanState, _fan, _btns[2], _btnChars[2], _btns[3], _btnChars[3]), 0, 1);

        // right: monitor
        var monitor = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 0, 0, 0) };

        var sendRow = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(0, 0, 0, 8) };
        sendRow.Controls.Add(_txtSend);
        sendRow.Controls.Add(_btnSend);

        var bottomBar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42, Padding = new Padding(0, 8, 0, 0) };
        bottomBar.Controls.Add(_chkAutoScroll);
        bottomBar.Controls.Add(_chkTimestamp);
        bottomBar.Controls.Add(_cmbEnding);
        bottomBar.Controls.Add(_btnClear);

        monitor.Controls.Add(_log);       // Fill first
        monitor.Controls.Add(bottomBar);
        monitor.Controls.Add(sendRow);

        body.Controls.Add(BuildLeftTabs(left), 0, 0);
        body.Controls.Add(monitor, 1, 0);

        Controls.Add(body); // Fill first
        Controls.Add(_top);
    }

    // =====================================================================
    // Left side: tab bar (numbers) + pages: 1 Light/Fan, 2 Car, 3 Switches, 4 Keypad, 5 GPS
    // =====================================================================
    private Control BuildLeftTabs(Control lightFanPage)
    {
        var host = new Panel { Dock = DockStyle.Fill };
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 80, WrapContents = true, Padding = new Padding(0, 0, 0, 6) };

        _pages.Add(lightFanPage);
        _pages.Add(BuildCarPage());
        _pages.Add(BuildSwitchPage());
        _pages.Add(BuildKeypadPage());
        _pages.Add(BuildGpsPage());
        _pages.Add(BuildLedPage());
        _pages.Add(BuildSensorPage());
        _pages.Add(BuildServoPage());

        string[] tabText = { "💡 1", "🚗 2", "🎚 3", "⌨ 4", "📍 5", "🌈 6", "📊 7", "🦾 8" };
        for (int i = 0; i < _pages.Count; i++)
        {
            int idx = i;
            var b = new Button { Text = tabText[i], Width = 66, Height = 32, Margin = new Padding(0, 0, 4, 4) };
            b.Click += (_, _) => ShowPage(idx);
            _tabBtns.Add(b);
            bar.Controls.Add(b);
            _pages[i].Dock = DockStyle.Fill;
            _pages[i].Visible = i == 0;
            _pageHost.Controls.Add(_pages[i]);
        }

        host.Controls.Add(_pageHost);   // Fill first
        host.Controls.Add(bar);
        return host;
    }

    private void ShowPage(int idx)
    {
        _tabIndex = idx;
        for (int i = 0; i < _pages.Count; i++) _pages[i].Visible = i == idx;
        StyleTabs();
    }

    private void StyleTabs()
    {
        for (int i = 0; i < _tabBtns.Count; i++)
        {
            var b = _tabBtns[i];
            bool sel = i == _tabIndex;
            b.FlatStyle = FlatStyle.Flat;
            b.UseVisualStyleBackColor = false;
            b.Cursor = Cursors.Hand;
            b.Font = new Font(_t.HeadFont, 11, FontStyle.Bold);
            b.FlatAppearance.BorderSize = sel ? 2 : 1;
            b.FlatAppearance.BorderColor = sel ? _t.Accent : _t.CardBorder;
            b.BackColor = sel ? Gfx.Mix(_t.InputBg, _t.Accent, 0.35f) : _t.InputBg;
            b.ForeColor = sel ? (_t.Neon ? _t.Accent : Color.White) : _t.InputText;
            b.FlatAppearance.MouseOverBackColor = Gfx.Mix(_t.InputBg, _t.Accent, 0.25f);
        }
    }

    // header row (title + state label) shared by the new pages
    private CardPanel NewPageCard(string title, Label? status = null)
    {
        var card = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(16, 10, 16, 12) };
        var header = new Panel { Dock = DockStyle.Top, Height = 34 };
        var lblTitle = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Text = title };
        header.Controls.Add(lblTitle);
        if (status != null)
        {
            status.Dock = DockStyle.Right;
            status.AutoSize = false;
            status.Width = 150;
            status.TextAlign = ContentAlignment.MiddleRight;
            header.Controls.Add(status);
        }
        _cardTitles.Add(lblTitle);
        _cards.Add(card);
        card.Tag = header;
        card.Controls.Add(header);
        return card;
    }

    // ---------------------------------------------------------------------
    // Page 2: car controller
    // ---------------------------------------------------------------------
    private Control BuildCarPage()
    {
        var card = NewPageCard("Car Controller", _lblCarState);

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        var main = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var pad = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 3 };
        for (int i = 0; i < 3; i++)
        {
            pad.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            pad.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));
        }
        // 0=Fwd 1=Back 2=Left 3=Right 4=Stop
        (int idx, int col, int row)[] pos = { (0, 1, 0), (1, 1, 2), (2, 0, 1), (3, 2, 1), (4, 1, 1) };
        foreach (var (idx, col, row) in pos)
        {
            var b = new ThemedButton { Text = CarSyms[idx], Dock = DockStyle.Fill, Margin = new Padding(4) };
            _carBtns[idx] = b;
            int k = idx;
            b.Click += (_, _) =>
            {
                if (!SendText(_carChars[k].Text, echoToInput: false)) return;
                SetCar(CarDirs[k]);
            };
            pad.Controls.Add(b, col, row);
        }

        main.Controls.Add(pad, 0, 0);
        main.Controls.Add(_car, 1, 0);

        var chars = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0, 8, 0, 0) };
        string[] names = { "Forward", "Back", "Left", "Right", "Stop" };
        for (int i = 0; i < 5; i++)
        {
            _carChars[i] = new TextBox
            {
                Text = CarCmds[i],
                TextAlign = HorizontalAlignment.Center,
                Width = 66,
                Margin = new Padding(0, 0, 8, 0)
            };
            _tip.SetToolTip(_carChars[i], names[i] + " button click panna Arduino-ku anuppa vendiya text");
            chars.Controls.Add(_carChars[i]);
        }

        body.Controls.Add(main, 0, 0);
        body.Controls.Add(chars, 0, 1);
        card.Controls.Add(body);
        body.BringToFront();
        return card;
    }

    private void SetCar(CarDir d)
    {
        _car.Dir = d;
        _lblCarState.Text = d switch
        {
            CarDir.Forward => "● FWD",
            CarDir.Back => "● REV",
            CarDir.Left => "● LEFT",
            CarDir.Right => "● RIGHT",
            _ => "● STOP"
        };
        _lblCarState.ForeColor = d == CarDir.Stop ? _t.OffCol : _t.OnCol;
    }

    // ---------------------------------------------------------------------
    // Page 3: 10 switches (2 rows x 5)
    // ---------------------------------------------------------------------
    private Control BuildSwitchPage()
    {
        var card = NewPageCard("10 Switches");

        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 2 };
        for (int c = 0; c < 5; c++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        for (int r = 0; r < 2; r++) grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        for (int i = 0; i < 10; i++)
        {
            int k = i;
            var s = new SwitchControl { Dock = DockStyle.Fill, Caption = (i + 1).ToString(), Margin = new Padding(2) };
            _sw[i] = s;
            _tip.SetToolTip(s, $"Switch {i + 1}  ->  SW{i + 1}:1 / SW{i + 1}:0");
            s.ToggleRequested += want =>
            {
                if (SendText($"SW{k + 1}:{(want ? 1 : 0)}", echoToInput: false)) s.IsOn = want;
            };
            grid.Controls.Add(s, i % 5, i / 5);
        }

        card.Controls.Add(grid);
        grid.BringToFront();
        return card;
    }

    // ---------------------------------------------------------------------
    // Page 4: 4x4 keypad
    // ---------------------------------------------------------------------
    private Control BuildKeypadPage()
    {
        var card = NewPageCard("4x4 Keypad", _lblLastKey);
        _lblLastKey.Text = "KEY  -";

        _keypad.KeyPressed += key =>
        {
            if (SendText("KEY:" + key, echoToInput: false))
                _lblLastKey.Text = "KEY  " + key;
        };

        card.Controls.Add(_keypad);
        _keypad.BringToFront();
        return card;
    }

    // ---------------------------------------------------------------------
    // Page 5: GPS (lat / long)
    // ---------------------------------------------------------------------
    private Control BuildGpsPage()
    {
        var card = NewPageCard("GPS Location");
        card.Controls.Add(_gps);
        _gps.BringToFront();
        return card;
    }

    // ---------------------------------------------------------------------
    // Page 6: LED brightness (0-100%) + RGB picker
    // ---------------------------------------------------------------------
    private Control BuildLedPage()
    {
        var card = NewPageCard("LED Control", _lblLedState);

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var top = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        top.Controls.Add(_led, 0, 0);
        top.Controls.Add(_wheel, 1, 0);

        var sliders = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        for (int i = 0; i < 4; i++) sliders.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        sliders.Controls.Add(_slBright, 0, 0);
        sliders.Controls.Add(_slR, 0, 1);
        sliders.Controls.Add(_slG, 0, 2);
        sliders.Controls.Add(_slB, 0, 3);

        body.Controls.Add(top, 0, 0);
        body.Controls.Add(sliders, 0, 1);
        card.Controls.Add(body);
        body.BringToFront();

        _slBright.ValueChanged += v => { _ledBright = v; _ledBrightDirty = true; RefreshLedUi(); };
        _slR.ValueChanged += v => { _ledColor = Color.FromArgb(v, _ledColor.G, _ledColor.B); _ledColorDirty = true; RefreshLedUi(); };
        _slG.ValueChanged += v => { _ledColor = Color.FromArgb(_ledColor.R, v, _ledColor.B); _ledColorDirty = true; RefreshLedUi(); };
        _slB.ValueChanged += v => { _ledColor = Color.FromArgb(_ledColor.R, _ledColor.G, v); _ledColorDirty = true; RefreshLedUi(); };
        _wheel.ColorPicked += c => { _ledColor = c; _ledColorDirty = true; RefreshLedUi(fromWheel: true); };

        // throttled send while dragging; the final value is logged once the mouse is released
        _ledTimer.Tick += (_, _) =>
        {
            if (!_port.IsOpen || !(_ledBrightDirty || _ledColorDirty)) return;
            bool log = MouseButtons == MouseButtons.None;
            if (_ledBrightDirty && SendText($"LED:B:{_ledBright}", false, log)) _ledBrightDirty = false;
            if (_ledColorDirty && SendText($"LED:RGB:{_ledColor.R},{_ledColor.G},{_ledColor.B}", false, log)) _ledColorDirty = false;
        };
        _ledTimer.Start();

        RefreshLedUi();
        _wheel.SetColor(_ledColor);
        return card;
    }

    private void RefreshLedUi(bool fromWheel = false)
    {
        var c = _ledColor;
        _led.LedColor = c;
        _led.Brightness = _ledBright;

        _slBright.Value = _ledBright;
        _slBright.ColorLeft = Color.Black;
        _slBright.ColorRight = c;
        _slR.Value = c.R; _slR.ColorLeft = Color.FromArgb(0, c.G, c.B); _slR.ColorRight = Color.FromArgb(255, c.G, c.B);
        _slG.Value = c.G; _slG.ColorLeft = Color.FromArgb(c.R, 0, c.B); _slG.ColorRight = Color.FromArgb(c.R, 255, c.B);
        _slB.Value = c.B; _slB.ColorLeft = Color.FromArgb(c.R, c.G, 0); _slB.ColorRight = Color.FromArgb(c.R, c.G, 255);
        if (!fromWheel) _wheel.SetColor(c);

        _lblLedState.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}  {_ledBright}%";
        _lblLedState.ForeColor = c.GetBrightness() < 0.12f ? _t.Text : c;
    }

    // ---------------------------------------------------------------------
    // Page 7: live sensor gauge widgets (+ add / remove your own)
    // ---------------------------------------------------------------------
    private Control BuildSensorPage()
    {
        var card = NewPageCard("Live Sensors", _lblSensorState);

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, WrapContents = false, Padding = new Padding(0, 4, 0, 0) };
        _btnAddGauge.Click += (_, _) => AddGaugeDialog();
        bar.Controls.Add(_btnAddGauge);
        _sensorHint.Text = "TEMP:28.5   |   right-click = remove";
        _sensorHint.AutoSize = true;
        _sensorHint.Margin = new Padding(10, 7, 0, 0);
        bar.Controls.Add(_sensorHint);

        _gaugeFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true };

        _gaugeEmpty.Dock = DockStyle.Fill;
        _gaugeEmpty.TextAlign = ContentAlignment.MiddleCenter;
        _gaugeEmpty.Text = "No sensor widgets yet\r\nClick  ＋ Add widget  to create one";

        card.Controls.Add(_gaugeEmpty);
        card.Controls.Add(_gaugeFlow);
        card.Controls.Add(bar);
        _gaugeFlow.BringToFront();
        _gaugeEmpty.BringToFront();

        UpdateSensorState();
        return card;
    }

    private void AddGauge(string key, string title, string unit, double min, double max, bool invert)
    {
        var gauge = new GaugeControl
        {
            Key = key, Title = title, Unit = unit, Minimum = min, Maximum = max, Invert = invert,
            Margin = new Padding(3), Skin = _t
        };
        var menu = new ContextMenuStrip();
        menu.Items.Add("Remove widget", null, (_, _) =>
        {
            _gauges.Remove(gauge);
            _gaugeFlow.Controls.Remove(gauge);
            gauge.Dispose();
            UpdateSensorState();
        });
        gauge.ContextMenuStrip = menu;
        _gauges.Add(gauge);
        _gaugeFlow.Controls.Add(gauge);
        UpdateSensorState();
    }

    private void AddGaugeDialog()
    {
        using var dlg = new GaugeDialog(_t);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        AddGauge(dlg.KeyName, dlg.Title2.Length > 0 ? dlg.Title2 : dlg.KeyName, dlg.UnitText, dlg.Min, dlg.Max, dlg.Invert);
    }

    private void UpdateSensorState()
    {
        int live = _gauges.Count(g => g.HasData);
        _gaugeEmpty.Visible = _gauges.Count == 0;
        _gaugeFlow.Visible = _gauges.Count > 0;
        _lblSensorState.Text = _gauges.Count == 0 ? "● no widgets" : $"● {live}/{_gauges.Count} live";
        _lblSensorState.ForeColor = live > 0 ? _t.OnCol : _t.OffCol;
    }

    // "TEMP:28.5"  or  "TEMP:28.5,HUM:60,DIST=35" -> matching gauges
    private void TryParseSensors(string line)
    {
        if (_gauges.Count == 0 || line.StartsWith('$')) return;
        bool any = false;
        foreach (Match m in SensorKv.Matches(line))
        {
            string key = m.Groups[1].Value.ToUpperInvariant();
            if (!double.TryParse(m.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double v)) continue;
            foreach (var g in _gauges)
                if (g.Key == key) { g.SetValue(v); any = true; }
        }
        if (any) UpdateSensorState();
    }

    // ---------------------------------------------------------------------
    // Page 8: 16-channel servo (SV1:90 ... SV16:90)
    // ---------------------------------------------------------------------
    private Control BuildServoPage()
    {
        var card = NewPageCard("16-Channel Servo");

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, WrapContents = false, Padding = new Padding(0, 4, 0, 0) };
        bar.Controls.Add(_btnSvCenter);
        bar.Controls.Add(_btnSvZero);
        bar.Controls.Add(_btnSvSweep);
        foreach (var b in new[] { _btnSvCenter, _btnSvZero, _btnSvSweep }) b.Margin = new Padding(0, 0, 8, 0);

        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 8 };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (int r = 0; r < 8; r++) grid.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5f));

        for (int i = 0; i < 16; i++)
        {
            int k = i;
            _svVal[i] = 90;
            var s = new GradientSlider
            {
                Dock = DockStyle.Fill, Caption = "S" + (i + 1), Suffix = "°", Maximum = 180,
                Value = 90, Margin = new Padding(2, 0, 2, 0), Height = 36
            };
            _servo[i] = s;
            s.ValueChanged += v => { _svVal[k] = v; _svDirty[k] = true; };
            // column-major: S1-S8 left, S9-S16 right
            grid.Controls.Add(s, i / 8, i % 8);
        }

        _btnSvCenter.Click += (_, _) => SetAllServos(90);
        _btnSvZero.Click += (_, _) => SetAllServos(0);
        _btnSvSweep.Click += (_, _) =>
        {
            _svSweep = !_svSweep;
            RefreshSweepBtn();
        };

        // throttled sender: max 3 channels per tick, round-robin so no channel starves
        _servoTimer.Tick += (_, _) =>
        {
            if (_svSweep)
            {
                _svPhase += 0.18f;
                for (int i = 0; i < 16; i++)
                {
                    int v = (int)Math.Round(90 + 80 * Math.Sin(_svPhase + i * 0.45));
                    if (v == _svVal[i]) continue;
                    _svVal[i] = v;
                    _servo[i].Value = v;
                    _svDirty[i] = true;
                }
            }
            if (!_port.IsOpen) return;
            bool log = MouseButtons == MouseButtons.None && !_svSweep;
            int sent = 0;
            for (int n = 0; n < 16 && sent < 3; n++)
            {
                int i = (_svNext + n) % 16;
                if (!_svDirty[i]) continue;
                if (SendText($"SV{i + 1}:{_svVal[i]}", false, log)) _svDirty[i] = false;
                _svNext = (i + 1) % 16;
                sent++;
            }
        };
        _servoTimer.Start();

        card.Controls.Add(grid);
        card.Controls.Add(bar);
        grid.BringToFront();
        return card;
    }

    private void SetAllServos(int deg)
    {
        _svSweep = false;
        RefreshSweepBtn();
        for (int i = 0; i < 16; i++)
        {
            _svVal[i] = deg;
            _servo[i].Value = deg;
            _svDirty[i] = true;
        }
    }

    private Label MakeLabel(string text)
    {
        var l = new Label { Text = text, AutoSize = true, Margin = new Padding(3, 6, 3, 0) };
        _topLabels.Add(l);
        return l;
    }

    // [ ON ] [ icon ] [ OFF ] card
    private CardPanel MakeDeviceCard(Label status, Control icon,
        Button on, TextBox onChar, Button off, TextBox offChar)
    {
        var card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 12), Padding = new Padding(16, 10, 16, 12) };

        var header = new Panel { Dock = DockStyle.Top, Height = 34 };
        var lblTitle = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        status.Dock = DockStyle.Right;
        status.AutoSize = false;
        status.Width = 90;
        status.TextAlign = ContentAlignment.MiddleRight;
        header.Controls.Add(lblTitle); // Fill first
        header.Controls.Add(status);
        _cardTitles.Add(lblTitle);

        var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31));
        row.Controls.Add(MakeCmdCell(on, onChar), 0, 0);
        row.Controls.Add(icon, 1, 0);
        row.Controls.Add(MakeCmdCell(off, offChar), 2, 0);

        card.Controls.Add(row);    // Fill first
        card.Controls.Add(header);
        _cards.Add(card);
        return card;
    }

    private static TableLayoutPanel MakeCmdCell(Button btn, TextBox txt)
    {
        var cell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
        cell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        cell.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        cell.Controls.Add(btn, 0, 0);
        cell.Controls.Add(txt, 0, 1);
        return cell;
    }

    // =====================================================================
    // Events
    // =====================================================================
    private void WireEvents()
    {
        _btnRefresh.Click += (_, _) => RefreshPorts();
        _btnConnect.Click += (_, _) => ToggleConnection();
        _btnSend.Click += (_, _) => SendText(_txtSend.Text);
        _txtSend.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SendText(_txtSend.Text);
            }
        };
        _btnClear.Click += (_, _) => _log.Clear();

        for (int i = 0; i < 4; i++)
        {
            int idx = i; // closure fix
            _btns[i].Click += (_, _) =>
            {
                if (!SendText(_btnChars[idx].Text, echoToInput: false)) return;
                switch (idx)
                {
                    case 0: SetLight(true); break;
                    case 1: SetLight(false); break;
                    case 2: SetFan(true); break;
                    case 3: SetFan(false); break;
                }
            };
        }

        _port.DataReceived += OnDataReceived;
        _flushTimer.Tick += (_, _) => FlushPartialLine();
        _flushTimer.Start();

        FormClosing += (_, _) =>
        {
            _flushTimer.Stop();
            _ledTimer.Stop();
            _servoTimer.Stop();
            if (_port.IsOpen) _port.Close();
        };
    }

    // =====================================================================
    // Light / Fan state
    // =====================================================================
    private void SetLight(bool on)
    {
        _light.IsOn = on;
        _lblLightState.Text = on ? "● ON" : "● OFF";
        _lblLightState.ForeColor = on ? _t.OnCol : _t.OffCol;
    }

    private void SetFan(bool on)
    {
        _fan.IsOn = on;
        _lblFanState.Text = on ? "● ON" : "● OFF";
        _lblFanState.ForeColor = on ? _t.OnCol : _t.OffCol;
    }

    // =====================================================================
    // Port list + connect / disconnect
    // =====================================================================
    private void RefreshPorts()
    {
        string? previous = _cmbPort.SelectedItem as string;
        _cmbPort.Items.Clear();
        var names = SerialPort.GetPortNames().OrderBy(n => n.Length).ThenBy(n => n).ToArray();
        _cmbPort.Items.AddRange(names);
        if (previous != null && names.Contains(previous)) _cmbPort.SelectedItem = previous;
        else if (names.Length > 0) _cmbPort.SelectedIndex = 0;
    }

    private void ToggleConnection()
    {
        if (_port.IsOpen)
        {
            _port.Close();
            AppendLine("--- Disconnected ---", LogKind.System);
            SetLight(false);
            SetFan(false);
            SetCar(CarDir.Stop);
            foreach (var s in _sw) s.IsOn = false;
            _lblLastKey.Text = "KEY  -";
            _gps.Reset();
            foreach (var gg in _gauges) gg.ClearData();
            UpdateSensorState();
            _svSweep = false;
            RefreshSweepBtn();
        }
        else
        {
            if (_cmbPort.SelectedItem is not string portName)
            {
                MessageBox.Show("Select COM port OR Click Refresh.");
                return;
            }
            try
            {
                _port.PortName = portName;
                _port.BaudRate = int.Parse((string)_cmbBaud.SelectedItem!);
                _port.Encoding = Encoding.UTF8;
                _port.DtrEnable = true; // Arduino IDE maari - board reset aagum
                _port.Open();
                AppendLine($"--- Connected to {portName} @ {_port.BaudRate} ---", LogKind.System);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connect aagala:\n{ex.Message}\n\nArduino IDE Serial Monitor open-la iruntha close pannunga.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        UpdateUiState();
    }

    private void UpdateUiState()
    {
        bool open = _port.IsOpen;
        _btnConnect.Text = open ? "DISCONNECT" : "CONNECT";
        PaintBtn(_btnConnect, open ? _t.OffCol : _t.OnCol, true, _t.BtnText);

        _cmbPort.Enabled = !open;
        _cmbBaud.Enabled = !open;
        _btnRefresh.Enabled = !open;

        _btnSend.Enabled = open;
        PaintBtn(_btnSend, _t.Accent, open, _t.AccentText);
        _txtSend.Enabled = open;

        for (int i = 0; i < 4; i++)
        {
            _btns[i].Enabled = open;
            PaintBtn(_btns[i], i % 2 == 0 ? _t.OnCol : _t.OffCol, open, _t.BtnText);
        }

        for (int i = 0; i < 5; i++)
        {
            _carBtns[i].Enabled = open;
            PaintBtn(_carBtns[i], i == 4 ? _t.OffCol : _t.OnCol, open, _t.BtnText);
        }
        foreach (var s in _sw) s.Enabled = open;
        _keypad.Enabled = open;
        _wheel.Enabled = open;
        foreach (var s in new[] { _slBright, _slR, _slG, _slB }) s.Enabled = open;
        foreach (var s in _servo) s.Enabled = open;
        _btnSvCenter.Enabled = open;
        _btnSvZero.Enabled = open;
        _btnSvSweep.Enabled = open;
    }

    // =====================================================================
    // Receive
    // =====================================================================
    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            string data = _port.ReadExisting();
            if (data.Length == 0) return;
            BeginInvoke(() => ProcessIncoming(data));
        }
        catch
        {
            // port close aagum bodhu vara exception - ignore
        }
    }

    private void ProcessIncoming(string data)
    {
        _lastRx = DateTime.Now;
        _rxBuffer.Append(data);

        string all = _rxBuffer.ToString();
        int start = 0;
        int nl;
        while ((nl = all.IndexOf('\n', start)) >= 0)
        {
            string line = all.Substring(start, nl - start).TrimEnd('\r');
            AppendLine(line, LogKind.Rx);
            start = nl + 1;
        }
        _rxBuffer.Clear();
        _rxBuffer.Append(all.Substring(start)); // remaining partial line
    }

    // newline illama vara data (Serial.print) kooda 300ms-la kaatta
    private void FlushPartialLine()
    {
        if (_rxBuffer.Length > 0 && (DateTime.Now - _lastRx).TotalMilliseconds > 300)
        {
            AppendLine(_rxBuffer.ToString().TrimEnd('\r'), LogKind.Rx);
            _rxBuffer.Clear();
        }
    }

    // =====================================================================
    // Send + line ending
    // =====================================================================
    private string LineEnding() => _cmbEnding.SelectedIndex switch
    {
        1 => "\n",
        2 => "\r",
        3 => "\r\n",
        _ => ""
    };

    private bool SendText(string text, bool echoToInput = true, bool log = true)
    {
        if (!_port.IsOpen) return false;
        try
        {
            _port.Write(text + LineEnding());
            if (log) AppendLine($"TX: {text}", LogKind.Tx);
            if (echoToInput) _txtSend.Clear();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Send aagala: {ex.Message}");
            return false;
        }
    }

    // =====================================================================
    // Log output (timestamp + autoscroll + themed colors)
    // =====================================================================
    // =====================================================================
    // GPS parsing: NMEA ($GPRMC/$GPGGA...), "LAT:11.01,LON:76.95", or plain "11.01,76.95"
    // =====================================================================
    private static readonly Regex GpsKeyed = new(
        @"lat\w*\s*[:=]\s*(-?\d+(?:\.\d+)?)\D+?(?:lon\w*|lng)\s*[:=]\s*(-?\d+(?:\.\d+)?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex GpsPlain = new(
        @"^\s*(-?\d{1,2}\.\d+)\s*[,;\s]\s*(-?\d{1,3}\.\d+)\s*$", RegexOptions.Compiled);

    private static double NmeaToDeg(string v, string hemi)
    {
        if (!double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double raw)) return double.NaN;
        double deg = Math.Floor(raw / 100.0);
        double d = deg + (raw - deg * 100.0) / 60.0;
        return hemi is "S" or "W" ? -d : d;
    }

    private void TryParseGps(string line)
    {
        double lat = double.NaN, lon = double.NaN;
        var inv = CultureInfo.InvariantCulture;

        if (line.StartsWith('$') && line.Length > 6)
        {
            string body = line.Split('*')[0];
            var f = body.Split(',');
            string id = f[0].Length >= 6 ? f[0].Substring(3) : "";
            if (id == "RMC" && f.Length > 6 && f[2] == "A")
            {
                lat = NmeaToDeg(f[3], f[4]);
                lon = NmeaToDeg(f[5], f[6]);
            }
            else if (id == "GGA" && f.Length > 6 && f[6] != "0" && f[6] != "")
            {
                lat = NmeaToDeg(f[2], f[3]);
                lon = NmeaToDeg(f[4], f[5]);
            }
        }
        else
        {
            var m = GpsKeyed.Match(line);
            if (!m.Success) m = GpsPlain.Match(line);
            if (m.Success)
            {
                double.TryParse(m.Groups[1].Value, NumberStyles.Float, inv, out lat);
                double.TryParse(m.Groups[2].Value, NumberStyles.Float, inv, out lon);
                if (lat == 0 && lon == 0) lat = lon = double.NaN;
            }
        }

        if (double.IsNaN(lat) || double.IsNaN(lon)) return;
        if (Math.Abs(lat) > 90 || Math.Abs(lon) > 180) return;
        _gps.SetPosition(lat, lon);
    }

    private void AppendLine(string text, LogKind kind = LogKind.Rx)
    {
        if (kind == LogKind.Rx)
        {
            TryParseGps(text);
            TryParseSensors(text);
        }

        string prefix = _chkTimestamp.Checked ? DateTime.Now.ToString("HH:mm:ss.fff") + " -> " : "";

        // log romba perusa aagama trim
        if (_log.TextLength > 500_000)
        {
            _log.Select(0, 250_000);
            _log.SelectedText = "";
        }

        _log.SelectionStart = _log.TextLength;
        _log.SelectionLength = 0;
        _log.SelectionColor = kind switch
        {
            LogKind.Tx => _t.LogTx,
            LogKind.System => _t.LogSys,
            _ => _t.LogRx
        };
        _log.SelectedText = prefix + text + Environment.NewLine;

        if (_chkAutoScroll.Checked)
        {
            _log.SelectionStart = _log.TextLength;
            _log.ScrollToCaret();
        }
    }
}

// =========================================================================
// Drawing helpers
// =========================================================================
internal static class Gfx
{
    public static GraphicsPath RoundRect(RectangleF r, float rad)
    {
        var p = new GraphicsPath();
        float d = rad * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    public static Color Mix(Color a, Color b, float f) => Color.FromArgb(
        (int)(a.R + (b.R - a.R) * f),
        (int)(a.G + (b.G - a.G) * f),
        (int)(a.B + (b.B - a.B) * f));

    // neon outline with optional glow
    public static void Neon(Graphics g, GraphicsPath p, Color c, float u, bool glow)
    {
        if (glow)
        {
            using var g1 = new Pen(Color.FromArgb(40, c), 9 * u) { LineJoin = LineJoin.Round };
            using var g2 = new Pen(Color.FromArgb(90, c), 4.5f * u) { LineJoin = LineJoin.Round };
            g.DrawPath(g1, p);
            g.DrawPath(g2, p);
        }
        using var fill = new SolidBrush(Color.FromArgb(glow ? 55 : 20, c));
        g.FillPath(fill, p);
        using var pen = new Pen(c, Math.Max(1.5f, 2 * u)) { LineJoin = LineJoin.Round };
        g.DrawPath(pen, p);
    }

    // ---------------------------------------------------------------------
    // Theme decor: ambient effects + small animated badge.
    //  (cx,cy,rx,ry) = ellipse around the icon, (bx,by,bu) = badge position/scale,
    //  ph = animation phase, on = device is ON
    // ---------------------------------------------------------------------
    public static void Decor(Graphics g, AppTheme t, float w, float h,
        float cx, float cy, float rx, float ry, float bx, float by, float bu, float ph, bool on)
    {
        Color c1 = on ? t.IconOn1 : t.IconOff;
        Color c2 = on ? t.IconOn2 : t.IconOff;
        float uu = rx / 45f;
        int lit = on ? ((int)(ph * 0.5f)) % 4 : 0;

        switch (t.Decor)
        {
            case DecorStyle.Orbit:
            {
                if (!on) break;
                for (int k = 0; k < 4; k++)
                {
                    float a = ph * 0.7f + k * MathF.PI / 2f;
                    float x = cx + MathF.Cos(a) * rx;
                    float y = cy + MathF.Sin(a) * ry;
                    float r = 2.6f * uu;
                    using var b = new SolidBrush(k % 2 == 0 ? t.IconOn1 : t.IconOn2);
                    g.FillEllipse(b, x - r, y - r, r * 2, r * 2);
                }
                break;
            }

            case DecorStyle.Matrix:
            {
                if (!on) break;
                float dh = Math.Max(6f, 7f * uu), dw = Math.Max(2f, 1.6f * uu);
                for (int col = 0; col < 4; col++)
                {
                    float x = col < 2 ? w * (0.05f + 0.05f * col) : w * (0.90f + 0.05f * (col - 2));
                    for (int k = 0; k < 4; k++)
                    {
                        float y = ((ph * 14f + col * 53f + k * 22f) % (h + 30f)) - 15f;
                        using var b = new SolidBrush(Color.FromArgb(Math.Max(40, 220 - k * 55), t.IconOn1));
                        g.FillRectangle(b, x, y, dw, dh);
                    }
                }
                break;
            }

            case DecorStyle.Scan:
            {
                float m = 5f, len = 14f;
                using var pen = new Pen(Color.FromArgb(on ? 255 : 110, c1), 2f);
                g.DrawLine(pen, m, m, m + len, m);
                g.DrawLine(pen, m, m, m, m + len);
                g.DrawLine(pen, w - m, m, w - m - len, m);
                g.DrawLine(pen, w - m, m, w - m, m + len);
                g.DrawLine(pen, m, h - m, m + len, h - m);
                g.DrawLine(pen, m, h - m, m, h - m - len);
                g.DrawLine(pen, w - m, h - m, w - m - len, h - m);
                g.DrawLine(pen, w - m, h - m, w - m, h - m - len);
                if (on)
                {
                    float y = m + (h - 2 * m) * ((ph * 0.12f) % 1f);
                    using var glow = new Pen(Color.FromArgb(40, c2), 7f);
                    using var line = new Pen(Color.FromArgb(170, c2), 2f);
                    g.DrawLine(glow, m + 4, y, w - m - 4, y);
                    g.DrawLine(line, m + 4, y, w - m - 4, y);
                }
                break;
            }

            case DecorStyle.Qubit:
            {
                float minor = rx * 0.34f;
                for (int k = 0; k < 3; k++)
                {
                    var st = g.Save();
                    g.TranslateTransform(cx, cy);
                    g.RotateTransform(k * 60f + (on ? ph * 3f : 0f));
                    using var pen = new Pen(Color.FromArgb(on ? 170 : 70, k == 1 ? c2 : c1), Math.Max(1f, 1.3f * uu));
                    g.DrawEllipse(pen, -rx, -minor, rx * 2, minor * 2);
                    if (on)
                    {
                        float a = ph * 1.3f + k * 2.1f;
                        float ex = rx * MathF.Cos(a), ey = minor * MathF.Sin(a);
                        float r = 2.4f * uu;
                        using var dot = new SolidBrush(c2);
                        g.FillEllipse(dot, ex - r, ey - r, r * 2, r * 2);
                    }
                    g.Restore(st);
                }
                break;
            }

            case DecorStyle.Signal:
                Signal(g, bx, by + 8 * bu, bu, c1, lit);
                break;

            case DecorStyle.Gear:
                Gear(g, bx, by, 10 * bu, on ? ph * 20f : 0f, c1, t.Card);
                break;

            case DecorStyle.Cloud:
                Cloud(g, bx, by, bu, c1, t.Card, on ? ((ph * 0.4f) % 1f) * 4f * bu : 0f);
                break;

            case DecorStyle.Bars:
                Bars(g, bx, by, bu, c1, c2, ph, on);
                break;

            case DecorStyle.Chain:
                Chain(g, bx, by, bu, c1, lit, on);
                break;

            case DecorStyle.Chip:
                Chip(g, bx, by, bu, c1, c2, ph, on);
                break;

            case DecorStyle.Packets:
                Packets(g, bx, by, bu, c1, c2, ph, on);
                break;

            case DecorStyle.Edge:
                EdgeNodes(g, bx, by, bu, c1, c2, on ? lit % 3 : -1);
                break;
        }
    }

    // Wi-Fi like arcs
    public static void Signal(Graphics g, float x, float y, float u, Color c, int lit)
    {
        for (int i = 1; i <= 3; i++)
        {
            float r = (4 + i * 4.5f) * u;
            using var pen = new Pen(Color.FromArgb(i <= lit ? 255 : 70, c), Math.Max(1.5f, 2 * u))
            { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawArc(pen, x - r, y - r, r * 2, r * 2, -135, 90);
        }
        using var dot = new SolidBrush(c);
        g.FillEllipse(dot, x - 2 * u, y - 2 * u, 4 * u, 4 * u);
    }

    // rotating gear
    public static void Gear(Graphics g, float x, float y, float r, float angleDeg, Color c, Color hole)
    {
        var pts = new List<PointF>();
        float a0 = angleDeg * MathF.PI / 180f;
        (float da, float rad)[] tooth = { (-0.20f, 0.74f), (-0.11f, 1f), (0.11f, 1f), (0.20f, 0.74f) };
        for (int k = 0; k < 8; k++)
        {
            float b = a0 + k * MathF.PI / 4f;
            foreach (var (da, rad) in tooth)
                pts.Add(new PointF(x + MathF.Cos(b + da) * r * rad, y + MathF.Sin(b + da) * r * rad));
        }
        using var br = new SolidBrush(c);
        g.FillPolygon(br, pts.ToArray());
        using var hb = new SolidBrush(hole);
        float hr = r * 0.34f;
        g.FillEllipse(hb, x - hr, y - hr, hr * 2, hr * 2);
    }

    // cloud with upload arrow
    public static void Cloud(Graphics g, float x, float y, float u, Color c, Color arrowCol, float bob)
    {
        using var br = new SolidBrush(c);
        g.FillEllipse(br, x - 12 * u, y - 3 * u, 12 * u, 11 * u);
        g.FillEllipse(br, x - 6 * u, y - 10 * u, 14 * u, 14 * u);
        g.FillEllipse(br, x + 1 * u, y - 4 * u, 12 * u, 11 * u);
        g.FillRectangle(br, x - 7 * u, y + 1 * u, 14 * u, 7 * u);
        using var pen = new Pen(arrowCol, Math.Max(1.5f, 1.8f * u)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        float ay = y + 5 * u - bob;
        g.DrawLine(pen, x, ay, x, ay - 7 * u);
        g.DrawLine(pen, x - 3 * u, ay - 4 * u, x, ay - 7 * u);
        g.DrawLine(pen, x + 3 * u, ay - 4 * u, x, ay - 7 * u);
    }

    // live bar chart
    public static void Bars(Graphics g, float x, float y, float u, Color c1, Color c2, float ph, bool on)
    {
        for (int k = 0; k < 4; k++)
        {
            float hgt = on ? 4 + (MathF.Sin(ph * 1.4f + k * 1.3f) + 1f) * 5f : 3 + k * 1.3f;
            var r = new RectangleF(x - 11 * u + k * 6 * u, y + 11 * u - hgt * u, 4.2f * u, hgt * u);
            using var p = RoundRect(r, 1.2f * u);
            using var b = new SolidBrush(k % 2 == 0 ? c1 : c2);
            g.FillPath(b, p);
        }
    }

    // chain of blocks
    public static void Chain(Graphics g, float x, float y, float u, Color c, int lit, bool on)
    {
        using var pen = new Pen(c, Math.Max(1.5f, 1.8f * u));
        using var fill = new SolidBrush(c);
        for (int i = 0; i < 3; i++)
        {
            float bx = x - 14 * u + i * 10.5f * u;
            var r = new RectangleF(bx, y - 3.5f * u, 7 * u, 7 * u);
            if (i < 2) g.DrawLine(pen, bx + 7 * u, y, bx + 10.5f * u, y);
            if (on && i < lit) g.FillRectangle(fill, r.X, r.Y, r.Width, r.Height);
            else g.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height);
        }
    }

    // microchip with blinking core
    public static void Chip(Graphics g, float x, float y, float u, Color c1, Color c2, float ph, bool on)
    {
        float lw = Math.Max(1.5f, 1.8f * u);
        using var pen = new Pen(c1, lw);
        using var body = RoundRect(new RectangleF(x - 7 * u, y - 7 * u, 14 * u, 14 * u), 2 * u);
        g.DrawPath(pen, body);
        using var pin = new Pen(c1, Math.Max(1.2f, 1.4f * u));
        for (int i = 0; i < 3; i++)
        {
            float o = (-4 + i * 4) * u;
            g.DrawLine(pin, x + o, y - 7 * u, x + o, y - 11 * u);
            g.DrawLine(pin, x + o, y + 7 * u, x + o, y + 11 * u);
            g.DrawLine(pin, x - 7 * u, y + o, x - 11 * u, y + o);
            g.DrawLine(pin, x + 7 * u, y + o, x + 11 * u, y + o);
        }
        int alpha = on ? (int)(110 + 145 * (MathF.Sin(ph * 2.2f) + 1f) / 2f) : 255;
        using var core = new SolidBrush(Color.FromArgb(alpha, on ? c2 : c1));
        g.FillRectangle(core, x - 3 * u, y - 3 * u, 6 * u, 6 * u);
    }

    // packets travelling between two nodes
    public static void Packets(Graphics g, float x, float y, float u, Color c1, Color c2, float ph, bool on)
    {
        using var line = new Pen(Color.FromArgb(90, c1), Math.Max(1.2f, 1.4f * u));
        g.DrawLine(line, x - 8 * u, y, x + 8 * u, y);
        using var node = new SolidBrush(c1);
        g.FillEllipse(node, x - 14 * u, y - 3 * u, 6 * u, 6 * u);
        g.FillEllipse(node, x + 8 * u, y - 3 * u, 6 * u, 6 * u);
        if (!on) return;
        using var pk = new SolidBrush(c2);
        for (int k = 0; k < 3; k++)
        {
            float t = ((ph * 0.35f) + k / 3f) % 1f;
            float px = x - 8 * u + 16 * u * t;
            g.FillRectangle(pk, px - 1.7f * u, y - 1.7f * u, 3.4f * u, 3.4f * u);
        }
    }

    // edge node cluster
    public static void EdgeNodes(Graphics g, float x, float y, float u, Color c1, Color c2, int hl)
    {
        using var line = new Pen(Color.FromArgb(120, c1), Math.Max(1.2f, 1.4f * u));
        using var b1 = new SolidBrush(c1);
        using var b2 = new SolidBrush(c2);
        float[] ang = { -90f, 30f, 150f };
        for (int k = 0; k < 3; k++)
        {
            float a = ang[k] * MathF.PI / 180f;
            float sx = x + MathF.Cos(a) * 9 * u, sy = y + MathF.Sin(a) * 9 * u;
            g.DrawLine(line, x, y, sx, sy);
            float r = (k == hl ? 3.4f : 2.4f) * u;
            g.FillEllipse(k == hl ? b2 : b1, sx - r, sy - r, r * 2, r * 2);
        }
        g.FillEllipse(b1, x - 3 * u, y - 3 * u, 6 * u, 6 * u);
    }
}

// =========================================================================
// Rounded card with border (+ glow)
// =========================================================================
public sealed class CardPanel : Panel
{
    public Color BorderColor { get; set; } = Color.Gray;
    public bool Glow { get; set; }
    public int Radius { get; set; } = 12;

    public CardPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? SystemColors.Control);

        var r = new RectangleF(2, 2, Width - 5, Height - 5);
        using var p = Gfx.RoundRect(r, Radius);
        using (var b = new SolidBrush(BackColor)) g.FillPath(b, p);
        if (Glow)
        {
            using var gp = new Pen(Color.FromArgb(55, BorderColor), 5f);
            g.DrawPath(gp, p);
        }
        using var pen = new Pen(BorderColor, 1.5f);
        g.DrawPath(pen, p);
    }
}

// =========================================================================
// Animated flashlight  (glow pulses + rays flicker when ON, themed decor)
// =========================================================================
public sealed class LightControl : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 30 };
    private float _phase;
    private bool _on;
    private AppTheme _t = AppTheme.AI;

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

    public LightControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = _t.Card;
        _timer.Tick += (_, _) =>
        {
            if (_on)
            {
                _phase += 0.18f;
                Invalidate();
            }
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

        float u = Math.Min(Width / 90f, Height / 110f);
        if (u <= 0) return;
        float cx = Width / 2f;
        float cy = Height / 2f + 8 * u;
        float pulse = (MathF.Sin(_phase) + 1f) / 2f;
        var t = _t;
        bool neon = t.Neon;

        if (_on)
        {
            // glow
            float gr = (42 + pulse * 8) * u;
            float gy = cy - 30 * u;
            using (var gp = new GraphicsPath())
            {
                gp.AddEllipse(cx - gr, gy - gr, gr * 2, gr * 2);
                using var pg = new PathGradientBrush(gp)
                {
                    CenterColor = Color.FromArgb(170, t.IconGlow),
                    SurroundColors = new[] { Color.FromArgb(0, t.IconGlow) }
                };
                g.FillPath(pg, gp);
            }

            // rays
            using var pen = new Pen(Color.FromArgb(225, t.IconGlow), Math.Max(2f, 3 * u))
            { StartCap = LineCap.Round, EndCap = LineCap.Round };
            float oy = cy - 26 * u;
            foreach (int deg in new[] { -60, -30, 0, 30, 60 })
            {
                float a = deg * MathF.PI / 180f;
                float r1 = 20 * u;
                float r2 = (28 + pulse * 9) * u;
                g.DrawLine(pen,
                    cx + MathF.Sin(a) * r1, oy - MathF.Cos(a) * r1,
                    cx + MathF.Sin(a) * r2, oy - MathF.Cos(a) * r2);
            }
        }

        Color headC = _on ? t.IconOn1 : t.IconOff;
        Color bodyC = _on ? t.IconOn2 : t.IconOff;
        Color lensC = _on ? Gfx.Mix(t.IconGlow, Color.White, 0.65f) : Gfx.Mix(t.IconOff, t.Card, 0.35f);

        using var body = Gfx.RoundRect(new RectangleF(cx - 13 * u, cy - 8 * u, 26 * u, 50 * u), 6 * u);
        using var sw = Gfx.RoundRect(new RectangleF(cx - 3.5f * u, cy + 8 * u, 7 * u, 14 * u), 3 * u);
        using var head = new GraphicsPath();
        head.AddPolygon(new[]
        {
            new PointF(cx - 19 * u, cy - 26 * u), new PointF(cx + 19 * u, cy - 26 * u),
            new PointF(cx + 13 * u, cy - 8 * u),  new PointF(cx - 13 * u, cy - 8 * u)
        });
        using var lens = new GraphicsPath();
        lens.AddEllipse(cx - 19 * u, cy - 31 * u, 38 * u, 10 * u);

        if (neon)
        {
            Gfx.Neon(g, body, bodyC, u, _on);
            Gfx.Neon(g, sw, headC, u, _on);
            Gfx.Neon(g, head, headC, u, _on);
            Gfx.Neon(g, lens, _on ? t.IconGlow : t.IconOff, u, _on);
        }
        else
        {
            using (var b = new SolidBrush(bodyC)) g.FillPath(b, body);
            using (var b = new SolidBrush(Gfx.Mix(bodyC, Color.Black, 0.40f))) g.FillPath(b, sw);
            using (var b = new SolidBrush(headC)) g.FillPath(b, head);
            using (var b = new SolidBrush(lensC)) g.FillPath(b, lens);
        }

        // ---- theme decor ----
        Gfx.Decor(g, t, Width, Height, cx, cy - 10 * u, 42 * u, 50 * u, cx + 30 * u, cy + 24 * u, u, _phase, _on);
    }
}

// =========================================================================
// Animated fan  (blades spin up / slow down smoothly, themed decor)
// =========================================================================
public sealed class FanControl : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 16 };
    private float _angle;
    private float _speed;
    private float _target;
    private float _orbit;
    private AppTheme _t = AppTheme.AI;

    public AppTheme Skin
    {
        get => _t;
        set { _t = value; BackColor = value.Card; Invalidate(); }
    }

    public bool IsOn
    {
        get => _target > 0;
        set { _target = value ? 24f : 0f; Invalidate(); }
    }

    public FanControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = _t.Card;
        _timer.Tick += (_, _) =>
        {
            bool was = _speed > 0;
            _speed += (_target - _speed) * 0.04f;   // spin-up / spin-down
            if (_speed < 0.05f && _target == 0) _speed = 0;
            if (_speed > 0)
            {
                _angle = (_angle + _speed) % 360f;
                _orbit = (_orbit + _speed * 0.006f) % 1000f;
            }
            if (_speed > 0 || was) Invalidate();
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
        float cx = Width / 2f, cy = Height / 2f;
        var t = _t;
        bool neon = t.Neon;
        bool spinning = _speed > 0.5f || _target > 0;
        Color bladeC = spinning ? t.IconOn1 : t.IconOff;
        Color ringC = spinning ? t.IconOn2 : t.IconOff;

        // guard
        using (var guard = new GraphicsPath())
        {
            guard.AddEllipse(cx - 44 * u, cy - 44 * u, 88 * u, 88 * u);
            if (neon)
            {
                Gfx.Neon(g, guard, ringC, u, spinning);
            }
            else
            {
                using var gb = new SolidBrush(t.Guard);
                g.FillPath(gb, guard);
                using var gp = new Pen(t.GuardLine, Math.Max(2f, 3 * u));
                g.DrawPath(gp, guard);
            }
        }
        using (var thin = new Pen(Gfx.Mix(neon ? t.IconOff : t.GuardLine, t.Card, 0.55f), 1f))
        {
            g.DrawEllipse(thin, cx - 30 * u, cy - 30 * u, 60 * u, 60 * u);
            g.DrawEllipse(thin, cx - 16 * u, cy - 16 * u, 32 * u, 32 * u);
        }

        // motion blur disc
        if (_speed > 8f)
        {
            int alpha = (int)Math.Min(110f, (_speed - 8f) * 8f);
            using var blur = new SolidBrush(Color.FromArgb(alpha, bladeC));
            g.FillEllipse(blur, cx - 38 * u, cy - 38 * u, 76 * u, 76 * u);
        }

        // blades
        using (var blade = new GraphicsPath())
        {
            blade.AddEllipse(3 * u, -12 * u, 36 * u, 24 * u);
            using var bb = new SolidBrush(bladeC);
            for (int i = 0; i < 3; i++)
            {
                var s = g.Save();
                g.TranslateTransform(cx, cy);
                g.RotateTransform(_angle + i * 120f);
                if (neon) Gfx.Neon(g, blade, bladeC, u, spinning);
                else g.FillPath(bb, blade);
                g.Restore(s);
            }
        }

        // hub
        using (var hub = new GraphicsPath())
        {
            hub.AddEllipse(cx - 9 * u, cy - 9 * u, 18 * u, 18 * u);
            if (neon)
            {
                Gfx.Neon(g, hub, ringC, u, spinning);
            }
            else
            {
                using var hb = new SolidBrush(t.Hub);
                g.FillPath(hb, hub);
                using var hb2 = new SolidBrush(Gfx.Mix(t.Hub, Color.White, 0.80f));
                g.FillEllipse(hb2, cx - 4 * u, cy - 4 * u, 8 * u, 8 * u);
            }
        }

        // ---- theme decor ----
        Gfx.Decor(g, t, Width, Height, cx, cy, 47 * u, 47 * u, cx + 37 * u, cy - 33 * u, u * 0.8f, _orbit, spinning);
    }
}
