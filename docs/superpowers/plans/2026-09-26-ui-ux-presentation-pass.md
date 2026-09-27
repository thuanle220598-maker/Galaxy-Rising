# P0.7 UI/UX Presentation Pass Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a coherent pixel sci-fi UI presentation pass across the existing portrait vertical slice.

**Architecture:** Extend `PrototypeGameFlow` with a small set of reusable native Unity UI styling helpers. Keep all gameplay callbacks and data sources unchanged, and lock the visible structure with the existing Play Mode regression suite.

**Tech Stack:** Unity 6000.3.25f1, C#, UnityEngine.UI, NUnit Play Mode tests

**Spec:** `docs/superpowers/specs/2026-09-26-ui-ux-presentation-pass-design.md`

## Global Constraints

- Reference resolution remains `360 x 640`, portrait `9:16`.
- Use native Unity UI only; add no package or imported production art.
- Do not change combat, progression, economy, save data, or navigation behavior.
- Preserve the dirty worktree and do not create a commit.
- Write code and tests first, then run Unity once at the end.

---

### Task 1: Lock the presentation hierarchy

**Files:**
- Modify: `Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs`

**Interfaces:**
- Consumes: runtime GameObjects created by `PrototypeGameFlow`.
- Produces: regression assertions for `UI Starfield`, `Resource Chip`, `Hero Card`, `Portrait`, `HP Meter`, `Energy Meter`, `Screen Frame`, and button color-tint feedback.

- [x] Add assertions to the existing Battle/Home test for the visual hierarchy and five hero cards.
- [x] Extend the overlay regression test to require the shared screen frame and an opaque CanvasGroup.
- [x] Keep the test reflection-only so the test assembly does not reference `Assembly-CSharp` types directly.

### Task 2: Add shared visual primitives

**Files:**
- Modify: `Assets/Scripts/PrototypeGameFlow.cs`

**Interfaces:**
- Produces: `AddScreenBackdrop`, `AddFramedPanel`, `AddMeter`, and enhanced `AddButton`/`AddText` behavior.
- Consumes: existing `AddImage`, `SetRect`, palette fields, and `LegacyRuntime.ttf`.

- [x] Add a deterministic code-generated UI starfield and layered screen chrome.
- [x] Add framed panels with an accent rail and outline.
- [x] Configure every button with native highlighted, pressed, selected, and disabled colors.
- [x] Add restrained text shadows for bold labels and headings.
- [x] Keep screen CanvasGroups fully opaque and interactive.

### Task 3: Upgrade Battle/Home readability

**Files:**
- Modify: `Assets/Scripts/PrototypeGameFlow.cs`

**Interfaces:**
- Consumes: `PrototypeSession` resources and current `PrototypeBattle.Allies` state.
- Produces: resource chip texts and arrays for hero portrait, selection, HP, and energy visuals.

- [x] Replace the single resource line with five named chips.
- [x] Build five hero cards using the combatant body sprite as the portrait.
- [x] Update HP and energy meter widths each frame and show selected-card state.
- [x] Apply framed styling to header, hero bar, action bar, navigation, activities, and results.

### Task 4: Unify secondary screens and verify

**Files:**
- Modify: `Assets/Scripts/PrototypeGameFlow.cs`
- Modify: `README.md`

**Interfaces:**
- Consumes: shared visual primitives from Task 2.
- Produces: consistent Squad, Heroes, and Summon presentation and P0.7 handoff notes.

- [x] Apply the shared backdrop and frame to Squad, Heroes, and Summon.
- [x] Preserve all existing buttons, touch targets, onboarding panels, and callbacks.
- [x] Update README to mark P0.7 complete and keep external playtest as the next step.
- [x] Copy changed scripts/tests into the imported Unity cache.
- [x] Run `BattleHomeUiPlayModeTests` once and require zero failures.
- [x] Run `git diff --check` and confirm no recovery files were created.
