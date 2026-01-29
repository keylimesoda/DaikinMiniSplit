# Scheduling Feature - UX Specification
**Document Version:** 1.0  
**Date:** January 26, 2026  
**Author:** Development Team  
**Status:** DRAFT - Pending Review

---

## 📋 Executive Summary

This document outlines the UX specification for adding a scheduling tab to the Daikin Manager V2 application. Since the BRP072A43 adapter does not provide native scheduling APIs, this feature will be implemented as **client-side scheduling** that runs on the user's PC.

---

## 🔌 API Assessment

### Available Hardware APIs
| Endpoint | Function | Scheduling Support |
|----------|----------|-------------------|
| `/aircon/get_control_info` | Read current state | ❌ No schedule data |
| `/aircon/set_control_info` | Apply settings | ✅ Can be triggered by schedule |
| `/aircon/get_sensor_info` | Read temperatures | ✅ Can inform smart schedules |

### Critical Constraint
**⚠️ The BRP072A43 adapter has NO built-in scheduling capability.**

The adapter is a "dumb" relay that executes commands immediately. All scheduling logic must be implemented client-side in the Windows application.

### Implementation Approach
- **Local Scheduler Service:** Windows background task or always-running app
- **Storage:** Local JSON or SQLite database for schedule definitions
- **Execution:** HTTP calls to device at scheduled times

---

## 👥 User Personas

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

## 📖 User Scenarios

### Scenario 1: Create a Simple Weekday Schedule
**User:** Dan  
**Goal:** Set AC to turn off when I leave for work, turn on before I get home

**Flow:**
1. Open app → Navigate to "Schedule" tab
2. Tap "Create Schedule"
3. Select days: Mon, Tue, Wed, Thu, Fri
4. Add event: 8:00 AM → Power OFF
5. Add event: 5:00 PM → Power ON, Cool, 72°F, Auto fan
6. Name schedule: "Work Week"
7. Save and enable

**Success Criteria:**
- Schedule created in < 60 seconds
- Visual timeline shows events clearly
- Confirmation toast: "Schedule 'Work Week' is now active"

---

### Scenario 2: Override Schedule Temporarily
**User:** Wendy  
**Goal:** I'm home sick today, I need the AC on now but don't want to delete my schedule

**Flow:**
1. Current schedule has AC off (work hours)
2. Open app → See "Scheduled: OFF until 5:00 PM"
3. Tap "Override" button
4. Select: "Turn on until next scheduled event" OR "Turn on for X hours"
5. Adjust settings if needed
6. Confirm override

**Success Criteria:**
- Override takes < 10 seconds
- Clear indication that schedule is overridden (badge/icon)
- Schedule resumes automatically at next event

---

### Scenario 3: Set Vacation Mode
**User:** Steve  
**Goal:** Going to Florida for 2 months, keep pipes from freezing

**Flow:**
1. Open app → Schedule tab
2. Tap "Quick Modes" → "Vacation"
3. Set date range: Feb 1 - Mar 31
4. Select preset: "Freeze Protection" (Heat, 50°F minimum)
5. Optional: Add return warmup (heat to 68°F on Mar 31 at 2 PM)
6. Activate

**Success Criteria:**
- Single screen for vacation setup
- Shows estimated energy savings vs. normal operation
- Sends notification reminder before return date

---

### Scenario 4: Create Weekend Schedule
**User:** Dan  
**Goal:** Different schedule on Sat/Sun - sleep in, stay cool all day

**Flow:**
1. Open app → Schedule tab → "Create Schedule"
2. Select days: Sat, Sun
3. Add events:
   - 10:00 AM → Cool, 70°F (sleep in, AC starts late)
   - 11:00 PM → OFF or higher temp (bedtime)
4. Name: "Weekend Chill"
5. Save

**Conflict Handling:**
- System detects: "You have 'Work Week' active. Weekend days will use 'Weekend Chill' instead."

---

### Scenario 5: Copy/Duplicate Schedule
**User:** Emma  
**Goal:** Create a summer schedule based on my winter schedule with different temps

**Flow:**
1. Open existing schedule "Winter Comfort"
2. Tap overflow menu → "Duplicate"
3. Rename to "Summer Cool"
4. Edit temperature setpoints (68°F → 74°F)
5. Change modes (Heat → Cool)
6. Save

**Success Criteria:**
- Duplicate created instantly
- All times preserved, only settings changed
- Original schedule unchanged

---

### Scenario 6: Sensor-Based Smart Schedule
**User:** Emma  
**Goal:** Only run AC when indoor temp exceeds 76°F during "away" hours

**Flow:**
1. Create schedule with condition:
   - Time: 8 AM - 5 PM (away)
   - Condition: IF indoor temp > 76°F THEN Cool to 74°F
   - ELSE: Power OFF
2. System polls sensor every 5 minutes during window
3. Automatically adjusts based on conditions

**Success Criteria:**
- Clear explanation of conditional logic
- Shows current sensor reading in schedule view
- Log of when conditions triggered

---

### Scenario 7: Quick "Leaving/Returning" Buttons
**User:** Wendy  
**Goal:** I'm leaving unexpectedly, quick way to trigger "away" mode

**Flow:**
1. On main Controls tab, see "Quick Actions" section
2. Tap "I'm Leaving" button
3. System applies away preset (defined in settings)
4. Later, tap "I'm Home" to resume comfort settings

**Success Criteria:**
- Single tap action
- Works independently of schedules
- Shows time since "away" was activated

---

## 🎨 Proposed UI Layout

### Schedule Tab Structure
```
┌─────────────────────────────────────────┐
│ SCHEDULES                          [+]  │
├─────────────────────────────────────────┤
│ ┌─────────────────────────────────────┐ │
│ │ 🟢 Work Week           Mon-Fri     │ │
│ │    Next: OFF at 8:00 AM            │ │
│ └─────────────────────────────────────┘ │
│ ┌─────────────────────────────────────┐ │
│ │ 🟢 Weekend Chill       Sat-Sun     │ │
│ │    Next: ON at 10:00 AM            │ │
│ └─────────────────────────────────────┘ │
│ ┌─────────────────────────────────────┐ │
│ │ ⚪ Vacation Mode       Inactive    │ │
│ │    Tap to configure                │ │
│ └─────────────────────────────────────┘ │
├─────────────────────────────────────────┤
│ QUICK ACTIONS                           │
│ ┌──────────┐  ┌──────────┐              │
│ │ I'm      │  │ I'm      │              │
│ │ Leaving  │  │ Home     │              │
│ └──────────┘  └──────────┘              │
├─────────────────────────────────────────┤
│ ⚠️ Override Active                      │
│ "Manual ON until 5:00 PM"    [Cancel]   │
└─────────────────────────────────────────┘
```

### Schedule Editor
```
┌─────────────────────────────────────────┐
│ ← Edit Schedule                  [Save] │
├─────────────────────────────────────────┤
│ Name: [Work Week________________]       │
│                                         │
│ Days: ○M ●T ●W ●T ●F ○S ○S             │
│                                         │
│ TIMELINE                                │
│ ├── 12 AM ──────────────────────────    │
│ │                                       │
│ ├── 6 AM ───────────────────────────    │
│ │                                       │
│ ├── 8 AM ─── 🔴 OFF ────────────────    │
│ │                                       │
│ ├── 12 PM ──────────────────────────    │
│ │                                       │
│ ├── 5 PM ─── ❄️ Cool 72°F Auto ─────    │
│ │                                       │
│ ├── 6 PM ───────────────────────────    │
│ │                                       │
│ └── 12 AM ──────────────────────────    │
│                                         │
│ [+ Add Event]                           │
└─────────────────────────────────────────┘
```

---

## ⚠️ Edge Cases & Error Handling

| Situation | Handling |
|-----------|----------|
| App closed during scheduled event | ❌ Event missed - warn user that app must be running |
| PC asleep during event | ❌ Event missed - recommend "prevent sleep" setting |
| Network unavailable at event time | Retry 3x over 30 seconds, then mark as failed |
| Conflicting schedules (same day/time) | Show conflict warning, require resolution |
| Device offline | Show last-known state, queue command for retry |
| Daylight saving time change | Schedules follow local clock automatically |

---

## 🚀 MVP vs. Future Features

### MVP (Phase 1)
- [ ] Create/edit/delete schedules
- [ ] Daily time-based events (power, mode, temp, fan)
- [ ] Day-of-week selection
- [ ] Enable/disable schedules
- [ ] Manual override with auto-resume
- [ ] "I'm Leaving / I'm Home" quick actions

### Phase 2
- [ ] Vacation mode with date ranges
- [ ] Duplicate/copy schedules
- [ ] Conditional schedules (sensor-based)
- [ ] Schedule import/export (JSON)
- [ ] Multiple device support

### Phase 3 (Future)
- [ ] Windows Service for background execution (app can be closed)
- [ ] Mobile companion app integration
- [ ] Smart home integration (Home Assistant, etc.)
- [ ] Energy cost estimation

---

## 📝 Open Questions for Review

1. **Background Execution:** Should we require the app to stay running, or invest in a Windows Service immediately?
   
2. **Schedule Conflicts:** If "Work Week" and "Weekend" both have Friday selected, which wins?

3. **Quick Actions Presets:** Should "I'm Leaving" be user-configurable, or use smart defaults?

4. **Notification Strategy:** Toast notifications for each scheduled event, or only for failures?

5. **Sync with IR Remote:** If user changes settings via physical remote, should schedule auto-resume or pause?

---

## 👀 Review Participants

| Role | Name | Review Focus |
|------|------|--------------|
| Senior UX Designer | TBD | Overall flow, accessibility, visual design |
| Junior UX Designer | TBD | Micro-interactions, error states, edge cases |
| External Consultant | TBD | Industry best practices, competitive analysis |
| Engineering Lead | TBD | Technical feasibility, API constraints |
| Product Owner | TBD | Business value, prioritization |

---

## 📅 Review Meeting

**Proposed Date:** [To be scheduled]  
**Duration:** 90 minutes  
**Agenda:**
1. API constraints walkthrough (10 min)
2. Persona & scenario review (20 min)
3. UI mockup feedback (30 min)
4. Open questions discussion (20 min)
5. Prioritization & next steps (10 min)

---

*This document is a living spec. Please add comments and suggestions via PR or direct annotation.*
