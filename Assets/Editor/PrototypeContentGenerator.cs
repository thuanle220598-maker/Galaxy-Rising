using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class PrototypeContentGenerator
{
    private const string Root = "Assets/Resources";

    static PrototypeContentGenerator()
    {
        EditorApplication.delayCall += GenerateMissingContent;
    }

    private static void GenerateMissingContent()
    {
        EnsureFolder(Root, "Skills");
        EnsureFolder(Root, "Stages");
        EnsureFolder(Root, "Dungeons");
        EnsureFolder(Root + "/Combatants", "Roster");

        CreateSkillSets();
        for (var stage = 1; stage <= 20; stage++)
        {
            CreateStage(stage);
        }

        CreateDungeon(PrototypeDungeonType.Credits, 100f, 100f, 60f, 0.18f);
        CreateDungeon(PrototypeDungeonType.Experience, 80f, 80f, 60f, 0.18f);
        CreateDungeon(PrototypeDungeonType.Materials, 5f, 5f, 75f, 0.2f);
        CreateHero(
            "Mira", PrototypeSpecies.Ocean, PrototypeCombatClass.Support, PrototypeRarity.SSR,
            new Color(0.12f, 0.78f, 0.82f), 145, 24, 7, 1.8f, 1.1f, PrototypeSkillKit.Mira, 0);
        CreateHero(
            "Drake", PrototypeSpecies.Dragon, PrototypeCombatClass.Fighter, PrototypeRarity.UR,
            new Color(0.92f, 0.24f, 0.1f), 185, 31, 8, 2.05f, 0.92f, PrototypeSkillKit.Drake, 1);

        AssetDatabase.SaveAssets();
    }

    private static void CreateSkillSets()
    {
        CreateSkill(PrototypeSkillKit.Nova,
            S("Photon Brand", "Every third hit marks the target for amplified Nova damage.", 0f, 0.36f, 0.5f, 1f, 18),
            S("Aegis Reactor", "At half health, gain a shield; at critical health, attack and charge faster.", 0f, 0.2f, 0.5f, 1f),
            S("Solar Thrust", "Damage and mark one enemy, then shield Nova.", 6f, 0.52f, 0.52f, 1.45f),
            S("Stellar Breaker", "Heavy single-target strike with splash damage to all enemies.", 0f, 0.72f, 0.53f, 2.2f));
        CreateSkill(PrototypeSkillKit.Ion,
            S("Arc Sequence", "Every fourth hit chains lightning to another enemy.", 0f, 0.34f, 0.5f, 1f, 18),
            S("Phase Shift", "Every fifth incoming hit is ignored and restores energy.", 0f, 0.2f, 0.5f, 1f),
            S("Static Field", "Damage and stun the target, then arc to another enemy.", 7f, 0.5f, 0.54f, 0.9f),
            S("Volt Rush", "Strike three times and retarget whenever an enemy falls.", 0f, 0.68f, 0.5f, 0.85f));
        CreateSkill(PrototypeSkillKit.Astra,
            S("Guiding Light", "Every third attack heals the weakest ally.", 0f, 0.38f, 0.5f, 1f, 18),
            S("Sanctuary", "Once per battle, shield an ally who drops below 30% health.", 0f, 0.2f, 0.5f, 1f),
            S("Star Ward", "Heal and shield the weakest ally.", 6f, 0.56f, 0.48f, 0.8f),
            S("Astral Renewal", "Heal the whole team and heavily protect the weakest ally.", 0f, 0.78f, 0.5f, 1f));
        CreateSkill(PrototypeSkillKit.Krag,
            S("Gravity Fist", "Front-line attacks build energy through sustained contact.", 0f, 0.4f, 0.5f, 1f, 18),
            S("Last Bastion", "Gain defense above 50% health and a large shield when critically wounded.", 0f, 0.2f, 0.5f, 1f),
            S("Crushing Orbit", "Damage and weaken an enemy while shielding Krag.", 7f, 0.58f, 0.5f, 1.15f),
            S("Gravity Bulwark", "Gain a massive shield and force all enemies to attack Krag.", 0f, 0.78f, 0.5f, 1f));
        CreateSkill(PrototypeSkillKit.Vex,
            S("Backline Reap", "Prioritize back-row targets and deal more damage to wounded enemies.", 0f, 0.3f, 0.52f, 1f, 18),
            S("Momentum", "Kills grant haste and a large amount of energy.", 0f, 0.2f, 0.5f, 1f),
            S("Void Step", "Burst a vulnerable back-row target and gain haste.", 5f, 0.46f, 0.56f, 1.7f),
            S("Crimson Execute", "Attack the weakest enemy; damage rises sharply below 30% health.", 0f, 0.64f, 0.58f, 2.4f));
        CreateSkill(PrototypeSkillKit.Rook,
            S("Piercing Cycle", "Every third shot ignores half of the target's defense.", 0f, 0.38f, 0.5f, 1f, 18),
            S("Ballistic Nest", "Maintains safe back-row positioning and sustained fire.", 0f, 0.2f, 0.5f, 1f),
            S("Pinning Shot", "Pierce armor and slow one enemy.", 6.5f, 0.54f, 0.54f, 1.5f),
            S("Rail Barrage", "Damage, pierce and slow every enemy.", 0f, 0.74f, 0.52f, 1.15f));
        CreateSkill(PrototypeSkillKit.Lyra,
            S("Twin Ray", "Every fourth arrow strikes a second target.", 0f, 0.36f, 0.5f, 1f, 18),
            S("Solar Range", "Long-range positioning keeps Lyra behind the front line.", 0f, 0.2f, 0.5f, 1f),
            S("Sunpiercer", "Damage and burn one target.", 6f, 0.52f, 0.54f, 1.35f),
            S("Helios Rain", "Damage and burn the entire enemy team.", 0f, 0.76f, 0.52f, 1.1f));
        CreateSkill(PrototypeSkillKit.Brakk,
            S("Guard Rhythm", "Every fourth attack grants Brakk a shield.", 0f, 0.42f, 0.5f, 1f, 18),
            S("Living Rampart", "Blocks access to allies by holding the front row.", 0f, 0.2f, 0.5f, 1f),
            S("Guard Link", "Shield and empower the weakest ally.", 7f, 0.58f, 0.48f, 1f),
            S("Stoneheart Pact", "Grant a durable shield to every ally.", 0f, 0.8f, 0.5f, 1f));
        CreateSkill(PrototypeSkillKit.Hex,
            S("Forked Virus", "Every third attack splashes to a second target.", 0f, 0.4f, 0.5f, 1f, 18),
            S("Persistent Corruption", "Poison continues damaging targets after the initial hit.", 0f, 0.2f, 0.5f, 1f),
            S("Corruption Code", "Poison a target and reduce its attack.", 6.5f, 0.56f, 0.52f, 0.85f),
            S("Singularity Mine", "Heavy damage followed by poison and slow.", 0f, 0.78f, 0.52f, 2.4f));
        CreateSkill(PrototypeSkillKit.Nyx,
            S("Drowning Verse", "Every fourth attack drains enemy energy and restores Nyx's energy.", 0f, 0.4f, 0.5f, 1f, 18),
            S("Abyssal Choir", "Supports the team safely from the back row.", 0f, 0.2f, 0.5f, 1f),
            S("Abyssal Hymn", "Heal, cleanse and restore energy to the weakest ally.", 6f, 0.58f, 0.48f, 0.7f),
            S("Night Bloom", "Heal, cleanse, hasten and empower the entire team.", 0f, 0.82f, 0.5f, 0.55f));
        CreateSkill(PrototypeSkillKit.Mira,
            S("Tidal Echo", "Every third hit slows the enemy and shields the weakest ally.", 0f, 0.4f, 0.52f, 1f, 18),
            S("Deep Current", "Protecting wounded allies grants extra energy.", 0f, 0.2f, 0.5f, 1f),
            S("Undertow", "Damage and slow enemies while healing the weakest ally.", 6f, 0.58f, 0.52f, 0.85f),
            S("Leviathan's Grace", "Heal and shield allies, then slow every enemy.", 0f, 0.84f, 0.52f, 0.45f));
        CreateSkill(PrototypeSkillKit.Drake,
            S("Ember Claw", "Every third strike applies a stacking burn pattern.", 0f, 0.34f, 0.55f, 1f, 18),
            S("Dragon Fury", "Below 50% health, gain haste, attack and energy once per battle.", 0f, 0.2f, 0.5f, 1f),
            S("Dragon Dive", "Burst and burn one enemy while shielding Drake.", 6.5f, 0.5f, 0.58f, 1.65f),
            S("Cataclysm Roar", "Damage, burn and weaken the entire enemy team.", 0f, 0.76f, 0.55f, 1.25f));
    }

    private static PrototypeSkillData S(
        string name,
        string description,
        float cooldown,
        float duration,
        float hitFrame,
        float power,
        int energy = 0)
    {
        return new PrototypeSkillData(name, description, cooldown, duration, hitFrame, power, energy);
    }

    private static void CreateSkill(
        PrototypeSkillKit kit,
        PrototypeSkillData basic,
        PrototypeSkillData passive,
        PrototypeSkillData active,
        PrototypeSkillData ultimate)
    {
        var path = $"{Root}/Skills/{kit}.asset";
        if (AssetDatabase.LoadAssetAtPath<SkillDefinition>(path) != null)
        {
            return;
        }

        var asset = ScriptableObject.CreateInstance<SkillDefinition>();
        asset.EditorConfigure(kit, basic, passive, active, ultimate);
        AssetDatabase.CreateAsset(asset, path);
    }

    private static void CreateStage(int stage)
    {
        var path = $"{Root}/Stages/Stage_{stage:00}.asset";
        DeleteMissingScriptAsset(path);
        if (AssetDatabase.LoadAssetAtPath<PrototypeStageDefinition>(path) != null)
        {
            return;
        }
        AssetDatabase.DeleteAsset(path);

        var boss = stage % 5 == 0;
        var multiplier = 1f + (stage - 1) * 0.08f;
        var gold = (7 + stage * 3) * (boss ? 2 : 1);
        var experience = (4 + stage * 2) * (boss ? 2 : 1);
        var tickets = boss ? stage >= 15 ? 2 : 1 : 0;
        var asset = ScriptableObject.CreateInstance<PrototypeStageDefinition>();
        asset.EditorConfigure(stage, boss, multiplier, gold, experience, tickets);
        AssetDatabase.CreateAsset(asset, path);
    }

    private static void CreateDungeon(
        PrototypeDungeonType type,
        float baseReward,
        float rewardPerLevel,
        float timeLimit,
        float growth)
    {
        var path = $"{Root}/Dungeons/{type}.asset";
        DeleteMissingScriptAsset(path);
        if (AssetDatabase.LoadAssetAtPath<PrototypeDungeonDefinition>(path) != null)
        {
            return;
        }
        AssetDatabase.DeleteAsset(path);

        var asset = ScriptableObject.CreateInstance<PrototypeDungeonDefinition>();
        asset.EditorConfigure(type, baseReward, rewardPerLevel, timeLimit, growth);
        AssetDatabase.CreateAsset(asset, path);
    }

    private static void CreateHero(
        string name,
        PrototypeSpecies species,
        PrototypeCombatClass combatClass,
        PrototypeRarity rarity,
        Color color,
        int health,
        int attack,
        int defense,
        float speed,
        float attackInterval,
        PrototypeSkillKit kit,
        int order)
    {
        var path = $"{Root}/Combatants/Roster/{name}.asset";
        if (AssetDatabase.LoadAssetAtPath<CombatantDefinition>(path) != null)
        {
            return;
        }

        var asset = ScriptableObject.CreateInstance<CombatantDefinition>();
        asset.EditorConfigure(
            name, species, combatClass, rarity, color, health, attack, defense,
            speed, attackInterval, kit, order);
        AssetDatabase.CreateAsset(asset, path);
    }

    private static void EnsureFolder(string parent, string name)
    {
        var path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static void DeleteMissingScriptAsset(string path)
    {
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        var absolutePath = Path.Combine(projectRoot, path);
        if (File.Exists(absolutePath) && File.ReadAllText(absolutePath).Contains("m_Script: {fileID: 0}"))
        {
            AssetDatabase.DeleteAsset(path);
        }
    }
}
