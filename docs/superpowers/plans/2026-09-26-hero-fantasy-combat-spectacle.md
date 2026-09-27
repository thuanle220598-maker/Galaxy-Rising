# P0.8 Hero Fantasy & Combat Spectacle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a polished 64x64 combat-art and skill-depth vertical slice for the five starter heroes and boss Krag.

**Architecture:** Keep the existing battle clock and `PrototypeSpriteSet` API. Add one focused 64x64 code-native sprite generator, extend the existing animation state with `Ultimate`, and add only the small combat/VFX helpers required by the approved hero loops and two team synergies.

**Tech Stack:** Unity 6000.3.25f1, C#, URP 2D, UnityEngine sprite/audio APIs, NUnit Play Mode tests

**Spec:** `docs/superpowers/specs/2026-09-26-hero-fantasy-combat-spectacle-design.md`

## Global Constraints

- Reference resolution remains portrait `360 x 640`; validate `720 x 1280` and `1080 x 1920` at the end.
- Starter and boss art uses true `64 x 64` RGBA textures, `64` pixels per unit, point filtering and no mipmaps.
- Existing 32x32 art remains the fallback for all non-starter identities.
- Add no package, imported production-art pipeline, save field, currency, manual skill control or general effect graph.
- Preserve the current action duration/hit-frame clock and resolve gameplay exactly once per action.
- Preserve the dirty worktree; do not revert unrelated changes and do not create a git commit.
- Write all code and tests first, then run Unity once for the combined final verification.

---

### Task 1: Lock P0.8 behavior with reflection-based Play Mode checks

**Files:**
- Modify: `Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs`

**Interfaces:**
- Consumes: runtime `PrototypeCombatant`, `PrototypeSpriteSet`, `PrototypeHeroPixelArt64` and starter resource definitions.
- Produces: regression coverage for 64x64 starter art, Ultimate frames, Nova mark detonation, Ion burn-aware chaining, Astra shield reinforcement and farm-wave reset.

- [x] Add a test that loads each starter definition, calls `PrototypePixelArt.Create(definition, false)` by reflection and verifies every returned frame uses a `64 x 64` point-filtered texture.

```csharp
var create = pixelArtType.GetMethod(
    "Create",
    BindingFlags.Public | BindingFlags.Static,
    null,
    new[] { definitionType, typeof(bool) },
    null);
var spriteSet = create.Invoke(null, new[] { definition, (object)false });
var ultimate = (System.Array)spriteSet.GetType().GetField("Ultimate").GetValue(spriteSet);
Assert.That(ultimate.Length, Is.EqualTo(10));
```

- [x] Add a fallback assertion that a non-starter skill kit still returns `32 x 32` frames.
- [x] Add focused reflection tests around internal static calculation helpers rather than simulating full battles:

```csharp
Assert.That(InvokeStatic(combatantType, "NovaDetonationMultiplier", true), Is.EqualTo(1.35f));
Assert.That(InvokeStatic(combatantType, "NovaDetonationMultiplier", false), Is.EqualTo(1f));
Assert.That(InvokeStatic(combatantType, "ShieldReinforcement", 1000, 200, 100), Is.EqualTo(100));
```

- [x] Extend the existing scene test to assert starter body sprites are 64x64 and boss Krag owns the phase/aura presentation objects.
- [x] Do not run Unity yet.

### Task 2: Add bespoke 64x64 starter and boss frames

**Files:**
- Create: `Assets/Scripts/PrototypeHeroPixelArt64.cs`
- Create: `Assets/Scripts/PrototypeHeroPixelArt64.cs.meta`
- Modify: `Assets/Scripts/PrototypePixelArt.cs`

**Interfaces:**
- Produces: `PrototypeHeroPixelArt64.Supports(PrototypeSkillKit kit)` and `PrototypeHeroPixelArt64.Create(PrototypeSkillKit kit, Color bodyColor, bool boss)`.
- Extends: `PrototypeAnimationState.Ultimate` and `PrototypeSpriteSet.Ultimate`.
- Consumes: existing `PrototypeSpriteSet` without creating a second rendering model.

- [x] Add `Ultimate` to the state and sprite set:

```csharp
internal enum PrototypeAnimationState
{
    Idle, Run, Attack, Skill, Ultimate, Hit, Death
}

public readonly Sprite[] Ultimate;
```

- [x] Add the overload used by combat and tests:

```csharp
public static PrototypeSpriteSet Create(CombatantDefinition definition, bool boss)
{
    if (!definition.HasImportedSprites && PrototypeHeroPixelArt64.Supports(definition.SkillKit))
    {
        return PrototypeHeroPixelArt64.Create(definition.SkillKit, definition.BodyColor, boss);
    }

    return CreateLegacy(definition);
}
```

- [x] In the new generator, use constants `Size = 64` and `PixelsPerUnit = 64f`, a cache key containing kit/body color/boss, and frame counts `4/6/6/8/10/3/6`.
- [x] Implement shared pixel primitives locally (`Pixel`, `Rect`, `Line`, `Ring`, `Mix`) and five direct draw methods plus Krag boss decoration. Do not introduce an interface, factory or data-driven drawing language.
- [x] Give each draw method its approved shape language: Nova blade/reactor, Ion horns/lightning, Astra halo/fragments, Lyra bow/cape, Brakk shield/runes and boss Krag orbiting plates/gravity core.
- [x] Name textures `{kit}_64_{state}_{frame}` and `{kit}_Boss64_{state}_{frame}` so scene diagnostics identify the active path.
- [x] Preserve imported frame precedence and the legacy 32x32 fallback.

### Task 3: Wire Ultimate animation and boss phase presentation

**Files:**
- Modify: `Assets/Scripts/CombatPrototype.cs`

**Interfaces:**
- Consumes: `PrototypePixelArt.Create(CombatantDefinition definition, bool boss)` and `PrototypeSpriteSet.Ultimate`.
- Produces: `isUltimateAction`, boss phase objects, and correct animation selection without changing the gameplay clock.

- [x] Pass `IsBoss` into both sprite-generation call sites.
- [x] Track the current action type in `BeginAction` and clear it on reset/action completion:

```csharp
private bool isUltimateAction;

private void BeginAction(Action action, float duration, float hitDelay, bool skill, bool ultimate)
{
    pendingAction = action;
    actionLockRemaining = duration;
    actionHitRemaining = hitDelay;
    isUltimateAction = ultimate;
    // existing pulse and feedback logic remains
}
```

- [x] Select `Ultimate` before `Skill` in `UpdateCharacterAnimation`; reset the flag after the locked action ends.
- [x] Add `Boss Phase Aura` and `Boss Orbit` under boss Krag and switch their color/rotation cadence once at `CurrentHealth <= MaxHealth * 0.5f`.
- [x] Telegraph Krag's active and ultimate during anticipation by pulsing the gravity core; gameplay still executes only through `pendingAction`.

### Task 4: Implement the two approved synergy loops

**Files:**
- Modify: `Assets/Scripts/CombatPrototype.cs`
- Modify: `Assets/Resources/Skills/Nova.asset`
- Modify: `Assets/Resources/Skills/Ion.asset`
- Modify: `Assets/Resources/Skills/Astra.asset`
- Modify: `Assets/Resources/Skills/Lyra.asset`
- Modify: `Assets/Resources/Skills/Brakk.asset`

**Interfaces:**
- Produces: `HasBurn`, `HasShield`, `ConsumePhotonMark`, `ReinforceShield`, `NovaDetonationMultiplier`, and `ShieldReinforcement`.
- Consumes: existing mark, burn, shield and target-selection state.

- [x] Add read-only transient state and bounded calculation helpers:

```csharp
internal bool HasBurn => burnRemaining > 0f;
internal bool HasShield => currentShield > 0;

internal static float NovaDetonationMultiplier(bool marked) => marked ? 1.35f : 1f;

internal static int ShieldReinforcement(int maxHealth, int currentShield, int healAmount)
{
    var cap = Mathf.RoundToInt(maxHealth * 0.3f);
    return Mathf.Clamp(Mathf.RoundToInt(healAmount * 0.35f), 0, Mathf.Max(0, cap - currentShield));
}
```

- [x] Nova: detect Nova's own mark before Stellar Breaker, multiply primary/splash presentation, then consume that mark after damage.
- [x] Ion: replace nearest-only secondary targeting with burn-first selection and allow one additional `0.3f` bounce only when a distinct living burning target exists.
- [x] Astra: after each heal, reinforce an existing shield using `ShieldReinforcement`; never create a shield from this helper.
- [x] Lyra: preserve burn refresh behavior and add stronger impact presentation for already-burning Helios Rain targets without another permanent stat.
- [x] Brakk: retain a transient Guard Link target/duration and clear it on expiry, death and `ResetForFarmWave`.
- [x] Update the five skill descriptions to mention mark detonation, burn-aware chaining and shield reinforcement accurately.

### Task 5: Replace generic starter skill feedback with readable native VFX

**Files:**
- Modify: `Assets/Scripts/CombatPrototype.cs`

**Interfaces:**
- Produces: direct native helpers on `PrototypeEffect`: `SpawnImpact`, `SpawnLine`, `SpawnAttached`, and `SpawnBarrier`.
- Consumes: existing square sprite, source/target world positions, skill accents and transient durations.

- [x] Keep `PrototypeEffect.Spawn` as the fallback and add the minimum direct helpers needed by the five starters.
- [x] Draw chain/guard connections as rotated rectangles between source and target; update attached effects until their duration expires.
- [x] Use per-kit shapes and colors: Nova slash/ring, Ion zigzag chain, Astra rising stars/inward shield, Lyra projectile/rain/burn and Brakk square barrier/link.
- [x] Keep all VFX optional: null/dead targets return immediately and never block gameplay.
- [x] Add hero-specific procedural action motifs by allowing `PrototypeBattleFeedback.PlayAction` to receive `PrototypeSkillKit`; derive pitch from a small switch and reuse the existing procedural clips.
- [x] Keep shake pixel-snapped and restrained; do not globally pause `Time.timeScale`.

### Task 6: Update roadmap handoff and perform one final validation

**Files:**
- Modify: `README.md`
- Modify: `docs/superpowers/plans/2026-09-26-hero-fantasy-combat-spectacle.md`

**Interfaces:**
- Produces: a machine-to-machine handoff that marks P0.8 complete only after validation.

- [x] Add P0.8 to the README roadmap and next-session prompt, including 64x64 starter art, boss Krag, the two synergies and deferred seven-hero expansion.
- [x] Copy changed scripts, resources and tests into the clean imported Unity validation project used by the existing workflow.
- [x] Start Unity with `-force-glcore`, run the entire Play Mode suite and require zero failures.
- [x] Confirm the soak test completes 100 battle/farm transitions and the result XML reports every test passing.
- [ ] Inspect one starter battle and stage-five boss sequence at portrait `360 x 640`; confirm no Console exception, missing sprite or opaque-screen regression.
- [x] Run `git diff --check` and verify no `_Recovery` files were created.
- [x] Mark every completed checkbox in this plan only after the corresponding evidence exists.
