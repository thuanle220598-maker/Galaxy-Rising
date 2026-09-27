# Fire God VFX Animation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. This project is being executed inline because the active feature branch contains the current uncommitted Unity asset state.

**Goal:** Build and integrate a complete pixel-art VFX set for Fire God Heavenly Demon's Ash, Basic, Active, Ultimate and passive mechanics.

**Architecture:** Generate deterministic transparent sprite sheets from the existing Fire God palette, import them through one Unity postprocessor, and play them through one lightweight runtime sprite animator. Combat mechanics and hit timing remain unchanged.

**Tech Stack:** Python 3/OpenCV, Pixelorama 1.2, FFmpeg 9, Unity 6.3, C#.

**Spec:** `docs/superpowers/specs/2026-09-27-fire-god-vfx-animation-design.md`

## Global Constraints

- Preserve free movement, target-relative range and current combat timing.
- Do not add Ultimate Cut-In, global hit-stop, global action pause, lanes or kiting.
- Use point-filtered transparent pixel art and the existing Fire God palette.
- Run Unity import/testing only after the asset and code pass is complete.

---

### Task 1: Asset Validation and Basic/Ash Pack

**Files:**
- Create: `Tools/Test-FireGodVfx.py`
- Create: `Tools/Generate-FireGodVfx.py`
- Create: `Assets/Resources/VFX/FireGod/*.png`

**Interfaces:**
- Produces named horizontal sprite sheets consumed by the Unity importer.

- [x] Write a failing asset test for exact frame counts, dimensions, alpha and stable centers.
- [x] Run `python Tools/Test-FireGodVfx.py` and confirm missing assets fail.
- [x] Generate projectile, impact, Ash 1/2/3, warning and detonation sheets.
- [x] Run the test and render an FFmpeg preview contact sheet.

### Task 2: Active Skill Pack

**Files:**
- Modify: `Tools/Generate-FireGodVfx.py`
- Modify: `Tools/Test-FireGodVfx.py`
- Create: `Assets/Resources/VFX/FireGod/FireGodHellfireImpact.png`
- Create: `Assets/Resources/VFX/FireGod/FireGodMagmaLoop.png`
- Create: `Assets/Resources/VFX/FireGod/FireGodMagmaBurst.png`

**Interfaces:**
- Produces Active-skill one-shots and a seamless Magma loop.

- [x] Add failing expected-sheet entries.
- [x] Generate anticipation, impact, debris, magma and Ash-triggered eruption frames.
- [x] Verify asset checks and preview timing.

### Task 3: Ultimate and Passive Pack

**Files:**
- Modify: `Tools/Generate-FireGodVfx.py`
- Modify: `Tools/Test-FireGodVfx.py`
- Create: `Assets/Resources/VFX/FireGod/FireGodCrimsonGale.png`
- Create: `Assets/Resources/VFX/FireGod/FireGodScorchLoop.png`
- Create: `Assets/Resources/VFX/FireGod/FireGodFirestormConvert.png`
- Create: `Assets/Resources/VFX/FireGod/FireGodFirestormLoop.png`
- Create: remaining shield, Heat and ember sheets under the same folder.

**Interfaces:**
- Produces Ultimate, Fire-zone conversion and passive VFX sheets.

- [x] Add failing expected-sheet entries.
- [x] Generate fan wave, scorched ground, Firestorm, shield and ember animations.
- [x] Verify asset checks and preview timing.

### Task 4: Unity Import and Runtime Playback

**Files:**
- Create: `Assets/Editor/FireGodVfxPostprocessor.cs`
- Create: `Assets/Scripts/PrototypeFireVfx.cs`
- Modify: `Assets/Scripts/CombatPrototype.cs`
- Modify: `Assets/Scripts/PrototypeFireCombat.cs`
- Test: `Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs`

**Interfaces:**
- Produces `PrototypeFireVfx.Spawn(name, position, direction, duration, loop)`.
- Consumes sheets from `Resources/VFX/FireGod`.

- [x] Add Play Mode coverage for loading and animation lifecycle.
- [x] Add point-filtered deterministic sprite slicing.
- [x] Add the minimal sprite animator and replace generic Fire God effect calls.
- [x] Run the Play Mode tests once after completing code and assets.

### Task 5: Final Verification

**Files:**
- Modify: `README.md`

- [x] Run the asset validator.
- [x] Run all Play Mode tests once.
- [ ] Inspect SampleScene at speed x1 and x2.
- [x] Record final paths, frame counts and test evidence in README.
