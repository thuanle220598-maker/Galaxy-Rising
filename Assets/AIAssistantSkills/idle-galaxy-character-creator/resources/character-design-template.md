# Character Design Template

Lưu bản đã điền tại `docs/characters/<internal-id>.md`.

## 1. Identity

- Internal ID:
- Display name:
- Title:
- Integration mode: `replace-existing` | `new-slot`
- Existing slot replaced (nếu có):
- Species:
- Class / formation:
- Rarity:
- Element / damage type:
- One-line fantasy:
- Character philosophy:
- Combat loop in one sentence:
- Intended strength:
- Intended weakness / counterplay:

## 2. Lore

### Origin

### Inciting tragedy or vow

### Present goal

### Cost of power

### Faction, location and relationships

### Story details that must appear visually

## 3. Visual Contract

- Primary reference path:
- Reference role: `identity/silhouette` | `pose` | `palette` | `style` | `edit target`
- Reference resolution / alpha:
- Traits that must remain unchanged:
- Allowed lore additions:
- Intended scaling/reconstruction method: `native` | `nearest-neighbor` | `Scale2x` | `redraw`
- Differences from the current asset:
- Silhouette:
- Body proportions:
- Hair shape and elemental streaks:
- Outfit and trailing cloth:
- Story accessories (1-2):
- Raised-hand magic object:
- Main dark color:
- Element accent ramp:
- Glow ramp:
- Metal/accessory color:
- Tinted outline:
- Secondary magic light interaction:
- Negative visual constraints:
- Canon comparison assets:

## 4. Gameplay Data

| Field | Value | Baseline/reason |
| --- | ---: | --- |
| Max Health |  |  |
| Attack |  |  |
| Defense |  |  |
| Move Speed |  |  |
| Attack Interval |  |  |
| Attack Range | derived from class |  |

## 5. Skill Kit

Điền cho Basic, Passive 1-3 nếu dùng, Active và Ultimate.

### [Slot]: [Skill name]

- Player-facing description:
- Gameplay purpose:
- Target / shape / range:
- Damage type and multiplier:
- Cooldown / energy:
- Action duration:
- Hit or release frame:
- Status / duration / cap:
- Combo and consumption rules:
- Counterplay:
- Animation action:
- Telegraph / projectile / impact / residue:
- SFX character:
- Required shared primitive:

## 6. Animation Manifest

| State | Canvas | Frames | FPS | Loop | Duration | Gameplay frame | File |
| --- | --- | ---: | ---: | --- | ---: | ---: | --- |
| Idle | 64x64 |  |  | yes |  | - |  |
| Run | 64x64 |  |  | yes |  | - |  |
| Basic | 64x64 |  |  | no |  |  |  |
| Active | 64x64 |  |  | no |  |  |  |
| Ultimate | 64x64 |  |  | no |  |  |  |
| Hit | 64x64 |  |  | no |  | - |  |
| Death | 64x64 |  |  | no |  | - |  |

Pivot/foot line:

Facing:

## 7. VFX Manifest

| Effect | Trigger | Shape/size | Lifetime | Palette | Reuse/new asset |
| --- | --- | --- | ---: | --- | --- |
| Basic travel |  |  |  |  |  |
| Basic impact |  |  |  |  |  |
| Passive/status |  |  |  |  |  |
| Active |  |  |  |  |  |
| Ultimate |  |  |  |  |  |

Readability budget at `360x640`:

Cleanup rule:

## 8. Integration Checklist

- [ ] Internal ID and replace/new-slot mode confirmed
- [ ] Combatant and SkillDefinition paths chosen
- [ ] Similar hero baselines recorded
- [ ] Every enum/helper caller searched
- [ ] Save/GUID compatibility decision recorded
- [ ] Reference inspected at native size and nearest-neighbor zoom
- [ ] Ref-to-current delta recorded before editing
- [ ] Idle key pose approved before full animation
- [ ] Import settings and frame manifests verified
- [ ] Gameplay hit frames match animation contacts
- [ ] VFX readable at x1/x2 and do not cover HUD
- [ ] Focused Play Mode regression added
- [ ] Full Play Mode suite passes with no Console errors

## 9. Open Decisions

- TBD:

## 10. Acceptance Criteria

- [ ] Lore, silhouette, skills and VFX express the same fantasy
- [ ] Strength and weakness are visible during auto battle
- [ ] No mechanic exists only in description
- [ ] No animation/VFX exists without a gameplay purpose
- [ ] Existing save, roster and combat behavior remain compatible
