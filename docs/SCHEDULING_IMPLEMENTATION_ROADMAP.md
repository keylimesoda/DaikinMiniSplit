# Scheduling Feature - Implementation Roadmap

**Date:** January 28, 2026  
**Purpose:** Decompose scheduling into incremental features with clear dependencies

---

## 📊 Feature Decomposition

### Guiding Principles
1. **Each feature ships independently** - delivers value on its own
2. **Simplest first** - get basic functionality working before polish
3. **API before UI** - service layer enables multiple UI iterations
4. **Test with real device** - validate each feature against hardware

---

## Feature Dependency Graph

```
F1: Read Schedule (API)
 │
 ├──► F2: View Today's Schedule (UI)
 │     │
 │     └──► F3: View Full Week (UI)
 │
 └──► F4: Enable/Disable Scheduling (API+UI)
      │
      └──► F5: Write Schedule (API)
           │
           ├──► F6: Edit Existing Event (UI)
           │     │
           │     └──► F7: Add/Delete Events (UI)
           │           │
           │           └──► F8: Copy Day to Other Days (UI)
           │
           └──► F9: Setup Wizard (UI)
                │
                └──► F10: Override from Controls Tab (UI)
```

---

## 🚀 Feature Breakdown

### FEATURE 1: Read Schedule from Device (API Layer) ✅ COMPLETE
**Priority:** P0 - Foundation  
**Effort:** 4 hours  
**Dependencies:** None  
**Status:** ✅ Implemented 2026-01-28

**Scope:**
- Add `GetScheduleAsync()` to `IDaikinApiService`
- Add `GetScheduleInfoAsync()` to `IDaikinApiService`
- Create `ScheduleEvent`, `DaySchedule`, `WeeklySchedule` models
- Parse device response into strongly-typed models
- Unit tests with sample device responses

**API Calls:**
```
GET /aircon/get_scdltimer
GET /aircon/get_scdltimer_info
```

**Models:**
```csharp
public record ScheduleEvent(
    bool Enabled,
    bool PowerOn,
    DaikinMode Mode,
    double? TemperatureCelsius,
    TimeOnly Time);

public record DaySchedule(
    DayOfWeek Day,
    IReadOnlyList<ScheduleEvent> Events);

public record WeeklySchedule(
    IReadOnlyList<DaySchedule> Days);

public record SchedulerInfo(
    bool Enabled,
    int ActiveSlot,
    int MaxEventsPerDay);
```

**Acceptance Criteria:**
- [ ] Can read schedule from real device
- [ ] Correctly parses all 7 days
- [ ] Handles empty schedule (all events disabled)
- [ ] Handles device offline gracefully

**Workflows Enabled:** None yet (API only)

---

### FEATURE 2: View Today's Schedule (Basic UI)
**Priority:** P0 - First visible feature  
**Effort:** 4 hours  
**Dependencies:** F1

**Scope:**
- Add "Schedule" tab to navigation
- Create `ScheduleViewModel` with today's events
- Simple list view showing today's scheduled events
- Show "Next Up" event with countdown
- Refresh on tab navigation

**UI (Minimal):**
```
┌─────────────────────────────────────────┐
│  SCHEDULE                               │
├─────────────────────────────────────────┤
│                                         │
│  Next: 5:00 PM → Cool 72°F              │
│        in 2h 34m                        │
│                                         │
│  ─────────────────────────────────────  │
│                                         │
│  Today (Tuesday)                        │
│  • 6:00 AM   Heat 70°F      ✓          │
│  • 8:00 AM   OFF            ✓          │
│  • 5:00 PM   Cool 72°F      ← next     │
│  • 10:30 PM  Cool 68°F                 │
│                                         │
│  ─────────────────────────────────────  │
│  ℹ️ Schedules run on device             │
└─────────────────────────────────────────┘
```

**WinUI3 Controls:**
- `ListView` with `DataTemplate` for events
- `TextBlock` for countdown (update via timer)
- Segoe Fluent Icons for mode indicators

**Acceptance Criteria:**
- [ ] Schedule tab appears in navigation
- [ ] Shows today's events in chronological order
- [ ] "Next Up" countdown updates every minute
- [ ] Checkmarks for past events
- [ ] Shows "No schedule configured" if empty

**Workflows Enabled:**
- ✅ #4: View Today's Schedule (daily use)

---

### FEATURE 3: View Full Week
**Priority:** P1  
**Effort:** 3 hours  
**Dependencies:** F2
**Status:** ✅ Implemented 2026-01-28

**Scope:**
- Add "View Week" button to Schedule tab
- Day selector (7 day buttons/tabs)
- Show selected day's events
- Highlight today

**UI:**
```
┌─────────────────────────────────────────┐
│  ← This Week                            │
├─────────────────────────────────────────┤
│  [Mon][Tue][Wed][Thu][Fri][Sat][Sun]    │
│         ▲                               │
│       today                             │
│                                         │
│  TUESDAY                                │
│  • 6:00 AM   Heat 70°F                 │
│  • 8:00 AM   OFF                       │
│  • 5:00 PM   Cool 72°F                 │
│  • 10:30 PM  Cool 68°F                 │
│                                         │
│  (Tap another day to view)              │
└─────────────────────────────────────────┘
```

**Acceptance Criteria:**
- [ ] Can navigate from Schedule home to Week view
- [ ] Day buttons show event count badge
- [ ] Tapping day shows that day's events
- [ ] Today is visually highlighted
- [ ] Back button returns to Schedule home

**Workflows Enabled:**
- ✅ #5: View Full Week

---

### FEATURE 4: Enable/Disable Scheduling
**Priority:** P0 - Critical for pause/resume  
**Effort:** 2 hours  
**Dependencies:** F1

**Scope:**
- Add `SetScheduleEnabledAsync(bool)` to API service
- Add toggle switch to Schedule tab header
- Persist state to device immediately

**API Call:**
```
GET /aircon/set_scdltimer_info?en_scdltimer=0|1
```

**UI:**
```
┌─────────────────────────────────────────┐
│  SCHEDULE                    [ON ●────] │
├─────────────────────────────────────────┤
```

When OFF:
```
┌─────────────────────────────────────────┐
│  SCHEDULE                    [○──── OFF]│
├─────────────────────────────────────────┤
│                                         │
│  ⏸️ Schedule Paused                     │
│                                         │
│  Your schedule is saved but not         │
│  running. Toggle above to resume.       │
│                                         │
└─────────────────────────────────────────┘
```

**Acceptance Criteria:**
- [ ] Toggle reflects current device state on load
- [ ] Toggling OFF sends API call immediately
- [ ] UI updates to show paused state
- [ ] Schedule data preserved when disabled

**Workflows Enabled:**
- ✅ #8a: Pause/Resume Schedule (simple)

---

### FEATURE 5: Write Schedule to Device (API Layer)
**Priority:** P0 - Required for any editing  
**Effort:** 4 hours  
**Dependencies:** F1

**Scope:**
- Add `SetScheduleAsync(WeeklySchedule)` to API service
- Build query string from model (all 7 days, atomic)
- Handle device response/errors
- Validate before sending (time order, limits)

**API Call:**
```
GET /aircon/set_scdltimer?moc=2&mo1_en=1&mo1_pow=1&...&suc=1&su1_en=0...
```

**Validation Rules:**
- Times must be in chronological order per day
- Max 6 events per day
- Temperature required if power=ON
- Mode required if power=ON

**Acceptance Criteria:**
- [ ] Can write schedule to real device
- [ ] Read-after-write confirms changes persisted
- [ ] Validation errors returned before API call
- [ ] Handles device offline gracefully

**Workflows Enabled:** None yet (API only, enables F6-F9)

---

### FEATURE 6: Edit Existing Event
**Priority:** P0 - Core editing  
**Effort:** 4 hours  
**Dependencies:** F3, F5

**Scope:**
- Tap event in week view → opens Event Editor dialog
- Edit time, power state, mode, temperature
- Save updates schedule on device
- Cancel discards changes

**UI (ContentDialog):**
```
┌─────────────────────────────────────────┐
│  Edit Event                             │
├─────────────────────────────────────────┤
│                                         │
│  Time        [5:00 PM    ◀▶]            │
│                                         │
│  Action                                 │
│  ● Turn ON                              │
│    Mode: [Cool ▼]  Temp: [72°F ◀▶]      │
│                                         │
│  ○ Turn OFF                             │
│                                         │
│       [Cancel]              [Save]      │
└─────────────────────────────────────────┘
```

**WinUI3 Controls:**
- `ContentDialog` for modal editing
- `TimePicker` for time
- `RadioButtons` for ON/OFF
- `ComboBox` for mode
- `NumberBox` for temperature

**Acceptance Criteria:**
- [ ] Tapping event opens editor with current values
- [ ] Can change time, mode, temperature
- [ ] Save writes to device and refreshes UI
- [ ] Cancel closes without changes
- [ ] Validation prevents invalid values

**Workflows Enabled:**
- ✅ #6: Edit Existing Schedule

---

### FEATURE 7: Add/Delete Events
**Priority:** P1  
**Effort:** 3 hours  
**Dependencies:** F6

**Scope:**
- "Add" button in day view → opens Event Editor (blank)
- "Delete" button in Event Editor
- Confirm before delete
- Enforce 6-event limit gracefully

**UI Additions:**
```
Week View:
│  TUESDAY                                │
│  • 6:00 AM   Heat 70°F          [Edit] │
│  • 8:00 AM   OFF                [Edit] │
│                                        │
│  [+ Add Event]                         │
```

Delete in Editor:
```
│       [Delete]    [Cancel]    [Save]   │
```

**Acceptance Criteria:**
- [ ] Can add new event to any day
- [ ] New event defaults to reasonable values
- [ ] Can delete existing event
- [ ] Delete requires confirmation
- [ ] Cannot add 7th event (graceful error)

**Workflows Enabled:**
- ✅ #3: First-time Setup (Custom) - partial

---

### FEATURE 8: Copy Day to Other Days
**Priority:** P1 - Major time saver  
**Effort:** 2 hours  
**Dependencies:** F7

**Scope:**
- "Copy to..." button in day view
- Multi-select target days
- Confirm and apply

**UI:**
```
┌─────────────────────────────────────────┐
│  Copy Monday to...                      │
├─────────────────────────────────────────┤
│                                         │
│  ☐ Tuesday    (will replace 4 events)  │
│  ☐ Wednesday  (will replace 4 events)  │
│  ☐ Thursday   (will replace 4 events)  │
│  ☐ Friday     (will replace 4 events)  │
│  ☐ Saturday   (will replace 2 events)  │
│  ☐ Sunday     (will replace 2 events)  │
│                                         │
│       [Cancel]              [Copy]      │
└─────────────────────────────────────────┘
```

**Acceptance Criteria:**
- [ ] Can copy one day's events to multiple days
- [ ] Shows warning about overwriting
- [ ] Writes all changes in single API call
- [ ] UI refreshes to show copied events

**Workflows Enabled:**
- ✅ #3: First-time Setup (Custom) - full
- ✅ Efficiency improvement for all editing

---

### FEATURE 9: Setup Wizard
**Priority:** P1 - Better first-run experience  
**Effort:** 6 hours  
**Dependencies:** F5

**Scope:**
- Detect "no schedule" state → show wizard
- 3 paths: Work Away / Work From Home / Custom
- Template-based questions
- Generates schedule and saves to device

**Flow:**
```
[No schedule detected]
        │
        ▼
┌─────────────────────────────────────────┐
│  Set Up Your Schedule                   │
├─────────────────────────────────────────┤
│                                         │
│  What's your typical day?               │
│                                         │
│  [🏢 Work outside home     ]            │
│  [🏠 Work from home        ]            │
│  [✏️ Custom schedule       ]            │
│                                         │
└─────────────────────────────────────────┘
        │
        ▼ (Work outside home)
┌─────────────────────────────────────────┐
│  Work Schedule Setup                    │
├─────────────────────────────────────────┤
│  When do you leave?    [8:00 AM ◀▶]     │
│  When do you return?   [5:30 PM ◀▶]     │
│                                         │
│  ☑ Cool down 30 min before I return     │
│                                         │
│  Comfort temp          [72°F ◀▶]        │
│                                         │
│  Which days?                            │
│  [M̲][T̲][W̲][T̲][F̲][S][S]                  │
│                                         │
│       [Back]              [Continue]    │
└─────────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────────┐
│  Weekends?                              │
├─────────────────────────────────────────┤
│  ○ Same as weekdays                     │
│  ● Keep comfortable all day             │
│  ○ No schedule (manual control)         │
│                                         │
│       [Back]              [Finish]      │
└─────────────────────────────────────────┘
        │
        ▼
[Schedule saved → Show Schedule Home]
```

**Acceptance Criteria:**
- [ ] Wizard appears when no schedule exists
- [ ] Can skip wizard (go to manual editing)
- [ ] "Work away" path creates 2-event weekday schedule
- [ ] "WFH" path creates 3-event schedule
- [ ] "Custom" path goes to week view for manual entry
- [ ] Weekend handling (same/different/none)
- [ ] Saves to device on Finish

**Workflows Enabled:**
- ✅ #1: First-time Setup (Commuter)
- ✅ #2: First-time Setup (WFH)
- ✅ #3: First-time Setup (Custom) - via redirect

---

### FEATURE 10: Override from Controls Tab
**Priority:** P2 - Polish  
**Effort:** 4 hours  
**Dependencies:** F2 (for schedule awareness)

**Scope:**
- Controls tab detects if schedule says AC should be OFF
- Prompt when user tries to turn ON
- Track override state locally
- Show override badge until next scheduled event

**Note:** This is client-side only - device doesn't have override concept.

**UI on Controls Tab:**
```
When schedule says OFF but user turns ON:
┌─────────────────────────────────────────┐
│  Override Schedule?                     │
├─────────────────────────────────────────┤
│  Your schedule has the AC off until     │
│  5:00 PM. Turn it on anyway?            │
│                                         │
│  [Turn On Until 5 PM]                   │
│  [Turn On for 2 Hours]                  │
│  [Cancel]                               │
└─────────────────────────────────────────┘
```

After override:
```
┌─────────────────────────────────────────┐
│  ⚠️ OVERRIDE ACTIVE                     │
│  Schedule will resume at 5:00 PM        │
│  [End Override]                         │
└─────────────────────────────────────────┘
```

**Acceptance Criteria:**
- [ ] Detects schedule conflict when user changes state
- [ ] Offers override options
- [ ] Shows override badge on Controls tab
- [ ] Override ends at next scheduled event
- [ ] "End Override" forces re-read of schedule

**Workflows Enabled:**
- ✅ #7: Quick Override

---

## 📅 Implementation Phases

### Phase 1: Read-Only (View Schedule)
**Features:** F1, F2, F4  
**Time:** ~10 hours  
**Deliverable:** Users can view their existing schedule and pause/resume it

| Feature | Hours | Cumulative |
|---------|-------|------------|
| F1: Read API | 4h | 4h |
| F2: View Today | 4h | 8h |
| F4: Enable/Disable | 2h | 10h |

**User Value:** "I can see what my AC is going to do today"

---

### Phase 2: Basic Editing
**Features:** F3, F5, F6  
**Time:** ~11 hours  
**Deliverable:** Users can view week and edit existing events

| Feature | Hours | Cumulative |
|---------|-------|------------|
| F3: View Week | 3h | 13h |
| F5: Write API | 4h | 17h |
| F6: Edit Event | 4h | 21h |

**User Value:** "I can change my schedule times"

---

### Phase 3: Full Editing
**Features:** F7, F8  
**Time:** ~5 hours  
**Deliverable:** Users can add/delete events and copy between days

| Feature | Hours | Cumulative |
|---------|-------|------------|
| F7: Add/Delete | 3h | 24h |
| F8: Copy Days | 2h | 26h |

**User Value:** "I can build any schedule I want"

---

### Phase 4: Guided Setup
**Features:** F9  
**Time:** ~6 hours  
**Deliverable:** First-time users get wizard-based setup

| Feature | Hours | Cumulative |
|---------|-------|------------|
| F9: Setup Wizard | 6h | 32h |

**User Value:** "Setting up was easy - just answered a few questions"

---

### Phase 5: Smart Override
**Features:** F10  
**Time:** ~4 hours  
**Deliverable:** Users can override schedule from Controls tab

| Feature | Hours | Cumulative |
|---------|-------|------------|
| F10: Override | 4h | 36h |

**User Value:** "I can quickly change things without messing up my schedule"

---

## 🗺️ Workflow Coverage by Phase

| Workflow | Phase 1 | Phase 2 | Phase 3 | Phase 4 | Phase 5 |
|----------|---------|---------|---------|---------|---------|
| #1 Setup (Commuter) | ❌ | ❌ | ⚠️ Manual | ✅ Wizard | ✅ |
| #2 Setup (WFH) | ❌ | ❌ | ⚠️ Manual | ✅ Wizard | ✅ |
| #3 Setup (Custom) | ❌ | ❌ | ✅ | ✅ | ✅ |
| #4 View Today | ✅ | ✅ | ✅ | ✅ | ✅ |
| #5 View Week | ❌ | ✅ | ✅ | ✅ | ✅ |
| #6 Edit Schedule | ❌ | ✅ | ✅ | ✅ | ✅ |
| #7 Quick Override | ❌ | ❌ | ❌ | ❌ | ✅ |
| #8 Pause/Resume | ✅ | ✅ | ✅ | ✅ | ✅ |
| #9 Seasonal Switch | ⚠️ | ⚠️ | ✅ | ✅ | ✅ |
| #10 Error Recovery | ⚠️ | ✅ | ✅ | ✅ | ✅ |

---

## ✅ Recommended Starting Point

**Start with Phase 1 (F1 + F2 + F4):**

1. Users can see existing schedules (if configured via OEM app)
2. Users can pause/resume scheduling
3. Foundation for all future features
4. Lowest risk - read operations can't break anything
5. ~10 hours to first usable feature

**Then decide:** Is the wizard (Phase 4) more important than full editing (Phase 2-3)?

For users with NO existing schedule:
- Phase 4 first → better experience
- Phase 2-3 first → more flexible but harder

**My recommendation:** Phase 1 → 2 → 3 → 4 → 5

This lets power users (who may have schedules from OEM app) benefit immediately, and builds up editing capability before adding the wizard polish.

---

## 📝 Definition of Done (Per Feature)

- [ ] Code complete with error handling
- [ ] Works against real device
- [ ] Basic UI tests pass
- [ ] No regressions in existing features
- [ ] Code reviewed
- [ ] Accessible (keyboard nav, screen reader)
