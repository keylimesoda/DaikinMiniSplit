# Daikin Manager - WinUI 3

A modern Windows 11 app for controlling your Daikin BRP072A43 mini-split system.

## Requirements

- Windows 10 (1809+) or Windows 11
- .NET 8.0 SDK
- Visual Studio 2022 (17.8+) with:
  - .NET Desktop Development workload
  - Windows App SDK C# Templates

## Build & Run

### Option 1: Visual Studio
1. Open `DaikinManager.csproj` in Visual Studio 2022
2. Select `x64` platform
3. Press F5 to build and run

### Option 2: Command Line
```powershell
cd DaikinWinUI
dotnet build
dotnet run
```

## Configuration

Edit the IP address in `Services\DaikinService.cs` or `MainWindow.xaml.cs`:
```csharp
_daikinService = new DaikinService("192.168.183.26");
```

## Features

- 🎨 **Modern Fluent Design** - Windows 11 native look and feel
- 🌡️ **Temperature Dial** - Drag to set temperature (60-90°F)
- 🔄 **Auto-refresh** - Updates every 30 seconds
- 📊 **Diagnostics** - Compressor status, outdoor temp, error codes
- ⚡ **Hysteresis Compensation** - Automatically adjusts setpoint for accurate shutoff

## Project Structure

```
DaikinWinUI/
├── App.xaml(.cs)           # Application entry point
├── MainWindow.xaml(.cs)    # Main window with navigation
├── Controls/
│   └── TemperatureDial.cs  # Custom circular dial control
├── Pages/
│   ├── ControlsPage.xaml   # Temperature, mode, fan controls
│   └── DiagnosticsPage.xaml # Sensors, hardware info
└── Services/
    └── DaikinService.cs    # API communication layer
```

## API Safety Note

The following endpoints are **NOT USED** as they crash the BRP072A43:
- `/aircon/get_week_power`
- `/aircon/get_year_power`
