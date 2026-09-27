# P1.0 Combat Animation & Ultimate Spectacle Design

## Goal

Turn the existing functional auto battle into a readable combat spectacle without pausing the simulation, changing the 5v5 rules, or restoring the removed combat roster panels.

## Scope

- Keep ultimate presentation on the battlefield; do not create a hero/skill cut-in overlay.
- Manual pause is the only feature allowed to stop combat.
- Stagger multi-hit and multi-target ultimate impacts instead of resolving every hit in one frame.
- Add ranged projectile travel, action afterimages, impact flash, ground shadows and Y-based sprite sorting.
- Show healing as green floating combat text and pulse a visible ultimate-ready marker above the combatant.
- Add native `x1/x2` speed and pause/resume controls to the battle HUD.
- Preserve on-character HP, energy and status icons. Do not add a bottom portrait roster.

## Architecture

`PrototypeCombatant` remains the gameplay owner. It starts native Unity coroutines for short visual hit sequences and calls the existing combat methods at each timed impact. `PrototypeGameFlow` owns the flash and battle controls because it already owns the safe-area UI. Lightweight projectile and afterimage behaviours live beside the existing effect classes in `CombatPrototype.cs`; no package or animation framework is added.

`SortingGroup` provides depth ordering for each combatant and its child bars/icons. A simple child shadow and the existing sprite frames provide battlefield depth without changing movement or targeting.

## Timing

- Volt Rush hits: three impacts separated by approximately `0.11s`.
- Multi-target barrages: targets separated by approximately `0.06s`.
- Ranged projectile: reaches the captured target position at the existing configured hit delay.
- Impact flash: short alpha fade; no global hit-stop or `Time.timeScale` manipulation.

## Constraints

- Unity `6000.3.25f1`, URP 2D, portrait `360 x 640`.
- No new packages, imported production animation system, Timeline graph, Spine, shader package or save field.
- Do not change combat damage formulas, target selection, free arena movement, stage progression or the 5v5 team size.
- Reset battle speed to `x1` whenever a battle is rebound or unbound.
- Run the complete Play Mode suite once after all code and documentation changes are complete.

## Acceptance

- Ultimate activation does not create a cut-in or pause combat.
- Timed sequences execute one callback per step rather than all callbacks in the same frame.
- Combatants own a shadow, a sorting group and an ultimate-ready visual.
- Battle UI exposes working `x1/x2` and pause/resume controls with at least `44 x 44` touch targets.
- Healing produces readable green floating text.
- The full Play Mode suite passes with zero failures.
