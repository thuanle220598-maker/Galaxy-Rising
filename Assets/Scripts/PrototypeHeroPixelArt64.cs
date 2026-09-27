using System.Collections.Generic;
using UnityEngine;

internal static class PrototypeHeroPixelArt64
{
    private const int Size = 64;
    private const float PixelsPerUnit = 64f;
    private static readonly Dictionary<long, PrototypeSpriteSet> Cache =
        new Dictionary<long, PrototypeSpriteSet>();

    public static bool Supports(PrototypeSkillKit kit)
    {
        return kit == PrototypeSkillKit.Nova || kit == PrototypeSkillKit.Ion ||
            kit == PrototypeSkillKit.Astra || kit == PrototypeSkillKit.Lyra ||
            kit == PrototypeSkillKit.Brakk || kit == PrototypeSkillKit.Krag;
    }

    public static PrototypeSpriteSet Create(PrototypeSkillKit kit, Color color, bool boss)
    {
        var rgba = (Color32)color;
        var key = ((long)kit << 40) | ((long)(boss ? 1 : 0) << 32) |
            (long)rgba.r << 16 | (long)rgba.g << 8 | rgba.b;
        PrototypeSpriteSet cached;
        if (Cache.TryGetValue(key, out cached) && cached.Idle[0] != null)
        {
            return cached;
        }

        var created = new PrototypeSpriteSet(
            CreateFrames(kit, color, boss, PrototypeAnimationState.Idle, 4),
            CreateFrames(kit, color, boss, PrototypeAnimationState.Run, 6),
            CreateFrames(kit, color, boss, PrototypeAnimationState.Attack, 6),
            CreateFrames(kit, color, boss, PrototypeAnimationState.Skill, 8),
            CreateFrames(kit, color, boss, PrototypeAnimationState.Ultimate, 10),
            CreateFrames(kit, color, boss, PrototypeAnimationState.Hit, 3),
            CreateFrames(kit, color, boss, PrototypeAnimationState.Death, 6));
        Cache[key] = created;
        return created;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()
    {
        Cache.Clear();
    }

    private static Sprite[] CreateFrames(
        PrototypeSkillKit kit,
        Color color,
        bool boss,
        PrototypeAnimationState state,
        int count)
    {
        var frames = new Sprite[count];
        for (var frame = 0; frame < count; frame++)
        {
            frames[frame] = CreateFrame(kit, color, boss, state, frame, count);
        }

        return frames;
    }

    private static Sprite CreateFrame(
        PrototypeSkillKit kit,
        Color color,
        bool boss,
        PrototypeAnimationState state,
        int frame,
        int count)
    {
        var suffix = boss ? "Boss64" : "64";
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = $"{kit}_{suffix}_{state}_{frame}"
        };
        var pixels = new Color32[Size * Size];
        var main = (Color32)color;
        var light = Mix(main, new Color32(255, 255, 255, 255), 0.48f);
        var dark = Mix(main, new Color32(7, 11, 24, 255), 0.62f);
        var outline = new Color32(7, 10, 22, 255);
        var accent = Accent(kit);
        var hot = Mix(accent, new Color32(255, 255, 255, 255), 0.5f);
        var progress = count <= 1 ? 1f : (float)frame / (count - 1);

        if (state == PrototypeAnimationState.Death)
        {
            DrawDeath(pixels, kit, frame, outline, dark, main, light, accent);
        }
        else
        {
            switch (kit)
            {
                case PrototypeSkillKit.Nova:
                    DrawNova(pixels, state, frame, progress, outline, dark, main, light, accent, hot);
                    break;
                case PrototypeSkillKit.Ion:
                    DrawIon(pixels, state, frame, progress, outline, dark, main, light, accent, hot);
                    break;
                case PrototypeSkillKit.Astra:
                    DrawAstra(pixels, state, frame, progress, outline, dark, main, light, accent, hot);
                    break;
                case PrototypeSkillKit.Lyra:
                    DrawLyra(pixels, state, frame, progress, outline, dark, main, light, accent, hot);
                    break;
                case PrototypeSkillKit.Brakk:
                    DrawBrakk(pixels, state, frame, progress, outline, dark, main, light, accent, hot, false);
                    break;
                case PrototypeSkillKit.Krag:
                    DrawBrakk(pixels, state, frame, progress, outline, dark, main, light, accent, hot, boss);
                    break;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    private static void DrawNova(
        Color32[] p, PrototypeAnimationState state, int frame, float progress,
        Color32 outline, Color32 dark, Color32 main, Color32 light, Color32 accent, Color32 hot)
    {
        var bob = Bob(state, frame);
        var recoil = state == PrototypeAnimationState.Hit ? -3 + frame : 0;
        var x = 30 + recoil;
        DrawLegs(p, x - 8, 8 + bob, state, frame, outline, dark, main);
        Rect(p, x - 10, 20 + bob, 20, 21, outline);
        Rect(p, x - 8, 22 + bob, 16, 17, main);
        Rect(p, x - 5, 24 + bob, 10, 12, dark);
        Diamond(p, x, 31 + bob, 4, accent);
        Pixel(p, x, 31 + bob, hot);
        Rect(p, x - 13, 33 + bob, 5, 8, outline);
        Rect(p, x + 8, 33 + bob, 5, 8, outline);
        Rect(p, x - 8, 42 + bob, 16, 13, outline);
        Rect(p, x - 6, 44 + bob, 12, 9, light);
        Rect(p, x - 5, 47 + bob, 10, 3, dark);
        Pixel(p, x - 3, 50 + bob, accent);
        Pixel(p, x + 3, 50 + bob, accent);
        Line(p, x - 8, 52 + bob, x - 13, 58 + bob, outline);
        Line(p, x + 8, 52 + bob, x + 13, 58 + bob, outline);

        var reach = state == PrototypeAnimationState.Attack ? frame * 3 : 0;
        var lift = state == PrototypeAnimationState.Ultimate ? Mathf.RoundToInt(progress * 18f) : 0;
        Line(p, x + 10, 35 + bob, x + 18 + reach, 28 + bob + lift, outline, 3);
        Line(p, x + 12, 35 + bob, x + 19 + reach, 29 + bob + lift, hot, 1);
        Diamond(p, x + 19 + reach, 29 + bob + lift, 3, accent);

        if (state == PrototypeAnimationState.Skill || state == PrototypeAnimationState.Ultimate)
        {
            Ring(p, x, 31 + bob, 12 + frame * 2, accent);
            if ((frame & 1) == 0)
            {
                Line(p, x - 20, 31, x + 20, 31, accent);
            }
        }
    }

    private static void DrawIon(
        Color32[] p, PrototypeAnimationState state, int frame, float progress,
        Color32 outline, Color32 dark, Color32 main, Color32 light, Color32 accent, Color32 hot)
    {
        var bob = Bob(state, frame);
        var lean = state == PrototypeAnimationState.Run ? frame % 3 - 1 : 0;
        Line(p, 18, 22 + bob, 8 - lean * 2, 15 + bob, outline, 5);
        Line(p, 11, 16 + bob, 5, 22 + bob, main, 3);
        DrawLegs(p, 26, 8 + bob, state, frame, outline, dark, main);
        Rect(p, 20 + lean, 20 + bob, 25, 19, outline);
        Rect(p, 22 + lean, 22 + bob, 21, 15, main);
        Rect(p, 26 + lean, 27 + bob, 13, 8, dark);
        Rect(p, 32 + lean, 39 + bob, 15, 12, outline);
        Rect(p, 34 + lean, 41 + bob, 11, 8, light);
        Pixel(p, 42 + lean, 46 + bob, hot);
        Line(p, 35 + lean, 50 + bob, 30, 59 + bob, outline, 3);
        Line(p, 42 + lean, 50 + bob, 48, 59 + bob, outline, 3);
        Line(p, 34 + lean, 53 + bob, 31, 60 + bob, accent);
        Line(p, 43 + lean, 53 + bob, 47, 60 + bob, accent);
        Line(p, 23 + lean, 35 + bob, 15, 43 + bob, outline, 4);
        Line(p, 44 + lean, 34 + bob, 54, 40 + bob, outline, 4);
        Pixel(p, 13, 45 + bob, accent);
        Pixel(p, 56, 42 + bob, accent);

        if (state == PrototypeAnimationState.Attack || state == PrototypeAnimationState.Skill ||
            state == PrototypeAnimationState.Ultimate)
        {
            var reach = 4 + Mathf.RoundToInt(progress * 14f);
            Zigzag(p, 44, 37 + bob, 44 + reach, 46 - frame % 5, accent, hot);
            Zigzag(p, 24, 40 + bob, 13 - frame, 50, accent, hot);
        }

        if (state == PrototypeAnimationState.Ultimate)
        {
            Ring(p, 35, 34 + bob, 18 + frame * 3, accent);
        }
    }

    private static void DrawAstra(
        Color32[] p, PrototypeAnimationState state, int frame, float progress,
        Color32 outline, Color32 dark, Color32 main, Color32 light, Color32 accent, Color32 hot)
    {
        var bob = 2 + Bob(state, frame);
        var spread = state == PrototypeAnimationState.Ultimate ? 5 + frame : 3;
        Ring(p, 32, 50 + bob, 20 + (state == PrototypeAnimationState.Ultimate ? frame * 2 : 0), accent);
        Rect(p, 27, 42 + bob, 10, 11, outline);
        Rect(p, 29, 44 + bob, 6, 7, light);
        Pixel(p, 30, 48 + bob, accent);
        Pixel(p, 34, 48 + bob, accent);
        Diamond(p, 32, 25 + bob, 13, outline);
        Diamond(p, 32, 27 + bob, 10, main);
        Diamond(p, 32, 31 + bob, 4, dark);
        Line(p, 26, 38 + bob, 18 - spread, 30 + bob, outline, 3);
        Line(p, 38, 38 + bob, 46 + spread, 30 + bob, outline, 3);
        Line(p, 27, 36 + bob, 19 - spread, 31 + bob, accent);
        Line(p, 37, 36 + bob, 45 + spread, 31 + bob, hot);
        Line(p, 26, 18 + bob, 20, 8 + bob, outline, 4);
        Line(p, 38, 18 + bob, 44, 8 + bob, outline, 4);
        Diamond(p, 15 - frame % 3, 42, 2, accent);
        Diamond(p, 50 + frame % 3, 37, 2, hot);

        if (state == PrototypeAnimationState.Skill || state == PrototypeAnimationState.Ultimate)
        {
            Ring(p, 32, 29 + bob, 16 + frame * 3, hot);
            Line(p, 32, 5, 32, 15 + Mathf.RoundToInt(progress * 20f), accent, 2);
        }
    }

    private static void DrawLyra(
        Color32[] p, PrototypeAnimationState state, int frame, float progress,
        Color32 outline, Color32 dark, Color32 main, Color32 light, Color32 accent, Color32 hot)
    {
        var bob = Bob(state, frame);
        var draw = state == PrototypeAnimationState.Attack || state == PrototypeAnimationState.Skill ? frame * 2 : 0;
        DrawLegs(p, 25, 7 + bob, state, frame, outline, dark, main);
        Rect(p, 22, 20 + bob, 15, 21, outline);
        Rect(p, 24, 22 + bob, 11, 17, main);
        Line(p, 22, 37 + bob, 14 - frame % 3, 25 + bob, dark, 5);
        Line(p, 23, 35 + bob, 11 - frame % 3, 28 + bob, accent, 2);
        Rect(p, 24, 41 + bob, 13, 12, outline);
        Rect(p, 26, 43 + bob, 9, 8, light);
        Pixel(p, 29, 48 + bob, accent);
        Line(p, 36, 39 + bob, 48 + draw, 45 + bob, outline, 3);
        var bowX = 48 + draw;
        Line(p, bowX, 28 + bob, bowX + 7, 40 + bob, hot, 2);
        Line(p, bowX + 7, 40 + bob, bowX, 53 + bob, hot, 2);
        Line(p, bowX, 28 + bob, bowX - 2, 53 + bob, outline);
        Line(p, 37, 39 + bob, Mathf.Min(63, bowX + 12), 39 + bob, accent, 2);
        Pixel(p, Mathf.Min(62, bowX + 13), 39 + bob, hot);

        if (state == PrototypeAnimationState.Skill)
        {
            Line(p, 40, 39 + bob, 63, 39 + bob + frame % 3, accent, 3);
        }
        else if (state == PrototypeAnimationState.Ultimate)
        {
            var height = 18 + Mathf.RoundToInt(progress * 35f);
            Line(p, 35, 40 + bob, 45, height, hot, 3);
            for (var ray = 0; ray < 4; ray++)
            {
                Line(p, 12 + ray * 13, 62, 15 + ray * 12, 51 - frame, accent, 2);
            }
        }
    }

    private static void DrawBrakk(
        Color32[] p, PrototypeAnimationState state, int frame, float progress,
        Color32 outline, Color32 dark, Color32 main, Color32 light, Color32 accent, Color32 hot,
        bool boss)
    {
        var bob = state == PrototypeAnimationState.Run ? frame % 2 : 0;
        var center = boss ? 34 : 36;
        var width = boss ? 34 : 28;
        DrawHeavyLegs(p, center, 5 + bob, state, frame, outline, dark, main);
        Rect(p, center - width / 2, 17 + bob, width, 29, outline);
        Rect(p, center - width / 2 + 3, 20 + bob, width - 6, 23, main);
        Rect(p, center - 8, 26 + bob, 16, 12, dark);
        Diamond(p, center, 32 + bob, boss ? 6 : 4, accent);
        Rect(p, center - 11, 45 + bob, 22, 12, outline);
        Rect(p, center - 8, 47 + bob, 16, 8, light);
        Rect(p, center - 6, 49 + bob, 12, 3, dark);
        Pixel(p, center - 3, 52 + bob, accent);
        Pixel(p, center + 3, 52 + bob, accent);

        var brace = state == PrototypeAnimationState.Skill || state == PrototypeAnimationState.Ultimate
            ? Mathf.RoundToInt(progress * 4f) : 0;
        Rect(p, 4 + brace, 17 + bob, 18, 29, outline);
        Rect(p, 7 + brace, 20 + bob, 12, 23, dark);
        Diamond(p, 13 + brace, 31 + bob, 7, accent);
        Ring(p, 13 + brace, 31 + bob, 12, hot);
        Line(p, center + width / 2, 35 + bob, 59, 27 + bob, outline, boss ? 6 : 4);

        if (state == PrototypeAnimationState.Skill || state == PrototypeAnimationState.Ultimate)
        {
            Ring(p, center, 30 + bob, 28 + frame * 3, accent);
            Line(p, 3, 14 + frame, 61, 14 + frame, accent, 2);
        }

        if (boss)
        {
            Line(p, center - 10, 57 + bob, center - 16, 63, outline, 3);
            Line(p, center + 10, 57 + bob, center + 16, 63, outline, 3);
            Diamond(p, 8 + frame * 2 % 12, 52, 3, accent);
            Diamond(p, 55 - frame * 2 % 12, 47, 3, hot);
        }
    }

    private static void DrawDeath(
        Color32[] p, PrototypeSkillKit kit, int frame,
        Color32 outline, Color32 dark, Color32 main, Color32 light, Color32 accent)
    {
        var remaining = Mathf.Max(3, 24 - frame * 4);
        var y = 7 + frame * 2;
        Rect(p, 12 + frame * 2, y, remaining + 12, 5, outline);
        Rect(p, 15 + frame * 2, y + 2, remaining + 5, 4, main);
        Line(p, 20, y + 7, 12 + frame * 2, y + 15, dark, 2);
        Line(p, 40, y + 7, 50 - frame * 2, y + 13, light, 2);
        for (var spark = 0; spark < 5 - Mathf.Min(frame, 4); spark++)
        {
            Pixel(p, 10 + spark * 10 + frame, 20 + spark * 6 + frame * 2, accent);
        }

        if (kit == PrototypeSkillKit.Astra || kit == PrototypeSkillKit.Ion)
        {
            Ring(p, 32, 28, Mathf.Max(4, 22 - frame * 3), accent);
        }
    }

    private static int Bob(PrototypeAnimationState state, int frame)
    {
        if (state == PrototypeAnimationState.Idle)
        {
            return frame == 1 || frame == 2 ? 1 : 0;
        }

        return state == PrototypeAnimationState.Run ? frame % 2 : 0;
    }

    private static void DrawLegs(
        Color32[] p, int x, int y, PrototypeAnimationState state, int frame,
        Color32 outline, Color32 dark, Color32 main)
    {
        var step = state == PrototypeAnimationState.Run ? frame % 3 - 1 : 0;
        Rect(p, x - step, y, 7, 16, outline);
        Rect(p, x + 12 + step, y, 7, 16, outline);
        Rect(p, x + 2 - step, y + 2, 4, 13, main);
        Rect(p, x + 14 + step, y + 2, 4, 13, dark);
        Rect(p, x - 2 - step, y, 11, 4, outline);
        Rect(p, x + 10 + step, y, 11, 4, outline);
    }

    private static void DrawHeavyLegs(
        Color32[] p, int center, int y, PrototypeAnimationState state, int frame,
        Color32 outline, Color32 dark, Color32 main)
    {
        var step = state == PrototypeAnimationState.Run ? frame % 3 - 1 : 0;
        Rect(p, center - 14 - step, y, 11, 17, outline);
        Rect(p, center + 3 + step, y, 11, 17, outline);
        Rect(p, center - 11 - step, y + 3, 6, 13, main);
        Rect(p, center + 6 + step, y + 3, 6, 13, dark);
        Rect(p, center - 17 - step, y, 16, 5, outline);
        Rect(p, center + 1 + step, y, 16, 5, outline);
    }

    private static void Zigzag(
        Color32[] p, int x0, int y0, int x1, int y1, Color32 accent, Color32 hot)
    {
        var midX = (x0 + x1) / 2;
        var midY = (y0 + y1) / 2;
        Line(p, x0, y0, midX - 2, midY + 3, accent, 2);
        Line(p, midX - 2, midY + 3, midX + 2, midY - 3, hot, 2);
        Line(p, midX + 2, midY - 3, x1, y1, accent, 2);
    }

    private static void Diamond(Color32[] p, int centerX, int centerY, int radius, Color32 color)
    {
        for (var y = -radius; y <= radius; y++)
        {
            var halfWidth = radius - Mathf.Abs(y);
            Rect(p, centerX - halfWidth, centerY + y, halfWidth * 2 + 1, 1, color);
        }
    }

    private static void Ring(Color32[] p, int centerX, int centerY, int diameter, Color32 color)
    {
        var radius = Mathf.Max(2, diameter / 2);
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

    private static void Line(
        Color32[] p, int x0, int y0, int x1, int y1, Color32 color, int thickness = 1)
    {
        var dx = Mathf.Abs(x1 - x0);
        var sx = x0 < x1 ? 1 : -1;
        var dy = -Mathf.Abs(y1 - y0);
        var sy = y0 < y1 ? 1 : -1;
        var error = dx + dy;
        while (true)
        {
            Rect(p, x0 - thickness / 2, y0 - thickness / 2, thickness, thickness, color);
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

    private static Color32 Accent(PrototypeSkillKit kit)
    {
        switch (kit)
        {
            case PrototypeSkillKit.Nova: return new Color32(255, 199, 44, 255);
            case PrototypeSkillKit.Ion: return new Color32(54, 232, 255, 255);
            case PrototypeSkillKit.Astra: return new Color32(118, 255, 196, 255);
            case PrototypeSkillKit.Lyra: return new Color32(255, 224, 62, 255);
            case PrototypeSkillKit.Brakk: return new Color32(93, 222, 255, 255);
            case PrototypeSkillKit.Krag: return new Color32(200, 92, 255, 255);
            default: return new Color32(255, 255, 255, 255);
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
