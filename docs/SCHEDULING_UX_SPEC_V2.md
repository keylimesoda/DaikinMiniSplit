# Scheduling Feature - UX Specification V2
**Document Version:** 2.0  
**Date:** January 28, 2026  
**Author:** Development Team  
**Status:** DRAFT - Updated with Actual API Capabilities

---

## 📋 Executive Summary

This document outlines the UX specification for adding a scheduling tab to the Daikin Manager V2 application. **CORRECTION from V1:** The BRP072A43 adapter **DOES have native scheduling APIs**. This feature will leverage the device's built-in weekly schedule timer, providing a more reliable and power-efficient solution than client-side scheduling.

### Key Correction from V1
| V1 Assumption | V2 Reality |
|---------------|------------|
| ❌ "No built-in scheduling capability" | ✅ Device has `/aircon/get_scdltimer` and `/aircon/set_scdltimer` APIs |
| ❌ "Client-side scheduling required" | ✅ Device executes schedules independently of app |
| ❌ "App must be running for schedules" | ✅ Schedules run on device firmware - app can be closed |

---

## 🔌 API Assessment - UPDATED

### Scheduling Endpoints (CONFIRMED WORKING)

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/aircon/get_scdltimer` | GET | Get current weekly schedule (all 7 days) |
| `/aircon/set_scdltimer` | GET | Set weekly schedule (must include all 7 days) |
| `/aircon/get_scdltimer_info` | GET | Get scheduler metadata (enabled state, capacity) |
| `/aircon/set_scdltimer_info` | GET | Enable/disable scheduler, set active schedule |

### Schedule Timer Info Response
```
ret=OK
format=v1
f_detail=total#18;_en#1;_pow#1;_mode#1;_temp#4;_time#4;_vol#1;_dir#1;_humi#3;_spmd#2
scdl_num=3          # Number of schedule slots (3 independent weekly schedules)
scdl_per_day=6      # Max events per day (6 events)
en_scdltimer=1      # Scheduler enabled (1) or disabled (0)
active_no=1         # Currently active schedule number (1-3)
en_oldtimer=0       # Legacy timer enabled
scdl1_name=         # Schedule 1 name (can be customized)
scdl2_name=         # Schedule 2 name
scdl3_name=         # Schedule 3 name
```

### Schedule Timer Data Format
```
# Pattern: {day_code}c = event count, {day_code}{n}_* = event n properties
# Day codes: mo, tu, we, th, fr, sa, su

moc=2               # Monday event count (2 events)
mo1_en=1            # Event 1 enabled
mo1_pow=1           # Event 1: Power ON
mo1_mod=3           # Event 1: Mode (3=Cool, 4=Heat, 2=Dry, 6=Fan)
mo1_tmp=22.0        # Event 1: Temperature (Celsius)
mo1_time=360        # Event 1: Time (minutes from midnight = 6:00 AM)

mo2_en=1            # Event 2 enabled
mo2_pow=0           # Event 2: Power OFF
mo2_mod=1           # Event 2: Mode (ignored when power=0)
mo2_tmp=--          # Event 2: Temperature (-- = not set)
mo2_time=1320       # Event 2: Time (minutes from midnight = 10:00 PM)
```

### Device Capabilities Summary
| Feature | Value | Notes |
|---------|-------|-------|
| Schedule slots | 3 | Can define 3 independent weekly schedules |
| Events per day | 6 | Up to 6 events per day |
| Days per week | 7 | Full week coverage |
| Event parameters | Power, Mode, Temp, Time | Full HVAC control per event |
| Time resolution | 1 minute | Minute-level precision |
| Execution | Device firmware | Works without app running |

### API Constraints
1. **Atomic Updates:** `set_scdltimer` requires ALL 7 days in one request
2. **Mode Values:** 1=Auto(?), 2=Dry, 3=Cool, 4=Heat, 6=Fan
3. **Temperature:** Celsius, `--` for power-off events
4. **Time:** Minutes from midnight (0-1439)
5. **Enable/Disable:** Individual events via `{day}{n}_en`, global via `en_scdltimer`

---

## 👥 User Personas (Unchanged from V1)

### Persona 1: "Daily Commuter Dan"
- Works 9-5 office job, predictable schedule
- Wants AC off when away, comfortable when arriving home
- Uses same schedule Mon-Fri, different on weekends

### Persona 2: "Work-From-Home Wendy"  
- Variable schedule, sometimes in meetings
- Wants temperature adjustments throughout the day
- Values "quick override" when schedule doesn't fit

### Persona 3: "Snowbird Steve"
- Away for extended periods (weeks/months)
- Wants "vacation mode" - minimal heating to prevent pipe freeze
- Needs remote monitoring (future feature)

### Persona 4: "Energy-Conscious Emma"
- Tracks utility costs closely
- Wants to optimize based on time-of-use electricity rates
- Interested in "eco schedules" that balance comfort vs. cost

---

## 📖 User Scenarios (Updated for Device-Based Scheduling)

### Scenario 1: Create a Simple Weekday Schedule
**User:** Dan  
**Goal:** Set AC to turn off when I leave for work, turn on before I get home

**Flow:**
1. Open app → Navigate to "Schedule" tab
2. Tap "Create Schedule" (uses one of 3 available slots)
3. Select days: Mon, Tue, Wed, Thu, Fri
4. Add event: 8:00 AM → Power OFF
5. Add event: 5:00 PM → Power ON, Cool, 72°F, Auto fan
6. Name schedule: "Work Week"
7. Save → Device receives schedule via `set_scdltimer`

**Success Criteria:**
- Schedule created in < 60 seconds
- Visual timeline shows events clearly
- Confirmation: "Schedule saved to device. Works even when app is closed."
- ✅ **NEW:** Schedule persists on device - no need to keep app running

---

### Scenario 2: Override Schedule Temporarily
**User:** Wendy  
**Goal:** I'm home sick today, I need the AC on now but don't want to delete my schedule

**Flow:**
1. Current schedule has AC off (work hours)
2. Open app → See "Scheduled: OFF until 5:00 PM"
3. Use main Controls tab to turn AC on manually
4. System tracks override state locally
5. Options:
   - Let schedule auto-resume at next event
   - Or manually tap "Resume Schedule"

**Implementation Note:** The device doesn't have a built-in override concept. We implement this client-side by:
- Sending manual control command
- Tracking that user overrode schedule
- Optionally re-enabling at next scheduled event or user action

**Success Criteria:**
- Override takes < 10 seconds
- Clear indication that schedule is overridden
- User understands schedule will resume automatically

---

### Scenario 3: Weekend Schedule
**User:** Dan  
**Goal:** Different schedule on Sat/Sun - sleep in, stay cool all day

**Flow:**
1. Open app → Schedule tab → Select existing schedule or create new
2. For Saturday and Sunday, set different events:
   - 10:00 AM → Cool, 70°F (sleep in, AC starts late)
   - 11:00 PM → OFF (bedtime)
3. Weekday events remain unchanged
4. Save

**Implementation:** All 7 days are part of one weekly schedule. Each day can have up to 6 events with different times/settings.

**Success Criteria:**
- Single schedule handles weekday vs weekend differences
- Visual distinction between day types
- Easy to copy settings between days

---

### Scenario 4: Use Multiple Schedule Slots
**User:** Emma  
**Goal:** Switch between "Summer" and "Winter" schedules easily

**Flow:**
1. Create Schedule 1: "Summer Cool" (cooling-focused, higher temps)
2. Create Schedule 2: "Winter Warm" (heating-focused, lower temps)
3. Schedule 3: Reserved for "Vacation"
4. Use "Active Schedule" selector to switch between them
5. Only one schedule active at a time (via `set_scdltimer_info?active_no=X`)

**Success Criteria:**
- Clear visual indicator of which schedule is active
- One-tap switching between schedules
- Schedules preserved when inactive

---

### Scenario 5: Quick Enable/Disable
**User:** Wendy  
**Goal:** Quickly disable all scheduling without losing settings

**Flow:**
1. Schedule tab shows "Scheduling: ON" toggle at top
2. Toggle OFF → Sends `set_scdltimer_info?en_scdltimer=0`
3. All schedule definitions preserved but not executing
4. Toggle ON → Schedules resume

**Success Criteria:**
- Single toggle for all scheduling
- Settings preserved during disable
- Clear feedback on current state

---

## 🎨 Proposed UI Layout (Updated)

### Schedule Tab Structure
```
┌─────────────────────────────────────────┐
│ SCHEDULES                    [ON ◯───●] │
│                              Enabled    │
├─────────────────────────────────────────┤
│ Select Active Schedule:                 │
│ ┌─────────────────────────────────────┐ │
│ │ ● Schedule 1: "Work Week"           │ │
│ │   5 events across Mon-Fri           │ │
│ └─────────────────────────────────────┘ │
│ ┌─────────────────────────────────────┐ │
│ │ ○ Schedule 2: "Summer Cool"         │ │
│ │   14 events across all days         │ │
│ └─────────────────────────────────────┘ │
│ ┌─────────────────────────────────────┐ │
│ │ ○ Schedule 3: (Not configured)      │ │
│ │   Tap to set up                     │ │
│ └─────────────────────────────────────┘ │
├─────────────────────────────────────────┤
│ TODAY'S SCHEDULE                        │
│ ┌─────────────────────────────────────┐ │
│ │ 6:00 AM   ❄️ Cool 72°F    ✓ Done    │ │
│ │ 8:00 AM   🔴 OFF          ✓ Done    │ │
│ │ 5:00 PM   ❄️ Cool 70°F    → Next    │ │
│ │ 10:00 PM  🔴 OFF                    │ │
│ └─────────────────────────────────────┘ │
├─────────────────────────────────────────┤
│ ℹ️ Schedules run on device - app can   │
│    be closed after configuration.       │
└─────────────────────────────────────────┘
```

### Schedule Editor (Weekly View)
```
┌─────────────────────────────────────────┐
│ ← Edit: Work Week                [Save] │
├─────────────────────────────────────────┤
│ ┌─────┬─────┬─────┬─────┬─────┬─────┬─────┐
│ │ Mon │ Tue │ Wed │ Thu │ Fri │ Sat │ Sun │
│ │ [2] │ [2] │ [2] │ [2] │ [2] │ [1] │ [1] │
│ └──●──┴──●──┴──●──┴──●──┴──●──┴──○──┴──○──┘
│                                         │
│ MONDAY EVENTS:                          │
│ ┌─────────────────────────────────────┐ │
│ │ 1. 6:00 AM  ❄️ Cool 72°F  [Edit][X]│ │
│ │ 2. 8:00 AM  🔴 OFF        [Edit][X]│ │
│ │ [+ Add Event] (4 more available)    │ │
│ └─────────────────────────────────────┘ │
│                                         │
│ [Copy Mon → Tue-Fri]                    │
└─────────────────────────────────────────┘
```

### Event Editor Dialog
```
┌─────────────────────────────────────────┐
│ Edit Event                              │
├─────────────────────────────────────────┤
│ Time:    [ 5 ] : [ 00 ] [PM ▼]         │
│                                         │
│ Action:  ● Turn ON    ○ Turn OFF       │
│                                         │
│ ─── When turning ON: ─────────────────  │
│                                         │
│ Mode:    [Cool      ▼]                  │
│          ❄️ Cool  🔥 Heat  💨 Fan  💧 Dry │
│                                         │
│ Temperature: [72°F    ]  [+][-]         │
│              ├────●────┤                │
│              60°F      90°F             │
│                                         │
│         [Cancel]            [Save]      │
└─────────────────────────────────────────┘
```

---

## ⚠️ Edge Cases & Error Handling (Updated)

| Situation | Handling |
|-----------|----------|
| ✅ App closed | Schedule continues on device - no problem |
| ✅ PC asleep | Schedule runs on device - no problem |
| ✅ PC off | Schedule runs on device - no problem |
| Network unavailable when saving | Retry, show error if device unreachable |
| Device offline | Cannot modify schedule, show cached last-known state |
| All 3 schedule slots in use | Must delete one to create new |
| 6 events per day limit reached | Show "Maximum events reached" message |
| Conflicting events same day/time | Device uses last-written; app should prevent |
| Daylight saving time | Device handles via `/common/get_datetime` timezone |

---

## 🚀 MVP vs. Future Features (Revised)

### MVP (Phase 1) - Device-Native Scheduling
- [x] Read existing schedules from device
- [ ] Create/edit/delete schedules (3 slots)
- [ ] Add up to 6 events per day
- [ ] Day-of-week selection with copy feature
- [ ] Enable/disable scheduling globally
- [ ] Switch between schedule slots
- [ ] Today's schedule view with next event

### Phase 2 - Enhanced UX
- [ ] Override tracking (client-side state)
- [ ] Schedule naming (`set_scdltimer_info` supports names)
- [ ] "Quick Actions" for common presets
- [ ] Import/export schedules (JSON backup)
- [ ] Schedule templates ("Work Week", "Weekend", "Vacation")

### Phase 3 - Advanced Features
- [ ] Multi-device scheduling (if user has multiple units)
- [ ] Geofencing integration (Windows location services)
- [ ] Smart suggestions based on usage patterns
- [ ] Energy cost estimation based on schedule

---

## 📋 API Implementation Checklist

### Required API Calls
```
GET  /aircon/get_scdltimer        # Read current schedule
GET  /aircon/set_scdltimer?...    # Write schedule (atomic, all 7 days)
GET  /aircon/get_scdltimer_info   # Read scheduler state
GET  /aircon/set_scdltimer_info?en_scdltimer=X&active_no=Y  # Control scheduler
```

### Data Model for App
```csharp
public class ScheduleEvent
{
    public bool Enabled { get; set; }
    public bool PowerOn { get; set; }
    public DaikinMode Mode { get; set; }
    public double? TemperatureCelsius { get; set; }  // null if power off
    public TimeSpan Time { get; set; }  // TimeOfDay
}

public class DaySchedule
{
    public DayOfWeek Day { get; set; }
    public List<ScheduleEvent> Events { get; set; }  // Max 6
}

public class WeeklySchedule
{
    public int SlotNumber { get; set; }  // 1-3
    public string Name { get; set; }
    public List<DaySchedule> Days { get; set; }  // 7 days
}

public class SchedulerState
{
    public bool Enabled { get; set; }
    public int ActiveScheduleNumber { get; set; }
    public int MaxSchedules { get; set; }  // 3
    public int MaxEventsPerDay { get; set; }  // 6
}
```

---

## 📝 Open Questions (Revised)

1. **Schedule Slot Naming:** The device supports `scdl1_name`, etc. Should we expose this or use app-side naming only?

2. **Event Count UX:** How to best communicate "6 events per day" limit? Progress indicator?

3. **Copy Features:** "Copy Mon → Tue-Fri" is useful. What about "Copy Schedule 1 → Schedule 2"?

4. **Override Tracking:** Since device doesn't track overrides, should the app poll to detect manual changes?

5. **Temperature Units:** Device uses Celsius. App shows user's preference (F/C). Confirm conversion is correct.

---

## 📅 Revised Timeline

| Phase | Scope | Duration |
|-------|-------|----------|
| Phase 1a | API integration + basic read | 1 week |
| Phase 1b | Create/edit schedules | 1 week |
| Phase 1c | Today's view + enable/disable | 1 week |
| Phase 2 | Enhanced UX features | 2 weeks |
| Phase 3 | Advanced features | TBD |

---

## ✅ Key Advantages of Device-Native Scheduling

1. **Reliability:** Schedules execute on device firmware - no dependency on app/PC
2. **Power Efficiency:** PC can sleep/shutdown after configuring schedule
3. **Instant Execution:** No network latency at execution time
4. **Survives Reboots:** Schedule persists through device/PC restarts
5. **Simpler Implementation:** No background service needed

---

*This document supersedes SCHEDULING_UX_SPEC.md (V1). The V1 assumption of "no scheduling APIs" was incorrect.*
