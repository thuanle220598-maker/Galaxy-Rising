using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

internal sealed class PrototypeGameFlow : MonoBehaviour
{
    private enum ScreenType
    {
        Battle,
        Squad,
        Heroes,
        Gacha
    }

    private static PrototypeGameFlow instance;
    private readonly Color background = new Color(0.025f, 0.045f, 0.09f, 0.98f);
    private readonly Color battleBackground = new Color(0.015f, 0.035f, 0.07f, 0.34f);
    private readonly Color panelColor = new Color(0.055f, 0.1f, 0.18f, 0.96f);
    private readonly Color buttonColor = new Color(0.08f, 0.3f, 0.42f, 1f);
    private readonly Color accentColor = new Color(1f, 0.72f, 0.18f, 1f);
    private readonly Color cyanColor = new Color(0.35f, 0.88f, 1f, 1f);

    private Canvas canvas;
    private Font font;
    private RectTransform screen;
    private CanvasGroup screenGroup;
    private ScreenType screenType;
    private PrototypeHud hud;
    private PrototypeBattle battle;
    private PrototypeCombatant selectedUnit;
    private Text titleText;
    private Text timerText;
    private Text announcementText;
    private Text skillText;
    private Text[] allyTexts;
    private Text[] enemyTexts;
    private GameObject challengeButton;
    private GameObject activitiesPanel;
    private GameObject resultPanel;
    private Text resultText;
    private bool activitiesVisible;
    private PrototypeDungeonType dungeonType;
    private int dungeonLevel = 1;
    private readonly List<CombatantDefinition> squad = new List<CombatantDefinition>(5);
    private readonly List<PrototypeFormationRow> squadRows = new List<PrototypeFormationRow>(5);
    private CombatantDefinition selectedHero;
    private PrototypeSummonResult[] summonResults = new PrototypeSummonResult[0];
    private string message = string.Empty;
    private bool resetConfirmation;

    public static bool HasInstance
    {
        get
        {
            RecoverInstance();
            return instance != null;
        }
    }

    public static PrototypeGameFlow Instance
    {
        get
        {
            RecoverInstance();
            if (instance == null)
            {
                var gameObject = new GameObject("Game Flow");
                instance = gameObject.AddComponent<PrototypeGameFlow>();
                DontDestroyOnLoad(gameObject);
            }
            return instance;
        }
    }

    private static void RecoverInstance()
    {
        if (instance == null)
        {
            instance = FindFirstObjectByType<PrototypeGameFlow>();
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateCanvas();
    }

    public void BindBattle(PrototypeHud battleHud, PrototypeBattle battleManager)
    {
        hud = battleHud;
        battle = battleManager;
        selectedUnit = battle.Allies[0];
        hud.SetCanvasOverlayOpen(false);
        BuildBattleScreen();
    }

    public void UnbindBattle(PrototypeHud battleHud)
    {
        if (hud == battleHud)
        {
            hud = null;
            battle = null;
        }
    }

    private void Update()
    {
        if (screenGroup != null && screenGroup.alpha < 1f)
        {
            screenGroup.alpha = Mathf.MoveTowards(screenGroup.alpha, 1f, Time.unscaledDeltaTime * 6f);
            screenGroup.interactable = screenGroup.alpha >= 0.95f;
            screenGroup.blocksRaycasts = screenGroup.interactable;
        }

        if (screenType != ScreenType.Battle || battle == null || titleText == null)
        {
            return;
        }

        UpdateBattleScreen();
    }

    private void CreateCanvas()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvasObject = new GameObject("Prototype Canvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(360f, 640f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var eventObject = new GameObject("Event System");
            eventObject.transform.SetParent(transform, false);
            eventObject.AddComponent<EventSystem>();
            eventObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }

    private void BuildBattleScreen()
    {
        screenType = ScreenType.Battle;
        activitiesVisible = false;
        ClearScreen();
        AddImage(screen, new Rect(0f, 0f, 360f, 640f), battleBackground)
            .GetComponent<Image>().raycastTarget = false;
        titleText = AddText(screen, new Rect(16f, 12f, 328f, 34f), string.Empty, 19, cyanColor, TextAnchor.MiddleCenter, true);
        timerText = AddText(screen, new Rect(130f, 194f, 100f, 24f), string.Empty, 12, Color.white, TextAnchor.MiddleCenter);
        announcementText = AddText(screen, new Rect(24f, 220f, 312f, 36f), string.Empty, 15, accentColor, TextAnchor.MiddleCenter, true);

        AddImage(screen, new Rect(12f, 54f, 164f, 136f), panelColor);
        AddText(screen, new Rect(16f, 58f, 156f, 20f), "ALLIES", 12, cyanColor, TextAnchor.MiddleCenter, true);
        AddImage(screen, new Rect(184f, 54f, 164f, 136f), panelColor);
        AddText(screen, new Rect(188f, 58f, 156f, 20f), "ENEMIES", 12, new Color(1f, 0.45f, 0.4f), TextAnchor.MiddleCenter, true);

        allyTexts = new Text[battle.Allies.Length];
        enemyTexts = new Text[battle.Enemies.Length];
        for (var index = 0; index < battle.Allies.Length; index++)
        {
            var captured = index;
            allyTexts[index] = AddButton(
                screen,
                new Rect(18f, 80f + index * 20f, 152f, 18f),
                string.Empty,
                () => selectedUnit = battle.Allies[captured],
                new Color(0.06f, 0.18f, 0.27f),
                9).GetComponentInChildren<Text>();
        }
        for (var index = 0; index < battle.Enemies.Length; index++)
        {
            var captured = index;
            enemyTexts[index] = AddButton(
                screen,
                new Rect(190f, 80f + index * 20f, 152f, 18f),
                string.Empty,
                () => selectedUnit = battle.Enemies[captured],
                new Color(0.2f, 0.09f, 0.12f),
                9).GetComponentInChildren<Text>();
        }

        AddImage(screen, new Rect(8f, 442f, 344f, 146f), new Color(0.025f, 0.055f, 0.1f, 0.9f));
        skillText = AddText(screen, new Rect(16f, 448f, 328f, 134f), string.Empty, 10, Color.white, TextAnchor.UpperLeft);
        challengeButton = AddButton(
            screen,
            new Rect(80f, 404f, 200f, 38f),
            "CHALLENGE",
            () => hud.ChallengePendingStage(),
            new Color(0.65f, 0.24f, 0.12f),
            12);

        BuildBottomNavigation();
        BuildActivitiesPanel();
        BuildResultPanel();
        UpdateBattleScreen();
    }

    private void BuildBottomNavigation()
    {
        if (battle.Mode == PrototypeGameMode.Idle)
        {
            AddButton(screen, new Rect(4f, 598f, 64f, 36f), "SQUAD", ShowSquad, buttonColor, 10);
            AddButton(screen, new Rect(74f, 598f, 64f, 36f), "HEROES", ShowHeroes, buttonColor, 10);
            AddButton(screen, new Rect(144f, 598f, 64f, 36f), "SUMMON", ShowGacha, new Color(0.5f, 0.28f, 0.08f), 10);
            AddButton(screen, new Rect(214f, 598f, 64f, 36f), "CLAIM", ClaimIdleRewards, buttonColor, 10);
            AddButton(screen, new Rect(284f, 598f, 64f, 36f), "MODES", ToggleActivities, buttonColor, 10);
        }
        else
        {
            AddButton(screen, new Rect(12f, 598f, 164f, 36f), "CLAIM IDLE", ClaimIdleRewards, buttonColor, 11);
            AddButton(screen, new Rect(184f, 598f, 164f, 36f), "RETURN TO IDLE", () => hud.ReturnToIdleBattle(), buttonColor, 11);
        }
    }

    private void BuildActivitiesPanel()
    {
        activitiesPanel = AddImage(screen, new Rect(24f, 404f, 312f, 182f), new Color(0.04f, 0.09f, 0.16f, 0.99f));
        AddText(activitiesPanel.transform, new Rect(8f, 6f, 296f, 24f), "ACTIVITIES · IDLE KEEPS ACCUMULATING", 11, accentColor, TextAnchor.MiddleCenter, true);
        AddButton(activitiesPanel.transform, new Rect(14f, 38f, 116f, 32f), dungeonType.ToString(), CycleDungeonType, buttonColor, 10).name = "Dungeon Type";
        AddButton(activitiesPanel.transform, new Rect(142f, 38f, 34f, 32f), "-", () => { dungeonLevel = Mathf.Max(1, dungeonLevel - 1); BuildBattleScreen(); activitiesVisible = true; SetActivitiesVisible(); }, buttonColor, 14);
        AddText(activitiesPanel.transform, new Rect(180f, 38f, 58f, 32f), $"Lv {dungeonLevel}", 11, Color.white, TextAnchor.MiddleCenter).name = "Dungeon Level";
        AddButton(activitiesPanel.transform, new Rect(242f, 38f, 34f, 32f), "+", () => { dungeonLevel = Mathf.Min(99, dungeonLevel + 1); BuildBattleScreen(); activitiesVisible = true; SetActivitiesVisible(); }, buttonColor, 14);
        AddText(
            activitiesPanel.transform,
            new Rect(14f, 78f, 264f, 28f),
            $"Reward {PrototypeSession.GetDungeonReward(dungeonType, dungeonLevel)} {dungeonType}",
            10,
            Color.white,
            TextAnchor.MiddleCenter).name = "Dungeon Reward";
        AddButton(
            activitiesPanel.transform,
            new Rect(14f, 120f, 116f, 40f),
            "DUNGEON",
            () => hud.StartActivity(PrototypeGameMode.Dungeon, dungeonType, dungeonLevel),
            new Color(0.1f, 0.42f, 0.36f),
            11);
        AddButton(
            activitiesPanel.transform,
            new Rect(162f, 120f, 116f, 40f),
            "PVP 5v5",
            () => hud.StartActivity(PrototypeGameMode.PvP),
            new Color(0.48f, 0.18f, 0.18f),
            11);
        activitiesPanel.SetActive(false);
    }

    private void BuildResultPanel()
    {
        resultPanel = AddImage(screen, new Rect(34f, 262f, 292f, 278f), new Color(0.035f, 0.07f, 0.13f, 0.99f));
        resultText = AddText(resultPanel.transform, new Rect(12f, 18f, 268f, 140f), string.Empty, 15, accentColor, TextAnchor.MiddleCenter, true);
        AddButton(resultPanel.transform, new Rect(62f, 210f, 168f, 44f), "RETURN TO IDLE", () => hud.ReturnToIdleBattle(), buttonColor, 12);
        resultPanel.SetActive(false);
    }

    private void UpdateBattleScreen()
    {
        titleText.text = GetBattleTitle();
        timerText.text = battle.TimeLimit > 0f ? $"{battle.RemainingTime:0.0}s" : $"{battle.ElapsedTime:0.0}s";
        announcementText.text = battle.HasAnnouncement ? battle.Announcement : string.Empty;
        for (var index = 0; index < battle.Allies.Length; index++)
        {
            allyTexts[index].text = UnitSummary(battle.Allies[index]);
        }
        for (var index = 0; index < battle.Enemies.Length; index++)
        {
            enemyTexts[index].text = UnitSummary(battle.Enemies[index]);
        }

        if (selectedUnit == null)
        {
            selectedUnit = battle.Allies[0];
        }
        var cooldown = selectedUnit.ActiveSkillCooldown <= 0f ? "READY" : $"{selectedUnit.ActiveSkillCooldown:0.0}s";
        skillText.text =
            $"{selectedUnit.Rarity} {selectedUnit.DisplayName} · {selectedUnit.Species} {selectedUnit.CombatClass} · {selectedUnit.FormationRow}\n" +
            $"Lv{selectedUnit.Level} {selectedUnit.Stars}* · B{selectedUnit.BasicSkillLevel}/P{selectedUnit.PassiveSkillLevel}/A{selectedUnit.ActiveSkillLevel}/U{selectedUnit.UltimateSkillLevel}\n" +
            $"Active {cooldown} · Energy {Mathf.RoundToInt(selectedUnit.Energy)}/100 · Damage {selectedUnit.DamageDealt}\n" +
            $"Status: {selectedUnit.StatusSummary}\n{selectedUnit.SkillDescription}";

        challengeButton.SetActive(
            battle.Mode == PrototypeGameMode.Idle && battle.IsIdleFarmMode && !activitiesVisible && !battle.IsFinished);
        if (challengeButton.activeSelf)
        {
            challengeButton.GetComponentInChildren<Text>().text = $"CHALLENGE STAGE {PrototypeSession.IdleStage}";
        }
        skillText.gameObject.SetActive(!activitiesVisible && !battle.IsFinished);
        SetActivitiesVisible();
        var showResult = battle.IsFinished && battle.Mode != PrototypeGameMode.Idle;
        resultPanel.SetActive(showResult);
        if (showResult)
        {
            resultText.text =
                (battle.Winner == PrototypeTeam.Allies ? "VICTORY" : "DEFEAT") +
                "\n\n" + hud.ResultMessage;
        }
    }

    private string GetBattleTitle()
    {
        if (battle.Mode == PrototypeGameMode.Dungeon)
        {
            return $"{battle.DungeonType} DUNGEON · LV {battle.DungeonLevel}";
        }
        if (battle.Mode == PrototypeGameMode.PvP)
        {
            return "PVP ARENA · 5v5";
        }
        if (battle.IsBossStage)
        {
            return $"IDLE STAGE {battle.Stage} · BOSS";
        }
        return battle.IsIdleFarmMode
            ? $"IDLE STAGE {battle.Stage} · FARM WAVE {battle.FarmWave}"
            : $"IDLE STAGE {battle.Stage}";
    }

    private static string UnitSummary(PrototypeCombatant unit)
    {
        return unit.IsAlive
            ? $"{unit.DisplayName}  {unit.CurrentHealth}/{unit.MaxHealth}  E{Mathf.RoundToInt(unit.Energy)}"
            : $"{unit.DisplayName}  KO";
    }

    private void ToggleActivities()
    {
        activitiesVisible = !activitiesVisible;
        SetActivitiesVisible();
    }

    private void SetActivitiesVisible()
    {
        if (activitiesPanel != null)
        {
            activitiesPanel.SetActive(activitiesVisible && battle != null && !battle.IsFinished);
        }
    }

    private void CycleDungeonType()
    {
        dungeonType = (PrototypeDungeonType)(((int)dungeonType + 1) % 3);
        BuildBattleScreen();
        activitiesVisible = true;
        SetActivitiesVisible();
    }

    private void ClaimIdleRewards()
    {
        int gold;
        int experience;
        PrototypeSession.ClaimIdleRewards(out gold, out experience);
        battle.Announce($"IDLE · +{gold} GOLD · +{experience} XP");
    }

    private void ShowSquad()
    {
        hud.SetCanvasOverlayOpen(true);
        screenType = ScreenType.Squad;
        var roster = PrototypeGacha.GetOwnedRoster(CombatPrototype.LoadRoster());
        squad.Clear();
        squadRows.Clear();
        CombatantDefinition[] currentSquad;
        PrototypeFormationRow[] currentRows;
        if (!PrototypeSession.TryGetSquad(roster, out currentSquad, out currentRows))
        {
            currentSquad = new CombatantDefinition[5];
            Array.Copy(roster, currentSquad, 5);
            currentRows = CombatPrototype.GetDefaultRows(currentSquad);
        }
        squad.AddRange(currentSquad);
        squadRows.AddRange(currentRows);
        BuildSquadScreen(roster);
    }

    private void BuildSquadScreen(CombatantDefinition[] roster)
    {
        ClearScreen();
        AddImage(screen, new Rect(0f, 0f, 360f, 640f), background);
        AddText(screen, new Rect(20f, 14f, 320f, 34f), "BUILD YOUR 5-HERO SQUAD", 19, cyanColor, TextAnchor.MiddleCenter, true);
        AddText(screen, new Rect(20f, 48f, 320f, 22f), $"SELECTED {squad.Count}/5", 11, Color.white, TextAnchor.MiddleCenter);
        for (var index = 0; index < roster.Length; index++)
        {
            var hero = roster[index];
            var captured = hero;
            var selected = squad.Contains(hero);
            AddButton(
                screen,
                new Rect(12f + index % 2 * 172f, 74f + index / 2 * 32f, 164f, 28f),
                $"{(selected ? "[X]" : "[ ]")} {hero.Rarity} {hero.DisplayName}\n{hero.Species} · {hero.CombatClass}",
                () =>
                {
                    var selectedIndex = squad.IndexOf(captured);
                    if (selectedIndex >= 0)
                    {
                        squad.RemoveAt(selectedIndex);
                        squadRows.RemoveAt(selectedIndex);
                    }
                    else if (squad.Count < 5)
                    {
                        squad.Add(captured);
                        squadRows.Add(captured.FormationRow);
                    }
                    BuildSquadScreen(roster);
                },
                selected ? new Color(0.12f, 0.42f, 0.3f) : buttonColor,
                10);
        }

        AddImage(screen, new Rect(12f, 274f, 336f, 266f), panelColor);
        AddText(screen, new Rect(20f, 280f, 320f, 22f), "FORMATION", 13, accentColor, TextAnchor.MiddleCenter, true);
        for (var index = 0; index < squad.Count; index++)
        {
            var captured = index;
            AddText(screen, new Rect(22f, 306f + index * 38f, 196f, 32f), $"{index + 1}. {squad[index].DisplayName} · {squad[index].CombatClass}", 10, Color.white, TextAnchor.MiddleLeft);
            AddButton(
                screen,
                new Rect(226f, 306f + index * 38f, 112f, 32f),
                squadRows[index].ToString(),
                () =>
                {
                    squadRows[captured] = (PrototypeFormationRow)(((int)squadRows[captured] + 1) % 3);
                    BuildSquadScreen(roster);
                },
                buttonColor,
                10);
        }
        AddButton(screen, new Rect(20f, 570f, 100f, 44f), "BACK", CloseOverlay, buttonColor, 11);
        var apply = AddButton(
            screen,
            new Rect(130f, 570f, 210f, 44f),
            "APPLY FORMATION",
            () => hud.ApplySquad(squad.ToArray(), squadRows.ToArray()),
            new Color(0.1f, 0.45f, 0.34f),
            11);
        apply.GetComponent<Button>().interactable = squad.Count == 5;
    }

    private void ShowHeroes()
    {
        hud.SetCanvasOverlayOpen(true);
        screenType = ScreenType.Heroes;
        var roster = PrototypeGacha.GetOwnedRoster(CombatPrototype.LoadRoster());
        if (selectedHero == null || !PrototypeGacha.IsOwned(selectedHero.DisplayName))
        {
            selectedHero = roster[0];
        }
        BuildHeroesScreen(roster);
    }

    private void BuildHeroesScreen(CombatantDefinition[] roster)
    {
        ClearScreen();
        AddImage(screen, new Rect(0f, 0f, 360f, 640f), background);
        AddText(screen, new Rect(20f, 10f, 320f, 32f), "HERO DEVELOPMENT", 19, cyanColor, TextAnchor.MiddleCenter, true);
        AddText(screen, new Rect(12f, 44f, 336f, 24f), $"Gold {PrototypeSession.Gold} · XP {PrototypeSession.Experience} · Materials {PrototypeSession.Materials}", 10, Color.white, TextAnchor.MiddleCenter);
        for (var index = 0; index < roster.Length; index++)
        {
            var hero = roster[index];
            var captured = hero;
            var progress = PrototypeProgression.Get(hero.DisplayName);
            AddButton(
                screen,
                new Rect(12f + index % 2 * 172f, 72f + index / 2 * 28f, 164f, 24f),
                $"{hero.Rarity} {hero.DisplayName} · Lv{progress.level} · {progress.stars}*",
                () => { selectedHero = captured; message = string.Empty; BuildHeroesScreen(roster); },
                hero == selectedHero ? new Color(0.12f, 0.42f, 0.3f) : buttonColor,
                9);
        }

        var selectedProgress = PrototypeProgression.Get(selectedHero.DisplayName);
        AddImage(screen, new Rect(12f, 244f, 336f, 322f), panelColor);
        AddText(screen, new Rect(20f, 250f, 320f, 24f), $"{selectedHero.Rarity} · {selectedHero.DisplayName} · {selectedHero.Species} {selectedHero.CombatClass}", 13, accentColor, TextAnchor.MiddleCenter, true);
        AddText(screen, new Rect(20f, 276f, 320f, 22f), $"Level {selectedProgress.level} · XP {selectedProgress.experience}/{PrototypeProgression.ExperienceRequired(selectedProgress)} · Stars {selectedProgress.stars} · Shards {selectedProgress.shards}/{PrototypeProgression.StarCost(selectedProgress)}", 9, Color.white, TextAnchor.MiddleCenter);
        AddText(screen, new Rect(20f, 300f, 320f, 22f), $"HP {Scaled(selectedHero.MaxHealth, PrototypeProgression.HealthMultiplier(selectedProgress))} · ATK {Scaled(selectedHero.Attack, PrototypeProgression.AttackMultiplier(selectedProgress))} · DEF {Scaled(selectedHero.Defense, PrototypeProgression.DefenseMultiplier(selectedProgress))}", 10, Color.white, TextAnchor.MiddleCenter);
        AddButton(screen, new Rect(20f, 330f, 150f, 34f), "TRAIN · 25 XP", () => { message = PrototypeSession.TrySpendReward(PrototypeDungeonType.Experience, 25) ? Train(selectedHero.DisplayName) : "Not enough XP."; BuildHeroesScreen(roster); }, buttonColor, 10);
        AddButton(screen, new Rect(190f, 330f, 150f, 34f), $"STAR UP · {PrototypeProgression.StarCost(selectedProgress)}", () => { message = PrototypeProgression.TryStarUp(selectedHero.DisplayName) ? "Star increased." : "Need more shards."; BuildHeroesScreen(roster); }, buttonColor, 10);
        AddSkillButton(roster, selectedProgress, PrototypeSkillSlot.Basic, new Rect(20f, 374f, 150f, 32f));
        AddSkillButton(roster, selectedProgress, PrototypeSkillSlot.Passive, new Rect(190f, 374f, 150f, 32f));
        AddSkillButton(roster, selectedProgress, PrototypeSkillSlot.Active, new Rect(20f, 412f, 150f, 32f));
        AddSkillButton(roster, selectedProgress, PrototypeSkillSlot.Ultimate, new Rect(190f, 412f, 150f, 32f));
        AddEquipmentButton(roster, selectedProgress, PrototypeEquipmentSlot.Weapon, new Rect(20f, 456f, 98f, 40f));
        AddEquipmentButton(roster, selectedProgress, PrototypeEquipmentSlot.Armor, new Rect(130f, 456f, 98f, 40f));
        AddEquipmentButton(roster, selectedProgress, PrototypeEquipmentSlot.Core, new Rect(240f, 456f, 98f, 40f));
        AddText(screen, new Rect(20f, 504f, 320f, 24f), message, 10, accentColor, TextAnchor.MiddleCenter);
        AddButton(screen, new Rect(110f, 584f, 140f, 40f), "BACK TO IDLE", CloseOverlay, buttonColor, 11);
    }

    private void AddSkillButton(CombatantDefinition[] roster, PrototypeCharacterProgress progress, PrototypeSkillSlot slot, Rect rect)
    {
        var level = PrototypeProgression.GetSkillLevel(progress, slot);
        AddButton(screen, rect, $"{slot} Lv{level} · {PrototypeProgression.SkillCost(progress, slot)}G", () => { message = PrototypeProgression.TryUpgradeSkill(selectedHero.DisplayName, slot) ? $"{slot} upgraded." : "Not enough Gold or max level."; BuildHeroesScreen(roster); }, buttonColor, 9);
    }

    private void AddEquipmentButton(CombatantDefinition[] roster, PrototypeCharacterProgress progress, PrototypeEquipmentSlot slot, Rect rect)
    {
        var level = PrototypeProgression.GetEquipmentLevel(progress, slot);
        AddButton(screen, rect, $"{slot}\nLv{level} · {PrototypeProgression.EquipmentCost(progress, slot)}M", () => { message = PrototypeProgression.TryUpgradeEquipment(selectedHero.DisplayName, slot) ? $"{slot} upgraded." : "Not enough Materials."; BuildHeroesScreen(roster); }, buttonColor, 9);
    }

    private void ShowGacha()
    {
        hud.SetCanvasOverlayOpen(true);
        screenType = ScreenType.Gacha;
        BuildGachaScreen(CombatPrototype.LoadRoster());
    }

    private void BuildGachaScreen(CombatantDefinition[] roster)
    {
        ClearScreen();
        AddImage(screen, new Rect(0f, 0f, 360f, 640f), background);
        AddText(screen, new Rect(20f, 14f, 320f, 32f), "GALAXY SUMMON", 20, accentColor, TextAnchor.MiddleCenter, true);
        AddText(screen, new Rect(20f, 48f, 320f, 24f), $"Tickets {PrototypeGacha.Tickets} · Owned {PrototypeGacha.OwnedCount}/{roster.Length}", 11, Color.white, TextAnchor.MiddleCenter);
        AddText(screen, new Rect(20f, 76f, 320f, 22f), "R 55% · SR 30% · SSR 12% · UR 3%", 10, Color.white, TextAnchor.MiddleCenter);
        AddText(screen, new Rect(20f, 102f, 320f, 22f), $"SSR pity {PrototypeGacha.HighRarityPityRemaining} · UR pity {PrototypeGacha.UrPityRemaining}", 10, cyanColor, TextAnchor.MiddleCenter);
        var one = AddButton(screen, new Rect(34f, 136f, 130f, 38f), "SUMMON 1 · 1 TICKET", () => { summonResults = PrototypeGacha.TrySummon(roster, 1); BuildGachaScreen(roster); }, new Color(0.5f, 0.28f, 0.08f), 10);
        one.GetComponent<Button>().interactable = PrototypeGacha.Tickets >= 1;
        var ten = AddButton(screen, new Rect(196f, 136f, 130f, 38f), "SUMMON 10 · 10 TICKETS", () => { summonResults = PrototypeGacha.TrySummon(roster, 10); BuildGachaScreen(roster); }, new Color(0.65f, 0.24f, 0.08f), 10);
        ten.GetComponent<Button>().interactable = PrototypeGacha.Tickets >= 10;
        AddImage(screen, new Rect(20f, 184f, 320f, 246f), panelColor);
        AddText(screen, new Rect(28f, 190f, 304f, 22f), "LATEST SUMMON", 12, accentColor, TextAnchor.MiddleCenter, true);
        for (var index = 0; index < summonResults.Length; index++)
        {
            var result = summonResults[index];
            AddText(screen, new Rect(32f, 216f + index * 20f, 296f, 18f), $"{result.Character.Rarity} · {result.Character.DisplayName} · {(result.IsNew ? "NEW HERO" : "+" + result.Shards + " SHARDS")}", 10, Color.white, TextAnchor.MiddleCenter);
        }
        AddImage(screen, new Rect(20f, 438f, 320f, 126f), panelColor);
        AddText(screen, new Rect(28f, 442f, 304f, 22f), "RECENT HISTORY", 11, cyanColor, TextAnchor.MiddleCenter, true);
        for (var index = 0; index < Mathf.Min(5, PrototypeGacha.History.Count); index++)
        {
            AddText(screen, new Rect(32f, 466f + index * 18f, 296f, 17f), PrototypeGacha.History[index], 9, Color.white, TextAnchor.MiddleCenter);
        }
        AddButton(
            screen,
            new Rect(12f, 584f, 88f, 40f),
            resetConfirmation ? "CONFIRM RESET" : "DEV RESET",
            ResetSave,
            resetConfirmation ? new Color(0.7f, 0.12f, 0.1f) : new Color(0.3f, 0.12f, 0.12f),
            9);
        AddButton(screen, new Rect(110f, 584f, 140f, 40f), "BACK TO IDLE", CloseOverlay, buttonColor, 11);
    }

    private void ResetSave()
    {
        if (!resetConfirmation)
        {
            resetConfirmation = true;
            BuildGachaScreen(CombatPrototype.LoadRoster());
            return;
        }

        resetConfirmation = false;
        summonResults = new PrototypeSummonResult[0];
        selectedHero = null;
        PrototypeSaveSystem.ResetAll();
        hud.ReturnToIdleBattle();
    }

    private void CloseOverlay()
    {
        if (hud == null || battle == null)
        {
            return;
        }
        resetConfirmation = false;
        hud.SetCanvasOverlayOpen(false);
        BuildBattleScreen();
    }

    private static int Scaled(int value, float multiplier)
    {
        return Mathf.RoundToInt(value * multiplier);
    }

    private static string Train(string characterName)
    {
        PrototypeProgression.AddExperience(characterName, 25);
        return "Character gained 25 XP.";
    }

    private void ClearScreen()
    {
        if (screen != null)
        {
            screen.gameObject.SetActive(false);
            Destroy(screen.gameObject);
        }
        var gameObject = new GameObject("Screen", typeof(RectTransform), typeof(CanvasGroup));
        gameObject.transform.SetParent(canvas.transform, false);
        screen = gameObject.GetComponent<RectTransform>();
        screen.anchorMin = Vector2.zero;
        screen.anchorMax = Vector2.one;
        screen.offsetMin = Vector2.zero;
        screen.offsetMax = Vector2.zero;
        screenGroup = gameObject.GetComponent<CanvasGroup>();
        screenGroup.alpha = 0f;
        screenGroup.interactable = false;
        screenGroup.blocksRaycasts = false;
    }

    private GameObject AddImage(Transform parent, Rect rect, Color color)
    {
        var gameObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        SetRect(gameObject.GetComponent<RectTransform>(), rect);
        gameObject.GetComponent<Image>().color = color;
        return gameObject;
    }

    private Text AddText(
        Transform parent,
        Rect rect,
        string value,
        int size,
        Color color,
        TextAnchor alignment,
        bool bold = false)
    {
        var gameObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        gameObject.transform.SetParent(parent, false);
        SetRect(gameObject.GetComponent<RectTransform>(), rect);
        var text = gameObject.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private GameObject AddButton(
        Transform parent,
        Rect rect,
        string label,
        Action action,
        Color color,
        int fontSize)
    {
        var gameObject = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        gameObject.transform.SetParent(parent, false);
        SetRect(gameObject.GetComponent<RectTransform>(), rect);
        gameObject.GetComponent<Image>().color = color;
        var button = gameObject.GetComponent<Button>();
        button.targetGraphic = gameObject.GetComponent<Image>();
        button.onClick.AddListener(() => action());
        var text = AddText(gameObject.transform, new Rect(0f, 0f, rect.width, rect.height), label, fontSize, Color.white, TextAnchor.MiddleCenter, true);
        text.raycastTarget = false;
        return gameObject;
    }

    private static void SetRect(RectTransform transform, Rect rect)
    {
        transform.anchorMin = new Vector2(0f, 1f);
        transform.anchorMax = new Vector2(0f, 1f);
        transform.pivot = new Vector2(0f, 1f);
        transform.anchoredPosition = new Vector2(rect.x, -rect.y);
        transform.sizeDelta = new Vector2(rect.width, rect.height);
    }
}
