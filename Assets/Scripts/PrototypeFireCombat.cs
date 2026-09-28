using System;
using UnityEngine;

internal enum PrototypeDamageType
{
    Physical,
    Fire,
    True,
    Magic
}

[Flags]
internal enum PrototypeDamageFlags
{
    None = 0,
    Direct = 1,
    DamageOverTime = 2,
    AshDetonation = 4
}

internal readonly struct PrototypeDamageResult
{
    public readonly int AppliedDamage;
    public readonly bool Killed;
    public readonly bool TargetWasBurning;

    public PrototypeDamageResult(int appliedDamage, bool killed, bool targetWasBurning)
    {
        AppliedDamage = appliedDamage;
        Killed = killed;
        TargetWasBurning = targetWasBurning;
    }
}

internal static class PrototypeFireCombat
{
    internal static int CalculateDamage(
        int attack,
        int defense,
        float multiplier,
        PrototypeDamageType damageType,
        float fireResistance,
        float defenseIgnore = 0f)
    {
        var raw = Mathf.RoundToInt(attack * multiplier);
        if (damageType == PrototypeDamageType.True)
        {
            return Mathf.Max(1, raw);
        }

        var effectiveDefense = Mathf.RoundToInt(defense * (1f - Mathf.Clamp01(defenseIgnore)));
        var baseDamage = Mathf.Max(1, raw - effectiveDefense);
        return damageType == PrototypeDamageType.Fire
            ? Mathf.Max(1, Mathf.RoundToInt(baseDamage * (1f - Mathf.Clamp(fireResistance, -1f, 0.9f))))
            : baseDamage;
    }

    internal static int RemainingDotDamage(int tickDamage, float remaining, float tickInterval)
    {
        return tickDamage <= 0 || remaining <= 0f
            ? 0
            : tickDamage * Mathf.CeilToInt(remaining / Mathf.Max(0.01f, tickInterval));
    }

    internal static float DecayingSlow(float start, float duration, float elapsed)
    {
        return Mathf.Lerp(
            Mathf.Clamp01(start),
            0f,
            Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration)));
    }

    internal static bool PointInCircle(Vector3 point, Vector3 center, float radius)
    {
        var delta = point - center;
        delta.z = 0f;
        return delta.sqrMagnitude <= radius * radius;
    }

    internal static bool PointInCone(
        Vector3 point,
        Vector3 origin,
        Vector3 direction,
        float range,
        float halfAngle)
    {
        var delta = point - origin;
        delta.z = 0f;
        return delta.sqrMagnitude <= range * range && delta.sqrMagnitude > 0.0001f &&
            Vector3.Angle(direction, delta) <= halfAngle;
    }

    internal static bool CircleIntersectsCone(
        Vector3 center,
        float radius,
        Vector3 origin,
        Vector3 direction,
        float range,
        float halfAngle)
    {
        var forward = direction.normalized;
        if (PointInCone(center, origin, forward, range, halfAngle) || PointInCircle(origin, center, radius))
        {
            return true;
        }

        var left = origin + Quaternion.Euler(0f, 0f, halfAngle) * forward * range;
        var right = origin + Quaternion.Euler(0f, 0f, -halfAngle) * forward * range;
        if (DistanceToSegment(center, origin, left) <= radius ||
            DistanceToSegment(center, origin, right) <= radius)
        {
            return true;
        }

        var delta = center - origin;
        delta.z = 0f;
        return delta.sqrMagnitude > 0.0001f &&
            Mathf.Abs(delta.magnitude - range) <= radius &&
            Vector3.Angle(forward, delta) <= halfAngle;
    }

    internal static int MissingHealthDetonation(int maxHealth, int currentHealth, int ashStacks)
    {
        return Mathf.RoundToInt(
            Mathf.Max(0, maxHealth - currentHealth) * 0.1f * Mathf.Clamp(ashStacks, 0, 3));
    }

    internal static int FlameShieldAmount(int maxHealth, int heatStacks)
    {
        return Mathf.RoundToInt(maxHealth * (0.12f + Mathf.Clamp(heatStacks, 0, 5) * 0.04f));
    }

    private static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        var segment = end - start;
        var lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= 0.0001f)
        {
            return Vector3.Distance(point, start);
        }

        var t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
        return Vector3.Distance(point, start + segment * t);
    }
}

internal enum PrototypeFireZoneKind
{
    Magma,
    Scorch
}

internal sealed class PrototypeFireZone : MonoBehaviour
{
    private PrototypeCombatant owner;
    private PrototypeBattle battle;
    private PrototypeFireZoneKind kind;
    private Vector3 origin;
    private Vector3 direction;
    private float duration;
    private float remainingDuration;
    private float tickInterval;
    private float tickCooldown;
    private float radius;
    private float range;
    private float halfAngle;
    private float multiplier;
    private int remainingTickCount;
    private bool converted;
    private Transform visualRoot;
    private PrototypeFireVfx animatedVisual;

    public float Radius => radius;
    public float RemainingDuration => remainingDuration;
    public int RemainingTickCount => remainingTickCount;

    internal static PrototypeFireZone SpawnMagma(
        PrototypeCombatant owner,
        PrototypeBattle battle,
        Sprite sprite,
        Vector3 center,
        float radius,
        float duration,
        float tickInterval,
        float multiplier)
    {
        var zone = Create(owner, battle, sprite, PrototypeFireZoneKind.Magma, center, Vector3.right,
            radius, 0f, 0f, duration, tickInterval, multiplier);
        zone.Tick();
        zone.tickCooldown = tickInterval;
        return zone;
    }

    internal static PrototypeFireZone SpawnScorch(
        PrototypeCombatant owner,
        PrototypeBattle battle,
        Sprite sprite,
        Vector3 origin,
        Vector3 direction,
        float range,
        float halfAngle,
        float duration,
        float tickInterval,
        float multiplier)
    {
        return Create(owner, battle, sprite, PrototypeFireZoneKind.Scorch, origin, direction,
            0f, range, halfAngle, duration, tickInterval, multiplier);
    }

    internal void ConvertToFirestorm()
    {
        if (kind != PrototypeFireZoneKind.Magma || converted)
        {
            return;
        }

        converted = true;
        radius *= 2f;
        if (animatedVisual != null)
        {
            Destroy(animatedVisual.gameObject);
            PrototypeFireVfx.SpawnAttached(
                "FireGodFirestormConvert", transform, 0.08f, radius * 2f / 3f, false, -18);
            animatedVisual = PrototypeFireVfx.SpawnAttached(
                "FireGodFirestormLoop", transform, 0.1f, radius * 2f / 3f, true, -19);
            return;
        }
        if (visualRoot != null)
        {
            visualRoot.localScale *= 2f;
            foreach (var renderer in visualRoot.GetComponentsInChildren<SpriteRenderer>())
            {
                renderer.color = new Color(1f, 0.2f, 0.02f, 0.48f);
            }
        }
    }

    internal static void ConvertIntersectingMagmaPools(
        PrototypeCombatant owner,
        Vector3 origin,
        Vector3 direction,
        float range,
        float halfAngle)
    {
        foreach (var zone in FindObjectsByType<PrototypeFireZone>(FindObjectsSortMode.None))
        {
            if (zone.owner == owner && zone.kind == PrototypeFireZoneKind.Magma &&
                PrototypeFireCombat.CircleIntersectsCone(
                    zone.origin, zone.radius, origin, direction, range, halfAngle))
            {
                zone.ConvertToFirestorm();
            }
        }
    }

    internal static void DestroyOwnedBy(PrototypeCombatant owner)
    {
        foreach (var zone in FindObjectsByType<PrototypeFireZone>(FindObjectsSortMode.None))
        {
            if (zone.owner == owner)
            {
                Destroy(zone.gameObject);
            }
        }
    }

    private static PrototypeFireZone Create(
        PrototypeCombatant owner,
        PrototypeBattle battle,
        Sprite sprite,
        PrototypeFireZoneKind kind,
        Vector3 origin,
        Vector3 direction,
        float radius,
        float range,
        float halfAngle,
        float duration,
        float tickInterval,
        float multiplier)
    {
        var gameObject = new GameObject(kind == PrototypeFireZoneKind.Magma ? "Magma Pool" : "Scorch Zone");
        gameObject.transform.position = origin;
        var zone = gameObject.AddComponent<PrototypeFireZone>();
        zone.owner = owner;
        zone.battle = battle;
        zone.kind = kind;
        zone.origin = origin;
        zone.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;
        zone.radius = radius;
        zone.range = range;
        zone.halfAngle = halfAngle;
        zone.duration = duration;
        zone.remainingDuration = duration;
        zone.tickInterval = Mathf.Max(0.05f, tickInterval);
        zone.tickCooldown = zone.tickInterval;
        zone.multiplier = multiplier;
        zone.remainingTickCount = Mathf.CeilToInt(duration / zone.tickInterval);
        zone.CreateVisual(sprite);
        return zone;
    }

    private void Update()
    {
        if (owner == null || battle == null || battle.IsFinished)
        {
            Destroy(gameObject);
            return;
        }

        remainingDuration = Mathf.Max(0f, remainingDuration - Time.deltaTime);
        tickCooldown -= Time.deltaTime;
        while (tickCooldown <= 0f && remainingTickCount > 0)
        {
            Tick();
            tickCooldown += tickInterval;
        }

        if (remainingDuration <= 0f || remainingTickCount <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void Tick()
    {
        if (remainingTickCount <= 0 || owner == null || battle == null)
        {
            return;
        }

        remainingTickCount--;
        foreach (var enemy in battle.GetOpponents(owner.Team))
        {
            if (!enemy.IsAlive || !ContainsPoint(enemy.transform.position))
            {
                continue;
            }

            if (kind == PrototypeFireZoneKind.Magma)
            {
                owner.ApplyMagmaTick(enemy, this, multiplier);
            }
            else
            {
                owner.ApplyScorchTick(enemy, this, multiplier);
            }
        }
    }

    private bool ContainsPoint(Vector3 point)
    {
        return kind == PrototypeFireZoneKind.Magma
            ? PrototypeFireCombat.PointInCircle(point, origin, radius)
            : PrototypeFireCombat.PointInCone(point, origin, direction, range, halfAngle);
    }

    private void CreateVisual(Sprite sprite)
    {
        visualRoot = new GameObject("Zone Visual").transform;
        visualRoot.SetParent(transform, false);
        if (kind == PrototypeFireZoneKind.Magma)
        {
            animatedVisual = PrototypeFireVfx.SpawnAttached(
                "FireGodMagmaLoop", visualRoot, 0.1f, radius, true, -20);
            if (animatedVisual != null)
            {
                return;
            }
            for (var index = 0; index < 3; index++)
            {
                var renderer = AddLayer(sprite, new Color(1f, 0.18f + index * 0.08f, 0.02f, 0.28f), -20 + index);
                renderer.transform.localRotation = Quaternion.Euler(0f, 0f, index * 30f);
                renderer.transform.localScale = Vector3.one * radius * (1.35f - index * 0.18f);
            }
            return;
        }

        animatedVisual = PrototypeFireVfx.SpawnAttached(
            "FireGodScorchLoop", visualRoot, 0.1f, range / 3f, true, -19);
        if (animatedVisual != null)
        {
            animatedVisual.transform.localPosition = direction * range * 0.5f;
            animatedVisual.transform.localRotation = Quaternion.Euler(
                0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            return;
        }

        for (var index = 0; index < 5; index++)
        {
            var angle = Mathf.Lerp(-halfAngle, halfAngle, index / 4f);
            var renderer = AddLayer(sprite, new Color(1f, 0.24f, 0.04f, 0.22f), -19 + index);
            renderer.transform.localPosition = Quaternion.Euler(0f, 0f, angle) * direction * range * 0.5f;
            renderer.transform.localRotation = Quaternion.Euler(
                0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + angle);
            renderer.transform.localScale = new Vector3(range, 0.34f, 1f);
        }
    }

    private SpriteRenderer AddLayer(Sprite sprite, Color color, int sortingOrder)
    {
        var layer = new GameObject("Fire Zone Layer");
        layer.transform.SetParent(visualRoot, false);
        var renderer = layer.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }
}
