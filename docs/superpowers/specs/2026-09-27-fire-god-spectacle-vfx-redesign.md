# Fire God Spectacle VFX Redesign

## Goal

Replace the current simple geometric Fire God effects with fluid, layered pixel-art combat animation inspired by the pacing and color depth of the supplied reference video. Preserve every existing combat rule and damage timing.

## Scope

The first deliverable is one production-quality vertical slice: Crimson Gale, its contact impact and its lingering battlefield residue. A preview video establishes the quality bar before the same visual language is applied to the remaining Fire God effects.

This work does not add Ultimate Cut-In, global hit-stop, global action pause, lanes, ranged kiting or changes to skill mechanics.

## Visual Language

- Use a white-gold core, red and crimson flame body, violet or indigo shadow energy, cyan-white sparks, black smoke and warm embers.
- Preserve hard pixel silhouettes and point filtering. Softness comes from layered transparent sprites and motion, not bilinear filtering.
- Make major attacks occupy a substantial part of the battlefield while keeping combatants, health bars and menu UI readable.
- Build effects from irregular flame, smoke and energy shapes. Avoid circles, straight spokes and symmetric polygons as the dominant silhouette.
- Use directional smears, afterimages, curved ribbons and staggered debris to communicate speed.

## Animation Rhythm

Crimson Gale runs at 24 FPS with 32 frames:

1. Frames 0-5: energy compression and narrow backward pull.
2. Frames 6-13: rapid forward release with overlapping flame ribbons and afterimages.
3. Frames 14-19: white-hot contact, asymmetric shockwave and debris burst.
4. Frames 20-31: fading violet smoke, falling embers and scorched residue.

The damaging event remains controlled by `CombatPrototype`; VFX timing never delays character actions or combat simulation.

## Layered Composition

Crimson Gale uses the existing sprite animator with several independently animated objects:

- `FireGodCrimsonGaleCore`: dense white-gold and crimson traveling body.
- `FireGodCrimsonGaleRibbon`: violet/indigo curved energy surrounding the core.
- `FireGodCrimsonGaleSparks`: cyan-white and gold directional particles.
- `FireGodCrimsonGaleImpact`: contact flash, shockwave and displaced debris.
- `FireGodCrimsonGaleResidue`: smoke, embers and scorched ground recovery.

`PrototypeFireVfx` gains only the reusable presentation controls required by these layers: color tint, alpha fade, scale interpolation, rotation speed and an optional animation curve. Combat code calls one Crimson Gale composition method instead of constructing each layer itself.

## Asset Pipeline

- Extend `Tools/Generate-FireGodVfx.py` rather than introduce another generator.
- Keep deterministic OpenCV generation so frames can be regenerated and reviewed consistently.
- Update `FireGodVfxPostprocessor` with the new sheet dimensions and frame counts.
- Update `Tools/Test-FireGodVfx.py` to validate transparency, dimensions, non-empty frames, palette diversity and stable intended anchors. Remove the old fire-only hue restriction because violet and cyan are now deliberate accents.
- Update the preview builder to show the complete Crimson Gale sequence against a representative battlefield-colored background.

## Rollout

After the Crimson Gale preview is accepted, reuse the same five-layer vocabulary for the remaining effects:

- Basic and Ash: compact core, violet ash shadow, cyan spark accents and short smoke recovery.
- Hellfire and Magma: white-gold heat core, black volcanic smoke, violet edges and molten residue.
- Firestorm: wide red-orange storm body, violet ribbons and intermittent cyan-white electrical accents.
- Flame Shield and passives: thinner layers and lower brightness so persistent effects do not obscure combat.

Each effect may use the frame count needed for smooth motion. Persistent loops target 12-18 frames; attacks target 18-40 frames.

## Validation

- Run the Python asset validator after generating the vertical slice.
- Produce `Previews/FireGodCrimsonGaleSpectacle.mp4` before replacing the remaining sheets.
- After the full VFX pass, run Unity import and Play Mode tests once, then inspect SampleScene at speed x1 and x2.
- Confirm no animation changes combat timing, movement, targeting or damage resolution.

