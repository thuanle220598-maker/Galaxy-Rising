using System.IO;
using System.Linq;
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

    public static void GenerateMissingContent()
    {
        EnsureFolder(Root, "Skills");
        EnsureFolder(Root, "Stages");
        EnsureFolder(Root, "Dungeons");
        EnsureFolder(Root + "/Combatants", "Roster");
        EnsureFolder(Root + "/Combatants", "Monsters");

        CreateSkillSets();
        ConfigureFireGodHeavenlyDemon();
        ConfigureAurelia();
        ConfigureKronos();
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
        CreateMonster(
            "Watcher", "Watcher.png", PrototypeSpecies.Fantasy, PrototypeCombatClass.Fighter,
            new Color(0.88f, 0.72f, 0.55f), 180, 26, 7, 1.9f, 1f, 0);
        CreateMonster(
            "Emberwing", "Emberwing.png", PrototypeSpecies.Dragon, PrototypeCombatClass.Fighter,
            new Color(0.65f, 0.12f, 0.16f), 165, 31, 6, 2.15f, 0.9f, 1);
        CreateMonster(
            "Voidroot", "Voidroot.png", PrototypeSpecies.Cosmic, PrototypeCombatClass.Mage,
            new Color(0.22f, 0.12f, 0.35f), 135, 32, 4, 1.75f, 1.15f, 2);
        CreateMonster(
            "Toxibot", "Toxibot.png", PrototypeSpecies.Machine, PrototypeCombatClass.Tanker,
            new Color(0.35f, 0.2f, 0.58f), 230, 22, 12, 1.55f, 1.2f, 3);
        CreateMonster(
            "Razorjaw", "Razorjaw.png", PrototypeSpecies.Fantasy, PrototypeCombatClass.Tanker,
            new Color(0.12f, 0.35f, 0.55f), 210, 25, 10, 1.6f, 1.1f, 4, true);
        CreateMonster(
            "Tide Slime", "TideSlime.png", PrototypeSpecies.Ocean, PrototypeCombatClass.Support,
            new Color(0.08f, 0.58f, 0.8f), 160, 24, 6, 1.7f, 1.1f, 5);

        AssetDatabase.SaveAssets();
    }

    private static void CreateSkillSets()
    {
        CreateSkill(PrototypeSkillKit.Nova,
            S("Flame Strike", "Fire attack that applies Ash; at three Ash, consumes the marks for splash and a delayed explosion.", 0f, 0.56f, 0.3214286f, 1f, 18),
            S("Ignition Core", "Direct Fire hits build up to three Ash Marks; each mark reduces Fire Resistance by 5%.", 0f, 0.2f, 0.5f, 1f),
            S("Hellfire Impact", "Fire AoE and knock-up that leaves a slowing Magma Pool; three Ash trigger True Damage and a decaying slow.", 6f, 1.3f, 0.6153846f, 1.45f),
            S("Crimson Gale", "Directional Fire wave with knockback, Ash missing-health detonation, Grounded Scorch, and Magma-to-Firestorm conversion.", 0f, 1.3f, 0.6153846f, 2.2f),
            S("Thermal Resonance", "Fire damage against Burning or detonated targets restores energy and builds Heat; critical health triggers Flame Shield.", 0f, 0.2f, 0.5f, 1f),
            S("Everburning Embers", "Fire DoT kills spread two Ash Marks and half of the remaining DoT to nearby enemies.", 0f, 0.2f, 0.5f, 1f));
        CreateSkill(PrototypeSkillKit.Ion,
            S("Arc Sequence", "Every fourth hit chains lightning to another enemy.", 0f, 0.34f, 0.5f, 1f, 18),
            S("Phase Shift", "Every fifth incoming hit is ignored and restores energy.", 0f, 0.2f, 0.5f, 1f),
            S("Static Field", "Damage and stun the target, then arc to another enemy.", 7f, 0.5f, 0.54f, 0.9f),
            S("Volt Rush", "Strike three times and retarget whenever an enemy falls.", 0f, 0.68f, 0.5f, 0.85f));
        CreateSkill(PrototypeSkillKit.Astra,
            S("Water Serpent", "Each attack creates two Hydro Beads for living allies.", 0f, 0.42f, 0.5f, 1f, 18),
            S("Oceanic Scales", "Hydro Beads restore energy and shield allies, or hasten Aurelia and reduce her Active cooldown.", 0f, 0.2f, 0.5f, 1f),
            S("Dragon Realm", "Create a healing leyline domain that reduces damage and absorbs Hydro Beads to last longer.", 6f, 0.7f, 0.57f, 1f),
            S("Draconian Aegis", "Shield the team for 8 seconds with damage, speed and control-immunity blessings.", 0f, 0.78f, 0.5f, 1f),
            S("Tidal Cleansing", "Aurelia's healing and shields cleanse nearby allies and are stronger on hard-controlled targets.", 0f, 0.2f, 0.5f, 1f),
            S("Dragon Pulse Aura", "Nearby allies resist damage and debuffs; burst damage triggers a defensive dragon-pressure wave.", 0f, 0.2f, 0.5f, 1f));
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
            S("Void Claw", "Void strikes infect enemies; at ten Void Resonance, the next strike releases a slowing shockwave.", 0f, 0.48f, 0.5f, 1f, 18),
            S("Gravitational Crust", "Damage builds up to ten Void Resonance, increasing defenses and reflecting damage.", 0f, 0.2f, 0.5f, 1f),
            S("Singularity Pull", "Pull nearby enemies inward, infect them and force them to attack Kronos while he takes less damage.", 7f, 0.82f, 0.56f, 1f),
            S("Cosmic Leviathan", "Transform for ten seconds with greater health, defenses, reach, cleaving lifesteal attacks and a decay aura.", 0f, 1.05f, 0.6f, 1f),
            S("Void Parasite", "Attacks reduce enemy healing and shielding; infected attackers feed temporary shields back to Kronos.", 0f, 0.2f, 0.5f, 1f),
            S("Devourer's Constitution", "Max Health becomes damage, and a fatal blow triggers a draining Black Hole Collapse once per cooldown.", 0f, 0.2f, 0.5f, 1f));
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
        PrototypeSkillData ultimate,
        PrototypeSkillData passive2 = null,
        PrototypeSkillData passive3 = null)
    {
        var path = $"{Root}/Skills/{kit}.asset";
        if (AssetDatabase.LoadAssetAtPath<SkillDefinition>(path) != null)
        {
            return;
        }

        var asset = ScriptableObject.CreateInstance<SkillDefinition>();
        asset.EditorConfigure(kit, basic, passive, active, ultimate, passive2, passive3);
        AssetDatabase.CreateAsset(asset, path);
    }

    private static void ConfigureFireGodHeavenlyDemon()
    {
        const string combatantPath = "Assets/Resources/Combatants/Allies/Nova.asset";
        const string skillPath = "Assets/Resources/Skills/Nova.asset";
        var combatant = AssetDatabase.LoadAssetAtPath<CombatantDefinition>(combatantPath);
        var skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(skillPath);
        if (combatant == null || skill == null)
        {
            return;
        }

        combatant.EditorConfigure(
            PrototypeCharacterNames.FireGodHeavenlyDemon,
            PrototypeSpecies.Human,
            PrototypeCombatClass.Mage,
            PrototypeRarity.SSR,
            new Color(1f, 0.28f, 0.05f),
            combatant.MaxHealth,
            combatant.Attack,
            combatant.Defense,
            combatant.MoveSpeed,
            combatant.AttackInterval,
            PrototypeSkillKit.Nova,
            combatant.FormationOrder);
        skill.EditorConfigure(
            PrototypeSkillKit.Nova,
            S("Flame Strike", "Fire attack that applies Ash; at three Ash, consumes the marks for splash and a delayed explosion.", 0f, 0.56f, 0.3214286f, 1f, 18),
            S("Ignition Core", "Direct Fire hits build up to three Ash Marks; each mark reduces Fire Resistance by 5%.", 0f, 0.2f, 0.5f, 1f),
            S("Hellfire Impact", "Fire AoE and knock-up that leaves a slowing Magma Pool; three Ash trigger True Damage and a decaying slow.", 6f, 1.3f, 0.6153846f, 1.45f),
            S("Crimson Gale", "Directional Fire wave with knockback, Ash missing-health detonation, Grounded Scorch, and Magma-to-Firestorm conversion.", 0f, 1.3f, 0.6153846f, 2.2f),
            S("Thermal Resonance", "Fire damage against Burning or detonated targets restores energy and builds Heat; critical health triggers Flame Shield.", 0f, 0.2f, 0.5f, 1f),
            S("Everburning Embers", "Fire DoT kills spread two Ash Marks and half of the remaining DoT to nearby enemies.", 0f, 0.2f, 0.5f, 1f));

        var idle = LoadSprites("Assets/Art/Characters/Nova/Combat/FireGodIdle.png");
        var run = LoadSprites("Assets/Art/Characters/Nova/Combat/FireGodRun.png");
        var attack = LoadSprites("Assets/Art/Characters/Nova/Combat/NovaFlameBasicAttack.png");
        var active = LoadSprites("Assets/Art/Characters/Nova/Combat/FireGodActive.png");
        var ultimate = LoadSprites("Assets/Art/Characters/Nova/Combat/FireGodUltimate.png");
        var hit = LoadSprites("Assets/Art/Characters/Nova/Combat/FireGodHit.png");
        var death = LoadSprites("Assets/Art/Characters/Nova/Combat/FireGodDeath.png");
        if (idle.Length == 8 && run.Length == 13 && attack.Length == 8 && active.Length == 13 &&
            ultimate.Length == 13 && hit.Length == 9 && death.Length == 9)
        {
            combatant.EditorConfigureAnimationFrames(
                idle, run, attack, active, ultimate, hit, death);
        }

        EditorUtility.SetDirty(combatant);
        EditorUtility.SetDirty(skill);
    }

    private static void ConfigureAurelia()
    {
        const string combatantPath = "Assets/Resources/Combatants/Allies/Astra.asset";
        const string skillPath = "Assets/Resources/Skills/Astra.asset";
        var combatant = AssetDatabase.LoadAssetAtPath<CombatantDefinition>(combatantPath);
        var skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(skillPath);
        if (combatant == null || skill == null)
        {
            return;
        }

        combatant.EditorConfigure(
            PrototypeCharacterNames.Aurelia,
            PrototypeSpecies.Dragon,
            PrototypeCombatClass.Support,
            combatant.Rarity,
            new Color(0.22f, 0.78f, 0.88f),
            combatant.MaxHealth,
            combatant.Attack,
            combatant.Defense,
            combatant.MoveSpeed,
            combatant.AttackInterval,
            PrototypeSkillKit.Astra,
            combatant.FormationOrder);
        skill.EditorConfigure(
            PrototypeSkillKit.Astra,
            S("Water Serpent", "Each attack creates two Hydro Beads for living allies.", 0f, 0.42f, 0.5f, 1f, 18),
            S("Oceanic Scales", "Hydro Beads restore energy and shield allies, or hasten Aurelia and reduce her Active cooldown.", 0f, 0.2f, 0.5f, 1f),
            S("Dragon Realm", "Create a healing leyline domain that reduces damage and absorbs Hydro Beads to last longer.", 6f, 0.7f, 0.57f, 1f),
            S("Draconian Aegis", "Shield the team for 8 seconds with damage, speed and control-immunity blessings.", 0f, 0.78f, 0.5f, 1f),
            S("Tidal Cleansing", "Aurelia's healing and shields cleanse nearby allies and are stronger on hard-controlled targets.", 0f, 0.2f, 0.5f, 1f),
            S("Dragon Pulse Aura", "Nearby allies resist damage and debuffs; burst damage triggers a defensive dragon-pressure wave.", 0f, 0.2f, 0.5f, 1f));

        var idle = LoadSprites("Assets/Art/Characters/Aurelia/Combat/AureliaIdle.png");
        var run = LoadSprites("Assets/Art/Characters/Aurelia/Combat/AureliaRun.png");
        var attack = LoadSprites("Assets/Art/Characters/Aurelia/Combat/AureliaBasic.png");
        var active = LoadSprites("Assets/Art/Characters/Aurelia/Combat/AureliaActive.png");
        var ultimate = LoadSprites("Assets/Art/Characters/Aurelia/Combat/AureliaUltimate.png");
        var hit = LoadSprites("Assets/Art/Characters/Aurelia/Combat/AureliaHit.png");
        var death = LoadSprites("Assets/Art/Characters/Aurelia/Combat/AureliaDeath.png");
        if (idle.Length == 6 && run.Length == 8 && attack.Length == 6 && active.Length == 10 &&
            ultimate.Length == 12 && hit.Length == 4 && death.Length == 8)
        {
            combatant.EditorConfigureAnimationFrames(idle, run, attack, active, ultimate, hit, death);
        }

        EditorUtility.SetDirty(combatant);
        EditorUtility.SetDirty(skill);
    }

    private static void ConfigureKronos()
    {
        const string combatantPath = "Assets/Resources/Combatants/Allies/Brakk.asset";
        const string skillPath = "Assets/Resources/Skills/Brakk.asset";
        var combatant = AssetDatabase.LoadAssetAtPath<CombatantDefinition>(combatantPath);
        var skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(skillPath);
        if (combatant == null || skill == null)
        {
            return;
        }

        combatant.EditorConfigure(
            PrototypeCharacterNames.Kronos,
            PrototypeSpecies.Cosmic,
            PrototypeCombatClass.Tanker,
            combatant.Rarity,
            new Color(0.48f, 0.2f, 0.72f),
            combatant.MaxHealth,
            combatant.Attack,
            combatant.Defense,
            combatant.MoveSpeed,
            combatant.AttackInterval,
            PrototypeSkillKit.Brakk,
            combatant.FormationOrder);
        skill.EditorConfigure(
            PrototypeSkillKit.Brakk,
            S("Void Claw", "Void strikes infect enemies; at ten Void Resonance, the next strike releases a slowing shockwave.", 0f, 0.48f, 0.5f, 1f, 18),
            S("Gravitational Crust", "Damage builds up to ten Void Resonance, increasing defenses and reflecting damage.", 0f, 0.2f, 0.5f, 1f),
            S("Singularity Pull", "Pull nearby enemies inward, infect them and force them to attack Kronos while he takes less damage.", 7f, 0.82f, 0.56f, 1f),
            S("Cosmic Leviathan", "Transform for ten seconds with greater health, defenses, reach, cleaving lifesteal attacks and a decay aura.", 0f, 1.05f, 0.6f, 1f),
            S("Void Parasite", "Attacks reduce enemy healing and shielding; infected attackers feed temporary shields back to Kronos.", 0f, 0.2f, 0.5f, 1f),
            S("Devourer's Constitution", "Max Health becomes damage, and a fatal blow triggers a draining Black Hole Collapse once per cooldown.", 0f, 0.2f, 0.5f, 1f));

        var idle = LoadSprites("Assets/Art/Characters/Kronos/Combat/KronosIdle.png");
        var run = LoadSprites("Assets/Art/Characters/Kronos/Combat/KronosRun.png");
        var attack = LoadSprites("Assets/Art/Characters/Kronos/Combat/KronosBasic.png");
        var active = LoadSprites("Assets/Art/Characters/Kronos/Combat/KronosActive.png");
        var ultimate = LoadSprites("Assets/Art/Characters/Kronos/Combat/KronosUltimate.png");
        var hit = LoadSprites("Assets/Art/Characters/Kronos/Combat/KronosHit.png");
        var death = LoadSprites("Assets/Art/Characters/Kronos/Combat/KronosDeath.png");
        if (idle.Length == 8 && run.Length == 8 && attack.Length == 8 && active.Length == 12 &&
            ultimate.Length == 14 && hit.Length == 5 && death.Length == 10)
        {
            combatant.EditorConfigureAnimationFrames(idle, run, attack, active, ultimate, hit, death);
        }

        EditorUtility.SetDirty(combatant);
        EditorUtility.SetDirty(skill);
    }

    private static Sprite[] LoadSprites(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name)
            .ToArray();
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

    private static void CreateMonster(
        string name,
        string spriteFile,
        PrototypeSpecies species,
        PrototypeCombatClass combatClass,
        Color color,
        int health,
        int attack,
        int defense,
        float speed,
        float attackInterval,
        int order,
        bool sourceFacesLeft = false)
    {
        var path = $"{Root}/Combatants/Monsters/{name.Replace(" ", string.Empty)}.asset";
        if (AssetDatabase.LoadAssetAtPath<CombatantDefinition>(path) != null)
        {
            return;
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Monsters/{spriteFile}");
        if (sprite == null)
        {
            Debug.LogWarning($"Monster sprite is missing: {spriteFile}");
            return;
        }

        var asset = ScriptableObject.CreateInstance<CombatantDefinition>();
        asset.EditorConfigure(
            name, species, combatClass, PrototypeRarity.R, color, health, attack, defense,
            speed, attackInterval, PrototypeSkillKit.None, order);
        asset.EditorConfigureStaticSprite(sprite, sourceFacesLeft);
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
