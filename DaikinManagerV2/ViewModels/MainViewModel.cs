using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using DaikinManagerV2.Models;
using DaikinManagerV2.Services;

namespace DaikinManagerV2.ViewModels;

/// <summary>
/// Main application ViewModel (Singleton).
/// Owns device state, connection state, and refresh timer.
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IDaikinApiService _apiService;
    private readonly ISettingsService _settings;
    private readonly DispatcherQueue _dispatcherQueue;
    
    private readonly DispatcherQueueTimer _refreshTimer;
    private readonly DispatcherQueueTimer _countdownTimer;
    private CancellationTokenSource? _refreshCts;
    private bool _disposed;

    public MainViewModel(IDaikinApiService apiService, ISettingsService settings)
    {
        _apiService = apiService;
        _settings = settings;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        
        // Main refresh timer
        _refreshTimer = _dispatcherQueue.CreateTimer();
        _refreshTimer.Interval = TimeSpan.FromSeconds(_settings.RefreshIntervalSeconds);
        _refreshTimer.Tick += (s, e) => _ = RefreshAsync();
        
        // 1-second countdown timer for UI indicator
        _countdownTimer = _dispatcherQueue.CreateTimer();
        _countdownTimer.Interval = TimeSpan.FromSeconds(1);
        _countdownTimer.Tick += OnCountdownTick;
        
        // Initialize state
        DaikinState = DaikinState.Default;
        SecondsUntilRefresh = _settings.RefreshIntervalSeconds;
    }

    #region Observable Properties

    [ObservableProperty]
    private ConnectionState _connectionState = ConnectionState.Disconnected;

    [ObservableProperty]
    private DaikinState _daikinState;

    /// <summary>
    /// True once we've successfully fetched data at least once.
    /// Used to determine if controls should be shown.
    /// </summary>
    [ObservableProperty]
    private bool _isDataLoaded;

    [ObservableProperty]
    private int _secondsUntilRefresh;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _refreshFailed;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>
    /// Refresh interval from settings.
    /// </summary>
    public int RefreshIntervalSeconds => _settings.RefreshIntervalSeconds;

    /// <summary>
    /// Progress value for countdown indicator (0.0 to 1.0).
    /// </summary>
    public double CountdownProgress => 
        (double)SecondsUntilRefresh / _settings.RefreshIntervalSeconds;

    #endregion

    #region Commands

    [RelayCommand]
    public async Task ConnectAsync()
    {
        await RefreshAsync();
        if (ConnectionState == ConnectionState.Connected)
        {
            StartAutoRefresh();
        }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        // Cancel any in-flight request
        _refreshCts?.Cancel();
        _refreshCts = new CancellationTokenSource();
        
        try
        {
            ConnectionState = ConnectionState.Connecting;
            IsRefreshing = true;
            RefreshFailed = false;
            
            var state = await _apiService.GetStateAsync(_refreshCts.Token);
            
            DaikinState = state;
            ConnectionState = ConnectionState.Connected;
            IsDataLoaded = true;  // We now have valid data to display
            ErrorMessage = string.Empty;
            
            // Reset countdown
            SecondsUntilRefresh = _settings.RefreshIntervalSeconds;
            OnPropertyChanged(nameof(CountdownProgress));
        }
        catch (OperationCanceledException)
        {
            // Intentional cancellation, ignore
        }
        catch (Exception ex)
        {
            ConnectionState = ConnectionState.Error;
            ErrorMessage = ex.Message;
            RefreshFailed = true;
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    public void Disconnect()
    {
        StopAutoRefresh();
        _refreshCts?.Cancel();
        ConnectionState = ConnectionState.Disconnected;
        IsDataLoaded = false;  // Reset data loaded flag
        DaikinState = DaikinState.Default;
    }

    #endregion

    #region Timer Management

    public void StartAutoRefresh()
    {
        SecondsUntilRefresh = _settings.RefreshIntervalSeconds;
        _refreshTimer.Start();
        _countdownTimer.Start();
    }

    public void StopAutoRefresh()
    {
        _refreshTimer.Stop();
        _countdownTimer.Stop();
    }

    private void OnCountdownTick(DispatcherQueueTimer sender, object args)
    {
        if (IsRefreshing) return;
        
        SecondsUntilRefresh = Math.Max(0, SecondsUntilRefresh - 1);
        OnPropertyChanged(nameof(CountdownProgress));
    }

    public void ResetAutoRefresh()
    {
        SecondsUntilRefresh = _settings.RefreshIntervalSeconds;
        OnPropertyChanged(nameof(CountdownProgress));
        
        // Restart timer
        _refreshTimer.Stop();
        _refreshTimer.Interval = TimeSpan.FromSeconds(_settings.RefreshIntervalSeconds);
        _refreshTimer.Start();
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (!_disposed)
        {
            _refreshCts?.Cancel();
            _refreshCts?.Dispose();
            _refreshTimer.Stop();
            _countdownTimer.Stop();
            _disposed = true;
        }
    }

    #endregion
}
