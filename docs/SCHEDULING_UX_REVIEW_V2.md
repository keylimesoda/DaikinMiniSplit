# Scheduling Feature V2 - UX Spec Review Meeting Notes

**Date:** January 28, 2026  
**Duration:** 90 minutes  
**Document Reviewed:** [SCHEDULING_UX_SPEC_V2.md](SCHEDULING_UX_SPEC_V2.md)  
**Review Focus:** Clean, elegant, WinUI3 compliant, dead simple for regular users

---

## 👥 Attendees

| Role | Name | Present |
|------|------|---------|
| Senior UX Designer | Maria Chen | ✅ |
| Junior UX Designer | Alex Rivera | ✅ |
| External Consultant | Dr. Sarah Kim (UX Research) | ✅ |
| Engineering Lead | (You) | ✅ |
| Product Owner | Jordan Park | ✅ |

---

## 📋 Meeting Summary

### Overall Assessment: **MAJOR REVISION RECOMMENDED**

The V2 spec correctly identifies the device capabilities, but **the proposed UI is too complex for regular users**. The panel recommends a significant simplification before implementation.

**Key Concern (Dr. Kim):**
> "The spec exposes too much of the underlying API complexity to users. Regular folks don't think in terms of '3 schedule slots with 6 events per day.' They think: 'I want it cool when I get home.'"

---

## 🎯 Core Design Principles (Agreed by Panel)

Before diving into feedback, the panel aligned on these principles:

| Principle | Description |
|-----------|-------------|
| **1. Hide Complexity** | Users shouldn't know about "slots" or "events per day limits" |
| **2. Task-Oriented** | Frame around user goals, not device capabilities |
| **3. Progressive Disclosure** | Simple by default, advanced options tucked away |
| **4. WinUI3 Native** | Use standard controls, follow Fluent Design patterns |
| **5. Forgiveness** | Easy to undo, hard to break |

---

## 💬 Feedback by Section

### 1. API Assessment Section

> ✅ **APPROVED** - Engineering documentation only

**Maria (Senior UX):**
- "This is excellent *engineering* documentation. But none of this should be visible to users."
- "The API section should not influence the UI design directly."

**Action Items:**
- [x] Keep API docs for engineering reference
- [ ] Create separate "User Mental Model" section that abstracts away API details

---

### 2. User Personas

> ✅ **APPROVED** with simplification

**Alex (Junior UX):**
- "Four personas is good, but let's focus MVP on Dan (Commuter) and Wendy (WFH)."
- "Steve (Snowbird) and Emma (Energy-conscious) are Phase 2+ users."

**Dr. Kim:**
- "Add a fifth persona: 'Simple Sam' - just wants AC on in morning, off at night. No frills. This should be achievable in under 30 seconds."

**Action Items:**
- [ ] Add "Simple Sam" persona for ultra-basic use case
- [ ] Prioritize Dan + Wendy + Sam for MVP

---

### 3. User Scenarios

> ⚠️ **NEEDS MAJOR REVISION** - Too many steps

**Dr. Kim's Analysis of Scenario 1:**

Current flow (7 steps):
```
1. Open app → Navigate to "Schedule" tab
2. Tap "Create Schedule" (uses one of 3 available slots)  ← JARGON
3. Select days: Mon, Tue, Wed, Thu, Fri
4. Add event: 8:00 AM → Power OFF
5. Add event: 5:00 PM → Power ON, Cool, 72°F, Auto fan
6. Name schedule: "Work Week"
7. Save
```

**Problems identified:**
- Step 2 mentions "slots" - user doesn't care
- Steps 4-5 require understanding "events" concept
- 7 steps is too many for a simple schedule

**Proposed Simplified Flow (4 steps):**
```
1. Open Schedule tab → See "Set up your first schedule"
2. Choose template: "Work Schedule" / "Home All Day" / "Custom"
3. Adjust times with sliders: "Leave home: [8:00 AM]" "Return home: [5:00 PM]"
4. Tap "Save" → Done
```

**Maria's Wireframe Suggestion:**

```
┌─────────────────────────────────────────┐
│ ← Schedule Setup                        │
├─────────────────────────────────────────┤
│                                         │
│   What's your typical day?              │
│                                         │
│   ┌─────────────────────────────────┐   │
│   │  🏢  Work Schedule              │   │
│   │  Away during business hours     │   │
│   └─────────────────────────────────┘   │
│   ┌─────────────────────────────────┐   │
│   │  🏠  Home All Day               │   │
│   │  Comfort all day long           │   │
│   └─────────────────────────────────┘   │
│   ┌─────────────────────────────────┐   │
│   │  🌙  Night Owl                  │   │
│   │  Sleep cool, wake comfortable   │   │
│   └─────────────────────────────────┘   │
│   ┌─────────────────────────────────┐   │
│   │  ✏️  Custom                     │   │
│   │  Build your own schedule        │   │
│   └─────────────────────────────────┘   │
│                                         │
└─────────────────────────────────────────┘
```

**After selecting "Work Schedule":**

```
┌─────────────────────────────────────────┐
│ ← Work Schedule                  [Save] │
├─────────────────────────────────────────┤
│                                         │
│   When do you leave for work?           │
│   ┌─────────────────────────────────┐   │
│   │  ◀  8:00 AM  ▶                  │   │
│   └─────────────────────────────────┘   │
│   AC will turn OFF at this time         │
│                                         │
│   When do you get home?                 │
│   ┌─────────────────────────────────┐   │
│   │  ◀  5:30 PM  ▶                  │   │
│   └─────────────────────────────────┘   │
│   AC will turn ON 30 min before         │
│                                         │
│   ─────────────────────────────────     │
│   Which days?                           │
│   [Mon][Tue][Wed][Thu][Fri] Sat  Sun    │
│    ●    ●    ●    ●    ●    ○    ○      │
│                                         │
│   Comfort temperature                   │
│   ┌─────────────────────────────────┐   │
│   │      ❄️ ──────●────── 🔥        │   │
│   │            72°F                 │   │
│   └─────────────────────────────────┘   │
│                                         │
└─────────────────────────────────────────┘
```

**Action Items:**
- [ ] Replace "event-based" model with "template-based" model for MVP
- [ ] Create 3-4 pre-built templates that cover 80% of use cases
- [ ] Keep "Custom" option for advanced users (reveals event editor)
- [ ] Target: Simple schedule in < 30 seconds, < 5 taps

---

### 4. Proposed UI Layout

> ❌ **NEEDS COMPLETE REDESIGN** - Too complex

**Maria (Senior UX) - Critical Feedback:**

> "The current Schedule Tab wireframe shows THREE schedule cards plus a 'Today's Schedule' list plus an info banner. That's information overload."

**Problems with Current Design:**

| Issue | Problem |
|-------|---------|
| "Select Active Schedule" with 3 cards | Exposes "slot" concept users don't need |
| Radio buttons for schedule selection | Confusing - why can't I use multiple? |
| "5 events across Mon-Fri" | Jargon - what's an "event"? |
| Event count badges [2] | Technical detail, not user value |
| "Today's Schedule" list | Useful but takes too much space |

**Proposed Simplified Layout:**

```
┌─────────────────────────────────────────┐
│  SCHEDULE                    [ON ◯───●] │
├─────────────────────────────────────────┤
│                                         │
│  ┌─────────────────────────────────────┐│
│  │  📅 Work Week Schedule              ││
│  │  ─────────────────────────────────  ││
│  │  Mon-Fri                            ││
│  │  OFF at 8:00 AM · ON at 5:00 PM     ││
│  │                              [Edit] ││
│  └─────────────────────────────────────┘│
│                                         │
│  ┌─────────────────────────────────────┐│
│  │  🌴 Weekend                         ││
│  │  ─────────────────────────────────  ││
│  │  Sat-Sun                            ││
│  │  ON at 9:00 AM · OFF at 11:00 PM    ││
│  │                              [Edit] ││
│  └─────────────────────────────────────┘│
│                                         │
│  [+ Add Another Schedule]               │
│                                         │
├─────────────────────────────────────────┤
│  ⏰ NEXT UP                             │
│  Today 5:00 PM → ON, Cool 72°F          │
│                     in 2 hours 15 min   │
└─────────────────────────────────────────┘
```

**Key Changes:**
1. **No "Active Schedule" concept** - all enabled schedules run (weekday + weekend)
2. **Human-readable summaries** - "OFF at 8 AM · ON at 5 PM" not "2 events"
3. **Single "Next Up" line** - shows only the next scheduled change
4. **Simple Add button** - no mention of "slots" or limits

**Jordan (Product Owner):**
> "How do we handle the device's 3-slot limit invisibly?"

**Engineering Response:**
> "Behind the scenes, we merge user's 'schedules' into the device's weekly format. If user has Work Week (Mon-Fri) + Weekend (Sat-Sun), we write one device schedule with all 7 days. The 3-slot limit only becomes visible if they try to create overlapping schedules for the same day."

**Action Items:**
- [ ] Redesign main Schedule tab per simplified layout
- [ ] Abstract away "3 slots" limitation - handle merging in code
- [ ] Show "Next Up" countdown prominently
- [ ] Use human-readable time descriptions

---

### 5. Schedule Editor

> ⚠️ **NEEDS REVISION** - Event list too technical

**Alex (Junior UX):**

Current editor shows:
```
MONDAY EVENTS:
│ 1. 6:00 AM  ❄️ Cool 72°F  [Edit][X]│
│ 2. 8:00 AM  🔴 OFF        [Edit][X]│
│ [+ Add Event] (4 more available)   │
```

**Problems:**
- "Events" is jargon
- Numbered list feels database-y
- "(4 more available)" exposes limits

**Proposed: Visual Timeline**

```
┌─────────────────────────────────────────┐
│  MONDAY                                 │
│                                         │
│  12AM  3AM  6AM  9AM  12PM  3PM  6PM  9PM│
│  ├────┼────┼────┼────┼────┼────┼────┼───┤│
│       ░░░░░░░░░░▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓░░░░░░  │
│            OFF  │    COOL 72°F   │ OFF  │
│                 │                │      │
│             8:00 AM          5:00 PM    │
│                                         │
│  Tap timeline to add or adjust          │
└─────────────────────────────────────────┘
```

**Dr. Kim:**
> "A visual timeline is more intuitive but harder to implement well. For MVP, consider a simpler 'time block' approach."

**Simpler Alternative - Time Blocks:**

```
┌─────────────────────────────────────────┐
│  MONDAY                          [Copy] │
├─────────────────────────────────────────┤
│                                         │
│  ┌──────────────────────────────────┐   │
│  │ 🌅 Morning         8:00 AM       │   │
│  │    Turn OFF                      │   │
│  │                          [Edit]  │   │
│  └──────────────────────────────────┘   │
│              │                          │
│              ▼                          │
│  ┌──────────────────────────────────┐   │
│  │ 🌆 Evening         5:00 PM       │   │
│  │    Turn ON · Cool · 72°F         │   │
│  │                          [Edit]  │   │
│  └──────────────────────────────────┘   │
│                                         │
│  [+ Add Time Block]                     │
│                                         │
└─────────────────────────────────────────┘
```

**Action Items:**
- [ ] Replace "Events" terminology with "Time Blocks" or just remove the term
- [ ] Use friendly time-of-day labels (Morning, Afternoon, Evening, Night)
- [ ] Add visual flow indicators (arrows between blocks)
- [ ] Consider visual timeline for Phase 2

---

### 6. Event Editor Dialog

> ⚠️ **NEEDS REVISION** - Mode selection confusing

**Current Design:**
```
│ Mode:    [Cool      ▼]                  │
│          ❄️ Cool  🔥 Heat  💨 Fan  💧 Dry │
```

**Maria's Feedback:**
> "Why show both a dropdown AND icon buttons? Pick one. Also, most users just want Cool or Heat - Dry and Fan are edge cases."

**Dr. Kim:**
> "The 'Turn ON / Turn OFF' radio buttons are good. But when turning ON, users shouldn't have to think about 'mode' - they should think about comfort."

**Proposed Simplified Editor:**

```
┌─────────────────────────────────────────┐
│                                         │
│   What time?                            │
│   ┌─────────────────────────────────┐   │
│   │       5 : 00   PM               │   │
│   └─────────────────────────────────┘   │
│                                         │
│   What should happen?                   │
│                                         │
│   ┌─────────────────────────────────┐   │
│   │  ❄️  Make it cool               │   │
│   │      72°F  [-] ████████░░ [+]   │   │
│   └─────────────────────────────────┘   │
│                                         │
│   ┌─────────────────────────────────┐   │
│   │  🔥  Make it warm               │   │
│   │      70°F  [-] ███████░░░ [+]   │   │
│   └─────────────────────────────────┘   │
│                                         │
│   ┌─────────────────────────────────┐   │
│   │  ⏻  Turn off                    │   │
│   └─────────────────────────────────┘   │
│                                         │
│   ┌ Advanced ─────────────────────────  │
│   │ Mode: [Auto ▼]  Fan: [Auto ▼]       │
│   └─────────────────────────────────────│
│                                         │
│        [Cancel]            [Save]       │
│                                         │
└─────────────────────────────────────────┘
```

**Key Improvements:**
1. **Natural language** - "Make it cool" not "Mode: Cool"
2. **Temperature inline** - Adjust temp without extra dialog
3. **Three clear choices** - Cool, Warm, or Off
4. **Advanced collapsed** - Fan/Mode options hidden by default

**Action Items:**
- [ ] Redesign event editor with natural language
- [ ] Combine action + temperature into single selection
- [ ] Hide advanced options (fan speed, mode) in expander
- [ ] Use WinUI3 NumberBox with increment buttons for temperature

---

### 7. WinUI3 Compliance Review

> ⚠️ **NEEDS ATTENTION** - Several non-standard patterns

**Maria's WinUI3 Audit:**

| Current Proposal | WinUI3 Recommendation |
|------------------|----------------------|
| ASCII toggle `[ON ◯───●]` | Use `ToggleSwitch` control |
| Custom day selector circles | Use `CalendarView` or styled `ToggleButton` group |
| Emoji icons (❄️ 🔥) | Use `FontIcon` from Segoe Fluent Icons |
| Custom timeline | Use `ItemsRepeater` with custom template |
| ASCII wireframes | Design in Figma with WinUI3 components |

**Specific Control Recommendations:**

```
Schedule Enable: ToggleSwitch (Header="Schedule", OnContent="Active", OffContent="Paused")

Day Selector: SegmentedControl or ToggleButton in StackPanel
  - Use accent color for selected days
  - Ensure 44x44 minimum tap target (accessibility)

Schedule Cards: Standard Card pattern with Expander for details

Time Picker: TimePicker control (built-in)

Temperature: NumberBox with SpinButtons, or custom Slider

Next Up: InfoBar or custom TeachingTip style
```

**Action Items:**
- [ ] Map all custom controls to WinUI3 equivalents
- [ ] Create Figma mockups using WinUI3 component library
- [ ] Ensure all tap targets ≥ 44x44 px
- [ ] Use Segoe Fluent Icons, not emoji
- [ ] Follow Fluent Design spacing (4px grid)

---

### 8. Accessibility Review

> ⚠️ **NOT ADDRESSED** - Need accessibility section

**Dr. Kim:**
> "The V2 spec has no accessibility considerations. This is required for any shipping product."

**Required Additions:**

| Requirement | Implementation |
|-------------|----------------|
| Screen reader | All controls need `AutomationProperties.Name` |
| Keyboard nav | Full keyboard navigation for schedule editor |
| High contrast | Test all states in high contrast mode |
| Color independence | Don't rely on color alone (✓ Done / → Next) |
| Text scaling | Support 200% text scaling |
| Reduced motion | Respect `prefers-reduced-motion` setting |

**Action Items:**
- [ ] Add Accessibility section to spec
- [ ] Define screen reader announcements for key actions
- [ ] Ensure all interactive elements keyboard accessible
- [ ] Test with Narrator before shipping

---

### 9. Error Handling UX

> ⚠️ **NEEDS EXPANSION** - Happy path only

**Alex (Junior UX):**
> "The spec covers edge cases technically but doesn't show what users SEE when things go wrong."

**Required Error States:**

**1. Device Offline:**
```
┌─────────────────────────────────────────┐
│  ⚠️ Can't reach your AC                 │
│                                         │
│  Your schedule is saved on the device   │
│  and will continue running.             │
│                                         │
│  [Try Again]    [View Cached Schedule]  │
└─────────────────────────────────────────┘
```

**2. Save Failed:**
```
┌─────────────────────────────────────────┐
│  ❌ Couldn't save schedule              │
│                                         │
│  The AC didn't respond. Your changes    │
│  haven't been applied yet.              │
│                                         │
│  [Retry]    [Discard Changes]           │
└─────────────────────────────────────────┘
```

**3. Conflict (same day, overlapping schedules):**
```
┌─────────────────────────────────────────┐
│  ⚠️ Schedule Overlap                    │
│                                         │
│  "Work Week" and "Custom" both have     │
│  events on Monday. Only one can be      │
│  active per day.                        │
│                                         │
│  [Keep Work Week]  [Keep Custom]        │
└─────────────────────────────────────────┘
```

**Action Items:**
- [ ] Design error state for each failure mode
- [ ] Use WinUI3 `ContentDialog` for blocking errors
- [ ] Use `InfoBar` for non-blocking warnings
- [ ] Always provide a recovery action

---

## 📊 Final Assessment by Reviewer

| Reviewer | Verdict | Key Concern |
|----------|---------|-------------|
| Maria Chen (Senior UX) | ⚠️ Revise | UI too complex, needs template-based approach |
| Alex Rivera (Junior UX) | ⚠️ Revise | Terminology too technical for regular users |
| Dr. Sarah Kim (Consultant) | ⚠️ Revise | Exposes API model instead of user mental model |
| Jordan Park (Product) | ✅ Approve concept | Love device-native, need simpler UX |
| Engineering Lead | ✅ Feasible | Can implement abstraction layer |

---

## 📝 Consolidated Action Items

### Must-Have for MVP (Blocking)

| # | Action | Owner | Priority |
|---|--------|-------|----------|
| 1 | Redesign with template-based setup (Work/Home/Night Owl/Custom) | UX | P0 |
| 2 | Replace "events" with natural language (time blocks) | UX | P0 |
| 3 | Hide 3-slot limit - merge schedules in code | Eng | P0 |
| 4 | Simplify event editor (Cool/Warm/Off, inline temp) | UX | P0 |
| 5 | Map to WinUI3 standard controls | UX | P0 |
| 6 | Add accessibility section | UX | P0 |
| 7 | Design error states with recovery actions | UX | P0 |

### Should-Have for MVP (Important)

| # | Action | Owner | Priority |
|---|--------|-------|----------|
| 8 | "Next Up" countdown on main tab | UX/Eng | P1 |
| 9 | Copy schedule between days | Eng | P1 |
| 10 | Figma mockups with WinUI3 components | UX | P1 |

### Nice-to-Have (Phase 2)

| # | Action | Owner | Priority |
|---|--------|-------|----------|
| 11 | Visual timeline editor | UX/Eng | P2 |
| 12 | Schedule templates library | UX | P2 |
| 13 | Override tracking | Eng | P2 |

---

## 🗓️ Next Steps

1. **UX Lead (Maria):** Revise spec with template-based model - Due: Feb 3
2. **Engineering:** Prototype schedule merging logic - Due: Feb 3  
3. **Review Meeting #2:** February 5, 2026 - Review revised mockups
4. **Implementation Start:** February 10, 2026 (pending approval)

---

## 💡 Key Insight from Review

**Dr. Kim's Summary:**
> "The device has 3 schedule slots with 6 events per day. That's an *engineering fact*, not a *user experience*. 
>
> The user experience should be: 'Tell the app when you leave and when you come home. It handles the rest.'
>
> Every time you expose a device limitation to the user, you've failed at abstraction. Your job is to make the complex simple."

---

*Review conducted January 28, 2026. V2 spec requires revision before implementation.*
