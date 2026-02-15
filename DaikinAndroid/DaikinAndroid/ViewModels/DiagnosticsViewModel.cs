using CommunityToolkit.Mvvm.ComponentModel;
using DaikinAndroid.Models;
using DaikinAndroid.Services;

namespace DaikinAndroid.ViewModels;

/// <summary>
/// Diagnostics page ViewModel (Transient).
/// Displays device info, sensor data, and system health.
/// </summary>
public sealed partial class DiagnosticsViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly ISettingsService _settings;

    public DiagnosticsViewModel(MainViewModel main, ISettingsService settings)
    {
        _main = main;
        _settings = settings;
        
        // Subscribe to state updates
        _main.PropertyChanged += OnMainPropertyChanged;
    }

    #region Device State Properties

    public DaikinState DeviceState => _main.DaikinState;

    /// <summary>
    /// Outside temperature formatted for display.
    /// </summary>
    public string OutdoorTemperatureDisplay =>
        TemperatureConverter.Format(DeviceState.OutdoorTempC, _settings.TemperatureUnit);

    /// <summary>
    /// Indoor temperature formatted for display.
    /// </summary>
    public string IndoorTemperatureDisplay =>
        TemperatureConverter.Format(DeviceState.IndoorTempC, _settings.TemperatureUnit);

    /// <summary>
    /// Compressor frequency with unit.
    /// </summary>
    public string CompressorDisplay => $"{DeviceState.CompressorFrequency} Hz";

    /// <summary>
    /// Compressor progress (0-100, assuming max 120 Hz).
    /// </summary>
    public double CompressorProgress => Math.Min(100, DeviceState.CompressorFrequency / 120.0 * 100);

    /// <summary>
    /// System health status text.
    /// </summary>
    public string SystemHealthDisplay => DeviceState.ErrorCode == 0 
        ? "System OK" 
        : $"Error Code: {DeviceState.ErrorCode}";

    /// <summary>
    /// Whether system has an error.
    /// </summary>
    public bool HasError => DeviceState.ErrorCode != 0;

    /// <summary>
    /// Device MAC address.
    /// </summary>
    public string MacAddress => DeviceState.MacAddress;

    /// <summary>
    /// Device firmware version.
    /// </summary>
    public string FirmwareVersion => DeviceState.FirmwareVersion;

    /// <summary>
    /// Device IP address from settings.
    /// </summary>
    public string IpAddress => _settings.DeviceIpAddress;

    /// <summary>
    /// Connection state for UI binding.
    /// </summary>
    public ConnectionState ConnectionState => _main.ConnectionState;

    #endregion

    #region Private Helpers

    private void OnMainPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.DaikinState))
        {
            OnPropertyChanged(nameof(DeviceState));
            OnPropertyChanged(nameof(OutdoorTemperatureDisplay));
            OnPropertyChanged(nameof(IndoorTemperatureDisplay));
            OnPropertyChanged(nameof(CompressorDisplay));
            OnPropertyChanged(nameof(CompressorProgress));
            OnPropertyChanged(nameof(SystemHealthDisplay));
            OnPropertyChanged(nameof(HasError));
            OnPropertyChanged(nameof(MacAddress));
            OnPropertyChanged(nameof(FirmwareVersion));
        }
        else if (e.PropertyName == nameof(MainViewModel.ConnectionState))
        {
            OnPropertyChanged(nameof(ConnectionState));
        }
    }

    #endregion
}
