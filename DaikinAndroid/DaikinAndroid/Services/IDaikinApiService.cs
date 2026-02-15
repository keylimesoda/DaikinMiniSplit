using DaikinAndroid.Models;

namespace DaikinAndroid.Services;

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

    #region Schedule APIs

    /// <summary>
    /// Get the weekly schedule from the device.
    /// </summary>
    Task<WeeklySchedule> GetScheduleAsync(CancellationToken ct = default);

    /// <summary>
    /// Get scheduler metadata (enabled state, active slot, names).
    /// </summary>
    Task<SchedulerInfo> GetSchedulerInfoAsync(CancellationToken ct = default);

    /// <summary>
    /// Enable or disable the scheduler globally.
    /// Schedule data is preserved when disabled.
    /// </summary>
    Task SetSchedulerEnabledAsync(bool enabled, CancellationToken ct = default);

    #endregion
}
