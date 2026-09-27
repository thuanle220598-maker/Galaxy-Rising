using System;
using UnityEngine;

internal sealed class PrototypeFireVfx : MonoBehaviour
{
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

    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private float frameDuration;
    private float elapsed;
    private bool loop;
    private bool travels;
    private Vector3 travelStart;
    private Vector3 travelEnd;
    private float travelDuration;
    private float arcHeight;
    private Vector3 baseScale;
    private Color tint = Color.white;
    private float startScale = 1f;
    private float endScale = 1f;
    private float startAlpha = 1f;
    private float endAlpha = 1f;
    private float rotationSpeed;
    private Vector2 proceduralFlowDirection = Vector2.up;
    private AnimationCurve progressCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    internal static PrototypeFireVfx Spawn(
        string resourceName,
        Vector3 position,
        float frameDuration,
        float scale,
        bool loop,
        int sortingOrder)
    {
        return Create(resourceName, position, null, frameDuration, scale, loop, sortingOrder, 0f);
    }

    internal static PrototypeFireVfx SpawnAttached(
        string resourceName,
        Transform parent,
        float frameDuration,
        float scale,
        bool loop,
        int sortingOrder)
    {
        var effect = Create(
            resourceName,
            parent == null ? Vector3.zero : parent.position,
            parent,
            frameDuration,
            scale,
            loop,
            sortingOrder,
            0f);
        return effect;
    }

    internal static PrototypeFireVfx SpawnTravel(
        string resourceName,
        Vector3 start,
        Vector3 end,
        float duration,
        float scale,
        int sortingOrder,
        float arcHeight = 0f)
    {
        var effect = Create(resourceName, start, null, 0.07f, scale, true, sortingOrder, 0f);
        if (effect == null)
        {
            return null;
        }

        effect.travels = true;
        effect.travelStart = start;
        effect.travelEnd = end;
        effect.travelDuration = Mathf.Max(0.05f, duration);
        effect.arcHeight = arcHeight;
        var delta = end - start;
        effect.ConfigureFlowDirection(delta);
        effect.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        return effect;
    }

    internal static GameObject SpawnCrimsonGale(Vector3 origin, Vector3 direction)
    {
        const float duration = 48f / 30f;
        var root = new GameObject("FireGodCrimsonGaleSpectacle");
        root.transform.position = origin + direction * 3.5f;
        root.transform.rotation = Quaternion.Euler(
            0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        PrototypeFireFlowVfx.Spawn(
            root.transform, new Vector3(0f, 0.45f, 0f), new Vector2(8.8f, 5.4f),
            0.3f, 0.72f, 108, duration);
        PrototypeFireFlowVfx.Spawn(
            root.transform, new Vector3(0.25f, 0.18f, 0f), new Vector2(7.2f, 4.45f),
            4.7f, 1.05f, 111, duration);
        SpawnCrimsonGaleLayer(root.transform, "FireGodCrimsonGaleResidue", 109, 1f, 1.06f, 0.9f, 0f, 0f);
        SpawnCrimsonGaleLayer(root.transform, "FireGodCrimsonGaleRibbon", 110, 0.92f, 1.08f, 0.8f, 0f, -8f);
        SpawnCrimsonGaleLayer(root.transform, "FireGodCrimsonGaleCore", 112, 0.96f, 1.04f, 1f, 0.15f, 0f);
        SpawnCrimsonGaleLayer(root.transform, "FireGodCrimsonGaleSparks", 113, 0.9f, 1.12f, 1f, 0f, 5f);
        SpawnCrimsonGaleLayer(root.transform, "FireGodCrimsonGaleImpact", 114, 0.9f, 1.1f, 1f, 0f, 0f);
        Destroy(root, duration + 0.1f);
        return root;
    }

    internal PrototypeFireVfx ConfigurePresentation(
        Color color,
        float fromScale,
        float toScale,
        float fromAlpha,
        float toAlpha,
        float degreesPerSecond,
        AnimationCurve curve = null)
    {
        tint = color;
        startScale = fromScale;
        endScale = toScale;
        startAlpha = fromAlpha;
        endAlpha = toAlpha;
        rotationSpeed = degreesPerSecond;
        progressCurve = curve ?? AnimationCurve.Linear(0f, 0f, 1f, 1f);
        return this;
    }

    private static void SpawnCrimsonGaleLayer(
        Transform parent,
        string resourceName,
        int sortingOrder,
        float fromScale,
        float toScale,
        float fromAlpha,
        float toAlpha,
        float rotationSpeed)
    {
        var layer = SpawnAttached(resourceName, parent, 1f / 30f, 2.35f, false, sortingOrder);
        if (layer == null)
        {
            return;
        }

        layer.transform.localPosition = Vector3.zero;
        layer.transform.localRotation = Quaternion.identity;
        layer.ConfigurePresentation(
            Color.white, fromScale, toScale, fromAlpha, toAlpha, rotationSpeed);
    }

    private static PrototypeFireVfx Create(
        string resourceName,
        Vector3 position,
        Transform parent,
        float frameDuration,
        float scale,
        bool loop,
        int sortingOrder,
        float rotation)
    {
        var sprites = Resources.LoadAll<Sprite>($"VFX/FireGod/{resourceName}");
        if (sprites == null || sprites.Length == 0)
        {
            return null;
        }

        Array.Sort(sprites, (left, right) => string.CompareOrdinal(left.name, right.name));
        var gameObject = new GameObject(resourceName);
        gameObject.transform.position = position;
        gameObject.transform.localScale = Vector3.one * scale;
        gameObject.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        if (parent != null)
        {
            gameObject.transform.SetParent(parent, true);
        }

        var effect = gameObject.AddComponent<PrototypeFireVfx>();
        effect.frames = sprites;
        effect.frameDuration = Mathf.Max(0.01f, frameDuration);
        effect.loop = loop;
        effect.baseScale = gameObject.transform.localScale;
        effect.spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        effect.spriteRenderer.sprite = sprites[0];
        effect.spriteRenderer.sortingOrder = sortingOrder;
        effect.proceduralFlowDirection = Vector2.up;
        ApplyProceduralMaterial(
            effect.spriteRenderer, resourceName, effect.proceduralFlowDirection);
        return effect;
    }

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

        var flow = flowDirection.sqrMagnitude > 0.001f
            ? flowDirection.normalized
            : Vector2.up;
        var block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetFloat(UseSpriteMaskId, 1f);
        block.SetFloat(SpriteContributionId, 0.45f);
        block.SetVector(FlowDirectionId, new Vector4(flow.x, flow.y, 0f, 0f));
        block.SetColor(OuterColorId, new Color(0.06f, 0.005f, 0.14f, 1f));
        block.SetColor(MidColorId, new Color(0.48f, 0.02f, 0.95f, 1f));
        block.SetColor(InnerColorId, new Color(1f, 0.08f, 0.58f, 1f));
        block.SetColor(HotColorId, new Color(1f, 0.82f, 0.95f, 1f));
        block.SetFloat(SpeedId, 1.35f);
        block.SetFloat(IntensityId, 0.9f);

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

    private void ConfigureFlowDirection(Vector2 direction)
    {
        proceduralFlowDirection = direction.sqrMagnitude > 0.001f
            ? direction.normalized
            : Vector2.right;
        if (spriteRenderer != null && spriteRenderer.sharedMaterial == maskedMaterial)
        {
            ApplyProceduralMaterial(spriteRenderer, gameObject.name, proceduralFlowDirection);
        }
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        var animationDuration = frameDuration * frames.Length;
        var normalizedTime = loop
            ? elapsed % animationDuration / animationDuration
            : Mathf.Clamp01(elapsed / animationDuration);
        var presentationTime = progressCurve.Evaluate(normalizedTime);
        transform.localScale = baseScale * Mathf.Lerp(startScale, endScale, presentationTime);
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        var alpha = Mathf.Lerp(startAlpha, endAlpha, presentationTime) * tint.a;
        spriteRenderer.color = new Color(tint.r, tint.g, tint.b, alpha);

        if (travels)
        {
            var progress = Mathf.Clamp01(elapsed / travelDuration);
            transform.position = Vector3.Lerp(travelStart, travelEnd, progress) +
                Vector3.up * (Mathf.Sin(progress * Mathf.PI) * arcHeight);
            if (progress >= 1f)
            {
                Destroy(gameObject);
                return;
            }
        }

        var rawFrame = Mathf.FloorToInt(elapsed / frameDuration);
        if (!loop && rawFrame >= frames.Length)
        {
            Destroy(gameObject);
            return;
        }

        spriteRenderer.sprite = frames[loop ? rawFrame % frames.Length : rawFrame];
    }
}
