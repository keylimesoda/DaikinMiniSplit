# Scheduling Feature - UX Spec Review Meeting Notes

**Date:** January 26, 2026  
**Duration:** 90 minutes  
**Document Reviewed:** [SCHEDULING_UX_SPEC.md](SCHEDULING_UX_SPEC.md)

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

### Overall Assessment: **APPROVED WITH REVISIONS**

The spec provides a solid foundation. The team approved moving forward to implementation with the revisions noted below.

---

## 💬 Feedback by Section

### 1. API Assessment (Maria - Senior UX)

> ✅ **APPROVED** - Clear and honest about constraints

**Comments:**
- "I appreciate the transparency about client-side limitations. This sets realistic expectations."
- "Consider adding a prominent warning in the UI that the app must remain running."

**Action Item:**
- [ ] Add persistent banner/tooltip on Schedule tab explaining background execution requirement

---

### 2. User Personas (Alex - Junior UX)

> ✅ **APPROVED** with additions

**Comments:**
- "Good coverage of primary use cases."
- "Missing persona: 'Tech-Savvy Tony' - power user who wants granular control, multiple conditions per event, API access."

**Action Item:**
- [ ] Add power user persona to cover advanced scheduling needs (Phase 2+)

---

### 3. User Scenarios (Dr. Kim - External Consultant)

> ⚠️ **NEEDS REVISION** - Scenario 6 complexity

**Comments:**
- "Scenarios 1-5 and 7 are well-crafted with clear success criteria."
- "Scenario 6 (Sensor-Based Smart Schedule) introduces significant complexity. Recommend moving to Phase 2 as designed, but provide a simpler 'comfort range' feature in MVP."
- "60-second creation time for Scenario 1 is ambitious. Recommend user testing to validate."

**Recommendations:**
1. MVP: Simple "comfort range" (min/max temp) rather than full conditional logic
2. Add scenario for "first-time setup wizard" experience
3. Include scenario for "schedule fails to execute" error recovery

**Action Items:**
- [ ] Add "First Run Experience" scenario with guided setup
- [ ] Add "Error Recovery" scenario for failed executions  
- [ ] Simplify sensor-based feature for MVP → "Comfort Guard" (temp limits only)

---

### 4. UI Layout (Maria - Senior UX)

> ⚠️ **NEEDS REVISION** - Timeline editor

**Comments:**

**Schedule List View:**
- "Clean and scannable. The 🟢/⚪ status indicators work well."
- "Add 'Next event in 2h 15m' countdown for context."
- "Quick Actions should be more prominent - consider making them sticky at bottom of tab."

**Timeline Editor:**
- "The vertical timeline is good but doesn't show duration well."
- "Consider a horizontal 24-hour bar view as alternative (A/B test both)."
- "Day selector circles are too small for touch - increase tap targets."

**Accessibility:**
- "Need keyboard navigation for timeline."
- "Color-only status indicators (🔴/❄️) need text labels for colorblind users."

**Action Items:**
- [ ] Add countdown to next event on schedule cards
- [ ] Design horizontal timeline alternative for user testing
- [ ] Increase day selector tap targets to 44px minimum
- [ ] Add text labels alongside color indicators
- [ ] Ensure full keyboard navigation support

---

### 5. Edge Cases & Error Handling (Alex - Junior UX)

> ✅ **APPROVED** with additions

**Comments:**
- "Good coverage of technical failures."
- "Add handling for: user deletes schedule while event is pending, timezone changes while traveling."

**Missing Cases Identified:**
| Situation | Proposed Handling |
|-----------|-------------------|
| Schedule deleted with pending event | Cancel pending, show confirmation |
| User travels to different timezone | Option: "Use home timezone" or "Use local timezone" |
| Duplicate event times in same schedule | Prevent save, highlight conflict |
| Device IP address changes | Re-discover device, prompt user if ambiguous |

**Action Item:**
- [ ] Add identified edge cases to spec

---

### 6. MVP vs. Future Features (Jordan - Product Owner)

> ⚠️ **NEEDS REVISION** - Prioritization

**Comments:**
- "MVP scope looks achievable for a 4-week sprint."
- "Move 'Vacation Mode' from Phase 2 to MVP - it's a key differentiator and Steve persona is high-value."
- "Windows Service (Phase 3) should be Phase 2 - the 'app must stay open' requirement will generate support tickets."

**Revised Prioritization:**

**MVP (Phase 1) - 4 weeks:**
- ✅ Create/edit/delete schedules
- ✅ Daily time-based events
- ✅ Day-of-week selection
- ✅ Manual override with auto-resume
- ✅ "I'm Leaving / I'm Home" quick actions
- ➕ **Vacation mode with date ranges** (moved from Phase 2)

**Phase 2 - 3 weeks:**
- Windows Service for background execution (moved from Phase 3)
- Duplicate/copy schedules
- Comfort Guard (simplified sensor-based)
- Schedule import/export

**Phase 3:**
- Full conditional schedules
- Multiple device support
- Smart home integration

**Action Item:**
- [ ] Update phase definitions per product owner direction

---

## ❓ Open Questions - Decisions Made

### Q1: Background Execution
**Decision:** MVP will require app running. Phase 2 adds Windows Service.
**Rationale:** Ship faster, gather feedback, prioritize service based on user complaints.

### Q2: Schedule Conflicts  
**Decision:** Most specific wins. "Weekend" beats "Work Week" for Sat/Sun.
**Rationale:** Intuitive behavior - user explicitly created weekend schedule.
**Implementation:** If same specificity, most recently modified wins. Show warning on save.

### Q3: Quick Actions Presets
**Decision:** User-configurable with smart defaults.
**Rationale:** Power users want control; new users need guidance.
**Defaults:** 
- "I'm Leaving" → Power OFF
- "I'm Home" → Power ON, last used mode/temp

### Q4: Notification Strategy
**Decision:** Opt-in notifications, minimal by default.
**Defaults:**
- ON: Failures only
- OFF: Successful events
- User can enable verbose notifications in settings

### Q5: IR Remote Sync
**Decision:** Schedule pauses if manual change detected (via sensor polling).
**UX:** Show "Schedule paused - manual override detected" with "Resume" button.
**Technical:** Poll device state every 60 seconds, compare to expected state.

---

## 🎯 Consolidated Action Items

| Priority | Item | Owner | Due |
|----------|------|-------|-----|
| 🔴 High | Update phase priorities per PO direction | Dev Lead | Jan 27 |
| 🔴 High | Add background execution warning to UI designs | Maria | Jan 28 |
| 🟡 Medium | Design horizontal timeline alternative | Maria | Jan 30 |
| 🟡 Medium | Add missing edge cases to spec | Alex | Jan 28 |
| 🟡 Medium | Create first-run setup flow wireframes | Alex | Jan 31 |
| 🟢 Low | Add power user persona | Alex | Feb 1 |
| 🟢 Low | Accessibility audit of proposed designs | Maria | Feb 3 |

---

## 📅 Next Steps

1. **Spec Revision:** Update SCHEDULING_UX_SPEC.md with all action items by Jan 28
2. **Design Sprint:** Maria & Alex to deliver high-fidelity mockups by Feb 3
3. **Tech Spike:** Engineering to prototype Windows Service feasibility by Feb 5
4. **Implementation Kickoff:** Feb 7 (assumes spec v1.1 approved)

---

## 📎 Attachments

- [Original Spec](SCHEDULING_UX_SPEC.md)
- Whiteboard photos: (to be uploaded)
- Figma link: (pending Maria's designs)

---

*Meeting notes prepared by: Development Team*  
*Last updated: January 26, 2026*
