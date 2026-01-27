using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DaikinManagerV2.Models;
using DaikinManagerV2.Services;

namespace DaikinManagerV2.ViewModels;

/// <summary>
/// Controls page ViewModel (Transient).
/// Maintains staged settings that can be applied to device.
/// </summary>
public sealed partial class ControlsViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly IDaikinApiService _apiService;
    private readonly ISettingsService _settings;

    public ControlsViewModel(MainViewModel main, IDaikinApiService apiService, ISettingsService settings)
    {
        _main = main;
        _apiService = apiService;
        _settings = settings;
        
        // Initialize staged values from current device state
        SyncFromDeviceState(_main.DaikinState);
        
        // Subscribe to future updates
        _main.PropertyChanged += OnMainPropertyChanged;
    }

    #region Device State (from MainViewModel)

    /// <summary>
    /// Current device state (read-only, from MainViewModel).
    /// </summary>
    public DaikinState DeviceState => _main.DaikinState;
    
    /// <summary>
    /// Connection state for UI binding.
    /// </summary>
    public ConnectionState ConnectionState => _main.ConnectionState;
    
    /// <summary>
    /// Whether currently refreshing.
    /// </summary>
    public bool IsRefreshing => _main.IsRefreshing;
    
    /// <summary>
    /// Whether data has been successfully loaded at least once.
    /// </summary>
    public bool IsDataLoaded => _main.IsDataLoaded;
    
    /// <summary>
    /// Error message from connection/refresh failures.
    /// </summary>
    public string ErrorMessage => _main.ErrorMessage;
    
    /// <summary>
    /// Current page state based on connection and data availability.
    /// Controls show Loading/Error/Ready panels.
    /// </summary>
    public PageState PageState
    {
        get
        {
            // Once we have data, stay in Ready state (even during subsequent refreshes)
            // This prevents the UI from flashing back to Loading on every 30-second refresh
            if (IsDataLoaded)
                return PageState.Ready;
            
            // If there's an error before we ever got data, show error
            if (ConnectionState == ConnectionState.Error)
                return PageState.Error;
            
            // Otherwise we're loading (initial connection)
            return PageState.Loading;
        }
    }

    #endregion

    #region Staged Settings (user's pending changes)

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPendingChanges))]
    private bool _stagedPower;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPendingChanges))]
    [NotifyPropertyChangedFor(nameof(SupportsTemperature))]
    private DaikinMode _stagedMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPendingChanges))]
    [NotifyPropertyChangedFor(nameof(StagedTemperatureDisplay))]
    private double _stagedTemperatureC;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPendingChanges))]
    private FanSpeed _stagedFanSpeed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPendingChanges))]
    private SwingMode _stagedSwingMode;

    /// <summary>
    /// Temperature in display units (F or C based on settings).
    /// </summary>
    public double StagedTemperatureDisplay
    {
        get => _settings.TemperatureUnit == TemperatureUnit.Fahrenheit
            ? TemperatureConverter.CelsiusToFahrenheit(StagedTemperatureC)
            : StagedTemperatureC;
        set
        {
            var celsius = _settings.TemperatureUnit == TemperatureUnit.Fahrenheit
                ? TemperatureConverter.FahrenheitToCelsius(value)
                : value;
            StagedTemperatureC = TemperatureConverter.ClampCelsius(celsius);
        }
    }

    /// <summary>
    /// Whether the current mode supports temperature control.
    /// </summary>
    public bool SupportsTemperature => ModeValidation.SupportsTemperature(StagedMode);

    /// <summary>
    /// True if staged values differ from device state.
    /// </summary>
    public bool HasPendingChanges =>
        StagedPower != DeviceState.IsPoweredOn ||
        StagedMode != DeviceState.Mode ||
        Math.Abs(StagedTemperatureC - DeviceState.SetTemperatureC) > 0.1 ||
        StagedFanSpeed != DeviceState.FanSpeed ||
        StagedSwingMode != DeviceState.SwingMode;

    #endregion

    #region Display Properties

    /// <summary>
    /// Indoor temperature formatted for display.
    /// </summary>
    public string IndoorTemperatureDisplay =>
        TemperatureConverter.Format(DeviceState.IndoorTempC, _settings.TemperatureUnit);

    /// <summary>
    /// Temperature unit for display.
    /// </summary>
    public TemperatureUnit TemperatureUnit => _settings.TemperatureUnit;

    /// <summary>
    /// Min temperature in display units.
    /// </summary>
    public double MinTemperature => _settings.TemperatureUnit == TemperatureUnit.Fahrenheit ? 64 : 18;

    /// <summary>
    /// Max temperature in display units.
    /// </summary>
    public double MaxTemperature => _settings.TemperatureUnit == TemperatureUnit.Fahrenheit ? 86 : 30;

    #endregion

    #region Commands

    [ObservableProperty]
    private bool _isApplying;

    [ObservableProperty]
    private bool _isTogglingPower;

    /// <summary>
    /// Toggle power immediately (does not wait for Apply Changes).
    /// Sends the new power state along with current device settings.
    /// </summary>
    public async Task SetPowerAsync(bool powerOn)
    {
        if (_isTogglingPower) return;
        
        try
        {
            IsTogglingPower = true;
            StagedPower = powerOn; // Update UI immediately
            
            // Send power change with current device settings (not staged)
            await _apiService.ApplySettingsAsync(
                powerOn,
                DeviceState.Mode,
                DeviceState.SetTemperatureC,
                DeviceState.FanSpeed,
                DeviceState.SwingMode
            );
            
            // Refresh to confirm changes and sync staged state
            await _main.RefreshAsync();
            _main.ResetAutoRefresh();
        }
        catch
        {
            // Revert UI on failure
            StagedPower = DeviceState.IsPoweredOn;
            throw;
        }
        finally
        {
            IsTogglingPower = false;
        }
    }

    [RelayCommand]
    public async Task ApplySettingsAsync()
    {
        if (!HasPendingChanges) return;
        
        try
        {
            IsApplying = true;
            
            await _apiService.ApplySettingsAsync(
                StagedPower,
                StagedMode,
                StagedTemperatureC,
                StagedFanSpeed,
                StagedSwingMode
            );
            
            // Refresh to confirm changes
            await _main.RefreshAsync();
            
            // Sync staged values from the refreshed device state to clear HasPendingChanges
            SyncFromDeviceState(DeviceState);
            
            _main.ResetAutoRefresh();
        }
        finally
        {
            IsApplying = false;
        }
    }

    [RelayCommand]
    public void ResetChanges()
    {
        SyncFromDeviceState(DeviceState);
    }
    
    /// <summary>
    /// Retry connection after a failure (delegates to MainViewModel).
    /// </summary>
    public async Task RetryConnectionAsync()
    {
        await _main.ConnectAsync();
    }

    #endregion

    #region Private Helpers

    private void SyncFromDeviceState(DaikinState state)
    {
        StagedPower = state.IsPoweredOn;
        StagedMode = state.Mode;
        StagedTemperatureC = state.SetTemperatureC;
        StagedFanSpeed = state.FanSpeed;
        StagedSwingMode = state.SwingMode;
        
        OnPropertyChanged(nameof(StagedTemperatureDisplay));
        OnPropertyChanged(nameof(IndoorTemperatureDisplay));
        OnPropertyChanged(nameof(HasPendingChanges));
    }

    private void OnMainPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.DaikinState):
                // Only sync if user hasn't made changes
                if (!HasPendingChanges)
                {
                    SyncFromDeviceState(_main.DaikinState);
                }
                OnPropertyChanged(nameof(DeviceState));
                OnPropertyChanged(nameof(IndoorTemperatureDisplay));
                break;
                
            case nameof(MainViewModel.ConnectionState):
                OnPropertyChanged(nameof(ConnectionState));
                OnPropertyChanged(nameof(PageState));
                break;
                
            case nameof(MainViewModel.IsRefreshing):
                OnPropertyChanged(nameof(IsRefreshing));
                break;
                
            case nameof(MainViewModel.IsDataLoaded):
                // CRITICAL: Force sync on first data load BEFORE notifying PageState
                // When IsDataLoaded becomes true, we MUST sync from actual device state
                // regardless of HasPendingChanges (which compares to stale defaults)
                if (_main.IsDataLoaded)
                {
                    SyncFromDeviceState(_main.DaikinState);
                }
                
                OnPropertyChanged(nameof(IsDataLoaded));
                OnPropertyChanged(nameof(PageState));
                break;
                
            case nameof(MainViewModel.ErrorMessage):
                OnPropertyChanged(nameof(ErrorMessage));
                break;
        }
    }

    #endregion
}
