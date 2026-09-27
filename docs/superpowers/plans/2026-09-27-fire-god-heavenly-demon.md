# Fire God Heavenly Demon Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace Nova with Fire God Heavenly Demon, import the full supplied PXO animation set, and implement the complete Ash/Heat/Fire-zone combo kit without regressing the existing smooth free-movement combat.

**Architecture:** Keep `PrototypeCombatant` as the owner of hero state and actions. Add one focused runtime file for typed damage geometry and persistent fire zones, extend the existing direct status fields instead of introducing a generic status framework, and reuse the current procedural VFX/audio helpers. Generate PNG sheets outside Unity, configure them through one editor postprocessor, then let the existing content generator assign the imported sprites and canonical data.

**Tech Stack:** Unity `6000.3.25f1`, C#, URP 2D, NUnit Play Mode tests, PowerShell/.NET ZIP and bitmap APIs, Unity `TextureImporter`.

**Spec:** `docs/superpowers/specs/2026-09-27-fire-god-heavenly-demon-redesign.md`

## Global Constraints

- Portrait target remains `360 x 640`; combat remains free movement with target-relative attack range.
- Display name is exactly `Fire God Heavenly Demon`; internal `PrototypeSkillKit.Nova`, resource filenames, GUIDs, and base stats remain compatible.
- Species is `Human`, class is `Mage`, rarity remains `SSR`, and Mage attack range remains `4.0`.
- Keep the existing eight-frame `NovaFlameBasicAttack` Basic Attack.
- Import PXO frames at `68 x 68`, `68 PPU`, Point filtering, no mipmaps, no compression.
- Do not add a package, Animator Controller, Timeline, cut-in, center line, fixed lane, ranged kiting, battle portrait roster, global hit-stop, or global action pause.
- Three passive descriptions are separate data entries but share the existing single Passive upgrade level and save field.
- Existing heroes must continue working with empty Passive 2 and Passive 3 values.
- Preserve dirty-worktree changes. Before every commit, inspect the exact staged diff; skip that checkpoint commit if it would include unrelated user changes.
- Write tests before the production change for each task, but do not launch Unity between tasks. Generate all code and assets first, then perform one final Unity import and full Play Mode run. Rerun only if that final run exposes failures.

## File Map

- Create `Assets/Scripts/PrototypeFireCombat.cs`: damage enums/results, pure circle/cone geometry, and the lightweight persistent fire-zone component.
- Create `Assets/Editor/FireGodSpriteSheetPostprocessor.cs`: deterministic slicing/import settings for the generated Fire God sheets.
- Create `Tools/Export-FireGodPxo.ps1`: reproducible PXO-to-PNG extraction without opening Unity or requiring Pixelorama.
- Modify `Assets/Scripts/SkillDefinition.cs`: optional Passive 2/3 data and null-safe summaries.
- Modify `Assets/Scripts/CombatantDefinition.cs`: canonical character-name constants and editor-only animation-array assignment.
- Modify `Assets/Scripts/CombatPrototype.cs`: name migration ownership for squads, Fire God runtime state/skills, action safety, status presentation, and reusable effects.
- Modify `Assets/Scripts/PrototypeProgression.cs`: progression-name migration and deterministic merge.
- Modify `Assets/Scripts/PrototypeGacha.cs`: starter-name update and owned-name migration.
- Modify `Assets/Scripts/PrototypeSaveSystem.cs`: version 4 migration orchestration.
- Modify `Assets/Editor/PrototypeContentGenerator.cs`: canonical Fire God data and sprite assignment.
- Modify `Assets/Resources/Combatants/Allies/Nova.asset`: generated identity/class/frame references.
- Modify `Assets/Resources/Skills/Nova.asset`: generated full skill text/timing/data.
- Create generated sheets under `Assets/Art/Characters/Nova/Combat/` and retain the PXO source under `Assets/Art/Characters/Nova/Source/Nova.pxo`.
- Modify `Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs`: focused regression tests for every new primitive and combo.
- Modify `README.md`: current identity, mechanics, source-art workflow, migration, and final verified test result.

---

### Task 1: Extend Skill Data And Add Typed Damage

**Files:**
- Create: `Assets/Scripts/PrototypeFireCombat.cs`
- Modify: `Assets/Scripts/SkillDefinition.cs`
- Test: `Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs`

**Interfaces:**
- Produces: `PrototypeDamageType`, `PrototypeDamageFlags`, `PrototypeDamageResult`, `PrototypeFireCombat.CalculateDamage(...)`.
- Produces: `SkillDefinition.Passive2`, `SkillDefinition.Passive3`, and a seven-argument editor configuration with optional trailing passives.
- Consumes: existing `PrototypeSkillData` and current attack-minus-defense behavior.

- [ ] **Step 1: Write failing typed-damage and passive-schema tests**

Add tests that use reflection because runtime types are internal:

```csharp
[Test]
public void TypedDamageAppliesDefenseResistanceAndTrueDamage()
{
    var fireCombat = System.Type.GetType("PrototypeFireCombat, Assembly-CSharp");
    var damageType = System.Type.GetType("PrototypeDamageType, Assembly-CSharp");

    Assert.That(InvokeStatic(fireCombat, "CalculateDamage", 100, 20, 1f,
        System.Enum.Parse(damageType, "Physical"), 0f, 0f), Is.EqualTo(80));
    Assert.That(InvokeStatic(fireCombat, "CalculateDamage", 100, 20, 1f,
        System.Enum.Parse(damageType, "Fire"), -0.15f, 0f), Is.EqualTo(92));
    Assert.That(InvokeStatic(fireCombat, "CalculateDamage", 100, 999, 1f,
        System.Enum.Parse(damageType, "True"), 0.9f, 0f), Is.EqualTo(100));
    Assert.That(InvokeStatic(fireCombat, "CalculateDamage", 100, 0, 1f,
        System.Enum.Parse(damageType, "Fire"), -5f, 0f), Is.EqualTo(200));
    Assert.That(InvokeStatic(fireCombat, "CalculateDamage", 100, 0, 1f,
        System.Enum.Parse(damageType, "Fire"), 5f, 0f), Is.EqualTo(10));
}

[Test]
public void SkillDefinitionSupportsThreeOptionalPassives()
{
    var nova = Resources.Load<SkillDefinition>("Skills/Nova");
    Assert.That(nova.Passive, Is.Not.Null);
    Assert.That(nova.Passive2, Is.Not.Null);
    Assert.That(nova.Passive3, Is.Not.Null);
    Assert.That(nova.Summary, Does.Contain("Ignition Core"));
    Assert.That(nova.Summary, Does.Contain("Thermal Resonance"));
    Assert.That(nova.Summary, Does.Contain("Everburning Embers"));

    var ion = Resources.Load<SkillDefinition>("Skills/Ion");
    Assert.That(ion.Passive2, Is.Null);
    Assert.That(ion.Passive3, Is.Null);
}
```

- [ ] **Step 2: Record deferred red-state expectation without launching Unity**

Expected on the final first compile before implementation: missing `PrototypeFireCombat`, `PrototypeDamageType`, `Passive2`, and `Passive3`. Do not open Unity yet.

- [ ] **Step 3: Implement the minimal shared damage types and formula**

Create `PrototypeFireCombat.cs` with these exact public surfaces:

```csharp
using System;
using UnityEngine;

internal enum PrototypeDamageType { Physical, Fire, True }

[Flags]
internal enum PrototypeDamageFlags
{
    None = 0,
    Direct = 1,
    DamageOverTime = 2,
    AshDetonation = 4
}

internal readonly struct PrototypeDamageResult
{
    public readonly int AppliedDamage;
    public readonly bool Killed;
    public readonly bool TargetWasBurning;

    public PrototypeDamageResult(int appliedDamage, bool killed, bool targetWasBurning)
    {
        AppliedDamage = appliedDamage;
        Killed = killed;
        TargetWasBurning = targetWasBurning;
    }
}

internal static class PrototypeFireCombat
{
    internal static int CalculateDamage(
        int attack,
        int defense,
        float multiplier,
        PrototypeDamageType damageType,
        float fireResistance,
        float defenseIgnore = 0f)
    {
        var raw = Mathf.RoundToInt(attack * multiplier);
        if (damageType == PrototypeDamageType.True)
        {
            return Mathf.Max(1, raw);
        }

        var effectiveDefense = Mathf.RoundToInt(defense * (1f - Mathf.Clamp01(defenseIgnore)));
        var baseDamage = Mathf.Max(1, raw - effectiveDefense);
        return damageType == PrototypeDamageType.Fire
            ? Mathf.Max(1, Mathf.RoundToInt(baseDamage * (1f - Mathf.Clamp(fireResistance, -1f, 0.9f))))
            : baseDamage;
    }
}
```

- [ ] **Step 4: Add optional passive fields without changing existing callers**

In `SkillDefinition.cs` add serialized `passive2` and `passive3`, expose properties, and build the summary from non-null rows:

```csharp
[SerializeField] private PrototypeSkillData passive2;
[SerializeField] private PrototypeSkillData passive3;

public PrototypeSkillData Passive2 => passive2;
public PrototypeSkillData Passive3 => passive3;

public string Summary
{
    get
    {
        var rows = new List<string>
        {
            $"Basic - {basic.DisplayName}: {basic.Description}",
            $"Passive 1 - {passive.DisplayName}: {passive.Description}"
        };
        if (passive2 != null) rows.Add($"Passive 2 - {passive2.DisplayName}: {passive2.Description}");
        if (passive3 != null) rows.Add($"Passive 3 - {passive3.DisplayName}: {passive3.Description}");
        rows.Add($"Active - {active.DisplayName}: {active.Description}");
        rows.Add($"Ultimate - {ultimate.DisplayName}: {ultimate.Description}");
        return string.Join("\n", rows);
    }
}
```

Extend `EditorConfigure(...)` with `PrototypeSkillData passiveSkill2 = null, PrototypeSkillData passiveSkill3 = null` after the existing Ultimate argument and assign both fields. Existing generator calls remain source-compatible.

- [ ] **Step 5: Inspect changes and make a checkpoint commit if safe**

```powershell
git diff -- Assets/Scripts/PrototypeFireCombat.cs Assets/Scripts/SkillDefinition.cs Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs
git add -- Assets/Scripts/PrototypeFireCombat.cs Assets/Scripts/PrototypeFireCombat.cs.meta Assets/Scripts/SkillDefinition.cs Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs
git diff --cached --check
git commit -m "feat: add fire damage and passive skill data"
```

If Unity has not generated the new `.meta` yet, omit it from this checkpoint and include it after the final import.

### Task 2: Add Ash, Heat, And Tracked Burn State

**Files:**
- Modify: `Assets/Scripts/CombatPrototype.cs:1172-2750`
- Test: `Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs`

**Interfaces:**
- Consumes: `PrototypeDamageType`, `PrototypeDamageFlags`, `PrototypeDamageResult`, `PrototypeFireCombat.CalculateDamage(...)`.
- Produces: `ApplyAsh(source, stacks)`, `AshStacksFrom(source)`, `ConsumeAsh(source)`, `ApplyBurn(...)`, `ApplyBurnTotal(...)`, `HeatStacks`, and typed `DealDamage(...)`.
- Produces for Task 4: `ApplyMovementSlow`, `ApplyDecayingSlow`, and Fire DoT kill callbacks.

- [ ] **Step 1: Write failing pure/state tests**

Add these reusable reflection helpers beside the test class's existing `InvokeStatic` helper:

```csharp
private static object InvokeInstance(object instance, string method, params object[] arguments)
{
    return instance.GetType().GetMethod(
        method,
        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
        .Invoke(instance, arguments);
}

private static T GetField<T>(object instance, string field)
{
    return (T)instance.GetType().GetField(
        field,
        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
        .GetValue(instance);
}

private static void SetField(object instance, string field, object value)
{
    instance.GetType().GetField(
        field,
        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
        .SetValue(instance, value);
}
```

Add reflection-driven tests around one spawned ally and enemy:

```csharp
[UnityTest]
public IEnumerator AshHeatAndBurnStateIsBoundedAndSourceOwned()
{
    SceneManager.LoadScene("SampleScene");
    yield return null;
    var combatPrototype = System.Type.GetType("CombatPrototype, Assembly-CSharp");
    var combatantType = System.Type.GetType("PrototypeCombatant, Assembly-CSharp");
    var battleType = System.Type.GetType("PrototypeBattle, Assembly-CSharp");
    combatPrototype.GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
    yield return null;

    var fireGod = GameObject.Find("Fire God Heavenly Demon").GetComponent(combatantType);
    var battle = Object.FindFirstObjectByType(battleType);
    var enemies = (System.Array)battleType.GetProperty("Enemies").GetValue(battle);
    var enemy = enemies.GetValue(0);

    InvokeInstance(enemy, "ApplyAsh", fireGod, 2);
    InvokeInstance(enemy, "ApplyAsh", fireGod, 2);
    Assert.That((int)InvokeInstance(enemy, "AshStacksFrom", fireGod), Is.EqualTo(3));
    Assert.That(GetInstanceProperty<float>(enemy, "FireResistance"), Is.EqualTo(-0.15f).Within(0.001f));
    Assert.That((int)InvokeInstance(enemy, "ConsumeAsh", fireGod), Is.EqualTo(3));
    Assert.That((int)InvokeInstance(enemy, "AshStacksFrom", fireGod), Is.Zero);

    InvokeInstance(fireGod, "AddHeat", 9);
    Assert.That(GetInstanceProperty<int>(fireGod, "HeatStacks"), Is.EqualTo(5));
    Assert.That(GetInstanceProperty<float>(fireGod, "FireDamageMultiplier"), Is.EqualTo(1.25f).Within(0.001f));
    Assert.That(GetInstanceProperty<float>(fireGod, "MovementSpeedMultiplier"), Is.EqualTo(1.20f).Within(0.001f));
}

[Test]
public void RemainingDotDamageUsesFutureTicksOnly()
{
    var fireCombat = System.Type.GetType("PrototypeFireCombat, Assembly-CSharp");
    Assert.That(InvokeStatic(fireCombat, "RemainingDotDamage", 10, 2.1f, 1f), Is.EqualTo(30));
    Assert.That(InvokeStatic(fireCombat, "RemainingDotDamage", 10, 0f, 1f), Is.Zero);
}
```

Add the pure helper to Task 1's class during this task:

```csharp
internal static int RemainingDotDamage(int tickDamage, float remaining, float tickInterval)
{
    return tickDamage <= 0 || remaining <= 0f
        ? 0
        : tickDamage * Mathf.CeilToInt(remaining / Mathf.Max(0.01f, tickInterval));
}
```

- [ ] **Step 2: Record deferred red-state expectation**

Expected: missing Ash, Heat, Fire Resistance, tracked Burn, and DoT helpers. Do not launch Unity.

- [ ] **Step 3: Replace Photon Mark fields with source-owned Ash state**

Inside `PrototypeCombatant`, add a small nested value type and dictionary:

```csharp
private struct AshState
{
    public int stacks;
    public float remaining;
}

private readonly Dictionary<PrototypeCombatant, AshState> ashBySource =
    new Dictionary<PrototypeCombatant, AshState>();

internal int AshStacksFrom(PrototypeCombatant source) =>
    source != null && ashBySource.TryGetValue(source, out var state) ? state.stacks : 0;

internal int ConsumeAsh(PrototypeCombatant source)
{
    var stacks = AshStacksFrom(source);
    if (stacks > 0) ashBySource.Remove(source);
    return stacks;
}
```

`ApplyAsh(source, amount)` clamps to three and resets `remaining` to `5f`. `UpdateStatuses()` copies keys to a reusable list, decrements timers, and removes expired/dead-source entries without mutating the dictionary during enumeration. `FireResistance` returns `-0.05f` times the sum of active stacks.

- [ ] **Step 4: Add Heat and typed damage routing**

Add fields/properties:

```csharp
private int heatStacks;
public int HeatStacks => heatStacks;
public float FireDamageMultiplier => 1f + heatStacks * 0.05f;
public float MovementSpeedMultiplier => 1f + heatStacks * 0.04f;
```

Expose `public float FireResistance` on the combatant. Replace the existing `DealDamage` return type with `PrototypeDamageResult` and default existing callers to Physical:

```csharp
private PrototypeDamageResult DealDamage(
    PrototypeCombatant target,
    float multiplier,
    PrototypeDamageType damageType = PrototypeDamageType.Physical,
    PrototypeDamageFlags flags = PrototypeDamageFlags.Direct,
    float defenseIgnore = 0f)
```

Capture `target.HasBurn` before damage. For Fire God Fire damage, multiply by `FireDamageMultiplier`, calculate against `target.FireResistance`, and after a positive hit call `AddHeat(1)` plus `GainEnergy(2f)` when the target was already Burning or `flags` contains `AshDetonation`. Keep Vex kill behavior and existing impact VFX in this single path. Change existing callers that used the old Boolean return to read `.Killed`.

- [ ] **Step 5: Upgrade Burn in place**

Keep one Burn per target with these fields:

```csharp
private PrototypeCombatant burnSource;
private PrototypeDamageType burnDamageType;
private int burnTickDamage;
private float burnTickInterval;
private float burnRemaining;
private float burnTickCooldown;
private float fireDotMarkerRemaining;
```

Use these exact entry points:

```csharp
private void ApplyBurn(PrototypeCombatant source, int tickDamage, float duration, float tickInterval = 1f)
private void ApplyBurnTotal(PrototypeCombatant source, int totalDamage, float duration, float tickInterval)
```

`ApplyBurnTotal` derives `tickDamage = Mathf.Max(1, Mathf.CeilToInt(totalDamage / tickCount))`. Reapplication keeps whichever candidate has the larger remaining total and refreshes duration/tick timing. On a killing Fire DoT tick, call `source.OnFireDotKill(this, remainingDamage, remainingDuration, tickInterval)` before clearing the victim state.

`HasBurn` returns true for either a tracked Burn or `fireDotMarkerRemaining > 0f`. Persistent zones refresh the marker for `0.6s` after their damage lands so the first zone tick establishes Burning and later ticks can trigger Thermal Resonance without creating a second ticking DoT.

- [ ] **Step 6: Reset all new state with the existing wave reset**

In `ResetForFarmWave()` and death cleanup, clear the Ash dictionary, Heat, Burn fields, and Fire visuals. Do not leave static collections or scene-owned state behind.

- [ ] **Step 7: Inspect and checkpoint commit if safe**

```powershell
git diff -- Assets/Scripts/CombatPrototype.cs Assets/Scripts/PrototypeFireCombat.cs Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs
git add -- Assets/Scripts/CombatPrototype.cs Assets/Scripts/PrototypeFireCombat.cs Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs
git diff --cached --check
git commit -m "feat: add ash heat and tracked fire damage"
```

### Task 3: Add Movement Control And Protect Pending Actions

**Files:**
- Modify: `Assets/Scripts/CombatPrototype.cs:1473-1869, 2299-2335, 2658-2945`
- Modify: `Assets/Scripts/PrototypeFireCombat.cs`
- Test: `Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs`

**Interfaces:**
- Produces: `ApplyMovementSlow(percent, duration)`, `ApplyDecayingSlow(percent, duration)`, `ApplyKnockback(direction, distance)`, `ApplyKnockUp(duration)`, `ApplyGrounded(duration)`, `TryVoluntaryDisplacement(destination)`.
- Consumes: existing `ClampToArena`, animation/body references, and action timer.

- [ ] **Step 1: Write failing movement and action-safety tests**

```csharp
[Test]
public void DecayingSlowReachesZeroAndKnockbackClampsToArena()
{
    var fireCombat = System.Type.GetType("PrototypeFireCombat, Assembly-CSharp");
    Assert.That(InvokeStatic(fireCombat, "DecayingSlow", 0.7f, 2f, 0f), Is.EqualTo(0.7f));
    Assert.That(InvokeStatic(fireCombat, "DecayingSlow", 0.7f, 2f, 1f), Is.EqualTo(0.35f).Within(0.001f));
    Assert.That(InvokeStatic(fireCombat, "DecayingSlow", 0.7f, 2f, 2f), Is.Zero);
}

[UnityTest]
public IEnumerator PendingHitCannotBeReplacedByANewAction()
{
    SceneManager.LoadScene("SampleScene");
    yield return null;
    var combatPrototype = System.Type.GetType("CombatPrototype, Assembly-CSharp");
    var combatantType = System.Type.GetType("PrototypeCombatant, Assembly-CSharp");
    combatPrototype.GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
    yield return null;
    var fireGod = GameObject.Find("Fire God Heavenly Demon").GetComponent(combatantType);
    var first = new System.Action(() => { });
    SetField(fireGod, "pendingAction", first);
    SetField(fireGod, "attackCooldown", -1f);
    var before = GetField<System.Action>(fireGod, "pendingAction");
    InvokeInstance(fireGod, "Update");
    Assert.That(GetField<System.Action>(fireGod, "pendingAction"), Is.SameAs(before));
}
```

Add tests that knock-up leaves the root unchanged, Grounded blocks only `TryVoluntaryDisplacement`, and forced knockback still works while Grounded.

- [ ] **Step 2: Record deferred red-state expectation**

Expected: missing slow/displacement functions and current action selection can overwrite `pendingAction`. Do not launch Unity.

- [ ] **Step 3: Separate movement slow from attack slow**

Retain the current attack-speed slow for existing skills and add dedicated movement fields:

```csharp
private float movementSlowPercent;
private float movementSlowRemaining;
private float decayingSlowStart;
private float decayingSlowDuration;
private float decayingSlowElapsed;

internal static float DecayingSlow(float start, float duration, float elapsed) =>
    Mathf.Lerp(Mathf.Clamp01(start), 0f, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration)));
```

Movement uses `MoveSpeed * MovementSpeedMultiplier * (1f - CurrentMovementSlow) * Time.deltaTime`. The stronger current slow wins; ordinary slow refreshes, decaying slow tracks its own elapsed duration.

- [ ] **Step 4: Add local displacement state**

Implement knockback as a target root interpolation toward `ClampToArena(current + normalizedDirection * distance)` over `0.18s`. Implement knock-up as a local body Y offset using a sine arc over the requested duration; the root position does not change. `airborneRemaining > 0f` prevents movement and new actions and explicitly clears the target's pending cast when knock-up begins.

Grounded uses one timer and one guard:

```csharp
private bool TryVoluntaryDisplacement(Vector3 destination)
{
    if (groundedRemaining > 0f) return false;
    transform.position = ClampToArena(destination);
    return true;
}
```

Enemy-forced knockback never calls this guard.

- [ ] **Step 5: Guard action selection, not the whole update loop**

Keep status updates, targeting, facing, and movement active. Wrap only the action selection block:

```csharp
if (pendingAction == null && airborneRemaining <= 0f)
{
    if (SkillKit != PrototypeSkillKit.None && energy >= MaxEnergy) StartUltimate();
    else if (SkillKit != PrototypeSkillKit.None && activeSkillCooldown <= 0f) StartActiveSkill();
    else if (attackCooldown <= 0f) StartBasicAttack();
}
```

This prevents callback overwrite without recreating the removed skill stutter.

- [ ] **Step 6: Inspect and checkpoint commit if safe**

```powershell
git diff -- Assets/Scripts/CombatPrototype.cs Assets/Scripts/PrototypeFireCombat.cs Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs
git add -- Assets/Scripts/CombatPrototype.cs Assets/Scripts/PrototypeFireCombat.cs Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs
git diff --cached --check
git commit -m "feat: add combat displacement and action safety"
```

### Task 4: Implement Persistent Circle And Cone Fire Zones

**Files:**
- Modify: `Assets/Scripts/PrototypeFireCombat.cs`
- Modify: `Assets/Scripts/CombatPrototype.cs` for internal zone callbacks/effects
- Test: `Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs`

**Interfaces:**
- Produces: `PrototypeFireZone.SpawnMagma(...)`, `PrototypeFireZone.SpawnScorch(...)`, `ConvertToFirestorm()`, `DestroyOwnedBy(owner)`, `ContainsPoint(...)`, `CircleIntersectsCone(...)`.
- Consumes: `PrototypeBattle.GetOpponents`, Fire God tick callbacks, existing square sprite and sorting conventions.

- [ ] **Step 1: Write failing geometry and lifetime tests**

```csharp
[Test]
public void FireZoneGeometryMatchesCircleAndDirectionalCone()
{
    var fireCombat = System.Type.GetType("PrototypeFireCombat, Assembly-CSharp");
    Assert.That(InvokeStatic(fireCombat, "PointInCircle",
        new Vector3(1.7f, 0f), Vector3.zero, 1.8f), Is.True);
    Assert.That(InvokeStatic(fireCombat, "PointInCircle",
        new Vector3(1.9f, 0f), Vector3.zero, 1.8f), Is.False);
    Assert.That(InvokeStatic(fireCombat, "PointInCone",
        new Vector3(5f, 0f), Vector3.zero, Vector3.right, 7f, 35f), Is.True);
    Assert.That(InvokeStatic(fireCombat, "PointInCone",
        new Vector3(-1f, 0f), Vector3.zero, Vector3.right, 7f, 35f), Is.False);
}

[Test]
public void FirestormConversionDoublesRadiusOnlyOnce()
{
    var zoneType = System.Type.GetType("PrototypeFireZone, Assembly-CSharp");
    var zoneObject = new GameObject("Test Magma");
    var zone = zoneObject.AddComponent(zoneType);
    SetField(zone, "radius", 1.8f);
    InvokeInstance(zone, "ConvertToFirestorm");
    InvokeInstance(zone, "ConvertToFirestorm");
    Assert.That(GetInstanceProperty<float>(zone, "Radius"), Is.EqualTo(3.6f).Within(0.001f));
    Object.DestroyImmediate(zoneObject);
}
```

- [ ] **Step 2: Record deferred red-state expectation**

Expected: missing geometry methods and `PrototypeFireZone`. Do not launch Unity.

- [ ] **Step 3: Add pure geometry helpers**

Add exact methods to `PrototypeFireCombat`:

```csharp
internal static bool PointInCircle(Vector3 point, Vector3 center, float radius)
{
    var delta = point - center;
    delta.z = 0f;
    return delta.sqrMagnitude <= radius * radius;
}

internal static bool PointInCone(
    Vector3 point, Vector3 origin, Vector3 direction, float range, float halfAngle)
{
    var delta = point - origin;
    delta.z = 0f;
    return delta.sqrMagnitude <= range * range && delta.sqrMagnitude > 0.0001f &&
        Vector3.Angle(direction, delta) <= halfAngle;
}

internal static bool CircleIntersectsCone(
    Vector3 center, float radius, Vector3 origin, Vector3 direction, float range, float halfAngle)
{
    var forward = direction.normalized;
    if (PointInCone(center, origin, forward, range, halfAngle) ||
        PointInCircle(origin, center, radius))
    {
        return true;
    }

    var left = origin + Quaternion.Euler(0f, 0f, halfAngle) * forward * range;
    var right = origin + Quaternion.Euler(0f, 0f, -halfAngle) * forward * range;
    if (DistanceToSegment(center, origin, left) <= radius ||
        DistanceToSegment(center, origin, right) <= radius)
    {
        return true;
    }

    var delta = center - origin;
    delta.z = 0f;
    return delta.sqrMagnitude > 0.0001f &&
        Mathf.Abs(delta.magnitude - range) <= radius &&
        Vector3.Angle(forward, delta) <= halfAngle;
}

private static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
{
    var segment = end - start;
    var lengthSquared = segment.sqrMagnitude;
    if (lengthSquared <= 0.0001f) return Vector3.Distance(point, start);
    var t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
    return Vector3.Distance(point, start + segment * t);
}
```

Keep the math in this file so tests do not need scene objects.

- [ ] **Step 4: Add one lightweight zone component**

`PrototypeFireZone` stores owner, battle, kind, center/origin/direction, duration, remaining duration, tick interval, radius/range/angle, multiplier, and converted flag. Expose public read-only `Radius`, `RemainingDuration`, and `RemainingTickCount` properties. Use an internal enum with only `Magma` and `Scorch`.

Required factories:

```csharp
internal static PrototypeFireZone SpawnMagma(
    PrototypeCombatant owner, PrototypeBattle battle, Sprite sprite,
    Vector3 center, float radius, float duration, float tickInterval, float multiplier)

internal static PrototypeFireZone SpawnScorch(
    PrototypeCombatant owner, PrototypeBattle battle, Sprite sprite,
    Vector3 origin, Vector3 direction, float range, float halfAngle,
    float duration, float tickInterval, float multiplier)
```

Magma calls `owner.ApplyMagmaTick(enemy, this)` immediately, then every `0.5s`, for eight total ticks. Scorch calls `owner.ApplyScorchTick(enemy, this)` every `0.5s`. Destroy the GameObject at duration end or when owner/battle is invalid. If a zone tick kills, the owner computes future zone damage from `RemainingTickCount` and forwards it to `OnFireDotKill` before the zone continues.

- [ ] **Step 5: Add minimal zone visuals and conversion lookup**

Use child `SpriteRenderer`s with the existing prototype square sprite. Magma is a low-sorting circular approximation using rotated translucent layers. Scorch is a fan of five translucent rays between `-35` and `+35` degrees. Conversion changes radius to `3.6f`, scales the visual to `2x`, brightens it, and is idempotent.

Add:

```csharp
internal static void ConvertIntersectingMagmaPools(
    PrototypeCombatant owner, Vector3 origin, Vector3 direction, float range, float halfAngle)
```

This scans `Object.FindObjectsByType<PrototypeFireZone>(FindObjectsSortMode.None)` because a battle contains only a few short-lived zones.

`DestroyOwnedBy(owner)` uses the same scan and destroys only zones whose owner matches; it is called by battle/farm-wave reset.

- [ ] **Step 6: Inspect and checkpoint commit if safe**

```powershell
git diff -- Assets/Scripts/PrototypeFireCombat.cs Assets/Scripts/CombatPrototype.cs Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs
git add -- Assets/Scripts/PrototypeFireCombat.cs Assets/Scripts/CombatPrototype.cs Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs
git diff --cached --check
git commit -m "feat: add magma and scorch combat zones"
```

### Task 5: Implement The Complete Fire God Skill Kit And Presentation

**Files:**
- Modify: `Assets/Scripts/CombatPrototype.cs:1551-2140, 2501-3279, 3480-3655`
- Modify: `Assets/Scripts/PrototypeFireCombat.cs`
- Test: `Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs`

**Interfaces:**
- Consumes: all Tasks 1-4 interfaces.
- Produces: `CastHellfireImpact`, `CastCrimsonGale`, `ApplyMagmaTick`, `ApplyScorchTick`, `OnFireDotKill`, `DealFireArea`, `DelayedFireArea`, `DealFlatDamage`, Flame Shield, Scorch Burst, Firestorm conversion, Ash/Heat/Grounded world presentation.

- [ ] **Step 1: Write failing Fire God combo tests**

Add focused tests rather than one long battle assertion:

```csharp
[Test]
public void CrimsonGaleMissingHealthDamageScalesWithConsumedAsh()
{
    var fireCombat = System.Type.GetType("PrototypeFireCombat, Assembly-CSharp");
    Assert.That(InvokeStatic(fireCombat, "MissingHealthDetonation", 1000, 600, 1), Is.EqualTo(40));
    Assert.That(InvokeStatic(fireCombat, "MissingHealthDetonation", 1000, 600, 3), Is.EqualTo(120));
}

[Test]
public void FlameShieldScalesFromTwelveToThirtyTwoPercent()
{
    var fireCombat = System.Type.GetType("PrototypeFireCombat, Assembly-CSharp");
    Assert.That(InvokeStatic(fireCombat, "FlameShieldAmount", 1000, 0), Is.EqualTo(120));
    Assert.That(InvokeStatic(fireCombat, "FlameShieldAmount", 1000, 5), Is.EqualTo(320));
}
```

Add Unity tests that invoke each cast directly and assert:

- normal Basic applies one Ash;
- Basic against three Ash consumes them and produces immediate plus delayed AoE;
- Hellfire Impact hits every enemy within `1.8m`, creates one Magma zone, and applies knock-up;
- a Magma tick at three Ash consumes stacks, deals True Damage, and starts `70%` decaying slow;
- Crimson Gale hits only cone members, knocks them in cast direction, consumes pre-hit Ash for missing-health damage, then leaves one new Ash;
- Scorch applies Fire DoT and Grounded;
- crossing `25%` HP activates one shield, then cannot reactivate before `90s`;
- Fire DoT kill spreads two Ash and half remaining DoT to every living enemy within `4m`.

- [ ] **Step 2: Add pure balance helpers**

In `PrototypeFireCombat` add:

```csharp
internal static int MissingHealthDetonation(int maxHealth, int currentHealth, int ashStacks) =>
    Mathf.RoundToInt(Mathf.Max(0, maxHealth - currentHealth) * 0.1f * Mathf.Clamp(ashStacks, 0, 3));

internal static int FlameShieldAmount(int maxHealth, int heatStacks) =>
    Mathf.RoundToInt(maxHealth * (0.12f + Mathf.Clamp(heatStacks, 0, 5) * 0.04f));
```

- [ ] **Step 3: Replace Nova's Basic behavior with Flame Strike**

For `PrototypeSkillKit.Nova`, make the Basic direct hit Fire. Check the target's Ash count before damage. At fewer than three stacks, apply one Ash after a surviving hit. At three stacks:

1. consume all Ash;
2. deal the normal primary Fire hit;
3. deal `0.45x` Fire/Ash-detonation splash to living opponents within `1.5m`;
4. start a combatant coroutine that waits `0.35s`, then deals `0.75x` Fire/Ash-detonation damage at the captured target position;
5. do not reapply Ash to the primary target on that empowered hit.

The coroutine must use a captured position and team query, not `pendingAction`.

```csharp
private void PerformFireGodBasic()
{
    if (!AcquireTarget()) return;
    var target = Target;
    var empowered = target.AshStacksFrom(this) >= 3;
    if (empowered) target.ConsumeAsh(this);

    DealDamage(target, SkillPower(skills.Basic, 1f), PrototypeDamageType.Fire);
    GainEnergy(skills.Basic.EnergyGain);
    if (!target.IsAlive) return;
    if (!empowered) { target.ApplyAsh(this, 1); return; }

    DealFireArea(target.transform.position, 1.5f, 0.45f, PrototypeDamageFlags.AshDetonation);
    StartCoroutine(DelayedFireArea(
        target.transform.position, 1.5f, 0.75f, 0.35f, PrototypeDamageFlags.AshDetonation));
}

private void DealFireArea(
    Vector3 center, float radius, float multiplier, PrototypeDamageFlags flags)
{
    foreach (var enemy in battle.GetOpponents(Team))
        if (enemy.IsAlive && PrototypeFireCombat.PointInCircle(enemy.transform.position, center, radius))
            DealDamage(enemy, multiplier, PrototypeDamageType.Fire, flags);
}

private IEnumerator DelayedFireArea(
    Vector3 center, float radius, float multiplier, float delay, PrototypeDamageFlags flags)
{
    yield return new WaitForSeconds(delay);
    if (!battle.IsFinished) DealFireArea(center, radius, multiplier, flags);
}
```

- [ ] **Step 4: Implement Thermal Resonance and Flame Shield**

Add `flameShieldCooldown`. Decrement it with statuses. In `TakeDamage`, compare previous and current health and activate only when crossing from above `25%` to at-or-below `25%` while alive and cooldown is zero.

Activation:

```csharp
GrantShield(PrototypeFireCombat.FlameShieldAmount(MaxHealth, heatStacks), 6f);
flameShieldCooldown = 90f;
foreach (var enemy in battle.GetOpponents(Team))
    if (enemy.IsAlive && Vector3.Distance(transform.position, enemy.transform.position) <= 1.75f)
        enemy.ApplyKnockback((enemy.transform.position - transform.position).normalized, 1.2f);
```

Spawn the current barrier plus an orange expanding effect. Remove old `Aegis Reactor`, `reactorTriggered`, and `IsLastLightActive` behavior so no legacy Nova passive remains.

- [ ] **Step 5: Implement Everburning Embers**

`OnFireDotKill` returns immediately for non-Nova sources or non-positive remaining damage. Otherwise, for every living opponent within `4m`:

```csharp
enemy.ApplyAsh(this, 2);
enemy.ApplyBurnTotal(this, Mathf.RoundToInt(remainingDamage * 0.5f), remainingDuration, tickInterval);
PrototypeProjectile.SpawnArc(prototypeSprite, deadTargetPosition, enemy.transform.position, SkillAccent, 0.24f);
```

Extend `PrototypeProjectile` with an optional perpendicular arc height; keep the existing straight `Spawn` behavior unchanged for all current callers.

- [ ] **Step 6: Implement Hellfire Impact**

Replace Nova's Active branch with `CastHellfireImpact()`:

- capture the current target position at impact;
- Fire-damage every living opponent within `1.8m` at `1.45x`;
- knock each hit target up for `0.28s`;
- apply one Ash after direct damage;
- spawn a `4s`, `0.5s`, `1.8m`, `0.18x` Magma zone.

`ApplyMagmaTick` deals Fire DoT, refreshes the target's Burning marker for `0.6s`, and refreshes `30%` movement slow for `0.6s`. If the tick kills, pass `tickDamage * zone.RemainingTickCount`, `zone.RemainingDuration`, and `0.5s` to `OnFireDotKill`. If the target has three Ash, consume them, deal `0.75x` True/Ash-detonation damage, and apply a `70%` two-second decaying slow.

```csharp
private void CastHellfireImpact()
{
    var point = Target.transform.position;
    foreach (var enemy in battle.GetOpponents(Team))
    {
        if (!enemy.IsAlive || !PrototypeFireCombat.PointInCircle(enemy.transform.position, point, 1.8f)) continue;
        DealDamage(enemy, 1.45f, PrototypeDamageType.Fire);
        if (enemy.IsAlive)
        {
            enemy.ApplyKnockUp(0.28f);
            enemy.ApplyAsh(this, 1);
        }
    }
    PrototypeFireZone.SpawnMagma(this, battle, prototypeSprite, point, 1.8f, 4f, 0.5f, 0.18f);
}
```

- [ ] **Step 7: Implement Crimson Gale**

Replace Nova's Ultimate branch with `CastCrimsonGale()`:

- reacquire a target if needed;
- capture normalized direction;
- for cone members within `7m` and `35` degrees, consume pre-hit Ash, deal `10%` missing-health Fire/Ash-detonation damage per stack, deal `2.2x` direct Fire damage, knock back `1.6m` along the cast direction, then apply one Ash if alive;
- spawn a `5s`, `0.5s`, `7m`, `35` degree, `0.12x` Scorch zone;
- convert each intersected owned Magma Pool once.

`ApplyScorchTick` deals Fire DoT, refreshes the Burning marker and Grounded for `0.6s`, and forwards remaining zone damage to `OnFireDotKill` when its tick kills.

```csharp
private void CastCrimsonGale()
{
    if (!AcquireTarget()) return;
    var origin = transform.position;
    var direction = (Target.transform.position - origin).normalized;
    foreach (var enemy in battle.GetOpponents(Team))
    {
        if (!enemy.IsAlive || !PrototypeFireCombat.PointInCone(
                enemy.transform.position, origin, direction, 7f, 35f)) continue;
        var stacks = enemy.ConsumeAsh(this);
        if (stacks > 0)
        {
            DealFlatDamage(enemy,
                PrototypeFireCombat.MissingHealthDetonation(enemy.MaxHealth, enemy.CurrentHealth, stacks),
                PrototypeDamageType.Fire,
                PrototypeDamageFlags.AshDetonation);
        }
        DealDamage(enemy, 2.2f, PrototypeDamageType.Fire);
        if (enemy.IsAlive)
        {
            enemy.ApplyKnockback(direction, 1.6f);
            enemy.ApplyAsh(this, 1);
        }
    }
    PrototypeFireZone.SpawnScorch(
        this, battle, prototypeSprite, origin, direction, 7f, 35f, 5f, 0.5f, 0.12f);
    PrototypeFireZone.ConvertIntersectingMagmaPools(this, origin, direction, 7f, 35f);
}
```

`DealFlatDamage` routes a precomputed amount through shield, damage totals, kill callbacks, Fire Resistance for Fire, and impact presentation; it must not bypass `TakeDamage`.

```csharp
private PrototypeDamageResult DealFlatDamage(
    PrototypeCombatant target, int amount, PrototypeDamageType type, PrototypeDamageFlags flags)
{
    var calculated = PrototypeFireCombat.CalculateDamage(
        amount, 0, 1f, type, target.FireResistance, 0f);
    return ApplyCalculatedDamage(target, calculated, type, flags);
}
```

Extract the existing post-calculation shield/health, damage-total, impact, Heat, DoT-kill, and Vex-kill handling into `ApplyCalculatedDamage(...)`; both `DealDamage` and `DealFlatDamage` call it so no result path is duplicated.

- [ ] **Step 8: Update status icons and Fire presentation**

Extend `PrototypeStatusIconArt.Get()` with Ash, Heat, and Grounded icons. Add parallel world-space count labels only for Ash and Heat. Keep the visible row capped at five and prioritize: stun/knock-up, Ash, Burn, Grounded, Heat, shield, movement slow, remaining legacy statuses.

```csharp
private TextMesh[] statusCounts;

private void SetStackCount(int iconIndex, int count)
{
    statusCounts[iconIndex].text = count > 1 ? count.ToString() : string.Empty;
    statusCounts[iconIndex].gameObject.SetActive(IsAlive && count > 0);
}
```

Repurpose the old Photon Mark visual as the three-stack Ash sigil, add a low-alpha orange Heat aura, and use existing `PrototypeEffect` helpers for Hellfire impact, cone rays, Firestorm brightness, and Flame Shield shockwave. Do not add a shader or camera cut-in.

- [ ] **Step 9: Reset and death cleanup**

Clear/destroy owned zones and delayed effects on battle/farm-wave reset. Let an already spawned Scorch Burst delayed explosion finish only while the battle is active. Keep the existing death animation completion and battlefield disappearance.

```csharp
ashBySource.Clear();
heatStacks = 0;
flameShieldCooldown = 0f;
movementSlowRemaining = decayingSlowDuration = groundedRemaining = airborneRemaining = 0f;
fireDotMarkerRemaining = 0f;
PrototypeFireZone.DestroyOwnedBy(this);
```

- [ ] **Step 10: Inspect and checkpoint commit if safe**

```powershell
git diff -- Assets/Scripts/CombatPrototype.cs Assets/Scripts/PrototypeFireCombat.cs Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs
git add -- Assets/Scripts/CombatPrototype.cs Assets/Scripts/PrototypeFireCombat.cs Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs
git diff --cached --check
git commit -m "feat: implement Fire God Heavenly Demon skill kit"
```

### Task 6: Migrate Identity And Save Data

**Files:**
- Modify: `Assets/Scripts/CombatantDefinition.cs`
- Modify: `Assets/Scripts/PrototypeProgression.cs`
- Modify: `Assets/Scripts/PrototypeGacha.cs`
- Modify: `Assets/Scripts/PrototypeSaveSystem.cs`
- Modify: `Assets/Scripts/CombatPrototype.cs:27-352`
- Test: `Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs`

**Interfaces:**
- Produces: `PrototypeCharacterNames.LegacyNova`, `PrototypeCharacterNames.FireGodHeavenlyDemon`.
- Produces: `MigrateCharacterName(oldName, newName)` on progression, gacha, and session save owners.
- Consumes: PlayerPrefs JSON formats already owned by those classes.

- [ ] **Step 1: Write failing migration and identity tests**

Create explicit version-3 PlayerPrefs payloads for progression, gacha, and squad, call `PrototypeSaveSystem.Migrate()`, reset caches, and assert:

```csharp
Assert.That(PlayerPrefs.GetInt("Prototype.SaveVersion"), Is.EqualTo(4));
Assert.That(PrototypeProgression.Get("Fire God Heavenly Demon").level, Is.EqualTo(27));
Assert.That(PrototypeGacha.IsOwned("Fire God Heavenly Demon"), Is.True);
Assert.That(PrototypeGacha.IsOwned("Nova"), Is.False);
var squadJson = PlayerPrefs.GetString("Prototype.Squad");
Assert.That(squadJson, Does.Contain("Fire God Heavenly Demon"));
Assert.That(squadJson, Does.Not.Contain("\"Nova\""));
```

Include a duplicate-record case and verify the merged record uses the maximum value for every numeric progression field and only one canonical entry remains.

- [ ] **Step 2: Add canonical name constants**

In `CombatantDefinition.cs`:

```csharp
public static class PrototypeCharacterNames
{
    public const string LegacyNova = "Nova";
    public const string FireGodHeavenlyDemon = "Fire God Heavenly Demon";
}
```

- [ ] **Step 3: Implement owner-local migrations**

`PrototypeProgression.MigrateCharacterName` loads its save, finds both records, renames the old one when no new record exists, or merges every numeric field with `Mathf.Max` then removes the duplicate. Save once.

```csharp
public static void MigrateCharacterName(string oldName, string newName)
{
    EnsureLoaded();
    var oldRecord = save.characters.Find(character => character.characterName == oldName);
    if (oldRecord == null) return;
    var newRecord = save.characters.Find(character => character.characterName == newName);
    if (newRecord == null)
    {
        oldRecord.characterName = newName;
    }
    else
    {
        newRecord.level = Mathf.Max(newRecord.level, oldRecord.level);
        newRecord.experience = Mathf.Max(newRecord.experience, oldRecord.experience);
        newRecord.stars = Mathf.Max(newRecord.stars, oldRecord.stars);
        newRecord.shards = Mathf.Max(newRecord.shards, oldRecord.shards);
        newRecord.basicSkillLevel = Mathf.Max(newRecord.basicSkillLevel, oldRecord.basicSkillLevel);
        newRecord.passiveSkillLevel = Mathf.Max(newRecord.passiveSkillLevel, oldRecord.passiveSkillLevel);
        newRecord.activeSkillLevel = Mathf.Max(newRecord.activeSkillLevel, oldRecord.activeSkillLevel);
        newRecord.ultimateSkillLevel = Mathf.Max(newRecord.ultimateSkillLevel, oldRecord.ultimateSkillLevel);
        newRecord.weaponLevel = Mathf.Max(newRecord.weaponLevel, oldRecord.weaponLevel);
        newRecord.armorLevel = Mathf.Max(newRecord.armorLevel, oldRecord.armorLevel);
        newRecord.coreLevel = Mathf.Max(newRecord.coreLevel, oldRecord.coreLevel);
        save.characters.Remove(oldRecord);
    }
    Save();
}
```

`PrototypeGacha.MigrateCharacterName` replaces all old-name owned entries, removes duplicates, and saves once. Change the starter array's first entry to the canonical constant.

```csharp
public static void MigrateCharacterName(string oldName, string newName)
{
    EnsureLoaded();
    var changed = save.ownedCharacters.RemoveAll(name => name == oldName) > 0;
    if (changed && !save.ownedCharacters.Contains(newName)) save.ownedCharacters.Add(newName);
    if (changed) Save();
}
```

`PrototypeSession.MigrateCharacterName` parses `Prototype.Squad`, replaces matching names, writes JSON back, and replaces any already loaded `squadNames` entries.

```csharp
public static void MigrateCharacterName(string oldName, string newName)
{
    var json = PlayerPrefs.GetString(SquadKey, string.Empty);
    if (!string.IsNullOrEmpty(json))
    {
        var data = JsonUtility.FromJson<PrototypeSquadSave>(json);
        if (data != null && data.names != null)
        {
            for (var index = 0; index < data.names.Count; index++)
                if (data.names[index] == oldName) data.names[index] = newName;
            PlayerPrefs.SetString(SquadKey, JsonUtility.ToJson(data));
        }
    }
    if (squadNames != null)
        for (var index = 0; index < squadNames.Length; index++)
            if (squadNames[index] == oldName) squadNames[index] = newName;
    PlayerPrefs.Save();
}
```

- [ ] **Step 4: Orchestrate version 4 migration**

Set `CurrentVersion = 4`. Before writing the new version:

```csharp
if (version < 4)
{
    PrototypeProgression.MigrateCharacterName(
        PrototypeCharacterNames.LegacyNova,
        PrototypeCharacterNames.FireGodHeavenlyDemon);
    PrototypeGacha.MigrateCharacterName(
        PrototypeCharacterNames.LegacyNova,
        PrototypeCharacterNames.FireGodHeavenlyDemon);
    PrototypeSession.MigrateCharacterName(
        PrototypeCharacterNames.LegacyNova,
        PrototypeCharacterNames.FireGodHeavenlyDemon);
}
```

Keep the existing onboarding migration unchanged.

- [ ] **Step 5: Update existing name-based tests and lookups**

Replace expected runtime/GameObject/starter strings from `Nova` to the canonical name. Keep resource paths and `PrototypeSkillKit.Nova` assertions unchanged.

- [ ] **Step 6: Inspect and checkpoint commit if safe**

```powershell
git diff -- Assets/Scripts/CombatantDefinition.cs Assets/Scripts/PrototypeProgression.cs Assets/Scripts/PrototypeGacha.cs Assets/Scripts/PrototypeSaveSystem.cs Assets/Scripts/CombatPrototype.cs Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs
git add -- Assets/Scripts/CombatantDefinition.cs Assets/Scripts/PrototypeProgression.cs Assets/Scripts/PrototypeGacha.cs Assets/Scripts/PrototypeSaveSystem.cs Assets/Scripts/CombatPrototype.cs Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs
git diff --cached --check
git commit -m "feat: migrate Nova saves to Fire God"
```

### Task 7: Export PXO Sheets And Configure Canonical Assets

**Files:**
- Create: `Tools/Export-FireGodPxo.ps1`
- Create: `Assets/Editor/FireGodSpriteSheetPostprocessor.cs`
- Modify: `Assets/Scripts/CombatantDefinition.cs`
- Modify: `Assets/Editor/PrototypeContentGenerator.cs`
- Create: `Assets/Art/Characters/Nova/Source/Nova.pxo`
- Create: `Assets/Art/Characters/Nova/Combat/FireGodIdle.png`
- Create: `Assets/Art/Characters/Nova/Combat/FireGodWalk.png`
- Create: `Assets/Art/Characters/Nova/Combat/FireGodRun.png`
- Create: `Assets/Art/Characters/Nova/Combat/FireGodActive.png`
- Create: `Assets/Art/Characters/Nova/Combat/FireGodUltimate.png`
- Create: `Assets/Art/Characters/Nova/Combat/FireGodHit.png`
- Create: `Assets/Art/Characters/Nova/Combat/FireGodDeath.png`
- Modify after import: `Assets/Resources/Combatants/Allies/Nova.asset`
- Modify after import: `Assets/Resources/Skills/Nova.asset`
- Test: `Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs`

**Interfaces:**
- Produces: deterministic sprite names `FireGod<Action>_00...` and exact frame counts.
- Produces: `CombatantDefinition.EditorConfigureAnimationFrames(...)`.
- Consumes: PXO raw RGBA cels and the existing flame Basic sheet.

- [ ] **Step 1: Write failing asset identity/frame tests**

Update the existing imported-sprite test to load `Combatants/Allies/Nova` by resource path and assert:

```csharp
Assert.That(definition.DisplayName, Is.EqualTo("Fire God Heavenly Demon"));
Assert.That(definition.Species.ToString(), Is.EqualTo("Human"));
Assert.That(definition.CombatClass.ToString(), Is.EqualTo("Mage"));
Assert.That(definition.AttackRange, Is.EqualTo(4f));

Assert.That(GetSpriteFrames(spriteSet, "Idle").Length, Is.EqualTo(8));
Assert.That(GetSpriteFrames(spriteSet, "Run").Length, Is.EqualTo(13));
Assert.That(GetSpriteFrames(spriteSet, "Attack").Length, Is.EqualTo(8));
Assert.That(GetSpriteFrames(spriteSet, "Skill").Length, Is.EqualTo(13));
Assert.That(GetSpriteFrames(spriteSet, "Ultimate").Length, Is.EqualTo(13));
Assert.That(GetSpriteFrames(spriteSet, "Hit").Length, Is.EqualTo(9));
Assert.That(GetSpriteFrames(spriteSet, "Death").Length, Is.EqualTo(9));
Assert.That(GetSpriteFrames(spriteSet, "Attack")[0].texture.name,
    Is.EqualTo("NovaFlameBasicAttack"));
```

For every imported frame, assert a non-null texture and a `68 x 68` texture rect. The exporter performs the non-zero-alpha validation before Unity because imported textures remain non-readable at runtime.

- [ ] **Step 2: Add the reproducible PXO exporter**

`Tools/Export-FireGodPxo.ps1` accepts optional `-Source` and `-ProjectRoot`, defaulting Source to `..\Model\Nova\Nova.pxo` relative to the project. It must:

1. open the PXO with `System.IO.Compression.ZipFile`;
2. parse `data.json` with `ConvertFrom-Json`;
3. validate `size_x = 68`, `size_y = 68`, and required layer names;
4. read each `image_data/frames/<1-based-frame>/layer_<1-based-layer>` byte array;
5. discard frames whose alpha bytes are all zero;
6. vertically flip rows into `System.Drawing.Bitmap` coordinates;
7. concatenate remaining frames left-to-right;
8. save the seven named PNGs;
9. validate exact counts `8, 8, 13, 13, 13, 9, 9`;
10. copy the source to `Assets/Art/Characters/Nova/Source/Nova.pxo`.

The script exits non-zero on a missing layer, wrong byte length, wrong count, or all-transparent exported sheet. End with a compact count report.

Use this implementation shape; keep it as one script with no dependency installation:

```powershell
param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [string]$Source
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($Source)) {
    $Source = (Resolve-Path (Join-Path $ProjectRoot '..\Model\Nova\Nova.pxo')).Path
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Drawing

$definitions = @(
    [pscustomobject]@{ Layer = 'Idle';         File = 'FireGodIdle.png';     Count = 8  },
    [pscustomobject]@{ Layer = 'Walk';         File = 'FireGodWalk.png';     Count = 8  },
    [pscustomobject]@{ Layer = 'Run';          File = 'FireGodRun.png';      Count = 13 },
    [pscustomobject]@{ Layer = 'Active Skill'; File = 'FireGodActive.png';   Count = 13 },
    [pscustomobject]@{ Layer = 'Ultimate';     File = 'FireGodUltimate.png'; Count = 13 },
    [pscustomobject]@{ Layer = 'Hit';          File = 'FireGodHit.png';      Count = 9  },
    [pscustomobject]@{ Layer = 'Death';        File = 'FireGodDeath.png';    Count = 9  }
)

$output = Join-Path $ProjectRoot 'Assets\Art\Characters\Nova\Combat'
$sourceOutput = Join-Path $ProjectRoot 'Assets\Art\Characters\Nova\Source'
New-Item -ItemType Directory -Force $output, $sourceOutput | Out-Null
$zip = [IO.Compression.ZipFile]::OpenRead($Source)
try {
    $dataEntry = $zip.GetEntry('data.json')
    if ($null -eq $dataEntry) { throw 'PXO data.json is missing.' }
    $reader = [IO.StreamReader]::new($dataEntry.Open())
    try { $data = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
    if ($data.size_x -ne 68 -or $data.size_y -ne 68) { throw 'PXO canvas must be 68x68.' }

    $report = @()
    foreach ($definition in $definitions) {
        $layerIndex = -1
        for ($index = 0; $index -lt $data.layers.Count; $index++) {
            if ($data.layers[$index].name -eq $definition.Layer) { $layerIndex = $index; break }
        }
        if ($layerIndex -lt 0) { throw "Missing PXO layer: $($definition.Layer)" }

        $frames = [Collections.Generic.List[byte[]]]::new()
        for ($frame = 1; $frame -le $data.frames.Count; $frame++) {
            $entry = $zip.GetEntry("image_data/frames/$frame/layer_$($layerIndex + 1)")
            if ($null -eq $entry) { throw "Missing cel for $($definition.Layer), frame $frame" }
            $stream = $entry.Open()
            $binary = [IO.BinaryReader]::new($stream)
            try { $bytes = $binary.ReadBytes([int]$entry.Length) } finally { $binary.Dispose() }
            if ($bytes.Length -ne 18496) { throw "Invalid cel length for $($definition.Layer), frame $frame" }

            $nonEmpty = $false
            for ($alpha = 3; $alpha -lt $bytes.Length; $alpha += 4) {
                if ($bytes[$alpha] -ne 0) { $nonEmpty = $true; break }
            }
            if ($nonEmpty) { $frames.Add($bytes) }
        }
        if ($frames.Count -ne $definition.Count) {
            throw "$($definition.Layer) expected $($definition.Count) frames, got $($frames.Count)."
        }

        $bitmap = [Drawing.Bitmap]::new(68 * $frames.Count, 68)
        try {
            for ($frame = 0; $frame -lt $frames.Count; $frame++) {
                $bytes = $frames[$frame]
                for ($y = 0; $y -lt 68; $y++) {
                    for ($x = 0; $x -lt 68; $x++) {
                        $offset = (($y * 68) + $x) * 4
                        $color = [Drawing.Color]::FromArgb(
                            $bytes[$offset + 3], $bytes[$offset], $bytes[$offset + 1], $bytes[$offset + 2])
                        $bitmap.SetPixel(($frame * 68) + $x, 67 - $y, $color)
                    }
                }
            }
            $bitmap.Save((Join-Path $output $definition.File), [Drawing.Imaging.ImageFormat]::Png)
        } finally { $bitmap.Dispose() }
        $report += "$($definition.Layer)=$($frames.Count)"
    }
} finally { $zip.Dispose() }

Copy-Item -LiteralPath $Source -Destination (Join-Path $sourceOutput 'Nova.pxo') -Force
Write-Output ($report -join ' ')
Write-Output 'Exported Fire God PXO sheets successfully.'
```

- [ ] **Step 3: Run the exporter without opening Unity**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Export-FireGodPxo.ps1
```

Expected output:

```text
Idle=8 Walk=8 Run=13 Active Skill=13 Ultimate=13 Hit=9 Death=9
Exported Fire God PXO sheets successfully.
```

Inspect the generated PNG dimensions with PowerShell and visually inspect at least Idle, Active, Ultimate, and Death before Unity import. Expected widths are frame count times `68`; height is `68`.

- [ ] **Step 4: Configure deterministic Unity slicing**

Create `FireGodSpriteSheetPostprocessor : AssetPostprocessor`. Match only the seven exact generated asset paths. Use the complete importer shape below:

```csharp
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

internal sealed class FireGodSpriteSheetPostprocessor : AssetPostprocessor
{
    private static readonly Dictionary<string, int> Counts = new Dictionary<string, int>
    {
        { "FireGodIdle", 8 }, { "FireGodWalk", 8 }, { "FireGodRun", 13 },
        { "FireGodActive", 13 }, { "FireGodUltimate", 13 },
        { "FireGodHit", 9 }, { "FireGodDeath", 9 }
    };

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Art/Characters/Nova/Combat/")) return;
        var sheetName = Path.GetFileNameWithoutExtension(assetPath);
        if (!Counts.TryGetValue(sheetName, out var count)) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 68f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.spriteGenerateFallbackPhysicsShape = false;

        var sprites = new SpriteMetaData[count];
        for (var index = 0; index < count; index++)
        {
            sprites[index] = new SpriteMetaData
            {
                name = $"{sheetName}_{index:00}",
                rect = new Rect(index * 68, 0, 68, 68),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            };
        }
        importer.spritesheet = sprites;
    }
}
```

- [ ] **Step 5: Add editor frame assignment**

Add to `CombatantDefinition` under `#if UNITY_EDITOR`:

```csharp
public void EditorConfigureAnimationFrames(
    Sprite[] idle, Sprite[] run, Sprite[] attack, Sprite[] skill,
    Sprite[] ultimate, Sprite[] hit, Sprite[] death, bool facesLeft = false)
{
    sourceFacesLeft = facesLeft;
    idleFrames = idle;
    runFrames = run;
    attackFrames = attack;
    skillFrames = skill;
    ultimateFrames = ultimate;
    hitFrames = hit;
    deathFrames = death;
}
```

Assign every array and `sourceFacesLeft`. Do not add Walk to runtime data because the current game has one movement state.

- [ ] **Step 6: Make the content generator update only canonical Nova assets**

Add `ConfigureFireGodHeavenlyDemon()` and call it after `CreateSkillSets()`. It loads the existing Nova combatant/skill assets, configures:

- canonical display name, Human, Mage, SSR;
- unchanged base stats/order/internal kit;
- Basic `Flame Strike`, `0.56s`, hit frame `0.3214286`, `1.0x`, `18` energy;
- Passive 1 `Ignition Core`;
- Passive 2 `Thermal Resonance`;
- Passive 3 `Everburning Embers`;
- Active `Hellfire Impact`, `6s`, `1.3s`, hit frame `0.6153846`, `1.45x`;
- Ultimate `Crimson Gale`, `1.3s`, hit frame `0.6153846`, `2.2x`;
- imported Idle/Run/Active/Ultimate/Hit/Death arrays sorted by sprite name;
- existing `NovaFlameBasicAttack` sprites sorted by sprite name.

Use these exact concise data descriptions; runtime mechanics remain authoritative:

```text
Flame Strike: Fire attack that applies Ash; at three Ash, consumes the marks for splash and a delayed explosion.
Ignition Core: Direct Fire hits build up to three Ash Marks; each mark reduces Fire Resistance by 5%.
Thermal Resonance: Fire damage against Burning or detonated targets restores energy and builds Heat; critical health triggers Flame Shield.
Everburning Embers: Fire DoT kills spread two Ash Marks and half of the remaining DoT to nearby enemies.
Hellfire Impact: Fire AoE and knock-up that leaves a slowing Magma Pool; three Ash trigger True Damage and a decaying slow.
Crimson Gale: Directional Fire wave with knockback, Ash missing-health detonation, Grounded Scorch, and Magma-to-Firestorm conversion.
```

Use `AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(sprite => sprite.name)` and `EditorUtility.SetDirty`. Do not change generic `CreateSkill`/`CreateHero` behavior or overwrite other hero assets.

- [ ] **Step 7: Inspect and checkpoint commit if safe**

```powershell
git diff -- Tools/Export-FireGodPxo.ps1 Assets/Editor/FireGodSpriteSheetPostprocessor.cs Assets/Scripts/CombatantDefinition.cs Assets/Editor/PrototypeContentGenerator.cs Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs
git status --short -- Assets/Art/Characters/Nova
git add -- Tools/Export-FireGodPxo.ps1 Assets/Editor/FireGodSpriteSheetPostprocessor.cs Assets/Scripts/CombatantDefinition.cs Assets/Editor/PrototypeContentGenerator.cs Assets/Art/Characters/Nova/Source/Nova.pxo Assets/Art/Characters/Nova/Combat/FireGod*.png Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs
git diff --cached --check
git commit -m "feat: import Fire God animation source"
```

Do not add generated `.meta` files until Unity creates them in the final import.

### Task 8: Document And Perform The Single Final Unity Verification

**Files:**
- Modify: `README.md`
- Generated/modified by final import: all new `.meta` files, `Assets/Resources/Combatants/Allies/Nova.asset`, `Assets/Resources/Skills/Nova.asset`
- Test result: `TestResults/FireGodPlayMode.xml`
- Unity log: `Logs/FireGodPlayMode.log`

**Interfaces:**
- Consumes: every previous task.
- Produces: one imported, tested project ready for manual portrait playtest.

- [ ] **Step 1: Update README before launching Unity**

Replace Nova-facing current-state text with:

- Fire God Heavenly Demon identity and Human Fire Mage class;
- PXO source and exact layer/frame mapping;
- retained flame Basic Attack and license reminder;
- Ash, Heat, Everburning, Hellfire Impact, Crimson Gale, Scorch, and Firestorm loop;
- save version 4 migration from `Nova`;
- explicit preserved constraints: free movement, no lanes, no kiting, no cut-in, no global hit-stop;
- exact regeneration command `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Export-FireGodPxo.ps1`;
- keep the existing validation line unchanged until the final command completes, then replace it with the observed total and result.

- [ ] **Step 2: Perform static preflight without opening Unity**

```powershell
git diff --check
rg -n "Photon Brand|Aegis Reactor|Solar Thrust|Stellar Breaker" Assets/Scripts Assets/Editor Assets/Resources README.md
rg -n "Fire God Heavenly Demon|Ignition Core|Thermal Resonance|Everburning Embers|Hellfire Impact|Crimson Gale" Assets/Scripts Assets/Editor README.md
```

Expected: no legacy Nova skill names in active data/runtime/docs; all six Fire God names appear. Legacy migration constant `"Nova"` is allowed.

- [ ] **Step 3: Run one Unity import and the complete Play Mode suite**

Ensure no interactive Unity process has the project locked, create output directories, then run:

```powershell
New-Item -ItemType Directory -Force TestResults, Logs | Out-Null
& 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe' `
  -batchmode -nographics -projectPath 'D:\thuan_workspace_2\Game\Galaxy-Rising' `
  -runTests -testPlatform PlayMode `
  -testResults 'D:\thuan_workspace_2\Game\Galaxy-Rising\TestResults\FireGodPlayMode.xml' `
  -logFile 'D:\thuan_workspace_2\Game\Galaxy-Rising\Logs\FireGodPlayMode.log' `
  -quit
```

This single launch imports/slices the PNGs, runs `PrototypeContentGenerator` to update the two Nova resource assets, compiles all code, and executes the complete suite.

- [ ] **Step 4: Read evidence and fix in one batch if necessary**

```powershell
Select-String -Path .\Logs\FireGodPlayMode.log -Pattern 'error CS|Exception|FAIL|Failed|All tests passed'
[xml]$results = Get-Content .\TestResults\FireGodPlayMode.xml
$results.'test-run' | Select-Object total, passed, failed, skipped, result
```

Expected: `failed=0`, `result=Passed`, and no compile/import exceptions. If the run fails, collect every reported compile/test/import issue, fix them together, and only then rerun the complete command once.

- [ ] **Step 5: Verify generated assets after the import**

Confirm:

- each new PNG has a `.meta` with Multiple sprites, Point filtering, `68 PPU`, no mipmaps/compression;
- `Nova.asset` references exact frame counts `8/13/8/13/13/9/9`;
- `Nova.asset` displays `Fire God Heavenly Demon`, Human, Mage, SSR;
- `Skills/Nova.asset` contains all three passives and exact action timings;
- Unity log contains no missing sprite, duplicate sprite ID, all-transparent frame, or save-migration error.

- [ ] **Step 6: Fill the README validation count and commit only feature files**

```powershell
git status --short
git diff --check
git add -- README.md Assets/Scripts/PrototypeFireCombat.cs Assets/Scripts/PrototypeFireCombat.cs.meta Assets/Scripts/SkillDefinition.cs Assets/Scripts/CombatantDefinition.cs Assets/Scripts/CombatPrototype.cs Assets/Scripts/PrototypeProgression.cs Assets/Scripts/PrototypeGacha.cs Assets/Scripts/PrototypeSaveSystem.cs Assets/Editor/PrototypeContentGenerator.cs Assets/Editor/FireGodSpriteSheetPostprocessor.cs Assets/Editor/FireGodSpriteSheetPostprocessor.cs.meta Assets/Resources/Combatants/Allies/Nova.asset Assets/Resources/Skills/Nova.asset Assets/Art/Characters/Nova/Source Assets/Art/Characters/Nova/Combat/FireGod*.png Assets/Art/Characters/Nova/Combat/FireGod*.png.meta Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs Tools/Export-FireGodPxo.ps1
git diff --cached --check
git diff --cached --stat
git commit -m "feat: redesign Nova as Fire God Heavenly Demon"
```

Do not stage unrelated dirty files, Unity logs, test result XML, `.vsconfig`, or solution files.

- [ ] **Step 7: Open Unity for the user's manual portrait playtest**

Open the project in Unity `6000.3.25f1`, load `SampleScene`, and leave it ready for Play. Manual checks are the spec's animation alignment, readable overhead statuses, Active/Ultimate clarity at `x1` and `x2`, smooth button/menu behavior, no action stutter, and death disappearance.
