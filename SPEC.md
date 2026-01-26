# Daikin Mini Split Controller - Application Specification

## Overview

A Windows desktop application for controlling Daikin mini split HVAC units over the local network. The app provides real-time monitoring and control of temperature, modes, fan speed, and swing settings.

---

## 1. Target Platform

| Attribute | Value |
|-----------|-------|
| Framework | WinUI 3 (.NET 8) |
| Architecture | MVVM with CommunityToolkit.Mvvm |
| Minimum Windows | Windows 10 19041+ |
| Platform | x64 |

---

## 2. Device API

### 2.1 Connection
- **Protocol:** HTTP GET requests
- **Discovery:** Device at configurable IP address (default stored in settings)
- **Timeout:** 10 seconds per request
- **Polling:** 30-second auto-refresh when connected

### 2.2 Endpoints

| Endpoint | Purpose | Response Format |
|----------|---------|-----------------|
| `/aircon/get_control_info` | Current settings | URL-encoded key=value pairs |
| `/aircon/set_control_info` | Update settings | URL-encoded key=value pairs |
| `/aircon/get_sensor_info` | Temperature sensors | URL-encoded key=value pairs |
| `/common/basic_info` | Device info (MAC, firmware) | URL-encoded key=value pairs |

### 2.3 API Parameters

#### Control Info Fields
| Field | Description | Values |
|-------|-------------|--------|
| `pow` | Power state | `0` = Off, `1` = On |
| `mode` | Operating mode | `2` = Dry, `3` = Cool, `4` = Heat, `6` = Fan |
| `stemp` | Set temperature | `18.0` - `30.0` (Celsius, 0.5 increments) |
| `f_rate` | Fan speed | `A` = Auto, `B` = Silent, `3` = Low, `5` = Medium, `7` = High |
| `f_dir` | Swing mode | `0` = Off, `1` = Vertical, `2` = Horizontal, `3` = Both |
| `shum` | Humidity (Dry mode) | `0` = Auto, `1`-`3` = Levels |

#### Sensor Info Fields
| Field | Description | Unit |
|-------|-------------|------|
| `htemp` | Indoor temperature | Celsius |
| `otemp` | Outdoor temperature | Celsius |
| `cmpfreq` | Compressor frequency | Hz (0 = idle) |

#### Basic Info Fields
| Field | Description |
|-------|-------------|
| `mac` | MAC address |
| `ver` | Firmware version |
| `err` | Error code (0 = OK) |

### 2.4 Hysteresis Compensation

The Daikin unit applies a 1°C hysteresis offset internally. When setting temperature:
- **Display to API:** Subtract 1°C (`stemp = display - 1`)
- **API to Display:** Add 1°C (`display = stemp + 1`)

---

## 3. Application Architecture

### 3.1 Project Structure

```
DaikinManager/
├── App.xaml                    # Application entry
├── MainWindow.xaml             # Shell with NavigationView
├── Models/
│   ├── DaikinState.cs          # Immutable device state DTO
│   ├── DaikinMode.cs           # Mode enum
│   ├── FanSpeed.cs             # Fan speed enum
│   └── SwingMode.cs            # Swing mode enum
├── Services/
│   ├── IDaikinApiService.cs    # API interface
│   ├── DaikinApiService.cs     # HTTP implementation
│   ├── ISettingsService.cs     # Settings interface
│   └── SettingsService.cs      # Local storage implementation
├── ViewModels/
│   ├── MainViewModel.cs        # App-wide state (connection, timer)
│   ├── ControlsViewModel.cs    # Controls page logic
│   └── DiagnosticsViewModel.cs # Diagnostics page logic
├── Views/
│   ├── ControlsPage.xaml       # Main controls UI
│   └── DiagnosticsPage.xaml    # Diagnostic info UI
├── Converters/
│   └── TemperatureConverter.cs # C↔F conversion
└── Assets/
    └── Icons/                  # Mode and status icons
```

### 3.2 Data Flow

```
┌─────────────┐    ┌──────────────┐    ┌─────────────┐
│   View      │◄──►│  ViewModel   │◄──►│   Service   │
│  (XAML)     │    │ (Observable) │    │   (HTTP)    │
└─────────────┘    └──────────────┘    └─────────────┘
      ▲                   ▲                   ▲
      │                   │                   │
   Binding          INotifyProp          async/await
```

### 3.3 State Management

```csharp
public enum ConnectionState
{
    Disconnected,   // No connection attempted
    Connecting,     // Request in flight
    Connected,      // Last request succeeded
    Error           // Last request failed
}
```

The `MainViewModel` owns:
- `ConnectionState` (affects status badge)
- `DaikinState` (latest device data)
- Auto-refresh timer lifecycle

Page ViewModels observe `MainViewModel.DaikinState` and expose UI-specific properties.

---

## 4. User Interface

### 4.1 Window Specifications

| Property | Value |
|----------|-------|
| Initial Size | 450 × 880 px |
| Min Size | 400 × 600 px |
| Title Bar | Custom (extended into content) |
| Theme | System default (light/dark) |

### 4.2 Navigation Structure

```
┌────────────────────────────────┐
│  ☰  Daikin Controller    ● ── │  ← Custom title bar with status
├────────────────────────────────┤
│  ┌──────────────────────────┐  │
│  │  🌡️ Controls            │  │  ← NavigationView
│  │  📊 Diagnostics          │  │
│  └──────────────────────────┘  │
├────────────────────────────────┤
│                                │
│     [ Page Content ]           │  ← Frame
│                                │
└────────────────────────────────┘
```

### 4.3 Controls Page Layout

```
┌────────────────────────────────┐
│        CURRENT TEMP            │
│           72°F                 │  ← Large display
│      Indoor  │  Outdoor        │
│       72°F   │   45°F          │
├────────────────────────────────┤
│  Power                    [◉]  │  ← ToggleSwitch
├────────────────────────────────┤
│  Mode                          │
│  ┌────┬────┬────┬────┐        │
│  │Cool│Heat│ Dry│Fan │        │  ← SegmentedControl or RadioButtons
│  └────┴────┴────┴────┘        │
├────────────────────────────────┤
│  Set Temperature               │
│      ┌─────────────┐           │
│   -  │    70°F     │  +        │  ← NumberBox with buttons
│      └─────────────┘           │
│   ════════════════════         │  ← Slider (60-86°F)
├────────────────────────────────┤
│  Fan Speed                     │
│  ┌────┬────┬────┬────┬────┐   │
│  │Auto│Quiet│ Lo │Med │ Hi │   │
│  └────┴────┴────┴────┴────┘   │
├────────────────────────────────┤
│  Swing                         │
│  ┌────┬────┬────┬────┐        │
│  │Off │Vert│Horz│Both│        │
│  └────┴────┴────┴────┘        │
└────────────────────────────────┘
```

### 4.4 Diagnostics Page Layout

```
┌────────────────────────────────┐
│  System Status                 │
│  ┌──────────────────────────┐  │
│  │ ✓ System Health: OK      │  │  ← Or error code
│  └──────────────────────────┘  │
├────────────────────────────────┤
│  Compressor                    │
│  ┌──────────────────────────┐  │
│  │ Frequency: 45 Hz         │  │
│  │ ████████████░░░░░░░░     │  │  ← Progress bar
│  └──────────────────────────┘  │
├────────────────────────────────┤
│  Device Info                   │
│  ┌──────────────────────────┐  │
│  │ MAC: AA:BB:CC:DD:EE:FF   │  │
│  │ Firmware: 1.2.3          │  │
│  │ IP: 192.168.1.100        │  │
│  └──────────────────────────┘  │
├────────────────────────────────┤
│  Raw API Data                  │
│  ┌──────────────────────────┐  │
│  │ pow=1,mode=3,stemp=21... │  │  ← Expander with raw response
│  └──────────────────────────┘  │
├────────────────────────────────┤
│         [ Refresh Data ]       │  ← Manual refresh button
└────────────────────────────────┘
```

### 4.5 Connection States UI

| State | Status Badge | Page Content |
|-------|--------------|--------------|
| `Disconnected` | Gray "Offline" | "Connect" button |
| `Connecting` | Blue "Connecting..." | ProgressRing + "Connecting..." |
| `Connected` | Green "Connected" | Normal controls |
| `Error` | Red "Offline" | Error message + "Retry" button |

### 4.6 Accessibility Requirements

- All controls must have `AutomationProperties.Name`
- Keyboard navigation for all interactive elements
- Minimum touch target size: 44×44 px
- Color contrast ratio: 4.5:1 minimum
- Screen reader announcements for state changes

---

## 5. Settings

### 5.1 User-Configurable Settings

| Setting | Type | Default | Storage |
|---------|------|---------|---------|
| Device IP Address | string | `192.168.183.26` | Local settings |
| Temperature Unit | enum | Fahrenheit | Local settings |
| Auto-Refresh Interval | int (seconds) | 30 | Local settings |
| Theme | enum | System | Local settings |

### 5.2 Settings Page (Future)

```
┌────────────────────────────────┐
│  Device                        │
│  IP Address: [192.168.1.100 ]  │
│              [ Test Connection]│
├────────────────────────────────┤
│  Display                       │
│  Temperature: (•) °F  ( ) °C   │
│  Theme:       [System     ▼]   │
├────────────────────────────────┤
│  Refresh                       │
│  Interval:    [30] seconds     │
└────────────────────────────────┘
```

---

## 6. Error Handling

### 6.1 Error Categories

| Category | Example | User Message | Recovery |
|----------|---------|--------------|----------|
| Network | Timeout | "Unable to connect to device" | Retry button |
| Network | DNS/IP | "Device not found" | Check settings |
| Device | Error code | "Device error: E03" | Show diagnostics |
| Validation | Invalid temp | (prevent input) | Constrain UI |

### 6.2 Error Display

- **Transient errors:** InfoBar at top of page (auto-dismiss after 5s)
- **Connection errors:** Full-page error state with retry
- **Device errors:** Persistent banner until resolved

---

## 7. Enums

### 7.1 DaikinMode
```csharp
public enum DaikinMode
{
    Cool = 3,
    Heat = 4,
    Dry = 2,
    Fan = 6
}
```

### 7.2 FanSpeed
```csharp
public enum FanSpeed
{
    Auto,   // "A"
    Silent, // "B"
    Low,    // "3"
    Medium, // "5"
    High    // "7"
}
```

### 7.3 SwingMode
```csharp
public enum SwingMode
{
    Off = 0,
    Vertical = 1,
    Horizontal = 2,
    Both = 3
}
```

---

## 8. Data Models

### 8.1 DaikinState (Immutable DTO)
```csharp
public record DaikinState(
    bool IsPoweredOn,
    DaikinMode Mode,
    double SetTemperatureC,
    FanSpeed FanSpeed,
    SwingMode SwingMode,
    double IndoorTempC,
    double OutdoorTempC,
    int CompressorFrequency,
    int ErrorCode,
    string MacAddress,
    string FirmwareVersion,
    DateTime LastUpdated
);
```

---

## 9. Service Interfaces

### 9.1 IDaikinApiService
```csharp
public interface IDaikinApiService
{
    Task<DaikinState> GetStateAsync(CancellationToken ct = default);
    Task SetPowerAsync(bool on, CancellationToken ct = default);
    Task SetModeAsync(DaikinMode mode, CancellationToken ct = default);
    Task SetTemperatureAsync(double celsius, CancellationToken ct = default);
    Task SetFanSpeedAsync(FanSpeed speed, CancellationToken ct = default);
    Task SetSwingModeAsync(SwingMode swing, CancellationToken ct = default);
}
```

### 9.2 ISettingsService
```csharp
public interface ISettingsService
{
    string DeviceIpAddress { get; set; }
    TemperatureUnit TemperatureUnit { get; set; }
    int RefreshIntervalSeconds { get; set; }
    AppTheme Theme { get; set; }
}
```

---

## 10. Temperature Conversion

```csharp
public static class TemperatureConverter
{
    public static double CelsiusToFahrenheit(double c) => c * 9.0 / 5.0 + 32.0;
    public static double FahrenheitToCelsius(double f) => (f - 32.0) * 5.0 / 9.0;
    
    // Constrain to valid range (18-30°C / 64-86°F)
    public static double ClampCelsius(double c) => Math.Clamp(c, 18.0, 30.0);
    public static double ClampFahrenheit(double f) => Math.Clamp(f, 64.0, 86.0);
}
```

---

## 11. Dependencies

| Package | Purpose |
|---------|---------|
| `Microsoft.WindowsAppSDK` | WinUI 3 runtime |
| `CommunityToolkit.Mvvm` | MVVM infrastructure |
| `CommunityToolkit.WinUI.UI.Controls` | Additional controls |
| `Microsoft.Extensions.DependencyInjection` | DI container |

---

## 12. Future Enhancements

1. **Multi-device support** - Manage multiple units
2. **Scheduling** - Time-based automation
3. **Energy monitoring** - Track compressor usage over time
4. **Widgets** - Windows 11 widget for quick status/control
5. **System tray** - Minimize to tray with quick controls
6. **Notifications** - Alert when outdoor temp crosses threshold

---

## 13. File Manifest

| File | Purpose | Lines (est.) |
|------|---------|--------------|
| `App.xaml.cs` | DI setup, activation | ~50 |
| `MainWindow.xaml` | Shell layout | ~60 |
| `MainWindow.xaml.cs` | Window code-behind (minimal) | ~30 |
| `MainViewModel.cs` | Connection state, timer | ~150 |
| `ControlsViewModel.cs` | Control logic | ~200 |
| `ControlsPage.xaml` | Controls UI | ~250 |
| `DiagnosticsViewModel.cs` | Diagnostics logic | ~80 |
| `DiagnosticsPage.xaml` | Diagnostics UI | ~150 |
| `DaikinApiService.cs` | HTTP implementation | ~200 |
| `SettingsService.cs` | Local storage | ~60 |
| `Models/*.cs` | DTOs and enums | ~50 |
| **Total** | | **~1,300** |

---

## 14. Implementation Phases

### Phase 1: Core Infrastructure
- [ ] Project setup with DI
- [ ] Models and enums
- [ ] Service interfaces and implementations
- [ ] MainViewModel with connection state

### Phase 2: Controls Page
- [ ] ControlsViewModel
- [ ] ControlsPage XAML
- [ ] Data binding and commands
- [ ] Temperature slider with constraints

### Phase 3: Diagnostics Page
- [ ] DiagnosticsViewModel
- [ ] DiagnosticsPage XAML
- [ ] Raw data display

### Phase 4: Polish
- [ ] Error handling and InfoBars
- [ ] Loading states
- [ ] Accessibility audit
- [ ] Settings persistence

### Phase 5: Settings Page (Optional)
- [ ] Settings UI
- [ ] IP configuration
- [ ] Theme selection

---

*Specification Version: 1.0*  
*Last Updated: January 26, 2026*
