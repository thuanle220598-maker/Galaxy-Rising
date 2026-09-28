using System;
using UnityEngine;

internal sealed class PrototypeVoidVfx : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private float frameDuration;
    private float elapsed;
    private bool loop;
    private bool travels;
    private Vector3 travelStart;
    private Vector3 travelEnd;
    private float travelDuration;

    internal static PrototypeVoidVfx Spawn(
        string resourceName,
        Vector3 position,
        float frameDuration,
        float scale,
        bool loop,
        int sortingOrder)
    {
        return Create(resourceName, position, null, frameDuration, scale, loop, sortingOrder);
    }

    internal static PrototypeVoidVfx SpawnAttached(
        string resourceName,
        Transform parent,
        float frameDuration,
        float scale,
        bool loop,
        int sortingOrder)
    {
        return Create(
            resourceName,
            parent == null ? Vector3.zero : parent.position,
            parent,
            frameDuration,
            scale,
            loop,
            sortingOrder);
    }

    internal static PrototypeVoidVfx SpawnTravel(
        string resourceName,
        Vector3 start,
        Vector3 end,
        float duration,
        float scale,
        int sortingOrder)
    {
        var effect = Create(resourceName, start, null, 0.06f, scale, true, sortingOrder);
        if (effect == null)
        {
            return null;
        }

        effect.travels = true;
        effect.travelStart = start;
        effect.travelEnd = end;
        effect.travelDuration = Mathf.Max(0.05f, duration);
        var delta = end - start;
        effect.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        return effect;
    }

    private static PrototypeVoidVfx Create(
        string resourceName,
        Vector3 position,
        Transform parent,
        float frameDuration,
        float scale,
        bool loop,
        int sortingOrder)
    {
        var sprites = Resources.LoadAll<Sprite>($"VFX/Kronos/{resourceName}");
        if (sprites == null || sprites.Length == 0)
        {
            return null;
        }

        Array.Sort(sprites, (left, right) => string.CompareOrdinal(left.name, right.name));
        var gameObject = new GameObject(resourceName);
        gameObject.transform.position = position;
        gameObject.transform.localScale = Vector3.one * scale;
        if (parent != null)
        {
            gameObject.transform.SetParent(parent, true);
        }

        var effect = gameObject.AddComponent<PrototypeVoidVfx>();
        effect.frames = sprites;
        effect.frameDuration = Mathf.Max(0.01f, frameDuration);
        effect.loop = loop;
        effect.spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        effect.spriteRenderer.sprite = sprites[0];
        effect.spriteRenderer.sortingOrder = sortingOrder;
        return effect;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (travels)
        {
            var progress = Mathf.Clamp01(elapsed / travelDuration);
            transform.position = Vector3.Lerp(travelStart, travelEnd, progress);
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
