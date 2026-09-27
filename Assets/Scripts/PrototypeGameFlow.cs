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

    private enum OnboardingStep
    {
        WatchBattle,
        ClaimReward,
        TrainHero,
        ChallengeBoss,
        Summon,
        Formation,
        Complete
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
    private RectTransform safeAreaRoot;
    private RectTransform portraitContent;
    private RectTransform screen;
    private CanvasGroup screenGroup;
    private ScreenType screenType;
    private PrototypeHud hud;
    private PrototypeBattle battle;
    private Text titleText;
    private Text[] resourceTexts;
    private Text timerText;
    private Text encounterText;
    private Text announcementText;
    private Text primaryStatusText;
    private Text onboardingText;
    private GameObject speedButton;
    private GameObject pauseButton;
    private Image impactFlash;
    private GameObject challengeButton;
    private GameObject activitiesPanel;
    private GameObject resultPanel;
    private Text resultText;
    private GameObject failureHeroesButton;
    private GameObject failureSquadButton;
    private bool activitiesVisible;
    private PrototypeDungeonType dungeonType;
    private int dungeonLevel = 1;
    private readonly List<CombatantDefinition> squad = new List<CombatantDefinition>(5);
    private readonly List<PrototypeFormationRow> squadRows = new List<PrototypeFormationRow>(5);
    private CombatantDefinition selectedHero;
    private PrototypeSummonResult[] summonResults = new PrototypeSummonResult[0];
    private string message = string.Empty;
    private bool resetConfirmation;
    private float onboardingBattleWatchTime;
    private float battleSpeed = 1f;
    private bool battlePaused;
    private float impactFlashRemaining;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize = new Vector2Int(-1, -1);
    private Vector2 lastCanvasSize;

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
        ResetBattleSpeed();
        hud = battleHud;
        battle = battleManager;
        onboardingBattleWatchTime = 0f;
        hud.SetCanvasOverlayOpen(false);
        BuildBattleScreen();
    }

    public void UnbindBattle(PrototypeHud battleHud)
    {
        if (hud == battleHud)
        {
            ResetBattleSpeed();
            hud = null;
            battle = null;
        }
    }

    private void Update()
    {
        ApplySafeArea();
        if (screenGroup != null && screenGroup.alpha < 1f)
        {
            screenGroup.alpha = Mathf.MoveTowards(screenGroup.alpha, 1f, Time.unscaledDeltaTime * 6f);
            screenGroup.interactable = screenGroup.alpha >= 0.95f;
            screenGroup.blocksRaycasts = screenGroup.interactable;
        }

        UpdateBattlePresentation();

        if (screenType != ScreenType.Battle || battle == null || titleText == null)
        {
            return;
        }

        if (CurrentOnboardingStep == OnboardingStep.WatchBattle)
        {
            onboardingBattleWatchTime += Time.unscaledDeltaTime;
            if (onboardingBattleWatchTime >= 3f)
            {
                AdvanceOnboarding(OnboardingStep.WatchBattle);
            }
        }
        else if (CurrentOnboardingStep == OnboardingStep.ChallengeBoss &&
                 battle.IsBossStage && !battle.IsIdleFarmMode)
        {
            AdvanceOnboarding(OnboardingStep.ChallengeBoss);
        }

        UpdateBattleScreen();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            Time.timeScale = 1f;
            instance = null;
        }
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

        var safeAreaObject = new GameObject("Safe Area", typeof(RectTransform));
        safeAreaObject.transform.SetParent(canvas.transform, false);
        safeAreaRoot = safeAreaObject.GetComponent<RectTransform>();
        safeAreaRoot.anchorMin = Vector2.zero;
        safeAreaRoot.anchorMax = Vector2.one;
        safeAreaRoot.offsetMin = Vector2.zero;
        safeAreaRoot.offsetMax = Vector2.zero;

        var contentObject = new GameObject("Portrait Content", typeof(RectTransform));
        contentObject.transform.SetParent(safeAreaRoot, false);
        portraitContent = contentObject.GetComponent<RectTransform>();
        portraitContent.anchorMin = new Vector2(0.5f, 0.5f);
        portraitContent.anchorMax = new Vector2(0.5f, 0.5f);
        portraitContent.pivot = new Vector2(0.5f, 0.5f);
        portraitContent.sizeDelta = new Vector2(360f, 640f);

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var eventObject = new GameObject("Event System");
            eventObject.transform.SetParent(transform, false);
            eventObject.AddComponent<EventSystem>();
            eventObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        Canvas.ForceUpdateCanvases();
        ApplySafeArea();
    }

    private void BuildBattleScreen()
    {
        screenType = ScreenType.Battle;
        activitiesVisible = false;
        ClearScreen();
        AddScreenBackdrop(battleBackground);

        var stageHeader = AddFramedPanel(
            screen,
            new Rect(4f, 4f, 352f, 60f),
            new Color(0.025f, 0.07f, 0.13f, 0.98f),
            cyanColor,
            "Stage Header");
        stageHeader.name = "Stage Header";
        titleText = AddText(stageHeader.transform, new Rect(8f, 3f, 336f, 25f), string.Empty, 16, cyanColor, TextAnchor.MiddleCenter, true);
        resourceTexts = new Text[5];
        var resourceLabels = new[] { "POWER", "GOLD", "XP", "MAT", "TICKET" };
        for (var index = 0; index < resourceTexts.Length; index++)
        {
            var chip = AddFramedPanel(
                stageHeader.transform,
                new Rect(5f + index * 68f, 30f, 66f, 25f),
                new Color(0.035f, 0.12f, 0.18f, 0.96f),
                index == 0 ? accentColor : cyanColor,
                "Resource Chip");
            resourceTexts[index] = AddText(
                chip.transform,
                new Rect(2f, 2f, 62f, 21f),
                resourceLabels[index],
                8,
                Color.white,
                TextAnchor.MiddleCenter,
                true);
        }

        var battlefieldHud = AddFramedPanel(
            screen,
            new Rect(4f, 66f, 352f, 412f),
            new Color(0.015f, 0.035f, 0.07f, 0.12f),
            new Color(0.2f, 0.55f, 0.7f, 0.65f),
            "Battlefield HUD");
        encounterText = AddText(battlefieldHud.transform, new Rect(8f, 4f, 138f, 44f), string.Empty, 10, Color.white, TextAnchor.MiddleLeft, true);
        speedButton = AddButton(battlefieldHud.transform, new Rect(150f, 4f, 54f, 44f), "SPEED x1", ToggleBattleSpeed, buttonColor, 8);
        pauseButton = AddButton(battlefieldHud.transform, new Rect(208f, 4f, 58f, 44f), "PAUSE", ToggleBattlePause, new Color(0.35f, 0.2f, 0.12f), 8);
        timerText = AddText(battlefieldHud.transform, new Rect(270f, 4f, 74f, 44f), string.Empty, 10, accentColor, TextAnchor.MiddleRight, true);
        announcementText = AddText(battlefieldHud.transform, new Rect(24f, 52f, 312f, 30f), string.Empty, 15, accentColor, TextAnchor.MiddleCenter, true);

        var primaryActionBar = AddFramedPanel(
            screen,
            new Rect(4f, 480f, 352f, 62f),
            new Color(0.02f, 0.055f, 0.1f, 0.99f),
            accentColor,
            "Primary Action Bar");
        primaryStatusText = AddText(primaryActionBar.transform, new Rect(10f, 8f, 112f, 48f), string.Empty, 10, Color.white, TextAnchor.MiddleCenter, true);
        challengeButton = AddButton(
            primaryActionBar.transform,
            new Rect(132f, 8f, 216f, 48f),
            "CHALLENGE",
            ChallengePendingStage,
            new Color(0.65f, 0.24f, 0.12f),
            13);

        BuildBottomNavigation();
        BuildActivitiesPanel();
        BuildResultPanel();
        BuildOnboardingPanel(280f);
        BuildBattlePresentation();
        UpdateBattleControlLabels();
        UpdateBattleScreen();
    }

    private void BuildBottomNavigation()
    {
        var navigation = AddFramedPanel(
            screen,
            new Rect(4f, 544f, 352f, 92f),
            new Color(0.02f, 0.055f, 0.1f, 1f),
            cyanColor,
            "Navigation Bar");
        if (battle.Mode == PrototypeGameMode.Idle)
        {
            AddButton(navigation.transform, new Rect(4f, 18f, 64f, 66f), "SQUAD", ShowSquad, buttonColor, 9);
            AddButton(navigation.transform, new Rect(74f, 18f, 64f, 66f), "HEROES", ShowHeroes, buttonColor, 9);
            AddButton(navigation.transform, new Rect(144f, 18f, 64f, 66f), "SUMMON", ShowGacha, new Color(0.5f, 0.28f, 0.08f), 9);
            AddButton(navigation.transform, new Rect(214f, 18f, 64f, 66f), "CLAIM", ClaimIdleRewards, buttonColor, 9);
            AddButton(navigation.transform, new Rect(284f, 18f, 64f, 66f), "MODES", ToggleActivities, buttonColor, 9);
        }
        else
        {
            AddButton(navigation.transform, new Rect(12f, 18f, 164f, 66f), "CLAIM IDLE", ClaimIdleRewards, buttonColor, 11);
            AddButton(navigation.transform, new Rect(184f, 18f, 164f, 66f), "RETURN TO IDLE", () => hud.ReturnToIdleBattle(), buttonColor, 11);
        }
    }

    private void BuildActivitiesPanel()
    {
        activitiesPanel = AddFramedPanel(
            screen,
            new Rect(24f, 148f, 312f, 228f),
            new Color(0.04f, 0.09f, 0.16f, 0.99f),
            accentColor,
            "Activities Panel");
        AddText(activitiesPanel.transform, new Rect(8f, 6f, 296f, 24f), "ACTIVITIES · IDLE KEEPS ACCUMULATING", 11, accentColor, TextAnchor.MiddleCenter, true);
        AddButton(activitiesPanel.transform, new Rect(14f, 34f, 116f, 44f), dungeonType.ToString(), CycleDungeonType, buttonColor, 10).name = "Dungeon Type";
        AddButton(activitiesPanel.transform, new Rect(138f, 34f, 44f, 44f), "-", () => { dungeonLevel = Mathf.Max(1, dungeonLevel - 1); BuildBattleScreen(); activitiesVisible = true; SetActivitiesVisible(); }, buttonColor, 14);
        AddText(activitiesPanel.transform, new Rect(184f, 34f, 48f, 44f), $"Lv {dungeonLevel}", 11, Color.white, TextAnchor.MiddleCenter).name = "Dungeon Level";
        AddButton(activitiesPanel.transform, new Rect(234f, 34f, 44f, 44f), "+", () => { dungeonLevel = Mathf.Min(99, dungeonLevel + 1); BuildBattleScreen(); activitiesVisible = true; SetActivitiesVisible(); }, buttonColor, 14);
        AddText(
            activitiesPanel.transform,
            new Rect(14f, 82f, 264f, 28f),
            $"Reward {PrototypeSession.GetDungeonReward(dungeonType, dungeonLevel)} {dungeonType}",
            10,
            Color.white,
            TextAnchor.MiddleCenter).name = "Dungeon Reward";
        AddButton(
            activitiesPanel.transform,
            new Rect(14f, 120f, 116f, 44f),
            "DUNGEON",
            () => hud.StartActivity(PrototypeGameMode.Dungeon, dungeonType, dungeonLevel),
            new Color(0.1f, 0.42f, 0.36f),
            11);
        AddButton(
            activitiesPanel.transform,
            new Rect(162f, 120f, 116f, 44f),
            "PVP 5v5",
            () => hud.StartActivity(PrototypeGameMode.PvP),
            new Color(0.48f, 0.18f, 0.18f),
            11);
        activitiesPanel.SetActive(false);
    }

    private void BuildResultPanel()
    {
        resultPanel = AddFramedPanel(
            screen,
            new Rect(34f, 150f, 292f, 278f),
            new Color(0.035f, 0.07f, 0.13f, 0.99f),
            accentColor,
            "Result Panel");
        resultText = AddText(resultPanel.transform, new Rect(12f, 18f, 268f, 126f), string.Empty, 15, accentColor, TextAnchor.MiddleCenter, true);
        failureHeroesButton = AddButton(resultPanel.transform, new Rect(16f, 156f, 124f, 44f), "HEROES", ShowHeroes, buttonColor, 11);
        failureSquadButton = AddButton(resultPanel.transform, new Rect(152f, 156f, 124f, 44f), "SQUAD", ShowSquad, buttonColor, 11);
        AddButton(resultPanel.transform, new Rect(62f, 210f, 168f, 44f), "RETURN TO IDLE", () => hud.ReturnToIdleBattle(), buttonColor, 12);
        resultPanel.SetActive(false);
    }

    private void BuildBattlePresentation()
    {
        var flashObject = AddImage(screen, new Rect(4f, 66f, 352f, 412f), Color.clear);
        flashObject.name = "Impact Flash";
        impactFlash = flashObject.GetComponent<Image>();
        impactFlash.raycastTarget = false;
        flashObject.SetActive(false);
    }

    public void ShowImpact(Color accent)
    {
        if (screenType != ScreenType.Battle || impactFlash == null)
        {
            return;
        }

        impactFlashRemaining = 0.14f;
        impactFlash.color = new Color(accent.r, accent.g, accent.b, 0.28f);
        impactFlash.gameObject.SetActive(true);
    }

    private void UpdateBattlePresentation()
    {
        if (impactFlash != null && impactFlash.gameObject.activeSelf)
        {
            impactFlashRemaining = Mathf.Max(0f, impactFlashRemaining - Time.unscaledDeltaTime);
            var color = impactFlash.color;
            color.a = 0.28f * Mathf.Clamp01(impactFlashRemaining / 0.14f);
            impactFlash.color = color;
            if (impactFlashRemaining <= 0f)
            {
                impactFlash.gameObject.SetActive(false);
            }
        }
    }

    private void ToggleBattleSpeed()
    {
        battleSpeed = battleSpeed < 1.5f ? 2f : 1f;
        if (!battlePaused)
        {
            Time.timeScale = battleSpeed;
        }
        UpdateBattleControlLabels();
    }

    private void ToggleBattlePause()
    {
        battlePaused = !battlePaused;
        Time.timeScale = battlePaused ? 0f : battleSpeed;
        UpdateBattleControlLabels();
    }

    private void ResetBattleSpeed()
    {
        battleSpeed = 1f;
        battlePaused = false;
        Time.timeScale = 1f;
        UpdateBattleControlLabels();
    }

    private void UpdateBattleControlLabels()
    {
        if (speedButton != null)
        {
            speedButton.GetComponentInChildren<Text>().text = $"SPEED x{battleSpeed:0}";
        }
        if (pauseButton != null)
        {
            pauseButton.GetComponentInChildren<Text>().text = battlePaused ? "RESUME" : "PAUSE";
        }
    }

    private void UpdateBattleScreen()
    {
        titleText.text = GetBattleTitle();
        resourceTexts[0].text = $"POWER\n{GetBattleTeamPower()}";
        resourceTexts[1].text = $"GOLD\n{PrototypeSession.Gold}";
        resourceTexts[2].text = $"XP\n{PrototypeSession.Experience}";
        resourceTexts[3].text = $"MAT\n{PrototypeSession.Materials}";
        resourceTexts[4].text = $"TICKET\n{PrototypeGacha.Tickets}";
        timerText.text = battle.TimeLimit > 0f ? $"{battle.RemainingTime:0.0}s LEFT" : $"{battle.ElapsedTime:0.0}s";
        encounterText.text = $"SQUAD {battle.LivingCount(PrototypeTeam.Allies)}/5  ·  ENEMY {battle.LivingCount(PrototypeTeam.Enemies)}/5";
        announcementText.text = battle.HasAnnouncement ? battle.Announcement : string.Empty;
        primaryStatusText.text = GetPrimaryStatus();

        challengeButton.SetActive(
            battle.Mode == PrototypeGameMode.Idle && battle.IsIdleFarmMode && !activitiesVisible && !battle.IsFinished);
        if (challengeButton.activeSelf)
        {
            challengeButton.GetComponentInChildren<Text>().text = $"CHALLENGE STAGE {PrototypeSession.IdleStage}";
        }
        SetActivitiesVisible();
        var defeat = battle.IsFinished && battle.Winner != PrototypeTeam.Allies;
        var showResult = battle.IsFinished && (battle.Mode != PrototypeGameMode.Idle || defeat);
        if (defeat && battle.Mode == PrototypeGameMode.Idle)
        {
            hud.SetCanvasOverlayOpen(true);
        }
        resultPanel.SetActive(showResult);
        failureHeroesButton.SetActive(defeat);
        failureSquadButton.SetActive(defeat);
        if (showResult)
        {
            resultText.text = defeat
                ? "DEFEAT\n\n" + hud.ResultMessage + "\nUPGRADE HEROES OR ADJUST FORMATION"
                : "VICTORY\n\n" + hud.ResultMessage;
        }
    }

    private int GetBattleTeamPower()
    {
        var power = 0;
        foreach (var unit in battle.Allies)
        {
            power += PrototypeProgression.GetPower(
                unit.MaxHealth,
                unit.Attack,
                unit.Defense,
                unit.BasicSkillLevel + unit.PassiveSkillLevel +
                unit.ActiveSkillLevel + unit.UltimateSkillLevel);
        }
        return power;
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

    private string GetPrimaryStatus()
    {
        if (battle.Mode == PrototypeGameMode.Dungeon)
        {
            return $"DUNGEON\nLV {battle.DungeonLevel}";
        }
        if (battle.Mode == PrototypeGameMode.PvP)
        {
            return "ARENA\n5V5";
        }
        return battle.IsIdleFarmMode
            ? $"FARM WAVE\n{battle.FarmWave}"
            : $"AUTO PUSH\nSTAGE {battle.Stage}";
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
        AdvanceOnboarding(OnboardingStep.ClaimReward);
    }

    private void ChallengePendingStage()
    {
        AdvanceOnboarding(OnboardingStep.ChallengeBoss);
        hud.ChallengePendingStage();
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
        AddScreenBackdrop(background);
        var header = AddFramedPanel(
            screen,
            new Rect(12f, 8f, 336f, 58f),
            new Color(0.035f, 0.1f, 0.17f, 0.98f),
            cyanColor,
            "Squad Header");
        AddText(header.transform, new Rect(8f, 4f, 320f, 28f), "BUILD YOUR 5-HERO SQUAD", 18, cyanColor, TextAnchor.MiddleCenter, true);
        AddText(header.transform, new Rect(8f, 32f, 320f, 20f), $"SELECTED {squad.Count}/5 · POWER {PrototypeProgression.GetTeamPower(squad)}", 10, Color.white, TextAnchor.MiddleCenter);
        for (var index = 0; index < roster.Length; index++)
        {
            var hero = roster[index];
            var captured = hero;
            var selected = squad.Contains(hero);
            AddButton(
                screen,
                new Rect(12f + index % 3 * 112f, 74f + index / 3 * 46f, 104f, 44f),
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
                8);
        }

        AddFramedPanel(screen, new Rect(12f, 264f, 336f, 280f), panelColor, accentColor, "Formation Panel");
        AddText(screen, new Rect(20f, 270f, 320f, 22f), "FORMATION", 13, accentColor, TextAnchor.MiddleCenter, true);
        for (var index = 0; index < squad.Count; index++)
        {
            var captured = index;
            AddText(screen, new Rect(22f, 292f + index * 44f, 196f, 44f), $"{index + 1}. {squad[index].DisplayName} · {squad[index].CombatClass}", 10, Color.white, TextAnchor.MiddleLeft);
            AddButton(
                screen,
                new Rect(226f, 292f + index * 44f, 112f, 44f),
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
            ApplySquad,
            new Color(0.1f, 0.45f, 0.34f),
            11);
        apply.GetComponent<Button>().interactable = squad.Count == 5;
        BuildOnboardingPanel(516f);
    }

    private void ApplySquad()
    {
        AdvanceOnboarding(OnboardingStep.Formation);
        hud.ApplySquad(squad.ToArray(), squadRows.ToArray());
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
        AddScreenBackdrop(background);
        var header = AddFramedPanel(
            screen,
            new Rect(12f, 8f, 336f, 58f),
            new Color(0.035f, 0.1f, 0.17f, 0.98f),
            cyanColor,
            "Heroes Header");
        AddText(header.transform, new Rect(8f, 4f, 320f, 28f), "HERO DEVELOPMENT", 18, cyanColor, TextAnchor.MiddleCenter, true);
        AddText(header.transform, new Rect(8f, 32f, 320f, 20f), $"GOLD {PrototypeSession.Gold} · XP {PrototypeSession.Experience} · MAT {PrototypeSession.Materials}", 10, Color.white, TextAnchor.MiddleCenter);
        for (var index = 0; index < roster.Length; index++)
        {
            var hero = roster[index];
            var captured = hero;
            var progress = PrototypeProgression.Get(hero.DisplayName);
            AddButton(
                screen,
                new Rect(12f + index % 3 * 112f, 72f + index / 3 * 46f, 104f, 44f),
                $"{hero.Rarity} {hero.DisplayName} · Lv{progress.level} · {progress.stars}*",
                () => { selectedHero = captured; message = string.Empty; BuildHeroesScreen(roster); },
                hero == selectedHero ? new Color(0.12f, 0.42f, 0.3f) : buttonColor,
                8);
        }

        var selectedProgress = PrototypeProgression.Get(selectedHero.DisplayName);
        AddFramedPanel(screen, new Rect(12f, 258f, 336f, 308f), panelColor, accentColor, "Hero Detail Panel");
        AddText(screen, new Rect(20f, 264f, 320f, 24f), $"{selectedHero.Rarity} · {selectedHero.DisplayName} · {selectedHero.Species} {selectedHero.CombatClass}", 13, accentColor, TextAnchor.MiddleCenter, true);
        AddText(screen, new Rect(20f, 290f, 320f, 22f), $"LV {selectedProgress.level} · XP {selectedProgress.experience}/{PrototypeProgression.ExperienceRequired(selectedProgress)} · {selectedProgress.stars}* · SHARDS {selectedProgress.shards}/{PrototypeProgression.StarCost(selectedProgress)}", 9, Color.white, TextAnchor.MiddleCenter);
        AddText(screen, new Rect(20f, 314f, 320f, 22f), $"HP {Scaled(selectedHero.MaxHealth, PrototypeProgression.HealthMultiplier(selectedProgress))} · ATK {Scaled(selectedHero.Attack, PrototypeProgression.AttackMultiplier(selectedProgress))} · DEF {Scaled(selectedHero.Defense, PrototypeProgression.DefenseMultiplier(selectedProgress))}", 10, Color.white, TextAnchor.MiddleCenter);
        AddButton(screen, new Rect(20f, 340f, 150f, 44f), "TRAIN · 25 XP", () => TrainSelectedHero(roster), buttonColor, 10);
        AddButton(screen, new Rect(190f, 340f, 150f, 44f), $"STAR UP · {PrototypeProgression.StarCost(selectedProgress)}", () => { message = PrototypeProgression.TryStarUp(selectedHero.DisplayName) ? "Star increased." : "Need more shards."; BuildHeroesScreen(roster); }, buttonColor, 10);
        AddSkillButton(roster, selectedProgress, PrototypeSkillSlot.Basic, new Rect(20f, 390f, 150f, 44f));
        AddSkillButton(roster, selectedProgress, PrototypeSkillSlot.Passive, new Rect(190f, 390f, 150f, 44f));
        AddSkillButton(roster, selectedProgress, PrototypeSkillSlot.Active, new Rect(20f, 440f, 150f, 44f));
        AddSkillButton(roster, selectedProgress, PrototypeSkillSlot.Ultimate, new Rect(190f, 440f, 150f, 44f));
        AddEquipmentButton(roster, selectedProgress, PrototypeEquipmentSlot.Weapon, new Rect(20f, 490f, 98f, 44f));
        AddEquipmentButton(roster, selectedProgress, PrototypeEquipmentSlot.Armor, new Rect(130f, 490f, 98f, 44f));
        AddEquipmentButton(roster, selectedProgress, PrototypeEquipmentSlot.Core, new Rect(240f, 490f, 98f, 44f));
        AddText(screen, new Rect(20f, 538f, 320f, 24f), message, 10, accentColor, TextAnchor.MiddleCenter);
        AddButton(screen, new Rect(110f, 584f, 140f, 44f), "BACK TO IDLE", CloseOverlay, buttonColor, 11);
        BuildOnboardingPanel(526f);
    }

    private void TrainSelectedHero(CombatantDefinition[] roster)
    {
        if (PrototypeSession.TrySpendReward(PrototypeDungeonType.Experience, 25))
        {
            message = Train(selectedHero.DisplayName);
            AdvanceOnboarding(OnboardingStep.TrainHero);
        }
        else
        {
            message = "Not enough XP.";
        }

        BuildHeroesScreen(roster);
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
        AddScreenBackdrop(background);
        var header = AddFramedPanel(
            screen,
            new Rect(12f, 8f, 336f, 116f),
            new Color(0.08f, 0.07f, 0.14f, 0.98f),
            accentColor,
            "Summon Header");
        AddText(header.transform, new Rect(8f, 4f, 320f, 32f), "GALAXY SUMMON", 20, accentColor, TextAnchor.MiddleCenter, true);
        AddText(header.transform, new Rect(8f, 38f, 320f, 22f), $"TICKETS {PrototypeGacha.Tickets} · OWNED {PrototypeGacha.OwnedCount}/{roster.Length}", 11, Color.white, TextAnchor.MiddleCenter);
        AddText(header.transform, new Rect(8f, 64f, 320f, 20f), "R 55% · SR 30% · SSR 12% · UR 3%", 10, Color.white, TextAnchor.MiddleCenter);
        AddText(header.transform, new Rect(8f, 88f, 320f, 20f), $"SSR PITY {PrototypeGacha.HighRarityPityRemaining} · UR PITY {PrototypeGacha.UrPityRemaining}", 10, cyanColor, TextAnchor.MiddleCenter);
        var one = AddButton(screen, new Rect(34f, 134f, 130f, 44f), "SUMMON 1 · 1 TICKET", () => Summon(roster, 1), new Color(0.5f, 0.28f, 0.08f), 10);
        one.GetComponent<Button>().interactable = PrototypeGacha.Tickets >= 1;
        var ten = AddButton(screen, new Rect(196f, 134f, 130f, 44f), "SUMMON 10 · 10 TICKETS", () => Summon(roster, 10), new Color(0.65f, 0.24f, 0.08f), 10);
        ten.GetComponent<Button>().interactable = PrototypeGacha.Tickets >= 10;
        AddFramedPanel(screen, new Rect(20f, 184f, 320f, 246f), panelColor, accentColor, "Summon Results Panel");
        AddText(screen, new Rect(28f, 190f, 304f, 22f), "LATEST SUMMON", 12, accentColor, TextAnchor.MiddleCenter, true);
        for (var index = 0; index < summonResults.Length; index++)
        {
            var result = summonResults[index];
            AddText(screen, new Rect(32f, 216f + index * 20f, 296f, 18f), $"{result.Character.Rarity} · {result.Character.DisplayName} · {(result.IsNew ? "NEW HERO" : "+" + result.Shards + " SHARDS")}", 10, Color.white, TextAnchor.MiddleCenter);
        }
        AddFramedPanel(screen, new Rect(20f, 438f, 320f, 126f), panelColor, cyanColor, "Summon History Panel");
        AddText(screen, new Rect(28f, 442f, 304f, 22f), "RECENT HISTORY", 11, cyanColor, TextAnchor.MiddleCenter, true);
        for (var index = 0; index < Mathf.Min(5, PrototypeGacha.History.Count); index++)
        {
            AddText(screen, new Rect(32f, 466f + index * 18f, 296f, 17f), PrototypeGacha.History[index], 9, Color.white, TextAnchor.MiddleCenter);
        }
        AddButton(
            screen,
            new Rect(12f, 584f, 88f, 44f),
            resetConfirmation ? "CONFIRM RESET" : "DEV RESET",
            ResetSave,
            resetConfirmation ? new Color(0.7f, 0.12f, 0.1f) : new Color(0.3f, 0.12f, 0.12f),
            9);
        AddButton(screen, new Rect(110f, 584f, 140f, 44f), "BACK TO IDLE", CloseOverlay, buttonColor, 11);
        BuildOnboardingPanel(526f);
    }

    private void Summon(CombatantDefinition[] roster, int count)
    {
        summonResults = PrototypeGacha.TrySummon(roster, count);
        if (summonResults.Length > 0)
        {
            AdvanceOnboarding(OnboardingStep.Summon);
        }

        BuildGachaScreen(roster);
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

    private OnboardingStep CurrentOnboardingStep =>
        (OnboardingStep)PrototypeSaveSystem.OnboardingStep;

    private void BuildOnboardingPanel(float y)
    {
        onboardingText = null;
        if (CurrentOnboardingStep == OnboardingStep.Complete)
        {
            return;
        }

        var panel = AddFramedPanel(
            screen,
            new Rect(12f, y, 336f, 54f),
            new Color(0.04f, 0.12f, 0.18f, 0.96f),
            accentColor,
            "Onboarding Panel");
        panel.GetComponent<Image>().raycastTarget = false;
        onboardingText = AddText(panel.transform, new Rect(8f, 4f, 320f, 46f), string.Empty, 11, accentColor, TextAnchor.MiddleCenter, true);
        onboardingText.raycastTarget = false;
        UpdateOnboardingPanel();
    }

    private void AdvanceOnboarding(OnboardingStep expectedStep)
    {
        PrototypeSaveSystem.AdvanceOnboarding((int)expectedStep);
        UpdateOnboardingPanel();
    }

    private void UpdateOnboardingPanel()
    {
        if (onboardingText == null)
        {
            return;
        }

        switch (CurrentOnboardingStep)
        {
            case OnboardingStep.WatchBattle:
                onboardingText.text = "1/6 · WATCH AUTO BATTLE\nCombat runs automatically.";
                break;
            case OnboardingStep.ClaimReward:
                onboardingText.text = "2/6 · CLAIM IDLE REWARDS\nTap CLAIM below.";
                break;
            case OnboardingStep.TrainHero:
                onboardingText.text = "3/6 · TRAIN A HERO\nOpen HEROES and spend 25 XP.";
                break;
            case OnboardingStep.ChallengeBoss:
                onboardingText.text = "4/6 · CHALLENGE THE BOSS\nFight the boss or tap CHALLENGE while farming.";
                break;
            case OnboardingStep.Summon:
                onboardingText.text = "5/6 · SUMMON A HERO\nOpen SUMMON and use a ticket.";
                break;
            case OnboardingStep.Formation:
                onboardingText.text = "6/6 · SET FORMATION\nOpen SQUAD, adjust a row, then apply.";
                break;
            default:
                onboardingText.transform.parent.gameObject.SetActive(false);
                break;
        }
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
            Destroy(screen.gameObject);
        }
        var gameObject = new GameObject("Screen", typeof(RectTransform), typeof(CanvasGroup));
        gameObject.transform.SetParent(portraitContent, false);
        screen = gameObject.GetComponent<RectTransform>();
        screen.anchorMin = Vector2.zero;
        screen.anchorMax = Vector2.one;
        screen.offsetMin = Vector2.zero;
        screen.offsetMax = Vector2.zero;
        screenGroup = gameObject.GetComponent<CanvasGroup>();
        screenGroup.alpha = 1f;
        screenGroup.interactable = true;
        screenGroup.blocksRaycasts = true;
    }

    private void AddScreenBackdrop(Color color)
    {
        var backdrop = AddImage(screen, new Rect(0f, 0f, 360f, 640f), color);
        backdrop.name = "Screen Backdrop";
        backdrop.GetComponent<Image>().raycastTarget = false;

        var starfield = new GameObject("UI Starfield", typeof(RectTransform));
        starfield.transform.SetParent(screen, false);
        SetRect(starfield.GetComponent<RectTransform>(), new Rect(0f, 0f, 360f, 640f));
        for (var index = 0; index < 18; index++)
        {
            var size = index % 3 == 0 ? 2f : 1f;
            var star = AddImage(
                starfield.transform,
                new Rect((index * 47 + 13) % 354, (index * 83 + 29) % 630, size, size),
                index % 4 == 0 ? accentColor : cyanColor);
            star.name = "UI Star";
            star.GetComponent<Image>().raycastTarget = false;
        }

        var frame = new GameObject("Screen Frame", typeof(RectTransform));
        frame.transform.SetParent(screen, false);
        SetRect(frame.GetComponent<RectTransform>(), new Rect(2f, 2f, 356f, 636f));
        AddFrameEdge(frame.transform, new Rect(0f, 0f, 356f, 2f), cyanColor);
        AddFrameEdge(frame.transform, new Rect(0f, 634f, 356f, 2f), cyanColor);
        AddFrameEdge(frame.transform, new Rect(0f, 0f, 2f, 636f), cyanColor);
        AddFrameEdge(frame.transform, new Rect(354f, 0f, 2f, 636f), cyanColor);
    }

    private void AddFrameEdge(Transform parent, Rect rect, Color color, float alpha = 0.42f)
    {
        var edge = AddImage(parent, rect, new Color(color.r, color.g, color.b, alpha));
        edge.name = "Frame Edge";
        edge.GetComponent<Image>().raycastTarget = false;
    }

    private GameObject AddFramedPanel(Transform parent, Rect rect, Color color, Color accent, string name)
    {
        var panel = AddImage(parent, rect, color);
        panel.name = name;
        AddFrameEdge(panel.transform, new Rect(0f, 0f, rect.width, 1f), accent, 0.58f);
        AddFrameEdge(panel.transform, new Rect(0f, rect.height - 1f, rect.width, 1f), accent, 0.58f);
        AddFrameEdge(panel.transform, new Rect(0f, 0f, 1f, rect.height), accent, 0.58f);
        AddFrameEdge(panel.transform, new Rect(rect.width - 1f, 0f, 1f, rect.height), accent, 0.58f);
        var rail = AddImage(panel.transform, new Rect(0f, 0f, rect.width, 2f), accent);
        rail.name = "Accent Rail";
        rail.GetComponent<Image>().raycastTarget = false;
        return panel;
    }

    private RectTransform AddMeter(
        Transform parent,
        Rect rect,
        Color backgroundColor,
        Color fillColor,
        string name)
    {
        var meter = AddImage(parent, rect, backgroundColor);
        meter.name = name;
        meter.GetComponent<Image>().raycastTarget = false;
        var fill = AddImage(meter.transform, new Rect(1f, 1f, rect.width - 2f, rect.height - 2f), fillColor);
        fill.name = name + " Fill";
        fill.GetComponent<Image>().raycastTarget = false;
        return fill.GetComponent<RectTransform>();
    }

    private static void SetMeter(RectTransform fill, float value, float width)
    {
        fill.sizeDelta = new Vector2(width * Mathf.Clamp01(value), fill.sizeDelta.y);
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
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(8, size - 3);
        text.resizeTextMaxSize = size;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        if (bold)
        {
            var shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.78f);
            shadow.effectDistance = new Vector2(1f, -1f);
        }
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
        var image = gameObject.GetComponent<Image>();
        image.color = Color.white;
        var outline = gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.25f, 0.82f, 1f, 0.45f);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = false;
        var button = gameObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        colors.pressedColor = ScaleColor(color, 0.68f);
        colors.selectedColor = Color.Lerp(color, accentColor, 0.16f);
        colors.disabledColor = new Color(color.r * 0.45f, color.g * 0.45f, color.b * 0.45f, 0.55f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.06f;
        button.colors = colors;
        image.canvasRenderer.SetColor(colors.normalColor);
        button.onClick.AddListener(() => action());
        var rail = AddImage(gameObject.transform, new Rect(0f, 0f, 3f, rect.height), accentColor);
        rail.name = "Button Accent";
        rail.GetComponent<Image>().raycastTarget = false;
        var text = AddText(gameObject.transform, new Rect(0f, 0f, rect.width, rect.height), label, fontSize, Color.white, TextAnchor.MiddleCenter, true);
        text.raycastTarget = false;
        return gameObject;
    }

    private static Color ScaleColor(Color color, float scale)
    {
        return new Color(color.r * scale, color.g * scale, color.b * scale, color.a);
    }

    private static void SetRect(RectTransform transform, Rect rect)
    {
        transform.anchorMin = new Vector2(0f, 1f);
        transform.anchorMax = new Vector2(0f, 1f);
        transform.pivot = new Vector2(0f, 1f);
        transform.anchoredPosition = new Vector2(rect.x, -rect.y);
        transform.sizeDelta = new Vector2(rect.width, rect.height);
    }

    public static Rect NormalizeSafeArea(Rect safeArea, Vector2 screenSize)
    {
        if (screenSize.x <= 0f || screenSize.y <= 0f)
        {
            return new Rect(0f, 0f, 1f, 1f);
        }

        var xMin = Mathf.Clamp01(safeArea.xMin / screenSize.x);
        var yMin = Mathf.Clamp01(safeArea.yMin / screenSize.y);
        var xMax = Mathf.Clamp01(safeArea.xMax / screenSize.x);
        var yMax = Mathf.Clamp01(safeArea.yMax / screenSize.y);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private void ApplySafeArea()
    {
        if (canvas == null || safeAreaRoot == null || portraitContent == null)
        {
            return;
        }

        var canvasSize = ((RectTransform)canvas.transform).rect.size;
        if (canvasSize.x <= 0f || canvasSize.y <= 0f)
        {
            return;
        }

        var currentSafeArea = Screen.safeArea;
        var currentScreenSize = new Vector2Int(Screen.width, Screen.height);
        if (currentSafeArea == lastSafeArea && currentScreenSize == lastScreenSize && canvasSize == lastCanvasSize)
        {
            return;
        }

        lastSafeArea = currentSafeArea;
        lastScreenSize = currentScreenSize;
        lastCanvasSize = canvasSize;
        var normalized = NormalizeSafeArea(currentSafeArea, currentScreenSize);
        safeAreaRoot.anchorMin = normalized.min;
        safeAreaRoot.anchorMax = normalized.max;
        safeAreaRoot.offsetMin = Vector2.zero;
        safeAreaRoot.offsetMax = Vector2.zero;

        var safeCanvasSize = Vector2.Scale(canvasSize, normalized.size);
        var scale = Mathf.Min(safeCanvasSize.x / 360f, safeCanvasSize.y / 640f);
        portraitContent.localScale = Vector3.one * Mathf.Max(0.01f, scale);
    }
}
