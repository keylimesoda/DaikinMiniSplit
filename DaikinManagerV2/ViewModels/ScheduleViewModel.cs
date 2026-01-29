using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DaikinManagerV2.Models;
using DaikinManagerV2.Services;
using System.Collections.ObjectModel;

namespace DaikinManagerV2.ViewModels;

/// <summary>
/// Schedule page ViewModel.
/// Displays today's schedule and next upcoming event.
/// </summary>
public sealed partial class ScheduleViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly IDaikinApiService _apiService;
    private System.Timers.Timer? _countdownTimer;

    public ScheduleViewModel(MainViewModel main, IDaikinApiService apiService)
    {
        _main = main;
        _apiService = apiService;
        
        // Subscribe to connection state changes
        _main.PropertyChanged += OnMainPropertyChanged;
        
        // Start countdown timer (updates every second)
        _countdownTimer = new System.Timers.Timer(1000);
        _countdownTimer.Elapsed += (s, e) => UpdateCountdown();
        _countdownTimer.Start();
    }

    #region Observable Properties

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isSchedulerEnabled;

    [ObservableProperty]
    private WeeklySchedule? _schedule;

    [ObservableProperty]
    private SchedulerInfo? _schedulerInfo;

    /// <summary>
    /// Today's events for display.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ScheduleEventViewModel> _todayEvents = new();

    /// <summary>
    /// The next upcoming event (if any).
    /// </summary>
    [ObservableProperty]
    private ScheduleEventViewModel? _nextEvent;

    /// <summary>
    /// Countdown string like "in 2h 34m" or "in 45 min".
    /// </summary>
    [ObservableProperty]
    private string _countdownText = string.Empty;

    /// <summary>
    /// Today's day name (e.g., "Tuesday").
    /// </summary>
    public string TodayName => DateTime.Now.ToString("dddd");

    /// <summary>
    /// Whether there are any events scheduled today.
    /// </summary>
    public bool HasTodayEvents => TodayEvents.Count > 0;

    /// <summary>
    /// Whether there's a next event to show.
    /// </summary>
    public bool HasNextEvent => NextEvent != null;

    /// <summary>
    /// Whether the schedule is empty (no events any day).
    /// </summary>
    public bool IsScheduleEmpty => Schedule?.TotalEnabledEvents == 0;

    /// <summary>
    /// Page is ready to show content (not loading, no error).
    /// </summary>
    public bool IsReady => !IsLoading && !HasError;

    #endregion

    #region Week View Properties

    /// <summary>
    /// Whether showing the week view (vs home/today view).
    /// </summary>
    [ObservableProperty]
    private bool _isWeekViewVisible;

    /// <summary>
    /// Currently selected day in week view.
    /// </summary>
    [ObservableProperty]
    private DayOfWeek _selectedDay = DateTime.Now.DayOfWeek;

    /// <summary>
    /// Events for the selected day.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ScheduleEventViewModel> _selectedDayEvents = new();

    /// <summary>
    /// Full name of selected day (e.g., "TUESDAY").
    /// </summary>
    public string SelectedDayName => SelectedDay.ToString().ToUpper();

    /// <summary>
    /// Whether there are events on the selected day.
    /// </summary>
    public bool HasSelectedDayEvents => SelectedDayEvents.Count > 0;

    /// <summary>
    /// Day info for each day of the week (for day buttons).
    /// </summary>
    public IReadOnlyList<DayButtonInfo> WeekDays { get; } = new[]
    {
        new DayButtonInfo(DayOfWeek.Monday, "Mon"),
        new DayButtonInfo(DayOfWeek.Tuesday, "Tue"),
        new DayButtonInfo(DayOfWeek.Wednesday, "Wed"),
        new DayButtonInfo(DayOfWeek.Thursday, "Thu"),
        new DayButtonInfo(DayOfWeek.Friday, "Fri"),
        new DayButtonInfo(DayOfWeek.Saturday, "Sat"),
        new DayButtonInfo(DayOfWeek.Sunday, "Sun")
    };

    #endregion

    #region Commands

    [RelayCommand]
    private async Task LoadScheduleAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            // Fetch schedule and info in parallel
            var scheduleTask = _apiService.GetScheduleAsync();
            var infoTask = _apiService.GetSchedulerInfoAsync();

            await Task.WhenAll(scheduleTask, infoTask);

            Schedule = await scheduleTask;
            SchedulerInfo = await infoTask;
            IsSchedulerEnabled = SchedulerInfo.Enabled;

            // Update today's events
            UpdateTodayEvents();

            // Update next event
            UpdateNextEvent();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Failed to load schedule: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsReady));
            OnPropertyChanged(nameof(HasTodayEvents));
            OnPropertyChanged(nameof(HasNextEvent));
            OnPropertyChanged(nameof(IsScheduleEmpty));
        }
    }

    [RelayCommand]
    private async Task ToggleSchedulerAsync()
    {
        if (SchedulerInfo == null) return;

        try
        {
            var newState = !IsSchedulerEnabled;
            await _apiService.SetSchedulerEnabledAsync(newState);
            IsSchedulerEnabled = newState;
        }
        catch (Exception ex)
        {
            // Revert on failure
            ErrorMessage = $"Failed to toggle scheduler: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ShowWeekView()
    {
        SelectedDay = DateTime.Now.DayOfWeek;
        UpdateSelectedDayEvents();
        IsWeekViewVisible = true;
    }

    [RelayCommand]
    private void HideWeekView()
    {
        IsWeekViewVisible = false;
    }

    [RelayCommand]
    private void SelectDay(DayOfWeek day)
    {
        SelectedDay = day;
        UpdateSelectedDayEvents();
        OnPropertyChanged(nameof(SelectedDayName));
    }

    #endregion

    #region Private Methods

    private void OnMainPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.ConnectionState))
        {
            // Reload schedule when connection state changes to Connected
            if (_main.ConnectionState == ConnectionState.Connected && Schedule == null)
            {
                _ = LoadScheduleAsync();
            }
        }
    }

    private void UpdateTodayEvents()
    {
        TodayEvents.Clear();

        if (Schedule == null) return;

        var today = Schedule.Today;
        var now = TimeOnly.FromDateTime(DateTime.Now);

        foreach (var evt in today.Events.Where(e => e.Enabled).OrderBy(e => e.Time))
        {
            var isPast = evt.Time < now;
            var vm = new ScheduleEventViewModel(evt, today.Day, isPast);
            TodayEvents.Add(vm);
        }

        OnPropertyChanged(nameof(HasTodayEvents));

        // Update week day event counts
        UpdateWeekDayEventCounts();
    }

    private void UpdateSelectedDayEvents()
    {
        SelectedDayEvents.Clear();

        if (Schedule == null) return;

        var daySchedule = Schedule.GetDay(SelectedDay);
        var now = TimeOnly.FromDateTime(DateTime.Now);
        var isToday = SelectedDay == DateTime.Now.DayOfWeek;

        foreach (var evt in daySchedule.Events.Where(e => e.Enabled).OrderBy(e => e.Time))
        {
            var isPast = isToday && evt.Time < now;
            var vm = new ScheduleEventViewModel(evt, SelectedDay, isPast);
            SelectedDayEvents.Add(vm);
        }

        OnPropertyChanged(nameof(HasSelectedDayEvents));
    }

    private void UpdateWeekDayEventCounts()
    {
        if (Schedule == null) return;

        foreach (var dayInfo in WeekDays)
        {
            var daySchedule = Schedule.GetDay(dayInfo.Day);
            dayInfo.EventCount = daySchedule.EnabledEventCount;
            dayInfo.IsToday = dayInfo.Day == DateTime.Now.DayOfWeek;
        }
    }

    private void UpdateNextEvent()
    {
        if (Schedule == null)
        {
            NextEvent = null;
            CountdownText = string.Empty;
            return;
        }

        var next = Schedule.GetNextEvent();
        if (next.HasValue)
        {
            var (day, evt, timeUntil) = next.Value;
            NextEvent = new ScheduleEventViewModel(evt, day, false);
            UpdateCountdownText(timeUntil);
        }
        else
        {
            NextEvent = null;
            CountdownText = string.Empty;
        }

        OnPropertyChanged(nameof(HasNextEvent));
    }

    private void UpdateCountdown()
    {
        if (Schedule == null || NextEvent == null) return;

        var next = Schedule.GetNextEvent();
        if (next.HasValue)
        {
            UpdateCountdownText(next.Value.TimeUntil);
            
            // Check if we crossed into a new event (refresh needed)
            if (next.Value.TimeUntil.TotalSeconds < 1)
            {
                _ = LoadScheduleAsync();
            }
        }
    }

    private void UpdateCountdownText(TimeSpan timeUntil)
    {
        if (timeUntil.TotalHours >= 24)
        {
            var days = (int)timeUntil.TotalDays;
            CountdownText = days == 1 ? "in 1 day" : $"in {days} days";
        }
        else if (timeUntil.TotalHours >= 1)
        {
            var hours = (int)timeUntil.TotalHours;
            var mins = timeUntil.Minutes;
            CountdownText = mins > 0 ? $"in {hours}h {mins}m" : $"in {hours}h";
        }
        else if (timeUntil.TotalMinutes >= 1)
        {
            CountdownText = $"in {(int)timeUntil.TotalMinutes} min";
        }
        else
        {
            CountdownText = "now";
        }
    }

    #endregion

    public void Cleanup()
    {
        _countdownTimer?.Stop();
        _countdownTimer?.Dispose();
        _main.PropertyChanged -= OnMainPropertyChanged;
    }
}

/// <summary>
/// View model for a single schedule event (for display in lists).
/// </summary>
public sealed class ScheduleEventViewModel
{
    public ScheduleEventViewModel(ScheduleEvent evt, DayOfWeek day, bool isPast)
    {
        Event = evt;
        Day = day;
        IsPast = isPast;
    }

    public ScheduleEvent Event { get; }
    public DayOfWeek Day { get; }
    public bool IsPast { get; }

    /// <summary>
    /// Time formatted as "5:00 PM".
    /// </summary>
    public string TimeText => Event.Time.ToString("h:mm tt");

    /// <summary>
    /// Icon glyph based on power/mode.
    /// </summary>
    public string IconGlyph => Event.PowerOn ? Event.Mode switch
    {
        DaikinMode.Cool => "\uE9CA",  // Thermometer (will show blue)
        DaikinMode.Heat => "\uE706",  // Brightness/sun
        DaikinMode.Dry => "\uE9C9",   // Drop
        DaikinMode.Fan => "\uE71E",   // Refresh/fan
        _ => "\uE9CA"                  // Thermometer
    } : "\uE7E8";  // Power icon for OFF

    /// <summary>
    /// Description like "Cool 72°F" or "Turn OFF".
    /// </summary>
    public string Description
    {
        get
        {
            if (!Event.PowerOn)
                return "Turn OFF";

            var tempF = Event.TemperatureCelsius.HasValue
                ? (int)(Event.TemperatureCelsius.Value * 9.0 / 5.0 + 32.0)
                : 0;

            return Event.Mode switch
            {
                DaikinMode.Cool => $"Cool {tempF}°F",
                DaikinMode.Heat => $"Heat {tempF}°F",
                DaikinMode.Dry => "Dry",
                DaikinMode.Fan => "Fan",
                DaikinMode.Auto => $"Auto {tempF}°F",
                _ => $"{tempF}°F"
            };
        }
    }

    /// <summary>
    /// Status indicator for past/next/upcoming.
    /// </summary>
    public string StatusText => IsPast ? "✓" : "";
}

/// <summary>
/// Info for a day button in the week view.
/// </summary>
public sealed class DayButtonInfo : ObservableObject
{
    public DayButtonInfo(DayOfWeek day, string shortName)
    {
        Day = day;
        ShortName = shortName;
    }

    public DayOfWeek Day { get; }
    public string ShortName { get; }

    private int _eventCount;
    public int EventCount
    {
        get => _eventCount;
        set
        {
            if (SetProperty(ref _eventCount, value))
            {
                OnPropertyChanged(nameof(HasEvents));
            }
        }
    }

    private bool _isToday;
    public bool IsToday
    {
        get => _isToday;
        set => SetProperty(ref _isToday, value);
    }

    /// <summary>
    /// Visibility for the event indicator dot.
    /// Returns Visible when events exist, Collapsed otherwise.
    /// </summary>
    public Microsoft.UI.Xaml.Visibility HasEvents => 
        EventCount > 0 ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
}
