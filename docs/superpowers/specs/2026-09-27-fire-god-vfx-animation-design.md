# Fire God VFX Animation Design

## Goal

Replace Fire God Heavenly Demon's generic tinted-square effects with readable pixel-art animation that matches the existing 68x68 character, preserves smooth combat and exposes every skill interaction visually.

## Visual Rules

- Reuse the character palette: black, deep crimson, hot red, orange-white and warm skin highlights.
- Hard pixel edges only; no bilinear filtering, gradients or independently generated AI frames.
- Keep character animation on the current 68x68 canvas and fixed bottom-center pivot.
- Use separate transparent VFX sheets so passive procs never interrupt the character action state.
- Animate at 10-15 FPS. Every one-shot has anticipation, impact and recovery; loops have no visible seam.
- No Ultimate Cut-In, global hit-stop, global action pause, lanes or ranged kiting.

## Asset Set

### Basic and Ash

- Flame projectile loop: 8 frames at 48x48.
- Flame impact: 8 frames at 64x64.
- Ash Mark 1 and 2 orbit loops: 8 frames at 32x32.
- Three-stack Ash sigil: 8 frames at 64x64.
- Empowered warning ring: 6 frames at 64x64.
- Ash detonation: 10 frames at 96x96.

### Hellfire Impact and Magma

- Hellfire impact: 12 frames at 128x128.
- Magma Pool loop: 8 frames at 128x128.
- Magma Ash burst: 10 frames at 128x128.

### Crimson Gale and Fire Zones

- Crimson Gale wave: 14 frames at 192x128.
- Scorch Zone loop: 8 frames at 192x128.
- Firestorm conversion: 10 frames at 192x192.
- Firestorm loop: 8 frames at 192x192.

### Passives

- Flame Shield spawn, loop and break.
- Everburning ash dissolve, homing ember and recipient ignition.
- Heat uses one reusable aura loop whose scale, opacity and emission increase with stack count.

## Integration

Sheets live under `Assets/Resources/VFX/FireGod`. A dedicated importer slices frames with point filtering and centered pivots. Runtime effects load by resource name and use a small non-looping/looping sprite animator; existing combat timing remains authoritative for damage.

## Validation

- Asset checks enforce dimensions, frame counts, transparency, palette family and stable visual centers.
- Unity Play Mode tests verify resources load, loops wrap and one-shots destroy themselves.
- Final manual verification runs SampleScene at speed x1 and x2.

