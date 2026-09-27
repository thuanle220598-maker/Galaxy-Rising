# Fire God Procedural VFX Rollout Design

## Goal

Apply continuous procedural fire animation to every Fire God Heavenly Demon combat effect while preserving the existing sprite silhouettes, authored timing and gameplay behavior. The result should feel smoother and more organic than frame-only animation without replacing character body sprites or increasing mobile rendering cost excessively.

## Scope

The rollout covers all 25 Fire God VFX resources used by Basic Attack, Ash, Hellfire, Magma, Crimson Gale, Scorch, Firestorm, Flame Shield, Heat and Everburning Embers.

Character body animations remain sprite-based:

- Idle and Combat Idle
- Move
- Basic Attack pose
- Active Skill pose
- Ultimate pose
- Hit
- Death

This work does not change damage, targeting, range, movement, knockback, status effects, action timing, hit timing, global pause, hit-stop or Ultimate Cut-In behavior.

## Chosen Approach

Use the existing sprite animation as an alpha mask and timing source, then render continuous procedural fire inside that mask.

The hybrid approach is preferred over full procedural geometry because each existing effect keeps its recognizable silhouette and contact frame. It is preferred over pre-rendered simulation because shader motion runs at the display frame rate without adding large flipbook textures.

Crimson Gale keeps its two existing battlefield-scale procedural quads. Its five sprite layers also receive the masked procedural treatment so they share the same visual language as the remaining kit.

## Shader Architecture

Extend `GalaxyRising/ProceduralFire` with an optional sprite-mask path:

- `_MainTex` supplies the current sprite frame.
- Sprite alpha clips procedural output to the authored silhouette.
- Sprite RGB remains visible so cyan sparks, white flashes and dark smoke are not flattened into one palette.
- `_UseSpriteMask` selects masked sprite rendering or unmasked quad rendering.
- `_FlowDirection` controls upward, horizontal or radial-feeling noise movement.
- Color, speed, turbulence, intensity and sprite-color contribution remain per-renderer properties.
- `_PreviewTime` remains editor-only in practice and defaults to runtime `_Time`.

The shader uses one additional sprite texture sample and the existing FBM calculation. It does not require VFX Graph, Shader Graph, bloom, camera opaque texture or a custom renderer feature.

## Runtime Integration

`PrototypeFireVfx` remains the single construction path for Fire God sprite effects. Its existing `Create` method applies one shared procedural material and a `MaterialPropertyBlock` configured from the resource name.

The implementation uses a small resource-name switch rather than new ScriptableObjects or a configuration subsystem. All 25 effects map into four visual profiles:

### Basic And Projectile

Resources:

- `FireGodFlameProjectile`
- `FireGodFlameImpact`
- `FireGodEmberProjectile`
- `FireGodEmberIgnite`

Treatment:

- Fast directional flow.
- White-pink core, crimson body and violet outer flame.
- Stronger luminance during impact frames.

### Ash And Detonation

Resources:

- `FireGodAshOne`
- `FireGodAshTwo`
- `FireGodAshSigil`
- `FireGodAshWarning`
- `FireGodAshDetonation`
- `FireGodAshDissolve`

Treatment:

- Slower violet-black turbulence.
- Lower white-core contribution.
- Irregular smoke-like breakup while retaining the authored warning and detonation silhouettes.

### Hellfire And Ground Zones

Resources:

- `FireGodHellfireImpact`
- `FireGodMagmaLoop`
- `FireGodMagmaBurst`
- `FireGodScorchLoop`
- `FireGodFirestormConvert`
- `FireGodFirestormLoop`

Treatment:

- Dense red-magenta base with white-hot pockets.
- Slower broad flow for persistent zones.
- Faster turbulent pulse for conversion and burst effects.

### Ultimate And Persistent Auras

Resources:

- `FireGodCrimsonGaleCore`
- `FireGodCrimsonGaleRibbon`
- `FireGodCrimsonGaleSparks`
- `FireGodCrimsonGaleImpact`
- `FireGodCrimsonGaleResidue`
- `FireGodFlameShieldSpawn`
- `FireGodFlameShieldLoop`
- `FireGodFlameShieldBreak`
- `FireGodHeatAura`

Treatment:

- Crimson Gale preserves its existing full-field composition and smooth quad layers.
- Shield and Heat use slower edge-focused movement with lower intensity so combatants remain readable.
- Impact and break effects receive a short bright pulse without gameplay hit-stop.

## Material Lifetime And Fallback

One shared procedural material is created lazily and reused by all masked sprite effects. Per-effect values use `MaterialPropertyBlock`, preventing one material allocation per cast.

If the shader is missing or unsupported, `PrototypeFireVfx` leaves the standard sprite material intact. The fallback must remain visually usable and must never display Unity's magenta error shader.

The two Crimson Gale quad materials remain instance materials because their phase and lifecycle are already isolated and short-lived.

## Preview Pipeline

Extend the Unity preview exporter to render the actual shader at `540x960` and `60 FPS`. The preview is divided into four labeled sections:

1. Basic, projectile and Ash.
2. Hellfire, Magma and their impacts.
3. Crimson Gale, Scorch and Firestorm.
4. Flame Shield, Heat and Everburning Embers.

Each section shows representative effects at native speed and includes persistent loops long enough to demonstrate continuous motion between sprite frames. Python/OpenCV only adds labels and encodes the Unity-rendered PNG frames; it does not simulate the shader.

The final artifact is `Previews/FireGodAllProceduralVfx.mp4`.

## Performance Constraints

- Target the current Android configuration and URP 2D renderer.
- Reuse one masked-sprite material.
- Add no extra geometry for ordinary effects.
- Keep the two existing Crimson Gale procedural quads as the only battlefield-scale additions.
- Do not enable global post-processing.
- Do not add VFX Graph, Recorder, Blender or new Unity packages.
- Preserve the existing maximum number of simultaneous sprite layers.

## Validation

Automated checks must verify:

- The shader has no Unity compiler messages on a graphics device.
- All 25 Fire God sheets still load with their expected frame counts.
- Every `PrototypeFireVfx` resource receives the intended visual profile.
- Masked procedural rendering produces visible output without a magenta fallback rectangle.
- Two different preview times produce measurably different pixels.
- One-shot effects still self-destruct and persistent loops continue correctly.
- All existing Play Mode tests pass.
- The final preview is `540x960`, `60 FPS` and opens successfully.

Manual review must confirm:

- Health bars and combatants remain readable.
- Persistent effects are less bright than attacks.
- Projectile flow follows travel direction.
- Ground effects do not look like upright rectangular flames.
- No VFX change delays character actions or combat simulation.

## Rollout Order

Implement and preview the profiles in this order:

1. Basic and projectile.
2. Ash and detonation.
3. Hellfire and ground zones.
4. Shield, Heat and Embers.
5. Revalidate Crimson Gale with masked sprite layers enabled.

All profiles are delivered in one implementation pass, followed by one Unity Play Mode run and one complete preview render.
