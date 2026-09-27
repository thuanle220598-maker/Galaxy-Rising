# P1.0 Combat Animation & Ultimate Spectacle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add timed ultimate impacts and battlefield depth to the existing 5v5 auto combat without a cut-in overlay.

**Architecture:** Keep gameplay in `PrototypeCombatant`, presentation UI in `PrototypeGameFlow`, and small sprite effects beside the existing effect behaviours. Use Unity coroutines and `SortingGroup`; add no dependency or general animation framework.

**Tech Stack:** Unity 6000.3.25f1, C#, UnityEngine.UI, URP 2D, NUnit Play Mode tests

**Spec:** `docs/superpowers/specs/2026-09-26-combat-animation-ultimate-spectacle-design.md`

## Global Constraints

- Keep 5v5, free arena movement and target-relative ranges unchanged.
- Ultimate presentation stays on the battlefield; no cut-in overlay is built.
- Manual pause is the only code path that sets `Time.timeScale` to zero.
- Do not add packages, save fields, Timeline, Spine or shader dependencies.
- Run Unity import and the complete Play Mode suite once after implementation.

---

### Task 1: Lock Presentation Behaviour

**Files:**
- Modify: `Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs`

**Interfaces:**
- Consumes: existing `CombatPrototype.Create`, `PrototypeGameFlow`, `PrototypeCombatant` reflection helpers.
- Produces: regression checks for no cut-in UI, timed sequences, shadows, sorting, ultimate-ready visual and battle controls.

- [x] Add a Play Mode test that creates the battle and verifies no `Ultimate Cut-In` exists while `Impact Flash`, `Body Shadow`, `Ultimate Ready`, `SortingGroup`, `SPEED x1` and `PAUSE` remain.
- [x] Add a test that invokes `RunTimedSequence(3, 0.11f, Action<int>)` and proves callbacks are separated by coroutine yields.
- [x] Add a Play Mode test that verifies the `ShowUltimate` API and cut-in hierarchy are absent.

### Task 2: Add Battlefield Depth and Timed Combat

**Files:**
- Modify: `Assets/Scripts/CombatPrototype.cs`

**Interfaces:**
- Produces: `RunTimedSequence(int, float, Action<int>)`, shadow/sorting setup, projectile and afterimage effects, timed Volt Rush/Rail Barrage/Helios Rain impacts, heal text and ultimate-ready pulse.

- [x] Add `SortingGroup`, child shadow and dynamic Y sorting to combatants.
- [x] Add a coroutine that yields before the first callback and between later callbacks.
- [x] Convert Volt Rush and multi-target barrage ultimates to timed sequences while preserving skill-level damage multipliers.
- [x] Spawn a projectile for ranged basic attacks using the configured hit delay.
- [x] Add bounded afterimages during movement/actions and green heal numbers.
- [x] Add and update the ultimate-ready marker.

### Task 3: Add Impact Presentation and Battle Controls

**Files:**
- Modify: `Assets/Scripts/PrototypeGameFlow.cs`
- Modify: `Assets/Scripts/CombatPrototype.cs`

**Interfaces:**
- Produces: `ShowImpact(Color)`, `SPEED x1/x2`, `PAUSE/RESUME`.

- [x] Build an impact flash over the battlefield HUD without a cut-in panel.
- [x] Animate the flash with `Time.unscaledDeltaTime`.
- [x] Trigger the flash at ultimate impact while keeping character animation in-world.
- [x] Add `44 x 44` speed and pause controls and reset `Time.timeScale` on battle lifecycle changes.

### Task 4: Document and Verify

**Files:**
- Modify: `README.md`

**Interfaces:**
- Consumes: completed P1.0 implementation and Play Mode result count.
- Produces: updated handoff state for the next machine/session.

- [x] Document P1.0 scope, constraints and implementation state.
- [x] Close Unity if necessary, run the complete Play Mode suite once, inspect zero failures and delete generated logs/results.
- [x] Reopen Unity for manual visual testing.
