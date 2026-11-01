# Serial Monitor

A themed Windows serial monitor and Arduino control panel, built with C# and WinForms on .NET 8. It works like the Arduino IDE Serial Monitor, with an animated dashboard on top for controlling hardware and visualising incoming data.

## Features

**Serial monitor**
- COM port selection with refresh, and baud rates from 300 to 250000
- Line ending options: none, newline, carriage return, or both
- Colour-coded log for received, sent, and system messages, with optional timestamps and autoscroll
- Auto-trimmed log, so it stays fast during long sessions
- DTR reset on connect, matching Arduino IDE behaviour

**Control pages (tabs 1 to 8)**

| Tab | Page | What it does |
|-----|------|--------------|
| 1 | Light and Fan | ON/OFF buttons with a custom character sent for each. Animated flashlight and spinning fan |
| 2 | Car controller | Forward, back, left, right and stop. Animated top-down car with a scrolling road. Default commands are `U`, `D`, `L`, `R`, `S` |
| 3 | 10 switches | Animated toggles that send `SW1:1` or `SW1:0` up to `SW10` |
| 4 | 4x4 keypad | Sends `KEY:<key>` with hover glow and a press ripple |
| 5 | GPS | Radar-style view with a position trail and a latitude/longitude readout |
| 6 | LED control | Colour wheel, RGB sliders and brightness. Sends `LED:B:<0-100>` and `LED:RGB:r,g,b` |
| 7 | Live sensors | Gauge widgets you add yourself and feed from incoming data such as `TEMP:28.5` |
| 8 | 16-channel servo | 16 sliders sending `SV1:90` up to `SV16:90`, plus Center, All 0 and Sweep |

**Themes**

12 switchable themes, chosen by number in the top bar: AI, Cybersecurity, IoT, Robotics, Cloud, Data Science, Blockchain, AR/VR/XR, Embedded, Networking, Quantum and Edge Computing. Each has its own colours, fonts and animated decor.

## Requirements

- Windows
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- An Arduino or other serial device (optional, for real hardware)

## Build and run

```bash
dotnet restore
dotnet run
```

To build a release executable:

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

The output is in `bin/Release/net8.0-windows/win-x64/publish/`.

If you prefer an IDE, open `SerialMonitor.csproj` in Visual Studio and press F5.

## Usage

1. Plug in your board and click **Refresh** to list COM ports.
2. Pick the port and baud rate, then click **CONNECT**.
3. Use the tabs on the left to send commands, and read the output in the log on the right.
4. Close the Arduino IDE Serial Monitor first, because only one program can hold a COM port at a time.

The command characters for the Light/Fan and Car pages can be edited in the text boxes under each button.

## Serial protocol

### Commands sent to the device

| Source | Format |
|--------|--------|
| Light and Fan buttons | User-set characters (default `1`, `2`, `3`, `4`) |
| Car buttons | User-set characters (default `U`, `D`, `L`, `R`, `S`) |
| Switches | `SW<n>:1` or `SW<n>:0` (n = 1 to 10) |
| Keypad | `KEY:<0-9, A-D, *, #>` |
| LED brightness | `LED:B:<0-100>` |
| LED colour | `LED:RGB:<r>,<g>,<b>` |
| Servo | `SV<n>:<0-180>` (n = 1 to 16) |

Each command is followed by the line ending selected in the bottom bar.

### Data the app understands from the device

- **Sensors:** `KEY:value` pairs, for example `TEMP:28.5` or `TEMP:28.5,HUM:60,DIST=35`. A gauge updates when its key matches (case-insensitive).
- **GPS:** any of these formats:
  - NMEA sentences (`$GPRMC` and `$GPGGA`)
  - `LAT:11.01,LON:76.95`
  - plain `11.01,76.95`

### Arduino example

```cpp
void setup() {
  Serial.begin(9600);
}

void loop() {
  Serial.print("TEMP:");
  Serial.println(analogRead(A0) * 0.488);   // appears in the TEMP gauge

  if (Serial.available()) {
    String cmd = Serial.readStringUntil('\n');
    // handle "1", "U", "SW3:1", "LED:B:80", "SV5:120", ...
  }
  delay(500);
}
```

## Adding a sensor widget

1. Open tab 7 and click **＋ Add widget**.
2. Enter the key your Arduino sends, a title, a unit, and the min and max range.
3. Tick **Low value = danger** for things like battery level or distance.
4. Right-click a widget to remove it.

## Project structure

```
SerialMonitor/
├── Program.cs            Entry point
├── MainForm.cs           Main window, themes, serial I/O, pages, drawing helpers,
│                         CardPanel, LightControl, FanControl
├── CarControl.cs         Animated car
├── SwitchControl.cs      Animated toggle switch
├── KeypadControl.cs      4x4 keypad
├── GpsControl.cs         GPS radar
├── GaugeControl.cs       Sensor gauge
├── GaugeDialog.cs        Add-widget dialog
├── LedControl.cs         LED bulb preview
├── PickerControls.cs     GradientSlider and ColorWheel
├── ThemedButton.cs       Button that keeps theme colours when disabled
└── SerialMonitor.csproj
```

## Dependencies

- `System.IO.Ports` 8.0.0

## Troubleshooting

- **Connect fails:** another program, usually the Arduino IDE, has the port open. Close it and try again.
- **No ports listed:** check the USB cable and drivers, then click Refresh.
- **Garbled text:** the baud rate must match `Serial.begin()` on the device.
- **Controls greyed out:** they are enabled only while connected.
