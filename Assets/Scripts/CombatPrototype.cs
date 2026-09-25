using System.Collections.Generic;
using UnityEngine;
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
    private const float FormationSpacing = 1.55f;
    private static Sprite squareSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (SceneManager.GetActiveScene().name != "SampleScene" ||
            Object.FindFirstObjectByType<PrototypeBattle>() != null ||
            Object.FindFirstObjectByType<PrototypeSquadSetup>() != null ||
            Object.FindFirstObjectByType<PrototypeCharacterScreen>() != null ||
            Object.FindFirstObjectByType<PrototypeGachaScreen>() != null)
        {
            return;
        }

        PrototypeSaveSystem.Migrate();
        var allyDefinitions = LoadDefinitions("Combatants/Allies");
        var enemyDefinitions = LoadDefinitions("Combatants/Enemies");
        PrototypeSession.EnsureIdleClock();
        if (allyDefinitions.Length == 0 || enemyDefinitions.Length == 0)
        {
            Debug.LogError("Combat prototype needs combatant definitions in Resources/Combatants.");
            return;
        }

        var roster = LoadRoster();
        var ownedRoster = PrototypeGacha.GetOwnedRoster(roster);
        var root = new GameObject("Combat Prototype");
        CombatantDefinition[] pendingSquad;
        PrototypeFormationRow[] pendingRows;
        if (PrototypeSession.TryConsumeBattle(ownedRoster, out pendingSquad, out pendingRows))
        {
            StartBattle(root.transform, pendingSquad, pendingRows, enemyDefinitions);
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
                    StartBattle(root.transform, selected, rows, enemyDefinitions);
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
            StartBattle(root.transform, defaultSquad, defaultRows, enemyDefinitions);
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
        PrototypeFormationRow[] allyRows,
        CombatantDefinition[] enemyDefinitions)
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
        root.gameObject.AddComponent<PrototypeHud>().Initialize(battle);

        Debug.Assert(allies.Length == 5 && enemies.Length == 5, "Battle must start as 5v5.");
        Debug.Assert(battle.FindClosestEnemy(allies[0]) != null, "Each team needs a valid target.");
        Debug.Assert(PrototypeCombatClassRules.GetAttackRange(PrototypeCombatClass.Archer) >
            PrototypeCombatClassRules.GetAttackRange(PrototypeCombatClass.Assassin),
            "Ranged classes must attack from farther away than melee classes.");
        Debug.Assert(!string.IsNullOrEmpty(allies[0].SkillDescription), "Each hero needs skill descriptions.");
        Debug.Assert(PrototypeCombatant.CalculateDamage(20, 5, 1f, true) == 20, "Marked damage formula is invalid.");
        Debug.Assert(PrototypeCombatant.CalculateDamage(20, 10, 1f, false, 0.5f) == 15, "Armor pierce formula is invalid.");
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
        var enemyDefinitions = LoadDefinitions("Combatants/Enemies");
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
        StartBattle(root.transform, squad, rows, enemyDefinitions);
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
        }

        return units;
    }

    private static void CreateArena(Transform parent)
    {
        CreateRectangle(parent, "Space", Vector3.zero, new Vector3(11f, 22f), new Color(0.035f, 0.06f, 0.13f), -20);
        CreateRectangle(parent, "Battle Lane", Vector3.zero, new Vector3(9f, 8f), new Color(0.07f, 0.12f, 0.22f), -10);
        CreateRectangle(parent, "Center Line", Vector3.zero, new Vector3(0.06f, 7.6f), new Color(0.2f, 0.75f, 0.9f, 0.45f), -5);
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
    }

    public PrototypeCombatant FindClosestEnemy(PrototypeCombatant source)
    {
        return FindClosestEnemy(source, null);
    }

    public PrototypeCombatant FindTarget(PrototypeCombatant source)
    {
        var preferredRow = source.CombatClass == PrototypeCombatClass.Assassin
            ? PrototypeFormationRow.Back
            : PrototypeFormationRow.Front;
        return FindClosestEnemy(source, preferredRow) ?? FindClosestEnemy(source);
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

internal sealed class PrototypeCombatant : MonoBehaviour
{
    private const float MaxEnergy = 100f;
    private const float HealthBarWidth = 1.45f;
    private const float BodySize = 1.05f;
    private const float BasicActionDuration = 0.36f;
    private const float BasicHitDelay = 0.18f;
    private const float SkillActionDuration = 0.52f;
    private const float SkillHitDelay = 0.27f;
    private const float UltimateActionDuration = 0.72f;
    private const float UltimateHitDelay = 0.38f;

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
    private Color bodyColor;
    private float bodySize = BodySize;
    private float attackCooldown;
    private float attackPulse;
    private float skillPulse;
    private float hitFlash;
    private float animationTime;
    private float actionLockRemaining;
    private float actionHitRemaining;
    private float energy;
    private int currentShield;
    private float shieldRemaining;
    private float markRemaining;
    private float hasteRemaining;
    private float slowRemaining;
    private float tauntRemaining;
    private float stunRemaining;
    private float burnRemaining;
    private float burnTickCooldown;
    private int burnDamage;
    private float poisonRemaining;
    private float poisonTickCooldown;
    private int poisonDamage;
    private float attackBuffRemaining;
    private float attackDebuffRemaining;
    private float activeSkillCooldown;
    private float abilityPowerMultiplier = 1f;
    private int currentHealth;
    private int basicSkillLevel = 1;
    private int passiveSkillLevel = 1;
    private int activeSkillLevel = 1;
    private int ultimateSkillLevel = 1;
    private int basicAttackCount;
    private int incomingHitCount;
    private bool reactorTriggered;
    private bool sanctuaryTriggered;
    private bool lastBastionTriggered;
    private bool furyTriggered;
    private bool isMoving;
    private PrototypeCombatant markedBy;
    private PrototypeCombatant forcedTarget;
    private PrototypeCombatant burnSource;
    private PrototypeCombatant poisonSource;
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
            if (stunRemaining > 0f) status += "Stun ";
            if (burnRemaining > 0f) status += "Burn ";
            if (poisonRemaining > 0f) status += "Poison ";
            if (slowRemaining > 0f) status += "Slow ";
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
                    return "Basic: every 3rd hit marks the target (+25% Nova damage).\nPassive: shield at 50% HP; faster attacks and energy below 35%.\nActive: Solar Thrust deals damage, marks and shields Nova.\nUltimate: Stellar Breaker hits one target heavily and splashes all enemies.";
                case PrototypeSkillKit.Ion:
                    return "Basic: every 4th hit chains lightning.\nPassive: every 5th incoming hit is phased and grants energy.\nActive: Static Field damages, stuns and chains.\nUltimate: Volt Rush strikes three times and retargets after kills.";
                case PrototypeSkillKit.Astra:
                    return "Basic: every 3rd hit heals the weakest ally.\nPassive: Sanctuary shields an ally below 30% HP once.\nActive: Star Ward heals and shields the weakest ally.\nUltimate: Astral Renewal heals the team and strongly protects one ally.";
                case PrototypeSkillKit.Krag:
                    return "Basic: attacks from the front row.\nPassive: +50% defense above 50% HP; Last Bastion shield below 35%.\nActive: Crushing Orbit damages, weakens attack and shields Krag.\nUltimate: Gravity Bulwark shields Krag and taunts the enemy team.";
                case PrototypeSkillKit.Vex:
                    return "Basic: prioritizes the back row and deals +30% damage below 50% target HP.\nPassive: kills grant haste and 50 energy.\nActive: Void Step bursts a back-row target.\nUltimate: Crimson Execute deals massive damage to wounded enemies.";
                case PrototypeSkillKit.Rook:
                    return "Basic: every 3rd shot ignores 50% defense.\nPassive: fights safely from the back row.\nActive: Pinning Shot pierces armor and slows.\nUltimate: Rail Barrage damages and slows every enemy.";
                case PrototypeSkillKit.Lyra:
                    return "Basic: every 4th shot hits a second target.\nPassive: long-range back-row positioning.\nActive: Sunpiercer deals damage and burns.\nUltimate: Helios Rain damages and burns the full enemy team.";
                case PrototypeSkillKit.Brakk:
                    return "Basic: every 4th attack grants a self shield.\nPassive: front-row Tanker blocks access to allies.\nActive: Guard Link shields and empowers the weakest ally.\nUltimate: Stoneheart Pact shields the entire team.";
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
        AttackInterval = definition.AttackInterval;
        currentHealth = MaxHealth;
        attackCooldown = AttackInterval * 0.5f;
        activeSkillCooldown = ActiveSkillInterval * 0.5f;
        bodyColor = definition.BodyColor;
        battle = battleManager;
        prototypeSprite = sprite;
        characterSprites = PrototypePixelArt.Create(definition);
        bodySize = isBoss ? BodySize * 1.25f : BodySize;
        spawnPosition = transform.position;

        body = CreateSprite("Body", characterSprites.Idle[0], Color.white, new Vector3(bodySize, bodySize), 2);
        body.flipX = Team == PrototypeTeam.Enemies;
        CreateHealthBar(sprite);
        CreateSkillVisuals(sprite);

        Debug.Assert(characterSprites.Attack.Length >= 4 && characterSprites.Skill.Length >= 4,
            "Combat actions need enough frames to expose a clear hit frame.");
        Debug.Assert(BasicHitDelay < BasicActionDuration && SkillHitDelay < SkillActionDuration &&
            UltimateHitDelay < UltimateActionDuration, "Action hit frames must occur before actions finish.");
    }

    internal void ResetForFarmWave()
    {
        transform.position = spawnPosition;
        currentHealth = MaxHealth;
        energy = 0f;
        attackCooldown = AttackInterval * 0.5f;
        activeSkillCooldown = ActiveSkillInterval * 0.5f;
        attackPulse = 0f;
        skillPulse = 0f;
        hitFlash = 0f;
        actionLockRemaining = 0f;
        actionHitRemaining = 0f;
        pendingAction = null;
        animationState = PrototypeAnimationState.Idle;
        animationTime = 0f;
        hasteRemaining = 0f;
        slowRemaining = 0f;
        tauntRemaining = 0f;
        stunRemaining = 0f;
        attackBuffRemaining = 0f;
        attackDebuffRemaining = 0f;
        markRemaining = 0f;
        markedBy = null;
        forcedTarget = null;
        Target = null;
        basicAttackCount = 0;
        incomingHitCount = 0;
        reactorTriggered = false;
        sanctuaryTriggered = false;
        lastBastionTriggered = false;
        furyTriggered = false;
        CleanseNegativeStatuses();
        DisableShield();
        markVisual.enabled = false;
        body.color = Color.white;
        UpdateHealthBar();
        UpdateEnergyBar();
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

        if (actionLockRemaining > 0f)
        {
            UpdateFeedback();
            return;
        }

        CheckAstraSanctuary();

        if (stunRemaining > 0f)
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
        var horizontalDistance = Target.transform.position.x - transform.position.x;
        if (Mathf.Abs(horizontalDistance) > AttackRange)
        {
            transform.position += Vector3.right * (Mathf.Sign(horizontalDistance) * MoveSpeed * Time.deltaTime);
            isMoving = true;
            UpdateFeedback();
            return;
        }

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

        UpdateFeedback();
    }

    private void StartBasicAttack()
    {
        attackCooldown = EffectiveAttackInterval;
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
        var defenseIgnore = SkillKit == PrototypeSkillKit.Rook && basicAttackCount % 3 == 0 ? 0.5f : 0f;
        DealDamage(Target, SkillPower(skills == null ? null : skills.Basic, 1f), defenseIgnore);
        var basicEnergy = skills != null && skills.Basic != null && skills.Basic.EnergyGain > 0
            ? skills.Basic.EnergyGain
            : 18f;
        GainEnergy(SkillKit == PrototypeSkillKit.Nova && IsLastLightActive ? basicEnergy * 1.5f : basicEnergy);

        switch (SkillKit)
        {
            case PrototypeSkillKit.Nova when basicAttackCount % 3 == 0 && Target.IsAlive:
                Target.ApplyPhotonMark(this);
                break;
            case PrototypeSkillKit.Ion when basicAttackCount % 4 == 0:
                TriggerArcChain();
                break;
            case PrototypeSkillKit.Astra when basicAttackCount % 3 == 0:
                TriggerGuidingLight();
                break;
            case PrototypeSkillKit.Lyra when basicAttackCount % 4 == 0:
                HitSecondaryTarget(0.75f);
                break;
            case PrototypeSkillKit.Brakk when basicAttackCount % 4 == 0:
                GrantShield(Mathf.RoundToInt(MaxHealth * 0.12f), 4f);
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
                battle.Announce("NOVA  ·  SOLAR THRUST");
                DealDamage(Target, SkillPower(skills == null ? null : skills.Active, 1.45f));
                if (Target.IsAlive) Target.ApplyPhotonMark(this);
                GrantShield(Mathf.RoundToInt(MaxHealth * 0.12f), 4f);
                break;
            case PrototypeSkillKit.Ion:
                battle.Announce("ION  ·  STATIC FIELD");
                DealDamage(Target, SkillPower(skills == null ? null : skills.Active, 0.9f));
                if (Target.IsAlive) Target.ApplyStun(1f);
                HitSecondaryTarget(0.45f);
                break;
            case PrototypeSkillKit.Astra:
                battle.Announce("ASTRA  ·  STAR WARD");
                var wounded = battle.FindLowestHealth(battle.GetTeam(Team));
                if (wounded != null)
                {
                    wounded.Heal(Mathf.RoundToInt(EffectiveAttack * 0.8f));
                    wounded.GrantShield(Mathf.RoundToInt(wounded.MaxHealth * 0.15f), 5f);
                }
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
                battle.Announce("BRAKK  ·  GUARD LINK");
                var guarded = battle.FindLowestHealth(battle.GetTeam(Team));
                if (guarded != null)
                {
                    guarded.GrantShield(Mathf.RoundToInt(guarded.MaxHealth * 0.25f), 5f);
                    guarded.ApplyAttackBuff(4f);
                }
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
        if ((SkillKit == PrototypeSkillKit.Nova || SkillKit == PrototypeSkillKit.Hex) && !AcquireTarget())
        {
            abilityPowerMultiplier = 1f;
            return;
        }

        switch (SkillKit)
        {
            case PrototypeSkillKit.Nova:
                CastStellarBreaker();
                break;
            case PrototypeSkillKit.Ion:
                CastVoltRush();
                break;
            case PrototypeSkillKit.Astra:
                CastAstralRenewal();
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
                CastStoneheartPact();
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
        actionLockRemaining = duration;
        actionHitRemaining = hitDelay;
        if (skill)
        {
            skillPulse = duration;
            PrototypeBattleFeedback.PlayAction(ultimate);
        }
        else
        {
            attackPulse = duration;
        }
    }

    private void UpdatePendingAction()
    {
        actionLockRemaining = Mathf.Max(0f, actionLockRemaining - Time.deltaTime);
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
        if (Target == null || !Target.IsAlive)
        {
            Target = battle.FindTarget(this);
        }

        return Target != null;
    }

    private void CastStellarBreaker()
    {
        battle.Announce("NOVA  ·  STELLAR BREAKER");

        var primaryTarget = Target;
        DealDamage(primaryTarget, SkillPower(skills == null ? null : skills.Ultimate, 2.2f));
        PrototypeEffect.Spawn(prototypeSprite, primaryTarget.transform.position, bodyColor, 0.45f, 2.2f, 0.45f);

        foreach (var enemy in battle.GetOpponents(Team))
        {
            if (enemy != primaryTarget && enemy.IsAlive)
            {
                DealDamage(enemy, 0.7f);
                PrototypeEffect.Spawn(prototypeSprite, enemy.transform.position, bodyColor, 0.3f, 1.4f, 0.35f);
            }
        }
    }

    private void CastVoltRush()
    {
        battle.Announce("ION  ·  VOLT RUSH");
        for (var hit = 0; hit < 3; hit++)
        {
            if (Target == null || !Target.IsAlive)
            {
                Target = battle.FindTarget(this);
            }

            if (Target == null)
            {
                break;
            }

            DealDamage(Target, SkillPower(skills == null ? null : skills.Ultimate, 0.85f));
            PrototypeEffect.Spawn(prototypeSprite, Target.transform.position, bodyColor, 0.2f, 0.9f, 0.2f);
        }
    }

    private void CastAstralRenewal()
    {
        battle.Announce("ASTRA  ·  ASTRAL RENEWAL");
        var allies = battle.GetTeam(Team);
        var primary = battle.FindLowestHealth(allies);

        foreach (var ally in allies)
        {
            if (ally.IsAlive)
            {
                ally.Heal(Mathf.RoundToInt(EffectiveAttack * 0.35f));
            }
        }

        if (primary != null)
        {
            primary.Heal(EffectiveAttack);
            primary.GrantShield(Mathf.RoundToInt(primary.MaxHealth * 0.2f), 5f);
            PrototypeEffect.Spawn(prototypeSprite, primary.transform.position, bodyColor, 0.5f, 1.8f, 0.45f);
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
        foreach (var enemy in battle.GetOpponents(Team))
        {
            if (!enemy.IsAlive)
            {
                continue;
            }

            DealDamage(enemy, SkillPower(skills == null ? null : skills.Ultimate, 1.15f), 0.3f);
            enemy.ApplyAttackSlow(4f);
            PrototypeEffect.Spawn(prototypeSprite, enemy.transform.position, bodyColor, 0.25f, 1.2f, 0.3f);
        }
    }

    private void CastHeliosRain()
    {
        battle.Announce("LYRA  ·  HELIOS RAIN");
        foreach (var enemy in battle.GetOpponents(Team))
        {
            if (enemy.IsAlive)
            {
                DealDamage(enemy, SkillPower(skills == null ? null : skills.Ultimate, 1.1f));
                enemy.ApplyBurn(this, Mathf.RoundToInt(EffectiveAttack * 0.2f), 4f);
                PrototypeEffect.Spawn(prototypeSprite, enemy.transform.position, bodyColor, 0.25f, 1.3f, 0.35f);
            }
        }
    }

    private void CastStoneheartPact()
    {
        battle.Announce("BRAKK  ·  STONEHEART PACT");
        foreach (var ally in battle.GetTeam(Team))
        {
            if (ally.IsAlive)
            {
                ally.GrantShield(Mathf.RoundToInt(ally.MaxHealth * 0.2f), 6f);
            }
        }

        PrototypeEffect.Spawn(prototypeSprite, transform.position, bodyColor, 0.8f, 2.2f, 0.5f);
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
        HitSecondaryTarget(0.6f);
    }

    private void HitSecondaryTarget(float multiplier)
    {
        PrototypeCombatant chainTarget = null;
        var closestDistance = float.MaxValue;
        foreach (var enemy in battle.GetOpponents(Team))
        {
            if (!enemy.IsAlive || enemy == Target)
            {
                continue;
            }

            var distance = (enemy.transform.position - Target.transform.position).sqrMagnitude;
            if (distance < closestDistance)
            {
                chainTarget = enemy;
                closestDistance = distance;
            }
        }

        if (chainTarget != null)
        {
            DealDamage(chainTarget, multiplier);
            PrototypeEffect.Spawn(prototypeSprite, chainTarget.transform.position, bodyColor, 0.2f, 1f, 0.25f);
        }
    }

    private void TriggerGuidingLight()
    {
        var ally = battle.FindLowestHealth(battle.GetTeam(Team));
        if (ally != null)
        {
            ally.Heal(Mathf.RoundToInt(EffectiveAttack * 0.45f));
            PrototypeEffect.Spawn(prototypeSprite, ally.transform.position, bodyColor, 0.25f, 1f, 0.3f);
        }
    }

    private void CheckAstraSanctuary()
    {
        if (SkillKit != PrototypeSkillKit.Astra || sanctuaryTriggered)
        {
            return;
        }

        var ally = battle.FindLowestHealth(battle.GetTeam(Team));
        if (ally == null || ally.CurrentHealth > ally.MaxHealth * 0.3f)
        {
            return;
        }

        sanctuaryTriggered = true;
        ally.GrantShield(Mathf.RoundToInt(MaxHealth * 0.25f), 6f);
        battle.Announce("ASTRA  ·  SANCTUARY");
        PrototypeEffect.Spawn(prototypeSprite, ally.transform.position, bodyColor, 0.8f, 1.7f, 0.4f);
    }

    private void TriggerMomentum()
    {
        hasteRemaining = 5f;
        GainEnergy(50f);
        battle.Announce("VEX  ·  MOMENTUM");
    }

    private void Heal(int amount)
    {
        if (!IsAlive || amount <= 0)
        {
            return;
        }

        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
        UpdateHealthBar();
    }

    private void GrantShield(int amount, float duration)
    {
        if (!IsAlive || amount <= 0)
        {
            return;
        }

        currentShield = Mathf.Max(currentShield, amount);
        shieldRemaining = Mathf.Max(shieldRemaining, duration);
        shieldVisual.enabled = true;
    }

    private void ApplyTaunt(PrototypeCombatant source, float duration)
    {
        forcedTarget = source;
        tauntRemaining = duration;
        Target = source;
    }

    private void ApplyAttackSlow(float duration)
    {
        slowRemaining = Mathf.Max(slowRemaining, duration);
    }

    private void ApplyHaste(float duration)
    {
        hasteRemaining = Mathf.Max(hasteRemaining, duration);
    }

    private void ApplyStun(float duration)
    {
        stunRemaining = Mathf.Max(stunRemaining, duration);
    }

    private void ApplyBurn(PrototypeCombatant source, int damage, float duration)
    {
        burnSource = source;
        burnDamage = Mathf.Max(burnDamage, damage);
        burnRemaining = Mathf.Max(burnRemaining, duration);
        burnTickCooldown = Mathf.Min(burnTickCooldown <= 0f ? 1f : burnTickCooldown, 1f);
    }

    private void ApplyPoison(PrototypeCombatant source, int damage, float duration)
    {
        poisonSource = source;
        poisonDamage = Mathf.Max(poisonDamage, damage);
        poisonRemaining = Mathf.Max(poisonRemaining, duration);
        poisonTickCooldown = Mathf.Min(poisonTickCooldown <= 0f ? 1f : poisonTickCooldown, 1f);
    }

    private void ApplyAttackBuff(float duration)
    {
        attackBuffRemaining = Mathf.Max(attackBuffRemaining, duration);
    }

    private void ApplyAttackDebuff(float duration)
    {
        attackDebuffRemaining = Mathf.Max(attackDebuffRemaining, duration);
    }

    private void CleanseNegativeStatuses()
    {
        stunRemaining = 0f;
        slowRemaining = 0f;
        burnRemaining = 0f;
        burnTickCooldown = 0f;
        burnDamage = 0;
        burnSource = null;
        poisonRemaining = 0f;
        poisonTickCooldown = 0f;
        poisonDamage = 0;
        poisonSource = null;
        attackDebuffRemaining = 0f;
    }

    private void DrainEnergy(float amount)
    {
        energy = Mathf.Max(0f, energy - amount);
        UpdateEnergyBar();
    }

    private bool DealDamage(PrototypeCombatant target, float multiplier, float defenseIgnore = 0f)
    {
        if (target == null || !target.IsAlive)
        {
            return false;
        }

        if (SkillKit == PrototypeSkillKit.Vex && target.CurrentHealth <= target.MaxHealth * 0.5f)
        {
            multiplier *= 1.3f;
        }

        var marked = target.markedBy == this && target.markRemaining > 0f;
        var wasAlive = target.IsAlive;
        var appliedDamage = target.TakeDamage(
            CalculateDamage(EffectiveAttack, target.EffectiveDefense, multiplier, marked, defenseIgnore));
        DamageDealt += appliedDamage;
        if (appliedDamage > 0)
        {
            var strongImpact = skillPulse > 0f;
            PrototypeEffect.Spawn(
                prototypeSprite,
                target.transform.position,
                SkillAccent,
                strongImpact ? 0.32f : 0.16f,
                strongImpact ? 1.3f : 0.7f,
                strongImpact ? 0.28f : 0.16f);
            PrototypeBattleFeedback.PlayImpact(strongImpact);
        }
        var killed = wasAlive && !target.IsAlive;
        if (killed && SkillKit == PrototypeSkillKit.Vex)
        {
            TriggerMomentum();
        }

        return killed;
    }

    private void DealStatusDamage(PrototypeCombatant target, int damage)
    {
        DamageDealt += target.TakeDamage(damage, false);
    }

    internal static int CalculateDamage(
        int attack,
        int defense,
        float multiplier,
        bool marked,
        float defenseIgnore = 0f)
    {
        var rawDamage = Mathf.RoundToInt(attack * multiplier * (marked ? 1.25f : 1f));
        var effectiveDefense = Mathf.RoundToInt(defense * (1f - Mathf.Clamp01(defenseIgnore)));
        return Mathf.Max(1, rawDamage - effectiveDefense);
    }

    private int TakeDamage(int damage, bool triggersOnHit = true)
    {
        if (!IsAlive)
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
        var absorbed = 0;
        if (currentShield > 0)
        {
            absorbed = Mathf.Min(currentShield, remainingDamage);
            currentShield -= absorbed;
            remainingDamage -= absorbed;
            if (currentShield <= 0)
            {
                DisableShield();
            }
        }

        var previousHealth = currentHealth;
        var minimumHealth = battle.IsIdleFarmMode && Team == PrototypeTeam.Allies ? 1 : 0;
        currentHealth = Mathf.Max(minimumHealth, currentHealth - remainingDamage);
        hitFlash = 0.12f;
        UpdateHealthBar();

        if (triggersOnHit && SkillKit != PrototypeSkillKit.None && IsAlive)
        {
            GainEnergy(SkillKit == PrototypeSkillKit.Nova && IsLastLightActive ? 9f : 6f);
        }

        if (SkillKit == PrototypeSkillKit.Nova && IsAlive)
        {
            if (!reactorTriggered && currentHealth <= MaxHealth * 0.5f)
            {
                ActivateAegisReactor();
            }
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
            DisableShield();
            markVisual.enabled = false;
            body.color = Color.white;
        }

        var appliedDamage = absorbed + previousHealth - currentHealth;
        if (appliedDamage > 0)
        {
            PrototypeDamageNumber.Spawn(
                transform.position + Vector3.up * 0.95f,
                appliedDamage,
                absorbed > 0 ? new Color(0.35f, 0.9f, 1f) : new Color(1f, 0.78f, 0.2f));
        }
        return appliedDamage;
    }

    private int EffectiveDefense =>
        SkillKit == PrototypeSkillKit.Krag && currentHealth > MaxHealth * 0.5f
            ? Mathf.RoundToInt(Defense * 1.5f)
            : Defense;

    private int EffectiveAttack
    {
        get
        {
            var multiplier = attackBuffRemaining > 0f ? 1.25f : 1f;
            if (attackDebuffRemaining > 0f)
            {
                multiplier *= 0.75f;
            }

            return Mathf.RoundToInt(
                Attack * multiplier * PassiveSkillMultiplier * abilityPowerMultiplier);
        }
    }

    private float BasicSkillMultiplier => 1f + (basicSkillLevel - 1) * 0.05f;
    private float PassiveSkillMultiplier => 1f + (passiveSkillLevel - 1) * 0.03f;
    private float ActiveSkillMultiplier => 1f + (activeSkillLevel - 1) * 0.08f;
    private float UltimateSkillMultiplier => 1f + (ultimateSkillLevel - 1) * 0.1f;

    private void ApplyPhotonMark(PrototypeCombatant source)
    {
        markedBy = source;
        markRemaining = 6f;
        markVisual.enabled = true;
        PrototypeEffect.Spawn(prototypeSprite, transform.position, new Color(1f, 0.82f, 0.2f), 0.25f, 1.1f, 0.3f);
    }

    private void ActivateAegisReactor()
    {
        reactorTriggered = true;
        currentShield = Mathf.RoundToInt(MaxHealth * 0.3f);
        shieldRemaining = 6f;
        shieldVisual.enabled = true;
        battle.Announce("NOVA  ·  AEGIS REACTOR");
        PrototypeEffect.Spawn(prototypeSprite, transform.position, bodyColor, 1.1f, 1.8f, 0.45f);
    }

    private void GainEnergy(float amount)
    {
        energy = Mathf.Min(MaxEnergy, energy + amount);
        UpdateEnergyBar();
    }

    private void UpdateStatuses()
    {
        activeSkillCooldown = Mathf.Max(0f, activeSkillCooldown - Time.deltaTime);
        hasteRemaining = Mathf.Max(0f, hasteRemaining - Time.deltaTime);
        slowRemaining = Mathf.Max(0f, slowRemaining - Time.deltaTime);
        stunRemaining = Mathf.Max(0f, stunRemaining - Time.deltaTime);
        attackBuffRemaining = Mathf.Max(0f, attackBuffRemaining - Time.deltaTime);
        attackDebuffRemaining = Mathf.Max(0f, attackDebuffRemaining - Time.deltaTime);
        TickDamageOverTime(ref burnRemaining, ref burnTickCooldown, burnDamage, burnSource);
        TickDamageOverTime(ref poisonRemaining, ref poisonTickCooldown, poisonDamage, poisonSource);

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

        if (markRemaining > 0f)
        {
            markRemaining -= Time.deltaTime;
            if (markRemaining <= 0f)
            {
                markedBy = null;
                markVisual.enabled = false;
            }
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

    private void DisableShield()
    {
        currentShield = 0;
        shieldRemaining = 0f;
        if (shieldVisual != null)
        {
            shieldVisual.enabled = false;
        }
    }

    private void UpdateFeedback()
    {
        skillPulse = Mathf.Max(0f, skillPulse - Time.deltaTime);
        if (attackPulse > 0f)
        {
            attackPulse = Mathf.Max(0f, attackPulse - Time.deltaTime);
            var progress = 1f - attackPulse / BasicActionDuration;
            var pulse = 1f + Mathf.Sin(progress * Mathf.PI) * 0.08f;
            body.transform.localScale = new Vector3(bodySize * pulse, bodySize * pulse, 1f);
        }
        else
        {
            body.transform.localScale = new Vector3(bodySize, bodySize, 1f);
        }

        if (IsAlive)
        {
            hitFlash -= Time.deltaTime;
            body.color = hitFlash > 0f ? new Color(1f, 0.55f, 0.55f) : Color.white;
        }

        UpdateCharacterAnimation();
    }

    private void UpdateCharacterAnimation()
    {
        if (characterSprites == null && Species != PrototypeSpecies.Unknown)
        {
            characterSprites = definition != null
                ? PrototypePixelArt.Create(definition)
                : PrototypePixelArt.Create(Species, CombatClass, SkillKit, bodyColor);
        }

        if (characterSprites == null || body == null)
        {
            return;
        }

        var nextState = !IsAlive
            ? PrototypeAnimationState.Death
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

        var frameDuration = animationState == PrototypeAnimationState.Idle
            ? 0.45f
            : animationState == PrototypeAnimationState.Run
                ? 0.1f
                : animationState == PrototypeAnimationState.Hit ? 0.06f : 0.09f;
        var rawFrame = Mathf.FloorToInt(animationTime / frameDuration);
        var loops = animationState == PrototypeAnimationState.Idle || animationState == PrototypeAnimationState.Run;
        var frameIndex = loops ? rawFrame % frames.Length : Mathf.Min(rawFrame, frames.Length - 1);
        if (frames[frameIndex] != null)
        {
            body.sprite = frames[frameIndex];
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
                case PrototypeSkillKit.Astra: return new Color(0.35f, 1f, 0.65f);
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
        var background = CreateSprite(
            "Health Background",
            sprite,
            new Color(0.02f, 0.025f, 0.04f),
            new Vector3(HealthBarWidth + 0.1f, 0.17f, 1f),
            4);
        background.transform.localPosition = new Vector3(0f, 0.78f);

        healthFill = CreateSprite(
            "Health Fill",
            sprite,
            bodyColor,
            new Vector3(HealthBarWidth, 0.09f, 1f),
            5);
        healthFill.transform.localPosition = new Vector3(0f, 0.78f);
    }

    private void CreateSkillVisuals(Sprite sprite)
    {
        shieldVisual = CreateSprite(
            "Aegis Shield",
            sprite,
            new Color(bodyColor.r, bodyColor.g, bodyColor.b, 0.25f),
            new Vector3(1.55f, 1.55f),
            1);
        shieldVisual.enabled = false;

        markVisual = CreateSprite(
            "Photon Mark",
            sprite,
            new Color(1f, 0.82f, 0.2f),
            new Vector3(0.18f, 0.18f),
            6);
        markVisual.transform.localPosition = new Vector3(0f, 1.08f);
        markVisual.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        markVisual.enabled = false;

        var background = CreateSprite(
            "Energy Background",
            sprite,
            new Color(0.02f, 0.025f, 0.04f),
            new Vector3(HealthBarWidth + 0.1f, 0.12f, 1f),
            4);
        background.transform.localPosition = new Vector3(0f, 0.61f);

        energyFill = CreateSprite(
            "Energy Fill",
            sprite,
            bodyColor,
            new Vector3(0f, 0.06f, 1f),
            5);
        energyFill.transform.localPosition = new Vector3(-HealthBarWidth * 0.5f, 0.61f);
    }

    private void UpdateHealthBar()
    {
        var ratio = (float)currentHealth / MaxHealth;
        healthFill.transform.localScale = new Vector3(HealthBarWidth * ratio, 0.09f, 1f);
        healthFill.transform.localPosition = new Vector3(-HealthBarWidth * (1f - ratio) * 0.5f, 0.78f);
    }

    private void UpdateEnergyBar()
    {
        if (energyFill == null)
        {
            return;
        }

        var ratio = energy / MaxEnergy;
        energyFill.transform.localScale = new Vector3(HealthBarWidth * ratio, 0.06f, 1f);
        energyFill.transform.localPosition = new Vector3(-HealthBarWidth * (1f - ratio) * 0.5f, 0.61f);
    }

    private bool IsLastLightActive => SkillKit == PrototypeSkillKit.Nova && currentHealth <= MaxHealth * 0.35f;

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
            var interval = IsLastLightActive ? AttackInterval * 0.7f : AttackInterval;
            if (hasteRemaining > 0f)
            {
                interval *= 0.65f;
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
    private AudioClip hitClip;
    private AudioClip skillClip;
    private AudioClip ultimateClip;
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
            feedback.audioSource.PlayOneShot(feedback.hitClip, strong ? 0.35f : 0.16f);
            feedback.audioCooldown = 0.045f;
        }
    }

    public static void PlayAction(bool ultimate)
    {
        var feedback = Get();
        if (feedback == null)
        {
            return;
        }

        feedback.audioSource.PlayOneShot(ultimate ? feedback.ultimateClip : feedback.skillClip, 0.42f);
        if (ultimate)
        {
            feedback.shakeRemaining = Mathf.Max(feedback.shakeRemaining, 0.18f);
            feedback.shakeStrength = Mathf.Max(feedback.shakeStrength, 0.06f);
        }
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

        if (hitClip == null) hitClip = CreateTone("Pixel Hit", 150f, 0.045f);
        if (skillClip == null) skillClip = CreateTone("Pixel Skill", 420f, 0.12f);
        if (ultimateClip == null) ultimateClip = CreateTone("Pixel Ultimate", 220f, 0.22f);
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
        const float pixelUnit = 1f / 32f;
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
}

internal sealed class PrototypeEffect : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private float startScale;
    private float endScale;
    private float duration;
    private float elapsed;

    public static void Spawn(Sprite sprite, Vector3 position, Color color, float start, float end, float lifetime)
    {
        var gameObject = new GameObject("Skill Effect");
        gameObject.transform.position = position;

        var effect = gameObject.AddComponent<PrototypeEffect>();
        effect.spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        effect.spriteRenderer.sprite = sprite;
        effect.spriteRenderer.color = new Color(color.r, color.g, color.b, 0.5f);
        effect.spriteRenderer.sortingOrder = 8;
        effect.startScale = start;
        effect.endScale = end;
        effect.duration = lifetime;
        gameObject.transform.localScale = new Vector3(start, start, 1f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        var progress = Mathf.Clamp01(elapsed / duration);
        var scale = Mathf.Lerp(startScale, endScale, progress);
        transform.localScale = new Vector3(scale, scale, 1f);

        var color = spriteRenderer.color;
        color.a = Mathf.Lerp(0.5f, 0f, progress);
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
        var gameObject = new GameObject("Damage Number");
        gameObject.transform.position = position;
        var number = gameObject.AddComponent<PrototypeDamageNumber>();
        number.textMesh = gameObject.AddComponent<TextMesh>();
        number.textMesh.text = damage.ToString();
        number.textMesh.anchor = TextAnchor.MiddleCenter;
        number.textMesh.alignment = TextAlignment.Center;
        number.textMesh.fontSize = 40;
        number.textMesh.characterSize = 0.07f;
        number.textMesh.color = color;
        number.textMesh.fontStyle = FontStyle.Bold;
        gameObject.GetComponent<MeshRenderer>().sortingOrder = 20;
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
