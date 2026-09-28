using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

internal enum PrototypeTeam
{
    Allies,
    Enemies
}

internal enum PrototypeGameMode
{
    Idle,
    Dungeon,
    PvP
}

public enum PrototypeDungeonType
{
    Credits,
    Experience,
    Materials
}

[System.Serializable]
internal sealed class PrototypeSquadSave
{
    public int version = 2;
    public List<string> names = new List<string>();
    public List<int> rows = new List<int>();
}

internal static class PrototypeSession
{
    private const string IdleStageKey = "Prototype.IdleStage";
    private const string IdleChallengePendingKey = "Prototype.IdleBossPending";
    private const string LastIdleClaimKey = "Prototype.LastIdleClaim";
    private const string GoldKey = "Prototype.Gold";
    private const string ExperienceKey = "Prototype.Experience";
    private const string MaterialsKey = "Prototype.Materials";
    private const string SquadKey = "Prototype.Squad";

    public static PrototypeGameMode Mode { get; private set; }
    public static PrototypeDungeonType DungeonType { get; private set; }
    public static int DungeonLevel { get; private set; } = 1;
    public static bool IdleFarmMode { get; private set; }
    private static bool launchBattle;
    private static bool openSquadSetup;
    private static bool openCharacterScreen;
    private static string[] squadNames;
    private static PrototypeFormationRow[] squadRows;

    public static int IdleStage => Mathf.Max(1, PlayerPrefs.GetInt(IdleStageKey, 1));
    public static bool IdleChallengePending => PlayerPrefs.GetInt(IdleChallengePendingKey, 0) == 1;
    public static int IdleRewardStage => IdleChallengePending ? Mathf.Max(1, IdleStage - 1) : IdleStage;
    public static int Gold => PlayerPrefs.GetInt(GoldKey, 0);
    public static int Experience => PlayerPrefs.GetInt(ExperienceKey, 0);
    public static int Materials => PlayerPrefs.GetInt(MaterialsKey, 0);

    public static void EnsureIdleClock()
    {
        if (!PlayerPrefs.HasKey(LastIdleClaimKey))
        {
            PlayerPrefs.SetInt(LastIdleClaimKey, GetUnixTime());
            PlayerPrefs.Save();
        }
    }

    public static void Configure(
        PrototypeGameMode mode,
        PrototypeDungeonType dungeonType,
        int dungeonLevel,
        bool idleFarmMode,
        CombatantDefinition[] squad,
        PrototypeFormationRow[] rows)
    {
        Mode = mode;
        DungeonType = dungeonType;
        DungeonLevel = Mathf.Max(1, dungeonLevel);
        IdleFarmMode = idleFarmMode;
        squadNames = new string[squad.Length];
        for (var index = 0; index < squad.Length; index++)
        {
            squadNames[index] = squad[index].DisplayName;
        }

        squadRows = (PrototypeFormationRow[])rows.Clone();
        SaveSquad();
    }

    public static bool TryConsumeBattle(
        CombatantDefinition[] roster,
        out CombatantDefinition[] squad,
        out PrototypeFormationRow[] rows)
    {
        squad = null;
        rows = null;
        if (!launchBattle || !TryGetSquad(roster, out squad, out rows))
        {
            return false;
        }

        launchBattle = false;
        return true;
    }

    public static bool TryGetSquad(
        CombatantDefinition[] roster,
        out CombatantDefinition[] squad,
        out PrototypeFormationRow[] rows)
    {
        squad = null;
        rows = null;
        if (squadNames == null || squadRows == null)
        {
            LoadSquad();
            if (squadNames == null || squadRows == null)
            {
                return false;
            }
        }

        squad = new CombatantDefinition[squadNames.Length];
        for (var squadIndex = 0; squadIndex < squadNames.Length; squadIndex++)
        {
            foreach (var definition in roster)
            {
                if (definition.DisplayName == squadNames[squadIndex])
                {
                    squad[squadIndex] = definition;
                    break;
                }
            }

            if (squad[squadIndex] == null)
            {
                return false;
            }
        }

        rows = (PrototypeFormationRow[])squadRows.Clone();
        return true;
    }

    public static void QueueActivity(
        PrototypeGameMode mode,
        PrototypeDungeonType dungeonType = PrototypeDungeonType.Credits,
        int dungeonLevel = 1)
    {
        Mode = mode;
        DungeonType = dungeonType;
        DungeonLevel = Mathf.Max(1, dungeonLevel);
        IdleFarmMode = false;
        launchBattle = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void OpenSquadSetup()
    {
        openSquadSetup = true;
        launchBattle = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void OpenCharacterScreen()
    {
        openCharacterScreen = true;
        launchBattle = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static bool ConsumeSquadSetupRequest()
    {
        var requested = openSquadSetup;
        openSquadSetup = false;
        return requested;
    }

    public static bool ConsumeCharacterScreenRequest()
    {
        var requested = openCharacterScreen;
        openCharacterScreen = false;
        return requested;
    }

    public static void ReturnToIdle()
    {
        Mode = PrototypeGameMode.Idle;
        IdleFarmMode = IdleChallengePending;
        launchBattle = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void SetIdleStage(int stage)
    {
        PlayerPrefs.SetInt(IdleStageKey, Mathf.Max(1, stage));
        PlayerPrefs.Save();
    }

    public static void SetChallengePending(bool pending)
    {
        PlayerPrefs.SetInt(IdleChallengePendingKey, pending ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static int GetUnclaimedIdleGold()
    {
        var elapsed = GetUnclaimedIdleSeconds();
        return Mathf.FloorToInt(elapsed * (0.04f + IdleRewardStage * 0.01f));
    }

    public static int GetUnclaimedIdleExperience()
    {
        var elapsed = GetUnclaimedIdleSeconds();
        return Mathf.FloorToInt(elapsed * (0.025f + IdleRewardStage * 0.006f));
    }

    public static void ClaimIdleRewards(out int gold, out int experience)
    {
        gold = GetUnclaimedIdleGold();
        experience = GetUnclaimedIdleExperience();
        PlayerPrefs.SetInt(GoldKey, Gold + gold);
        PlayerPrefs.SetInt(ExperienceKey, Experience + experience);
        PlayerPrefs.SetInt(LastIdleClaimKey, GetUnixTime());
        PlayerPrefs.Save();
    }

    public static void AddReward(PrototypeDungeonType type, int amount)
    {
        var key = type == PrototypeDungeonType.Credits
            ? GoldKey
            : type == PrototypeDungeonType.Experience ? ExperienceKey : MaterialsKey;
        PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key, 0) + Mathf.Max(0, amount));
        PlayerPrefs.Save();
    }

    public static void AddIdleRewards(int gold, int experience)
    {
        PlayerPrefs.SetInt(GoldKey, Gold + Mathf.Max(0, gold));
        PlayerPrefs.SetInt(ExperienceKey, Experience + Mathf.Max(0, experience));
        PlayerPrefs.Save();
    }

    public static bool TrySpendReward(PrototypeDungeonType type, int amount)
    {
        var key = type == PrototypeDungeonType.Credits
            ? GoldKey
            : type == PrototypeDungeonType.Experience ? ExperienceKey : MaterialsKey;
        var balance = PlayerPrefs.GetInt(key, 0);
        if (amount < 0 || balance < amount)
        {
            return false;
        }

        PlayerPrefs.SetInt(key, balance - amount);
        PlayerPrefs.Save();
        return true;
    }

    public static int GetDungeonReward(PrototypeDungeonType type, int level)
    {
        var definition = PrototypeContentCatalog.GetDungeon(type);
        if (definition != null)
        {
            return definition.GetReward(level);
        }

        return type == PrototypeDungeonType.Credits
            ? 100 * level
            : type == PrototypeDungeonType.Experience ? 80 * level : 5 * level;
    }

    public static void ResetRuntime()
    {
        Mode = PrototypeGameMode.Idle;
        DungeonType = PrototypeDungeonType.Credits;
        DungeonLevel = 1;
        IdleFarmMode = false;
        launchBattle = false;
        openSquadSetup = false;
        openCharacterScreen = false;
        squadNames = null;
        squadRows = null;
    }

    public static void MigrateCharacterName(string oldName, string newName)
    {
        var json = PlayerPrefs.GetString(SquadKey, string.Empty);
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var data = JsonUtility.FromJson<PrototypeSquadSave>(json);
                if (data != null && data.names != null)
                {
                    for (var index = 0; index < data.names.Count; index++)
                    {
                        if (data.names[index] == oldName)
                        {
                            data.names[index] = newName;
                        }
                    }
                    PlayerPrefs.SetString(SquadKey, JsonUtility.ToJson(data));
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"Saved squad name migration was skipped: {exception.Message}");
            }
        }

        if (squadNames != null)
        {
            for (var index = 0; index < squadNames.Length; index++)
            {
                if (squadNames[index] == oldName)
                {
                    squadNames[index] = newName;
                }
            }
        }
        PlayerPrefs.Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        ResetRuntime();
    }

    private static int GetUnixTime()
    {
        return (int)System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private static int GetUnclaimedIdleSeconds()
    {
        var now = GetUnixTime();
        return Mathf.Clamp(now - PlayerPrefs.GetInt(LastIdleClaimKey, now), 0, 8 * 60 * 60);
    }

    private static void SaveSquad()
    {
        if (squadNames == null || squadRows == null || squadNames.Length != squadRows.Length)
        {
            return;
        }

        var data = new PrototypeSquadSave();
        data.names.AddRange(squadNames);
        foreach (var row in squadRows)
        {
            data.rows.Add((int)row);
        }

        PlayerPrefs.SetString(SquadKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    private static void LoadSquad()
    {
        var json = PlayerPrefs.GetString(SquadKey, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        try
        {
            var data = JsonUtility.FromJson<PrototypeSquadSave>(json);
            if (data == null || data.names == null || data.rows == null ||
                data.names.Count != data.rows.Count || data.names.Count != 5)
            {
                return;
            }

            squadNames = data.names.ToArray();
            squadRows = new PrototypeFormationRow[data.rows.Count];
            for (var index = 0; index < data.rows.Count; index++)
            {
                squadRows[index] = (PrototypeFormationRow)Mathf.Clamp(data.rows[index], 0, 2);
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"Saved squad was invalid and has been ignored: {exception.Message}");
        }
    }
}

public static class CombatPrototype
{
    private const float SpawnX = 4.2f;
    private const float FormationSpacing = 1.75f;
    private const float EngagementSpacing = 1.5f;
    private static Sprite squareSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        var scene = SceneManager.GetActiveScene();
        if (!IsPrototypeScene(scene.name, scene.path) ||
            Object.FindFirstObjectByType<PrototypeBattle>() != null ||
            Object.FindFirstObjectByType<PrototypeSquadSetup>() != null ||
            Object.FindFirstObjectByType<PrototypeCharacterScreen>() != null ||
            Object.FindFirstObjectByType<PrototypeGachaScreen>() != null)
        {
            return;
        }

        PrototypeSaveSystem.Migrate();
        var allyDefinitions = LoadDefinitions("Combatants/Allies");
        var monsterDefinitions = LoadDefinitions("Combatants/Monsters");
        PrototypeSession.EnsureIdleClock();
        if (allyDefinitions.Length == 0 || monsterDefinitions.Length < 5)
        {
            Debug.LogError("Combat prototype needs five allies and at least five PvE monsters in Resources/Combatants.");
            return;
        }

        var roster = LoadRoster();
        var ownedRoster = PrototypeGacha.GetOwnedRoster(roster);
        var root = new GameObject("Combat Prototype");
        CombatantDefinition[] pendingSquad;
        PrototypeFormationRow[] pendingRows;
        if (PrototypeSession.TryConsumeBattle(ownedRoster, out pendingSquad, out pendingRows))
        {
            StartBattle(root.transform, pendingSquad, pendingRows);
        }
        else if (PrototypeSession.ConsumeSquadSetupRequest())
        {
            CombatantDefinition[] configuredSquad;
            PrototypeFormationRow[] configuredRows;
            if (!PrototypeSession.TryGetSquad(ownedRoster, out configuredSquad, out configuredRows))
            {
                configuredSquad = allyDefinitions;
                configuredRows = GetDefaultRows(allyDefinitions);
            }

            root.AddComponent<PrototypeSquadSetup>().Initialize(
                ownedRoster,
                configuredSquad,
                configuredRows,
                (selected, rows) =>
                {
                    var farmMode = PrototypeSession.IdleChallengePending;
                    PrototypeSession.Configure(
                        PrototypeGameMode.Idle,
                        PrototypeDungeonType.Credits,
                        1,
                        farmMode,
                        selected,
                        rows);
                    StartBattle(root.transform, selected, rows);
                });
        }
        else if (PrototypeSession.ConsumeCharacterScreenRequest())
        {
            root.AddComponent<PrototypeCharacterScreen>().Initialize(ownedRoster);
        }
        else
        {
            CombatantDefinition[] defaultSquad;
            PrototypeFormationRow[] defaultRows;
            if (!PrototypeSession.TryGetSquad(ownedRoster, out defaultSquad, out defaultRows))
            {
                defaultSquad = allyDefinitions;
                defaultRows = GetDefaultRows(defaultSquad);
            }
            var farmMode = PrototypeSession.IdleChallengePending;
            PrototypeSession.Configure(
                PrototypeGameMode.Idle,
                PrototypeDungeonType.Credits,
                1,
                farmMode,
                defaultSquad,
                defaultRows);
            StartBattle(root.transform, defaultSquad, defaultRows);
        }

        var camera = Camera.main;
        if (camera != null)
        {
            camera.backgroundColor = new Color(0.025f, 0.04f, 0.09f);
        }

        Debug.Assert(roster.Length >= 12, "The MVP roster needs at least twelve characters.");
        Debug.Assert(ownedRoster.Length >= 5, "The player needs at least five starter characters.");
        var bossCount = 0;
        for (var stage = 1; stage <= 20; stage++)
        {
            Debug.Assert(PrototypeContentCatalog.GetStage(stage) != null, $"Stage {stage} data is missing.");
            if (PrototypeContentCatalog.IsBossStage(stage))
            {
                bossCount++;
            }
        }
        Debug.Assert(bossCount == 4, "The first twenty stages must contain four bosses.");
        for (var kit = PrototypeSkillKit.Nova; kit <= PrototypeSkillKit.Drake; kit++)
        {
            Debug.Assert(PrototypeSkillCatalog.Get(kit) != null, $"Skill data for {kit} is missing.");
        }
        for (var type = PrototypeDungeonType.Credits; type <= PrototypeDungeonType.Materials; type++)
        {
            Debug.Assert(PrototypeContentCatalog.GetDungeon(type) != null, $"Dungeon data for {type} is missing.");
        }
    }

    internal static bool IsPrototypeScene(string sceneName, string scenePath)
    {
        return sceneName == "SampleScene" ||
            scenePath.StartsWith("Temp/__Backupscenes/", System.StringComparison.OrdinalIgnoreCase) &&
            scenePath.EndsWith(".backup", System.StringComparison.OrdinalIgnoreCase);
    }

    internal static PrototypeFormationRow[] GetDefaultRows(CombatantDefinition[] definitions)
    {
        var rows = new PrototypeFormationRow[definitions.Length];
        for (var index = 0; index < definitions.Length; index++)
        {
            rows[index] = definitions[index].FormationRow;
        }

        return rows;
    }

    private static void StartBattle(
        Transform root,
        CombatantDefinition[] allyDefinitions,
        PrototypeFormationRow[] allyRows)
    {
        if (root.GetComponent<PrototypeBattle>() != null)
        {
            return;
        }

        var battle = root.gameObject.AddComponent<PrototypeBattle>();
        CreateArena(root);
        var stage = PrototypeSession.Mode == PrototypeGameMode.Idle && PrototypeSession.IdleFarmMode
            ? Mathf.Max(1, PrototypeSession.IdleStage - 1)
            : PrototypeSession.IdleStage;
        var encounterSeed = PrototypeSession.Mode == PrototypeGameMode.Dungeon
            ? (int)PrototypeSession.DungeonType * 100 + PrototypeSession.DungeonLevel
            : stage;
        var enemyDefinitions = LoadEnemyDefinitionsForMode(PrototypeSession.Mode, encounterSeed);
        if (enemyDefinitions.Length < 5)
        {
            Debug.LogError($"{PrototypeSession.Mode} needs at least five enemy definitions.");
            Object.Destroy(root.gameObject);
            return;
        }
        var isBoss = PrototypeSession.Mode == PrototypeGameMode.Idle &&
            !PrototypeSession.IdleFarmMode && PrototypeContentCatalog.IsBossStage(stage);
        var dungeonDefinition = PrototypeContentCatalog.GetDungeon(PrototypeSession.DungeonType);
        var enemyMultiplier = PrototypeSession.Mode == PrototypeGameMode.Idle
            ? PrototypeContentCatalog.GetStageMultiplier(stage)
            : PrototypeSession.Mode == PrototypeGameMode.Dungeon
                ? dungeonDefinition != null
                    ? dungeonDefinition.GetEnemyMultiplier(PrototypeSession.DungeonLevel)
                    : 1f + (PrototypeSession.DungeonLevel - 1) * 0.18f
                : 1f;
        var timeLimit = PrototypeSession.Mode == PrototypeGameMode.Dungeon
            ? dungeonDefinition != null ? dungeonDefinition.TimeLimit : 60f
            : PrototypeSession.Mode == PrototypeGameMode.PvP ? 90f : 0f;
        var allies = CreateTeam(root, battle, PrototypeTeam.Allies, -SpawnX, allyDefinitions, allyRows, 1f, false);
        var enemies = CreateTeam(root, battle, PrototypeTeam.Enemies, SpawnX, enemyDefinitions, null, enemyMultiplier, isBoss);
        battle.Initialize(
            allies,
            enemies,
            PrototypeSession.Mode,
            stage,
            isBoss,
            timeLimit,
            PrototypeSession.DungeonType,
            PrototypeSession.DungeonLevel);
        PrototypeBattleFeedback.StartBattleLoop();
        root.gameObject.AddComponent<PrototypeHud>().Initialize(battle);

        Debug.Assert(allies.Length == 5 && enemies.Length == 5, "Battle must start as 5v5.");
        Debug.Assert(battle.FindClosestEnemy(allies[0]) != null, "Each team needs a valid target.");
        Debug.Assert(PrototypeCombatClassRules.GetAttackRange(PrototypeCombatClass.Archer) >
            PrototypeCombatClassRules.GetAttackRange(PrototypeCombatClass.Assassin),
            "Ranged classes must attack from farther away than melee classes.");
        Debug.Assert(!string.IsNullOrEmpty(allies[0].SkillDescription), "Each hero needs skill descriptions.");
        Debug.Assert(PrototypeFireCombat.CalculateDamage(
            20, 5, 1f, PrototypeDamageType.Physical, 0f) == 15, "Physical damage formula is invalid.");
        Debug.Assert(PrototypeFireCombat.CalculateDamage(
            20, 10, 1f, PrototypeDamageType.Physical, 0f, 0.5f) == 15, "Armor pierce formula is invalid.");
    }

    internal static void RestartIdleBattle(GameObject currentRoot, bool farmMode)
    {
        RestartBattle(
            currentRoot,
            PrototypeGameMode.Idle,
            PrototypeDungeonType.Credits,
            1,
            farmMode);
    }

    internal static void RestartActivity(
        GameObject currentRoot,
        PrototypeGameMode mode,
        PrototypeDungeonType dungeonType = PrototypeDungeonType.Credits,
        int dungeonLevel = 1)
    {
        RestartBattle(currentRoot, mode, dungeonType, dungeonLevel, false);
    }

    private static void RestartBattle(
        GameObject currentRoot,
        PrototypeGameMode mode,
        PrototypeDungeonType dungeonType,
        int dungeonLevel,
        bool farmMode)
    {
        var allyDefinitions = LoadDefinitions("Combatants/Allies");
        var roster = LoadRoster();
        var ownedRoster = PrototypeGacha.GetOwnedRoster(roster);

        CombatantDefinition[] squad;
        PrototypeFormationRow[] rows;
        if (!PrototypeSession.TryGetSquad(ownedRoster, out squad, out rows))
        {
            squad = allyDefinitions;
            rows = GetDefaultRows(allyDefinitions);
        }

        PrototypeSession.Configure(
            mode,
            dungeonType,
            dungeonLevel,
            farmMode,
            squad,
            rows);
        currentRoot.SetActive(false);
        Object.Destroy(currentRoot);
        var root = new GameObject("Combat Prototype");
        StartBattle(root.transform, squad, rows);
    }

    private static CombatantDefinition[] LoadDefinitions(string path)
    {
        var definitions = Resources.LoadAll<CombatantDefinition>(path);
        System.Array.Sort(definitions, (left, right) => left.FormationOrder.CompareTo(right.FormationOrder));
        return definitions;
    }

    internal static CombatantDefinition[] LoadRoster()
    {
        var allies = LoadDefinitions("Combatants/Allies");
        var enemies = LoadDefinitions("Combatants/Enemies");
        var reserve = LoadDefinitions("Combatants/Roster");
        var roster = new List<CombatantDefinition>(allies.Length + enemies.Length + reserve.Length);
        roster.AddRange(allies);
        roster.AddRange(enemies);
        roster.AddRange(reserve);
        return roster.ToArray();
    }

    internal static CombatantDefinition[] LoadEnemyDefinitionsForMode(PrototypeGameMode mode, int encounterSeed)
    {
        var pool = LoadDefinitions(mode == PrototypeGameMode.PvP
            ? "Combatants/Enemies"
            : "Combatants/Monsters");
        var teamSize = Mathf.Min(5, pool.Length);
        var team = new CombatantDefinition[teamSize];
        var offset = pool.Length == 0 ? 0 : Mathf.Max(0, encounterSeed - 1) % pool.Length;
        for (var index = 0; index < teamSize; index++)
        {
            team[index] = pool[(offset + index) % pool.Length];
        }
        return team;
    }

    private static PrototypeCombatant[] CreateTeam(
        Transform parent,
        PrototypeBattle battle,
        PrototypeTeam team,
        float x,
        CombatantDefinition[] definitions,
        PrototypeFormationRow[] formationRows,
        float statMultiplier,
        bool bossLeader)
    {
        var units = new PrototypeCombatant[definitions.Length];
        var center = (definitions.Length - 1) * 0.5f;

        for (var index = 0; index < definitions.Length; index++)
        {
            var row = formationRows == null ? definitions[index].FormationRow : formationRows[index];
            var rowOffset = row == PrototypeFormationRow.Front ? 0.8f : row == PrototypeFormationRow.Back ? -0.8f : 0f;
            var towardCenter = team == PrototypeTeam.Allies ? 1f : -1f;
            var position = new Vector3(x + rowOffset * towardCenter, (index - center) * FormationSpacing);
            var gameObject = new GameObject(definitions[index].DisplayName);
            gameObject.transform.SetParent(parent);
            gameObject.transform.position = position;

            units[index] = gameObject.AddComponent<PrototypeCombatant>();
            units[index].Initialize(
                definitions[index],
                row,
                team,
                battle,
                SquareSprite,
                statMultiplier,
                bossLeader && index == 0);
            units[index].SetEngagementOffset((index - center) * EngagementSpacing);
        }

        return units;
    }

    private static void CreateArena(Transform parent)
    {
        CreateRectangle(parent, "Deep Space", Vector3.zero, new Vector3(11f, 22f), new Color(0.018f, 0.028f, 0.075f), -30);
        CreateRectangle(parent, "Nebula Left", new Vector3(-3.5f, 2.2f), new Vector3(3.4f, 15f), new Color(0.12f, 0.08f, 0.28f, 0.55f), -28);
        CreateRectangle(parent, "Nebula Right", new Vector3(3.8f, -1.8f), new Vector3(2.8f, 14f), new Color(0.02f, 0.25f, 0.32f, 0.38f), -27);

        var starfield = new GameObject("Starfield");
        starfield.transform.SetParent(parent);
        var stars = new[]
        {
            new Vector3(-4.4f, 9.2f), new Vector3(-2.8f, 6.8f), new Vector3(0.6f, 8.5f),
            new Vector3(4.1f, 7.4f), new Vector3(-4.6f, 3.5f), new Vector3(3.2f, 4.4f),
            new Vector3(-3.8f, -4.7f), new Vector3(1.3f, -6.4f), new Vector3(4.5f, -8.2f)
        };
        for (var index = 0; index < stars.Length; index++)
        {
            var size = index % 3 == 0 ? 0.11f : 0.06f;
            CreateRectangle(starfield.transform, $"Star {index + 1}", stars[index], new Vector3(size, size), new Color(0.65f, 0.9f, 1f, 0.8f), -25);
        }

        CreateRectangle(parent, "Battle Deck", Vector3.zero, new Vector3(9.4f, 9.2f), new Color(0.035f, 0.075f, 0.14f), -15);
        CreateRectangle(parent, "Battle Lane", Vector3.zero, new Vector3(8.8f, 8.6f), new Color(0.065f, 0.12f, 0.22f), -14);
        CreateRectangle(parent, "Lane Edge Top", new Vector3(0f, 4.25f), new Vector3(8.9f, 0.08f), new Color(0.25f, 0.78f, 0.95f, 0.55f), -12);
        CreateRectangle(parent, "Lane Edge Bottom", new Vector3(0f, -4.25f), new Vector3(8.9f, 0.08f), new Color(0.25f, 0.78f, 0.95f, 0.55f), -12);
    }

    private static void CreateRectangle(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 scale,
        Color color,
        int sortingOrder)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent);
        gameObject.transform.position = position;
        gameObject.transform.localScale = scale;

        var renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = SquareSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }

    private static Sprite SquareSprite
    {
        get
        {
            if (squareSprite == null)
            {
                squareSprite = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f);
                squareSprite.name = "Prototype Square";
            }

            return squareSprite;
        }
    }
}

internal sealed class PrototypeSquadSetup : MonoBehaviour
{
    private CombatantDefinition[] roster;
    private readonly List<CombatantDefinition> selected = new List<CombatantDefinition>(5);
    private readonly List<PrototypeFormationRow> rows = new List<PrototypeFormationRow>(5);
    private System.Action<CombatantDefinition[], PrototypeFormationRow[]> startBattle;
    private System.Action close;
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private bool started;

    public void Initialize(
        CombatantDefinition[] availableRoster,
        CombatantDefinition[] defaultSquad,
        PrototypeFormationRow[] defaultRows,
        System.Action<CombatantDefinition[], PrototypeFormationRow[]> onStartBattle,
        System.Action closeAction = null)
    {
        roster = availableRoster;
        startBattle = onStartBattle;
        close = closeAction;
        for (var index = 0; index < defaultSquad.Length && selected.Count < 5; index++)
        {
            selected.Add(defaultSquad[index]);
            rows.Add(defaultRows[index]);
        }
    }

    private void OnGUI()
    {
        if (started || roster == null)
        {
            return;
        }

        CreateStyles();
        var previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 360f, Screen.height / 640f, 1f));

        GUI.Box(new Rect(0f, 0f, 360f, 640f), string.Empty);
        GUI.Label(new Rect(20f, 16f, 320f, 32f), "BUILD YOUR 5-HERO SQUAD", titleStyle);
        GUI.Label(new Rect(20f, 48f, 320f, 22f), "Tap a hero to add/remove. Tap its row to reposition.", labelStyle);
        GUI.Label(new Rect(120f, 70f, 120f, 20f), $"SELECTED {selected.Count}/5", labelStyle);

        for (var index = 0; index < roster.Length; index++)
        {
            var definition = roster[index];
            var selectedIndex = selected.IndexOf(definition);
            var column = index % 2;
            var row = index / 2;
            var text = selectedIndex >= 0 ? "[X] " : "[ ] ";
            text += $"{definition.DisplayName} · {definition.Rarity}\n{definition.Species} · {definition.CombatClass}";
            if (GUI.Button(new Rect(12f + column * 172f, 94f + row * 42f, 164f, 36f), text))
            {
                Toggle(definition);
            }
        }

        GUI.Box(new Rect(12f, 310f, 336f, 230f), "FORMATION", labelStyle);
        for (var index = 0; index < selected.Count; index++)
        {
            var definition = selected[index];
            GUI.Label(
                new Rect(22f, 340f + index * 38f, 196f, 32f),
                $"{index + 1}. {definition.DisplayName} · {definition.CombatClass}",
                labelStyle);
            if (GUI.Button(new Rect(226f, 340f + index * 38f, 112f, 32f), rows[index].ToString()))
            {
                rows[index] = (PrototypeFormationRow)(((int)rows[index] + 1) % 3);
            }
        }

        var previousEnabled = GUI.enabled;
        GUI.enabled = selected.Count == 5;
        if (GUI.Button(new Rect(130f, 562f, 210f, 48f), "APPLY FORMATION"))
        {
            started = true;
            startBattle(selected.ToArray(), rows.ToArray());
            Destroy(this);
        }
        GUI.enabled = previousEnabled;
        if (GUI.Button(new Rect(20f, 562f, 100f, 48f), "BACK"))
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

    private void Toggle(CombatantDefinition definition)
    {
        var index = selected.IndexOf(definition);
        if (index >= 0)
        {
            selected.RemoveAt(index);
            rows.RemoveAt(index);
        }
        else if (selected.Count < 5)
        {
            selected.Add(definition);
            rows.Add(definition.FormationRow);
        }

        Debug.Assert(selected.Count == rows.Count && selected.Count <= 5, "Squad selection is invalid.");
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
            fontSize = 20,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = new Color(0.42f, 0.9f, 1f);

        labelStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11
        };
        labelStyle.normal.textColor = Color.white;
    }
}

internal sealed class PrototypeBattle : MonoBehaviour
{
    public PrototypeCombatant[] Allies { get; private set; }
    public PrototypeCombatant[] Enemies { get; private set; }
    public bool IsFinished { get; private set; }
    public PrototypeTeam Winner { get; private set; }
    public PrototypeGameMode Mode { get; private set; }
    public PrototypeDungeonType DungeonType { get; private set; }
    public int DungeonLevel { get; private set; }
    public int Stage { get; private set; }
    public bool IsBossStage { get; private set; }
    public bool IsIdleFarmMode { get; private set; }
    public int FarmWave { get; private set; } = 1;
    public float TimeLimit { get; private set; }
    public float RemainingTime => TimeLimit <= 0f ? 0f : Mathf.Max(0f, TimeLimit - ElapsedTime);
    public float ElapsedTime { get; private set; }
    public string Announcement { get; private set; }
    public bool HasAnnouncement => announcementTime > 0f;

    private float announcementTime;

    public void Initialize(
        PrototypeCombatant[] allies,
        PrototypeCombatant[] enemies,
        PrototypeGameMode mode,
        int stage,
        bool isBossStage,
        float timeLimit,
        PrototypeDungeonType dungeonType,
        int dungeonLevel)
    {
        Allies = allies;
        Enemies = enemies;
        Mode = mode;
        Stage = stage;
        IsBossStage = isBossStage;
        IsIdleFarmMode = mode == PrototypeGameMode.Idle && PrototypeSession.IdleFarmMode;
        TimeLimit = timeLimit;
        DungeonType = dungeonType;
        DungeonLevel = dungeonLevel;
        foreach (var combatant in allies)
        {
            combatant.OnBattleStarted();
        }
        foreach (var combatant in enemies)
        {
            combatant.OnBattleStarted();
        }
    }

    public PrototypeCombatant FindClosestEnemy(PrototypeCombatant source)
    {
        return FindClosestEnemy(source, null);
    }

    public PrototypeCombatant FindTarget(PrototypeCombatant source)
    {
        var preferredRow = PreferredTargetRow(source.CombatClass);
        return preferredRow.HasValue
            ? FindClosestEnemy(source, preferredRow) ?? FindClosestEnemy(source)
            : FindClosestEnemy(source);
    }

    internal static PrototypeFormationRow? PreferredTargetRow(PrototypeCombatClass combatClass)
    {
        switch (combatClass)
        {
            case PrototypeCombatClass.Tanker:
            case PrototypeCombatClass.Fighter:
                return PrototypeFormationRow.Front;
            case PrototypeCombatClass.Assassin:
                return PrototypeFormationRow.Back;
            default:
                return null;
        }
    }

    private PrototypeCombatant FindClosestEnemy(PrototypeCombatant source, PrototypeFormationRow? row)
    {
        var candidates = GetOpponents(source.Team);
        PrototypeCombatant closest = null;
        var closestDistance = float.MaxValue;

        foreach (var candidate in candidates)
        {
            if (!candidate.IsAlive || (row.HasValue && candidate.FormationRow != row.Value))
            {
                continue;
            }

            var distance = (candidate.transform.position - source.transform.position).sqrMagnitude;
            if (distance < closestDistance)
            {
                closest = candidate;
                closestDistance = distance;
            }
        }

        return closest;
    }

    public PrototypeCombatant[] GetOpponents(PrototypeTeam team)
    {
        return team == PrototypeTeam.Allies ? Enemies : Allies;
    }

    public PrototypeCombatant[] GetTeam(PrototypeTeam team)
    {
        return team == PrototypeTeam.Allies ? Allies : Enemies;
    }

    public PrototypeCombatant FindLowestHealth(PrototypeCombatant[] units)
    {
        PrototypeCombatant lowest = null;
        var lowestRatio = float.MaxValue;

        foreach (var unit in units)
        {
            if (!unit.IsAlive)
            {
                continue;
            }

            var ratio = (float)unit.CurrentHealth / unit.MaxHealth;
            if (ratio < lowestRatio)
            {
                lowest = unit;
                lowestRatio = ratio;
            }
        }

        return lowest;
    }

    public int LivingCount(PrototypeTeam team)
    {
        var units = team == PrototypeTeam.Allies ? Allies : Enemies;
        var count = 0;
        foreach (var unit in units)
        {
            if (unit.IsAlive)
            {
                count++;
            }
        }

        return count;
    }

    public void Announce(string message)
    {
        Announcement = message;
        announcementTime = 1.2f;
    }

    private void Update()
    {
        announcementTime = Mathf.Max(0f, announcementTime - Time.deltaTime);
        if (Allies == null || Enemies == null || IsFinished)
        {
            return;
        }

        ElapsedTime += Time.deltaTime;
        if (IsIdleFarmMode)
        {
            if (LivingCount(PrototypeTeam.Enemies) == 0)
            {
                CompleteFarmWave();
            }
            return;
        }

        if (LivingCount(PrototypeTeam.Allies) == 0)
        {
            Finish(PrototypeTeam.Enemies);
        }
        else if (LivingCount(PrototypeTeam.Enemies) == 0)
        {
            Finish(PrototypeTeam.Allies);
        }
        else if (TimeLimit > 0f && ElapsedTime >= TimeLimit)
        {
            Finish(Mode == PrototypeGameMode.Dungeon
                ? PrototypeTeam.Enemies
                : GetHealthRatio(Allies) >= GetHealthRatio(Enemies)
                    ? PrototypeTeam.Allies
                    : PrototypeTeam.Enemies);
        }
    }

    private void CompleteFarmWave()
    {
        var clearedWave = FarmWave++;
        var goldReward = PrototypeContentCatalog.GetStageGold(Stage);
        var experienceReward = PrototypeContentCatalog.GetStageExperience(Stage);
        var ticketReward = clearedWave % 10 == 0 ? 1 : 0;
        PrototypeSession.AddIdleRewards(goldReward, experienceReward);
        if (ticketReward > 0)
        {
            PrototypeGacha.AddTickets(ticketReward);
        }
        foreach (var ally in Allies)
        {
            ally.ResetForFarmWave();
        }
        foreach (var enemy in Enemies)
        {
            enemy.ResetForFarmWave();
        }

        Announce(
            $"WAVE {clearedWave} · +{goldReward} GOLD · +{experienceReward} XP" +
            (ticketReward > 0 ? " · +1 TICKET" : string.Empty));
    }

    private void Finish(PrototypeTeam winner)
    {
        Winner = winner;
        IsFinished = true;
    }

    private static float GetHealthRatio(PrototypeCombatant[] units)
    {
        var current = 0;
        var maximum = 0;
        foreach (var unit in units)
        {
            current += unit.CurrentHealth;
            maximum += unit.MaxHealth;
        }

        return maximum == 0 ? 0f : (float)current / maximum;
    }
}

internal static class PrototypeStatusIconArt
{
    private const int Size = 12;
    private const int GlyphSize = 8;
    private const float PixelsPerUnit = 32f;
    private static Sprite[] cache;

    public static Sprite[] Get()
    {
        if (cache != null && cache[0] != null)
        {
            return cache;
        }

        cache = new[]
        {
            Create("Shield", new Color32(80, 220, 255, 255), "........", "..####..", ".######.", ".######.", ".######.", "..####..", "...##...", "........"),
            Create("Stun", new Color32(255, 225, 70, 255), "...##...", "..##....", "..###...", ".#####..", "...##...", "..##....", "..#.....", "........"),
            Create("Burn", new Color32(255, 100, 30, 255), "...#....", "..##....", ".####...", ".#####..", "######..", ".####...", "..##....", "........"),
            Create("Poison", new Color32(120, 255, 80, 255), "..#..#..", "...##...", "..####..", ".######.", ".##..##.", ".######.", "..####..", "........"),
            Create("Slow", new Color32(110, 190, 255, 255), "...##...", ".#.##.#.", "..####..", "########", "########", "..####..", ".#.##.#.", "...##..."),
            Create("Haste", new Color32(80, 255, 210, 255), ".##..##.", "..##..##", "...##..#", "..##..##", ".##..##.", "..##..##", "...##..#", "........"),
            Create("Attack Up", new Color32(120, 255, 140, 255), "...#..#.", "..##.###", ".###..#.", "..##....", "..##....", ".####...", "..##....", "........"),
            Create("Attack Down", new Color32(255, 90, 90, 255), "..##....", ".####...", "..##....", "..##....", ".###....", "..##....", "....###.", "........"),
            Create("Taunt", new Color32(255, 75, 130, 255), "..####..", "..####..", "..####..", "...##...", "...##...", "........", "...##...", "........"),
            Create("Ash", new Color32(255, 105, 30, 255), "...##...", "..####..", ".##..##.", "##.##.##", ".######.", "..####..", "...##...", "........"),
            Create("Heat", new Color32(255, 55, 20, 255), "..#..#..", ".##.##..", "..###...", ".#####..", "#######.", ".#####..", "..###...", "........"),
            Create("Grounded", new Color32(210, 145, 70, 255), "........", "..####..", ".######.", "########", "..#..#..", ".##..##.", "##....##", "........")
        };
        return cache;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache()
    {
        cache = null;
    }

    private static Sprite Create(string name, Color32 color, params string[] rows)
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = name + " Status Icon"
        };
        var pixels = new Color32[Size * Size];
        var background = new Color32(5, 10, 20, 230);
        for (var index = 0; index < pixels.Length; index++)
        {
            pixels[index] = background;
        }
        for (var y = 0; y < GlyphSize; y++)
        {
            for (var x = 0; x < GlyphSize; x++)
            {
                if (rows[GlyphSize - 1 - y][x] == '#')
                {
                    pixels[(y + 2) * Size + x + 2] = color;
                }
            }
        }
        pixels[0] = pixels[Size - 1] = pixels[(Size - 1) * Size] = pixels[Size * Size - 1] = default;
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        sprite.name = name + " Status Icon";
        return sprite;
    }
}

internal sealed class PrototypeCombatant : MonoBehaviour
{
    private struct AshState
    {
        public int stacks;
        public float remaining;
    }

    private const float MaxEnergy = 100f;
    private const float HealthBarWidth = 1.45f;
    private const float BodySize = 1.05f * 2.2f;
    private const float BasicActionDuration = 0.36f;
    private const float BasicHitDelay = 0.18f;
    private const float SkillActionDuration = 0.52f;
    private const float SkillHitDelay = 0.27f;
    private const float UltimateActionDuration = 0.72f;
    private const float UltimateHitDelay = 0.38f;
    private const float ArenaHalfExtent = 4.2f;

    private PrototypeBattle battle;
    private CombatantDefinition definition;
    private SkillDefinition skills;
    private Sprite prototypeSprite;
    private PrototypeSpriteSet characterSprites;
    private SpriteRenderer body;
    private SpriteRenderer healthFill;
    private SpriteRenderer energyFill;
    private SpriteRenderer shieldVisual;
    private SpriteRenderer markVisual;
    private SpriteRenderer burnVisual;
    private SpriteRenderer heatAura;
    private SpriteRenderer ultimateReadyVisual;
    private SpriteRenderer bossPhaseAura;
    private SpriteRenderer bossOrbit;
    private PrototypeFireVfx heatVfx;
    private PrototypeFireVfx flameShieldVfx;
    private PrototypeWaterVfx dragonAuraVfx;
    private PrototypeWaterVfx aureliaShieldVfx;
    private PrototypeVoidVfx voidMassVfx;
    private PrototypeVoidVfx voidParasiteVfx;
    private PrototypeVoidVfx voidAscensionVfx;
    private PrototypeVoidVfx voidCollapseVfx;
    private SpriteRenderer[] statusIcons;
    private TextMesh[] statusCounts;
    private SortingGroup sortingGroup;
    private Color bodyColor;
    private float bodySize = BodySize;
    private float attackCooldown;
    private float attackPulse;
    private float skillPulse;
    private float hitFlash;
    private float animationTime;
    private float afterimageCooldown;
    private float actionHitRemaining;
    private float energy;
    private int currentShield;
    private float shieldRemaining;
    private float hasteRemaining;
    private float slowRemaining;
    private float movementSlowPercent;
    private float movementSlowRemaining;
    private float decayingSlowStart;
    private float decayingSlowDuration;
    private float decayingSlowElapsed;
    private float groundedRemaining;
    private float airborneRemaining;
    private float airborneDuration;
    private float knockbackRemaining;
    private float knockbackDuration;
    private Vector3 knockbackStart;
    private Vector3 knockbackEnd;
    private float tauntRemaining;
    private float stunRemaining;
    private float burnRemaining;
    private float burnTickCooldown;
    private float burnTickInterval = 1f;
    private int burnTickDamage;
    private PrototypeDamageType burnDamageType = PrototypeDamageType.Fire;
    private float fireDotMarkerRemaining;
    private float poisonRemaining;
    private float poisonTickCooldown;
    private int poisonDamage;
    private float attackBuffRemaining;
    private float attackDebuffRemaining;
    private float activeSkillCooldown;
    private float abilityPowerMultiplier = 1f;
    private float guardLinkRemaining;
    private float flameShieldCooldown;
    private float waterStrideRemaining;
    private float dragonPressureCooldown;
    private float dragonPressureWindowRemaining;
    private int dragonPressureDamage;
    private float aureliaAegisRemaining;
    private float aureliaControlImmunityRemaining;
    private float voidPressureRemaining;
    private float voidControlImmunityRemaining;
    private float voidTauntReductionRemaining;
    private float voidAscensionRemaining;
    private float voidDecayTickRemaining;
    private float voidRebirthCooldown;
    private float voidCollapseRemaining;
    private float voidCollapseTickRemaining;
    private float voidParasiteRemaining;
    private float voidResonanceDamage;
    private float baseAttackRange;
    private float engagementOffset;
    private int currentHealth;
    private int baseMaxHealth;
    private int voidCollapseHealth;
    private int voidResonanceStacks;
    private int basicSkillLevel = 1;
    private int passiveSkillLevel = 1;
    private int activeSkillLevel = 1;
    private int ultimateSkillLevel = 1;
    private int basicAttackCount;
    private int incomingHitCount;
    private bool lastBastionTriggered;
    private bool furyTriggered;
    private bool isMoving;
    private bool isUltimateAction;
    private bool bossPhaseTriggered;
    private bool deathVisualsHidden;
    private PrototypeCombatant forcedTarget;
    private PrototypeCombatant guardLinkTarget;
    private PrototypeCombatant burnSource;
    private PrototypeCombatant poisonSource;
    private PrototypeCombatant aureliaAegisSource;
    private PrototypeCombatant voidParasiteSource;
    private readonly Dictionary<PrototypeCombatant, AshState> ashBySource =
        new Dictionary<PrototypeCombatant, AshState>();
    private readonly List<PrototypeCombatant> ashSources = new List<PrototypeCombatant>();
    private int heatStacks;
    private System.Action pendingAction;
    private PrototypeAnimationState animationState;
    private Vector3 spawnPosition;

    public string DisplayName { get; private set; }
    public PrototypeTeam Team { get; private set; }
    public PrototypeSkillKit SkillKit { get; private set; }
    public PrototypeSpecies Species { get; private set; }
    public PrototypeCombatClass CombatClass { get; private set; }
    public PrototypeRarity Rarity { get; private set; }
    public PrototypeFormationRow FormationRow { get; private set; }
    public int MaxHealth { get; private set; }
    public int Attack { get; private set; }
    public int Defense { get; private set; }
    public float MoveSpeed { get; private set; }
    public float AttackRange { get; private set; }
    public float AttackInterval { get; private set; }
    public int CurrentHealth => currentHealth;
    public int CurrentShield => currentShield;
    internal bool HasBurn => burnRemaining > 0f || fireDotMarkerRemaining > 0f;
    internal bool HasShield => currentShield > 0;
    public int HeatStacks => heatStacks;
    public float FireDamageMultiplier => 1f + heatStacks * 0.05f;
    public float MovementSpeedMultiplier =>
        (1f + heatStacks * 0.04f) *
        (waterStrideRemaining > 0f ? 1.15f : 1f) *
        (HasAureliaAegis ? 1.2f : 1f);
    public float FireResistance
    {
        get
        {
            var stacks = 0;
            foreach (var state in ashBySource.Values)
            {
                stacks += state.stacks;
            }
            return stacks * -0.05f;
        }
    }
    public float Energy => energy;
    public float ActiveSkillCooldown => activeSkillCooldown;
    public int DamageDealt { get; private set; }
    public int Level { get; private set; } = 1;
    public int Stars { get; private set; } = 1;
    public int BasicSkillLevel => basicSkillLevel;
    public int PassiveSkillLevel => passiveSkillLevel;
    public int ActiveSkillLevel => activeSkillLevel;
    public int UltimateSkillLevel => ultimateSkillLevel;
    public bool IsBoss { get; private set; }
    public bool IsAlive => currentHealth > 0;
    public PrototypeCombatant Target { get; private set; }
    public string StatusSummary
    {
        get
        {
            var status = currentShield > 0 ? "Shield " : string.Empty;
            if (stunRemaining > 0f || airborneRemaining > 0f) status += "Stun ";
            if (TotalAshStacks > 0) status += $"Ash {TotalAshStacks} ";
            if (burnRemaining > 0f) status += "Burn ";
            if (poisonRemaining > 0f) status += "Poison ";
            if (slowRemaining > 0f || CurrentMovementSlow > 0f) status += "Slow ";
            if (groundedRemaining > 0f) status += "Grounded ";
            if (heatStacks > 0) status += $"Heat {heatStacks} ";
            if (hasteRemaining > 0f) status += "Haste ";
            if (attackBuffRemaining > 0f) status += "ATK+ ";
            if (attackDebuffRemaining > 0f) status += "ATK- ";
            if (tauntRemaining > 0f) status += "Taunt ";
            return status.Length == 0 ? "Normal" : status.TrimEnd();
        }
    }
    public string SkillDescription
    {
        get
        {
            if (skills != null)
            {
                return skills.Summary;
            }

            switch (SkillKit)
            {
                case PrototypeSkillKit.Nova:
                    return "Basic: Fire attacks build Ash and detonate at three stacks.\nPassives: Ash lowers Fire Resistance, Heat empowers Fire damage, and Fire DoT kills spread embers.\nActive: Hellfire Impact creates a slowing Magma Pool.\nUltimate: Crimson Gale consumes Ash and turns Magma into Firestorm.";
                case PrototypeSkillKit.Ion:
                    return "Basic: every 4th hit chains lightning.\nPassive: every 5th incoming hit is phased and grants energy.\nActive: Static Field damages, stuns and chains.\nUltimate: Volt Rush strikes three times and retargets after kills.";
                case PrototypeSkillKit.Astra:
                    return "Basic: each water-serpent attack creates two Hydro Beads that restore energy or empower Aurelia.\nPassives: support effects cleanse, nearby allies gain resistance, and burst damage triggers Dragon Pressure.\nActive: Dragon Realm heals and protects allies while absorbing Hydro Beads.\nUltimate: Draconian Aegis shields and empowers the whole team.";
                case PrototypeSkillKit.Krag:
                    return "Basic: attacks from the front row.\nPassive: +50% defense above 50% HP; Last Bastion shield below 35%.\nActive: Crushing Orbit damages, weakens attack and shields Krag.\nUltimate: Gravity Bulwark shields Krag and taunts the enemy team.";
                case PrototypeSkillKit.Vex:
                    return "Basic: prioritizes the back row and deals +30% damage below 50% target HP.\nPassive: kills grant haste and 50 energy.\nActive: Void Step bursts a back-row target.\nUltimate: Crimson Execute deals massive damage to wounded enemies.";
                case PrototypeSkillKit.Rook:
                    return "Basic: every 3rd shot ignores 50% defense.\nPassive: fights safely from the back row.\nActive: Pinning Shot pierces armor and slows.\nUltimate: Rail Barrage damages and slows every enemy.";
                case PrototypeSkillKit.Lyra:
                    return "Basic: every 4th shot hits a second target.\nPassive: long-range back-row positioning.\nActive: Sunpiercer deals damage and burns.\nUltimate: Helios Rain damages and burns the full enemy team.";
                case PrototypeSkillKit.Brakk:
                    return "Basic: Void Claw infects enemies and releases a slowing shockwave at ten Resonance.\nPassives: damage hardens Kronos, parasites steal shield energy, and fatal damage triggers Black Hole Collapse.\nActive: Singularity Pull drags and taunts nearby enemies.\nUltimate: Cosmic Leviathan transforms Kronos into a larger cleaving drain-tank with a decay aura.";
                case PrototypeSkillKit.Hex:
                    return "Basic: every 3rd attack splashes to a second target.\nPassive: poison deals damage over time.\nActive: Corruption Code poisons and reduces attack.\nUltimate: Singularity Mine deals heavy damage, poison and slow.";
                case PrototypeSkillKit.Nyx:
                    return "Basic: every 4th attack drains enemy energy.\nPassive: supports allies from the back row.\nActive: Abyssal Hymn heals, restores energy and cleanses one ally.\nUltimate: Night Bloom heals, cleanses, hastes and empowers the team.";
                case PrototypeSkillKit.Mira:
                    return "Basic: every 3rd attack slows the target and shields the weakest ally.\nPassive: gains energy when protecting wounded allies.\nActive: Undertow damages and slows enemies while healing an ally.\nUltimate: Leviathan's Grace heals and shields the team, then slows enemies.";
                case PrototypeSkillKit.Drake:
                    return "Basic: every 3rd strike burns the target.\nPassive: enters Dragon Fury below 50% HP.\nActive: Dragon Dive burns a target and shields Drake.\nUltimate: Cataclysm Roar damages and weakens every enemy.";
                default:
                    return "Basic attack only.";
            }
        }
    }

    public void Initialize(
        CombatantDefinition definition,
        PrototypeFormationRow formationRow,
        PrototypeTeam team,
        PrototypeBattle battleManager,
        Sprite sprite,
        float statMultiplier,
        bool isBoss)
    {
        this.definition = definition;
        skills = definition.Skills;
        IsBoss = isBoss;
        DisplayName = isBoss ? $"BOSS {definition.DisplayName}" : definition.DisplayName;
        Team = team;
        SkillKit = definition.SkillKit;
        Species = definition.Species;
        CombatClass = definition.CombatClass;
        Rarity = definition.Rarity;
        FormationRow = formationRow;
        var progress = team == PrototypeTeam.Allies
            ? PrototypeProgression.Get(definition.DisplayName)
            : null;
        if (progress != null)
        {
            Level = progress.level;
            Stars = progress.stars;
            basicSkillLevel = progress.basicSkillLevel;
            passiveSkillLevel = progress.passiveSkillLevel;
            activeSkillLevel = progress.activeSkillLevel;
            ultimateSkillLevel = progress.ultimateSkillLevel;
        }

        var healthProgression = progress == null ? 1f : PrototypeProgression.HealthMultiplier(progress);
        var attackProgression = progress == null ? 1f : PrototypeProgression.AttackMultiplier(progress);
        var defenseProgression = progress == null ? 1f : PrototypeProgression.DefenseMultiplier(progress);
        MaxHealth = Mathf.RoundToInt(
            definition.MaxHealth * statMultiplier * (isBoss ? 2.5f : 1f) * healthProgression);
        Attack = Mathf.RoundToInt(
            definition.Attack * statMultiplier * (isBoss ? 1.35f : 1f) * attackProgression);
        Defense = Mathf.RoundToInt(
            definition.Defense * statMultiplier * (isBoss ? 1.2f : 1f) * defenseProgression);
        MoveSpeed = definition.MoveSpeed;
        AttackRange = definition.AttackRange;
        baseAttackRange = AttackRange;
        AttackInterval = definition.AttackInterval;
        currentHealth = MaxHealth;
        baseMaxHealth = MaxHealth;
        attackCooldown = AttackInterval * 0.5f;
        activeSkillCooldown = ActiveSkillInterval * 0.5f;
        bodyColor = definition.BodyColor;
        battle = battleManager;
        prototypeSprite = sprite;
        characterSprites = PrototypePixelArt.Create(definition, isBoss);
        bodySize = isBoss ? BodySize * 1.35f : BodySize;
        spawnPosition = transform.position;

        sortingGroup = gameObject.AddComponent<SortingGroup>();
        UpdateSortingOrder();
        var shadow = CreateSprite(
            "Body Shadow",
            characterSprites.Idle[0],
            new Color(0f, 0f, 0f, 0.42f),
            new Vector3(bodySize * 0.72f, bodySize * 0.18f, 1f),
            -4);
        shadow.transform.localPosition = new Vector3(0f, -bodySize * 0.38f);

        body = CreateSprite("Body", characterSprites.Idle[0], Color.white, new Vector3(bodySize, bodySize), 2);
        body.flipX = ShouldFlipSprite(Team == PrototypeTeam.Enemies, definition.SourceFacesLeft);
        if (IsBoss)
        {
            CreateBossVisuals();
        }
        CreateHealthBar(sprite);
        CreateSkillVisuals(sprite);
        CreateStatusIcons();
        if (SkillKit == PrototypeSkillKit.Astra)
        {
            dragonAuraVfx = PrototypeWaterVfx.SpawnAttached(
                "AureliaDragonAura", transform, 0.11f, 2.8f, true, 1);
        }

        Debug.Assert(characterSprites.Attack.Length >= 4 && characterSprites.Skill.Length >= 4 &&
            characterSprites.Ultimate.Length >= 4,
            "Combat actions need enough frames to expose a clear hit frame.");
        Debug.Assert(BasicHitDelay < BasicActionDuration && SkillHitDelay < SkillActionDuration &&
            UltimateHitDelay < UltimateActionDuration, "Action hit frames must occur before actions finish.");
    }

    internal void OnBattleStarted()
    {
        if (SkillKit != PrototypeSkillKit.Brakk || !IsAlive)
        {
            return;
        }

        voidControlImmunityRemaining = 3f;
        CleanseNegativeStatuses();
        battle.Announce("KRONOS  ·  EVENT HORIZON");
        PrototypeVoidVfx.Spawn("KronosDimensionalTear", transform.position, 0.055f, 1.8f, false, 116);
        PrototypeVoidVfx.Spawn("KronosVoidField", transform.position, 0.08f, 2.2f, false, 42);
        if (PrototypeGameFlow.HasInstance)
        {
            PrototypeGameFlow.Instance.ShowImpact(new Color(0.18f, 0.14f, 0.22f));
        }
        foreach (var enemy in battle.GetOpponents(Team))
        {
            if (!enemy.IsAlive)
            {
                continue;
            }
            enemy.voidPressureRemaining = Mathf.Max(enemy.voidPressureRemaining, 5f);
            enemy.ApplyMovementSlow(0.15f, 5f);
        }
    }

    internal void SetEngagementOffset(float offset)
    {
        engagementOffset = offset;
    }

    internal void ResetForFarmWave()
    {
        StopAllCoroutines();
        transform.position = spawnPosition;
        currentHealth = MaxHealth;
        energy = 0f;
        attackCooldown = AttackInterval * 0.5f;
        activeSkillCooldown = ActiveSkillInterval * 0.5f;
        attackPulse = 0f;
        skillPulse = 0f;
        hitFlash = 0f;
        actionHitRemaining = 0f;
        pendingAction = null;
        isUltimateAction = false;
        animationState = PrototypeAnimationState.Idle;
        animationTime = 0f;
        deathVisualsHidden = false;
        SetCombatVisualsVisible(true);
        hasteRemaining = 0f;
        slowRemaining = 0f;
        movementSlowPercent = 0f;
        movementSlowRemaining = 0f;
        decayingSlowStart = 0f;
        decayingSlowDuration = 0f;
        decayingSlowElapsed = 0f;
        groundedRemaining = 0f;
        airborneRemaining = 0f;
        airborneDuration = 0f;
        knockbackRemaining = 0f;
        tauntRemaining = 0f;
        stunRemaining = 0f;
        attackBuffRemaining = 0f;
        attackDebuffRemaining = 0f;
        ashBySource.Clear();
        heatStacks = 0;
        flameShieldCooldown = 0f;
        waterStrideRemaining = 0f;
        dragonPressureCooldown = 0f;
        dragonPressureWindowRemaining = 0f;
        dragonPressureDamage = 0;
        voidPressureRemaining = 0f;
        voidControlImmunityRemaining = 0f;
        voidTauntReductionRemaining = 0f;
        DisableVoidAscension();
        voidDecayTickRemaining = 0f;
        voidRebirthCooldown = 0f;
        voidCollapseRemaining = 0f;
        voidCollapseTickRemaining = 0f;
        voidCollapseHealth = 0;
        voidResonanceStacks = 0;
        voidResonanceDamage = 0f;
        ClearVoidParasite();
        if (voidMassVfx != null)
        {
            Destroy(voidMassVfx.gameObject);
            voidMassVfx = null;
        }
        if (voidCollapseVfx != null)
        {
            Destroy(voidCollapseVfx.gameObject);
            voidCollapseVfx = null;
        }
        DisableAureliaAegis();
        fireDotMarkerRemaining = 0f;
        forcedTarget = null;
        guardLinkTarget = null;
        guardLinkRemaining = 0f;
        Target = null;
        basicAttackCount = 0;
        incomingHitCount = 0;
        lastBastionTriggered = false;
        furyTriggered = false;
        CleanseNegativeStatuses();
        DisableShield();
        PrototypeFireZone.DestroyOwnedBy(this);
        PrototypeHydroBead.DestroyOwnedBy(this);
        PrototypeWaterDomain.DestroyOwnedBy(this);
        markVisual.enabled = false;
        burnVisual.enabled = false;
        if (heatAura != null) heatAura.enabled = false;
        bossPhaseTriggered = false;
        UpdateBossPhaseVisuals();
        body.color = Color.white;
        UpdateHealthBar();
        UpdateEnergyBar();
        UpdateStatusIcons();
    }

    private void Update()
    {
        UpdateStatuses();
        isMoving = false;
        UpdatePendingAction();
        if (!IsAlive || battle.IsFinished)
        {
            pendingAction = null;
            UpdateFeedback();
            return;
        }

        if (voidCollapseRemaining > 0f)
        {
            UpdateFeedback();
            return;
        }

        if (stunRemaining > 0f || airborneRemaining > 0f)
        {
            UpdateFeedback();
            return;
        }

        if (forcedTarget != null && forcedTarget.IsAlive && tauntRemaining > 0f)
        {
            Target = forcedTarget;
        }
        else if (Target == null || !Target.IsAlive)
        {
            Target = battle.FindTarget(this);
        }

        if (Target == null)
        {
            UpdateFeedback();
            return;
        }

        attackCooldown -= Time.deltaTime;
        var targetPosition = Target.transform.position;
        var targetDelta = targetPosition - transform.position;
        var stoppingDistance = CombatStoppingDistance(AttackRange, bodySize, Target.bodySize);
        if (Mathf.Abs(targetDelta.x) > 0.01f)
        {
            body.flipX = ShouldFlipSprite(targetDelta.x < 0f, definition.SourceFacesLeft);
        }

        var attackRangeWithTolerance = stoppingDistance + 0.001f;
        var outsideAttackRange = targetDelta.sqrMagnitude > attackRangeWithTolerance * attackRangeWithTolerance;
        if (outsideAttackRange)
        {
            var engagementPoint = ClampToArena(CalculateEngagementPoint(
                transform.position,
                targetPosition,
                stoppingDistance,
                engagementOffset));
            transform.position = Vector3.MoveTowards(
                transform.position,
                engagementPoint,
                MoveSpeed * MovementSpeedMultiplier * (1f - CurrentMovementSlow) * Time.deltaTime);
            isMoving = true;
            UpdateFeedback();
            return;
        }

        if (pendingAction == null)
        {
            if (SkillKit != PrototypeSkillKit.None && energy >= MaxEnergy)
            {
                StartUltimate();
            }
            else if (SkillKit != PrototypeSkillKit.None && activeSkillCooldown <= 0f)
            {
                StartActiveSkill();
            }
            else if (attackCooldown <= 0f)
            {
                StartBasicAttack();
            }
        }

        UpdateFeedback();
    }

    private void StartBasicAttack()
    {
        attackCooldown = EffectiveAttackInterval;
        if (Target != null && AttackRange >= 3f)
        {
            var hitDelay = ConfiguredHitDelay(skills == null ? null : skills.Basic, BasicHitDelay);
            if (SkillKit == PrototypeSkillKit.Nova)
            {
                PrototypeFireVfx.SpawnTravel(
                    "FireGodFlameProjectile",
                    transform.position + Vector3.up * 0.12f,
                    Target.transform.position,
                    hitDelay,
                    0.75f,
                    112);
            }
            else if (SkillKit == PrototypeSkillKit.Astra)
            {
                PrototypeWaterVfx.SpawnTravel(
                    "AureliaWaterSerpent",
                    transform.position + Vector3.up * 0.12f,
                    Target.transform.position,
                    hitDelay,
                    0.8f,
                    112,
                    0.18f);
            }
            else
            {
                PrototypeProjectile.Spawn(
                    prototypeSprite,
                    transform.position + Vector3.up * 0.12f,
                    Target.transform.position,
                    SkillAccent,
                    hitDelay);
            }
        }
        BeginAction(
            PerformBasicAttack,
            SkillDuration(skills == null ? null : skills.Basic, BasicActionDuration),
            ConfiguredHitDelay(skills == null ? null : skills.Basic, BasicHitDelay),
            false,
            false);
    }

    private void PerformBasicAttack()
    {
        if (!AcquireTarget())
        {
            return;
        }

        abilityPowerMultiplier = BasicSkillMultiplier;
        basicAttackCount++;
        if (SkillKit == PrototypeSkillKit.Nova)
        {
            PerformFireGodBasic();
            abilityPowerMultiplier = 1f;
            return;
        }
        if (SkillKit == PrototypeSkillKit.Brakk)
        {
            PerformKronosBasic();
            abilityPowerMultiplier = 1f;
            return;
        }
        var defenseIgnore = SkillKit == PrototypeSkillKit.Rook && basicAttackCount % 3 == 0 ? 0.5f : 0f;
        DealDamage(Target, SkillPower(skills == null ? null : skills.Basic, 1f), defenseIgnore);
        var basicEnergy = skills != null && skills.Basic != null && skills.Basic.EnergyGain > 0
            ? skills.Basic.EnergyGain
            : 18f;
        GainEnergy(basicEnergy);

        switch (SkillKit)
        {
            case PrototypeSkillKit.Ion when basicAttackCount % 4 == 0:
                TriggerArcChain();
                break;
            case PrototypeSkillKit.Astra:
                SpawnHydroBeads();
                break;
            case PrototypeSkillKit.Lyra when basicAttackCount % 4 == 0:
                HitSecondaryTarget(0.75f);
                break;
            case PrototypeSkillKit.Hex when basicAttackCount % 3 == 0:
                HitSecondaryTarget(0.5f);
                break;
            case PrototypeSkillKit.Nyx when basicAttackCount % 4 == 0:
                Target.DrainEnergy(20f);
                GainEnergy(10f);
                break;
            case PrototypeSkillKit.Mira when basicAttackCount % 3 == 0:
                Target.ApplyAttackSlow(3f);
                var protectedAlly = battle.FindLowestHealth(battle.GetTeam(Team));
                if (protectedAlly != null)
                {
                    protectedAlly.GrantShield(Mathf.RoundToInt(protectedAlly.MaxHealth * 0.1f), 4f);
                    GainEnergy(8f);
                }
                break;
            case PrototypeSkillKit.Drake when basicAttackCount % 3 == 0:
                Target.ApplyBurn(this, Mathf.RoundToInt(EffectiveAttack * 0.18f), 3f);
                break;
        }

        abilityPowerMultiplier = 1f;
    }

    private void PerformKronosBasic()
    {
        var basicPower = SkillPower(skills == null ? null : skills.Basic, 1f);
        var totalDamage = 0;
        if (voidAscensionRemaining > 0f)
        {
            var origin = transform.position;
            var direction = (Target.transform.position - origin).normalized;
            foreach (var enemy in battle.GetOpponents(Team))
            {
                if (!enemy.IsAlive || !PrototypeFireCombat.PointInCone(
                        enemy.transform.position, origin, direction, AttackRange + 1.5f, 52f))
                {
                    continue;
                }
                totalDamage += DealDamage(enemy, basicPower, PrototypeDamageType.Magic).AppliedDamage;
            }
            Heal(Mathf.RoundToInt(totalDamage * 0.3f));
            PrototypeVoidVfx.Spawn("KronosVoidSlash", transform.position, 0.055f, 1.45f, false, 114);
        }
        else
        {
            totalDamage = DealDamage(Target, basicPower, PrototypeDamageType.Magic).AppliedDamage;
        }

        var basicEnergy = skills != null && skills.Basic != null && skills.Basic.EnergyGain > 0
            ? skills.Basic.EnergyGain
            : 18f;
        GainEnergy(basicEnergy);
        if (voidResonanceStacks < 10)
        {
            return;
        }

        voidResonanceStacks = 0;
        voidResonanceDamage = 0f;
        RefreshVoidMassVisual();
        PrototypeVoidVfx.Spawn("KronosResonanceBurst", transform.position, 0.055f, 1.75f, false, 115);
        foreach (var enemy in battle.GetOpponents(Team))
        {
            if (!enemy.IsAlive || !PrototypeFireCombat.PointInCircle(enemy.transform.position, transform.position, 2.6f))
            {
                continue;
            }
            DealFlatDamage(
                enemy,
                Mathf.RoundToInt(MaxHealth * 0.06f * BasicSkillMultiplier),
                PrototypeDamageType.Magic,
                PrototypeDamageFlags.Direct);
            if (enemy.IsAlive)
            {
                enemy.ApplyMovementSlow(0.4f, 2f);
            }
        }
    }

    private void PerformFireGodBasic()
    {
        if (!AcquireTarget())
        {
            return;
        }

        var target = Target;
        var center = target.transform.position;
        var empowered = target.AshStacksFrom(this) >= 3;
        if (empowered)
        {
            target.ConsumeAsh(this);
            PrototypeFireVfx.Spawn("FireGodAshWarning", center, 0.05f, 1.45f, false, 111);
        }

        DealDamage(
            target,
            SkillPower(skills == null ? null : skills.Basic, 1f),
            PrototypeDamageType.Fire);
        PrototypeFireVfx.Spawn("FireGodFlameImpact", center, 0.055f, 0.9f, false, 112);
        var basicEnergy = skills != null && skills.Basic != null && skills.Basic.EnergyGain > 0
            ? skills.Basic.EnergyGain
            : 18f;
        GainEnergy(basicEnergy);
        if (!target.IsAlive)
        {
            return;
        }

        if (!empowered)
        {
            target.ApplyAsh(this, 1);
            return;
        }

        DealFireArea(center, 1.5f, 0.45f, PrototypeDamageFlags.AshDetonation);
        StartCoroutine(DelayedFireArea(
            center, 1.5f, 0.75f, 0.35f, PrototypeDamageFlags.AshDetonation));
    }

    private void DealFireArea(
        Vector3 center,
        float radius,
        float multiplier,
        PrototypeDamageFlags flags)
    {
        foreach (var enemy in battle.GetOpponents(Team))
        {
            if (enemy.IsAlive && PrototypeFireCombat.PointInCircle(enemy.transform.position, center, radius))
            {
                DealDamage(enemy, multiplier, PrototypeDamageType.Fire, flags);
            }
        }
    }

    private IEnumerator DelayedFireArea(
        Vector3 center,
        float radius,
        float multiplier,
        float delay,
        PrototypeDamageFlags flags)
    {
        yield return new WaitForSeconds(delay);
        if (battle != null && !battle.IsFinished)
        {
            DealFireArea(center, radius, multiplier, flags);
            PrototypeFireVfx.Spawn("FireGodAshDetonation", center, 0.055f, 1.25f, false, 113);
        }
    }

    private void StartActiveSkill()
    {
        activeSkillCooldown = ActiveSkillInterval;
        attackCooldown = EffectiveAttackInterval;
        BeginAction(
            PerformActiveSkill,
            SkillDuration(skills == null ? null : skills.Active, SkillActionDuration),
            ConfiguredHitDelay(skills == null ? null : skills.Active, SkillHitDelay),
            true,
            false);
    }

    private void PerformActiveSkill()
    {
        abilityPowerMultiplier = ActiveSkillMultiplier;
        var requiresTarget = SkillKit != PrototypeSkillKit.Astra &&
            SkillKit != PrototypeSkillKit.Brakk && SkillKit != PrototypeSkillKit.Nyx;
        if (requiresTarget && !AcquireTarget())
        {
            abilityPowerMultiplier = 1f;
            return;
        }

        switch (SkillKit)
        {
            case PrototypeSkillKit.Nova:
                CastHellfireImpact();
                break;
            case PrototypeSkillKit.Ion:
                battle.Announce("ION  ·  STATIC FIELD");
                DealDamage(Target, SkillPower(skills == null ? null : skills.Active, 0.9f));
                if (Target.IsAlive) Target.ApplyStun(1f);
                HitSecondaryTarget(0.45f, true);
                break;
            case PrototypeSkillKit.Astra:
                CastDragonRealm();
                break;
            case PrototypeSkillKit.Krag:
                battle.Announce("KRAG  ·  CRUSHING ORBIT");
                DealDamage(Target, SkillPower(skills == null ? null : skills.Active, 1.15f));
                Target.ApplyAttackDebuff(4f);
                GrantShield(Mathf.RoundToInt(MaxHealth * 0.15f), 4f);
                break;
            case PrototypeSkillKit.Vex:
                battle.Announce("VEX  ·  VOID STEP");
                Target = battle.FindTarget(this);
                DealDamage(Target, SkillPower(skills == null ? null : skills.Active, 1.7f));
                ApplyHaste(3f);
                break;
            case PrototypeSkillKit.Rook:
                battle.Announce("ROOK  ·  PINNING SHOT");
                DealDamage(Target, SkillPower(skills == null ? null : skills.Active, 1.5f), 0.5f);
                Target.ApplyAttackSlow(3f);
                break;
            case PrototypeSkillKit.Lyra:
                battle.Announce("LYRA  ·  SUNPIERCER");
                DealDamage(Target, SkillPower(skills == null ? null : skills.Active, 1.35f));
                Target.ApplyBurn(this, Mathf.RoundToInt(EffectiveAttack * 0.25f), 4f);
                break;
            case PrototypeSkillKit.Brakk:
                CastSingularityPull();
                break;
            case PrototypeSkillKit.Hex:
                battle.Announce("HEX  ·  CORRUPTION CODE");
                DealDamage(Target, SkillPower(skills == null ? null : skills.Active, 0.85f));
                Target.ApplyPoison(this, Mathf.RoundToInt(Target.MaxHealth * 0.025f), 5f);
                Target.ApplyAttackDebuff(5f);
                break;
            case PrototypeSkillKit.Nyx:
                battle.Announce("NYX  ·  ABYSSAL HYMN");
                var restored = battle.FindLowestHealth(battle.GetTeam(Team));
                if (restored != null)
                {
                    restored.Heal(Mathf.RoundToInt(EffectiveAttack * 0.7f));
                    restored.GainEnergy(15f);
                    restored.CleanseNegativeStatuses();
                }
                break;
            case PrototypeSkillKit.Mira:
                battle.Announce("MIRA  ·  UNDERTOW");
                DealDamage(Target, SkillPower(skills == null ? null : skills.Active, 0.85f));
                Target.ApplyAttackSlow(4f);
                HitSecondaryTarget(0.45f);
                var tideAlly = battle.FindLowestHealth(battle.GetTeam(Team));
                if (tideAlly != null)
                {
                    tideAlly.Heal(Mathf.RoundToInt(EffectiveAttack * 0.55f));
                }
                break;
            case PrototypeSkillKit.Drake:
                battle.Announce("DRAKE  ·  DRAGON DIVE");
                DealDamage(Target, SkillPower(skills == null ? null : skills.Active, 1.65f));
                Target.ApplyBurn(this, Mathf.RoundToInt(EffectiveAttack * 0.3f), 4f);
                GrantShield(Mathf.RoundToInt(MaxHealth * 0.18f), 4f);
                break;
        }

        abilityPowerMultiplier = 1f;
    }

    private void StartUltimate()
    {
        energy = 0f;
        UpdateEnergyBar();
        attackCooldown = EffectiveAttackInterval;
        BeginAction(
            PerformUltimate,
            SkillDuration(skills == null ? null : skills.Ultimate, UltimateActionDuration),
            ConfiguredHitDelay(skills == null ? null : skills.Ultimate, UltimateHitDelay),
            true,
            true);
    }

    private void PerformUltimate()
    {
        abilityPowerMultiplier = UltimateSkillMultiplier;
        if (SkillKit == PrototypeSkillKit.Hex && !AcquireTarget())
        {
            abilityPowerMultiplier = 1f;
            return;
        }

        switch (SkillKit)
        {
            case PrototypeSkillKit.Nova:
                CastCrimsonGale();
                break;
            case PrototypeSkillKit.Ion:
                CastVoltRush();
                break;
            case PrototypeSkillKit.Astra:
                CastDraconianAegis();
                break;
            case PrototypeSkillKit.Krag:
                CastGravityBulwark();
                break;
            case PrototypeSkillKit.Vex:
                CastCrimsonExecute();
                break;
            case PrototypeSkillKit.Rook:
                CastRailBarrage();
                break;
            case PrototypeSkillKit.Lyra:
                CastHeliosRain();
                break;
            case PrototypeSkillKit.Brakk:
                CastCosmicLeviathan();
                break;
            case PrototypeSkillKit.Hex:
                CastSingularityMine();
                break;
            case PrototypeSkillKit.Nyx:
                CastNightBloom();
                break;
            case PrototypeSkillKit.Mira:
                CastLeviathansGrace();
                break;
            case PrototypeSkillKit.Drake:
                CastCataclysmRoar();
                break;
        }

        abilityPowerMultiplier = 1f;
    }

    private void BeginAction(
        System.Action action,
        float duration,
        float hitDelay,
        bool skill,
        bool ultimate)
    {
        pendingAction = action;
        actionHitRemaining = hitDelay;
        isUltimateAction = ultimate;
        if (skill)
        {
            skillPulse = duration;
            PrototypeBattleFeedback.PlayAction(SkillKit, ultimate);
            if (IsBoss)
            {
                PrototypeEffect.Spawn(
                    prototypeSprite,
                    transform.position,
                    SkillAccent,
                    ultimate ? 0.5f : 0.3f,
                    ultimate ? 1.9f : 1.3f,
                    Mathf.Min(duration, 0.45f));
            }
        }
        else
        {
            attackPulse = duration;
        }
    }

    private void UpdatePendingAction()
    {
        if (pendingAction == null)
        {
            return;
        }

        actionHitRemaining -= Time.deltaTime;
        if (actionHitRemaining > 0f)
        {
            return;
        }

        var action = pendingAction;
        pendingAction = null;
        if (IsAlive && !battle.IsFinished)
        {
            action();
        }
    }

    private bool AcquireTarget()
    {
        return Target != null && Target.IsAlive;
    }

    private void CastHellfireImpact()
    {
        battle.Announce("FIRE GOD - HELLFIRE IMPACT");
        var point = Target.transform.position;
        foreach (var enemy in battle.GetOpponents(Team))
        {
            if (!enemy.IsAlive || !PrototypeFireCombat.PointInCircle(enemy.transform.position, point, 1.8f))
            {
                continue;
            }

            DealDamage(
                enemy,
                SkillPower(skills == null ? null : skills.Active, 1.45f),
                PrototypeDamageType.Fire);
            if (enemy.IsAlive)
            {
                enemy.ApplyKnockUp(0.28f);
                enemy.ApplyAsh(this, 1);
            }
        }

        PrototypeFireVfx.Spawn("FireGodHellfireImpact", point, 0.065f, 1.35f, false, 112);
        PrototypeFireZone.SpawnMagma(this, battle, prototypeSprite, point, 1.8f, 4f, 0.5f, 0.18f);
    }

    private void CastCrimsonGale()
    {
        if (!AcquireTarget())
        {
            Target = battle.FindTarget(this);
        }
        if (!AcquireTarget())
        {
            return;
        }

        battle.Announce("FIRE GOD - CRIMSON GALE");
        var origin = transform.position;
        var direction = (Target.transform.position - origin).normalized;
        foreach (var enemy in battle.GetOpponents(Team))
        {
            if (!enemy.IsAlive || !PrototypeFireCombat.PointInCone(
                    enemy.transform.position, origin, direction, 7f, 35f))
            {
                continue;
            }

            var stacks = enemy.ConsumeAsh(this);
            if (stacks > 0)
            {
                DealFlatDamage(
                    enemy,
                    PrototypeFireCombat.MissingHealthDetonation(enemy.MaxHealth, enemy.CurrentHealth, stacks),
                    PrototypeDamageType.Fire,
                    PrototypeDamageFlags.AshDetonation);
            }
            DealDamage(
                enemy,
                SkillPower(skills == null ? null : skills.Ultimate, 2.2f),
                PrototypeDamageType.Fire);
            if (enemy.IsAlive)
            {
                enemy.ApplyKnockback(direction, 1.6f);
                enemy.ApplyAsh(this, 1);
            }
        }

        PrototypeFireVfx.SpawnCrimsonGale(origin, direction);
        PrototypeFireZone.SpawnScorch(
            this, battle, prototypeSprite, origin, direction, 7f, 35f, 5f, 0.5f, 0.12f);
        PrototypeFireZone.ConvertIntersectingMagmaPools(this, origin, direction, 7f, 35f);
    }

    private void CastVoltRush()
    {
        battle.Announce("ION  ·  VOLT RUSH");
        var power = SkillPower(skills == null ? null : skills.Ultimate, 0.85f) * UltimateSkillMultiplier;
        StartCoroutine(RunTimedSequence(3, 0.11f, hit =>
        {
            if (!IsAlive || battle.IsFinished)
            {
                return;
            }
            if (Target == null || !Target.IsAlive)
            {
                Target = battle.FindTarget(this);
            }

            if (Target == null)
            {
                return;
            }

            DealDamage(Target, power);
            PrototypeEffect.Spawn(prototypeSprite, Target.transform.position, bodyColor, 0.2f, 0.9f, 0.2f);
        }));
    }

    private void CastDragonRealm()
    {
        battle.Announce("AURELIA  ·  LONG MACH TRAN");
        PrototypeWaterDomain.Spawn(this, battle, transform.position, 6f);
        PrototypeWaterVfx.Spawn("AureliaCleansingRing", transform.position, 0.055f, 1.8f, false, 113);
    }

    private void CastDraconianAegis()
    {
        battle.Announce("AURELIA  ·  VAN LY LONG BICH");
        PrototypeWaterVfx.Spawn("AureliaUltimateDragon", transform.position, 0.065f, 2.15f, false, 112);
        var shield = Mathf.RoundToInt(MaxHealth * 0.35f * UltimateSkillMultiplier);
        foreach (var ally in battle.GetTeam(Team))
        {
            if (!ally.IsAlive)
            {
                continue;
            }

            ally.GrantAureliaAegis(this, shield);
            ApplyAureliaSupport(ally, 0, 0, 0f);
            PrototypeWaterVfx.Spawn(
                "AureliaBlessingImpact", ally.transform.position, 0.055f, 1.1f, false, 115);
        }
    }

    private void CastGravityBulwark()
    {
        battle.Announce("KRAG  ·  GRAVITY BULWARK");
        GrantShield(Mathf.RoundToInt(MaxHealth * 0.45f), 5f);
        foreach (var enemy in battle.GetOpponents(Team))
        {
            if (enemy.IsAlive)
            {
                enemy.ApplyTaunt(this, 4f);
            }
        }

        PrototypeEffect.Spawn(prototypeSprite, transform.position, bodyColor, 1.1f, 2f, 0.5f);
    }

    private void CastCrimsonExecute()
    {
        Target = battle.FindLowestHealth(battle.GetOpponents(Team));
        if (Target == null)
        {
            return;
        }

        battle.Announce("VEX  ·  CRIMSON EXECUTE");
        var basePower = SkillPower(skills == null ? null : skills.Ultimate, 2.4f);
        var multiplier = Target.CurrentHealth <= Target.MaxHealth * 0.3f ? basePower * 1.67f : basePower;
        DealDamage(Target, multiplier);
        PrototypeEffect.Spawn(prototypeSprite, Target.transform.position, bodyColor, 0.35f, 1.7f, 0.35f);
    }

    private void CastRailBarrage()
    {
        battle.Announce("ROOK  ·  RAIL BARRAGE");
        var targets = new List<PrototypeCombatant>();
        foreach (var enemy in battle.GetOpponents(Team))
        {
            if (enemy.IsAlive)
            {
                targets.Add(enemy);
            }
        }
        var power = SkillPower(skills == null ? null : skills.Ultimate, 1.15f) * UltimateSkillMultiplier;
        StartCoroutine(RunTimedSequence(targets.Count, 0.06f, index =>
        {
            var enemy = targets[index];
            if (!enemy.IsAlive || battle.IsFinished)
            {
                return;
            }
            DealDamage(enemy, power, 0.3f);
            enemy.ApplyAttackSlow(4f);
            PrototypeEffect.Spawn(prototypeSprite, enemy.transform.position, bodyColor, 0.25f, 1.2f, 0.3f);
        }));
    }

    private void CastHeliosRain()
    {
        battle.Announce("LYRA  ·  HELIOS RAIN");
        var targets = new List<PrototypeCombatant>();
        foreach (var enemy in battle.GetOpponents(Team))
        {
            if (enemy.IsAlive)
            {
                targets.Add(enemy);
            }
        }
        var power = SkillPower(skills == null ? null : skills.Ultimate, 1.1f) * UltimateSkillMultiplier;
        StartCoroutine(RunTimedSequence(targets.Count, 0.06f, index =>
        {
            var enemy = targets[index];
            if (!enemy.IsAlive || battle.IsFinished)
            {
                return;
            }
            var wasBurning = enemy.HasBurn;
            DealDamage(enemy, power);
            enemy.ApplyBurn(this, Mathf.RoundToInt(EffectiveAttack * UltimateSkillMultiplier * 0.2f), 4f);
            PrototypeEffect.SpawnLine(
                prototypeSprite,
                enemy.transform.position + Vector3.up * 2.2f,
                enemy.transform.position,
                wasBurning ? new Color(1f, 0.28f, 0.04f) : SkillAccent,
                0.28f,
                wasBurning ? 0.12f : 0.07f);
        }));
    }

    internal static IEnumerator RunTimedSequence(int count, float interval, System.Action<int> step)
    {
        yield return null;
        for (var index = 0; index < count; index++)
        {
            step(index);
            if (index + 1 < count)
            {
                yield return new WaitForSeconds(interval);
            }
        }
    }

    private void CastSingularityPull()
    {
        battle.Announce("KRONOS  ·  SINGULARITY PULL");
        var empowered = voidAscensionRemaining > 0f;
        var radius = empowered ? 9f : 6f;
        var tauntDuration = empowered ? 2.5f : 2f;
        voidTauntReductionRemaining = Mathf.Max(voidTauntReductionRemaining, tauntDuration);
        PrototypeVoidVfx.Spawn("KronosSingularity", transform.position, 0.065f, empowered ? 2.5f : 2f, false, 45);
        foreach (var enemy in battle.GetOpponents(Team))
        {
            if (!enemy.IsAlive)
            {
                continue;
            }
            var delta = transform.position - enemy.transform.position;
            var distance = delta.magnitude;
            if (distance > radius)
            {
                continue;
            }
            enemy.ApplyKnockback(delta, Mathf.Max(0f, distance - 0.9f));
            enemy.ApplyTaunt(this, tauntDuration);
            enemy.ApplyVoidParasite(this);
        }
    }

    private void CastCosmicLeviathan()
    {
        battle.Announce("KRONOS  ·  COSMIC LEVIATHAN");
        if (voidAscensionRemaining <= 0f)
        {
            var bonusHealth = Mathf.RoundToInt(baseMaxHealth * 0.4f);
            MaxHealth = baseMaxHealth + bonusHealth;
            currentHealth = Mathf.Min(MaxHealth, currentHealth + bonusHealth);
            AttackRange = baseAttackRange * 1.2f;
            body.transform.localScale = Vector3.one * bodySize * 2f;
            UpdateHealthBar();
        }
        voidAscensionRemaining = 10f;
        voidDecayTickRemaining = 0f;
        PrototypeVoidVfx.Spawn("KronosLeviathanAwaken", transform.position, 0.065f, 2.4f, false, 44);
        if (voidAscensionVfx != null)
        {
            Destroy(voidAscensionVfx.gameObject);
        }
        voidAscensionVfx = PrototypeVoidVfx.SpawnAttached(
            "KronosLeviathanAura", transform, 0.08f, 2.5f, true, 1);
    }

    private void CastSingularityMine()
    {
        battle.Announce("HEX  ·  SINGULARITY MINE");
        DealDamage(Target, SkillPower(skills == null ? null : skills.Ultimate, 2.4f));
        Target.ApplyAttackSlow(5f);
        Target.ApplyPoison(this, Mathf.RoundToInt(Target.MaxHealth * 0.03f), 5f);
        PrototypeEffect.Spawn(prototypeSprite, Target.transform.position, bodyColor, 0.35f, 2f, 0.5f);
    }

    private void CastNightBloom()
    {
        battle.Announce("NYX  ·  NIGHT BLOOM");
        foreach (var ally in battle.GetTeam(Team))
        {
            if (ally.IsAlive)
            {
                ally.Heal(Mathf.RoundToInt(EffectiveAttack * 0.55f));
                ally.ApplyHaste(5f);
                ally.ApplyAttackBuff(5f);
                ally.CleanseNegativeStatuses();
            }
        }

        PrototypeEffect.Spawn(prototypeSprite, transform.position, bodyColor, 0.7f, 2f, 0.5f);
    }

    private void CastLeviathansGrace()
    {
        battle.Announce("MIRA  ·  LEVIATHAN'S GRACE");
        foreach (var ally in battle.GetTeam(Team))
        {
            if (!ally.IsAlive)
            {
                continue;
            }

            ally.Heal(Mathf.RoundToInt(EffectiveAttack * 0.45f));
            ally.GrantShield(Mathf.RoundToInt(ally.MaxHealth * 0.12f), 5f);
        }

        foreach (var enemy in battle.GetOpponents(Team))
        {
            if (enemy.IsAlive)
            {
                enemy.ApplyAttackSlow(5f);
            }
        }

        PrototypeEffect.Spawn(prototypeSprite, transform.position, bodyColor, 0.8f, 2.2f, 0.5f);
    }

    private void CastCataclysmRoar()
    {
        battle.Announce("DRAKE  ·  CATACLYSM ROAR");
        var power = SkillPower(skills == null ? null : skills.Ultimate, 1.25f);
        foreach (var enemy in battle.GetOpponents(Team))
        {
            if (!enemy.IsAlive)
            {
                continue;
            }

            DealDamage(enemy, power);
            enemy.ApplyAttackDebuff(5f);
            enemy.ApplyBurn(this, Mathf.RoundToInt(EffectiveAttack * 0.15f), 3f);
        }

        ApplyHaste(5f);
        PrototypeEffect.Spawn(prototypeSprite, transform.position, bodyColor, 0.8f, 2.4f, 0.5f);
    }

    private void TriggerArcChain()
    {
        HitSecondaryTarget(0.6f, true);
    }

    private void HitSecondaryTarget(float multiplier, bool burnAware = false)
    {
        var origin = Target;
        var chainTarget = FindChainTarget(origin, null, burnAware, false);
        if (chainTarget == null)
        {
            return;
        }

        DealDamage(chainTarget, multiplier);
        PrototypeEffect.SpawnLine(
            prototypeSprite,
            origin.transform.position,
            chainTarget.transform.position,
            SkillAccent,
            0.2f,
            0.06f);

        var extraTarget = burnAware
            ? FindChainTarget(chainTarget, origin, true, true)
            : null;
        var extraMultiplier = IonBurnBounceMultiplier(extraTarget != null);
        if (extraTarget != null && extraMultiplier > 0f)
        {
            DealDamage(extraTarget, extraMultiplier);
            PrototypeEffect.SpawnLine(
                prototypeSprite,
                chainTarget.transform.position,
                extraTarget.transform.position,
                SkillAccent,
                0.2f,
                0.045f);
        }
    }

    private PrototypeCombatant FindChainTarget(
        PrototypeCombatant origin,
        PrototypeCombatant excluded,
        bool preferBurn,
        bool requireBurn)
    {
        PrototypeCombatant best = null;
        var bestScore = float.MaxValue;
        foreach (var enemy in battle.GetOpponents(Team))
        {
            if (!enemy.IsAlive || enemy == origin || enemy == excluded || requireBurn && !enemy.HasBurn)
            {
                continue;
            }

            var burnPriority = preferBurn && enemy.HasBurn ? -1000f : 0f;
            var score = burnPriority + (enemy.transform.position - origin.transform.position).sqrMagnitude;
            if (score < bestScore)
            {
                best = enemy;
                bestScore = score;
            }
        }

        return best;
    }

    private void SpawnHydroBeads()
    {
        var candidates = new List<PrototypeCombatant>();
        foreach (var ally in battle.GetTeam(Team))
        {
            if (ally.IsAlive)
            {
                candidates.Add(ally);
            }
        }

        for (var count = Mathf.Min(2, candidates.Count); count > 0; count--)
        {
            var index = UnityEngine.Random.Range(0, candidates.Count);
            PrototypeHydroBead.Spawn(this, candidates[index]);
            candidates.RemoveAt(index);
        }
    }

    internal void ResolveHydroBead(PrototypeCombatant recipient)
    {
        if (recipient == null || !recipient.IsAlive || recipient.Team != Team)
        {
            return;
        }

        if (recipient == this)
        {
            waterStrideRemaining = Mathf.Max(waterStrideRemaining, 3f);
            activeSkillCooldown = Mathf.Max(0f, activeSkillCooldown - 1f);
            PrototypeWaterVfx.Spawn(
                "AureliaCleansingRing", transform.position, 0.055f, 0.75f, false, 113);
            return;
        }

        recipient.GainEnergy(3f);
        ApplyAureliaSupport(recipient, 0, Mathf.RoundToInt(MaxHealth * 0.06f), 4f);
    }

    internal void TickWaterDomain(PrototypeWaterDomain domain)
    {
        var heal = Mathf.Max(1, Mathf.RoundToInt(MaxHealth * 0.02f * ActiveSkillMultiplier));
        foreach (var ally in battle.GetTeam(Team))
        {
            if (domain.Contains(ally))
            {
                ApplyAureliaSupport(ally, heal, 0, 0f);
            }
        }
    }

    internal void BurstWaterDomain()
    {
        var heal = Mathf.Max(1, Mathf.RoundToInt(MaxHealth * 0.04f * ActiveSkillMultiplier));
        foreach (var ally in battle.GetTeam(Team))
        {
            if (ally.IsAlive)
            {
                ApplyAureliaSupport(ally, heal, 0, 0f);
            }
        }
    }

    internal void HealLowestAlly(int amount)
    {
        var ally = battle.FindLowestHealth(battle.GetTeam(Team));
        if (ally != null)
        {
            ApplyAureliaSupport(ally, amount, 0, 0f);
        }
    }

    private void ApplyAureliaSupport(PrototypeCombatant recipient, int heal, int shield, float shieldDuration)
    {
        if (recipient == null || !recipient.IsAlive || recipient.Team != Team)
        {
            return;
        }

        var multiplier = recipient.IsHardControlled ? 1.3f : 1f;
        if (heal > 0)
        {
            recipient.Heal(Mathf.RoundToInt(heal * multiplier));
        }
        if (shield > 0)
        {
            recipient.GrantShield(Mathf.RoundToInt(shield * multiplier), shieldDuration);
        }
        foreach (var ally in battle.GetTeam(Team))
        {
            if (ally.IsAlive && (ally.transform.position - recipient.transform.position).sqrMagnitude <= 9f)
            {
                ally.CleanseOneNegativeStatus();
            }
        }
        PrototypeWaterVfx.Spawn(
            "AureliaCleansingRing", recipient.transform.position, 0.055f, 0.9f, false, 113);
    }

    private void TriggerMomentum()
    {
        hasteRemaining = 5f;
        GainEnergy(50f);
        battle.Announce("VEX  ·  MOMENTUM");
    }

    private int Heal(int amount)
    {
        if (!IsAlive || amount <= 0)
        {
            return 0;
        }

        if (HasVoidParasite)
        {
            amount = Mathf.Max(1, Mathf.RoundToInt(amount * 0.85f));
        }
        var previous = currentHealth;
        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
        UpdateHealthBar();
        var healed = currentHealth - previous;
        if (healed > 0)
        {
            PrototypeDamageNumber.SpawnText(
                transform.position + Vector3.up * 1.12f,
                $"+{healed}",
                new Color(0.35f, 1f, 0.5f));
        }
        return healed;
    }

    private void HealAndReinforce(int amount)
    {
        var hadShield = HasShield;
        var healed = Heal(amount);
        if (hadShield && healed > 0)
        {
            ReinforceShield(healed);
        }
    }

    private void ReinforceShield(int healAmount)
    {
        var reinforcement = ShieldReinforcement(MaxHealth, currentShield, healAmount);
        if (reinforcement <= 0)
        {
            return;
        }

        currentShield += reinforcement;
        shieldVisual.enabled = true;
        PrototypeEffect.SpawnBarrier(prototypeSprite, transform.position, new Color(0.45f, 1f, 0.75f), 0.3f);
    }

    private void GrantShield(int amount, float duration)
    {
        if (!IsAlive || amount <= 0)
        {
            return;
        }

        if (HasVoidParasite)
        {
            amount = Mathf.Max(1, Mathf.RoundToInt(amount * 0.85f));
        }
        currentShield = Mathf.Max(currentShield, amount);
        shieldRemaining = Mathf.Max(shieldRemaining, duration);
        shieldVisual.enabled = true;
    }

    private void AddTemporaryShield(int amount, float duration)
    {
        if (!IsAlive || amount <= 0)
        {
            return;
        }

        var cap = Mathf.RoundToInt(MaxHealth * 0.6f);
        currentShield = Mathf.Min(cap, currentShield + amount);
        shieldRemaining = Mathf.Max(shieldRemaining, duration);
        shieldVisual.enabled = currentShield > 0;
    }

    private void ApplyVoidParasite(PrototypeCombatant source)
    {
        if (!IsAlive || source == null || !source.IsAlive)
        {
            return;
        }

        voidParasiteSource = source;
        voidParasiteRemaining = 4f;
        if (voidParasiteVfx == null)
        {
            voidParasiteVfx = PrototypeVoidVfx.SpawnAttached(
                "KronosParasite", transform, 0.08f, 1.25f, true, 108);
        }
    }

    private void ClearVoidParasite()
    {
        voidParasiteRemaining = 0f;
        voidParasiteSource = null;
        if (voidParasiteVfx != null)
        {
            Destroy(voidParasiteVfx.gameObject);
            voidParasiteVfx = null;
        }
    }

    private void GrantAureliaAegis(PrototypeCombatant source, int amount)
    {
        GrantShield(amount, 8f);
        aureliaAegisSource = source;
        aureliaAegisRemaining = 8f;
        aureliaControlImmunityRemaining = 2f;
        if (aureliaShieldVfx != null)
        {
            Destroy(aureliaShieldVfx.gameObject);
        }
        aureliaShieldVfx = PrototypeWaterVfx.SpawnAttached(
            "AureliaShieldLoop", transform, 0.09f, 2.6f, true, 113);
    }

    private void ApplyTaunt(PrototypeCombatant source, float duration)
    {
        if (HasControlImmunity)
        {
            return;
        }
        forcedTarget = source;
        tauntRemaining = AdjustNegativeDuration(duration);
        Target = source;
    }

    private void ApplyAttackSlow(float duration)
    {
        if (HasNegativeStatusImmunity)
        {
            return;
        }
        slowRemaining = Mathf.Max(slowRemaining, AdjustNegativeDuration(duration));
    }

    internal void ApplyMovementSlow(float percent, float duration)
    {
        if (HasNegativeStatusImmunity)
        {
            return;
        }
        if (percent >= movementSlowPercent || movementSlowRemaining <= 0f)
        {
            movementSlowPercent = Mathf.Clamp01(percent);
        }
        movementSlowRemaining = Mathf.Max(movementSlowRemaining, AdjustNegativeDuration(duration));
    }

    internal void ApplyDecayingSlow(float percent, float duration)
    {
        if (HasNegativeStatusImmunity)
        {
            return;
        }
        decayingSlowStart = Mathf.Max(decayingSlowStart, Mathf.Clamp01(percent));
        decayingSlowDuration = Mathf.Max(decayingSlowDuration, AdjustNegativeDuration(duration));
        decayingSlowElapsed = 0f;
    }

    internal void ApplyKnockback(Vector3 direction, float distance)
    {
        if (HasControlImmunity)
        {
            return;
        }
        direction.z = 0f;
        if (direction.sqrMagnitude <= 0.0001f || distance <= 0f)
        {
            return;
        }

        knockbackStart = transform.position;
        knockbackEnd = ClampToArena(knockbackStart + direction.normalized * distance);
        knockbackDuration = AdjustNegativeDuration(0.18f);
        knockbackRemaining = knockbackDuration;
    }

    internal void ApplyKnockUp(float duration)
    {
        if (HasControlImmunity)
        {
            return;
        }
        airborneDuration = Mathf.Max(0.05f, AdjustNegativeDuration(duration));
        airborneRemaining = airborneDuration;
        pendingAction = null;
        actionHitRemaining = 0f;
        attackPulse = 0f;
        skillPulse = 0f;
        isUltimateAction = false;
    }

    internal void ApplyGrounded(float duration)
    {
        if (HasNegativeStatusImmunity)
        {
            return;
        }
        groundedRemaining = Mathf.Max(groundedRemaining, AdjustNegativeDuration(duration));
    }

    private bool TryVoluntaryDisplacement(Vector3 destination)
    {
        if (groundedRemaining > 0f)
        {
            return false;
        }

        transform.position = ClampToArena(destination);
        return true;
    }

    private void ApplyHaste(float duration)
    {
        hasteRemaining = Mathf.Max(hasteRemaining, duration);
    }

    private void ApplyStun(float duration)
    {
        if (!HasControlImmunity)
        {
            stunRemaining = Mathf.Max(stunRemaining, AdjustNegativeDuration(duration));
        }
    }

    private void ApplyBurn(PrototypeCombatant source, int damage, float duration, float tickInterval = 1f)
    {
        if (HasNegativeStatusImmunity)
        {
            return;
        }
        var interval = Mathf.Max(0.05f, tickInterval);
        var candidateTotal = PrototypeFireCombat.RemainingDotDamage(damage, duration, interval);
        var currentTotal = PrototypeFireCombat.RemainingDotDamage(
            burnTickDamage, burnRemaining, burnTickInterval);
        if (candidateTotal >= currentTotal)
        {
            burnSource = source;
            burnDamageType = PrototypeDamageType.Fire;
            burnTickDamage = Mathf.Max(1, damage);
            burnTickInterval = interval;
            burnRemaining = Mathf.Max(0f, AdjustNegativeDuration(duration));
        }
        burnTickCooldown = Mathf.Min(
            burnTickCooldown <= 0f ? interval : burnTickCooldown,
            interval);
        burnVisual.enabled = true;
    }

    private void ApplyBurnTotal(
        PrototypeCombatant source,
        int totalDamage,
        float duration,
        float tickInterval)
    {
        var ticks = Mathf.Max(1, Mathf.CeilToInt(duration / Mathf.Max(0.05f, tickInterval)));
        ApplyBurn(source, Mathf.Max(1, Mathf.CeilToInt((float)totalDamage / ticks)), duration, tickInterval);
    }

    private void ApplyPoison(PrototypeCombatant source, int damage, float duration)
    {
        if (HasNegativeStatusImmunity)
        {
            return;
        }
        poisonSource = source;
        poisonDamage = Mathf.Max(poisonDamage, damage);
        poisonRemaining = Mathf.Max(poisonRemaining, AdjustNegativeDuration(duration));
        poisonTickCooldown = Mathf.Min(poisonTickCooldown <= 0f ? 1f : poisonTickCooldown, 1f);
    }

    private void ApplyAttackBuff(float duration)
    {
        attackBuffRemaining = Mathf.Max(attackBuffRemaining, duration);
    }

    private void ApplyAttackDebuff(float duration)
    {
        if (HasNegativeStatusImmunity)
        {
            return;
        }
        attackDebuffRemaining = Mathf.Max(attackDebuffRemaining, AdjustNegativeDuration(duration));
    }

    private bool IsHardControlled =>
        stunRemaining > 0f || airborneRemaining > 0f || knockbackRemaining > 0f || tauntRemaining > 0f;

    private bool HasControlImmunity =>
        voidControlImmunityRemaining > 0f || aureliaControlImmunityRemaining > 0f && currentShield > 0;

    private bool HasNegativeStatusImmunity => voidControlImmunityRemaining > 0f;

    private bool HasVoidParasite => voidParasiteRemaining > 0f && voidParasiteSource != null;

    private bool HasAureliaAegis => aureliaAegisRemaining > 0f && currentShield > 0;

    private float AdjustNegativeDuration(float duration)
    {
        return FindAureliaAuraSource() == null ? duration : duration * 0.9f;
    }

    private PrototypeCombatant FindAureliaAuraSource()
    {
        if (battle == null)
        {
            return null;
        }

        foreach (var ally in battle.GetTeam(Team))
        {
            if (ally != null && ally.IsAlive && ally.SkillKit == PrototypeSkillKit.Astra &&
                (ally.transform.position - transform.position).sqrMagnitude <= 64f)
            {
                return ally;
            }
        }
        return null;
    }

    private PrototypeWaterDomain FindAureliaDomain()
    {
        if (battle == null)
        {
            return null;
        }

        foreach (var ally in battle.GetTeam(Team))
        {
            if (ally != null && ally.SkillKit == PrototypeSkillKit.Astra)
            {
                var domain = PrototypeWaterDomain.FindFor(ally);
                if (domain != null && domain.Contains(this))
                {
                    return domain;
                }
            }
        }
        return null;
    }

    private void CleanseNegativeStatuses()
    {
        stunRemaining = 0f;
        slowRemaining = 0f;
        movementSlowPercent = 0f;
        movementSlowRemaining = 0f;
        decayingSlowStart = 0f;
        decayingSlowDuration = 0f;
        decayingSlowElapsed = 0f;
        groundedRemaining = 0f;
        airborneRemaining = 0f;
        burnRemaining = 0f;
        burnTickCooldown = 0f;
        burnTickDamage = 0;
        burnTickInterval = 1f;
        burnSource = null;
        fireDotMarkerRemaining = 0f;
        burnVisual.enabled = false;
        poisonRemaining = 0f;
        poisonTickCooldown = 0f;
        poisonDamage = 0;
        poisonSource = null;
        attackDebuffRemaining = 0f;
    }

    private bool CleanseOneNegativeStatus()
    {
        var statuses = new List<int>();
        if (stunRemaining > 0f || airborneRemaining > 0f || knockbackRemaining > 0f) statuses.Add(0);
        if (slowRemaining > 0f || movementSlowRemaining > 0f || decayingSlowDuration > 0f) statuses.Add(1);
        if (groundedRemaining > 0f) statuses.Add(2);
        if (burnRemaining > 0f || fireDotMarkerRemaining > 0f) statuses.Add(3);
        if (poisonRemaining > 0f) statuses.Add(4);
        if (attackDebuffRemaining > 0f) statuses.Add(5);
        if (tauntRemaining > 0f) statuses.Add(6);
        if (statuses.Count == 0)
        {
            return false;
        }

        switch (statuses[UnityEngine.Random.Range(0, statuses.Count)])
        {
            case 0:
                stunRemaining = 0f;
                airborneRemaining = 0f;
                knockbackRemaining = 0f;
                break;
            case 1:
                slowRemaining = 0f;
                movementSlowPercent = 0f;
                movementSlowRemaining = 0f;
                decayingSlowStart = 0f;
                decayingSlowDuration = 0f;
                decayingSlowElapsed = 0f;
                break;
            case 2:
                groundedRemaining = 0f;
                break;
            case 3:
                burnRemaining = 0f;
                burnTickCooldown = 0f;
                burnTickDamage = 0;
                burnSource = null;
                fireDotMarkerRemaining = 0f;
                burnVisual.enabled = false;
                break;
            case 4:
                poisonRemaining = 0f;
                poisonTickCooldown = 0f;
                poisonDamage = 0;
                poisonSource = null;
                break;
            case 5:
                attackDebuffRemaining = 0f;
                break;
            default:
                tauntRemaining = 0f;
                forcedTarget = null;
                Target = null;
                break;
        }
        return true;
    }

    private void DrainEnergy(float amount)
    {
        energy = Mathf.Max(0f, energy - amount);
        UpdateEnergyBar();
    }

    private PrototypeDamageResult DealDamage(
        PrototypeCombatant target,
        float multiplier,
        PrototypeDamageType damageType = PrototypeDamageType.Physical,
        PrototypeDamageFlags flags = PrototypeDamageFlags.Direct,
        float defenseIgnore = 0f)
    {
        if (target == null || !target.IsAlive)
        {
            return new PrototypeDamageResult(0, false, false);
        }

        if (SkillKit == PrototypeSkillKit.Vex && target.CurrentHealth <= target.MaxHealth * 0.5f)
        {
            multiplier *= 1.3f;
        }
        if (SkillKit == PrototypeSkillKit.Nova && damageType == PrototypeDamageType.Fire)
        {
            multiplier *= FireDamageMultiplier;
        }
        if (HasAureliaAegis)
        {
            multiplier *= 1.25f;
        }

        var calculated = PrototypeFireCombat.CalculateDamage(
            EffectiveAttack,
            target.EffectiveDefense,
            multiplier,
            damageType,
            target.FireResistance,
            defenseIgnore);
        return ApplyCalculatedDamage(target, calculated, damageType, flags);
    }

    private PrototypeDamageResult DealDamage(
        PrototypeCombatant target,
        float multiplier,
        float defenseIgnore)
    {
        return DealDamage(
            target,
            multiplier,
            PrototypeDamageType.Physical,
            PrototypeDamageFlags.Direct,
            defenseIgnore);
    }

    private PrototypeDamageResult DealFlatDamage(
        PrototypeCombatant target,
        int amount,
        PrototypeDamageType damageType,
        PrototypeDamageFlags flags)
    {
        var scaledAmount = SkillKit == PrototypeSkillKit.Nova && damageType == PrototypeDamageType.Fire
            ? Mathf.RoundToInt(amount * FireDamageMultiplier)
            : amount;
        var calculated = PrototypeFireCombat.CalculateDamage(
            scaledAmount, 0, 1f, damageType, target.FireResistance, 0f);
        return ApplyCalculatedDamage(target, calculated, damageType, flags);
    }

    private PrototypeDamageResult ApplyCalculatedDamage(
        PrototypeCombatant target,
        int calculatedDamage,
        PrototypeDamageType damageType,
        PrototypeDamageFlags flags)
    {
        if (target == null || !target.IsAlive)
        {
            return new PrototypeDamageResult(0, false, false);
        }

        if (voidPressureRemaining > 0f)
        {
            calculatedDamage = Mathf.Max(1, Mathf.RoundToInt(calculatedDamage * 0.9f));
        }
        var targetWasBurning = target.HasBurn;
        var wasAlive = target.IsAlive;
        var appliedDamage = target.TakeDamageFrom(
            calculatedDamage,
            (flags & PrototypeDamageFlags.Direct) != 0,
            this);
        DamageDealt += appliedDamage;
        if (appliedDamage > 0)
        {
            var strongImpact = skillPulse > 0f && (flags & PrototypeDamageFlags.DamageOverTime) == 0;
            PrototypeEffect.Spawn(
                prototypeSprite,
                target.transform.position,
                SkillAccent,
                strongImpact ? 0.32f : 0.16f,
                strongImpact ? 1.3f : 0.7f,
                strongImpact ? 0.28f : 0.16f);
            PrototypeBattleFeedback.PlayImpact(strongImpact);
            if (isUltimateAction && PrototypeGameFlow.HasInstance)
            {
                PrototypeGameFlow.Instance.ShowImpact(SkillAccent);
            }
        }
        var killed = wasAlive && !target.IsAlive;
        if (killed && SkillKit == PrototypeSkillKit.Vex)
        {
            TriggerMomentum();
        }
        if (appliedDamage > 0 && SkillKit == PrototypeSkillKit.Nova &&
            ((damageType == PrototypeDamageType.Fire && targetWasBurning) ||
             (flags & PrototypeDamageFlags.AshDetonation) != 0))
        {
            AddHeat(1);
            GainEnergy(2f);
        }
        if (appliedDamage > 0 && target.IsAlive && SkillKit == PrototypeSkillKit.Brakk &&
            (flags & PrototypeDamageFlags.Direct) != 0)
        {
            target.ApplyVoidParasite(this);
        }

        return new PrototypeDamageResult(appliedDamage, killed, targetWasBurning);
    }

    private void DealStatusDamage(PrototypeCombatant target, int damage)
    {
        var scaledDamage = voidPressureRemaining > 0f
            ? Mathf.Max(1, Mathf.RoundToInt(damage * 0.9f))
            : damage;
        DamageDealt += target.TakeDamageFrom(scaledDamage, false, this);
    }

    internal static float CombatStoppingDistance(float attackRange, float attackerBodySize, float targetBodySize)
    {
        return Mathf.Max(attackRange, (attackerBodySize + targetBodySize) * 0.58f);
    }

    internal static Vector3 MoveTowardAttackRange(
        Vector3 current,
        Vector3 target,
        float moveDistance,
        float stoppingDistance)
    {
        var delta = target - current;
        delta.z = 0f;
        var distance = delta.magnitude;
        if (distance <= stoppingDistance || distance <= 0.0001f || moveDistance <= 0f)
        {
            return current;
        }

        return current + delta / distance * Mathf.Min(moveDistance, distance - stoppingDistance);
    }

    internal static Vector3 CalculateEngagementPoint(
        Vector3 current,
        Vector3 target,
        float stoppingDistance,
        float lateralOffset)
    {
        var radial = current - target;
        radial.z = 0f;
        radial = radial.sqrMagnitude > 0.0001f ? radial.normalized : Vector3.left;
        var lateral = Mathf.Clamp(
            lateralOffset,
            -stoppingDistance * 0.68f,
            stoppingDistance * 0.68f);
        var radialDistance = Mathf.Sqrt(Mathf.Max(0f, stoppingDistance * stoppingDistance - lateral * lateral));
        var tangent = new Vector3(-radial.y, radial.x);
        return target + radial * radialDistance + tangent * lateral;
    }

    internal static Vector3 ClampToArena(Vector3 position)
    {
        return new Vector3(
            Mathf.Clamp(position.x, -ArenaHalfExtent, ArenaHalfExtent),
            Mathf.Clamp(position.y, -ArenaHalfExtent, ArenaHalfExtent),
            position.z);
    }

    internal static float IonBurnBounceMultiplier(bool hasBurningTarget)
    {
        return hasBurningTarget ? 0.3f : 0f;
    }

    internal static int ShieldReinforcement(int maxHealth, int currentShield, int healAmount)
    {
        var cap = Mathf.RoundToInt(maxHealth * 0.3f);
        return Mathf.Clamp(
            Mathf.RoundToInt(healAmount * 0.35f),
            0,
            Mathf.Max(0, cap - currentShield));
    }

    internal static bool ShouldFlipSprite(bool facesLeft, bool sourceFacesLeft)
    {
        return facesLeft != sourceFacesLeft;
    }

    private int TakeDamage(int damage, bool triggersOnHit = true)
    {
        return TakeDamageFrom(damage, triggersOnHit, null);
    }

    private int TakeDamageFrom(int damage, bool triggersOnHit, PrototypeCombatant source)
    {
        if (!IsAlive || voidCollapseRemaining > 0f)
        {
            return 0;
        }

        if (triggersOnHit)
        {
            incomingHitCount++;
            if (SkillKit == PrototypeSkillKit.Ion && incomingHitCount % 5 == 0)
            {
                GainEnergy(20f);
                battle.Announce("ION  ·  PHASE SHIFT");
                PrototypeEffect.Spawn(prototypeSprite, transform.position, bodyColor, 0.3f, 1.4f, 0.3f);
                return 0;
            }
        }

        var remainingDamage = damage;
        if (voidTauntReductionRemaining > 0f)
        {
            remainingDamage = Mathf.Max(1, Mathf.RoundToInt(remainingDamage * 0.7f));
        }
        if (FindAureliaAuraSource() != null)
        {
            remainingDamage = Mathf.Max(1, Mathf.RoundToInt(remainingDamage * 0.92f));
        }
        var domain = FindAureliaDomain();
        if (domain != null)
        {
            remainingDamage = domain.ReduceDamage(this, remainingDamage);
        }
        var absorbed = 0;
        if (currentShield > 0)
        {
            absorbed = Mathf.Min(currentShield, remainingDamage);
            currentShield -= absorbed;
            remainingDamage -= absorbed;
            if (currentShield <= 0)
            {
                DisableShield(true);
            }
        }

        var previousHealth = currentHealth;
        var minimumHealth = battle.IsIdleFarmMode && Team == PrototypeTeam.Allies ? 1 : 0;
        var triggersCollapse = minimumHealth == 0 && SkillKit == PrototypeSkillKit.Brakk &&
            voidRebirthCooldown <= 0f && remainingDamage >= currentHealth;
        currentHealth = triggersCollapse
            ? 1
            : Mathf.Max(minimumHealth, currentHealth - remainingDamage);
        if (triggersCollapse)
        {
            BeginBlackHoleCollapse();
        }
        hitFlash = 0.12f;
        UpdateHealthBar();

        if (triggersOnHit && SkillKit != PrototypeSkillKit.None && IsAlive)
        {
            GainEnergy(6f);
        }

        if (SkillKit == PrototypeSkillKit.Nova && IsAlive && flameShieldCooldown <= 0f &&
            previousHealth > MaxHealth * 0.25f && currentHealth <= MaxHealth * 0.25f)
        {
            ActivateFlameShield();
        }

        if (SkillKit == PrototypeSkillKit.Krag && IsAlive &&
            !lastBastionTriggered && currentHealth <= MaxHealth * 0.35f)
        {
            lastBastionTriggered = true;
            GrantShield(Mathf.RoundToInt(MaxHealth * 0.35f), 6f);
            battle.Announce("KRAG  ·  LAST BASTION");
        }

        if (SkillKit == PrototypeSkillKit.Drake && IsAlive &&
            !furyTriggered && currentHealth <= MaxHealth * 0.5f)
        {
            furyTriggered = true;
            ApplyHaste(6f);
            ApplyAttackBuff(6f);
            GainEnergy(30f);
            battle.Announce("DRAKE  ·  DRAGON FURY");
        }

        if (!IsAlive)
        {
            Target = null;
            guardLinkTarget = null;
            guardLinkRemaining = 0f;
            DisableShield();
            PrototypeFireZone.DestroyOwnedBy(this);
            PrototypeHydroBead.DestroyOwnedBy(this);
            PrototypeWaterDomain.DestroyOwnedBy(this);
            DisableVoidAscension();
            ClearVoidParasite();
            if (voidMassVfx != null)
            {
                Destroy(voidMassVfx.gameObject);
                voidMassVfx = null;
            }
            if (voidCollapseVfx != null)
            {
                Destroy(voidCollapseVfx.gameObject);
                voidCollapseVfx = null;
            }
            markVisual.enabled = false;
            burnVisual.enabled = false;
            if (heatAura != null) heatAura.enabled = false;
            if (heatVfx != null)
            {
                Destroy(heatVfx.gameObject);
                heatVfx = null;
            }
            body.color = Color.white;
        }

        var appliedDamage = absorbed + previousHealth - currentHealth;
        TrackAureliaBurstDamage(appliedDamage);
        TrackVoidResonance(appliedDamage);
        if (appliedDamage > 0 && source != null && source.IsAlive &&
            source.HasVoidParasite && source.voidParasiteSource == this && IsAlive)
        {
            AddTemporaryShield(Mathf.RoundToInt(appliedDamage * 0.2f), 4f);
            PrototypeVoidVfx.SpawnTravel(
                "KronosParasiteDrain", source.transform.position, transform.position, 0.22f, 0.85f, 113);
        }
        if (appliedDamage > 0 && SkillKit == PrototypeSkillKit.Brakk && voidResonanceStacks > 0 &&
            source != null && source.IsAlive && source != this)
        {
            var reflectRate = voidResonanceStacks * 0.01f * (voidTauntReductionRemaining > 0f ? 2f : 1f);
            source.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(appliedDamage * reflectRate)), false);
        }
        if (appliedDamage > 0)
        {
            PrototypeDamageNumber.Spawn(
                transform.position + Vector3.up * 0.95f,
                appliedDamage,
                absorbed > 0 ? new Color(0.35f, 0.9f, 1f) : new Color(1f, 0.78f, 0.2f));
        }
        return appliedDamage;
    }

    private void TrackAureliaBurstDamage(int damage)
    {
        if (SkillKit != PrototypeSkillKit.Astra || !IsAlive || damage <= 0 || dragonPressureCooldown > 0f)
        {
            return;
        }

        if (dragonPressureWindowRemaining <= 0f)
        {
            dragonPressureWindowRemaining = 1f;
            dragonPressureDamage = 0;
        }
        dragonPressureDamage += damage;
        if (dragonPressureDamage <= MaxHealth * 0.3f)
        {
            return;
        }

        dragonPressureCooldown = 60f;
        dragonPressureWindowRemaining = 0f;
        dragonPressureDamage = 0;
        battle.Announce("AURELIA  ·  UY AP LONG VUONG");
        PrototypeWaterVfx.Spawn("AureliaDragonPressure", transform.position, 0.06f, 1.7f, false, 116);
        foreach (var enemy in battle.GetOpponents(Team))
        {
            var delta = enemy.transform.position - transform.position;
            if (enemy.IsAlive && delta.sqrMagnitude <= 2.5f * 2.5f)
            {
                enemy.ApplyKnockback(delta.normalized, 3f);
                enemy.ApplyMovementSlow(0.5f, 2f);
            }
        }
    }

    private void TriggerAureliaShieldBreak(Vector3 center)
    {
        if (!IsAlive || battle == null || battle.IsFinished)
        {
            return;
        }

        PrototypeWaterVfx.Spawn("AureliaShieldBreak", center, 0.055f, 1.25f, false, 116);
        foreach (var enemy in battle.GetOpponents(Team))
        {
            if (!enemy.IsAlive || !PrototypeFireCombat.PointInCircle(enemy.transform.position, center, 1.8f))
            {
                continue;
            }
            DealDamage(enemy, 0.8f, PrototypeDamageType.Magic);
            if (enemy.IsAlive)
            {
                enemy.ApplyMovementSlow(0.4f, 2f);
            }
        }
    }

    private void TrackVoidResonance(int damage)
    {
        if (SkillKit != PrototypeSkillKit.Brakk || !IsAlive || damage <= 0 || voidResonanceStacks >= 10)
        {
            return;
        }

        voidResonanceDamage += damage;
        var threshold = Mathf.Max(1f, MaxHealth * 0.05f);
        while (voidResonanceDamage >= threshold && voidResonanceStacks < 10)
        {
            voidResonanceDamage -= threshold;
            voidResonanceStacks++;
        }
        RefreshVoidMassVisual();
    }

    private void RefreshVoidMassVisual()
    {
        if (voidMassVfx != null)
        {
            Destroy(voidMassVfx.gameObject);
            voidMassVfx = null;
        }
        if (voidResonanceStacks <= 0 || !IsAlive)
        {
            return;
        }

        voidMassVfx = PrototypeVoidVfx.SpawnAttached(
            "KronosMassOrbit", transform, 0.08f, 1.2f + voidResonanceStacks * 0.06f, true, 107);
    }

    private void BeginBlackHoleCollapse()
    {
        voidRebirthCooldown = 120f;
        voidCollapseRemaining = 2f;
        voidCollapseTickRemaining = 1f;
        voidCollapseHealth = 0;
        pendingAction = null;
        actionHitRemaining = 0f;
        attackPulse = 0f;
        skillPulse = 0f;
        body.enabled = false;
        battle.Announce("KRONOS  ·  BLACK HOLE COLLAPSE");
        voidCollapseVfx = PrototypeVoidVfx.SpawnAttached(
            "KronosCollapse", transform, 0.06f, 1.8f, true, 116);
    }

    private void TickBlackHoleCollapse()
    {
        if (voidCollapseRemaining <= 0f)
        {
            return;
        }

        voidCollapseRemaining = Mathf.Max(0f, voidCollapseRemaining - Time.deltaTime);
        voidCollapseTickRemaining -= Time.deltaTime;
        if (voidCollapseTickRemaining <= 0f)
        {
            voidCollapseTickRemaining += 1f;
            PrototypeVoidVfx.Spawn("KronosDecayPulse", transform.position, 0.055f, 1.7f, false, 114);
            foreach (var enemy in battle.GetOpponents(Team))
            {
                if (!enemy.IsAlive || !PrototypeFireCombat.PointInCircle(
                        enemy.transform.position, transform.position, 3.2f))
                {
                    continue;
                }
                var drained = enemy.TakeDamageFrom(Mathf.RoundToInt(enemy.MaxHealth * 0.05f), false, this);
                voidCollapseHealth += drained;
                DamageDealt += drained;
            }
        }

        if (voidCollapseRemaining > 0f)
        {
            return;
        }

        currentHealth = Mathf.Clamp(voidCollapseHealth, 1, MaxHealth);
        body.enabled = true;
        if (voidCollapseVfx != null)
        {
            Destroy(voidCollapseVfx.gameObject);
            voidCollapseVfx = null;
        }
        UpdateHealthBar();
    }

    private void TickVoidAscension()
    {
        if (voidAscensionRemaining <= 0f)
        {
            return;
        }

        voidAscensionRemaining = Mathf.Max(0f, voidAscensionRemaining - Time.deltaTime);
        voidDecayTickRemaining -= Time.deltaTime;
        if (voidDecayTickRemaining <= 0f)
        {
            voidDecayTickRemaining += 1f;
            PrototypeVoidVfx.Spawn("KronosDecayPulse", transform.position, 0.055f, 1.5f, false, 112);
            foreach (var enemy in battle.GetOpponents(Team))
            {
                if (enemy.IsAlive && PrototypeFireCombat.PointInCircle(
                        enemy.transform.position, transform.position, 3f))
                {
                    DealFlatDamage(
                        enemy,
                        Mathf.RoundToInt(MaxHealth * 0.03f),
                        PrototypeDamageType.Magic,
                        PrototypeDamageFlags.Direct);
                }
            }
        }
        if (voidAscensionRemaining <= 0f)
        {
            DisableVoidAscension();
        }
    }

    private void DisableVoidAscension()
    {
        voidAscensionRemaining = 0f;
        if (baseMaxHealth > 0)
        {
            MaxHealth = baseMaxHealth;
            currentHealth = Mathf.Min(currentHealth, MaxHealth);
        }
        if (baseAttackRange > 0f)
        {
            AttackRange = baseAttackRange;
        }
        if (body != null)
        {
            body.transform.localScale = Vector3.one * bodySize;
        }
        if (voidAscensionVfx != null)
        {
            Destroy(voidAscensionVfx.gameObject);
            voidAscensionVfx = null;
        }
    }

    private int EffectiveDefense
    {
        get
        {
            var multiplier = SkillKit == PrototypeSkillKit.Krag && currentHealth > MaxHealth * 0.5f
                ? 1.5f
                : 1f;
            if (SkillKit == PrototypeSkillKit.Brakk)
            {
                multiplier *= 1f + voidResonanceStacks * 0.02f;
                if (voidAscensionRemaining > 0f)
                {
                    multiplier *= 1.5f;
                }
            }
            return Mathf.RoundToInt(Defense * multiplier);
        }
    }

    private int EffectiveAttack
    {
        get
        {
            var multiplier = attackBuffRemaining > 0f ? 1.25f : 1f;
            if (attackDebuffRemaining > 0f)
            {
                multiplier *= 0.75f;
            }

            var effectiveAttack = Attack + (SkillKit == PrototypeSkillKit.Brakk
                ? Mathf.RoundToInt(MaxHealth * 0.015f)
                : 0);
            return Mathf.RoundToInt(
                effectiveAttack * multiplier * PassiveSkillMultiplier * abilityPowerMultiplier);
        }
    }

    private float BasicSkillMultiplier => 1f + (basicSkillLevel - 1) * 0.05f;
    private float PassiveSkillMultiplier => 1f + (passiveSkillLevel - 1) * 0.03f;
    private float ActiveSkillMultiplier => 1f + (activeSkillLevel - 1) * 0.08f;
    private float UltimateSkillMultiplier => 1f + (ultimateSkillLevel - 1) * 0.1f;

    internal void ApplyAsh(PrototypeCombatant source, int amount)
    {
        if (source == null || amount <= 0 || !IsAlive)
        {
            return;
        }

        ashBySource.TryGetValue(source, out var state);
        state.stacks = Mathf.Clamp(state.stacks + amount, 0, 3);
        state.remaining = 5f;
        ashBySource[source] = state;
        markVisual.enabled = TotalAshStacks > 0;
        var sheet = TotalAshStacks >= 3
            ? "FireGodAshSigil"
            : TotalAshStacks == 2 ? "FireGodAshTwo" : "FireGodAshOne";
        PrototypeFireVfx.Spawn(sheet, transform.position, 0.1f, 0.72f, false, 109);
    }

    internal int AshStacksFrom(PrototypeCombatant source)
    {
        return source != null && ashBySource.TryGetValue(source, out var state) ? state.stacks : 0;
    }

    internal int ConsumeAsh(PrototypeCombatant source)
    {
        var stacks = AshStacksFrom(source);
        if (stacks > 0)
        {
            ashBySource.Remove(source);
        }
        markVisual.enabled = TotalAshStacks > 0;
        return stacks;
    }

    private int TotalAshStacks
    {
        get
        {
            var total = 0;
            foreach (var state in ashBySource.Values)
            {
                total += state.stacks;
            }
            return total;
        }
    }

    internal void AddHeat(int amount)
    {
        heatStacks = Mathf.Clamp(heatStacks + Mathf.Max(0, amount), 0, 5);
        if (heatAura != null)
        {
            heatAura.enabled = heatStacks > 0;
        }
        if (heatStacks > 0 && heatVfx == null)
        {
            heatVfx = PrototypeFireVfx.SpawnAttached(
                "FireGodHeatAura", transform, 0.1f, 1.6f, true, 58);
        }
    }

    private void ActivateFlameShield()
    {
        GrantShield(PrototypeFireCombat.FlameShieldAmount(MaxHealth, heatStacks), 6f);
        flameShieldCooldown = 90f;
        battle.Announce("FIRE GOD - FLAME SHIELD");
        PrototypeFireVfx.Spawn("FireGodFlameShieldSpawn", transform.position, 0.06f, 1.65f, false, 114);
        if (flameShieldVfx != null)
        {
            Destroy(flameShieldVfx.gameObject);
        }
        flameShieldVfx = PrototypeFireVfx.SpawnAttached(
            "FireGodFlameShieldLoop", transform, 0.1f, 1.65f, true, 113);
        foreach (var enemy in battle.GetOpponents(Team))
        {
            var delta = enemy.transform.position - transform.position;
            if (enemy.IsAlive && delta.sqrMagnitude <= 1.75f * 1.75f)
            {
                enemy.ApplyKnockback(delta.normalized, 1.2f);
            }
        }
    }

    internal void ApplyMagmaTick(
        PrototypeCombatant target,
        PrototypeFireZone zone,
        float multiplier)
    {
        var result = DealDamage(
            target,
            multiplier,
            PrototypeDamageType.Fire,
            PrototypeDamageFlags.DamageOverTime);
        target.fireDotMarkerRemaining = Mathf.Max(target.fireDotMarkerRemaining, 0.6f);
        if (result.Killed)
        {
            OnFireDotKill(
                target,
                result.AppliedDamage * zone.RemainingTickCount,
                zone.RemainingDuration,
                0.5f);
            return;
        }

        target.ApplyMovementSlow(0.3f, 0.6f);
        if (target.AshStacksFrom(this) >= 3)
        {
            target.ConsumeAsh(this);
            PrototypeFireVfx.Spawn(
                "FireGodMagmaBurst", target.transform.position, 0.065f, 1.1f, false, 112);
            DealDamage(
                target,
                0.75f,
                PrototypeDamageType.True,
                PrototypeDamageFlags.AshDetonation);
            if (target.IsAlive)
            {
                target.ApplyDecayingSlow(0.7f, 2f);
            }
        }
    }

    internal void ApplyScorchTick(
        PrototypeCombatant target,
        PrototypeFireZone zone,
        float multiplier)
    {
        var result = DealDamage(
            target,
            multiplier,
            PrototypeDamageType.Fire,
            PrototypeDamageFlags.DamageOverTime);
        target.fireDotMarkerRemaining = Mathf.Max(target.fireDotMarkerRemaining, 0.6f);
        if (result.Killed)
        {
            OnFireDotKill(
                target,
                result.AppliedDamage * zone.RemainingTickCount,
                zone.RemainingDuration,
                0.5f);
            return;
        }
        target.ApplyGrounded(0.6f);
    }

    internal void OnFireDotKill(
        PrototypeCombatant deadTarget,
        int remainingDamage,
        float remainingDuration,
        float tickInterval)
    {
        if (SkillKit != PrototypeSkillKit.Nova || deadTarget == null || remainingDamage <= 0)
        {
            return;
        }

        var origin = deadTarget.transform.position;
        PrototypeFireVfx.Spawn("FireGodAshDissolve", origin, 0.08f, 1.1f, false, 112);
        foreach (var enemy in battle.GetOpponents(Team))
        {
            if (!enemy.IsAlive || !PrototypeFireCombat.PointInCircle(enemy.transform.position, origin, 4f))
            {
                continue;
            }

            enemy.ApplyAsh(this, 2);
            enemy.ApplyBurnTotal(
                this,
                Mathf.RoundToInt(remainingDamage * 0.5f),
                remainingDuration,
                tickInterval);
            PrototypeFireVfx.SpawnTravel(
                "FireGodEmberProjectile", origin, enemy.transform.position, 0.24f, 0.7f, 113, 0.55f);
            StartCoroutine(DelayedEmberIgnite(enemy.transform.position, 0.24f));
        }
    }

    private IEnumerator DelayedEmberIgnite(Vector3 position, float delay)
    {
        yield return new WaitForSeconds(delay);
        PrototypeFireVfx.Spawn("FireGodEmberIgnite", position, 0.07f, 0.9f, false, 112);
    }

    private void GainEnergy(float amount)
    {
        energy = Mathf.Min(MaxEnergy, energy + amount);
        UpdateEnergyBar();
    }

    private void UpdateStatuses()
    {
        activeSkillCooldown = Mathf.Max(0f, activeSkillCooldown - Time.deltaTime);
        voidPressureRemaining = Mathf.Max(0f, voidPressureRemaining - Time.deltaTime);
        voidControlImmunityRemaining = Mathf.Max(0f, voidControlImmunityRemaining - Time.deltaTime);
        voidTauntReductionRemaining = Mathf.Max(0f, voidTauntReductionRemaining - Time.deltaTime);
        voidRebirthCooldown = Mathf.Max(0f, voidRebirthCooldown - Time.deltaTime);
        if (voidParasiteRemaining > 0f)
        {
            voidParasiteRemaining = Mathf.Max(0f, voidParasiteRemaining - Time.deltaTime);
            if (voidParasiteRemaining <= 0f || voidParasiteSource == null || !voidParasiteSource.IsAlive)
            {
                ClearVoidParasite();
            }
        }
        TickBlackHoleCollapse();
        if (voidCollapseRemaining <= 0f)
        {
            TickVoidAscension();
        }
        flameShieldCooldown = Mathf.Max(0f, flameShieldCooldown - Time.deltaTime);
        waterStrideRemaining = Mathf.Max(0f, waterStrideRemaining - Time.deltaTime);
        dragonPressureCooldown = Mathf.Max(0f, dragonPressureCooldown - Time.deltaTime);
        dragonPressureWindowRemaining = Mathf.Max(0f, dragonPressureWindowRemaining - Time.deltaTime);
        if (dragonPressureWindowRemaining <= 0f) dragonPressureDamage = 0;
        var hadAureliaAegis = aureliaAegisRemaining > 0f;
        aureliaAegisRemaining = Mathf.Max(0f, aureliaAegisRemaining - Time.deltaTime);
        aureliaControlImmunityRemaining = Mathf.Max(0f, aureliaControlImmunityRemaining - Time.deltaTime);
        if (hadAureliaAegis && aureliaAegisRemaining <= 0f)
        {
            DisableAureliaAegis();
        }
        hasteRemaining = Mathf.Max(0f, hasteRemaining - Time.deltaTime);
        slowRemaining = Mathf.Max(0f, slowRemaining - Time.deltaTime);
        stunRemaining = Mathf.Max(0f, stunRemaining - Time.deltaTime);
        movementSlowRemaining = Mathf.Max(0f, movementSlowRemaining - Time.deltaTime);
        if (movementSlowRemaining <= 0f) movementSlowPercent = 0f;
        if (decayingSlowDuration > 0f)
        {
            decayingSlowElapsed = Mathf.Min(decayingSlowDuration, decayingSlowElapsed + Time.deltaTime);
            if (decayingSlowElapsed >= decayingSlowDuration)
            {
                decayingSlowStart = 0f;
                decayingSlowDuration = 0f;
                decayingSlowElapsed = 0f;
            }
        }
        groundedRemaining = Mathf.Max(0f, groundedRemaining - Time.deltaTime);
        fireDotMarkerRemaining = Mathf.Max(0f, fireDotMarkerRemaining - Time.deltaTime);
        UpdateDisplacement();
        attackBuffRemaining = Mathf.Max(0f, attackBuffRemaining - Time.deltaTime);
        attackDebuffRemaining = Mathf.Max(0f, attackDebuffRemaining - Time.deltaTime);
        TickBurn();
        TickDamageOverTime(ref poisonRemaining, ref poisonTickCooldown, poisonDamage, poisonSource);
        if (burnRemaining <= 0f)
        {
            burnVisual.enabled = false;
            burnTickDamage = 0;
            burnSource = null;
        }

        ashSources.Clear();
        ashSources.AddRange(ashBySource.Keys);
        foreach (var source in ashSources)
        {
            var state = ashBySource[source];
            state.remaining -= Time.deltaTime;
            if (source == null || !source.IsAlive || state.remaining <= 0f)
            {
                ashBySource.Remove(source);
            }
            else
            {
                ashBySource[source] = state;
            }
        }
        markVisual.enabled = IsAlive && TotalAshStacks > 0;
        if (heatAura != null)
        {
            heatAura.enabled = IsAlive && heatStacks > 0;
            heatAura.color = new Color(1f, 0.18f, 0.02f, 0.08f + heatStacks * 0.035f);
        }

        if (guardLinkRemaining > 0f)
        {
            guardLinkRemaining = Mathf.Max(0f, guardLinkRemaining - Time.deltaTime);
            if (guardLinkRemaining <= 0f || guardLinkTarget == null || !guardLinkTarget.IsAlive)
            {
                guardLinkTarget = null;
            }
        }

        if (tauntRemaining > 0f)
        {
            tauntRemaining -= Time.deltaTime;
            if (tauntRemaining <= 0f)
            {
                if (Target == forcedTarget)
                {
                    Target = null;
                }

                forcedTarget = null;
            }
        }

        if (shieldRemaining > 0f)
        {
            shieldRemaining -= Time.deltaTime;
            if (shieldRemaining <= 0f)
            {
                DisableShield();
            }
        }

    }

    private float CurrentMovementSlow => Mathf.Max(
        movementSlowRemaining > 0f ? movementSlowPercent : 0f,
        decayingSlowDuration > 0f
            ? PrototypeFireCombat.DecayingSlow(decayingSlowStart, decayingSlowDuration, decayingSlowElapsed)
            : 0f);

    private void UpdateDisplacement()
    {
        if (knockbackRemaining > 0f)
        {
            knockbackRemaining = Mathf.Max(0f, knockbackRemaining - Time.deltaTime);
            var progress = 1f - knockbackRemaining / Mathf.Max(0.01f, knockbackDuration);
            transform.position = Vector3.Lerp(knockbackStart, knockbackEnd, progress);
        }
        if (airborneRemaining > 0f)
        {
            airborneRemaining = Mathf.Max(0f, airborneRemaining - Time.deltaTime);
        }
    }

    private void TickBurn()
    {
        if (burnRemaining <= 0f || burnTickDamage <= 0 || !IsAlive)
        {
            return;
        }

        burnRemaining = Mathf.Max(0f, burnRemaining - Time.deltaTime);
        burnTickCooldown -= Time.deltaTime;
        if (burnTickCooldown > 0f)
        {
            return;
        }

        var source = burnSource;
        var result = source != null
            ? source.DealFlatDamage(
                this,
                burnTickDamage,
                burnDamageType,
                PrototypeDamageFlags.DamageOverTime)
            : new PrototypeDamageResult(TakeDamage(burnTickDamage, false), !IsAlive, true);
        burnTickCooldown += burnTickInterval;
        if (result.Killed && source != null)
        {
            source.OnFireDotKill(
                this,
                PrototypeFireCombat.RemainingDotDamage(
                    burnTickDamage, burnRemaining, burnTickInterval),
                burnRemaining,
                burnTickInterval);
            burnRemaining = 0f;
        }
    }

    private void TickDamageOverTime(
        ref float remaining,
        ref float tickCooldown,
        int damage,
        PrototypeCombatant source)
    {
        if (remaining <= 0f || damage <= 0 || !IsAlive)
        {
            return;
        }

        remaining = Mathf.Max(0f, remaining - Time.deltaTime);
        tickCooldown -= Time.deltaTime;
        if (tickCooldown <= 0f)
        {
            if (source != null)
            {
                source.DealStatusDamage(this, damage);
            }
            else
            {
                TakeDamage(damage, false);
            }

            tickCooldown += 1f;
        }
    }

    private void DisableShield(bool broken = false)
    {
        var aegisSource = broken && HasAureliaAegis ? aureliaAegisSource : null;
        currentShield = 0;
        shieldRemaining = 0f;
        DisableAureliaAegis();
        if (shieldVisual != null)
        {
            shieldVisual.enabled = false;
        }
        if (flameShieldVfx != null)
        {
            Destroy(flameShieldVfx.gameObject);
            flameShieldVfx = null;
            if (IsAlive)
            {
                PrototypeFireVfx.Spawn(
                    "FireGodFlameShieldBreak", transform.position, 0.06f, 1.65f, false, 114);
            }
        }
        if (aegisSource != null)
        {
            aegisSource.TriggerAureliaShieldBreak(transform.position);
        }
    }

    private void DisableAureliaAegis()
    {
        aureliaAegisRemaining = 0f;
        aureliaControlImmunityRemaining = 0f;
        aureliaAegisSource = null;
        if (aureliaShieldVfx != null)
        {
            Destroy(aureliaShieldVfx.gameObject);
            aureliaShieldVfx = null;
        }
    }

    private void UpdateFeedback()
    {
        UpdateSortingOrder();
        afterimageCooldown = Mathf.Max(0f, afterimageCooldown - Time.deltaTime);
        if (IsAlive && afterimageCooldown <= 0f && (isMoving || attackPulse > 0f || skillPulse > 0f))
        {
            PrototypeAfterimage.Spawn(body, sortingGroup.sortingOrder - 1, SkillAccent);
            afterimageCooldown = isMoving ? 0.12f : 0.08f;
        }
        skillPulse = Mathf.Max(0f, skillPulse - Time.deltaTime);
        if (skillPulse <= 0f)
        {
            isUltimateAction = false;
        }
        var voidScale = voidAscensionRemaining > 0f ? 2f : 1f;
        if (attackPulse > 0f)
        {
            attackPulse = Mathf.Max(0f, attackPulse - Time.deltaTime);
            var progress = 1f - attackPulse / BasicActionDuration;
            var pulse = 1f + Mathf.Sin(progress * Mathf.PI) * 0.08f;
            body.transform.localScale = new Vector3(
                bodySize * pulse * voidScale,
                bodySize * pulse * voidScale,
                1f);
        }
        else
        {
            body.transform.localScale = new Vector3(bodySize * voidScale, bodySize * voidScale, 1f);
        }

        if (IsAlive)
        {
            hitFlash -= Time.deltaTime;
            body.color = hitFlash > 0f
                ? new Color(1f, 0.55f, 0.55f)
                : Color.white;
        }

        UpdateBossPhaseVisuals();
        UpdateUltimateReadyVisual();
        UpdateAshVisual();
        UpdateStatusIcons();
        UpdateCharacterAnimation();
        UpdateAirborneVisual();
    }

    private void UpdateAirborneVisual()
    {
        if (body == null)
        {
            return;
        }

        if (airborneRemaining > 0f)
        {
            var progress = 1f - airborneRemaining / Mathf.Max(0.01f, airborneDuration);
            var position = definition != null && definition.HasStaticImportedSprite
                ? body.transform.localPosition
                : Vector3.zero;
            position.y += Mathf.Sin(progress * Mathf.PI) * 0.58f;
            body.transform.localPosition = position;
        }
        else if (definition != null && !definition.HasStaticImportedSprite)
        {
            body.transform.localPosition = Vector3.zero;
        }
    }

    private void UpdateAshVisual()
    {
        if (markVisual == null || !markVisual.enabled)
        {
            return;
        }

        var stacks = TotalAshStacks;
        if (stacks >= 3)
        {
            markVisual.transform.localPosition = new Vector3(0f, -bodySize * 0.36f);
            markVisual.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
            markVisual.color = new Color(1f, 0.18f, 0.02f, 0.9f);
        }
        else
        {
            var angle = Time.time * 4.5f;
            markVisual.transform.localPosition = new Vector3(
                Mathf.Cos(angle) * 0.48f,
                0.55f + Mathf.Sin(angle) * 0.22f);
            markVisual.transform.localScale = new Vector3(0.16f, 0.16f, 1f);
            markVisual.color = new Color(1f, 0.42f, 0.06f, 0.85f);
        }
        markVisual.transform.Rotate(0f, 0f, 150f * Time.deltaTime);
    }

    private void UpdateSortingOrder()
    {
        if (sortingGroup != null)
        {
            sortingGroup.sortingOrder = 60 - Mathf.RoundToInt(transform.position.y * 4f);
        }
    }

    private void UpdateUltimateReadyVisual()
    {
        if (ultimateReadyVisual == null)
        {
            return;
        }

        if (!IsAlive)
        {
            ultimateReadyVisual.enabled = false;
            return;
        }

        if (!ultimateReadyVisual.enabled)
        {
            return;
        }

        var pulse = 1f + Mathf.Sin(Time.time * 12f) * 0.22f;
        ultimateReadyVisual.transform.localScale = new Vector3(0.2f * pulse, 0.2f * pulse, 1f);
        ultimateReadyVisual.transform.Rotate(0f, 0f, 120f * Time.deltaTime);
    }

    private void UpdateCharacterAnimation()
    {
        if (deathVisualsHidden)
        {
            return;
        }

        if (characterSprites == null && Species != PrototypeSpecies.Unknown)
        {
            characterSprites = definition != null
                ? PrototypePixelArt.Create(definition, IsBoss)
                : PrototypePixelArt.Create(Species, CombatClass, SkillKit, bodyColor);
        }

        if (characterSprites == null || body == null)
        {
            return;
        }

        var nextState = !IsAlive
            ? PrototypeAnimationState.Death
            : isUltimateAction && skillPulse > 0f
                ? PrototypeAnimationState.Ultimate
                : skillPulse > 0f
                ? PrototypeAnimationState.Skill
                : attackPulse > 0f
                    ? PrototypeAnimationState.Attack
                    : hitFlash > 0f
                        ? PrototypeAnimationState.Hit
                        : isMoving
                            ? PrototypeAnimationState.Run
                            : PrototypeAnimationState.Idle;

        if (nextState != animationState)
        {
            animationState = nextState;
            animationTime = 0f;
        }
        else
        {
            animationTime += Time.deltaTime;
        }

        var frames = characterSprites.Get(animationState);
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        var frameDuration = SkillKit == PrototypeSkillKit.Nova
            ? animationState == PrototypeAnimationState.Attack
                ? SkillDuration(skills == null ? null : skills.Basic, 0.56f) / frames.Length
                : animationState == PrototypeAnimationState.Skill
                    ? SkillDuration(skills == null ? null : skills.Active, 1.3f) / frames.Length
                    : animationState == PrototypeAnimationState.Ultimate
                        ? SkillDuration(skills == null ? null : skills.Ultimate, 1.3f) / frames.Length
                        : 0.1f
            : animationState == PrototypeAnimationState.Idle
                ? 0.45f
                : animationState == PrototypeAnimationState.Run
                    ? 0.1f
                    : animationState == PrototypeAnimationState.Hit
                        ? 0.06f
                        : animationState == PrototypeAnimationState.Ultimate ? 0.065f : 0.07f;
        if (animationState == PrototypeAnimationState.Death &&
            animationTime >= frames.Length * frameDuration)
        {
            deathVisualsHidden = true;
            SetCombatVisualsVisible(false);
            return;
        }
        var rawFrame = Mathf.FloorToInt(animationTime / frameDuration);
        var loops = animationState == PrototypeAnimationState.Idle || animationState == PrototypeAnimationState.Run;
        var frameIndex = loops ? rawFrame % frames.Length : Mathf.Min(rawFrame, frames.Length - 1);
        if (frames[frameIndex] != null)
        {
            body.sprite = frames[frameIndex];
        }

        if (definition != null && definition.HasStaticImportedSprite)
        {
            UpdateStaticSpritePose();
        }
    }

    private void UpdateStaticSpritePose()
    {
        var position = Vector3.zero;
        var angle = 0f;
        var towardEnemy = Team == PrototypeTeam.Allies ? 1f : -1f;
        switch (animationState)
        {
            case PrototypeAnimationState.Idle:
                position.y = Mathf.Sin(animationTime * 4f) * 0.035f;
                break;
            case PrototypeAnimationState.Run:
                position.y = Mathf.Abs(Mathf.Sin(animationTime * 15f)) * 0.07f;
                angle = Mathf.Sin(animationTime * 15f) * 2.5f;
                break;
            case PrototypeAnimationState.Attack:
                position.x = towardEnemy * Mathf.Sin(Mathf.Clamp01(animationTime / BasicActionDuration) * Mathf.PI) * 0.12f;
                break;
            case PrototypeAnimationState.Skill:
            case PrototypeAnimationState.Ultimate:
                position.y = Mathf.Sin(Mathf.Clamp01(animationTime / SkillActionDuration) * Mathf.PI) * 0.09f;
                break;
            case PrototypeAnimationState.Hit:
                position.x = Mathf.Sin(animationTime * 55f) * 0.05f;
                break;
            case PrototypeAnimationState.Death:
                angle = towardEnemy * Mathf.Lerp(0f, 82f, Mathf.Clamp01(animationTime / 0.35f));
                position.y = -Mathf.Lerp(0f, 0.14f, Mathf.Clamp01(animationTime / 0.35f));
                break;
        }

        body.transform.localPosition = position;
        body.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void SetCombatVisualsVisible(bool visible)
    {
        foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.enabled = visible;
        }
    }

    private Color SkillAccent
    {
        get
        {
            switch (SkillKit)
            {
                case PrototypeSkillKit.Nova: return new Color(1f, 0.72f, 0.12f);
                case PrototypeSkillKit.Ion: return new Color(0.2f, 0.9f, 1f);
                case PrototypeSkillKit.Krag: return new Color(0.68f, 0.4f, 1f);
                case PrototypeSkillKit.Vex: return new Color(1f, 0.2f, 0.55f);
                case PrototypeSkillKit.Astra: return new Color(0.22f, 0.88f, 0.9f);
                case PrototypeSkillKit.Lyra: return new Color(1f, 0.9f, 0.2f);
                case PrototypeSkillKit.Brakk: return new Color(0.62f, 0.28f, 1f);
                case PrototypeSkillKit.Hex: return new Color(0.45f, 1f, 0.2f);
                case PrototypeSkillKit.Mira: return new Color(0.15f, 0.85f, 0.9f);
                case PrototypeSkillKit.Drake: return new Color(1f, 0.32f, 0.12f);
                default: return bodyColor;
            }
        }
    }

    private SpriteRenderer CreateSprite(string objectName, Sprite sprite, Color color, Vector3 scale, int sortingOrder)
    {
        var child = new GameObject(objectName);
        child.transform.SetParent(transform, false);
        child.transform.localScale = scale;

        var renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private void CreateHealthBar(Sprite sprite)
    {
        var healthBarY = bodySize * 0.5f + 0.12f;
        var background = CreateSprite(
            "Health Background",
            sprite,
            new Color(0.02f, 0.025f, 0.04f),
            new Vector3(HealthBarWidth + 0.1f, 0.17f, 1f),
            4);
        background.transform.localPosition = new Vector3(0f, healthBarY);

        healthFill = CreateSprite(
            "Health Fill",
            sprite,
            bodyColor,
            new Vector3(HealthBarWidth, 0.09f, 1f),
            5);
        healthFill.transform.localPosition = new Vector3(0f, healthBarY);
    }

    private void CreateBossVisuals()
    {
        var accent = SkillAccent;
        var aura = CreateSprite(
            "Boss Aura",
            characterSprites.Idle[0],
            new Color(accent.r, accent.g, accent.b, 0.22f),
            new Vector3(bodySize * 1.65f, bodySize * 1.65f),
            0);
        aura.flipX = body.flipX;

        var crown = CreateSprite(
            "Boss Crown",
            prototypeSprite,
            accent,
            new Vector3(0.72f, 0.12f, 1f),
            7);
        crown.transform.localPosition = new Vector3(0f, 1.08f);
        crown.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        bossPhaseAura = CreateSprite(
            "Boss Phase Aura",
            characterSprites.Idle[0],
            new Color(1f, 0.18f, 0.08f, 0.28f),
            new Vector3(bodySize * 1.95f, bodySize * 1.95f),
            -1);
        bossPhaseAura.flipX = body.flipX;
        bossPhaseAura.enabled = false;

        bossOrbit = CreateSprite(
            "Boss Orbit",
            prototypeSprite,
            new Color(accent.r, accent.g, accent.b, 0.65f),
            new Vector3(1.9f, 0.08f, 1f),
            6);
        bossOrbit.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
    }

    private void UpdateBossPhaseVisuals()
    {
        if (!IsBoss || bossPhaseAura == null || bossOrbit == null)
        {
            return;
        }

        var secondPhase = IsAlive && currentHealth <= MaxHealth * 0.5f;
        if (secondPhase && !bossPhaseTriggered)
        {
            bossPhaseTriggered = true;
            battle.Announce("KRAG - GRAVITY CORE AWAKENED");
            PrototypeBattleFeedback.PlayImpact(true);
        }

        bossPhaseAura.enabled = secondPhase;
        var pulse = 1f + Mathf.Sin(Time.time * (secondPhase ? 8f : 3f)) * 0.06f;
        bossPhaseAura.transform.localScale = new Vector3(
            bodySize * 1.95f * pulse,
            bodySize * 1.95f * pulse,
            1f);
        bossOrbit.transform.Rotate(0f, 0f, (secondPhase ? 150f : 55f) * Time.deltaTime);
        bossOrbit.color = secondPhase
            ? new Color(1f, 0.24f, 0.12f, 0.85f)
            : new Color(SkillAccent.r, SkillAccent.g, SkillAccent.b, 0.65f);
    }

    private void CreateSkillVisuals(Sprite sprite)
    {
        var healthBarY = bodySize * 0.5f + 0.12f;
        var energyBarY = healthBarY - 0.16f;
        heatAura = CreateSprite(
            "Heat Aura",
            characterSprites.Idle[0],
            new Color(1f, 0.18f, 0.02f, 0.12f),
            new Vector3(bodySize * 1.18f, bodySize * 1.18f),
            0);
        heatAura.flipX = body.flipX;
        heatAura.enabled = false;
        shieldVisual = CreateSprite(
            "Aegis Shield",
            sprite,
            new Color(bodyColor.r, bodyColor.g, bodyColor.b, 0.25f),
            new Vector3(1.55f, 1.55f),
            1);
        shieldVisual.enabled = false;

        markVisual = CreateSprite(
            "Ash Sigil",
            sprite,
            new Color(1f, 0.82f, 0.2f),
            new Vector3(0.18f, 0.18f),
            6);
        markVisual.transform.localPosition = new Vector3(0f, 1.08f);
        markVisual.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        markVisual.enabled = false;

        burnVisual = CreateSprite(
            "Burn Status",
            sprite,
            new Color(1f, 0.28f, 0.05f, 0.82f),
            new Vector3(0.16f, 0.24f, 1f),
            6);
        burnVisual.transform.localPosition = new Vector3(0.42f, 0.92f);
        burnVisual.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        burnVisual.enabled = false;

        ultimateReadyVisual = CreateSprite(
            "Ultimate Ready",
            sprite,
            new Color(SkillAccent.r, SkillAccent.g, SkillAccent.b, 0.9f),
            new Vector3(0.2f, 0.2f, 1f),
            7);
        ultimateReadyVisual.transform.localPosition = new Vector3(-0.46f, bodySize * 0.5f + 0.36f);
        ultimateReadyVisual.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        ultimateReadyVisual.enabled = false;

        var background = CreateSprite(
            "Energy Background",
            sprite,
            new Color(0.02f, 0.025f, 0.04f),
            new Vector3(HealthBarWidth + 0.1f, 0.12f, 1f),
            4);
        background.transform.localPosition = new Vector3(0f, energyBarY);

        energyFill = CreateSprite(
            "Energy Fill",
            sprite,
            bodyColor,
            new Vector3(0f, 0.06f, 1f),
            5);
        energyFill.transform.localPosition = new Vector3(-HealthBarWidth * 0.5f, energyBarY);
    }

    private void CreateStatusIcons()
    {
        var root = new GameObject("Status Icons");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(0f, bodySize * 0.5f + 0.46f);
        var sprites = PrototypeStatusIconArt.Get();
        statusIcons = new SpriteRenderer[sprites.Length];
        statusCounts = new TextMesh[sprites.Length];
        for (var index = 0; index < sprites.Length; index++)
        {
            var icon = new GameObject(sprites[index].name.Replace(" Status", string.Empty));
            icon.transform.SetParent(root.transform, false);
            var renderer = icon.AddComponent<SpriteRenderer>();
            renderer.sprite = sprites[index];
            renderer.sortingOrder = 9;
            renderer.enabled = false;
            statusIcons[index] = renderer;
            var count = new GameObject("Count").AddComponent<TextMesh>();
            count.transform.SetParent(icon.transform, false);
            count.transform.localPosition = new Vector3(0.15f, -0.12f, -0.1f);
            count.characterSize = 0.08f;
            count.fontSize = 42;
            count.anchor = TextAnchor.MiddleCenter;
            count.alignment = TextAlignment.Center;
            count.color = Color.white;
            count.GetComponent<MeshRenderer>().sortingOrder = 10;
            count.gameObject.SetActive(false);
            statusCounts[index] = count;
        }
    }

    private void UpdateStatusIcons()
    {
        if (statusIcons == null)
        {
            return;
        }

        var visibleCount = Mathf.Min(5,
            (stunRemaining > 0f || airborneRemaining > 0f ? 1 : 0) +
            (TotalAshStacks > 0 ? 1 : 0) +
            (HasBurn ? 1 : 0) +
            (groundedRemaining > 0f ? 1 : 0) +
            (heatStacks > 0 ? 1 : 0) +
            (poisonRemaining > 0f ? 1 : 0) +
            (slowRemaining > 0f || CurrentMovementSlow > 0f ? 1 : 0) +
            (attackDebuffRemaining > 0f ? 1 : 0) +
            (tauntRemaining > 0f ? 1 : 0) +
            (currentShield > 0 ? 1 : 0) +
            (hasteRemaining > 0f ? 1 : 0) +
            (attackBuffRemaining > 0f ? 1 : 0));
        var slot = 0;
        SetStatusIcon(1, stunRemaining > 0f || airborneRemaining > 0f,
            Mathf.Max(stunRemaining, airborneRemaining), visibleCount, ref slot);
        SetStatusIcon(9, TotalAshStacks > 0, 5f, visibleCount, ref slot);
        SetStackCount(9, TotalAshStacks);
        SetStatusIcon(2, HasBurn, Mathf.Max(burnRemaining, fireDotMarkerRemaining), visibleCount, ref slot);
        SetStatusIcon(11, groundedRemaining > 0f, groundedRemaining, visibleCount, ref slot);
        SetStatusIcon(10, heatStacks > 0, 99f, visibleCount, ref slot);
        SetStackCount(10, heatStacks);
        SetStatusIcon(3, poisonRemaining > 0f, poisonRemaining, visibleCount, ref slot);
        SetStatusIcon(4, slowRemaining > 0f || CurrentMovementSlow > 0f,
            Mathf.Max(slowRemaining, movementSlowRemaining), visibleCount, ref slot);
        SetStatusIcon(7, attackDebuffRemaining > 0f, attackDebuffRemaining, visibleCount, ref slot);
        SetStatusIcon(8, tauntRemaining > 0f, tauntRemaining, visibleCount, ref slot);
        SetStatusIcon(0, currentShield > 0, shieldRemaining, visibleCount, ref slot);
        SetStatusIcon(5, hasteRemaining > 0f, hasteRemaining, visibleCount, ref slot);
        SetStatusIcon(6, attackBuffRemaining > 0f, attackBuffRemaining, visibleCount, ref slot);
    }

    private void SetStackCount(int iconIndex, int count)
    {
        if (statusCounts == null || iconIndex < 0 || iconIndex >= statusCounts.Length)
        {
            return;
        }

        statusCounts[iconIndex].text = count > 1 ? count.ToString() : string.Empty;
        statusCounts[iconIndex].gameObject.SetActive(IsAlive && count > 0 && statusIcons[iconIndex].enabled);
    }

    private void SetStatusIcon(
        int index,
        bool active,
        float remaining,
        int visibleCount,
        ref int slot)
    {
        var renderer = statusIcons[index];
        if (!IsAlive || !active || slot >= 5)
        {
            renderer.enabled = false;
            if (statusCounts != null) statusCounts[index].gameObject.SetActive(false);
            return;
        }

        renderer.transform.localPosition = new Vector3((slot - (visibleCount - 1) * 0.5f) * 0.38f, 0f);
        renderer.enabled = remaining > 1f || (Mathf.FloorToInt(Time.time * 8f) & 1) == 0;
        slot++;
    }

    private void UpdateHealthBar()
    {
        var ratio = (float)currentHealth / MaxHealth;
        var healthBarY = bodySize * 0.5f + 0.12f;
        healthFill.transform.localScale = new Vector3(HealthBarWidth * ratio, 0.09f, 1f);
        healthFill.transform.localPosition = new Vector3(-HealthBarWidth * (1f - ratio) * 0.5f, healthBarY);
    }

    private void UpdateEnergyBar()
    {
        if (energyFill == null)
        {
            return;
        }

        var ratio = energy / MaxEnergy;
        var energyBarY = bodySize * 0.5f - 0.04f;
        energyFill.transform.localScale = new Vector3(HealthBarWidth * ratio, 0.06f, 1f);
        energyFill.transform.localPosition = new Vector3(-HealthBarWidth * (1f - ratio) * 0.5f, energyBarY);
        if (ultimateReadyVisual != null)
        {
            ultimateReadyVisual.enabled = IsAlive && ratio >= 0.85f;
        }
    }

    private float ActiveSkillInterval
    {
        get
        {
            if (skills != null && skills.Active != null && skills.Active.Cooldown > 0f)
            {
                return skills.Active.Cooldown;
            }

            switch (SkillKit)
            {
                case PrototypeSkillKit.Vex: return 5f;
                case PrototypeSkillKit.Nova:
                case PrototypeSkillKit.Astra:
                case PrototypeSkillKit.Lyra:
                case PrototypeSkillKit.Nyx:
                    return 6f;
                case PrototypeSkillKit.Rook:
                case PrototypeSkillKit.Hex:
                    return 6.5f;
                default:
                    return 7f;
            }
        }
    }

    private static float SkillDuration(PrototypeSkillData skill, float fallback)
    {
        return skill != null && skill.ActionDuration > 0f ? skill.ActionDuration : fallback;
    }

    private static float ConfiguredHitDelay(PrototypeSkillData skill, float fallback)
    {
        return skill != null
            ? skill.ActionDuration * Mathf.Clamp(skill.HitFrame, 0.05f, 0.95f)
            : fallback;
    }

    private static float SkillPower(PrototypeSkillData skill, float fallback)
    {
        return skill != null && skill.PowerMultiplier > 0f ? skill.PowerMultiplier : fallback;
    }

    private float EffectiveAttackInterval
    {
        get
        {
            var interval = AttackInterval;
            if (hasteRemaining > 0f)
            {
                interval *= 0.65f;
            }
            if (HasAureliaAegis)
            {
                interval *= 0.8f;
            }

            if (slowRemaining > 0f)
            {
                interval *= 1.25f;
            }

            return interval;
        }
    }
}

internal sealed class PrototypeBattleFeedback : MonoBehaviour
{
    private const int SampleRate = 22050;
    private static PrototypeBattleFeedback instance;

    private AudioSource audioSource;
    private AudioSource musicSource;
    private AudioClip hitClip;
    private AudioClip skillClip;
    private AudioClip ultimateClip;
    private AudioClip battleLoopClip;
    private Vector3 restingPosition;
    private float shakeRemaining;
    private float shakeStrength;
    private float audioCooldown;

    public static void PlayImpact(bool strong)
    {
        var feedback = Get();
        if (feedback == null)
        {
            return;
        }

        feedback.shakeRemaining = Mathf.Max(feedback.shakeRemaining, strong ? 0.12f : 0.06f);
        feedback.shakeStrength = Mathf.Max(feedback.shakeStrength, strong ? 0.08f : 0.025f);
        if (feedback.audioCooldown <= 0f)
        {
            feedback.audioSource.pitch = 1f;
            feedback.audioSource.PlayOneShot(feedback.hitClip, strong ? 0.35f : 0.16f);
            feedback.audioCooldown = 0.045f;
        }
    }

    public static void StartBattleLoop()
    {
        var feedback = Get();
        if (feedback != null && !feedback.musicSource.isPlaying)
        {
            feedback.musicSource.Play();
        }
    }

    public static void PlayAction(PrototypeSkillKit kit, bool ultimate)
    {
        var feedback = Get();
        if (feedback == null)
        {
            return;
        }

        feedback.audioSource.pitch = ActionPitch(kit, ultimate);
        feedback.audioSource.PlayOneShot(ultimate ? feedback.ultimateClip : feedback.skillClip, 0.42f);
        if (ultimate)
        {
            feedback.shakeRemaining = Mathf.Max(feedback.shakeRemaining, 0.18f);
            feedback.shakeStrength = Mathf.Max(feedback.shakeStrength, 0.06f);
        }
    }

    private static float ActionPitch(PrototypeSkillKit kit, bool ultimate)
    {
        var pitch = kit == PrototypeSkillKit.Nova ? 1.08f
            : kit == PrototypeSkillKit.Ion ? 1.28f
            : kit == PrototypeSkillKit.Astra ? 0.92f
            : kit == PrototypeSkillKit.Lyra ? 1.36f
            : kit == PrototypeSkillKit.Brakk ? 0.82f
            : kit == PrototypeSkillKit.Krag ? 0.7f
            : 1f;
        return ultimate ? pitch * 0.88f : pitch;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance()
    {
        instance = null;
    }

    private static PrototypeBattleFeedback Get()
    {
        if (instance != null)
        {
            instance.EnsureInitialized();
            return instance;
        }

        var camera = Camera.main;
        if (camera == null)
        {
            return null;
        }

        instance = camera.GetComponent<PrototypeBattleFeedback>();
        if (instance == null)
        {
            instance = camera.gameObject.AddComponent<PrototypeBattleFeedback>();
        }
        instance.EnsureInitialized();
        return instance;
    }

    private void Awake()
    {
        instance = this;
        restingPosition = transform.localPosition;
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume = 0.08f;
        }

        if (hitClip == null) hitClip = CreateTone("Pixel Hit", 150f, 0.045f);
        if (skillClip == null) skillClip = CreateTone("Pixel Skill", 420f, 0.12f);
        if (ultimateClip == null) ultimateClip = CreateTone("Pixel Ultimate", 220f, 0.22f);
        if (battleLoopClip == null)
        {
            battleLoopClip = CreateBattleLoop();
            musicSource.clip = battleLoopClip;
        }
    }

    private void Update()
    {
        audioCooldown = Mathf.Max(0f, audioCooldown - Time.unscaledDeltaTime);
        if (shakeRemaining <= 0f)
        {
            transform.localPosition = restingPosition;
            return;
        }

        shakeRemaining -= Time.unscaledDeltaTime;
        var offset = Random.insideUnitCircle * shakeStrength;
        const float pixelUnit = 1f / 64f;
        offset.x = Mathf.Round(offset.x / pixelUnit) * pixelUnit;
        offset.y = Mathf.Round(offset.y / pixelUnit) * pixelUnit;
        transform.localPosition = restingPosition + new Vector3(offset.x, offset.y, 0f);
        shakeStrength = Mathf.MoveTowards(shakeStrength, 0f, Time.unscaledDeltaTime * 0.7f);
    }

    private void OnDisable()
    {
        transform.localPosition = restingPosition;
    }

    private static AudioClip CreateTone(string name, float frequency, float duration)
    {
        var sampleCount = Mathf.CeilToInt(SampleRate * duration);
        var samples = new float[sampleCount];
        for (var index = 0; index < sampleCount; index++)
        {
            var envelope = 1f - (float)index / sampleCount;
            samples[index] = Mathf.Sin(2f * Mathf.PI * frequency * index / SampleRate) * envelope * 0.35f;
        }

        var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateBattleLoop()
    {
        const float duration = 2f;
        var sampleCount = Mathf.CeilToInt(SampleRate * duration);
        var samples = new float[sampleCount];
        var notes = new[] { 220f, 277.18f, 329.63f, 277.18f };
        for (var index = 0; index < sampleCount; index++)
        {
            var time = (float)index / SampleRate;
            var note = notes[Mathf.FloorToInt(time * 4f) % notes.Length];
            var pulse = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * note * time)) * 0.035f;
            var bass = Mathf.Sin(2f * Mathf.PI * 55f * time) * 0.04f;
            samples[index] = pulse + bass;
        }

        var clip = AudioClip.Create("Pixel Battle Loop", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}

internal sealed class PrototypeEffect : MonoBehaviour
{
    private SpriteRenderer[] layers;
    private float startScale;
    private float endScale;
    private float duration;
    private float elapsed;
    private float rotationSpeed = 120f;

    public static void Spawn(Sprite sprite, Vector3 position, Color color, float start, float end, float lifetime)
    {
        var gameObject = new GameObject("Skill Effect");
        gameObject.transform.position = position;
        gameObject.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        var effect = gameObject.AddComponent<PrototypeEffect>();
        effect.layers = new SpriteRenderer[5];
        effect.layers[0] = AddEffectLayer(gameObject.transform, "Core", sprite, color, Vector3.zero, new Vector3(0.5f, 0.5f), 109);
        effect.layers[1] = AddEffectLayer(gameObject.transform, "Ray Up", sprite, color, new Vector3(0f, 0.55f), new Vector3(0.12f, 0.7f), 108);
        effect.layers[2] = AddEffectLayer(gameObject.transform, "Ray Down", sprite, color, new Vector3(0f, -0.55f), new Vector3(0.12f, 0.7f), 108);
        effect.layers[3] = AddEffectLayer(gameObject.transform, "Ray Left", sprite, color, new Vector3(-0.55f, 0f), new Vector3(0.7f, 0.12f), 108);
        effect.layers[4] = AddEffectLayer(gameObject.transform, "Ray Right", sprite, color, new Vector3(0.55f, 0f), new Vector3(0.7f, 0.12f), 108);
        effect.startScale = start;
        effect.endScale = end;
        effect.duration = lifetime;
        gameObject.transform.localScale = new Vector3(start, start, 1f);
    }

    public static void SpawnLine(
        Sprite sprite,
        Vector3 start,
        Vector3 end,
        Color color,
        float lifetime,
        float thickness)
    {
        var delta = end - start;
        if (delta.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        var gameObject = new GameObject("Skill Line");
        gameObject.transform.position = (start + end) * 0.5f;
        gameObject.transform.rotation = Quaternion.Euler(
            0f,
            0f,
            Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        var effect = gameObject.AddComponent<PrototypeEffect>();
        effect.layers = new[]
        {
            AddEffectLayer(
                gameObject.transform,
                "Beam",
                sprite,
                color,
                Vector3.zero,
                new Vector3(delta.magnitude, thickness, 1f),
                110)
        };
        effect.startScale = 1f;
        effect.endScale = 1f;
        effect.duration = lifetime;
        effect.rotationSpeed = 0f;
    }

    public static void SpawnBarrier(Sprite sprite, Vector3 position, Color color, float lifetime)
    {
        var gameObject = new GameObject("Skill Barrier");
        gameObject.transform.position = position;
        var effect = gameObject.AddComponent<PrototypeEffect>();
        effect.layers = new SpriteRenderer[4];
        effect.layers[0] = AddEffectLayer(gameObject.transform, "Top", sprite, color, new Vector3(0f, 0.7f), new Vector3(1.35f, 0.08f), 108);
        effect.layers[1] = AddEffectLayer(gameObject.transform, "Bottom", sprite, color, new Vector3(0f, -0.7f), new Vector3(1.35f, 0.08f), 108);
        effect.layers[2] = AddEffectLayer(gameObject.transform, "Left", sprite, color, new Vector3(-0.7f, 0f), new Vector3(0.08f, 1.35f), 108);
        effect.layers[3] = AddEffectLayer(gameObject.transform, "Right", sprite, color, new Vector3(0.7f, 0f), new Vector3(0.08f, 1.35f), 108);
        effect.startScale = 0.72f;
        effect.endScale = 1.08f;
        effect.duration = lifetime;
        effect.rotationSpeed = 0f;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        var progress = Mathf.Clamp01(elapsed / duration);
        var scale = Mathf.Lerp(startScale, endScale, progress);
        transform.localScale = new Vector3(scale, scale, 1f);
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        foreach (var layer in layers)
        {
            var color = layer.color;
            color.a = Mathf.Lerp(0.65f, 0f, progress);
            layer.color = color;
        }

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private static SpriteRenderer AddEffectLayer(
        Transform parent,
        string name,
        Sprite sprite,
        Color color,
        Vector3 position,
        Vector3 scale,
        int sortingOrder)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = position;
        gameObject.transform.localScale = scale;
        var renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(color.r, color.g, color.b, 0.65f);
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }
}

internal sealed class PrototypeProjectile : MonoBehaviour
{
    private Vector3 start;
    private Vector3 end;
    private float duration;
    private float elapsed;
    private float arcHeight;

    public static void Spawn(Sprite sprite, Vector3 start, Vector3 end, Color color, float duration)
    {
        Create(sprite, start, end, color, duration, 0f);
    }

    public static void SpawnArc(
        Sprite sprite,
        Vector3 start,
        Vector3 end,
        Color color,
        float duration,
        float arcHeight)
    {
        Create(sprite, start, end, color, duration, arcHeight);
    }

    private static void Create(
        Sprite sprite,
        Vector3 start,
        Vector3 end,
        Color color,
        float duration,
        float arcHeight)
    {
        var gameObject = new GameObject("Combat Projectile");
        gameObject.transform.position = start;
        var delta = end - start;
        gameObject.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        var projectile = gameObject.AddComponent<PrototypeProjectile>();
        projectile.start = start;
        projectile.end = end;
        projectile.duration = Mathf.Max(0.05f, duration);
        projectile.arcHeight = arcHeight;
        AddLayer(gameObject.transform, "Trail", sprite, new Color(color.r, color.g, color.b, 0.35f), new Vector3(-0.18f, 0f), new Vector3(0.38f, 0.08f), 111);
        AddLayer(gameObject.transform, "Core", sprite, color, Vector3.zero, new Vector3(0.14f, 0.14f), 112);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        var progress = Mathf.Clamp01(elapsed / duration);
        var position = Vector3.Lerp(start, end, progress);
        position.y += Mathf.Sin(progress * Mathf.PI) * arcHeight;
        transform.position = position;
        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private static SpriteRenderer AddLayer(
        Transform parent,
        string name,
        Sprite sprite,
        Color color,
        Vector3 position,
        Vector3 scale,
        int order)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = position;
        child.transform.localScale = scale;
        var renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return renderer;
    }
}

internal sealed class PrototypeAfterimage : MonoBehaviour
{
    private const float Lifetime = 0.18f;
    private SpriteRenderer spriteRenderer;
    private float elapsed;

    public static void Spawn(SpriteRenderer source, int sortingOrder, Color accent)
    {
        if (source == null || source.sprite == null)
        {
            return;
        }

        var gameObject = new GameObject("Combat Afterimage");
        gameObject.transform.position = source.transform.position;
        gameObject.transform.rotation = source.transform.rotation;
        gameObject.transform.localScale = source.transform.lossyScale;
        var afterimage = gameObject.AddComponent<PrototypeAfterimage>();
        afterimage.spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        afterimage.spriteRenderer.sprite = source.sprite;
        afterimage.spriteRenderer.flipX = source.flipX;
        afterimage.spriteRenderer.color = new Color(accent.r, accent.g, accent.b, 0.22f);
        afterimage.spriteRenderer.sortingOrder = sortingOrder;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        var progress = Mathf.Clamp01(elapsed / Lifetime);
        var color = spriteRenderer.color;
        color.a = Mathf.Lerp(0.22f, 0f, progress);
        spriteRenderer.color = color;
        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }
}

internal sealed class PrototypeDamageNumber : MonoBehaviour
{
    private const float Lifetime = 0.65f;
    private TextMesh textMesh;
    private float elapsed;

    public static void Spawn(Vector3 position, int damage, Color color)
    {
        SpawnText(position, damage.ToString(), color);
    }

    public static void SpawnText(Vector3 position, string value, Color color)
    {
        var gameObject = new GameObject("Damage Number");
        gameObject.transform.position = position;
        var number = gameObject.AddComponent<PrototypeDamageNumber>();
        number.textMesh = gameObject.AddComponent<TextMesh>();
        number.textMesh.text = value;
        number.textMesh.anchor = TextAnchor.MiddleCenter;
        number.textMesh.alignment = TextAlignment.Center;
        number.textMesh.fontSize = 40;
        number.textMesh.characterSize = 0.07f;
        number.textMesh.color = color;
        number.textMesh.fontStyle = FontStyle.Bold;
        gameObject.GetComponent<MeshRenderer>().sortingOrder = 120;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        transform.position += Vector3.up * (0.8f * Time.deltaTime);
        var color = textMesh.color;
        color.a = 1f - Mathf.Clamp01(elapsed / Lifetime);
        textMesh.color = color;
        if (elapsed >= Lifetime)
        {
            Destroy(gameObject);
        }
    }
}

internal sealed class PrototypeHud : MonoBehaviour
{
    private PrototypeBattle battle;
    private PrototypeCombatant selectedUnit;
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle detailStyle;
    private GUIStyle resultStyle;
    private bool resultProcessed;
    private bool transitioning;
    private bool showActivities;
    private bool showSquadSetup;
    private bool showCharacterScreen;
    private bool showGachaScreen;
    private bool canvasOverlayOpen;
    private bool nextIdleFarmMode;
    private PrototypeDungeonType selectedDungeonType;
    private int selectedDungeonLevel = 1;
    private float transitionDelay;
    private string resultMessage;

    public void Initialize(PrototypeBattle battleManager)
    {
        battle = battleManager;
        selectedUnit = battle.Allies[0];
        PrototypeGameFlow.Instance.BindBattle(this, battle);
    }

    internal PrototypeBattle Battle => battle;
    internal string ResultMessage => resultMessage;

    internal void SetCanvasOverlayOpen(bool open)
    {
        canvasOverlayOpen = open;
    }

    internal void ApplySquad(CombatantDefinition[] selected, PrototypeFormationRow[] rows)
    {
        transitioning = true;
        PrototypeSession.Configure(
            PrototypeGameMode.Idle,
            PrototypeDungeonType.Credits,
            1,
            PrototypeSession.IdleChallengePending,
            selected,
            rows);
        CombatPrototype.RestartIdleBattle(gameObject, PrototypeSession.IdleChallengePending);
    }

    internal void StartActivity(
        PrototypeGameMode mode,
        PrototypeDungeonType dungeonType = PrototypeDungeonType.Credits,
        int dungeonLevel = 1)
    {
        transitioning = true;
        CombatPrototype.RestartActivity(gameObject, mode, dungeonType, dungeonLevel);
    }

    internal void ReturnToIdleBattle()
    {
        transitioning = true;
        CombatPrototype.RestartIdleBattle(gameObject, PrototypeSession.IdleChallengePending);
    }

    internal void ChallengePendingStage()
    {
        transitioning = true;
        CombatPrototype.RestartIdleBattle(gameObject, false);
    }

    private void Update()
    {
        if (battle == null || !battle.IsFinished || transitioning)
        {
            return;
        }

        if (!resultProcessed)
        {
            ProcessResult();
        }

        if (battle.Mode == PrototypeGameMode.Idle &&
            !showSquadSetup && !showCharacterScreen && !showGachaScreen && !canvasOverlayOpen)
        {
            transitionDelay -= Time.deltaTime;
            if (transitionDelay <= 0f)
            {
                ContinueIdle();
            }
        }
    }

    private void OnGUI()
    {
        if (PrototypeGameFlow.HasInstance)
        {
            return;
        }

        if (battle == null || showSquadSetup || showCharacterScreen || showGachaScreen)
        {
            return;
        }

        CreateStyles();
        var previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 360f, Screen.height / 640f, 1f));

        GUI.Label(new Rect(20f, 18f, 320f, 30f), GetBattleTitle(), titleStyle);
        DrawTeam(new Rect(12f, 56f, 164f, 134f), "ALLIES", battle.Allies, PrototypeTeam.Allies);
        DrawTeam(new Rect(184f, 56f, 164f, 134f), "ENEMIES", battle.Enemies, PrototypeTeam.Enemies);
        var timer = battle.TimeLimit > 0f ? $"{battle.RemainingTime:0.0}s" : $"{battle.ElapsedTime:0.0}s";
        GUI.Label(new Rect(130f, 196f, 100f, 24f), timer, labelStyle);

        if (battle.HasAnnouncement)
        {
            GUI.Label(new Rect(30f, 224f, 300f, 34f), battle.Announcement, titleStyle);
        }

        if (battle.IsFinished)
        {
            if (!resultProcessed)
            {
                ProcessResult();
            }

            if (battle.Mode != PrototypeGameMode.Idle)
            {
                GUI.Label(
                    new Rect(30f, 274f, 300f, 48f),
                    battle.Winner == PrototypeTeam.Allies ? "VICTORY" : "DEFEAT",
                    resultStyle);
                GUI.Label(new Rect(24f, 306f, 312f, 20f), resultMessage, labelStyle);
                DrawDamageReport();
                if (GUI.Button(new Rect(100f, 548f, 160f, 42f), "RETURN TO IDLE"))
                {
                    transitioning = true;
                    CombatPrototype.RestartIdleBattle(gameObject, PrototypeSession.IdleChallengePending);
                }
            }
        }
        else
        {
            if (battle.Mode == PrototypeGameMode.Idle)
            {
                DrawIdleNavigation();
            }
            else
            {
                DrawSecondaryModeNavigation();
            }

            if (battle.Mode == PrototypeGameMode.Idle && !showActivities &&
                battle.IsIdleFarmMode &&
                GUI.Button(
                    new Rect(80f, 410f, 200f, 36f),
                    $"CHALLENGE STAGE {PrototypeSession.IdleStage}"))
            {
                transitioning = true;
                CombatPrototype.RestartIdleBattle(gameObject, false);
            }
            if (!showActivities)
            {
                DrawSkillDetails();
            }
        }

        GUI.matrix = previousMatrix;
    }

    private void OnDestroy()
    {
        if (PrototypeGameFlow.HasInstance)
        {
            PrototypeGameFlow.Instance.UnbindBattle(this);
        }
    }

    private void DrawIdleNavigation()
    {
        if (GUI.Button(new Rect(4f, 600f, 64f, 32f), "SQUAD"))
        {
            showSquadSetup = true;
            var ownedRoster = PrototypeGacha.GetOwnedRoster(CombatPrototype.LoadRoster());
            CombatantDefinition[] configuredSquad;
            PrototypeFormationRow[] configuredRows;
            if (!PrototypeSession.TryGetSquad(ownedRoster, out configuredSquad, out configuredRows))
            {
                configuredSquad = new CombatantDefinition[5];
                System.Array.Copy(ownedRoster, configuredSquad, 5);
                configuredRows = CombatPrototype.GetDefaultRows(configuredSquad);
            }
            gameObject.AddComponent<PrototypeSquadSetup>().Initialize(
                ownedRoster,
                configuredSquad,
                configuredRows,
                (selected, rows) =>
                {
                    PrototypeSession.Configure(
                        PrototypeGameMode.Idle,
                        PrototypeDungeonType.Credits,
                        1,
                        PrototypeSession.IdleChallengePending,
                        selected,
                        rows);
                    CombatPrototype.RestartIdleBattle(gameObject, PrototypeSession.IdleChallengePending);
                },
                () => showSquadSetup = false);
            return;
        }

        if (GUI.Button(new Rect(74f, 600f, 64f, 32f), "HEROES"))
        {
            showCharacterScreen = true;
            gameObject.AddComponent<PrototypeCharacterScreen>().Initialize(
                PrototypeGacha.GetOwnedRoster(CombatPrototype.LoadRoster()),
                () => showCharacterScreen = false);
            return;
        }

        if (GUI.Button(new Rect(144f, 600f, 64f, 32f), "SUMMON"))
        {
            showGachaScreen = true;
            gameObject.AddComponent<PrototypeGachaScreen>().Initialize(
                CombatPrototype.LoadRoster(),
                () => showGachaScreen = false);
            return;
        }

        var idleGold = PrototypeSession.GetUnclaimedIdleGold();
        var idleExperience = PrototypeSession.GetUnclaimedIdleExperience();
        if (GUI.Button(new Rect(214f, 600f, 64f, 32f), "CLAIM"))
        {
            PrototypeSession.ClaimIdleRewards(out idleGold, out idleExperience);
            battle.Announce($"IDLE · +{idleGold} GOLD · +{idleExperience} XP");
        }

        if (GUI.Button(new Rect(284f, 600f, 64f, 32f), showActivities ? "CLOSE" : "MODES"))
        {
            showActivities = !showActivities;
        }

        if (!showActivities)
        {
            return;
        }

        GUI.Box(new Rect(24f, 414f, 312f, 174f), "ACTIVITIES · IDLE REWARDS KEEP ACCUMULATING", labelStyle);
        if (GUI.Button(new Rect(34f, 448f, 116f, 32f), selectedDungeonType.ToString()))
        {
            selectedDungeonType = (PrototypeDungeonType)(((int)selectedDungeonType + 1) % 3);
        }
        if (GUI.Button(new Rect(160f, 448f, 32f, 32f), "-"))
        {
            selectedDungeonLevel = Mathf.Max(1, selectedDungeonLevel - 1);
        }
        GUI.Label(new Rect(194f, 448f, 56f, 32f), $"Lv {selectedDungeonLevel}", labelStyle);
        if (GUI.Button(new Rect(252f, 448f, 32f, 32f), "+"))
        {
            selectedDungeonLevel = Mathf.Min(99, selectedDungeonLevel + 1);
        }
        GUI.Label(
            new Rect(34f, 486f, 268f, 28f),
            $"Reward {PrototypeSession.GetDungeonReward(selectedDungeonType, selectedDungeonLevel)} {selectedDungeonType}",
            labelStyle);
        if (GUI.Button(new Rect(34f, 524f, 116f, 40f), "DUNGEON"))
        {
            transitioning = true;
            CombatPrototype.RestartActivity(
                gameObject,
                PrototypeGameMode.Dungeon,
                selectedDungeonType,
                selectedDungeonLevel);
        }
        if (GUI.Button(new Rect(186f, 524f, 116f, 40f), "PVP 5v5"))
        {
            transitioning = true;
            CombatPrototype.RestartActivity(gameObject, PrototypeGameMode.PvP);
        }
    }

    private void DrawSecondaryModeNavigation()
    {
        var idleGold = PrototypeSession.GetUnclaimedIdleGold();
        var idleExperience = PrototypeSession.GetUnclaimedIdleExperience();
        if (GUI.Button(
            new Rect(12f, 600f, 164f, 32f),
            $"CLAIM {idleGold}G + {idleExperience}XP"))
        {
            PrototypeSession.ClaimIdleRewards(out idleGold, out idleExperience);
            battle.Announce($"IDLE · +{idleGold} GOLD · +{idleExperience} XP");
        }

        if (GUI.Button(new Rect(184f, 600f, 164f, 32f), "RETURN TO IDLE"))
        {
            transitioning = true;
            CombatPrototype.RestartIdleBattle(gameObject, PrototypeSession.IdleChallengePending);
        }
    }

    private string GetBattleTitle()
    {
        switch (battle.Mode)
        {
            case PrototypeGameMode.Idle:
                return battle.IsBossStage
                    ? $"IDLE STAGE {battle.Stage} · BOSS"
                    : battle.IsIdleFarmMode
                        ? $"IDLE STAGE {battle.Stage} · FARM WAVE {battle.FarmWave}"
                        : $"IDLE STAGE {battle.Stage}";
            case PrototypeGameMode.Dungeon:
                return $"{battle.DungeonType} DUNGEON · LV {battle.DungeonLevel}";
            default:
                return "PVP ARENA · 5v5";
        }
    }

    private void ProcessResult()
    {
        resultProcessed = true;
        if (battle.Mode == PrototypeGameMode.Idle)
        {
            var victory = battle.Winner == PrototypeTeam.Allies;
            var goldReward = victory ? PrototypeContentCatalog.GetStageGold(battle.Stage) : 0;
            var experienceReward = victory ? PrototypeContentCatalog.GetStageExperience(battle.Stage) : 0;
            var ticketReward = victory ? PrototypeContentCatalog.GetStageTickets(battle.Stage) : 0;
            if (victory)
            {
                PrototypeSession.AddIdleRewards(goldReward, experienceReward);
                if (ticketReward > 0)
                {
                    PrototypeGacha.AddTickets(ticketReward);
                }
            }

            if (victory)
            {
                foreach (var ally in battle.Allies)
                {
                    PrototypeProgression.AddExperience(ally.DisplayName, 8 + battle.Stage * 2);
                    if (battle.IsBossStage)
                    {
                        PrototypeProgression.AddShards(ally.DisplayName, 3);
                    }
                }
            }

            if (!battle.IsIdleFarmMode)
            {
                if (victory)
                {
                    PrototypeSession.SetIdleStage(battle.Stage + 1);
                    PrototypeSession.SetChallengePending(false);
                }
                else
                {
                    PrototypeSession.SetChallengePending(true);
                }
            }

            nextIdleFarmMode = battle.IsIdleFarmMode || !victory;
            resultMessage = victory
                ? $"+{goldReward} Gold · +{experienceReward} XP" +
                    (ticketReward > 0 ? $" · +{ticketReward} Tickets" : string.Empty) + " · continuing"
                : $"Stage failed · farming stage {Mathf.Max(1, battle.Stage - 1)}";
            transitionDelay = victory ? 0.2f : 0.8f;
            return;
        }

        if (battle.Mode == PrototypeGameMode.Dungeon)
        {
            if (battle.Winner == PrototypeTeam.Allies)
            {
                var reward = PrototypeSession.GetDungeonReward(battle.DungeonType, battle.DungeonLevel);
                PrototypeSession.AddReward(battle.DungeonType, reward);
                foreach (var ally in battle.Allies)
                {
                    PrototypeProgression.AddExperience(ally.DisplayName, 15 * battle.DungeonLevel);
                }
                resultMessage = $"Reward: +{reward} {battle.DungeonType}";
            }
            else
            {
                resultMessage = "Time expired or squad defeated · no reward";
            }
            return;
        }

        var pvpVictory = battle.Winner == PrototypeTeam.Allies;
        if (pvpVictory)
        {
            foreach (var ally in battle.Allies)
            {
                PrototypeProgression.AddExperience(ally.DisplayName, 10);
            }
        }
        resultMessage = pvpVictory
            ? "PvP victory · +10 hero XP · ranking comes later"
            : "PvP defeat · ranking and server rewards come later";
    }

    private void ContinueIdle()
    {
        if (transitioning)
        {
            return;
        }

        transitioning = true;
        CombatPrototype.RestartIdleBattle(gameObject, nextIdleFarmMode);
    }

    private void DrawTeam(Rect area, string title, PrototypeCombatant[] units, PrototypeTeam team)
    {
        GUI.Box(area, $"{title}  {battle.LivingCount(team)}/{units.Length}", labelStyle);
        for (var index = 0; index < units.Length; index++)
        {
            var unit = units[index];
            var status = unit.IsAlive ? $"{unit.CurrentHealth}/{unit.MaxHealth}" : "KO";
            if (unit.IsAlive && unit.SkillKit != PrototypeSkillKit.None)
            {
                status += $"  E{Mathf.RoundToInt(unit.Energy)}";
            }

            if (GUI.Button(
                new Rect(area.x + 6f, area.y + 25f + index * 20f, area.width - 12f, 18f),
                $"{unit.DisplayName}  {status}"))
            {
                selectedUnit = unit;
            }
        }
    }

    private void DrawSkillDetails()
    {
        if (selectedUnit == null)
        {
            return;
        }

        var cooldown = selectedUnit.ActiveSkillCooldown <= 0f
            ? "READY"
            : $"{selectedUnit.ActiveSkillCooldown:0.0}s";
        GUI.Box(new Rect(12f, 446f, 336f, 142f), string.Empty);
        GUI.Label(
            new Rect(20f, 450f, 320f, 22f),
            $"{selectedUnit.DisplayName} · {selectedUnit.Species} {selectedUnit.CombatClass} · {selectedUnit.FormationRow}",
            titleStyle);
        GUI.Label(
            new Rect(20f, 474f, 320f, 20f),
            $"Active: {cooldown}   Ultimate: {Mathf.RoundToInt(selectedUnit.Energy)}/100   Damage: {selectedUnit.DamageDealt}",
            labelStyle);
        GUI.Label(
            new Rect(20f, 494f, 320f, 18f),
            $"Lv{selectedUnit.Level} {selectedUnit.Stars}* · Skills B{selectedUnit.BasicSkillLevel}/P{selectedUnit.PassiveSkillLevel}/A{selectedUnit.ActiveSkillLevel}/U{selectedUnit.UltimateSkillLevel}",
            labelStyle);
        GUI.Label(new Rect(20f, 512f, 320f, 18f), $"Status: {selectedUnit.StatusSummary}", labelStyle);
        GUI.Label(new Rect(20f, 532f, 320f, 50f), selectedUnit.SkillDescription, detailStyle);
    }

    private void DrawDamageReport()
    {
        DrawDamageTeam(new Rect(12f, 326f, 164f, 194f), "ALLY DAMAGE", battle.Allies);
        DrawDamageTeam(new Rect(184f, 326f, 164f, 194f), "ENEMY DAMAGE", battle.Enemies);
    }

    private void DrawDamageTeam(Rect area, string title, PrototypeCombatant[] units)
    {
        GUI.Box(area, title, labelStyle);
        for (var index = 0; index < units.Length; index++)
        {
            GUI.Label(
                new Rect(area.x + 8f, area.y + 28f + index * 29f, area.width - 16f, 24f),
                $"{units[index].DisplayName}  {units[index].DamageDealt}",
                labelStyle);
        }
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
            fontSize = 19,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = new Color(0.42f, 0.9f, 1f);

        labelStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11
        };
        labelStyle.normal.textColor = Color.white;

        detailStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 10,
            wordWrap = true
        };
        detailStyle.normal.textColor = Color.white;

        resultStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 30,
            fontStyle = FontStyle.Bold
        };
        resultStyle.normal.textColor = new Color(1f, 0.82f, 0.2f);
    }
}
