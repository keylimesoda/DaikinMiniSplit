using DaikinManagerV2.Models;

namespace DaikinManagerV2.Services;

/// <summary>
/// Interface for Daikin device API communication.
/// </summary>
public interface IDaikinApiService : IDisposable
{
    /// <summary>
    /// Get current device state (control info + sensor info + basic info).
    /// Makes parallel HTTP calls and composes immutable result.
    /// </summary>
    Task<DaikinState> GetStateAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Apply all settings in a single API call.
    /// No retry - could double-apply settings.
    /// </summary>
    Task ApplySettingsAsync(
        bool power,
        DaikinMode mode,
        double temperatureCelsius,
        FanSpeed fanSpeed,
        SwingMode swingMode,
        CancellationToken ct = default);
    
    /// <summary>
    /// Update the device IP address.
    /// </summary>
    void SetDeviceAddress(string ipAddress);
}
