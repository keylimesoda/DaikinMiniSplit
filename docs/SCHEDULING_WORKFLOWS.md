# Scheduling UX - Complete Workflow Analysis

**Document:** Supplement to SCHEDULING_UX_REVIEW_V2.md  
**Date:** January 28, 2026  
**Purpose:** Address the full range of user workflows, not just simple setup

---

## 🎯 The Problem with "Just Templates"

The V2 review over-indexed on first-time setup. Real users need to:

1. **Set up** (one-time)
2. **View** what's scheduled (daily)
3. **Edit** existing schedules (occasional)
4. **Override** temporarily (frequent)
5. **Disable/Enable** (seasonal)

Each deserves proper UX attention.

---

## 📋 Complete Workflow Inventory

### WORKFLOW 1: First-Time Setup (Simple)
**User:** Dan (Commuter)  
**Goal:** "AC off when I'm at work, on when I get home"  
**Frequency:** Once

```
┌─────────────────────────────────────────┐
│  Let's set up your schedule             │
│                                         │
│  I mostly...                            │
│  ┌─────────────────────────────────┐    │
│  │ 🏢 Work outside the home        │ ←  │
│  └─────────────────────────────────┘    │
│  ┌─────────────────────────────────┐    │
│  │ 🏠 Work from home               │    │
│  └─────────────────────────────────┘    │
│  ┌─────────────────────────────────┐    │
│  │ 🎲 Have a variable schedule     │    │
│  └─────────────────────────────────┘    │
└─────────────────────────────────────────┘
         │
         ▼ (Selected "Work outside")
┌─────────────────────────────────────────┐
│  When do you usually...                 │
│                                         │
│  Leave home?     [ 8:00 AM  ◀▶ ]        │
│  Get back?       [ 5:30 PM  ◀▶ ]        │
│                                         │
│  ☑ Start cooling 30 min before I return │
│                                         │
│  Which days?                            │
│  [M̲][T̲][W̲][T̲][F̲][ S ][ S ]              │
│                                         │
│  My comfort temperature                 │
│  ❄️ ─────────●───── 🔥                  │
│            72°F                         │
│                                         │
│  [Back]                    [Continue]   │
└─────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────┐
│  What about weekends?                   │
│                                         │
│  ○ Same as weekdays                     │
│  ● Different schedule                   │
│  ○ No schedule (manual control)         │
│                                         │
│  ─────────────────────────────────────  │
│  (if "Different" selected)              │
│                                         │
│  Wake up time?   [ 9:00 AM  ◀▶ ]        │
│  Bedtime?        [ 11:00 PM ◀▶ ]        │
│                                         │
│  ☑ Keep it comfortable all day          │
│                                         │
│  [Back]                    [Finish]     │
└─────────────────────────────────────────┘
```

**Result:** System creates ONE device schedule with:
- Mon-Fri: OFF at 8:00 AM, ON at 5:00 PM
- Sat-Sun: ON at 9:00 AM, OFF at 11:00 PM

**Time to complete:** ~45 seconds

---

### WORKFLOW 2: First-Time Setup (Work From Home)
**User:** Wendy (WFH)  
**Goal:** "Comfortable during work hours, save energy at night"  
**Frequency:** Once

```
┌─────────────────────────────────────────┐
│  Work from home schedule                │
│                                         │
│  When does your day start?              │
│  [ 7:30 AM  ◀▶ ]                        │
│                                         │
│  When do you usually finish work?       │
│  [ 6:00 PM  ◀▶ ]                        │
│                                         │
│  Bedtime?                               │
│  [ 10:30 PM ◀▶ ]                        │
│                                         │
│  ─────────────────────────────────────  │
│                                         │
│  Daytime comfort      [ 72°F  ◀▶ ]      │
│  Sleeping comfort     [ 68°F  ◀▶ ]      │
│                                         │
│  Which days do you work from home?      │
│  [M̲][T̲][W̲][T̲][F̲][ S ][ S ]              │
│                                         │
│  [Back]                    [Finish]     │
└─────────────────────────────────────────┘
```

**Result:** Device schedule with:
- Mon-Fri: 
  - 7:30 AM → ON, Cool 72°F
  - 10:30 PM → Cool 68°F (or OFF based on preference)
- Sat-Sun: (prompts separately or copies weekday)

---

### WORKFLOW 3: First-Time Setup (Variable/Custom)
**User:** Power user or complex needs  
**Goal:** "I need specific control"  
**Frequency:** Once

```
┌─────────────────────────────────────────┐
│  Custom Schedule Builder                │
│                                         │
│  ┌─────────────────────────────────────┐│
│  │ Mon  Tue  Wed  Thu  Fri  Sat  Sun  ││
│  │ [●]  [●]  [●]  [●]  [●]  [ ]  [ ]  ││
│  └─────────────────────────────────────┘│
│  Editing: Weekdays (Mon-Fri)            │
│                                         │
│  YOUR DAY                               │
│  ┌─────────────────────────────────────┐│
│  │ 6:00 AM   Wake up     🔥 Heat 70°F ││
│  │     │                               ││
│  │     ▼                               ││
│  │ 8:00 AM   Leave       ⏻ OFF        ││
│  │     │                               ││
│  │     ▼                               ││
│  │ 5:00 PM   Return      ❄️ Cool 72°F ││
│  │     │                               ││
│  │     ▼                               ││
│  │ 10:30 PM  Bedtime     ❄️ Cool 68°F ││
│  └─────────────────────────────────────┘│
│                                         │
│  [+ Add another time]                   │
│                                         │
│  [Back]                    [Continue]   │
└─────────────────────────────────────────┘
```

**Key UX elements:**
- Visual timeline with clear flow
- Friendly labels ("Wake up", "Leave", "Return", "Bedtime")
- Can add up to 6 times per day (device limit, but don't show number)
- Select multiple days to edit together

---

### WORKFLOW 4: View Current Schedule (Daily Use)
**User:** Any  
**Goal:** "What's my AC going to do today?"  
**Frequency:** Daily

This is the **Schedule tab home state** - what users see most often:

```
┌─────────────────────────────────────────┐
│  SCHEDULE                    [Enabled ●]│
├─────────────────────────────────────────┤
│                                         │
│  ⏰ NEXT UP                             │
│  ┌─────────────────────────────────────┐│
│  │                                     ││
│  │   5:00 PM  →  ❄️ Cool 72°F          ││
│  │                                     ││
│  │   in 2 hours 34 minutes             ││
│  │                                     ││
│  └─────────────────────────────────────┘│
│                                         │
│  TODAY (Tuesday)                        │
│  ┌─────────────────────────────────────┐│
│  │ ✓  6:00 AM    🔥 Heat 70°F          ││
│  │ ✓  8:00 AM    ⏻ OFF                 ││
│  │ →  5:00 PM    ❄️ Cool 72°F          ││
│  │    10:30 PM   ❄️ Cool 68°F          ││
│  └─────────────────────────────────────┘│
│                                         │
│  [View Full Week]        [Edit Schedule]│
│                                         │
└─────────────────────────────────────────┘
```

**Key elements:**
- **"Next Up" is hero** - biggest, most prominent
- **Countdown** - "in 2 hours 34 minutes" creates anticipation
- **Today's list** - checkmarks for completed, arrow for next
- **Actions** - View week or Edit

---

### WORKFLOW 5: View Full Week
**User:** Any  
**Goal:** "What's the schedule for the whole week?"  
**Frequency:** Occasional

```
┌─────────────────────────────────────────┐
│  ← This Week                     [Edit] │
├─────────────────────────────────────────┤
│                                         │
│  ┌─────┬─────┬─────┬─────┬─────┬─────┬─────┐
│  │ MON │ TUE │ WED │ THU │ FRI │ SAT │ SUN │
│  ├─────┼─────┼─────┼─────┼─────┼─────┼─────┤
│  │  4  │  4  │  4  │  4  │  4  │  2  │  2  │
│  └─────┴──●──┴─────┴─────┴─────┴─────┴─────┘
│          ▲ Today                        │
│                                         │
│  TUESDAY                                │
│  ┌─────────────────────────────────────┐│
│  │  6:00 AM    🔥 Heat 70°F            ││
│  │  8:00 AM    ⏻ Turn OFF              ││
│  │  5:00 PM    ❄️ Cool 72°F            ││
│  │ 10:30 PM    ❄️ Cool 68°F            ││
│  └─────────────────────────────────────┘│
│                                         │
│  ─── Tap a day above to view ───        │
│                                         │
└─────────────────────────────────────────┘
```

---

### WORKFLOW 6: Edit Existing Schedule
**User:** Dan  
**Goal:** "My work hours changed, now I leave at 7:30"  
**Frequency:** Occasional

**Entry point:** "Edit Schedule" button or tap on specific event

```
┌─────────────────────────────────────────┐
│  ← Edit Schedule                 [Save] │
├─────────────────────────────────────────┤
│                                         │
│  WEEKDAYS (Mon-Fri)                     │
│  ┌─────────────────────────────────────┐│
│  │  6:00 AM    🔥 Heat 70°F     [Edit] ││
│  │  8:00 AM    ⏻ OFF            [Edit] │◀── Tap
│  │  5:00 PM    ❄️ Cool 72°F     [Edit] ││
│  │ 10:30 PM    ❄️ Cool 68°F     [Edit] ││
│  │                                     ││
│  │ [+ Add time]                        ││
│  └─────────────────────────────────────┘│
│                                         │
│  WEEKENDS (Sat-Sun)                     │
│  ┌─────────────────────────────────────┐│
│  │  9:00 AM    ❄️ Cool 72°F     [Edit] ││
│  │ 11:00 PM    ⏻ OFF            [Edit] ││
│  │                                     ││
│  │ [+ Add time]                        ││
│  └─────────────────────────────────────┘│
│                                         │
└─────────────────────────────────────────┘
         │
         ▼ (Tapped Edit on 8:00 AM)
┌─────────────────────────────────────────┐
│  Edit Time                              │
├─────────────────────────────────────────┤
│                                         │
│  Time         [ 7 ] : [ 30 ]  [ AM ▼]   │
│                                         │
│  Action       ⏻ Turn OFF                │
│               (no settings when off)    │
│                                         │
│  Applies to   [M̲][T̲][W̲][T̲][F̲][ ][ ]     │
│                                         │
│  [Delete This Time]                     │
│                                         │
│       [Cancel]              [Save]      │
└─────────────────────────────────────────┘
```

---

### WORKFLOW 7: Quick Override ("Home Sick Today")
**User:** Wendy  
**Goal:** "I'm home, turn AC on NOW, but don't mess up my schedule"  
**Frequency:** Few times per month

**This happens from the Controls tab, not Schedule tab:**

```
┌─────────────────────────────────────────┐
│  CONTROLS                               │
├─────────────────────────────────────────┤
│                                         │
│     ┌─────────────────────────┐         │
│     │                         │         │
│     │      ⏻ OFF              │         │
│     │                         │         │
│     │   Scheduled until       │         │
│     │      5:00 PM            │         │
│     │                         │         │
│     └─────────────────────────┘         │
│                                         │
│  ┌─────────────────────────────────────┐│
│  │  💡 Override Schedule?              ││
│  │                                     ││
│  │  Your schedule has the AC off now.  ││
│  │  Turn it on temporarily?            ││
│  │                                     ││
│  │  [Turn On Until 5 PM]  [Turn On 2hr]││
│  └─────────────────────────────────────┘│
│                                         │
└─────────────────────────────────────────┘
```

**After override:**

```
┌─────────────────────────────────────────┐
│  CONTROLS                               │
├─────────────────────────────────────────┤
│                                         │
│     ┌─────────────────────────┐         │
│     │       ❄️ 72°F           │         │
│     │                         │         │
│     │      OVERRIDE           │ ◀─ Yellow badge
│     │   Until 5:00 PM         │         │
│     │                         │         │
│     └─────────────────────────┘         │
│                                         │
│  ┌─────────────────────────────────────┐│
│  │  ⚠️ Schedule overridden             ││
│  │                                     ││
│  │  Normal schedule will resume at     ││
│  │  5:00 PM (in 3 hours 22 min)        ││
│  │                                     ││
│  │  [End Override Now]                 ││
│  └─────────────────────────────────────┘│
│                                         │
└─────────────────────────────────────────┘
```

---

### WORKFLOW 8: Pause Schedule (Vacation)
**User:** Steve  
**Goal:** "I'm away for 2 weeks, just keep pipes from freezing"  
**Frequency:** Few times per year

**Option A: Simple toggle (minimum viable)**

```
┌─────────────────────────────────────────┐
│  SCHEDULE                   [Paused ○●] │
├─────────────────────────────────────────┤
│                                         │
│  ┌─────────────────────────────────────┐│
│  │  ⏸️ Schedule Paused                 ││
│  │                                     ││
│  │  Your schedule is saved but not     ││
│  │  running. The AC is in manual mode. ││
│  │                                     ││
│  │  [Resume Schedule]                  ││
│  └─────────────────────────────────────┘│
│                                         │
│  💡 TIP: Set a minimum temperature     │
│     on the Controls tab to prevent     │
│     freezing while you're away.        │
│                                         │
└─────────────────────────────────────────┘
```

**Option B: Vacation Mode (enhanced - Phase 2)**

```
┌─────────────────────────────────────────┐
│  Vacation Mode                          │
├─────────────────────────────────────────┤
│                                         │
│  I'll be away...                        │
│                                         │
│  From  [ Jan 15 ▼ ]  To  [ Jan 28 ▼ ]   │
│                                         │
│  While I'm away:                        │
│  ┌─────────────────────────────────────┐│
│  │ ● Freeze protection (Heat to 50°F   ││
│  │   if it gets too cold)              ││
│  └─────────────────────────────────────┘│
│  ┌─────────────────────────────────────┐│
│  │ ○ Eco mode (minimal heating/cooling ││
│  │   to save energy)                   ││
│  └─────────────────────────────────────┘│
│  ┌─────────────────────────────────────┐│
│  │ ○ Completely off                    ││
│  └─────────────────────────────────────┘│
│                                         │
│  ☑ Restore normal schedule when I return│
│                                         │
│       [Cancel]         [Start Vacation] │
└─────────────────────────────────────────┘
```

---

### WORKFLOW 9: Seasonal Switch
**User:** Emma  
**Goal:** "Summer's here, switch from heating to cooling schedule"  
**Frequency:** 2x per year

**Question: Do we need separate schedules for seasons?**

**Analysis:**
- Device has 3 schedule slots
- We could use: Slot 1 = Summer, Slot 2 = Winter, Slot 3 = Vacation
- BUT: This exposes complexity to users

**Simpler approach: Let the MODE handle it**

The schedule defines TIMES, not modes. When the event fires:
- If user's preferred mode is "Cool" → AC cools to set temp
- If user's preferred mode is "Heat" → AC heats to set temp
- If user's preferred mode is "Auto" → AC decides

```
┌─────────────────────────────────────────┐
│  SCHEDULE                               │
├─────────────────────────────────────────┤
│                                         │
│  Season mode:  [ Auto (recommended) ▼]  │
│                                         │
│                ┌──────────────────┐     │
│                │ ○ Auto           │     │
│                │ ○ Cooling only   │     │
│                │ ○ Heating only   │     │
│                └──────────────────┘     │
│                                         │
│  ℹ️ Your schedule times stay the same.  │
│     Just change the mode when seasons   │
│     change.                             │
│                                         │
└─────────────────────────────────────────┘
```

**This way:**
- ONE schedule covers all seasons
- User just flips "Season mode" when needed
- No need to manage multiple schedule slots

---

### WORKFLOW 10: Error Recovery
**User:** Any  
**Goal:** "Something went wrong, fix it"  
**Frequency:** Rare (hopefully)

**10a: Device unreachable when saving**

```
┌─────────────────────────────────────────┐
│  ❌ Couldn't Save                       │
├─────────────────────────────────────────┤
│                                         │
│  Your AC isn't responding.              │
│                                         │
│  Your changes have NOT been saved yet.  │
│                                         │
│  Things to try:                         │
│  • Check if the AC is powered on        │
│  • Make sure you're on the same WiFi    │
│  • The AC might be busy - wait a moment │
│                                         │
│       [Try Again]       [Cancel]        │
│                                         │
└─────────────────────────────────────────┘
```

**10b: Schedule not running as expected**

```
┌─────────────────────────────────────────┐
│  SCHEDULE                    [Enabled ●]│
├─────────────────────────────────────────┤
│                                         │
│  ⚠️ Schedule may be out of sync        │
│  ┌─────────────────────────────────────┐│
│  │  The AC's current state doesn't     ││
│  │  match the schedule. This can       ││
│  │  happen if someone used the remote  ││
│  │  or the app was used on another     ││
│  │  device.                            ││
│  │                                     ││
│  │  [Re-sync Now]    [Ignore]          ││
│  └─────────────────────────────────────┘│
│                                         │
└─────────────────────────────────────────┘
```

---

## 📊 Workflow Complexity Matrix

| Workflow | Frequency | Steps | Target Time | MVP? |
|----------|-----------|-------|-------------|------|
| 1. Setup (Simple) | Once | 4-5 | 45 sec | ✅ |
| 2. Setup (WFH) | Once | 4-5 | 45 sec | ✅ |
| 3. Setup (Custom) | Once | 6-8 | 90 sec | ✅ |
| 4. View Today | Daily | 1 | 2 sec | ✅ |
| 5. View Week | Weekly | 2 | 5 sec | ✅ |
| 6. Edit Schedule | Monthly | 3-4 | 20 sec | ✅ |
| 7. Quick Override | Few/month | 2 | 5 sec | ✅ |
| 8a. Pause (Simple) | Few/year | 1 | 2 sec | ✅ |
| 8b. Vacation Mode | Few/year | 4 | 30 sec | Phase 2 |
| 9. Seasonal Switch | 2x/year | 1 | 3 sec | ✅ |
| 10. Error Recovery | Rare | 2-3 | 10 sec | ✅ |

---

## 🔑 Key Design Decisions

### Decision 1: One Schedule, Not Three Slots
**Recommendation:** Hide the 3-slot limitation entirely. Present as ONE schedule that covers the whole week.

**Why:** Users don't think "I need to switch to slot 2." They think "Weekends are different."

**Implementation:** All 7 days go in one device schedule slot. Only use multiple slots if we add Vacation Mode (Phase 2).

---

### Decision 2: Weekday/Weekend Grouping
**Recommendation:** Default UX groups Mon-Fri and Sat-Sun. Power users can edit individual days.

```
Default View:              Power User View:
┌─────────────────┐        ┌─────────────────┐
│ WEEKDAYS        │        │ Mon  [4 events] │
│ Mon-Fri         │   OR   │ Tue  [4 events] │
│ [4 events]      │        │ Wed  [4 events] │
├─────────────────┤        │ Thu  [4 events] │
│ WEEKENDS        │        │ Fri  [4 events] │
│ Sat-Sun         │        │ Sat  [2 events] │
│ [2 events]      │        │ Sun  [2 events] │
└─────────────────┘        └─────────────────┘
```

Toggle: `[ ] Edit days individually`

---

### Decision 3: Override Lives on Controls Tab
**Recommendation:** Override is NOT a schedule feature. It's a control feature.

**Why:** When users want AC on NOW, they go to Controls, not Schedule. The override prompt appears there.

---

### Decision 4: Temperature vs Mode in Schedule
**Recommendation:** Schedule stores temperature + mode together as a single "comfort setting"

Bad: "5 PM: Mode=Cool, Temp=72°F, Fan=Auto"
Good: "5 PM: Make it 72°F" (mode inferred from season setting)

---

### Decision 5: Maximum Times Per Day
**Device limit:** 6 events per day  
**UX approach:** Never show the number. Just say "Add another time" and gracefully handle the limit.

```
If user tries to add 7th event:
┌─────────────────────────────────────────┐
│  That's a lot of changes!               │
│                                         │
│  Your AC can handle up to 6 scheduled   │
│  changes per day. Consider combining    │
│  some times or removing one.            │
│                                         │
│  [OK]                                   │
└─────────────────────────────────────────┘
```

---

## 📱 Navigation Structure

```
┌─────────────────────────────────────────┐
│  DAIKIN MANAGER                         │
├───────────┬───────────┬─────────────────┤
│ Controls  │ Schedule  │ Diagnostics     │
│    ●      │     ○     │       ○         │
└───────────┴───────────┴─────────────────┘

Schedule Tab:
├── Schedule Home (View Today + Next Up)
│   ├── [View Full Week] → Week View
│   │   └── Tap day → Day Detail
│   └── [Edit Schedule] → Schedule Editor
│       ├── Weekdays section
│       │   ├── Tap event → Event Editor
│       │   └── [+ Add time] → Event Editor (new)
│       └── Weekends section
│           ├── Tap event → Event Editor  
│           └── [+ Add time] → Event Editor (new)
│
└── First-Run: Setup Wizard (if no schedule exists)
    ├── Choose lifestyle → 
    ├── Set times →
    ├── Set days →
    └── Confirm → Schedule Home
```

---

## ✅ Updated Action Items

| # | Action | Priority |
|---|--------|----------|
| 1 | Design Setup Wizard with 3 paths (Work/WFH/Custom) | P0 |
| 2 | Design Schedule Home (Next Up + Today) | P0 |
| 3 | Design Week View with day selection | P0 |
| 4 | Design Schedule Editor (Weekday/Weekend groups) | P0 |
| 5 | Design Event Editor dialog | P0 |
| 6 | Design Override flow on Controls tab | P0 |
| 7 | Design Pause toggle | P0 |
| 8 | Design error states | P0 |
| 9 | Design individual day editing (power user) | P1 |
| 10 | Design Vacation Mode | P2 |

---

*This supplement expands the V2 review to cover the full range of user workflows.*
