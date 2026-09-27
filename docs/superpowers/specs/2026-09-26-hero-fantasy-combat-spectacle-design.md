# P0.8 Hero Fantasy & Combat Spectacle Design

## Status

Approved on 2026-09-26. This phase is the highest-priority production pass after P0.7.

## Goal

Turn the existing functional combat into a memorable pixel sci-fi vertical slice by giving the five starter heroes and the stage-five boss distinct 64x64 character art, readable animation, signature skill presentation, and a small amount of team-building depth.

P0.8 improves the combat already in the project. It does not replace the battle simulation, add manual skill controls, or expand the roster.

## Selected Approach

Build one polished vertical slice for Nova, Ion, Astra, Lyra, Brakk, and boss Krag before upgrading the remaining roster.

This is preferred over upgrading all twelve heroes at once because it gives the starter experience a complete visual identity without spreading the art and animation budget too thin. It is preferred over a visual-only pass because the existing skills already contain useful mechanics that can support a few visible team synergies.

## Art Direction

- Keep the established pixel sci-fi command-deck identity.
- Render starter combat sprites on a `64 x 64` canvas at `64` pixels per unit.
- Use `FilterMode.Point`, no mipmaps, no texture compression, and pixel-snapped presentation.
- Keep the current 32x32 generator as the fallback for non-starter roster members.
- Generate and cache the starter sprites deterministically in code. Do not add packages or a general-purpose asset pipeline.
- Use a dark outline, one dominant body color, one material highlight, and one skill accent per hero.
- Preserve visual space around the figure: the body should normally occupy 52-58 pixels so weapons and VFX can break the silhouette without clipping.

## Character Identity

### Nova - Solar Vanguard

- Angular armor, broad energy blade, bright solar reactor in the chest.
- Gold and warm white energy with short orange impact embers.
- Pose language: planted melee stance, forward thrust, heavy overhead ultimate.
- Critical-health `Last Light` state brightens the reactor and adds a controlled pulse.

### Ion - Storm Dragon

- Lean dragon-mage silhouette, Tesla-coil horns, charged claws and tail.
- Cyan-white lightning with sharp zigzag paths and brief afterimages.
- Pose language: low stalking run, recoil on cast, body fragments during Phase Shift.
- Chain effects must visibly connect the actual hit targets in order.

### Astra - Astral Oracle

- Slim cosmic silhouette with a broken halo and floating star fragments.
- Mint, white and pale gold; healing rises while shielding closes inward.
- Pose language: hovering idle, open-handed cast, halo expansion on ultimate.
- Heal, shield and cleanse must use different shapes, not only different text.

### Lyra - Helios Ranger

- Asymmetric solar bow, light cape and a narrow ranged silhouette.
- Yellow-white arrows, orange-red burn trails and downward rain lines.
- Pose language: drawn bow anticipation, strong release recoil, upward ultimate shot.
- Burn remains visible as a small, low-noise flame attached to the target.

### Brakk - Stoneheart Guardian

- Wide body, oversized shield, heavy feet and cyan runes in stone armor.
- Blue-white protection effects with square and hexagonal shapes.
- Pose language: braced idle, shield-led movement, ground plant on team protection.
- Guard Link visibly connects Brakk to the protected ally for its opening beat.

### Krag - Stage-Five Boss

- A dedicated boss variant, not only a scaled normal sprite.
- Larger shoulders, gravity core, crown silhouette and orbiting armor fragments.
- Purple-red gravity VFX distinct from Brakk's defensive cyan.
- Clearly telegraph Gravity Bulwark before the barrier appears.
- At half health, change aura and animation cadence to communicate the second phase.

## Animation Language

Add an explicit `Ultimate` animation state while preserving the current action timing model.

Starter and boss targets:

| State | Frames | Purpose |
| --- | ---: | --- |
| Idle | 4 | Breathing, floating parts and readable personality |
| Run | 6 | Weight and class-specific movement |
| Attack | 6 | Anticipation, hit pose and recovery |
| Skill | 8 | Distinct active-skill cast |
| Ultimate | 10 | Signature anticipation, release and recovery |
| Hit | 3 | Directional reaction without hiding the attacker |
| Death | 6 | Clean collapse or dissolve, no abrupt disappearance |

Gameplay effects still execute at `SkillDefinition.HitFrame`. Animation selects the matching pose around that normalized time; visual timing must never delay or duplicate damage.

## Skill Depth

The main depth comes from making existing mechanics legible. Add only interactions that produce a meaningful team-building choice and can be expressed through the current combat state.

### Hero Loops

- **Nova:** build Photon Mark through the third basic hit or Solar Thrust, then detonate Nova's mark with Stellar Breaker for stronger primary impact and splash. The mark is consumed on detonation.
- **Ion:** Arc Sequence and Static Field draw real chain paths. Chains prefer burning targets and gain one additional reduced-power bounce when a burning target is available.
- **Astra:** Guiding Light, Star Ward and Astral Renewal clearly identify the weakest ally. Healing a shielded ally reinforces that shield by a small capped amount instead of wasting the defensive interaction.
- **Lyra:** Sunpiercer applies or refreshes burn. Helios Rain deals its normal hit to all enemies and shows a stronger impact on enemies already burning; it does not add a new permanent stat.
- **Brakk:** Guard Link records the protected ally for the effect duration. The link is visible, and Astra's shield reinforcement can extend the value of this defensive pairing. Stoneheart Pact remains the team-wide defensive climax.

### Team Synergies

1. **Solar Storm:** Lyra supplies burn; Ion's chain finds burning targets and gains one reduced-power bounce.
2. **Living Constellation:** Brakk supplies durable shields; Astra converts healing on shielded allies into a small capped shield reinforcement.

No elemental matrix, combo counter, talent tree, manual targeting, new currency, or save field is added in P0.8.

## Skill Presentation

Replace the generalized five-ray effect for starter signature skills with a small native VFX vocabulary:

- projectile or directional slash;
- target-to-target chain;
- attached status marker;
- ground or area telegraph;
- impact burst;
- shield shell or team barrier;
- short-lived afterimage.

Each effect is configured directly by `PrototypeSkillKit`; do not build a generic node editor or effect graph.

Ultimate sequence:

1. 100-180 ms anticipation with the caster accent and a restrained screen tint.
2. Signature movement or projectile release.
3. Damage/heal resolves at the configured hit frame.
4. A 40-70 ms hit-stop impression is created through animation and effect timing; the battle simulation is not globally paused.
5. Pixel-snapped camera shake and a hero-specific procedural audio motif complete the impact.

Only one large ultimate treatment may dominate the screen at a time. Status effects remain smaller and lower contrast so the 5v5 battle is readable on `360 x 640`.

## Architecture

### `PrototypePixelArt.cs`

- Preserve the current 32x32 fallback generator.
- Route Nova, Ion, Astra, Lyra, Brakk and boss Krag to a focused 64x64 generator.
- Add the `Ultimate` state to `PrototypeAnimationState` and `PrototypeSpriteSet`.
- Keep generated textures cached by identity and state.

### New focused 64x64 generator

- Add one file responsible only for the six bespoke combat silhouettes and their frames.
- Reuse the existing low-level pixel primitives where practical; do not introduce inheritance, factories, or a general animation framework.
- Return the existing `PrototypeSpriteSet` so combat consumers do not need a second rendering API.

### `CombatPrototype.cs`

- Preserve `BeginAction`, `SkillDefinition.ActionDuration`, and `SkillDefinition.HitFrame` as the gameplay clock.
- Track whether the current skill action is an ultimate so animation can select `Ultimate` frames.
- Add minimal helpers for chain paths, attached statuses, guard links, and shield reinforcement.
- Keep skill mechanics inside the existing skill-kit execution flow. Do not rewrite the combat monolith in this phase.

### `SkillDefinition.cs` and resource assets

- Keep the four-skill data model.
- Adjust action duration and hit frame only where the new animation needs a clearer anticipation window.
- Update descriptions so every new interaction is visible in the Heroes screen.
- Add no serialized polymorphism or new skill scripting language.

## Data Flow

1. `CombatantDefinition` selects the skill kit and either imported frames or generated frames.
2. Starter heroes and boss Krag receive cached 64x64 frames; other units use their current fallback.
3. `BeginAction` starts Attack, Skill or Ultimate presentation using the existing duration and hit frame.
4. The animation reaches its impact pose while the existing pending action resolves gameplay once.
5. Skill-kit-specific VFX read the resolved source, targets and statuses to draw paths, marks, links or barriers.
6. HUD health, energy and selected-hero cards continue reading the same combatant state.

## Failure And Fallback Behavior

- Missing bespoke frames fall back to the existing generated frame set for that state.
- Missing optional VFX never blocks damage, healing, targeting or battle completion.
- A dead or invalid chain target stops that bounce without throwing an exception.
- Guard links and attached status visuals destroy themselves when either unit dies, the effect expires, or the battle object is replaced.
- Restarting farm/battle clears new transient combat state; no P0.8 state is saved.

## Performance Boundaries

- Generate textures once per identity and cache them for the process lifetime.
- Use uncompressed 64x64 RGBA textures only for the six detailed identities in this slice.
- Keep simultaneous attached effects bounded by active combatants and statuses.
- Do not add pooling unless profiling shows effect-object churn causing visible frame spikes.
- Target stable presentation on the current low-end Intel UHD 620 development machine and Android-class portrait devices.

## Verification

### Automated

- Extend Play Mode tests to verify the five starters use 64x64 point-filtered sprites and expose an Ultimate frame set.
- Verify each configured action resolves its gameplay exactly once at its hit frame.
- Add focused checks for Nova mark detonation, Ion's burn-aware extra bounce, Astra shield reinforcement, and transient-state reset.
- Preserve the existing `12/12` regression suite and 100-transition soak behavior.

### Visual smoke test

- Run Unity once after all code and test changes are complete.
- Inspect one basic, active, ultimate, hit and death sequence for every starter.
- Inspect Krag's barrier telegraph and half-health phase.
- Check 5v5 readability at `360 x 640`, `720 x 1280`, and `1080 x 1920`.
- Confirm UI remains inside the existing safe area and large VFX do not cover primary navigation.
- Confirm no Console exceptions and no missing sprite references.

## Acceptance Criteria

- The five starter heroes are identifiable by silhouette without reading their names.
- Starter combat sprites are true 64x64 point-filtered frames; non-starters still render correctly through fallback art.
- Basic, active and ultimate actions have distinct anticipation, impact and recovery.
- Nova, Ion, Astra, Lyra and Brakk each expose their existing combat loop through visible state and effect language.
- Solar Storm and Living Constellation synergies work and are described in the relevant skill text.
- Krag reads as a boss, telegraphs its defensive action, and visibly changes at half health.
- Gameplay damage/heal resolves once per action and battle/save/progression behavior does not regress.
- The combined Play Mode suite and 100-transition soak pass after one final Unity validation run.

## Explicitly Deferred

- Remaining seven heroes and later bosses.
- Imported production sprite sheets, Spine, skeletal animation or shader packages.
- Manual ultimate controls, elemental affinity systems and combo meters.
- Skill trees, runes, equipment-set bonuses or new progression currencies.
- Full audio production; P0.8 uses distinct procedural motifs and leaves authored music/SFX for a later production pass.
