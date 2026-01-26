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
    ├── snowflake.svg           # Cool mode icon (custom SVG)
    └── fan.png                 # Fan mode icon (custom PNG)
```

### 3.2 Icon Definitions

All icons use **Segoe Fluent Icons** (WinUI 3 built-in) unless noted otherwise.

#### Mode Icons (ToggleButtons)
| Mode | Icon | Source | Notes |
|------|------|--------|-------|
| **Cool** | ❄️ Snowflake | `Assets/snowflake.svg` | Custom SVG, use `SvgImageSource` |
| **Heat** | ☀️ Sun/Brightness | FontIcon `&#xE706;` | Segoe Fluent "Brightness" |
| **Auto** | 🔄 Sync | FontIcon `&#xE793;` | Segoe Fluent "Sync" |
| **Dry** | ≋ Triple tilde | TextBlock `≋` | Unicode char in Viewbox, scaled |
| **Fan** | 🌀 Fan blades | `Assets/fan.png` | Custom PNG image |

#### Navigation Icons
| Tab | Icon | Source |
|-----|------|--------|
| Controls | 🎛️ Dial/Adjustment | FontIcon `&#xE9A1;` |
| Diagnostics | 🔧 Diagnostic | FontIcon `&#xE9D9;` |

#### Status & Info Icons
| Purpose | Icon | Source |
|---------|------|--------|
| App header / Temperature | 🌡️ Thermometer | FontIcon `&#xE9CA;` |
| Power button | ⏻ Power | FontIcon `&#xE7E8;` |
| Error state | ⚠️ Warning | FontIcon `&#xE783;` |
| Compressor | ⚙️ Settings | FontIcon `&#xE945;` |
| System health | 🩺 Diagnostic | FontIcon `&#xE946;` |

#### Asset Files to Include
```
Assets/
├── snowflake.svg    # Cool mode - custom snowflake design
└── fan.png          # Fan mode - fan blade icon (20x20 px recommended)
```

**Implementation Notes:**
- SVG icons use `<SvgImageSource UriSource="ms-appx:///Assets/snowflake.svg"/>`
- PNG icons use `<Image Source="ms-appx:///Assets/fan.png" Width="20" Height="20"/>`
- The Dry mode uses a creative approach: `≋` (triple tilde) character in a Viewbox for consistent sizing
- All FontIcons should use `FontSize="20"` for mode buttons, larger for headers

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

### 3.3 Dependency Injection & Lifetimes

```csharp
// In App.xaml.cs
services.AddSingleton<IDaikinApiService, DaikinApiService>();
services.AddSingleton<ISettingsService, SettingsService>();
services.AddSingleton<MainViewModel>();      // Owns timer, survives navigation
services.AddTransient<ControlsViewModel>();  // Fresh per navigation
services.AddTransient<DiagnosticsViewModel>();
```

| Component | Lifetime | Rationale |
|-----------|----------|----------|
| `DaikinApiService` | Singleton | Owns HttpClient, reuse connections |
| `SettingsService` | Singleton | Single source of truth for settings |
| `MainViewModel` | Singleton | Owns refresh timer, connection state |
| `ControlsViewModel` | Transient | Gets fresh state from MainViewModel |
| `DiagnosticsViewModel` | Transient | Stateless display of current data |

### 3.4 Navigation & Data Flow

**Tab switching is instant** — no network calls on navigation:

```
┌─────────────────────────────────────────────────────────────┐
│                 MainViewModel (Singleton)                    │
│  ┌─────────────┐  ┌─────────────────┐  ┌────────────────┐   │
│  │ DaikinState │  │ ConnectionState │  │ RefreshTimer   │   │
│  │  (cached)   │  │                 │  │ (always runs)  │   │
│  └──────┬──────┘  └─────────────────┘  └────────────────┘   │
│         │                                                    │
│         │  ← Refreshes every 30s in background               │
└─────────┼────────────────────────────────────────────────────┘
          │
    ┌─────┴─────┐
    │           │
    ▼           ▼
┌────────┐  ┌────────┐
│Controls│  │Diagnos-│   ← Transient VMs read cached state
│  VM    │  │tics VM │     on construction (instant)
└────────┘  └────────┘
```

**Key behaviors:**
- **On tab switch:** ViewModel created, reads `MainViewModel.DaikinState` — no wait
- **Background refresh:** Timer runs regardless of active tab
- **State propagation:** `MainViewModel.DaikinState` changes notify all listeners via `INotifyPropertyChanged`

```csharp
public partial class ControlsViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    
    public ControlsViewModel(MainViewModel main)
    {
        _main = main;
        // Instant: just read cached state, no await
        SyncFromDeviceState(_main.DaikinState);
        
        // Subscribe to future updates
        _main.PropertyChanged += OnMainPropertyChanged;
    }
    
    private void OnMainPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.DaikinState))
            SyncFromDeviceState(_main.DaikinState);
    }
}
```

### 3.5 State Management

```csharp
public enum ConnectionState
{
    Disconnected,   // No connection attempted
    Connecting,     // Request in flight
    Connected,      // Last request succeeded
    Error           // Last request failed (with message)
}
```

The `MainViewModel` owns:
- `ConnectionState` (affects status badge)
- `DaikinState` (latest device data from API)
- `SecondsUntilRefresh` (countdown for circular indicator)
- Auto-refresh timer lifecycle
- `CancellationTokenSource` for in-flight requests
- `ErrorMessage` (for Error state)

### 3.6 Staged Settings Model

The `ControlsViewModel` maintains two sets of values:

```csharp
// From device (read-only, updated on refresh)
public DaikinState DeviceState { get; }

// User's pending changes (editable)
public bool StagedPower { get; set; }
public DaikinMode StagedMode { get; set; }
public double StagedTemperature { get; set; }
public FanSpeed StagedFanSpeed { get; set; }
public SwingMode StagedSwingMode { get; set; }

// True if staged values differ from device
public bool HasPendingChanges { get; }
```

**Behavior:**
- On refresh: `DeviceState` updates, `Staged*` values reset to match
- User edits: `Staged*` values change, `HasPendingChanges` becomes true
- Apply button: sends `Staged*` values to device, then refreshes
- Visual indicator: Apply button could show "*" or highlight when `HasPendingChanges`

### 3.7 Cancellation Strategy

```csharp
public partial class MainViewModel : ObservableObject
{
    private CancellationTokenSource? _refreshCts;
    
    private async Task RefreshAsync()
    {
        // Cancel any in-flight request
        _refreshCts?.Cancel();
        _refreshCts = new CancellationTokenSource();
        
        try
        {
            var state = await _apiService.GetStateAsync(_refreshCts.Token);
            // Update state...
        }
        catch (OperationCanceledException)
        {
            // Intentional cancellation, ignore
        }
    }
}
```

**Cancel on:**
- New refresh request starts (debounce overlapping)
- Window closing
- Manual disconnect

**Do NOT cancel on:**
- Page navigation (MainViewModel is singleton, keeps refreshing)

### 3.8 Retry Policy

```csharp
// For GET requests only (idempotent reads)
private async Task<T> WithRetryAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct)
{
    int[] delays = { 1000, 2000, 4000 }; // Exponential backoff
    
    for (int attempt = 0; attempt <= delays.Length; attempt++)
    {
        try
        {
            return await operation(ct);
        }
        catch (HttpRequestException) when (attempt < delays.Length)
        {
            await Task.Delay(delays[attempt], ct);
        }
    }
    throw; // Final attempt failed
}
```

| Request Type | Retry? | Rationale |
|--------------|--------|----------|
| `GetStateAsync` | Yes (3 attempts) | Reads are idempotent |
| `ApplySettingsAsync` | No | Could double-apply settings |

### 3.9 Thread Safety

The `DaikinApiService.GetStateAsync` makes 3 parallel HTTP calls. To avoid race conditions:

```csharp
public async Task<DaikinState> GetStateAsync(CancellationToken ct)
{
    // Parallel fetch - all calls start together
    var basicTask = GetBasicInfoAsync(ct);
    var controlTask = GetControlInfoAsync(ct);
    var sensorTask = GetSensorInfoAsync(ct);
    
    await Task.WhenAll(basicTask, controlTask, sensorTask);
    
    // Compose immutable record from results (no shared mutable state)
    return new DaikinState(
        IsPoweredOn: controlTask.Result.Power,
        Mode: controlTask.Result.Mode,
        SetTemperatureC: controlTask.Result.Temperature,
        FanSpeed: controlTask.Result.FanSpeed,
        SwingMode: controlTask.Result.SwingMode,
        IndoorTempC: sensorTask.Result.IndoorTemp,
        OutdoorTempC: sensorTask.Result.OutdoorTemp,
        CompressorFrequency: sensorTask.Result.CompressorFreq,
        ErrorCode: sensorTask.Result.ErrorCode,
        MacAddress: basicTask.Result.Mac,
        FirmwareVersion: basicTask.Result.Version,
        LastUpdated: DateTime.Now
    );
}
```

**Key principle:** Build immutable results from each call, compose into final record. No mutable shared state.

### 3.10 Disposal Pattern

```csharp
public sealed class DaikinApiService : IDaikinApiService, IDisposable
{
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
    private bool _disposed;
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly DispatcherTimer _refreshTimer;
    private readonly DispatcherTimer _countdownTimer;
    private CancellationTokenSource? _refreshCts;
    
    public void Dispose()
    {
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshTimer.Stop();
        _countdownTimer.Stop();
    }
}
```

**Disposal triggers:**
- `App.OnSuspending` or window close
- DI container disposal (if using `IHost`)

### 3.11 Input Debounce

The arc dial fires continuous value changes during drag. Debounce to prevent UI jank:

```csharp
private DispatcherTimer? _debounceTimer;

private void OnDialValueChanged(double newValue)
{
    _debounceTimer?.Stop();
    _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
    _debounceTimer.Tick += (s, e) =>
    {
        _debounceTimer.Stop();
        StagedTemperature = newValue;  // Actually update binding
    };
    _debounceTimer.Start();
}
```

**Debounce values:**
| Control | Debounce | Rationale |
|---------|----------|----------|
| Temperature dial | 100ms | Smooth drag, prevent binding thrash |
| Mode buttons | None | Discrete clicks |
| Dropdowns | None | Single selection |

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
┌─────────────────────────────────────┐
│  🌡️ Daikin Manager    [Connected]  │  ← Title bar with status badge
├─────────────────────────────────────┤
│  ┌─────────────┬─────────────┐      │
│  │  Controls   │ Diagnostics │      │  ← Tab buttons (not NavigationView)
│  └─────────────┴─────────────┘      │
├─────────────────────────────────────┤
│                                     │
│        [ Page Content ]             │  ← Frame
│                                     │
│                                     │
│                   Updated 15s ago   │  ← Subtle timestamp
└─────────────────────────────────────┘
```

**Navigation Choice:** Simple tab buttons instead of NavigationView. With only 2 pages, a hamburger menu adds unnecessary complexity.

### 4.3 Controls Page Layout

```
┌─────────────────────────────────────┐
│  ┌─────────────────────────────┐    │
│  │    ⏻  POWER: ON             │    │  ← Prominent power button (E7E8)
│  └─────────────────────────────┘    │     Green=ON, Gray=OFF
├─────────────────────────────────────┤
│  Mode                               │
│  ┌─────┬─────┬─────┬─────┬─────┐   │
│  │ ❄️  │ ☀️  │ 🔄  │ ≋  │ 🌀  │   │  ← Icon buttons with labels
│  │Cool │Heat │Auto │ Dry │ Fan │   │     Selected = highlighted
│  │(SVG)│E706 │E793 │text │(PNG)│   │
│  └─────┴─────┴─────┴─────┴─────┘   │
├─────────────────────────────────────┤
│                                     │
│            ╭─────────╮              │
│          ╭─┤  ○      ├─╮            │  ← Draggable arc dial
│         ╱              ╲            │     Orange/blue arc based on mode
│        │                │           │
│        │     76°F       │           │  ← Setpoint (large, centered)
│        │                │           │
│         ╲              ╱            │
│          ╰────────────╯             │
│         Actual: 73.4°F              │  ← Indoor temp (small, below)
│                                     │
├─────────────────────────────────────┤
│  Fan Speed       ┌────────────────┐ │
│                  │ Auto         ▼ │ │  ← ComboBox dropdown
│                  └────────────────┘ │
├─────────────────────────────────────┤
│  Swing           ┌────────────────┐ │
│                  │ Vertical     ▼ │ │  ← ComboBox dropdown
│                  └────────────────┘ │
├─────────────────────────────────────┤
│  ┌─────────────────────────────┐    │
│  │      APPLY SETTINGS         │    │  ← Primary action button
│  └─────────────────────────────┘    │     Orange accent color
└─────────────────────────────────────┘
```

**Key Design Decisions:**
- **Power button** is prominent and colored (most common action)
- **Arc dial** for temperature - tactile, Nest-like, intuitive for HVAC
- **Setpoint vs Actual** clearly differentiated (large vs small)
- **Dropdowns** for Fan/Swing (5+ options don't fit well as buttons)
- **Explicit Apply button** batches changes (fewer API calls, prevents accidents)

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

| State | Status Badge | Page Content | Notes |
|-------|--------------|--------------|-------|
| `Disconnected` | Gray "Not Connected" | "Connect" button | Never attempted |
| `Connecting` | Blue "Connecting..." | ProgressRing overlay | Request in flight |
| `Connected` | Green "Connected" | Normal controls | Shows refresh countdown |
| `Error` | Red "Connection Lost" | Error message + "Retry" | Distinct from Disconnected |

### 4.6 Refresh Countdown Indicator

A small circular progress indicator shows time until next auto-refresh:

```
┌─────────────────────────────────────┐
│  🌡️ Daikin Manager    [Connected] ◐ │  ← Circular countdown (30→0s)
└─────────────────────────────────────┘

◯ = Just refreshed (full)
◔ = 75% remaining
◑ = 50% remaining  
◕ = 25% remaining
● = Refreshing now (spinning)
⚠ = Refresh failed (tap to retry)
```

**Implementation:**
```csharp
// In MainViewModel
public int SecondsUntilRefresh { get; private set; } = 30;
public bool IsRefreshing { get; private set; }
public bool RefreshFailed { get; private set; }

// 1-second countdown timer
private void OnCountdownTick()
{
    SecondsUntilRefresh--;
    if (SecondsUntilRefresh <= 0)
        _ = RefreshAsync();
}
```

**Visual states:**
- Normal: Arc fills counter-clockwise as countdown progresses
- Refreshing: Indeterminate spin animation
- Failed: Red warning icon, tappable to retry

### 4.7 Loading States

| Action | UI Feedback |
|--------|-------------|
| Initial connect | Full-page ProgressRing with "Connecting..." |
| Apply settings | Apply button shows spinner, text becomes "Applying..." |
| Auto-refresh | Countdown indicator spins |
| Manual refresh | Countdown indicator spins |

### 4.8 Accessibility Requirements

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
    Auto = 0,   // Automatic mode selection
    Dry = 2,    // Dehumidify
    Cool = 3,   // Cooling
    Heat = 4,   // Heating
    Fan = 6     // Fan only (no compressor)
}
```

**UI Display Order:** Cool, Heat, Auto, Dry, Fan (by frequency of use)

### 7.2 FanSpeed
```csharp
public enum FanSpeed
{
    Auto,   // "A" - Automatic speed
    Quiet,  // "B" - Silent/low noise mode
    Low,    // "3"
    Medium, // "5"
    High    // "7"
}
```

**Note:** Use "Quiet" (not "Silent") in UI - implies reduced noise while still having airflow.

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
    /// <summary>Get current device state (control info + sensor info + basic info)</summary>
    Task<DaikinState> GetStateAsync(CancellationToken ct = default);
    
    /// <summary>Apply all settings in a single API call</summary>
    Task ApplySettingsAsync(
        bool power,
        DaikinMode mode,
        double temperatureCelsius,
        FanSpeed fanSpeed,
        SwingMode swingMode,
        CancellationToken ct = default);
}
```

**Design Note:** Single `ApplySettingsAsync` method instead of individual setters. This matches the batched UI pattern (user adjusts multiple settings, then clicks Apply).

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

## 10. Temperature Conversion & Validation

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

### 10.1 Mode-Aware Temperature Validation

Not all modes support temperature control:

```csharp
public static class ModeValidation
{
    /// <summary>
    /// Returns whether temperature control is applicable for the given mode.
    /// </summary>
    public static bool SupportsTemperature(DaikinMode mode) => mode switch
    {
        DaikinMode.Cool => true,
        DaikinMode.Heat => true,
        DaikinMode.Auto => true,
        DaikinMode.Dry => false,  // Dehumidify - no temp control
        DaikinMode.Fan => false,  // Fan only - no temp control
        _ => false
    };
    
    /// <summary>
    /// Get the valid temperature range for a mode (in Celsius).
    /// Returns null if mode doesn't support temperature.
    /// </summary>
    public static (double Min, double Max)? GetTemperatureRange(DaikinMode mode) => mode switch
    {
        DaikinMode.Cool => (18.0, 30.0),
        DaikinMode.Heat => (18.0, 30.0),
        DaikinMode.Auto => (18.0, 30.0),
        _ => null
    };
}
```

**UI Behavior:**
- When mode is `Dry` or `Fan`: Disable/hide temperature dial
- Show "N/A" or dim the temperature display
- Apply button should not send `stemp` parameter for these modes

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

*Specification Version: 1.2*  
*Last Updated: January 26, 2026*

---

## Revision History

| Version | Date | Changes |
|---------|------|--------|
| 1.2 | 2026-01-26 | Architecture review: DI lifetimes, cancellation strategy, retry policy, thread safety, disposal pattern, input debounce, refresh countdown indicator, mode-aware temperature validation |
| 1.1 | 2026-01-26 | UX review: Tab nav, arc dial, Auto mode, batched Apply, first-run flow, loading states |
| 1.0 | 2026-01-26 | Initial specification |
