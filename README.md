# Daikin Mini Split Controller

A local-network HVAC controller for **Daikin BRP072A43** mini-split Wi-Fi adapters. No cloud, no account — just direct HTTP control over your local network.

Built with **WinUI 3** for Windows and ported to **Android** via [Uno Platform](https://platform.uno/).

![.NET](https://img.shields.io/badge/.NET_8%2F10-512BD4?logo=dotnet&logoColor=white)
![WinUI 3](https://img.shields.io/badge/WinUI_3-0078D4?logo=windows&logoColor=white)
![Android](https://img.shields.io/badge/Android-3DDC84?logo=android&logoColor=white)
![Uno Platform](https://img.shields.io/badge/Uno_Platform-7A00E6?logo=uno&logoColor=white)

---

## Features

- **Real-time control** — Temperature, mode (Cool / Heat / Auto / Dry / Fan), fan speed, and swing direction
- **Custom temperature dial** — Draggable arc knob with live Fahrenheit/Celsius display
- **Staged settings** — Edit multiple values, then apply in one tap
- **Auto-refresh** — Polls device state every 30 seconds in the background
- **Weekly scheduling** — Create up to 3 named schedules with 6 events per day
- **Diagnostics** — Live indoor/outdoor temperature, compressor frequency, error codes, raw API responses
- **Dark theme** — Designed for always-on display or casual use
- **Connection resilience** — Exponential backoff retry on GET requests, status badge indicators

## Supported Hardware

| Attribute | Value |
|-----------|-------|
| **Adapter** | Daikin BRP072A43 (North American market) |
| **Chipset** | Murata / AMPAK (2.4 GHz Wi-Fi) |
| **Protocol** | HTTP GET on port 80 — unauthenticated |
| **Discovery** | Configurable IP address (default stored in settings) |

> **Note:** This adapter uses a legacy local REST API. The energy usage endpoints (`get_week_power`, `get_year_power`) are intentionally excluded — they crash the BRP072A43's single-threaded web server and require a breaker reset.

## Architecture

```
MVVM + Dependency Injection
├── Models/          Immutable DTOs (DaikinState record), enums, converters
├── Services/        HTTP API client, settings persistence
├── ViewModels/      CommunityToolkit.Mvvm ObservableObjects
├── Views/           XAML pages (Controls, Schedule, Diagnostics)
└── Controls/        Custom TemperatureDial (canvas-drawn arc + knob)
```

### Data Flow

```
┌───────────┐    ┌──────────────┐    ┌─────────────┐    ┌─────────────┐
│   XAML    │◄──►│  ViewModel   │◄──►│  API Service │───►│ Daikin Unit │
│  Views    │    │ (Observable) │    │   (HTTP)     │    │ (port 80)   │
└───────────┘    └──────────────┘    └─────────────┘    └─────────────┘
```

- **MainViewModel** (singleton) — owns the refresh timer, connection state, and cached `DaikinState`
- **ControlsViewModel** (singleton) — staged settings with dirty tracking
- **DiagnosticsViewModel** (transient) — reads cached state, zero network calls on navigation
- Tab switching is instant — no round-trips

## Projects

| Project | Target | Framework | Description |
|---------|--------|-----------|-------------|
| **DaikinManagerV2** | Windows | WinUI 3 / .NET 8 | Primary desktop app |
| **DaikinAndroid** | Android + Windows | Uno Platform 6.5 / .NET 10 | Mobile port |
| **DaikinWinUI** | Windows | WinUI 3 / .NET 8 | Original prototype (archived) |

## Building

### Windows (DaikinManagerV2)

```powershell
cd DaikinManagerV2
dotnet build /p:Platform=x64
dotnet run /p:Platform=x64
```

**Requirements:** .NET 8 SDK, Windows 10 19041+, Windows App SDK 1.4

### Android (DaikinAndroid)

```powershell
cd DaikinAndroid/DaikinAndroid

# Set environment
$env:JAVA_HOME = "C:\Program Files\Microsoft\jdk-21.0.10.7-hotspot"
$env:ANDROID_HOME = "D:\Android\Sdk"

# Build APK
dotnet build -f net10.0-android -p:EmbedAssembliesIntoApk=true

# Install on device/emulator
adb install -r bin/Debug/net10.0-android/com.companyname.daikinandroid-Signed.apk
```

**Requirements:** .NET 10 SDK, JDK 21, Android SDK (API 35+, build-tools 35)

## Configuration

The device IP address is stored via `ISettingsService`. Default: `192.168.183.26`.

To change it, update the IP in `Services/SettingsService.cs` or through the app's settings.

## API Reference

The BRP072A43 exposes a local HTTP API on port 80. All requests are `GET`, even writes.

| Endpoint | Purpose |
|----------|---------|
| `/aircon/get_control_info` | Current power, mode, temperature, fan, swing |
| `/aircon/set_control_info` | Apply settings (requires all params) |
| `/aircon/get_sensor_info` | Indoor/outdoor temp, compressor Hz, error codes |
| `/common/basic_info` | MAC address, firmware version |
| `/aircon/get_scdltimer` | Read weekly schedule data |
| `/aircon/set_scdltimer` | Write weekly schedule (atomic, all 7 days) |

### Key Behaviors

- **Hysteresis compensation:** The unit applies a 1°C internal offset. The app adds/subtracts 1°C transparently so the displayed temperature matches what you'd expect.
- **Compressor guard:** Rapid OFF→ON toggling causes a 3-minute safety delay before the compressor restarts.
- **Poll rate:** The adapter is single-threaded — polling faster than every 5–10 seconds risks timeouts.

## Acknowledgments

This project relies on the community-documented **Daikin BRP072A43 local API**, reverse-engineered and shared by contributors across the home automation community. Key references:

- [**ael-code/daikin-control**](https://github.com/ael-code/daikin-control) — Comprehensive Daikin Wi-Fi API documentation and Python client
- [**Apollon77/daikin-controller-cloud**](https://github.com/Apollon77/daikin-controller-cloud) — Daikin cloud and local API research
- [**Home Assistant Daikin integration**](https://www.home-assistant.io/integrations/daikin/) — Community-maintained integration with extensive endpoint documentation

## License

Private project — not licensed for redistribution.
