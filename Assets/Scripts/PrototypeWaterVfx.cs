using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class PrototypeWaterVfx : MonoBehaviour
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
    private float arcHeight;

    internal static PrototypeWaterVfx Spawn(
        string resourceName,
        Vector3 position,
        float frameDuration,
        float scale,
        bool loop,
        int sortingOrder)
    {
        return Create(resourceName, position, null, frameDuration, scale, loop, sortingOrder);
    }

    internal static PrototypeWaterVfx SpawnAttached(
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

    internal static PrototypeWaterVfx SpawnTravel(
        string resourceName,
        Vector3 start,
        Vector3 end,
        float duration,
        float scale,
        int sortingOrder,
        float arcHeight = 0f)
    {
        var effect = Create(resourceName, start, null, 0.07f, scale, true, sortingOrder);
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
        effect.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        return effect;
    }

    private static PrototypeWaterVfx Create(
        string resourceName,
        Vector3 position,
        Transform parent,
        float frameDuration,
        float scale,
        bool loop,
        int sortingOrder)
    {
        var sprites = Resources.LoadAll<Sprite>($"VFX/Aurelia/{resourceName}");
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

        var effect = gameObject.AddComponent<PrototypeWaterVfx>();
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

internal sealed class PrototypeHydroBead : MonoBehaviour
{
    private static readonly List<PrototypeHydroBead> Active = new List<PrototypeHydroBead>();
    private PrototypeCombatant source;
    private PrototypeCombatant target;
    private Vector3 start;
    private float elapsed;
    private const float Duration = 0.42f;

    internal static void Spawn(PrototypeCombatant source, PrototypeCombatant target)
    {
        if (source == null || target == null || !source.IsAlive || !target.IsAlive)
        {
            return;
        }

        var domain = PrototypeWaterDomain.FindFor(source);
        if (domain != null)
        {
            PrototypeWaterVfx.SpawnTravel(
                "AureliaHydroBead", source.transform.position, domain.transform.position,
                0.18f, 0.75f, 114, 0.35f);
            domain.AbsorbBead();
            return;
        }

        var gameObject = new GameObject("Hydro Bead");
        gameObject.transform.position = source.transform.position + Vector3.up * 0.45f;
        var bead = gameObject.AddComponent<PrototypeHydroBead>();
        bead.source = source;
        bead.target = target;
        bead.start = gameObject.transform.position;
        PrototypeWaterVfx.SpawnAttached("AureliaHydroBead", gameObject.transform, 0.07f, 0.75f, true, 114);
        Active.Add(bead);
    }

    internal static void AbsorbAll(PrototypeCombatant source, PrototypeWaterDomain domain)
    {
        for (var index = Active.Count - 1; index >= 0; index--)
        {
            var bead = Active[index];
            if (bead != null && bead.source == source)
            {
                bead.Absorb(domain);
            }
        }
    }

    internal static void DestroyOwnedBy(PrototypeCombatant source)
    {
        for (var index = Active.Count - 1; index >= 0; index--)
        {
            var bead = Active[index];
            if (bead != null && bead.source == source)
            {
                Active.RemoveAt(index);
                Destroy(bead.gameObject);
            }
        }
    }

    private void Update()
    {
        if (source == null || target == null || !source.IsAlive || !target.IsAlive)
        {
            Destroy(gameObject);
            return;
        }

        var domain = PrototypeWaterDomain.FindFor(source);
        if (domain != null)
        {
            Absorb(domain);
            return;
        }

        elapsed += Time.deltaTime;
        var progress = Mathf.Clamp01(elapsed / Duration);
        transform.position = Vector3.Lerp(start, target.transform.position + Vector3.up * 0.45f, progress) +
            Vector3.up * (Mathf.Sin(progress * Mathf.PI) * 0.5f);
        if (progress >= 1f)
        {
            source.ResolveHydroBead(target);
            Destroy(gameObject);
        }
    }

    private void Absorb(PrototypeWaterDomain domain)
    {
        PrototypeWaterVfx.SpawnTravel(
            "AureliaHydroBead", transform.position, domain.transform.position,
            0.18f, 0.75f, 114, 0.25f);
        domain.AbsorbBead();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        Active.Remove(this);
    }
}

internal sealed class PrototypeWaterDomain : MonoBehaviour
{
    private static readonly List<PrototypeWaterDomain> Active = new List<PrototypeWaterDomain>();
    private const float Radius = 4.2f;
    private PrototypeCombatant source;
    private PrototypeBattle battle;
    private float remaining;
    private float tickRemaining;
    private float preventedHealing;

    public float Remaining => remaining;

    internal static PrototypeWaterDomain Spawn(
        PrototypeCombatant source,
        PrototypeBattle battle,
        Vector3 position,
        float duration)
    {
        DestroyOwnedBy(source);
        var gameObject = new GameObject("Long Mach Domain");
        gameObject.transform.position = position;
        var domain = gameObject.AddComponent<PrototypeWaterDomain>();
        domain.source = source;
        domain.battle = battle;
        domain.remaining = duration;
        domain.tickRemaining = 0f;
        PrototypeWaterVfx.SpawnAttached("AureliaDomain", gameObject.transform, 0.08f, Radius * 2f, true, 45);
        Active.Add(domain);
        PrototypeHydroBead.AbsorbAll(source, domain);
        return domain;
    }

    internal static PrototypeWaterDomain FindFor(PrototypeCombatant source)
    {
        foreach (var domain in Active)
        {
            if (domain != null && domain.source == source && domain.remaining > 0f)
            {
                return domain;
            }
        }
        return null;
    }

    internal static void DestroyOwnedBy(PrototypeCombatant source)
    {
        for (var index = Active.Count - 1; index >= 0; index--)
        {
            var domain = Active[index];
            if (domain != null && domain.source == source)
            {
                Active.RemoveAt(index);
                Destroy(domain.gameObject);
            }
        }
    }

    internal bool Contains(PrototypeCombatant target)
    {
        return target != null && target.IsAlive && target.Team == source.Team &&
            (target.transform.position - transform.position).sqrMagnitude <= Radius * Radius;
    }

    internal int ReduceDamage(PrototypeCombatant target, int damage)
    {
        if (!Contains(target) || damage <= 0)
        {
            return damage;
        }

        var reduced = Mathf.Max(1, Mathf.RoundToInt(damage * 0.8f));
        preventedHealing += (damage - reduced) * 0.1f;
        var heal = Mathf.FloorToInt(preventedHealing);
        if (heal > 0)
        {
            preventedHealing -= heal;
            source.HealLowestAlly(heal);
        }
        return reduced;
    }

    internal void AbsorbBead()
    {
        remaining += 1f;
        source.BurstWaterDomain();
        PrototypeWaterVfx.Spawn("AureliaCleansingRing", transform.position, 0.055f, 1.6f, false, 113);
    }

    private void Update()
    {
        if (source == null || battle == null || !source.IsAlive || battle.IsFinished)
        {
            Destroy(gameObject);
            return;
        }

        remaining -= Time.deltaTime;
        tickRemaining -= Time.deltaTime;
        if (tickRemaining <= 0f)
        {
            tickRemaining += 0.5f;
            source.TickWaterDomain(this);
        }
        if (remaining <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        Active.Remove(this);
    }
}
