using System;
using System.Collections.Generic;
using UnityEngine;

internal enum PrototypeSkillSlot
{
    Basic,
    Passive,
    Active,
    Ultimate
}

internal enum PrototypeEquipmentSlot
{
    Weapon,
    Armor,
    Core
}

[Serializable]
internal sealed class PrototypeCharacterProgress
{
    public string characterName;
    public int level = 1;
    public int experience;
    public int stars = 1;
    public int shards;
    public int basicSkillLevel = 1;
    public int passiveSkillLevel = 1;
    public int activeSkillLevel = 1;
    public int ultimateSkillLevel = 1;
    public int weaponLevel;
    public int armorLevel;
    public int coreLevel;
}

[Serializable]
internal sealed class PrototypeProgressionSave
{
    public int version = 2;
    public List<PrototypeCharacterProgress> characters = new List<PrototypeCharacterProgress>();
}

internal static class PrototypeProgression
{
    private const string SaveKey = "Prototype.CharacterProgression";
    private const int MaxLevel = 100;
    private const int MaxStars = 5;
    private const int MaxSkillLevel = 10;
    private static PrototypeProgressionSave save;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetCache()
    {
        save = null;
    }

    public static PrototypeCharacterProgress Get(string characterName)
    {
        EnsureLoaded();
        foreach (var character in save.characters)
        {
            if (character.characterName == characterName)
            {
                return character;
            }
        }

        var created = new PrototypeCharacterProgress { characterName = characterName };
        save.characters.Add(created);
        Save();
        return created;
    }

    public static int ExperienceRequired(PrototypeCharacterProgress progress)
    {
        return 50 + progress.level * 25;
    }

    public static int StarCost(PrototypeCharacterProgress progress)
    {
        return progress.stars * 20;
    }

    public static int SkillCost(PrototypeCharacterProgress progress, PrototypeSkillSlot slot)
    {
        return GetSkillLevel(progress, slot) * 30;
    }

    public static int EquipmentCost(PrototypeCharacterProgress progress, PrototypeEquipmentSlot slot)
    {
        return 2 + GetEquipmentLevel(progress, slot);
    }

    public static void AddExperience(string characterName, int amount)
    {
        var progress = Get(characterName);
        progress.experience += Mathf.Max(0, amount);
        while (progress.level < MaxLevel && progress.experience >= ExperienceRequired(progress))
        {
            progress.experience -= ExperienceRequired(progress);
            progress.level++;
        }

        Save();
    }

    public static void AddShards(string characterName, int amount)
    {
        var progress = Get(characterName);
        progress.shards += Mathf.Max(0, amount);
        Save();
    }

    public static bool TryStarUp(string characterName)
    {
        var progress = Get(characterName);
        var cost = StarCost(progress);
        if (progress.stars >= MaxStars || progress.shards < cost)
        {
            return false;
        }

        progress.shards -= cost;
        progress.stars++;
        Save();
        return true;
    }

    public static bool TryUpgradeSkill(string characterName, PrototypeSkillSlot slot)
    {
        var progress = Get(characterName);
        var level = GetSkillLevel(progress, slot);
        var cost = SkillCost(progress, slot);
        if (level >= MaxSkillLevel || !PrototypeSession.TrySpendReward(PrototypeDungeonType.Credits, cost))
        {
            return false;
        }

        SetSkillLevel(progress, slot, level + 1);
        Save();
        return true;
    }

    public static bool TryUpgradeEquipment(string characterName, PrototypeEquipmentSlot slot)
    {
        var progress = Get(characterName);
        var cost = EquipmentCost(progress, slot);
        if (!PrototypeSession.TrySpendReward(PrototypeDungeonType.Materials, cost))
        {
            return false;
        }

        SetEquipmentLevel(progress, slot, GetEquipmentLevel(progress, slot) + 1);
        Save();
        return true;
    }

    public static float LevelMultiplier(PrototypeCharacterProgress progress)
    {
        return 1f + (progress.level - 1) * 0.04f;
    }

    public static float StarMultiplier(PrototypeCharacterProgress progress)
    {
        return 1f + (progress.stars - 1) * 0.1f;
    }

    public static float AttackMultiplier(PrototypeCharacterProgress progress)
    {
        return LevelMultiplier(progress) * StarMultiplier(progress) * (1f + progress.weaponLevel * 0.03f);
    }

    public static float HealthMultiplier(PrototypeCharacterProgress progress)
    {
        return LevelMultiplier(progress) * StarMultiplier(progress) * (1f + progress.armorLevel * 0.04f);
    }

    public static float DefenseMultiplier(PrototypeCharacterProgress progress)
    {
        return LevelMultiplier(progress) * StarMultiplier(progress) * (1f + progress.coreLevel * 0.03f);
    }

    public static int GetSkillLevel(PrototypeCharacterProgress progress, PrototypeSkillSlot slot)
    {
        switch (slot)
        {
            case PrototypeSkillSlot.Basic: return progress.basicSkillLevel;
            case PrototypeSkillSlot.Passive: return progress.passiveSkillLevel;
            case PrototypeSkillSlot.Active: return progress.activeSkillLevel;
            default: return progress.ultimateSkillLevel;
        }
    }

    public static int GetEquipmentLevel(PrototypeCharacterProgress progress, PrototypeEquipmentSlot slot)
    {
        switch (slot)
        {
            case PrototypeEquipmentSlot.Weapon: return progress.weaponLevel;
            case PrototypeEquipmentSlot.Armor: return progress.armorLevel;
            default: return progress.coreLevel;
        }
    }

    private static void SetSkillLevel(
        PrototypeCharacterProgress progress,
        PrototypeSkillSlot slot,
        int level)
    {
        switch (slot)
        {
            case PrototypeSkillSlot.Basic: progress.basicSkillLevel = level; break;
            case PrototypeSkillSlot.Passive: progress.passiveSkillLevel = level; break;
            case PrototypeSkillSlot.Active: progress.activeSkillLevel = level; break;
            case PrototypeSkillSlot.Ultimate: progress.ultimateSkillLevel = level; break;
        }
    }

    private static void SetEquipmentLevel(
        PrototypeCharacterProgress progress,
        PrototypeEquipmentSlot slot,
        int level)
    {
        switch (slot)
        {
            case PrototypeEquipmentSlot.Weapon: progress.weaponLevel = level; break;
            case PrototypeEquipmentSlot.Armor: progress.armorLevel = level; break;
            case PrototypeEquipmentSlot.Core: progress.coreLevel = level; break;
        }
    }

    private static void EnsureLoaded()
    {
        if (save != null)
        {
            return;
        }

        var json = PlayerPrefs.GetString(SaveKey, string.Empty);
        try
        {
            save = string.IsNullOrEmpty(json)
                ? new PrototypeProgressionSave()
                : JsonUtility.FromJson<PrototypeProgressionSave>(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Progression save was invalid and has been recreated: {exception.Message}");
            save = new PrototypeProgressionSave();
        }

        if (save == null || save.characters == null)
        {
            save = new PrototypeProgressionSave();
        }

        save.version = 2;
    }

    private static void Save()
    {
        EnsureLoaded();
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save));
        PlayerPrefs.Save();
    }
}

internal sealed class PrototypeCharacterScreen : MonoBehaviour
{
    private CombatantDefinition[] roster;
    private CombatantDefinition selected;
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private string message;
    private Action close;

    public void Initialize(CombatantDefinition[] availableRoster, Action closeAction = null)
    {
        roster = availableRoster;
        selected = roster[0];
        message = string.Empty;
        close = closeAction;
    }

    private void OnGUI()
    {
        if (roster == null || selected == null)
        {
            return;
        }

        CreateStyles();
        var previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 360f, Screen.height / 640f, 1f));
        var progress = PrototypeProgression.Get(selected.DisplayName);

        GUI.Box(new Rect(0f, 0f, 360f, 640f), string.Empty);
        GUI.Label(new Rect(20f, 12f, 320f, 30f), "HERO DEVELOPMENT", titleStyle);
        GUI.Label(
            new Rect(12f, 46f, 336f, 24f),
            $"Gold {PrototypeSession.Gold}   XP {PrototypeSession.Experience}   Materials {PrototypeSession.Materials}",
            labelStyle);

        for (var index = 0; index < roster.Length; index++)
        {
            var column = index % 2;
            var row = index / 2;
            var hero = roster[index];
            var heroProgress = PrototypeProgression.Get(hero.DisplayName);
            if (GUI.Button(
                new Rect(12f + column * 172f, 78f + row * 34f, 164f, 30f),
                $"{hero.Rarity} {hero.DisplayName}  Lv{heroProgress.level}  {heroProgress.stars}*"))
            {
                selected = hero;
                message = string.Empty;
            }
        }

        GUI.Box(new Rect(12f, 256f, 336f, 322f), string.Empty);
        GUI.Label(
            new Rect(20f, 262f, 320f, 24f),
            $"{selected.Rarity} · {selected.DisplayName} · {selected.Species} {selected.CombatClass}",
            titleStyle);
        GUI.Label(
            new Rect(20f, 288f, 320f, 22f),
            $"Level {progress.level}  XP {progress.experience}/{PrototypeProgression.ExperienceRequired(progress)}   Stars {progress.stars}  Shards {progress.shards}/{PrototypeProgression.StarCost(progress)}",
            labelStyle);
        GUI.Label(
            new Rect(20f, 314f, 320f, 22f),
            $"HP {ScaledStat(selected.MaxHealth, PrototypeProgression.HealthMultiplier(progress))}   ATK {ScaledStat(selected.Attack, PrototypeProgression.AttackMultiplier(progress))}   DEF {ScaledStat(selected.Defense, PrototypeProgression.DefenseMultiplier(progress))}",
            labelStyle);

        if (GUI.Button(new Rect(20f, 344f, 150f, 34f), "TRAIN · 25 XP"))
        {
            message = PrototypeSession.TrySpendReward(PrototypeDungeonType.Experience, 25)
                ? Train(selected.DisplayName)
                : "Not enough XP resource.";
        }
        if (GUI.Button(new Rect(190f, 344f, 150f, 34f), $"STAR UP · {PrototypeProgression.StarCost(progress)} shards"))
        {
            message = PrototypeProgression.TryStarUp(selected.DisplayName)
                ? "Star rank increased."
                : "Not enough shards or already max stars.";
        }

        DrawSkillButton(new Rect(20f, 390f, 150f, 32f), progress, PrototypeSkillSlot.Basic);
        DrawSkillButton(new Rect(190f, 390f, 150f, 32f), progress, PrototypeSkillSlot.Passive);
        DrawSkillButton(new Rect(20f, 428f, 150f, 32f), progress, PrototypeSkillSlot.Active);
        DrawSkillButton(new Rect(190f, 428f, 150f, 32f), progress, PrototypeSkillSlot.Ultimate);

        DrawEquipmentButton(new Rect(20f, 474f, 98f, 42f), progress, PrototypeEquipmentSlot.Weapon, "ATK +3%");
        DrawEquipmentButton(new Rect(130f, 474f, 98f, 42f), progress, PrototypeEquipmentSlot.Armor, "HP +4%");
        DrawEquipmentButton(new Rect(240f, 474f, 98f, 42f), progress, PrototypeEquipmentSlot.Core, "DEF +3%");

        GUI.Label(new Rect(20f, 526f, 320f, 22f), message, labelStyle);
        if (GUI.Button(new Rect(110f, 590f, 140f, 38f), "BACK TO IDLE"))
        {
            if (close == null)
            {
                PrototypeSession.ReturnToIdle();
            }
            else
            {
                close();
                Destroy(this);
            }
        }

        GUI.matrix = previousMatrix;
    }

    private void DrawSkillButton(Rect area, PrototypeCharacterProgress progress, PrototypeSkillSlot slot)
    {
        var level = PrototypeProgression.GetSkillLevel(progress, slot);
        var cost = PrototypeProgression.SkillCost(progress, slot);
        if (GUI.Button(area, $"{slot} Lv{level} · {cost} Gold"))
        {
            message = PrototypeProgression.TryUpgradeSkill(selected.DisplayName, slot)
                ? $"{slot} skill upgraded."
                : "Not enough Gold or skill already maxed.";
        }
    }

    private void DrawEquipmentButton(
        Rect area,
        PrototypeCharacterProgress progress,
        PrototypeEquipmentSlot slot,
        string substat)
    {
        var level = PrototypeProgression.GetEquipmentLevel(progress, slot);
        var cost = PrototypeProgression.EquipmentCost(progress, slot);
        if (GUI.Button(area, $"{slot} Lv{level}\n{substat} · {cost} Mat"))
        {
            message = PrototypeProgression.TryUpgradeEquipment(selected.DisplayName, slot)
                ? $"{slot} upgraded."
                : "Not enough Materials.";
        }
    }

    private static string Train(string characterName)
    {
        PrototypeProgression.AddExperience(characterName, 25);
        return "Character gained 25 XP.";
    }

    private static int ScaledStat(int value, float multiplier)
    {
        return Mathf.RoundToInt(value * multiplier);
    }

    private void CreateStyles()
    {
        if (titleStyle != null)
        {
            return;
        }

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = new Color(0.42f, 0.9f, 1f);
        labelStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 10,
            wordWrap = true
        };
        labelStyle.normal.textColor = Color.white;
    }
}
