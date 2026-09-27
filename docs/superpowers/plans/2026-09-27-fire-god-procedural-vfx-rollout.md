# Fire God Procedural VFX Rollout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Apply smooth masked procedural fire animation to all 25 Fire God Heavenly Demon VFX resources and produce a Unity-rendered preview covering the complete kit.

**Architecture:** Keep every existing sprite sheet as the shape and timing source. Extend `GalaxyRising/ProceduralFire` so a shared material can animate noise inside each sprite's alpha mask, then configure four lightweight visual profiles from the existing `PrototypeFireVfx.Create` path. Reuse the same runtime profile application from the Unity editor preview exporter so the preview cannot drift from gameplay.

**Tech Stack:** Unity 6.3 LTS, C#, URP 2D, ShaderLab/HLSL, NUnit Unity Test Framework, Python 3, OpenCV.

**Spec:** `docs/superpowers/specs/2026-09-27-fire-god-procedural-vfx-rollout-design.md`

## Global Constraints

- Cover all 25 Fire God VFX resources listed in `FireGodVfxPostprocessor`.
- Keep character body animations sprite-based.
- Preserve all damage, targeting, movement, hit timing and action timing.
- Keep the current Android target and URP 2D renderer.
- Use one shared material for ordinary masked sprite effects.
- Add no geometry except the two existing Crimson Gale procedural quads.
- Add no VFX Graph, Recorder, Blender, post-processing or Unity package dependency.
- Fall back to the original sprite material when the shader is missing or unsupported.
- Never show Unity's magenta error material.
- Preserve the user's workflow preference: write all code and tests first, then perform one Unity import/Play Mode run at the end. Static Python and source checks may run before that final Unity pass.
- Preserve free movement, target-relative ranges, no lanes, no ranged kiting, no Ultimate Cut-In and no global hit-stop.

## File Structure

- Modify `Assets/Resources/Shaders/FireGodProceduralFire.shader`: add sprite texture masking, sprite color preservation and directional flow.
- Modify `Assets/Scripts/PrototypeFireVfx.cs`: own the shared material, resource-to-profile mapping and per-renderer property blocks.
- Modify `Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs`: validate all resources receive a supported masked shader and rendered frames animate without fallback magenta.
- Modify `Assets/Editor/FireGodProceduralPreviewExporter.cs`: render four complete-kit preview sections using the runtime material/profile path.
- Create `Tools/Build-FireGodAllProceduralPreview.py`: encode the Unity-rendered frames and label each section/effect.
- Modify `README.md`: document the new hybrid procedural VFX pipeline and final verification result.

---

### Task 1: Lock The Masked-Shader Contract In Play Mode Tests

**Files:**
- Modify: `Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs:512`

**Interfaces:**
- Consumes: existing reflection helpers and `PrototypeFireVfx.Spawn(string, Vector3, float, float, bool, int)`.
- Produces: tests expecting `PrototypeFireVfx.ApplyProceduralMaterial(SpriteRenderer, string, Vector2) -> bool` and the shader properties `_UseSpriteMask`, `_FlowDirection`, `_SpriteContribution`.

- [ ] **Step 1: Add a test covering all 25 resources**

Add this editor Play Mode test after `FireGodVfxSheetsLoadAndOneShotCompletes`:

```csharp
[Test]
public void EveryFireGodSpriteEffectUsesTheMaskedProceduralShader()
{
    var names = new[]
    {
        "FireGodFlameProjectile", "FireGodFlameImpact",
        "FireGodAshOne", "FireGodAshTwo", "FireGodAshSigil",
        "FireGodAshWarning", "FireGodAshDetonation",
        "FireGodHellfireImpact", "FireGodMagmaLoop", "FireGodMagmaBurst",
        "FireGodCrimsonGaleCore", "FireGodCrimsonGaleRibbon",
        "FireGodCrimsonGaleSparks", "FireGodCrimsonGaleImpact",
        "FireGodCrimsonGaleResidue", "FireGodScorchLoop",
        "FireGodFirestormConvert", "FireGodFirestormLoop",
        "FireGodFlameShieldSpawn", "FireGodFlameShieldLoop",
        "FireGodFlameShieldBreak", "FireGodHeatAura",
        "FireGodEmberProjectile", "FireGodEmberIgnite", "FireGodAshDissolve"
    };
    var vfxType = System.Type.GetType("PrototypeFireVfx, Assembly-CSharp");
    var spawn = vfxType.GetMethod("Spawn", BindingFlags.NonPublic | BindingFlags.Static);
    foreach (var name in names)
    {
        var effect = (Component)spawn.Invoke(
            null, new object[] { name, Vector3.zero, 1f, 1f, true, 100 });
        var renderer = effect.GetComponent<SpriteRenderer>();
        var properties = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(properties);
        Assert.That(renderer.sharedMaterial.shader.name,
            Is.EqualTo("GalaxyRising/ProceduralFire"), name);
        Assert.That(properties.GetFloat("_UseSpriteMask"), Is.EqualTo(1f), name);
        Assert.That(properties.GetFloat("_SpriteContribution"), Is.GreaterThan(0f), name);
        Assert.That(properties.GetVector("_FlowDirection").sqrMagnitude,
            Is.GreaterThan(0.5f), name);
        Object.DestroyImmediate(effect.gameObject);
    }
}
```

- [ ] **Step 2: Extend the rendered-frame regression test**

Change `ProceduralFirePreviewRendersVisibleAnimatedFrames` so it calls a new editor method with a resource name:

```csharp
var renderFrame = exporterType.GetMethod(
    "RenderEffectFrame", BindingFlags.NonPublic | BindingFlags.Static);
var first = (Texture2D)renderFrame.Invoke(
    null, new object[] { "FireGodMagmaLoop", 0.25f, 180, 320 });
var later = (Texture2D)renderFrame.Invoke(
    null, new object[] { "FireGodMagmaLoop", 0.85f, 180, 320 });
```

Keep the existing assertions for visible pixels, changed pixels and fewer than 50% fallback-magenta pixels.

- [ ] **Step 3: Record the expected pre-implementation failures**

Do not launch Unity yet. The tests are expected to fail because ordinary effects still use the default sprite material and `RenderEffectFrame` does not exist. This deferred red run is the explicit project exception required by the user's single-import workflow.

- [ ] **Step 4: Commit only the test change**

```powershell
git add -- Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs
git commit -m "test: cover masked procedural fire effects"
```

---

### Task 2: Add Sprite Masking And Directional Flow To The Shader

**Files:**
- Modify: `Assets/Resources/Shaders/FireGodProceduralFire.shader:3`

**Interfaces:**
- Consumes: existing `_PreviewTime`, `_Fade`, color, speed and intensity properties.
- Produces: `_MainTex`, `_UseSpriteMask`, `_SpriteContribution`, `_FlowDirection`; retains unmasked behavior when `_UseSpriteMask == 0`.

- [ ] **Step 1: Add sprite-renderer properties**

Add to `Properties`:

```hlsl
[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
_UseSpriteMask ("Use Sprite Mask", Range(0, 1)) = 0
_SpriteContribution ("Sprite Contribution", Range(0, 1)) = 0
_FlowDirection ("Flow Direction", Vector) = (0, 1, 0, 0)
```

- [ ] **Step 2: Pass vertex color and declare the sprite texture**

Update shader structures and declarations:

```hlsl
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    half4 color : COLOR;
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    half4 color : COLOR;
};
```

Add `_UseSpriteMask`, `_SpriteContribution` and `_FlowDirection` to `UnityPerMaterial`, then copy `input.color` in `Vert`.

- [ ] **Step 3: Rotate the noise domain into the requested flow direction**

At the start of `Frag`, replace direct use of `input.uv` with:

```hlsl
float2 direction = _FlowDirection.xy;
direction = dot(direction, direction) < 0.001 ? float2(0.0, 1.0) : normalize(direction);
float2 tangent = float2(direction.y, -direction.x);
float2 centered = input.uv - 0.5;
float2 uv = float2(dot(centered, tangent), dot(centered, direction)) + 0.5;
```

- [ ] **Step 4: Mask procedural output with the current sprite frame**

Before returning from `Frag`, sample the sprite and combine it with the procedural result:

```hlsl
half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
float mask = lerp(1.0, sprite.a, _UseSpriteMask);
alpha *= mask;
color = lerp(color, max(color, sprite.rgb), _SpriteContribution * sprite.a);
alpha = max(alpha, sprite.a * _SpriteContribution * 0.35) * input.color.a;
return half4(color * _Intensity * input.color.rgb, alpha);
```

The existing quad materials keep `_UseSpriteMask = 0`, so Crimson Gale's procedural rectangles remain unchanged.

- [ ] **Step 5: Run static checks only**

Run:

```powershell
rg -n "_MainTex|_UseSpriteMask|_SpriteContribution|_FlowDirection" Assets/Resources/Shaders/FireGodProceduralFire.shader
git diff --check -- Assets/Resources/Shaders/FireGodProceduralFire.shader
```

Expected: all four properties appear and `git diff --check` exits `0`.

- [ ] **Step 6: Commit the shader change**

```powershell
git add -- Assets/Resources/Shaders/FireGodProceduralFire.shader
git commit -m "feat: support masked procedural fire sprites"
```

---

### Task 3: Apply Four Procedural Profiles From The Shared VFX Factory

**Files:**
- Modify: `Assets/Scripts/PrototypeFireVfx.cs:1`

**Interfaces:**
- Consumes: shader properties from Task 2.
- Produces: `internal static bool ApplyProceduralMaterial(SpriteRenderer renderer, string resourceName, Vector2 flowDirection)`; `private static Material GetMaskedMaterial()`; `private void ConfigureFlowDirection(Vector2 direction)`.

- [ ] **Step 1: Add cached shader property IDs and material state**

Add fields to `PrototypeFireVfx`:

```csharp
private static readonly int UseSpriteMaskId = Shader.PropertyToID("_UseSpriteMask");
private static readonly int SpriteContributionId = Shader.PropertyToID("_SpriteContribution");
private static readonly int FlowDirectionId = Shader.PropertyToID("_FlowDirection");
private static readonly int OuterColorId = Shader.PropertyToID("_OuterColor");
private static readonly int MidColorId = Shader.PropertyToID("_MidColor");
private static readonly int InnerColorId = Shader.PropertyToID("_InnerColor");
private static readonly int HotColorId = Shader.PropertyToID("_HotColor");
private static readonly int SpeedId = Shader.PropertyToID("_Speed");
private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
private static Material maskedMaterial;
private Vector2 proceduralFlowDirection = Vector2.up;
```

- [ ] **Step 2: Implement the lazy shared material with fallback**

```csharp
private static Material GetMaskedMaterial()
{
    if (maskedMaterial != null)
    {
        return maskedMaterial;
    }
    var shader = Resources.Load<Shader>("Shaders/FireGodProceduralFire");
    if (shader == null || !shader.isSupported)
    {
        return null;
    }
    maskedMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
    return maskedMaterial;
}
```

- [ ] **Step 3: Implement the resource-name profile switch**

Add `ApplyProceduralMaterial` and set defaults first:

```csharp
internal static bool ApplyProceduralMaterial(
    SpriteRenderer renderer,
    string resourceName,
    Vector2 flowDirection)
{
    var material = GetMaskedMaterial();
    if (renderer == null || material == null)
    {
        return false;
    }
    var block = new MaterialPropertyBlock();
    renderer.GetPropertyBlock(block);
    block.SetFloat(UseSpriteMaskId, 1f);
    block.SetFloat(SpriteContributionId, 0.45f);
    block.SetVector(FlowDirectionId, flowDirection.sqrMagnitude > 0.001f
        ? flowDirection.normalized : Vector2.up);
    block.SetColor(OuterColorId, new Color(0.06f, 0.005f, 0.14f, 1f));
    block.SetColor(MidColorId, new Color(0.48f, 0.02f, 0.95f, 1f));
    block.SetColor(InnerColorId, new Color(1f, 0.08f, 0.58f, 1f));
    block.SetColor(HotColorId, new Color(1f, 0.82f, 0.95f, 1f));
    block.SetFloat(SpeedId, 1.35f);
    block.SetFloat(IntensityId, 0.9f);
```

Complete the method with one compact switch:

```csharp
    if (resourceName.Contains("Ash"))
    {
        block.SetColor(MidColorId, new Color(0.24f, 0.02f, 0.48f, 1f));
        block.SetColor(InnerColorId, new Color(0.72f, 0.12f, 1f, 1f));
        block.SetFloat(SpeedId, 0.72f);
        block.SetFloat(IntensityId, 0.66f);
        block.SetFloat(SpriteContributionId, 0.62f);
    }
    else if (resourceName.Contains("Magma") || resourceName.Contains("Scorch") ||
        resourceName.Contains("Firestorm") || resourceName.Contains("Hellfire"))
    {
        block.SetColor(OuterColorId, new Color(0.15f, 0.005f, 0.015f, 1f));
        block.SetColor(MidColorId, new Color(0.85f, 0.025f, 0.12f, 1f));
        block.SetColor(InnerColorId, new Color(1f, 0.24f, 0.04f, 1f));
        block.SetColor(HotColorId, new Color(1f, 0.92f, 0.58f, 1f));
        block.SetFloat(SpeedId, resourceName.Contains("Loop") ? 0.62f : 1.18f);
    }
    else if (resourceName.Contains("Shield") || resourceName.Contains("Heat"))
    {
        block.SetFloat(SpeedId, 0.58f);
        block.SetFloat(IntensityId, 0.58f);
        block.SetFloat(SpriteContributionId, 0.7f);
    }
    renderer.sharedMaterial = material;
    renderer.SetPropertyBlock(block);
    return true;
}
```

The remaining names use the Basic/Projectile defaults, including Ember and Crimson Gale sprite layers.

- [ ] **Step 4: Apply the material at the single shared creation point**

At the end of `Create`, after creating `spriteRenderer`:

```csharp
effect.proceduralFlowDirection = Vector2.up;
ApplyProceduralMaterial(effect.spriteRenderer, resourceName, effect.proceduralFlowDirection);
```

This changes all ordinary calls without editing `CombatPrototype` or `PrototypeFireCombat`.

- [ ] **Step 5: Update travel direction without allocating a material**

Add:

```csharp
private void ConfigureFlowDirection(Vector2 direction)
{
    proceduralFlowDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
    if (spriteRenderer != null && spriteRenderer.sharedMaterial == maskedMaterial)
    {
        ApplyProceduralMaterial(spriteRenderer, gameObject.name, proceduralFlowDirection);
    }
}
```

In `SpawnTravel`, after calculating `delta`, call:

```csharp
effect.ConfigureFlowDirection(delta);
```

- [ ] **Step 6: Run source checks only**

Run:

```powershell
rg -n "ApplyProceduralMaterial|GetMaskedMaterial|ConfigureFlowDirection" Assets/Scripts/PrototypeFireVfx.cs
git diff --check -- Assets/Scripts/PrototypeFireVfx.cs
```

Expected: one application point in `Create`, one direction update in `SpawnTravel`, exit `0`.

- [ ] **Step 7: Commit the runtime integration**

```powershell
git add -- Assets/Scripts/PrototypeFireVfx.cs
git commit -m "feat: apply procedural profiles to fire god vfx"
```

---

### Task 4: Render The Complete Kit With The Runtime Profiles

**Files:**
- Modify: `Assets/Editor/FireGodProceduralPreviewExporter.cs:17`

**Interfaces:**
- Consumes: `PrototypeFireVfx.ApplyProceduralMaterial(SpriteRenderer, string, Vector2)` via reflection.
- Produces: `public static void ExportAll()` and `internal static Texture2D RenderEffectFrame(string resourceName, float sourceTime, int width, int height)`.

- [ ] **Step 1: Add four preview sections**

Define section resource arrays:

```csharp
private static readonly string[][] AllSections =
{
    new[] { "FireGodFlameProjectile", "FireGodFlameImpact", "FireGodAshOne",
        "FireGodAshTwo", "FireGodAshSigil", "FireGodAshWarning", "FireGodAshDetonation",
        "FireGodAshDissolve" },
    new[] { "FireGodHellfireImpact", "FireGodMagmaLoop", "FireGodMagmaBurst" },
    new[] { "FireGodCrimsonGaleCore", "FireGodCrimsonGaleRibbon",
        "FireGodCrimsonGaleSparks", "FireGodCrimsonGaleImpact",
        "FireGodCrimsonGaleResidue", "FireGodScorchLoop",
        "FireGodFirestormConvert", "FireGodFirestormLoop" },
    new[] { "FireGodFlameShieldSpawn", "FireGodFlameShieldLoop",
        "FireGodFlameShieldBreak", "FireGodHeatAura",
        "FireGodEmberProjectile", "FireGodEmberIgnite" }
};
```

The four arrays contain all 25 names exactly once.

- [ ] **Step 2: Reuse runtime profile application**

Add a reflection helper:

```csharp
private static void ApplyRuntimeProfile(
    SpriteRenderer renderer,
    string resourceName,
    Vector2 flowDirection)
{
    var type = Type.GetType("PrototypeFireVfx, Assembly-CSharp");
    var method = type.GetMethod(
        "ApplyProceduralMaterial", BindingFlags.NonPublic | BindingFlags.Static);
    if (!(bool)method.Invoke(null, new object[] { renderer, resourceName, flowDirection }))
    {
        throw new InvalidOperationException($"Could not apply profile for {resourceName}.");
    }
}
```

- [ ] **Step 3: Add one-effect rendering for regression tests**

Implement:

```csharp
internal static Texture2D RenderEffectFrame(
    string resourceName,
    float sourceTime,
    int width,
    int height)
```

Use the same camera/RenderTexture cleanup already present in `RenderFrame`. Load and ordinal-sort the resource sprites, choose `Mathf.FloorToInt(sourceTime * 30f) % frames.Length`, create a centered `SpriteRenderer`, call `ApplyRuntimeProfile`, set `_PreviewTime` through its `MaterialPropertyBlock`, render and return the texture.

- [ ] **Step 4: Add the complete preview exporter**

Implement:

```csharp
[MenuItem("Tools/Fire God/Export All Procedural VFX")]
public static void ExportAll()
```

Render each section for `144` frames at `60 FPS`, producing `576` PNG files under `Library/FireGodAllProceduralPreviewFrames`. Use a 2-column grid for sections with up to eight effects. Scale each sprite from its bounds so its panel width is no more than `3.7` world units. Set `_PreviewTime` on every renderer each output frame and advance sprite frames at `30 FPS`.

Log progress every 24 frames and throw immediately when a resource is missing, a frame count is zero or runtime profile application returns `false`.

- [ ] **Step 5: Keep the existing Crimson-only exporter working**

Do not remove `Export()` or `RenderFrame(float, int, int)`. Both continue producing `FireGodCrimsonGaleProcedural.mp4` inputs and use unmasked quad rendering.

- [ ] **Step 6: Run source checks only**

Run:

```powershell
rg -n "ExportAll|RenderEffectFrame|AllSections|ApplyRuntimeProfile" Assets/Editor/FireGodProceduralPreviewExporter.cs
git diff --check -- Assets/Editor/FireGodProceduralPreviewExporter.cs
```

Expected: all new interfaces appear, exit `0`.

- [ ] **Step 7: Commit the editor exporter**

```powershell
git add -- Assets/Editor/FireGodProceduralPreviewExporter.cs
git commit -m "feat: export complete procedural vfx preview"
```

---

### Task 5: Encode And Label The Full-Kit Preview

**Files:**
- Create: `Tools/Build-FireGodAllProceduralPreview.py`
- Modify: `README.md:24`

**Interfaces:**
- Consumes: `Library/FireGodAllProceduralPreviewFrames/frame_000.png` through `frame_575.png`.
- Produces: `Previews/FireGodAllProceduralVfx.mp4` at `540x960`, `60 FPS`, `9.6` seconds.

- [ ] **Step 1: Create the encoder**

Create a Python script with these exact constants:

```python
ROOT = Path(__file__).resolve().parents[1]
FRAMES = ROOT / "Library/FireGodAllProceduralPreviewFrames"
OUTPUT = ROOT / "Previews/FireGodAllProceduralVfx.mp4"
FPS = 60
SECTION_FRAMES = 144
SECTIONS = (
    "BASIC / PROJECTILE / ASH",
    "HELLFIRE / MAGMA",
    "ULTIMATE / SCORCH / FIRESTORM",
    "SHIELD / HEAT / EMBERS",
)
```

Require all `576` frame paths, initialize `cv2.VideoWriter` with `mp4v`, add the current section title and `60 FPS UNITY PROCEDURAL RENDER` to each frame, then write every frame in order.

- [ ] **Step 2: Add a runnable metadata check**

After `writer.release()`, reopen the MP4 and assert:

```python
capture = cv2.VideoCapture(str(OUTPUT))
assert int(capture.get(cv2.CAP_PROP_FRAME_COUNT)) == 576
assert capture.get(cv2.CAP_PROP_FPS) == 60
assert int(capture.get(cv2.CAP_PROP_FRAME_WIDTH)) == 540
assert int(capture.get(cv2.CAP_PROP_FRAME_HEIGHT)) == 960
capture.release()
```

- [ ] **Step 3: Update README pipeline documentation**

Document that all 25 Fire God sheets use their sprite alpha as a procedural mask, ordinary effects share one material, and the final full-kit review artifact is `Previews/FireGodAllProceduralVfx.mp4`.

- [ ] **Step 4: Run Python checks**

Run:

```powershell
python -m py_compile Tools/Build-FireGodAllProceduralPreview.py
git diff --check -- Tools/Build-FireGodAllProceduralPreview.py README.md
```

Expected: exit `0`.

- [ ] **Step 5: Commit the encoder and docs**

```powershell
git add -- Tools/Build-FireGodAllProceduralPreview.py README.md
git commit -m "tools: build full procedural vfx preview"
```

---

### Task 6: Perform The Single Unity Import, Test And Preview Pass

**Files:**
- Verify: `Assets/Resources/Shaders/FireGodProceduralFire.shader`
- Verify: `Assets/Scripts/PrototypeFireVfx.cs`
- Verify: `Assets/Editor/FireGodProceduralPreviewExporter.cs`
- Verify: `Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs`
- Generate: `Previews/FireGodAllProceduralVfx.mp4`
- Generate: `TestResults/FireGodAllProceduralVfxPlayMode.xml`
- Generate: `Logs/FireGodAllProceduralVfxPlayMode.log`

**Interfaces:**
- Consumes: all completed tasks.
- Produces: verified Unity test results and the user-reviewable final preview.

- [ ] **Step 1: Run final static checks in parallel**

Run:

```powershell
python -m py_compile Tools/Build-FireGodVfxPreview.py Tools/Build-FireGodProceduralPreview.py Tools/Build-FireGodAllProceduralPreview.py Tools/Test-FireGodVfx.py
python Tools/Test-FireGodVfx.py
git diff --check
```

Expected: Python compiles, asset validator reports `25 sheets`, diff check exits `0` apart from line-ending notices.

- [ ] **Step 2: Run the complete Unity Play Mode suite once with a graphics device**

Run without `-nographics`:

```powershell
& 'D:\Program Files\Unity\Editor\6000.3.25f1\Editor\Unity.exe' `
  -batchmode `
  -projectPath 'D:\thuan_workspace_2\Game\Galaxy-Rising' `
  -runTests -testPlatform PlayMode `
  -testResults 'D:\thuan_workspace_2\Game\Galaxy-Rising\TestResults\FireGodAllProceduralVfxPlayMode.xml' `
  -logFile 'D:\thuan_workspace_2\Game\Galaxy-Rising\Logs\FireGodAllProceduralVfxPlayMode.log'
```

Do not add `-quit`; the test runner exits after writing results. Wait for the Unity process to terminate, then parse `/test-run` and require `failed=0` and `skipped=0`.

- [ ] **Step 3: Verify shader compiler output**

Run:

```powershell
Select-String `
  -LiteralPath 'Logs\FireGodAllProceduralVfxPlayMode.log' `
  -Pattern "Shader error in 'GalaxyRising/ProceduralFire'|error CS|Compilation failed" `
  -CaseSensitive:$false
```

Expected: no matching lines.

- [ ] **Step 4: Export all Unity-rendered frames**

Run:

```powershell
& 'D:\Program Files\Unity\Editor\6000.3.25f1\Editor\Unity.exe' `
  -batchmode `
  -projectPath 'D:\thuan_workspace_2\Game\Galaxy-Rising' `
  -executeMethod FireGodProceduralPreviewExporter.ExportAll `
  -logFile 'D:\thuan_workspace_2\Game\Galaxy-Rising\Logs\FireGodAllProceduralPreviewExport.log' `
  -quit
```

Expected: `576` PNG files under `Library/FireGodAllProceduralPreviewFrames` and exit `0`.

- [ ] **Step 5: Encode and validate the MP4**

Run:

```powershell
python Tools/Build-FireGodAllProceduralPreview.py
```

Expected: `Previews/FireGodAllProceduralVfx.mp4`, `576` frames, `60 FPS`, `540x960`, `9.6` seconds.

- [ ] **Step 6: Inspect representative frames**

Build a contact sheet from frames `0`, `48`, `96`, `143`, `144`, `216`, `287`, `288`, `360`, `431`, `432`, `504`, `575`. Confirm no full-panel magenta fallback, every section contains its expected effects and persistent effects visibly change between samples.

- [ ] **Step 7: Open the final preview**

Run:

```powershell
Start-Process (Resolve-Path 'Previews\FireGodAllProceduralVfx.mp4')
```

- [ ] **Step 8: Commit final generated and verification-facing changes**

Stage only files owned by this rollout; do not stage unrelated dirty-worktree changes:

```powershell
git add -- `
  Assets/Resources/Shaders/FireGodProceduralFire.shader `
  Assets/Scripts/PrototypeFireVfx.cs `
  Assets/Editor/FireGodProceduralPreviewExporter.cs `
  Assets/Tests/PlayMode/BattleHomeUiPlayModeTests.cs `
  Tools/Build-FireGodAllProceduralPreview.py `
  Previews/FireGodAllProceduralVfx.mp4 `
  README.md
git commit -m "feat: roll procedural fire across fire god vfx"
```

Do not commit `Library`, `Logs` or `TestResults`.
