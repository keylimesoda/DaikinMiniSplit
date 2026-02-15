using DaikinAndroid.Models;

namespace DaikinAndroid.Services;

/// <summary>
/// Interface for persistent application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Device IP address. Default: 192.168.183.26
    /// </summary>
    string DeviceIpAddress { get; set; }
    
    /// <summary>
    /// Temperature display unit. Default: Fahrenheit
    /// </summary>
    TemperatureUnit TemperatureUnit { get; set; }
    
    /// <summary>
    /// Auto-refresh interval in seconds. Default: 30
    /// </summary>
    int RefreshIntervalSeconds { get; set; }
    
    /// <summary>
    /// Whether the first-run setup has been completed.
    /// </summary>
    bool HasCompletedSetup { get; set; }
}
