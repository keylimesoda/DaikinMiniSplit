namespace DaikinAndroid.Models;

/// <summary>
/// A single scheduled event (e.g., "Turn on at 5:00 PM").
/// Immutable record for thread safety.
/// </summary>
public sealed record ScheduleEvent(
    bool Enabled,
    bool PowerOn,
    DaikinMode Mode,
    double? TemperatureCelsius,
    TimeOnly Time)
{
    /// <summary>
    /// Creates a disabled/empty event.
    /// </summary>
    public static ScheduleEvent Empty(TimeOnly time) => new(
        Enabled: false,
        PowerOn: false,
        Mode: DaikinMode.Auto,
        TemperatureCelsius: null,
        Time: time);

    /// <summary>
    /// Creates a "power off" event.
    /// </summary>
    public static ScheduleEvent PowerOff(TimeOnly time) => new(
        Enabled: true,
        PowerOn: false,
        Mode: DaikinMode.Auto,
        TemperatureCelsius: null,
        Time: time);

    /// <summary>
    /// Creates a "power on" event with settings.
    /// </summary>
    public static ScheduleEvent PowerOnAt(TimeOnly time, DaikinMode mode, double temperatureCelsius) => new(
        Enabled: true,
        PowerOn: true,
        Mode: mode,
        TemperatureCelsius: temperatureCelsius,
        Time: time);

    /// <summary>
    /// Human-readable description for UI.
    /// </summary>
    public string Description => (Enabled, PowerOn) switch
    {
        (false, _) => "Not set",
        (true, false) => "Turn OFF",
        (true, true) => $"{Mode} {TemperatureCelsius:F0}°C"
    };
}

/// <summary>
/// Schedule for a single day (up to 6 events).
/// </summary>
public sealed record DaySchedule(
    DayOfWeek Day,
    IReadOnlyList<ScheduleEvent> Events)
{
    /// <summary>
    /// Number of enabled events for this day.
    /// </summary>
    public int EnabledEventCount => Events.Count(e => e.Enabled);

    /// <summary>
    /// Creates an empty day schedule.
    /// </summary>
    public static DaySchedule Empty(DayOfWeek day) => new(day, Array.Empty<ScheduleEvent>());
}

/// <summary>
/// Complete weekly schedule (all 7 days).
/// </summary>
public sealed record WeeklySchedule(
    IReadOnlyList<DaySchedule> Days)
{
    /// <summary>
    /// Get schedule for a specific day.
    /// </summary>
    public DaySchedule GetDay(DayOfWeek day) => 
        Days.FirstOrDefault(d => d.Day == day) ?? DaySchedule.Empty(day);

    /// <summary>
    /// Get today's schedule.
    /// </summary>
    public DaySchedule Today => GetDay(DateTime.Now.DayOfWeek);

    /// <summary>
    /// Total enabled events across all days.
    /// </summary>
    public int TotalEnabledEvents => Days.Sum(d => d.EnabledEventCount);

    /// <summary>
    /// Returns true if any events are scheduled.
    /// </summary>
    public bool HasAnyEvents => TotalEnabledEvents > 0;

    /// <summary>
    /// Get the next upcoming event (today or later this week).
    /// Returns null if no events scheduled.
    /// </summary>
    public (DayOfWeek Day, ScheduleEvent Event, TimeSpan TimeUntil)? GetNextEvent()
    {
        var now = DateTime.Now;
        var currentDay = now.DayOfWeek;
        var currentTime = TimeOnly.FromDateTime(now);

        // Check today first (events after current time)
        var todaySchedule = GetDay(currentDay);
        var nextToday = todaySchedule.Events
            .Where(e => e.Enabled && e.Time > currentTime)
            .OrderBy(e => e.Time)
            .FirstOrDefault();

        if (nextToday != null)
        {
            var eventDateTime = now.Date.Add(nextToday.Time.ToTimeSpan());
            return (currentDay, nextToday, eventDateTime - now);
        }

        // Check subsequent days (wrap around week)
        for (int i = 1; i <= 7; i++)
        {
            var checkDay = (DayOfWeek)(((int)currentDay + i) % 7);
            var daySchedule = GetDay(checkDay);
            var nextEvent = daySchedule.Events
                .Where(e => e.Enabled)
                .OrderBy(e => e.Time)
                .FirstOrDefault();

            if (nextEvent != null)
            {
                var daysUntil = i;
                var eventDateTime = now.Date.AddDays(daysUntil).Add(nextEvent.Time.ToTimeSpan());
                return (checkDay, nextEvent, eventDateTime - now);
            }
        }

        return null;
    }

    /// <summary>
    /// Creates an empty weekly schedule.
    /// </summary>
    public static WeeklySchedule Empty() => new(
        Enum.GetValues<DayOfWeek>().Select(DaySchedule.Empty).ToList());
}

/// <summary>
/// Scheduler metadata from the device.
/// </summary>
public sealed record SchedulerInfo(
    bool Enabled,
    int ActiveSlot,
    int MaxSchedules,
    int MaxEventsPerDay,
    string Schedule1Name,
    string Schedule2Name,
    string Schedule3Name)
{
    /// <summary>
    /// Default info when device doesn't respond.
    /// </summary>
    public static SchedulerInfo Default() => new(
        Enabled: false,
        ActiveSlot: 1,
        MaxSchedules: 3,
        MaxEventsPerDay: 6,
        Schedule1Name: "",
        Schedule2Name: "",
        Schedule3Name: "");
}
