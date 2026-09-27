# Fire God Heavenly Demon Redesign

## Goal

Replace Nova's temporary identity and shallow kit with the human Fire Mage `Fire God Heavenly Demon`, using the supplied Pixelorama animation source and the full combo design from `skill.txt`.

The result must preserve the current smooth free-movement auto battle while adding the minimum reusable combat primitives required by the authored kit: typed damage, Fire Resistance, stackable Ash Marks, Heat, tracked Fire DoT, persistent zones, knockback, knock-up, Grounded, and decaying movement slow.

## Source Inputs

- Animation source: `D:\thuan_workspace_2\Game\Model\Nova\Nova.pxo`
- Skill source: `D:\thuan_workspace_2\Game\Model\Nova\skill.txt`
- Existing Basic Attack source: `Assets/Art/Characters/Nova/Combat/NovaFlameBasicAttack.png`
- Pixelorama project: version `1.2.3`, PXO version `7`, canvas `68 x 68`, source FPS `10`

The PXO is a ZIP container. Each cel is raw `68 x 68 x 4` RGBA data under `image_data/frames/<frame>/layer_<layer>`, so the editor importer can read it without requiring Pixelorama or another package.

## Identity And Compatibility

- Display name: `Fire God Heavenly Demon`
- Species: `Human`
- Class: `Mage`
- Rarity: retain the existing `SSR` value.
- Base health, attack, defense, move speed, attack interval, internal `PrototypeSkillKit.Nova`, resource filenames, GUIDs, and asset paths remain unchanged for compatibility and to isolate this pass from unrelated balance changes.
- Changing class to Mage changes attack range through the existing class rule to `4.0` world units.
- Runtime objects and UI show the new display name. Internal switch cases may continue to use `Nova`.

Save version increases once. Existing saved references named `Nova` are migrated to `Fire God Heavenly Demon` in progression, owned heroes, and saved squad data. If both names already exist, the migration keeps one canonical record and preserves the highest level, XP, stars, shards, skill levels, and equipment levels instead of duplicating progress. The starter roster uses the new display name after migration.

## Animation Import

Export one horizontal PNG sprite sheet per PXO layer into `Assets/Art/Characters/Nova/Combat/` and generate matching Unity metadata with:

- sprite mode: Multiple
- frame size: `68 x 68`
- pixels per unit: `68`
- filter mode: Point
- compression: None
- mipmaps: disabled
- transparent background preserved
- stable left-to-right frame order

Layer mapping:

| PXO layer | Frames | Runtime use |
| --- | ---: | --- |
| Idle | 8 | Idle loop |
| Walk | 8 | Imported and reserved; not assigned because combat has one movement state |
| Run | 13 | Current movement state |
| Active Skill | 13 | Hellfire Impact cast |
| Ultimate | 13 | Crimson Gale cast |
| Hit | 9 | Hit reaction |
| Death | 9 | Death animation, then existing battlefield removal |

The existing eight-frame flame Basic Attack remains assigned because the PXO has no Basic Attack layer. Imported PXO frames replace the current temporary generated/partial idle, run, active, ultimate, hit, and death arrays.

Playback uses the source `10 FPS` timing for idle, run, hit, and death. Active and Ultimate use all 13 frames over `1.3s`; gameplay impact occurs at frame 9 (`0.8s` after the action starts). Basic Attack remains `0.56s` with its current hit timing near `0.18s`. Animation playback never changes `Time.timeScale` and never pauses other combatants.

## Skill Data Shape

`SkillDefinition` keeps Basic, Active, and Ultimate and gains two optional passive fields beside the existing passive field. Fire God Heavenly Demon fills all three passive entries; existing heroes leave Passive 2 and Passive 3 empty. Skill summaries omit empty entries.

All three passives share the existing `Passive` progression level. No new save fields, upgrade currency rules, or hero-screen buttons are introduced.

The four upgrade multipliers remain the existing Basic, Passive, Active, and Ultimate multipliers. Passive level affects all three passive effects through the current passive attack multiplier; fixed percentages defined below are not separately level-scaled in this prototype.

## Shared Combat Primitives

### Typed Damage

Add `Physical`, `Fire`, and `True` damage types to the shared damage path.

- Physical uses the current attack-minus-defense formula.
- Fire uses the same base formula, then applies Fire Resistance: `round(baseDamage * (1 - fireResistance))`.
- True ignores defense and resistance.
- Fire Resistance is clamped from `-100%` to `90%`; negative resistance increases damage.
- Existing skills default to Physical unless explicitly marked Fire. Existing status damage keeps its current behavior unless its caller supplies Fire or True.

This extends the single existing damage function rather than creating a separate combat framework.

### Ash Mark

Ash is tracked per target and per source because another future Fire Mage must not consume this hero's stacks.

- Maximum: 3 stacks.
- Duration: 5 seconds from the latest application; applying a stack refreshes the full duration.
- Resistance reduction: `5%` Fire Resistance per stack, up to `-15%` from this source.
- Direct Basic, Active impact, and Ultimate wave hits apply one stack after resolving their damage.
- Periodic zone/DoT ticks do not add stacks unless a skill rule explicitly spreads them.
- Consuming Ash removes all stacks from that source and clears the visual.

The overhead status display uses one Ash icon with a small `1`, `2`, or `3` count. At three stacks, the existing procedural effect helpers add a brighter ground ring.

### Heat

Heat belongs to Fire God Heavenly Demon and persists until the current battle or farm wave resets.

- Maximum: 5 stacks.
- Trigger: this hero deals Fire damage to a target that was already Burning, or the damage event is an Ash detonation.
- Each trigger restores `2` energy, equivalent to `2%` of the current `100` maximum.
- Each stack grants `4%` move speed and `5%` Fire damage.
- Heat modifies only this hero and is applied before Fire Resistance.

The hero shows one Heat icon with a stack count and an orange aura whose intensity increases by stack count.

### Tracked Fire DoT

Upgrade the existing Burn state rather than adding a generic status engine. A Burn stores source, damage type, tick damage, tick interval, remaining duration, and remaining total damage. Reapplying Burn keeps the stronger remaining total and refreshes timing; it does not create an unbounded list of DoTs.

When a Fire DoT kills a target, the source receives the kill callback before the target clears statuses. This makes Everburning Embers able to read and spread the remaining damage.

### Movement Statuses And Displacement

- Movement slow changes actual move speed only; it no longer reuses the attack-speed slow field.
- Decaying slow starts at the requested percentage and linearly reaches zero over its duration.
- Knockback moves the target root away from the source over a short visual duration and clamps every step to the existing arena bounds.
- Knock-up keeps the root in place, raises the body visual, and prevents movement/actions for `0.28s`; other combatants continue normally.
- Grounded is a timed status and visible icon that blocks voluntary dash/leap displacement through one shared guard. It does not block enemy knockback or knock-up. Current movement and ordinary attacks are unaffected.

No center line, lane boundary, ranged kiting, collision wall, or global action lock is added.

### Persistent Fire Zones

Use one lightweight runtime zone component with either a circle or directional cone shape. A zone stores owner, team, duration, tick interval, damage, status payload, and geometry. It scans the current opposing team on each `0.5s` tick; the battle contains only five enemies, so no spatial index is needed.

Zones are destroyed when their duration ends or the battle is torn down. Dead units and allies are ignored. Positions and directional vectors are captured at cast impact so a zone does not follow the caster or target.

### Pending Actions

The current single timed action remains, but a combatant may not start a second Basic, Active, or Ultimate while its hit callback is pending. Movement/status updates continue, so this prevents callback overwrite without bringing back the visible skill pause. Delayed explosions and zones use their own lightweight runtime objects/coroutines and never occupy the combatant action slot.

## Complete Skill Kit

### Basic Attack: Flame Strike

- Keep the current eight-frame flame animation, `0.56s` duration, `0.18s` hit timing, `1.0x` power, and `18` energy gain.
- Damage type changes to Fire.
- A normal hit applies one Ash stack after damage.
- If the target already has three Ash stacks when the hit resolves, the attack becomes an empowered `Scorch Burst`: consume all three stacks, deal the normal primary hit, deal `0.45x Attack` Fire splash within `1.5m`, then create a delayed explosion after `0.35s` for `0.75x Attack` Fire damage in the same radius.
- The empowered primary target does not immediately receive a replacement Ash stack; the next direct hit starts a new cycle.
- Both burst stages are tagged as Ash detonation damage for Thermal Resonance.

### Passive 1: Ignition Core

Ignition Core owns the Ash rules above: direct Fire attacks build Ash, each stack reduces Fire Resistance by `5%`, and a Basic Attack against three stacks triggers Scorch Burst. Its VFX uses orbiting embers at stacks one and two, a fire sigil at stack three, and the existing procedural impact sound/effect for the empowered explosion.

### Passive 2: Thermal Resonance

Thermal Resonance owns Heat and Flame Shield.

- Fire damage against a Burning target or Ash detonation restores `2` energy and grants one Heat stack, up to five.
- Heat grants the movement and Fire damage bonuses defined above.
- When health crosses from above to at-or-below `25%`, Flame Shield activates if its `90s` cooldown is ready.
- Shield amount: `12% Max HP + 4% Max HP per current Heat stack`, lasting `6s`.
- The activation knocks enemies within `1.75m` back by `1.2m`.
- The cooldown starts on activation, counts down during the current battle, and resets when a new battle or idle farm wave is initialized.

The shield uses the current shield system and adds a fire shockwave effect; it does not create a second shield type.

### Passive 3: Everburning Embers

When this hero's Fire DoT deals the killing damage:

- Find every living enemy within `4m` of the defeated target.
- Launch one Homing Ember visual toward each target.
- Apply two Ash stacks from this hero, refreshing their duration.
- Apply a Fire Burn whose total remaining damage is `50%` of the defeated target's remaining tracked DoT damage, using the same remaining duration and tick interval.

Spread Burns may trigger Everburning Embers again, but every generation halves the remaining damage, so the chain naturally terminates. Direct-hit kills do not trigger this passive.

### Active: Hellfire Impact

- Cooldown: `6s`.
- Animation: PXO `Active Skill`, `13` frames over `1.3s`; impact on frame 9.
- Target point: the current target position captured at impact.
- Initial area: circle radius `1.8m`.
- Initial damage: `1.45x Attack`, Fire.
- Initial crowd control: knock-up for `0.28s`.
- Each enemy hit by the initial impact receives one Ash stack after damage.

The impact creates a Magma Pool at the captured point:

- Duration: `4s`.
- Tick interval: `0.5s`.
- The first tick occurs immediately on creation, producing exactly eight ticks over the pool lifetime.
- Radius: `1.8m`.
- Tick damage: `0.18x Attack`, Fire DoT.
- Movement slow while affected: `30%`, refreshed for `0.6s` per tick.

If a Magma Pool tick finds an enemy at three Ash stacks from this hero, it consumes the stacks and triggers Magma Detonation once for that stack cycle:

- `0.75x Attack` True Damage.
- `70%` movement slow decaying linearly to zero over `2s`.
- Tagged as Ash detonation for Thermal Resonance.

### Ultimate: Crimson Gale

- Energy cost: the existing full `100` energy.
- Animation: PXO `Ultimate`, `13` frames over `1.3s`; wave release on frame 9.
- Aim: direction from caster to the current target at release; if that target died, reacquire the nearest valid target.
- Geometry: `7m` range and `35` degree half-angle.
- Wave damage: `2.2x Attack`, Fire.
- Knockback: `1.6m` along the cast direction, arena-clamped.
- Each direct wave hit applies one Ash stack after all pre-hit Ash interactions resolve.

For each enemy that had one or more Ash stacks before the wave hit:

- Consume all existing stacks.
- Deal extra Fire detonation damage equal to `10%` of that enemy's missing health per consumed stack.
- Mark this damage as Ash detonation for Thermal Resonance.

Crimson Gale leaves a directional Scorch Zone matching the cone:

- Duration: `5s`.
- Tick interval: `0.5s`.
- Tick damage: `0.12x Attack`, Fire DoT.
- Enemies inside receive Grounded for `0.6s`, refreshed by each tick.

Every active Magma Pool intersected by the cone becomes a Firestorm:

- Radius doubles from `1.8m` to `3.6m`.
- Remaining duration and tick schedule do not reset.
- Future ticks retain Magma Pool damage and combo behavior over the expanded area.
- Conversion happens at most once per pool.

There is no Ultimate Cut-In, cinematic overlay, hit-stop, or global combat pause.

## Presentation

Reuse the existing procedural sprite, line, ring, flash, projectile, damage-number, screen-impact, and audio helpers.

- Ash: small orbiting embers and numeric icon; bright sigil at three stacks.
- Heat: orange-red aura and numeric icon.
- Hellfire Impact: descending fire core, circular blast, cracked magma circle, brief airborne body motion.
- Crimson Gale: layered expanding cone/crescent sprites and ember particles; the ground cone remains as the Scorch Zone.
- Firestorm: larger, brighter Magma Pool with more frequent visual embers, but unchanged tick rate.
- Homing Embers: reuse the current projectile behavior with a small perpendicular arc offset; no new animation system is added.
- Flame Shield: reuse the current barrier plus an expanding orange shockwave.

Heat distortion, custom shaders, authored SFX files, and camera cut-ins are outside this pass. They can be added only after the mechanical and sprite implementation is validated.

## Data Flow

1. An action captures its target point/direction and schedules one hit callback through the existing action timer.
2. The hit builds a typed damage request with source and flags such as direct, Fire DoT, or Ash detonation.
3. The target calculates defense and Fire Resistance, applies shield/health damage, and reports the applied damage and kill state.
4. The source reacts to the result: Heat gain, Ash application/consumption, Everburning spread, and VFX.
5. Persistent zones repeat the same typed damage path every `0.5s`; they do not bypass shields, death handling, damage totals, or battle completion.

All transient Fire God state is cleared by the existing battle/farm-wave reset: Ash ownership, Heat, Flame Shield cooldown, Burn tracking, movement statuses, displacement, delayed explosions, and owned zones.

## Files Expected To Change

- `Assets/Scripts/SkillDefinition.cs`: two optional passive entries and summary/config support.
- `Assets/Scripts/CombatPrototype.cs`: typed damage, Fire God skill behavior, statuses, displacement, zones, effects, and pending-action guard.
- `Assets/Scripts/CombatantDefinition.cs`: no new runtime identity type; only use existing fields and imported frame arrays.
- `Assets/Scripts/PrototypePixelArt.cs`: keep imported-frame fallback behavior and map the new sheets.
- `Assets/Scripts/PrototypeProgression.cs`, `Assets/Scripts/PrototypeGacha.cs`, and squad save ownership in `Assets/Scripts/CombatPrototype.cs`: name migration helpers.
- `Assets/Scripts/PrototypeSaveSystem.cs`: save-version migration orchestration.
- `Assets/Editor/PrototypeContentGenerator.cs`: canonical Fire God identity, complete skill data, and PXO sprite assignment.
- `Assets/Resources/Combatants/Allies/Nova.asset` and `Assets/Resources/Skills/Nova.asset`: generated canonical data.
- `Assets/Art/Characters/Nova/Combat/`: PXO-exported PNG sheets and Unity metadata.
- `Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs`: focused regression coverage.
- `README.md`: identity, animation source, mechanics, save migration, and validation result.

No new package, assembly, scene, prefab system, Animator Controller, Timeline, or third-party runtime is required.

## Testing

Write focused tests before production edits, but defer the Unity import and full Play Mode run until all code and assets are ready, per the current workflow.

Automated coverage must verify:

- Fire God identity is Human, Mage, SSR, range `4.0`, and old `Nova` saves migrate without losing progress/ownership/squad placement.
- Imported frame counts are Idle 8, Run 13, Basic 8, Active 13, Ultimate 13, Hit 9, and Death 9; no frame is an all-transparent placeholder.
- Passive 2 and Passive 3 remain optional for every existing hero and appear in Fire God's summary.
- Physical, Fire, negative Fire Resistance, resistance clamping, and True Damage calculations are deterministic.
- Ash caps at three, refreshes to five seconds, lowers Fire Resistance, is source-owned, and is consumed by each specified combo.
- Scorch Burst executes both AoE stages without overwriting another pending combat action.
- Heat caps at five, modifies move speed/Fire damage, restores energy on valid triggers only, and Flame Shield respects its threshold and `90s` cooldown.
- Burn tracks remaining total damage and Everburning spreads exactly two Ash stacks plus half of the remaining DoT to valid enemies within `4m`.
- Hellfire Impact uses circular range, creates an eight-tick Magma Pool, applies knock-up and movement slow, and triggers True Damage plus decaying slow at three Ash.
- Crimson Gale uses directional cone membership, knockback, missing-health detonation, Scorch Grounded/Burn, and one-time Magma-to-Firestorm conversion.
- Knockback remains arena-clamped, Knock-up does not move the root, and Grounded blocks voluntary displacement without blocking ordinary movement.
- Existing free movement, target-relative ranges, death disappearance, no center line, no ranged kiting, no battle portrait roster, no Ultimate Cut-In, battle speed, and pause controls continue to pass.

Manual portrait validation in `SampleScene` must confirm:

- consistent `68 x 68` alignment and scale across every Fire God state;
- no white/missing idle or attack frame;
- the character faces the target correctly;
- HP, energy, Ash, Heat, Burn, Grounded, and shield visuals do not cover the character action;
- Active and Ultimate read clearly at normal and `x2` speed;
- no button/menu flash regression and no action stutter/global pause;
- death finishes its animation and then removes the character.

Run the complete Play Mode suite once after Unity finishes the final import. The pass is complete only with zero test failures and no Unity console errors.

## Acceptance Criteria

- Every authored passive, Active interaction, Ultimate interaction, and super-combo from `skill.txt` is represented in gameplay rather than description-only text.
- Fire God Heavenly Demon visibly uses the supplied PXO animation layers and retains the approved flame Basic Attack.
- Ash -> Hellfire Impact -> Crimson Gale -> Firestorm is observable and mechanically functional during normal auto battle.
- The new mechanics reuse the current combat loop and effects wherever practical, without weakening the explicitly requested skill depth.
- Existing saves and all previously accepted combat/UI behavior remain compatible.
