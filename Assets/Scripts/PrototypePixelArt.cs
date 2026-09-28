using System.Collections.Generic;
using UnityEngine;

internal enum PrototypeAnimationState
{
    Idle,
    Run,
    Attack,
    Skill,
    Ultimate,
    Hit,
    Death
}

internal sealed class PrototypeSpriteSet
{
    public readonly Sprite[] Idle;
    public readonly Sprite[] Run;
    public readonly Sprite[] Attack;
    public readonly Sprite[] Skill;
    public readonly Sprite[] Ultimate;
    public readonly Sprite[] Hit;
    public readonly Sprite[] Death;

    public PrototypeSpriteSet(Sprite[] idle, Sprite[] run, Sprite[] attack, Sprite[] skill, Sprite[] hit, Sprite[] death)
        : this(idle, run, attack, skill, skill, hit, death)
    {
    }

    public PrototypeSpriteSet(
        Sprite[] idle,
        Sprite[] run,
        Sprite[] attack,
        Sprite[] skill,
        Sprite[] ultimate,
        Sprite[] hit,
        Sprite[] death)
    {
        Idle = idle;
        Run = run;
        Attack = attack;
        Skill = skill;
        Ultimate = ultimate;
        Hit = hit;
        Death = death;
    }

    public Sprite[] Get(PrototypeAnimationState state)
    {
        switch (state)
        {
            case PrototypeAnimationState.Run: return Run;
            case PrototypeAnimationState.Attack: return Attack;
            case PrototypeAnimationState.Skill: return Skill;
            case PrototypeAnimationState.Ultimate: return Ultimate;
            case PrototypeAnimationState.Hit: return Hit;
            case PrototypeAnimationState.Death: return Death;
            default: return Idle;
        }
    }
}

internal static class PrototypePixelArt
{
    private const int Size = 32;
    private const float PixelsPerUnit = 32f;
    private static readonly Dictionary<long, PrototypeSpriteSet> Cache =
        new Dictionary<long, PrototypeSpriteSet>();

    public static PrototypeSpriteSet Create(CombatantDefinition definition)
    {
        return Create(definition, false);
    }

    public static PrototypeSpriteSet Create(CombatantDefinition definition, bool boss)
    {
        var generated = PrototypeHeroPixelArt64.Supports(definition.SkillKit)
            ? PrototypeHeroPixelArt64.Create(definition.SkillKit, definition.BodyColor, boss)
            : Create(definition.Species, definition.CombatClass, definition.SkillKit, definition.BodyColor);
        if (!definition.HasImportedSprites)
        {
            return generated;
        }

        if (definition.HasStaticImportedSprite)
        {
            var sprite = definition.IdleFrames[0];
            return new PrototypeSpriteSet(
                Repeat(sprite, 2),
                Repeat(sprite, 4),
                Repeat(sprite, 4),
                Repeat(sprite, 4),
                Repeat(sprite, 4),
                Repeat(sprite, 2),
                Repeat(sprite, 3));
        }

        return new PrototypeSpriteSet(
            FramesOrFallback(definition.IdleFrames, generated.Idle),
            FramesOrFallback(definition.RunFrames, generated.Run),
            FramesOrFallback(definition.AttackFrames, generated.Attack),
            FramesOrFallback(definition.SkillFrames, generated.Skill),
            FramesOrFallback(definition.UltimateFrames, generated.Ultimate),
            FramesOrFallback(definition.HitFrames, generated.Hit),
            FramesOrFallback(definition.DeathFrames, generated.Death));
    }

    private static Sprite[] Repeat(Sprite sprite, int count)
    {
        var frames = new Sprite[count];
        for (var index = 0; index < count; index++)
        {
            frames[index] = sprite;
        }
        return frames;
    }

    public static PrototypeSpriteSet Create(
        PrototypeSpecies species,
        PrototypeCombatClass combatClass,
        PrototypeSkillKit skillKit,
        Color color)
    {
        var rgba = (Color32)color;
        var key = ((long)species << 48) | ((long)combatClass << 40) | ((long)skillKit << 32) |
            (long)rgba.r << 16 | (long)rgba.g << 8 | rgba.b;
        PrototypeSpriteSet cached;
        if (Cache.TryGetValue(key, out cached) && cached.Idle[0] != null)
        {
            return cached;
        }

        var created = new PrototypeSpriteSet(
            CreateFrames(species, combatClass, skillKit, color, PrototypeAnimationState.Idle, 2),
            CreateFrames(species, combatClass, skillKit, color, PrototypeAnimationState.Run, 4),
            CreateFrames(species, combatClass, skillKit, color, PrototypeAnimationState.Attack, 4),
            CreateFrames(species, combatClass, skillKit, color, PrototypeAnimationState.Skill, 4),
            CreateFrames(species, combatClass, skillKit, color, PrototypeAnimationState.Hit, 2),
            CreateFrames(species, combatClass, skillKit, color, PrototypeAnimationState.Death, 3));
        Cache[key] = created;
        return created;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()
    {
        Cache.Clear();
    }

    private static Sprite[] CreateFrames(
        PrototypeSpecies species,
        PrototypeCombatClass combatClass,
        PrototypeSkillKit skillKit,
        Color color,
        PrototypeAnimationState state,
        int count)
    {
        var frames = new Sprite[count];
        for (var frame = 0; frame < count; frame++)
        {
            frames[frame] = CreateFrame(species, combatClass, skillKit, color, state, frame);
        }

        return frames;
    }

    private static Sprite[] FramesOrFallback(Sprite[] frames, Sprite[] fallback)
    {
        return frames != null && frames.Length > 0 ? frames : fallback;
    }

    private static Sprite CreateFrame(
        PrototypeSpecies species,
        PrototypeCombatClass combatClass,
        PrototypeSkillKit skillKit,
        Color color,
        PrototypeAnimationState state,
        int frame)
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = $"{skillKit}_{species}_{state}_{frame}"
        };

        var pixels = new Color32[Size * Size];
        var main = (Color32)color;
        var light = Mix(main, new Color32(255, 255, 255, 255), 0.4f);
        var dark = Mix(main, new Color32(8, 12, 24, 255), 0.58f);
        var outline = new Color32(9, 13, 25, 255);
        var accent = GetAccent(skillKit);

        if (state == PrototypeAnimationState.Death)
        {
            var height = Mathf.Max(2, 7 - frame * 2);
            Rect(pixels, 7 + frame * 2, 5, 20 - frame * 4, 2, outline);
            Rect(pixels, 9 + frame * 2, 7, 16 - frame * 4, height, dark);
            Rect(pixels, 12 + frame, 8, 8 - frame * 2, Mathf.Max(1, height - 2), main);
            Pixel(pixels, 6 + frame, 6 + frame, light);
            Pixel(pixels, 26 - frame, 5, accent);
        }
        else
        {
            var pose = frame & 1;
            var bob = state == PrototypeAnimationState.Idle && pose == 1 ? 1 : 0;
            switch (species)
            {
                case PrototypeSpecies.Human:
                    DrawHuman(pixels, state, pose, bob, outline, dark, main, light, accent);
                    break;
                case PrototypeSpecies.Dragon:
                    DrawDragon(pixels, state, pose, bob, outline, dark, main, light, accent);
                    break;
                case PrototypeSpecies.Cosmic:
                    DrawCosmic(pixels, state, pose, bob, outline, dark, main, light, accent);
                    break;
                case PrototypeSpecies.Machine:
                    DrawMachine(pixels, state, pose, bob, outline, dark, main, light, accent);
                    break;
                case PrototypeSpecies.Ocean:
                    DrawOcean(pixels, state, pose, bob, outline, dark, main, light, accent);
                    break;
                case PrototypeSpecies.Fantasy:
                    DrawFantasy(pixels, state, pose, bob, outline, dark, main, light, accent);
                    break;
                default:
                    Rect(pixels, 10, 8, 12, 16, main);
                    break;
            }

            DrawClassIdentity(pixels, combatClass, state, frame, outline, light, accent);
            DrawSignature(pixels, skillKit, state, frame, main, light, accent);
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    private static Color32 GetAccent(PrototypeSkillKit skillKit)
    {
        switch (skillKit)
        {
            case PrototypeSkillKit.Nova: return new Color32(255, 207, 58, 255);
            case PrototypeSkillKit.Ion: return new Color32(70, 230, 255, 255);
            case PrototypeSkillKit.Krag: return new Color32(178, 112, 255, 255);
            case PrototypeSkillKit.Vex: return new Color32(255, 70, 155, 255);
            case PrototypeSkillKit.Rook: return new Color32(255, 125, 55, 255);
            case PrototypeSkillKit.Lyra: return new Color32(255, 238, 95, 255);
            case PrototypeSkillKit.Astra: return new Color32(86, 228, 200, 255);
            case PrototypeSkillKit.Brakk: return new Color32(158, 72, 255, 255);
            case PrototypeSkillKit.Hex: return new Color32(135, 255, 80, 255);
            case PrototypeSkillKit.Nyx: return new Color32(115, 165, 255, 255);
            case PrototypeSkillKit.Mira: return new Color32(55, 225, 235, 255);
            case PrototypeSkillKit.Drake: return new Color32(255, 82, 32, 255);
            default: return new Color32(255, 230, 110, 255);
        }
    }

    private static void DrawClassIdentity(
        Color32[] p,
        PrototypeCombatClass combatClass,
        PrototypeAnimationState state,
        int frame,
        Color32 outline,
        Color32 light,
        Color32 accent)
    {
        var reach = state == PrototypeAnimationState.Attack ? frame * 2 : 0;
        switch (combatClass)
        {
            case PrototypeCombatClass.Tanker:
                Rect(p, 5, 12, 5, 11, outline);
                Rect(p, 6, 13, 3, 9, accent);
                break;
            case PrototypeCombatClass.Fighter:
                Line(p, 22, 18, 27 + reach, 12, outline);
                Line(p, 23, 18, 28 + reach, 12, light);
                Pixel(p, 22, 19, accent);
                break;
            case PrototypeCombatClass.Assassin:
                Line(p, 8, 13, 3, 8 - Mathf.Min(frame, 2), accent);
                Line(p, 24, 13, 29, 8 - Mathf.Min(frame, 2), accent);
                break;
            case PrototypeCombatClass.Mage:
                Line(p, 25, 8, 25, 25, outline);
                Ring(p, 25, 25, 5 + (state == PrototypeAnimationState.Skill ? frame * 2 : 0), accent);
                break;
            case PrototypeCombatClass.Archer:
                Line(p, 25, 10, 29, 16, light);
                Line(p, 29, 16, 25, 22, light);
                Line(p, 25, 10, 25, 22, outline);
                break;
            case PrototypeCombatClass.Support:
                Ring(p, 16, 28, 8 + (state == PrototypeAnimationState.Skill ? frame * 2 : 0), accent);
                break;
        }
    }

    private static void DrawSignature(
        Color32[] p,
        PrototypeSkillKit skillKit,
        PrototypeAnimationState state,
        int frame,
        Color32 main,
        Color32 light,
        Color32 accent)
    {
        switch (skillKit)
        {
            case PrototypeSkillKit.Nova:
                Rect(p, 15, 16, 3, 3, accent);
                if (state == PrototypeAnimationState.Skill)
                {
                    Ring(p, 16, 17, 10 + frame * 3, accent);
                }
                break;
            case PrototypeSkillKit.Ion:
                Pixel(p, 11 - frame, 26, accent);
                Pixel(p, 21 + frame, 24, accent);
                if (state == PrototypeAnimationState.Attack || state == PrototypeAnimationState.Skill)
                {
                    Line(p, 20, 20, 24, 23, accent);
                    Line(p, 24, 23, 21, 27, light);
                }
                break;
            case PrototypeSkillKit.Krag:
                Rect(p, 9, 19, 3, 7, accent);
                Rect(p, 21, 19, 3, 7, accent);
                if (state == PrototypeAnimationState.Skill)
                {
                    Ring(p, 16, 17, 12 + frame * 2, main);
                }
                break;
            case PrototypeSkillKit.Vex:
                Line(p, 7, 20, 3, 15 - Mathf.Min(frame, 2), accent);
                Line(p, 25, 20, 29, 15 - Mathf.Min(frame, 2), accent);
                Pixel(p, 14, 23, accent);
                Pixel(p, 19, 23, accent);
                break;
            case PrototypeSkillKit.Astra:
                Line(p, 16, 19, 16, 25, light);
                Line(p, 13, 22, 19, 22, accent);
                if (state == PrototypeAnimationState.Skill)
                {
                    Pixel(p, 7 + frame, 25, accent);
                    Pixel(p, 24 - frame, 27, light);
                    Ring(p, 16, 20, 8 + frame * 2, accent);
                }
                break;
            case PrototypeSkillKit.Lyra:
                Line(p, 11, 26, 16, 29, accent);
                Line(p, 16, 29, 21, 26, light);
                if (state == PrototypeAnimationState.Attack || state == PrototypeAnimationState.Skill)
                {
                    Line(p, 22, 17, 31, 17 + Mathf.Min(frame, 2), accent);
                }
                break;
            case PrototypeSkillKit.Brakk:
                Rect(p, 12, 18, 3, 5, light);
                Rect(p, 18, 18, 3, 5, accent);
                Pixel(p, 16, 21, accent);
                if (state == PrototypeAnimationState.Skill)
                {
                    Ring(p, 16, 18, 16 + frame * 2, accent);
                }
                break;
        }
    }

    private static void DrawHuman(
        Color32[] p, PrototypeAnimationState state, int frame, int bob,
        Color32 outline, Color32 dark, Color32 main, Color32 light, Color32 accent)
    {
        var y = bob;
        Rect(p, 13, 20 + y, 7, 7, outline);
        Rect(p, 14, 21 + y, 5, 5, light);
        Rect(p, 12, 12 + y, 9, 9, outline);
        Rect(p, 14, 13 + y, 5, 8, main);
        Rect(p, 8, 14 + y, 4, 7, dark);
        Rect(p, 21, 14 + y, state == PrototypeAnimationState.Attack && frame == 1 ? 8 : 4, 3, outline);
        Rect(p, 22, 15 + y, state == PrototypeAnimationState.Attack && frame == 1 ? 7 : 3, 1, light);
        DrawLegs(p, state, frame, 13, 7 + y, main, outline);
        Rect(p, 10, 23 + y, 2, 2, accent);
        Rect(p, 21, 23 + y, 2, 2, accent);
        Pixel(p, 16, 28 + y, accent);
        Pixel(p, 12, 27 + y, accent);
        Pixel(p, 20, 27 + y, accent);
        if (state == PrototypeAnimationState.Skill)
        {
            Ring(p, 16, 17 + y, 13 + frame * 2, accent);
        }
    }

    private static void DrawDragon(
        Color32[] p, PrototypeAnimationState state, int frame, int bob,
        Color32 outline, Color32 dark, Color32 main, Color32 light, Color32 accent)
    {
        var y = bob;
        Line(p, 13, 25 + y, 10, 30 + y, outline);
        Line(p, 19, 25 + y, 22, 30 + y, outline);
        Rect(p, 13, 20 + y, 7, 7, outline);
        Rect(p, 14, 21 + y, 5, 5, main);
        Rect(p, 14, 12 + y, 6, 9, outline);
        Rect(p, 15, 13 + y, 4, 8, dark);
        Rect(p, 10, 13 + y, 4, 3, main);
        var reach = state == PrototypeAnimationState.Attack && frame == 1 ? 8 : 4;
        Rect(p, 20, 14 + y, reach, 2, light);
        DrawLegs(p, state, frame, 13, 6 + y, main, outline);
        Line(p, 12, 12 + y, 7, 9 + y, accent);
        Pixel(p, 7, 8 + y, accent);
        Pixel(p, 17, 23 + y, accent);
        if (state == PrototypeAnimationState.Skill)
        {
            Line(p, 5, 24, 10, 19, accent);
            Line(p, 21, 12, 27, 18, accent);
            Line(p, 8, 7, 12, 11, accent);
        }
    }

    private static void DrawCosmic(
        Color32[] p, PrototypeAnimationState state, int frame, int bob,
        Color32 outline, Color32 dark, Color32 main, Color32 light, Color32 accent)
    {
        var y = 1 + bob;
        Rect(p, 12, 18 + y, 9, 8, outline);
        Rect(p, 13, 19 + y, 7, 6, light);
        Rect(p, 14, 12 + y, 5, 7, main);
        Rect(p, 7, 17 + y, 5, 3 + frame, dark);
        Rect(p, 21, 17 + y, 5, 3 + (1 - frame), dark);
        Line(p, 14, 12 + y, 11, 6 + y, main);
        Line(p, 16, 12 + y, 16, 5 + y, light);
        Line(p, 18, 12 + y, 22, 7 + y, main);
        Pixel(p, 15, 22 + y, accent);
        Pixel(p, 18, 22 + y, accent);
        if (state == PrototypeAnimationState.Attack)
        {
            Line(p, 21, 18 + y, 28, 18 + y + frame, accent);
        }
        if (state == PrototypeAnimationState.Skill)
        {
            Ring(p, 16, 17 + y, 11 + frame * 3, light);
            Pixel(p, 6, 25, accent);
            Pixel(p, 26, 24, accent);
        }
    }

    private static void DrawFantasy(
        Color32[] p, PrototypeAnimationState state, int frame, int bob,
        Color32 outline, Color32 dark, Color32 main, Color32 light, Color32 accent)
    {
        var y = bob;
        Rect(p, 9, 17 + y, 15, 10, outline);
        Rect(p, 11, 18 + y, 12, 8, main);
        Rect(p, 6, 12 + y, 7, 10, outline);
        Rect(p, 7, 13 + y, 5, 8, dark);
        var armWidth = state == PrototypeAnimationState.Attack && frame == 1 ? 10 : 6;
        Rect(p, 22, 13 + y, armWidth, 7, outline);
        Rect(p, 23, 14 + y, armWidth - 1, 5, main);
        Rect(p, 10, 6 + y, 6, 8, outline);
        Rect(p, 19, 6 + y, 6, 8, outline);
        Rect(p, 11, 7 + y, 4, 6, dark);
        Rect(p, 20, 7 + y, 4, 6, dark);
        Line(p, 11, 26 + y, 7, 30 + y, outline);
        Line(p, 21, 26 + y, 25, 30 + y, outline);
        Rect(p, 19, 21 + y, 2, 2, accent);
        if (state == PrototypeAnimationState.Skill)
        {
            Rect(p, 3, 5, 3, 3, accent);
            Rect(p, 27, 7, 3, 3, accent);
            Ring(p, 17, 16, 14 + frame * 2, main);
        }
    }

    private static void DrawOcean(
        Color32[] p, PrototypeAnimationState state, int frame, int bob,
        Color32 outline, Color32 dark, Color32 main, Color32 light, Color32 accent)
    {
        var y = bob;
        Rect(p, 13, 21 + y, 7, 5, outline);
        Rect(p, 14, 22 + y, 5, 3, main);
        Line(p, 14, 25 + y, 10, 30 + y, outline);
        Line(p, 19, 25 + y, 23, 30 + y, outline);
        Rect(p, 14, 13 + y, 6, 9, outline);
        Rect(p, 15, 14 + y, 4, 7, dark);
        Line(p, 14, 18 + y, 8, 13 + y, main);
        Line(p, 19, 18 + y, state == PrototypeAnimationState.Attack ? 30 : 25, 13 + y + frame, light);
        Line(p, 15, 13 + y, 10 + frame * 2, 6 + y, main);
        Line(p, 19, 13 + y, 24 - frame * 2, 6 + y, main);
        Line(p, 13, 15 + y, 6, 18 + y, dark);
        Pixel(p, 15, 23 + y, accent);
        Pixel(p, 18, 23 + y, accent);
        if (state == PrototypeAnimationState.Skill)
        {
            Line(p, 21, 20, 30, 11, accent);
            Line(p, 22, 22, 29, 15, light);
        }
    }

    private static void DrawMachine(
        Color32[] p, PrototypeAnimationState state, int frame, int bob,
        Color32 outline, Color32 dark, Color32 main, Color32 light, Color32 accent)
    {
        var y = bob;
        Rect(p, 10, 17 + y, 13, 10, outline);
        Rect(p, 12, 19 + y, 9, 6, main);
        Rect(p, 14, 21 + y, 5, 2, accent);
        Rect(p, 11, 10 + y, 12, 8, outline);
        Rect(p, 13, 11 + y, 8, 6, dark);
        Rect(p, 8, 7 + y, 7, 4, outline);
        Rect(p, 19, 7 + y, 7, 4, outline);
        Line(p, 16, 27 + y, 16, 30 + y, light);
        Pixel(p, 16, 31, accent);
        var cannon = state == PrototypeAnimationState.Attack && frame == 1 ? 10 : 6;
        Rect(p, 22, 14 + y, cannon, 5, outline);
        Rect(p, 23, 15 + y, cannon - 1, 3, light);
        if (state == PrototypeAnimationState.Skill)
        {
            Rect(p, 26, 14 + y, 6, 5, accent);
            Ring(p, 16, 18, 12 + frame * 2, main);
        }
    }

    private static void DrawLegs(
        Color32[] p,
        PrototypeAnimationState state,
        int frame,
        int x,
        int y,
        Color32 main,
        Color32 outline)
    {
        var offset = state == PrototypeAnimationState.Run ? frame * 2 - 1 : 0;
        Rect(p, x - offset, y, 4, 7, outline);
        Rect(p, x + 5 + offset, y, 4, 7, outline);
        Rect(p, x + 1 - offset, y + 1, 2, 5, main);
        Rect(p, x + 6 + offset, y + 1, 2, 5, main);
    }

    private static void Ring(Color32[] p, int centerX, int centerY, int diameter, Color32 color)
    {
        var radius = diameter / 2;
        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                var distance = x * x + y * y;
                if (distance >= (radius - 1) * (radius - 1) && distance <= radius * radius)
                {
                    Pixel(p, centerX + x, centerY + y, color);
                }
            }
        }
    }

    private static void Line(Color32[] p, int x0, int y0, int x1, int y1, Color32 color)
    {
        var dx = Mathf.Abs(x1 - x0);
        var sx = x0 < x1 ? 1 : -1;
        var dy = -Mathf.Abs(y1 - y0);
        var sy = y0 < y1 ? 1 : -1;
        var error = dx + dy;

        while (true)
        {
            Pixel(p, x0, y0, color);
            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            var doubled = error * 2;
            if (doubled >= dy)
            {
                error += dy;
                x0 += sx;
            }

            if (doubled <= dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }

    private static void Rect(Color32[] p, int x, int y, int width, int height, Color32 color)
    {
        for (var px = x; px < x + width; px++)
        {
            for (var py = y; py < y + height; py++)
            {
                Pixel(p, px, py, color);
            }
        }
    }

    private static void Pixel(Color32[] p, int x, int y, Color32 color)
    {
        if (x >= 0 && x < Size && y >= 0 && y < Size)
        {
            p[y * Size + x] = color;
        }
    }

    private static Color32 Mix(Color32 from, Color32 to, float amount)
    {
        return new Color32(
            (byte)Mathf.RoundToInt(Mathf.Lerp(from.r, to.r, amount)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(from.g, to.g, amount)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(from.b, to.b, amount)),
            255);
    }
}
