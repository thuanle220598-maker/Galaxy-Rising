using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class BattleHomeUiPlayModeTests
{
    private const string FireGodName = "Fire God Heavenly Demon";
    private const string AureliaName = "Aurelia";
    private const string KronosName = "Kronos";
    private const string SaveVersionKey = "Prototype.SaveVersion";
    private const string OnboardingStepKey = "Prototype.OnboardingStep";
    private const string IdleStageKey = "Prototype.IdleStage";
    private const string IdleBossPendingKey = "Prototype.IdleBossPending";
    private const string LastIdleClaimKey = "Prototype.LastIdleClaim";
    private const string GoldKey = "Prototype.Gold";
    private const string ExperienceKey = "Prototype.Experience";
    private const string MaterialsKey = "Prototype.Materials";
    private const string SquadKey = "Prototype.Squad";
    private const string ProgressionKey = "Prototype.CharacterProgression";
    private const string GachaKey = "Prototype.Gacha";
    private static readonly string[] IntKeys =
    {
        SaveVersionKey,
        OnboardingStepKey,
        IdleStageKey,
        IdleBossPendingKey,
        LastIdleClaimKey,
        GoldKey,
        ExperienceKey,
        MaterialsKey
    };
    private static readonly string[] StringKeys = { SquadKey, ProgressionKey, GachaKey };
    private readonly Dictionary<string, int> savedInts = new Dictionary<string, int>();
    private readonly HashSet<string> existingInts = new HashSet<string>();
    private readonly Dictionary<string, string> savedStrings = new Dictionary<string, string>();
    private readonly HashSet<string> existingStrings = new HashSet<string>();

    [SetUp]
    public void SetUpOnboardingStep()
    {
        savedInts.Clear();
        existingInts.Clear();
        foreach (var key in IntKeys)
        {
            if (PlayerPrefs.HasKey(key))
            {
                existingInts.Add(key);
                savedInts[key] = PlayerPrefs.GetInt(key);
            }
        }

        savedStrings.Clear();
        existingStrings.Clear();
        foreach (var key in StringKeys)
        {
            if (PlayerPrefs.HasKey(key))
            {
                existingStrings.Add(key);
                savedStrings[key] = PlayerPrefs.GetString(key);
            }
        }
        PlayerPrefs.SetInt(SaveVersionKey, 6);
        PlayerPrefs.SetInt(OnboardingStepKey, 1);
        PlayerPrefs.Save();
    }

    [TearDown]
    public void RestorePlayerPrefs()
    {
        Time.timeScale = 1f;
        foreach (var key in IntKeys)
        {
            RestoreInt(key, existingInts.Contains(key), savedInts.ContainsKey(key) ? savedInts[key] : 0);
        }

        foreach (var key in StringKeys)
        {
            RestoreString(
                key,
                existingStrings.Contains(key),
                savedStrings.ContainsKey(key) ? savedStrings[key] : string.Empty);
        }
        PlayerPrefs.Save();

        var session = System.Type.GetType("PrototypeSession, Assembly-CSharp");
        session?.GetMethod("ResetRuntime", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
        System.Type.GetType("PrototypeProgression, Assembly-CSharp")
            ?.GetMethod("ResetCache", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
        System.Type.GetType("PrototypeGacha, Assembly-CSharp")
            ?.GetMethod("ResetCache", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
    }

    [UnityTest]
    public IEnumerator BattleHomeHasOrderedReadableSectionsAndGuidesTheNextAction()
    {
        SceneManager.LoadScene("SampleScene");
        yield return null;
        var combatPrototype = System.Type.GetType("CombatPrototype, Assembly-CSharp");
        Assert.That(combatPrototype, Is.Not.Null);
        var create = combatPrototype.GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(create, Is.Not.Null);
        create.Invoke(null, null);
        yield return null;

        var safeAreaObject = GameObject.Find("Safe Area");
        var portraitContentObject = GameObject.Find("Portrait Content");
        Assert.That(safeAreaObject, Is.Not.Null);
        Assert.That(portraitContentObject, Is.Not.Null);
        var safeArea = safeAreaObject.GetComponent<RectTransform>();
        var portraitContent = portraitContentObject.GetComponent<RectTransform>();
        Assert.That(safeArea, Is.Not.Null);
        Assert.That(portraitContent, Is.Not.Null);
        Assert.That(portraitContent.sizeDelta, Is.EqualTo(new Vector2(360f, 640f)));

        Assert.That(GameObject.Find("Starfield"), Is.Not.Null);
        Assert.That(GameObject.Find("Nebula Left"), Is.Not.Null);
        Assert.That(GameObject.Find("Battle Deck"), Is.Not.Null);
        Assert.That(HasAudioClip(Camera.main, "Pixel Battle Loop"), Is.True);

        foreach (var heroName in new[] { FireGodName, "Ion", AureliaName, "Lyra", KronosName })
        {
            var body = GameObject.Find(heroName).transform.Find("Body").GetComponent<SpriteRenderer>();
            Assert.That(body.sprite.texture.name, Is.Not.Empty);
            Assert.That(body.sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(body.transform.localScale.x, Is.EqualTo(2.31f).Within(0.001f));
            Assert.That(body.transform.localScale.y, Is.EqualTo(2.31f).Within(0.001f));
            var healthBarY = body.transform.parent.Find("Health Background").localPosition.y;
            var energyBarY = body.transform.parent.Find("Energy Background").localPosition.y;
            var statusIconsY = body.transform.parent.Find("Status Icons").localPosition.y;
            Assert.That(healthBarY, Is.GreaterThan(body.transform.localScale.y * 0.5f));
            Assert.That(energyBarY, Is.GreaterThan(body.transform.localScale.y * 0.45f));
            Assert.That(statusIconsY, Is.GreaterThan(healthBarY));
            Assert.That(body.transform.parent.Find("Body Shadow"), Is.Not.Null);
            Assert.That(body.transform.parent.Find("Ultimate Ready"), Is.Not.Null);
            Assert.That(body.transform.parent.GetComponent<SortingGroup>(), Is.Not.Null);
        }

        var allyPositions = new[] { FireGodName, "Ion", AureliaName, "Lyra", KronosName };
        System.Array.Sort(allyPositions, (left, right) =>
            GameObject.Find(left).transform.position.y.CompareTo(GameObject.Find(right).transform.position.y));
        for (var index = 1; index < allyPositions.Length; index++)
        {
            var previousY = GameObject.Find(allyPositions[index - 1]).transform.position.y;
            var currentY = GameObject.Find(allyPositions[index]).transform.position.y;
            Assert.That(currentY - previousY, Is.GreaterThan(1.6f));
        }

        var effectType = System.Type.GetType("PrototypeEffect, Assembly-CSharp");
        Assert.That(effectType, Is.Not.Null);
        var spawnEffect = effectType.GetMethod("Spawn", BindingFlags.Public | BindingFlags.Static);
        Assert.That(spawnEffect, Is.Not.Null);
        var novaBody = GameObject.Find(FireGodName).transform.Find("Body").GetComponent<SpriteRenderer>();
        spawnEffect.Invoke(null, new object[] { novaBody.sprite, Vector3.zero, Color.cyan, 0.2f, 1f, 0.5f });
        var skillEffect = GameObject.Find("Skill Effect");
        Assert.That(skillEffect.GetComponentsInChildren<SpriteRenderer>(), Has.Length.GreaterThanOrEqualTo(5));
        Object.Destroy(skillEffect);

        var header = RequireSection("Stage Header", 52f);
        var battlefield = RequireSection("Battlefield HUD", 400f);
        var actions = RequireSection("Primary Action Bar", 44f);
        var navigation = RequireSection("Navigation Bar", 80f);

        Assert.Less(Top(header), Top(battlefield));
        Assert.Less(Top(battlefield), Top(actions));
        Assert.Less(Top(actions), Top(navigation));
        Assert.That(GameObject.Find("Hero Bar"), Is.Null);
        Assert.That(GameObject.Find("Selected Hero Status"), Is.Null);
        Assert.That(HasText(header, "POWER"), Is.True);
        Assert.That(GameObject.Find("UI Starfield"), Is.Not.Null);
        Assert.That(CountNamedChildren(header, "Resource Chip"), Is.EqualTo(5));
        var speedButton = FindButton(battlefield, "SPEED x1");
        var pauseButton = FindButton(battlefield, "PAUSE");
        Assert.That(speedButton.GetComponent<RectTransform>().sizeDelta.x, Is.GreaterThanOrEqualTo(44f));
        Assert.That(speedButton.GetComponent<RectTransform>().sizeDelta.y, Is.GreaterThanOrEqualTo(44f));
        Assert.That(pauseButton.GetComponent<RectTransform>().sizeDelta.x, Is.GreaterThanOrEqualTo(44f));
        Assert.That(pauseButton.GetComponent<RectTransform>().sizeDelta.y, Is.GreaterThanOrEqualTo(44f));
        speedButton.onClick.Invoke();
        Assert.That(Time.timeScale, Is.EqualTo(2f));
        Assert.That(speedButton.GetComponentInChildren<Text>().text, Is.EqualTo("SPEED x2"));
        speedButton.onClick.Invoke();
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        pauseButton.onClick.Invoke();
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(pauseButton.GetComponentInChildren<Text>().text, Is.EqualTo("RESUME"));
        pauseButton.onClick.Invoke();
        Assert.That(Time.timeScale, Is.EqualTo(1f));

        var screen = GameObject.Find("Screen").GetComponent<RectTransform>();
        Assert.That(FindNamedChild(screen, "Ultimate Cut-In"), Is.Null);
        Assert.That(FindNamedChild(screen, "Impact Flash"), Is.Not.Null);

        Assert.That(battlefield.GetComponent<Outline>(), Is.Null);
        var nova = GameObject.Find(FireGodName);
        var statusIcons = nova.transform.Find("Status Icons");
        Assert.That(statusIcons, Is.Not.Null);
        Assert.That(statusIcons.childCount, Is.EqualTo(12));
        foreach (var icon in statusIcons.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Assert.That(icon.sprite.texture.width, Is.EqualTo(12));
            Assert.That(icon.sprite.texture.height, Is.EqualTo(12));
            Assert.That(icon.sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
        }

        var combatantType = System.Type.GetType("PrototypeCombatant, Assembly-CSharp");
        var grantShield = combatantType.GetMethod("GrantShield", BindingFlags.NonPublic | BindingFlags.Instance);
        grantShield.Invoke(nova.GetComponent(combatantType), new object[] { 10, 3f });
        yield return null;
        Assert.That(statusIcons.Find("Shield Icon").GetComponent<SpriteRenderer>().enabled, Is.True);

        var squadButton = FindButton(navigation, "SQUAD");
        Assert.That(squadButton.transition, Is.EqualTo(Selectable.Transition.ColorTint));
        Assert.That(squadButton.colors.pressedColor, Is.Not.EqualTo(squadButton.colors.normalColor));
        AssertMinimumTouchTargets(navigation);

        var onboarding = GameObject.Find("Onboarding Panel");
        Assert.That(onboarding, Is.Not.Null);
        Assert.That(onboarding.GetComponentInChildren<Text>().text, Does.Contain("CLAIM IDLE REWARDS"));

        FindButton(navigation, "CLAIM").onClick.Invoke();
        yield return null;

        Assert.That(PlayerPrefs.GetInt(OnboardingStepKey), Is.EqualTo(2));
        Assert.That(onboarding.GetComponentInChildren<Text>().text, Does.Contain("TRAIN A HERO"));

        FindButton(navigation, "SQUAD").onClick.Invoke();
        yield return null;

        var squadScreen = GameObject.Find("Screen").GetComponent<RectTransform>();
        Assert.That(HasText(squadScreen, "SELECTED 5/5"), Is.True);
        Assert.That(HasText(squadScreen, "POWER"), Is.True);
        AssertMinimumTouchTargets(squadScreen);
        FindButton(squadScreen, "BACK").onClick.Invoke();
        yield return null;

        var battleType = System.Type.GetType("PrototypeBattle, Assembly-CSharp");
        var teamType = System.Type.GetType("PrototypeTeam, Assembly-CSharp");
        Assert.That(battleType, Is.Not.Null);
        Assert.That(teamType, Is.Not.Null);
        var battle = GameObject.Find("Combat Prototype").GetComponentInChildren(battleType);
        var finish = battleType.GetMethod("Finish", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(battle, Is.Not.Null);
        Assert.That(finish, Is.Not.Null);
        finish.Invoke(battle, new[] { System.Enum.Parse(teamType, "Enemies") });
        yield return null;
        yield return null;

        var resultPanel = GameObject.Find("Result Panel");
        Assert.That(resultPanel, Is.Not.Null);
        Assert.That(HasText(resultPanel.GetComponent<RectTransform>(), "UPGRADE HEROES"), Is.True);
        Assert.That(FindButton(resultPanel.GetComponent<RectTransform>(), "HEROES"), Is.Not.Null);
        Assert.That(FindButton(resultPanel.GetComponent<RectTransform>(), "SQUAD"), Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator OpeningAnOverlayDoesNotRevealTheBattleScreen()
    {
        SceneManager.LoadScene("SampleScene");
        yield return null;
        var combatPrototype = System.Type.GetType("CombatPrototype, Assembly-CSharp");
        combatPrototype.GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        yield return null;

        var battleScreen = GameObject.Find("Screen").GetComponent<RectTransform>();
        FindButton(battleScreen, "HEROES").onClick.Invoke();

        var overlay = battleScreen.parent.GetChild(battleScreen.parent.childCount - 1).GetComponent<CanvasGroup>();
        Assert.That(overlay.transform, Is.Not.SameAs(battleScreen));
        Assert.That(battleScreen.gameObject.activeSelf, Is.True,
            "Keep the previous screen visible until Unity renders its replacement.");
        Assert.That(overlay.alpha, Is.EqualTo(1f));
        Assert.That(overlay.interactable, Is.True);
        Assert.That(overlay.blocksRaycasts, Is.True);
        foreach (var button in overlay.GetComponentsInChildren<Button>())
        {
            Assert.That(
                Vector4.Distance(button.targetGraphic.canvasRenderer.GetColor(), button.colors.normalColor),
                Is.LessThan(0.001f),
                $"{button.GetComponentInChildren<Text>().text} must start at its normal tint.");
        }
        Assert.That(GameObject.Find("Screen Frame"), Is.Not.Null);
        Assert.That(GameObject.Find("UI Starfield"), Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator SkillAnimationDoesNotPauseMovement()
    {
        SceneManager.LoadScene("SampleScene");
        yield return null;
        var combatPrototype = System.Type.GetType("CombatPrototype, Assembly-CSharp");
        combatPrototype.GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        yield return null;

        var nova = GameObject.Find(FireGodName);
        var combatantType = System.Type.GetType("PrototypeCombatant, Assembly-CSharp");
        var startActiveSkill = combatantType.GetMethod(
            "StartActiveSkill",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(nova, Is.Not.Null);
        Assert.That(startActiveSkill, Is.Not.Null);

        var startX = nova.transform.position.x;
        startActiveSkill.Invoke(nova.GetComponent(combatantType), null);
        yield return null;

        Assert.That(nova.transform.position.x, Is.GreaterThan(startX));
    }

    [UnityTest]
    public IEnumerator DeadTargetDuringWindupDoesNotReceiveAnInstantRetargetedHit()
    {
        SceneManager.LoadScene("SampleScene");
        yield return null;
        var combatPrototype = System.Type.GetType("CombatPrototype, Assembly-CSharp");
        combatPrototype.GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        yield return null;

        var combatantType = System.Type.GetType("PrototypeCombatant, Assembly-CSharp");
        var nova = GameObject.Find(FireGodName).GetComponent(combatantType);
        var target = GetInstanceProperty<object>(nova, "Target");
        Assert.That(target, Is.Not.Null);

        combatantType.GetMethod("StartBasicAttack", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(nova, null);
        target.GetType().GetMethod("TakeDamage", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(target, new object[] { 999999, false });
        combatantType.GetField("actionHitRemaining", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(nova, 0f);
        var damageBefore = GetInstanceProperty<int>(nova, "DamageDealt");

        combatantType.GetMethod("UpdatePendingAction", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(nova, null);

        Assert.That(GetInstanceProperty<int>(nova, "DamageDealt"), Is.EqualTo(damageBefore));
    }

    [TestCase(360, 640)]
    [TestCase(720, 1280)]
    [TestCase(1080, 1920)]
    public void SafeAreaNormalizesTargetPortraitResolutions(int width, int height)
    {
        var gameFlow = System.Type.GetType("PrototypeGameFlow, Assembly-CSharp");
        Assert.That(gameFlow, Is.Not.Null);
        var normalize = gameFlow.GetMethod("NormalizeSafeArea", BindingFlags.Public | BindingFlags.Static);
        Assert.That(normalize, Is.Not.Null);

        var safe = new Rect(0f, height * 0.05f, width, height * 0.9f);
        var normalized = (Rect)normalize.Invoke(null, new object[] { safe, new Vector2(width, height) });

        Assert.That(normalized.x, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(normalized.y, Is.EqualTo(0.05f).Within(0.0001f));
        Assert.That(normalized.width, Is.EqualTo(1f).Within(0.0001f));
        Assert.That(normalized.height, Is.EqualTo(0.9f).Within(0.0001f));
    }

    [TestCase("SampleScene", "Assets/Scenes/SampleScene.unity", true)]
    [TestCase("0", "Temp/__Backupscenes/0.backup", true)]
    [TestCase("OtherScene", "Assets/Scenes/OtherScene.unity", false)]
    public void CombatBootstrapRecognizesTheGameSceneAndItsUnityBackup(
        string sceneName,
        string scenePath,
        bool expected)
    {
        var combatPrototype = System.Type.GetType("CombatPrototype, Assembly-CSharp");
        Assert.That(combatPrototype, Is.Not.Null);
        var isPrototypeScene = combatPrototype.GetMethod(
            "IsPrototypeScene",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(isPrototypeScene, Is.Not.Null);

        Assert.That(isPrototypeScene.Invoke(null, new object[] { sceneName, scenePath }), Is.EqualTo(expected));
    }

    [Test]
    public void TeamPowerUsesCombatStatsAndSkillLevels()
    {
        var progression = System.Type.GetType("PrototypeProgression, Assembly-CSharp");
        Assert.That(progression, Is.Not.Null);
        var getPower = progression.GetMethod(
            "GetPower",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(int), typeof(int), typeof(int), typeof(int) },
            null);
        Assert.That(getPower, Is.Not.Null);

        var basePower = (int)getPower.Invoke(null, new object[] { 100, 20, 5, 4 });
        var upgradedPower = (int)getPower.Invoke(null, new object[] { 120, 25, 7, 8 });

        Assert.That(basePower, Is.EqualTo(420));
        Assert.That(upgradedPower, Is.GreaterThan(basePower));
    }

    [Test]
    public void TypedDamageAppliesDefenseResistanceAndTrueDamage()
    {
        var fireCombat = System.Type.GetType("PrototypeFireCombat, Assembly-CSharp");
        var damageType = System.Type.GetType("PrototypeDamageType, Assembly-CSharp");
        Assert.That(fireCombat, Is.Not.Null);
        Assert.That(damageType, Is.Not.Null);

        Assert.That(InvokeStatic(fireCombat, "CalculateDamage", 100, 20, 1f,
            System.Enum.Parse(damageType, "Physical"), 0f, 0f), Is.EqualTo(80));
        Assert.That(InvokeStatic(fireCombat, "CalculateDamage", 100, 20, 1f,
            System.Enum.Parse(damageType, "Fire"), -0.15f, 0f), Is.EqualTo(92));
        Assert.That(InvokeStatic(fireCombat, "CalculateDamage", 100, 999, 1f,
            System.Enum.Parse(damageType, "True"), 0.9f, 0f), Is.EqualTo(100));
        Assert.That(InvokeStatic(fireCombat, "CalculateDamage", 100, 0, 1f,
            System.Enum.Parse(damageType, "Fire"), -5f, 0f), Is.EqualTo(200));
        Assert.That(InvokeStatic(fireCombat, "CalculateDamage", 100, 0, 1f,
            System.Enum.Parse(damageType, "Fire"), 5f, 0f), Is.EqualTo(10));
    }

    [Test]
    public void SkillDefinitionSupportsThreeOptionalPassives()
    {
        var skillType = System.Type.GetType("SkillDefinition, Assembly-CSharp");
        var fireGod = Resources.Load("Skills/Nova", skillType);
        Assert.That(GetInstanceProperty<object>(fireGod, "Passive"), Is.Not.Null);
        Assert.That(GetInstanceProperty<object>(fireGod, "Passive2"), Is.Not.Null);
        Assert.That(GetInstanceProperty<object>(fireGod, "Passive3"), Is.Not.Null);
        Assert.That(GetInstanceProperty<string>(fireGod, "Summary"), Does.Contain("Ignition Core"));
        Assert.That(GetInstanceProperty<string>(fireGod, "Summary"), Does.Contain("Thermal Resonance"));
        Assert.That(GetInstanceProperty<string>(fireGod, "Summary"), Does.Contain("Everburning Embers"));

        var ion = Resources.Load("Skills/Ion", skillType);
        Assert.That(GetInstanceProperty<object>(ion, "Passive2"), Is.Null);
        Assert.That(GetInstanceProperty<object>(ion, "Passive3"), Is.Null);
    }

    [Test]
    public void AureliaReplacesAstraInPlaceWithImportedAnimationAndWaterVfx()
    {
        var definitionType = System.Type.GetType("CombatantDefinition, Assembly-CSharp");
        var skillType = System.Type.GetType("SkillDefinition, Assembly-CSharp");
        var pixelArtType = System.Type.GetType("PrototypePixelArt, Assembly-CSharp");
        var definition = Resources.Load("Combatants/Allies/Astra", definitionType);
        var skill = Resources.Load("Skills/Astra", skillType);
        Assert.That(definition, Is.Not.Null);
        Assert.That(skill, Is.Not.Null);
        Assert.That(GetInstanceProperty<string>(definition, "DisplayName"), Is.EqualTo(AureliaName));
        Assert.That(GetInstanceProperty<object>(definition, "Species").ToString(), Is.EqualTo("Dragon"));
        Assert.That(GetInstanceProperty<object>(definition, "CombatClass").ToString(), Is.EqualTo("Support"));
        Assert.That(GetInstanceProperty<string>(skill, "Summary"), Does.Contain("Oceanic Scales"));
        Assert.That(GetInstanceProperty<string>(skill, "Summary"), Does.Contain("Tidal Cleansing"));
        Assert.That(GetInstanceProperty<string>(skill, "Summary"), Does.Contain("Dragon Pulse Aura"));

        var create = pixelArtType.GetMethod(
            "Create", BindingFlags.Public | BindingFlags.Static, null,
            new[] { definitionType, typeof(bool) }, null);
        var sprites = create.Invoke(null, new[] { definition, (object)false });
        Assert.That(GetSpriteFrames(sprites, "Idle"), Has.Length.EqualTo(6));
        Assert.That(GetSpriteFrames(sprites, "Run"), Has.Length.EqualTo(8));
        Assert.That(GetSpriteFrames(sprites, "Attack"), Has.Length.EqualTo(6));
        Assert.That(GetSpriteFrames(sprites, "Skill"), Has.Length.EqualTo(10));
        Assert.That(GetSpriteFrames(sprites, "Ultimate"), Has.Length.EqualTo(12));
        Assert.That(GetSpriteFrames(sprites, "Hit"), Has.Length.EqualTo(4));
        Assert.That(GetSpriteFrames(sprites, "Death"), Has.Length.EqualTo(8));

        var effects = new Dictionary<string, int>
        {
            { "AureliaWaterSerpent", 8 }, { "AureliaHydroBead", 6 },
            { "AureliaCleansingRing", 8 }, { "AureliaDragonAura", 8 },
            { "AureliaDragonPressure", 10 }, { "AureliaDomain", 12 },
            { "AureliaShieldLoop", 8 }, { "AureliaShieldBreak", 8 },
            { "AureliaUltimateDragon", 12 }, { "AureliaBlessingImpact", 8 }
        };
        foreach (var effect in effects)
        {
            Assert.That(Resources.LoadAll<Sprite>($"VFX/Aurelia/{effect.Key}"),
                Has.Length.EqualTo(effect.Value), effect.Key);
        }
    }

    [UnityTest]
    public IEnumerator AureliaHydroBeadsCleanseAndAegisBlocksControl()
    {
        SceneManager.LoadScene("SampleScene");
        yield return null;
        var combatPrototype = System.Type.GetType("CombatPrototype, Assembly-CSharp");
        var combatantType = System.Type.GetType("PrototypeCombatant, Assembly-CSharp");
        combatPrototype.GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        yield return null;
        Time.timeScale = 0f;

        var aurelia = GameObject.Find(AureliaName).GetComponent(combatantType);
        var ally = GameObject.Find("Ion").GetComponent(combatantType);
        Assert.That(aurelia, Is.Not.Null);
        Assert.That(ally, Is.Not.Null);

        InvokeInstance(ally, "ApplyMovementSlow", 0.5f, 2f);
        Assert.That(GetInstanceProperty<string>(ally, "StatusSummary"), Does.Contain("Slow"));
        var previousEnergy = GetInstanceProperty<float>(ally, "Energy");
        InvokeInstance(aurelia, "ResolveHydroBead", ally);
        Assert.That(GetInstanceProperty<float>(ally, "Energy"), Is.EqualTo(previousEnergy + 3f));
        Assert.That(GetInstanceProperty<int>(ally, "CurrentShield"), Is.GreaterThan(0));
        Assert.That(GetInstanceProperty<string>(ally, "StatusSummary"), Does.Not.Contain("Slow"));

        SetField(aurelia, "activeSkillCooldown", 5f);
        InvokeInstance(aurelia, "ResolveHydroBead", aurelia);
        Assert.That(GetInstanceProperty<float>(aurelia, "ActiveSkillCooldown"), Is.EqualTo(4f));
        Assert.That(GetInstanceProperty<float>(aurelia, "MovementSpeedMultiplier"), Is.EqualTo(1.15f).Within(0.001f));

        InvokeInstance(aurelia, "CastDragonRealm");
        var domain = GameObject.Find("Long Mach Domain");
        Assert.That(domain, Is.Not.Null);
        var beforeAbsorb = GetInstanceProperty<float>(domain.GetComponent(
            System.Type.GetType("PrototypeWaterDomain, Assembly-CSharp")), "Remaining");
        InvokeInstance(domain.GetComponent(System.Type.GetType("PrototypeWaterDomain, Assembly-CSharp")), "AbsorbBead");
        Assert.That(GetInstanceProperty<float>(domain.GetComponent(
            System.Type.GetType("PrototypeWaterDomain, Assembly-CSharp")), "Remaining"),
            Is.EqualTo(beforeAbsorb + 1f));

        InvokeInstance(aurelia, "CastDraconianAegis");
        InvokeInstance(ally, "ApplyStun", 2f);
        Assert.That(GetInstanceProperty<string>(ally, "StatusSummary"), Does.Not.Contain("Stun"));
        Assert.That(GetInstanceProperty<float>(ally, "MovementSpeedMultiplier"), Is.GreaterThanOrEqualTo(1.2f));
    }

    [Test]
    public void KronosReplacesBrakkInPlaceWithImportedAnimationAndVoidVfx()
    {
        var definitionType = System.Type.GetType("CombatantDefinition, Assembly-CSharp");
        var skillType = System.Type.GetType("SkillDefinition, Assembly-CSharp");
        var pixelArtType = System.Type.GetType("PrototypePixelArt, Assembly-CSharp");
        var definition = Resources.Load("Combatants/Allies/Brakk", definitionType);
        var skill = Resources.Load("Skills/Brakk", skillType);
        Assert.That(definition, Is.Not.Null);
        Assert.That(skill, Is.Not.Null);
        Assert.That(GetInstanceProperty<string>(definition, "DisplayName"), Is.EqualTo(KronosName));
        Assert.That(GetInstanceProperty<object>(definition, "Species").ToString(), Is.EqualTo("Cosmic"));
        Assert.That(GetInstanceProperty<object>(definition, "CombatClass").ToString(), Is.EqualTo("Tanker"));
        Assert.That(GetInstanceProperty<object>(definition, "SkillKit").ToString(), Is.EqualTo("Brakk"));
        Assert.That(GetInstanceProperty<string>(skill, "Summary"), Does.Contain("Gravitational Crust"));
        Assert.That(GetInstanceProperty<string>(skill, "Summary"), Does.Contain("Void Parasite"));
        Assert.That(GetInstanceProperty<string>(skill, "Summary"), Does.Contain("Devourer's Constitution"));

        var create = pixelArtType.GetMethod(
            "Create", BindingFlags.Public | BindingFlags.Static, null,
            new[] { definitionType, typeof(bool) }, null);
        var sprites = create.Invoke(null, new[] { definition, (object)false });
        Assert.That(GetSpriteFrames(sprites, "Idle"), Has.Length.EqualTo(8));
        Assert.That(GetSpriteFrames(sprites, "Run"), Has.Length.EqualTo(8));
        Assert.That(GetSpriteFrames(sprites, "Attack"), Has.Length.EqualTo(8));
        Assert.That(GetSpriteFrames(sprites, "Skill"), Has.Length.EqualTo(12));
        Assert.That(GetSpriteFrames(sprites, "Ultimate"), Has.Length.EqualTo(14));
        Assert.That(GetSpriteFrames(sprites, "Hit"), Has.Length.EqualTo(5));
        Assert.That(GetSpriteFrames(sprites, "Death"), Has.Length.EqualTo(10));

        var effects = new Dictionary<string, int>
        {
            { "KronosDimensionalTear", 10 }, { "KronosVoidField", 12 },
            { "KronosMassOrbit", 10 }, { "KronosResonanceBurst", 10 },
            { "KronosParasite", 8 }, { "KronosParasiteDrain", 8 },
            { "KronosSingularity", 12 }, { "KronosLeviathanAwaken", 14 },
            { "KronosLeviathanAura", 12 }, { "KronosVoidSlash", 8 },
            { "KronosCollapse", 10 }, { "KronosDecayPulse", 10 }
        };
        foreach (var effect in effects)
        {
            Assert.That(Resources.LoadAll<Sprite>($"VFX/Kronos/{effect.Key}"),
                Has.Length.EqualTo(effect.Value), effect.Key);
        }
    }

    [UnityTest]
    public IEnumerator KronosVoidKitPullsInfectsTransformsAndSurvivesFatalDamage()
    {
        SceneManager.LoadScene("SampleScene");
        yield return null;
        var combatPrototype = System.Type.GetType("CombatPrototype, Assembly-CSharp");
        var battleType = System.Type.GetType("PrototypeBattle, Assembly-CSharp");
        var combatantType = System.Type.GetType("PrototypeCombatant, Assembly-CSharp");
        combatPrototype.GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        yield return null;
        Time.timeScale = 0f;

        var kronos = GameObject.Find(KronosName).GetComponent(combatantType);
        var battle = Object.FindFirstObjectByType(battleType);
        SetField(battle, "<IsIdleFarmMode>k__BackingField", false);
        var enemies = (System.Array)battleType.GetProperty("Enemies").GetValue(battle);
        var enemy = enemies.GetValue(0);
        Assert.That(kronos, Is.Not.Null);
        Assert.That(GetInstanceProperty<string>(enemy, "StatusSummary"), Does.Contain("Slow"));

        InvokeInstance(kronos, "ApplyStun", 2f);
        Assert.That(GetInstanceProperty<string>(kronos, "StatusSummary"), Does.Not.Contain("Stun"));

        var maxHealth = GetInstanceProperty<int>(kronos, "MaxHealth");
        InvokeInstance(kronos, "TakeDamage", Mathf.CeilToInt(maxHealth * 0.06f), false);
        Assert.That(GetField<int>(kronos, "voidResonanceStacks"), Is.GreaterThanOrEqualTo(1));
        SetField(kronos, "voidResonanceStacks", 10);
        InvokeInstance(kronos, "PerformKronosBasic");
        Assert.That(GetField<int>(kronos, "voidResonanceStacks"), Is.Zero);

        ((Component)enemy).transform.position = ((Component)kronos).transform.position + Vector3.right * 2f;
        InvokeInstance(kronos, "CastSingularityPull");
        Assert.That(GetField<object>(enemy, "forcedTarget"), Is.SameAs(kronos));
        Assert.That(GetField<float>(enemy, "voidParasiteRemaining"), Is.EqualTo(4f));
        Assert.That(GetField<float>(kronos, "voidTauntReductionRemaining"), Is.EqualTo(2f));

        var enemyMaximum = GetInstanceProperty<int>(enemy, "MaxHealth");
        SetField(enemy, "currentHealth", enemyMaximum - 100);
        Assert.That((int)InvokeInstance(enemy, "Heal", 100), Is.EqualTo(85));
        SetField(enemy, "currentShield", 0);
        InvokeInstance(enemy, "GrantShield", 100, 4f);
        Assert.That(GetInstanceProperty<int>(enemy, "CurrentShield"), Is.EqualTo(85));

        SetField(kronos, "voidTauntReductionRemaining", 0f);
        InvokeInstance(kronos, "TakeDamageFrom", 100, false, enemy);
        Assert.That(GetInstanceProperty<int>(kronos, "CurrentShield"), Is.GreaterThanOrEqualTo(18));

        var baseRange = GetInstanceProperty<float>(kronos, "AttackRange");
        InvokeInstance(kronos, "CastCosmicLeviathan");
        Assert.That(GetInstanceProperty<int>(kronos, "MaxHealth"), Is.GreaterThan(maxHealth));
        Assert.That(GetInstanceProperty<float>(kronos, "AttackRange"), Is.GreaterThan(baseRange));
        InvokeInstance(kronos, "CastSingularityPull");
        Assert.That(GetField<float>(kronos, "voidTauntReductionRemaining"), Is.EqualTo(2.5f));

        SetField(kronos, "currentShield", 0);
        SetField(kronos, "currentHealth", 10);
        SetField(kronos, "voidRebirthCooldown", 0f);
        InvokeInstance(kronos, "TakeDamage", 999999, false);
        Assert.That(GetInstanceProperty<int>(kronos, "CurrentHealth"), Is.EqualTo(1));
        Assert.That(GetField<float>(kronos, "voidCollapseRemaining"), Is.EqualTo(2f));
    }

    [Test]
    public void FireCombatMathKeepsDotSlowGeometryAndBalanceBounded()
    {
        var fireCombat = System.Type.GetType("PrototypeFireCombat, Assembly-CSharp");
        Assert.That(InvokeStatic(fireCombat, "RemainingDotDamage", 10, 2.1f, 1f), Is.EqualTo(30));
        Assert.That(InvokeStatic(fireCombat, "RemainingDotDamage", 10, 0f, 1f), Is.Zero);
        Assert.That(InvokeStatic(fireCombat, "DecayingSlow", 0.7f, 2f, 0f), Is.EqualTo(0.7f));
        Assert.That(InvokeStatic(fireCombat, "DecayingSlow", 0.7f, 2f, 1f), Is.EqualTo(0.35f).Within(0.001f));
        Assert.That(InvokeStatic(fireCombat, "DecayingSlow", 0.7f, 2f, 2f), Is.Zero);
        Assert.That(InvokeStatic(fireCombat, "PointInCircle", new Vector3(1.7f, 0f), Vector3.zero, 1.8f), Is.True);
        Assert.That(InvokeStatic(fireCombat, "PointInCircle", new Vector3(1.9f, 0f), Vector3.zero, 1.8f), Is.False);
        Assert.That(InvokeStatic(fireCombat, "PointInCone", new Vector3(5f, 0f), Vector3.zero, Vector3.right, 7f, 35f), Is.True);
        Assert.That(InvokeStatic(fireCombat, "PointInCone", new Vector3(-1f, 0f), Vector3.zero, Vector3.right, 7f, 35f), Is.False);
        Assert.That(InvokeStatic(fireCombat, "MissingHealthDetonation", 1000, 600, 1), Is.EqualTo(40));
        Assert.That(InvokeStatic(fireCombat, "MissingHealthDetonation", 1000, 600, 3), Is.EqualTo(120));
        Assert.That(InvokeStatic(fireCombat, "FlameShieldAmount", 1000, 0), Is.EqualTo(120));
        Assert.That(InvokeStatic(fireCombat, "FlameShieldAmount", 1000, 5), Is.EqualTo(320));
    }

    [Test]
    public void FirestormConversionDoublesRadiusOnlyOnce()
    {
        var zoneType = System.Type.GetType("PrototypeFireZone, Assembly-CSharp");
        var zoneObject = new GameObject("Test Magma");
        var zone = zoneObject.AddComponent(zoneType);
        SetField(zone, "radius", 1.8f);
        InvokeInstance(zone, "ConvertToFirestorm");
        InvokeInstance(zone, "ConvertToFirestorm");
        Assert.That(GetInstanceProperty<float>(zone, "Radius"), Is.EqualTo(3.6f).Within(0.001f));
        Object.DestroyImmediate(zoneObject);
    }

    [UnityTest]
    public IEnumerator AshHeatAndPendingActionsAreBounded()
    {
        SceneManager.LoadScene("SampleScene");
        yield return null;
        var combatPrototype = System.Type.GetType("CombatPrototype, Assembly-CSharp");
        var combatantType = System.Type.GetType("PrototypeCombatant, Assembly-CSharp");
        var battleType = System.Type.GetType("PrototypeBattle, Assembly-CSharp");
        combatPrototype.GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        yield return null;

        var fireGod = GameObject.Find(FireGodName).GetComponent(combatantType);
        var battle = Object.FindFirstObjectByType(battleType);
        var enemy = ((System.Array)battleType.GetProperty("Enemies").GetValue(battle)).GetValue(0);
        InvokeInstance(enemy, "ApplyAsh", fireGod, 2);
        InvokeInstance(enemy, "ApplyAsh", fireGod, 2);
        Assert.That(InvokeInstance(enemy, "AshStacksFrom", fireGod), Is.EqualTo(3));
        Assert.That(GetInstanceProperty<float>(enemy, "FireResistance"), Is.EqualTo(-0.15f).Within(0.001f));
        Assert.That(InvokeInstance(enemy, "ConsumeAsh", fireGod), Is.EqualTo(3));

        InvokeInstance(fireGod, "AddHeat", 9);
        Assert.That(GetInstanceProperty<int>(fireGod, "HeatStacks"), Is.EqualTo(5));
        Assert.That(GetInstanceProperty<float>(fireGod, "FireDamageMultiplier"), Is.EqualTo(1.25f).Within(0.001f));
        Assert.That(GetInstanceProperty<float>(fireGod, "MovementSpeedMultiplier"), Is.EqualTo(1.2f).Within(0.001f));

        var rootPosition = ((Component)enemy).transform.position;
        InvokeInstance(enemy, "ApplyGrounded", 1f);
        Assert.That(InvokeInstance(enemy, "TryVoluntaryDisplacement", rootPosition + Vector3.right), Is.False);
        Assert.That(((Component)enemy).transform.position, Is.EqualTo(rootPosition));
        InvokeInstance(enemy, "ApplyKnockback", Vector3.right, 1f);
        Assert.That(GetField<float>(enemy, "knockbackRemaining"), Is.GreaterThan(0f));
        InvokeInstance(enemy, "ApplyKnockUp", 0.28f);
        Assert.That(((Component)enemy).transform.position, Is.EqualTo(rootPosition));

        var first = new System.Action(() => { });
        SetField(fireGod, "airborneRemaining", 0f);
        ((Component)fireGod).transform.position = ((Component)enemy).transform.position;
        SetField(fireGod, "<Target>k__BackingField", enemy);
        SetField(fireGod, "pendingAction", first);
        SetField(fireGod, "actionHitRemaining", 1f);
        SetField(fireGod, "attackCooldown", -1f);
        InvokeInstance(fireGod, "Update");
        Assert.That(GetField<System.Action>(fireGod, "pendingAction"), Is.SameAs(first));
    }

    [UnityTest]
    public IEnumerator FireGodVfxSheetsLoadAndOneShotCompletes()
    {
        var expectedFrames = new Dictionary<string, int>
        {
            { "FireGodFlameProjectile", 8 }, { "FireGodFlameImpact", 8 },
            { "FireGodAshOne", 8 }, { "FireGodAshTwo", 8 }, { "FireGodAshSigil", 8 },
            { "FireGodAshWarning", 6 }, { "FireGodAshDetonation", 10 },
            { "FireGodHellfireImpact", 12 }, { "FireGodMagmaLoop", 8 },
            { "FireGodMagmaBurst", 10 }, { "FireGodCrimsonGaleCore", 48 },
            { "FireGodCrimsonGaleRibbon", 48 }, { "FireGodCrimsonGaleSparks", 48 },
            { "FireGodCrimsonGaleImpact", 48 }, { "FireGodCrimsonGaleResidue", 48 },
            { "FireGodScorchLoop", 8 }, { "FireGodFirestormConvert", 10 },
            { "FireGodFirestormLoop", 8 }, { "FireGodFlameShieldSpawn", 8 },
            { "FireGodFlameShieldLoop", 8 }, { "FireGodFlameShieldBreak", 6 },
            { "FireGodHeatAura", 8 }, { "FireGodEmberProjectile", 8 },
            { "FireGodEmberIgnite", 6 }, { "FireGodAshDissolve", 10 }
        };
        foreach (var pair in expectedFrames)
        {
            Assert.That(
                Resources.LoadAll<Sprite>($"VFX/FireGod/{pair.Key}"),
                Has.Length.EqualTo(pair.Value),
                pair.Key);
        }

        var vfxType = System.Type.GetType("PrototypeFireVfx, Assembly-CSharp");
        Assert.That(vfxType, Is.Not.Null);
        var spawn = vfxType.GetMethod("Spawn", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(spawn, Is.Not.Null);
        var effect = (Component)spawn.Invoke(
            null,
            new object[] { "FireGodFlameImpact", Vector3.zero, 0.01f, 1f, false, 110 });
        Assert.That(effect, Is.Not.Null);
        Assert.That(effect.GetComponent<SpriteRenderer>().sprite, Is.Not.Null);

        yield return new WaitForSeconds(0.12f);
        Assert.That(effect == null, Is.True);

        var spawnCrimsonGale = vfxType.GetMethod(
            "SpawnCrimsonGale", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(spawnCrimsonGale, Is.Not.Null);
        var crimsonGale = (GameObject)spawnCrimsonGale.Invoke(
            null, new object[] { Vector3.zero, Vector3.right });
        Assert.That(crimsonGale, Is.Not.Null);
        Assert.That(crimsonGale.GetComponentsInChildren(vfxType, true), Has.Length.EqualTo(5));

        var flowType = System.Type.GetType("PrototypeFireFlowVfx, Assembly-CSharp");
        Assert.That(flowType, Is.Not.Null);
        var flowLayers = crimsonGale.GetComponentsInChildren(flowType, true);
        Assert.That(flowLayers, Has.Length.EqualTo(2));
        foreach (Component flowLayer in flowLayers)
        {
            var renderer = flowLayer.GetComponent<MeshRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sharedMaterial, Is.Not.Null);
            Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("GalaxyRising/ProceduralFire"));
#if UNITY_EDITOR
            var shaderMessages = UnityEditor.ShaderUtil.GetShaderMessages(renderer.sharedMaterial.shader);
            Assert.That(
                shaderMessages,
                Is.Empty,
                shaderMessages.Length == 0 ? string.Empty : shaderMessages[0].message);
#endif
        }
        Object.Destroy(crimsonGale);
    }

    [Test]
    public void EveryFireGodSpriteEffectUsesTheMaskedProceduralShader()
    {
        var names = new[]
        {
            "FireGodFlameProjectile", "FireGodFlameImpact",
            "FireGodAshOne", "FireGodAshTwo", "FireGodAshSigil",
            "FireGodAshWarning", "FireGodAshDetonation",
            "FireGodHellfireImpact", "FireGodMagmaLoop", "FireGodMagmaBurst",
            "FireGodCrimsonGaleCore", "FireGodCrimsonGaleRibbon",
            "FireGodCrimsonGaleSparks", "FireGodCrimsonGaleImpact",
            "FireGodCrimsonGaleResidue", "FireGodScorchLoop",
            "FireGodFirestormConvert", "FireGodFirestormLoop",
            "FireGodFlameShieldSpawn", "FireGodFlameShieldLoop",
            "FireGodFlameShieldBreak", "FireGodHeatAura",
            "FireGodEmberProjectile", "FireGodEmberIgnite", "FireGodAshDissolve"
        };
        var vfxType = System.Type.GetType("PrototypeFireVfx, Assembly-CSharp");
        Assert.That(vfxType, Is.Not.Null);
        var spawn = vfxType.GetMethod("Spawn", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(spawn, Is.Not.Null);

        foreach (var name in names)
        {
            var effect = (Component)spawn.Invoke(
                null, new object[] { name, Vector3.zero, 1f, 1f, true, 100 });
            Assert.That(effect, Is.Not.Null, name);
            var renderer = effect.GetComponent<SpriteRenderer>();
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            Assert.That(renderer.sharedMaterial.shader.name,
                Is.EqualTo("GalaxyRising/ProceduralFire"), name);
            Assert.That(properties.GetFloat("_UseSpriteMask"), Is.EqualTo(1f), name);
            Assert.That(properties.GetFloat("_SpriteContribution"), Is.GreaterThan(0f), name);
            Assert.That(properties.GetVector("_FlowDirection").sqrMagnitude,
                Is.GreaterThan(0.5f), name);
            Object.DestroyImmediate(effect.gameObject);
        }
    }

#if UNITY_EDITOR
    [Test]
    public void ProceduralFirePreviewRendersVisibleAnimatedFrames()
    {
        var exporterType = System.Type.GetType(
            "FireGodProceduralPreviewExporter, Assembly-CSharp-Editor");
        Assert.That(exporterType, Is.Not.Null);
        var renderFrame = exporterType.GetMethod(
            "RenderEffectFrame", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(renderFrame, Is.Not.Null);

        var first = (Texture2D)renderFrame.Invoke(
            null, new object[] { "FireGodMagmaLoop", 0.25f, 180, 320 });
        var later = (Texture2D)renderFrame.Invoke(
            null, new object[] { "FireGodMagmaLoop", 0.85f, 180, 320 });
        try
        {
            var firstPixels = first.GetPixels32();
            var laterPixels = later.GetPixels32();
            var visiblePixels = 0;
            var changedPixels = 0;
            var fallbackPixels = 0;
            for (var index = 0; index < firstPixels.Length; index++)
            {
                var firstPixel = firstPixels[index];
                var laterPixel = laterPixels[index];
                if (firstPixel.r > 110 || firstPixel.g > 110 || firstPixel.b > 110)
                {
                    visiblePixels++;
                }
                if (Mathf.Abs(firstPixel.r - laterPixel.r) +
                    Mathf.Abs(firstPixel.g - laterPixel.g) +
                    Mathf.Abs(firstPixel.b - laterPixel.b) > 45)
                {
                    changedPixels++;
                }
                if (firstPixel.r > 220 && firstPixel.g < 80 && firstPixel.b > 220)
                {
                    fallbackPixels++;
                }
            }

            Assert.That(visiblePixels, Is.GreaterThan(firstPixels.Length / 100));
            Assert.That(changedPixels, Is.GreaterThan(firstPixels.Length / 100));
            Assert.That(fallbackPixels, Is.LessThan(firstPixels.Length / 2));
        }
        finally
        {
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(later);
        }
    }
#endif

    [Test]
    public void StarterArtUsesDetailedFramesAndNonStartersKeepLegacyFallback()
    {
        var definitionType = System.Type.GetType("CombatantDefinition, Assembly-CSharp");
        var pixelArtType = System.Type.GetType("PrototypePixelArt, Assembly-CSharp");
        Assert.That(definitionType, Is.Not.Null);
        Assert.That(pixelArtType, Is.Not.Null);

        var create = pixelArtType.GetMethod(
            "Create",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { definitionType, typeof(bool) },
            null);
        Assert.That(create, Is.Not.Null);

        foreach (var heroName in new[] { "Nova", "Ion", "Astra", "Lyra", "Brakk" })
        {
            var definition = Resources.Load($"Combatants/Allies/{heroName}", definitionType);
            Assert.That(definition, Is.Not.Null);
            var spriteSet = create.Invoke(null, new[] { definition, (object)false });
            if (heroName != "Astra" && heroName != "Brakk")
            {
                AssertDetailedSpriteSet(spriteSet, heroName == "Nova" ? 68 : 64, heroName == "Nova" ? 8 : 6);
            }
            if (heroName == "Nova")
            {
                foreach (var state in new[] { "Idle", "Run", "Attack", "Skill", "Ultimate", "Hit", "Death" })
                {
                    foreach (var frame in GetSpriteFrames(spriteSet, state))
                    {
                        Assert.That(frame, Is.Not.Null);
                        Assert.That(frame.rect.size, Is.EqualTo(new Vector2(68f, 68f)));
                    }
                }
                Assert.That(GetInstanceProperty<string>(definition, "DisplayName"), Is.EqualTo(FireGodName));
                Assert.That(GetInstanceProperty<object>(definition, "Species").ToString(), Is.EqualTo("Human"));
                Assert.That(GetInstanceProperty<object>(definition, "CombatClass").ToString(), Is.EqualTo("Mage"));
                Assert.That(GetInstanceProperty<float>(definition, "AttackRange"), Is.EqualTo(4f));
                Assert.That(GetSpriteFrames(spriteSet, "Attack")[0].texture.name, Is.EqualTo("NovaFlameBasicAttack"));
            }
            else if (heroName == "Astra")
            {
                Assert.That(GetInstanceProperty<string>(definition, "DisplayName"), Is.EqualTo(AureliaName));
                Assert.That(GetSpriteFrames(spriteSet, "Idle"), Has.Length.EqualTo(6));
                Assert.That(GetSpriteFrames(spriteSet, "Attack")[0].texture.name, Is.EqualTo("AureliaBasic"));
            }
            else if (heroName == "Brakk")
            {
                Assert.That(GetInstanceProperty<string>(definition, "DisplayName"), Is.EqualTo(KronosName));
                Assert.That(GetSpriteFrames(spriteSet, "Idle"), Has.Length.EqualTo(8));
                Assert.That(GetSpriteFrames(spriteSet, "Attack")[0].texture.name, Is.EqualTo("KronosBasic"));
            }
        }

        var fallbackDefinition = Resources.Load("Combatants/Enemies/RaiderGunner", definitionType);
        Assert.That(fallbackDefinition, Is.Not.Null);
        var fallbackSet = create.Invoke(null, new[] { fallbackDefinition, (object)false });
        var fallbackIdle = GetSpriteFrames(fallbackSet, "Idle");
        Assert.That(fallbackIdle[0].texture.width, Is.EqualTo(32));
        Assert.That(fallbackIdle[0].texture.height, Is.EqualTo(32));
    }

    [Test]
    public void PvEModesUseMonsterPoolAndPvPUsesSummonableHeroes()
    {
        var combatPrototype = System.Type.GetType("CombatPrototype, Assembly-CSharp");
        var definitionType = System.Type.GetType("CombatantDefinition, Assembly-CSharp");
        var gameModeType = System.Type.GetType("PrototypeGameMode, Assembly-CSharp");
        Assert.That(combatPrototype, Is.Not.Null);
        Assert.That(definitionType, Is.Not.Null);
        Assert.That(gameModeType, Is.Not.Null);

        var loadEnemies = combatPrototype.GetMethod(
            "LoadEnemyDefinitionsForMode",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(loadEnemies, Is.Not.Null);

        var monsterPool = Resources.LoadAll("Combatants/Monsters", definitionType);
        Assert.That(monsterPool, Has.Length.EqualTo(6));
        var monsterNames = new HashSet<string>();
        foreach (var monster in monsterPool)
        {
            monsterNames.Add(((Object)monster).name);
        }

        foreach (var modeName in new[] { "Idle", "Dungeon" })
        {
            var enemies = (System.Array)loadEnemies.Invoke(
                null,
                new[] { System.Enum.Parse(gameModeType, modeName), (object)3 });
            Assert.That(enemies, Has.Length.EqualTo(5));
            var selectedNames = new HashSet<string>();
            foreach (var enemy in enemies)
            {
                var enemyName = ((Object)enemy).name;
                Assert.That(monsterNames, Does.Contain(enemyName));
                selectedNames.Add(enemyName);
            }
            Assert.That(selectedNames, Has.Count.EqualTo(5));
        }

        var pvpEnemies = (System.Array)loadEnemies.Invoke(
            null,
            new[] { System.Enum.Parse(gameModeType, "PvP"), (object)3 });
        Assert.That(pvpEnemies, Has.Length.EqualTo(5));
        foreach (var enemy in pvpEnemies)
        {
            Assert.That(monsterNames, Does.Not.Contain(((Object)enemy).name));
        }

        var roster = (System.Array)InvokeStatic(combatPrototype, "LoadRoster");
        foreach (var character in roster)
        {
            Assert.That(monsterNames, Does.Not.Contain(((Object)character).name));
        }
    }

    [Test]
    public void MonsterReferencesProvideCombatAnimationFrames()
    {
        var definitionType = System.Type.GetType("CombatantDefinition, Assembly-CSharp");
        var pixelArtType = System.Type.GetType("PrototypePixelArt, Assembly-CSharp");
        Assert.That(definitionType, Is.Not.Null);
        Assert.That(pixelArtType, Is.Not.Null);

        var create = pixelArtType.GetMethod(
            "Create",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { definitionType, typeof(bool) },
            null);
        Assert.That(create, Is.Not.Null);

        var monsters = Resources.LoadAll("Combatants/Monsters", definitionType);
        Assert.That(monsters, Has.Length.EqualTo(6));
        foreach (var monster in monsters)
        {
            var spriteSet = create.Invoke(null, new[] { monster, (object)false });
            Assert.That(GetSpriteFrames(spriteSet, "Idle"), Has.Length.AtLeast(2));
            Assert.That(GetSpriteFrames(spriteSet, "Run"), Has.Length.AtLeast(4));
            Assert.That(GetSpriteFrames(spriteSet, "Attack"), Has.Length.AtLeast(4));
            Assert.That(GetSpriteFrames(spriteSet, "Skill"), Has.Length.AtLeast(4));
            Assert.That(GetSpriteFrames(spriteSet, "Ultimate"), Has.Length.AtLeast(4));
            Assert.That(GetSpriteFrames(spriteSet, "Hit"), Has.Length.AtLeast(2));
            Assert.That(GetSpriteFrames(spriteSet, "Death"), Has.Length.AtLeast(3));
            Assert.That(GetSpriteFrames(spriteSet, "Idle")[0].texture.width, Is.GreaterThanOrEqualTo(64));
        }
    }

    [Test]
    public void RazorjawKeepsItsLeftFacingSourceOrientation()
    {
        var definitionType = System.Type.GetType("CombatantDefinition, Assembly-CSharp");
        var combatantType = System.Type.GetType("PrototypeCombatant, Assembly-CSharp");
        Assert.That(definitionType, Is.Not.Null);
        Assert.That(combatantType, Is.Not.Null);

        var razorjaw = Resources.Load("Combatants/Monsters/Razorjaw", definitionType);
        var sourceFacesLeft = definitionType.GetProperty(
            "SourceFacesLeft",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var shouldFlip = combatantType.GetMethod(
            "ShouldFlipSprite",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(razorjaw, Is.Not.Null);
        Assert.That(sourceFacesLeft, Is.Not.Null);
        Assert.That(shouldFlip, Is.Not.Null);
        Assert.That(sourceFacesLeft.GetValue(razorjaw), Is.True);
        Assert.That(shouldFlip.Invoke(null, new object[] { true, true }), Is.False);
        Assert.That(shouldFlip.Invoke(null, new object[] { false, true }), Is.True);
    }

    [UnityTest]
    public IEnumerator DeathAnimationHidesVisualsWithoutDestroyingCombatant()
    {
        SceneManager.LoadScene("SampleScene");
        yield return null;
        var combatPrototype = System.Type.GetType("CombatPrototype, Assembly-CSharp");
        var battleType = System.Type.GetType("PrototypeBattle, Assembly-CSharp");
        var combatantType = System.Type.GetType("PrototypeCombatant, Assembly-CSharp");
        combatPrototype.GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        yield return null;

        var battle = Object.FindFirstObjectByType(battleType);
        var enemies = (System.Array)battleType.GetProperty("Enemies").GetValue(battle);
        var enemy = enemies.GetValue(0);
        var enemyObject = ((Component)enemy).gameObject;
        var body = enemyObject.transform.Find("Body").GetComponent<SpriteRenderer>();

        combatantType.GetMethod("TakeDamage", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(enemy, new object[] { 999999, false });
        var updateAnimation = combatantType.GetMethod(
            "UpdateCharacterAnimation",
            BindingFlags.NonPublic | BindingFlags.Instance);
        updateAnimation.Invoke(enemy, null);
        combatantType.GetField("animationTime", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(enemy, 10f);
        updateAnimation.Invoke(enemy, null);

        Assert.That(body.enabled, Is.False);
        Assert.That(enemyObject.activeSelf, Is.True);
    }

    [Test]
    public void StarterSkillSynergyMathIsBounded()
    {
        var combatantType = System.Type.GetType("PrototypeCombatant, Assembly-CSharp");
        Assert.That(combatantType, Is.Not.Null);

        Assert.That(InvokeStatic(combatantType, "IonBurnBounceMultiplier", true), Is.EqualTo(0.3f));
        Assert.That(InvokeStatic(combatantType, "IonBurnBounceMultiplier", false), Is.EqualTo(0f));
        Assert.That(InvokeStatic(combatantType, "ShieldReinforcement", 1000, 200, 100), Is.EqualTo(35));
        Assert.That(InvokeStatic(combatantType, "ShieldReinforcement", 1000, 290, 100), Is.EqualTo(10));
        Assert.That(InvokeStatic(combatantType, "ShieldReinforcement", 1000, 300, 100), Is.EqualTo(0));
        Assert.That(
            (float)InvokeStatic(combatantType, "CombatStoppingDistance", 1f, 2.31f, 2.31f),
            Is.EqualTo(2.6796f).Within(0.001f));
        Assert.That(
            (float)InvokeStatic(combatantType, "CombatStoppingDistance", 4.4f, 2.31f, 2.31f),
            Is.EqualTo(4.4f).Within(0.001f));
    }

    [Test]
    public void SmartMovementUsesBothAxesAndStopsAtAttackRange()
    {
        var combatantType = System.Type.GetType("PrototypeCombatant, Assembly-CSharp");
        Assert.That(combatantType, Is.Not.Null);

        var diagonal = (Vector3)InvokeStatic(
            combatantType,
            "MoveTowardAttackRange",
            Vector3.zero,
            new Vector3(3f, 4f),
            1f,
            0f);
        Assert.That(diagonal.x, Is.EqualTo(0.6f).Within(0.001f));
        Assert.That(diagonal.y, Is.EqualTo(0.8f).Within(0.001f));

        var stopped = (Vector3)InvokeStatic(
            combatantType,
            "MoveTowardAttackRange",
            Vector3.zero,
            new Vector3(3f, 0f),
            10f,
            1f);
        Assert.That(stopped, Is.EqualTo(new Vector3(2f, 0f)));
    }

    [Test]
    public void TimedCombatSequencesYieldBetweenHits()
    {
        var combatantType = System.Type.GetType("PrototypeCombatant, Assembly-CSharp");
        Assert.That(combatantType, Is.Not.Null);
        var hits = new List<int>();
        var sequence = (IEnumerator)InvokeStatic(
            combatantType,
            "RunTimedSequence",
            3,
            0.11f,
            new System.Action<int>(index => hits.Add(index)));

        Assert.That(sequence.MoveNext(), Is.True);
        Assert.That(hits, Is.Empty);
        Assert.That(sequence.MoveNext(), Is.True);
        Assert.That(hits, Is.EqualTo(new[] { 0 }));
        Assert.That(sequence.Current, Is.InstanceOf<WaitForSeconds>());
        Assert.That(sequence.MoveNext(), Is.True);
        Assert.That(hits, Is.EqualTo(new[] { 0, 1 }));
        Assert.That(sequence.MoveNext(), Is.False);
        Assert.That(hits, Is.EqualTo(new[] { 0, 1, 2 }));
    }

    [UnityTest]
    public IEnumerator UltimateCutInIsNotBuilt()
    {
        SceneManager.LoadScene("SampleScene");
        yield return null;
        var combatPrototype = System.Type.GetType("CombatPrototype, Assembly-CSharp");
        combatPrototype.GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        yield return null;

        var flowType = System.Type.GetType("PrototypeGameFlow, Assembly-CSharp");
        var showUltimate = flowType.GetMethod("ShowUltimate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(showUltimate, Is.Null);
        Assert.That(GameObject.Find("Ultimate Cut-In"), Is.Null);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [Test]
    public void EngagementPointsStayRelativeToMovingTargets()
    {
        var combatantType = System.Type.GetType("PrototypeCombatant, Assembly-CSharp");
        Assert.That(combatantType, Is.Not.Null);

        var first = (Vector3)InvokeStatic(
            combatantType,
            "CalculateEngagementPoint",
            new Vector3(-3f, 0f),
            new Vector3(1f, 0f),
            2f,
            0.6f);
        var moved = (Vector3)InvokeStatic(
            combatantType,
            "CalculateEngagementPoint",
            new Vector3(-3f, 3f),
            new Vector3(1f, 3f),
            2f,
            0.6f);
        var oppositeOffset = (Vector3)InvokeStatic(
            combatantType,
            "CalculateEngagementPoint",
            new Vector3(-3f, 0f),
            new Vector3(1f, 0f),
            2f,
            -0.6f);

        Assert.That(Vector3.Distance(first, new Vector3(1f, 0f)), Is.EqualTo(2f).Within(0.001f));
        Assert.That(Vector3.Distance(moved, new Vector3(1f, 3f)), Is.EqualTo(2f).Within(0.001f));
        Assert.That(Vector3.Distance(oppositeOffset, new Vector3(1f, 0f)), Is.EqualTo(2f).Within(0.001f));
        Assert.That(moved - first, Is.EqualTo(Vector3.up * 3f));
        Assert.That(first.y, Is.Not.EqualTo(oppositeOffset.y));
    }

    [Test]
    public void EngagementPointCanCrossTheFormerCenterLine()
    {
        var combatantType = System.Type.GetType("PrototypeCombatant, Assembly-CSharp");
        Assert.That(combatantType, Is.Not.Null);

        var point = (Vector3)InvokeStatic(
            combatantType,
            "CalculateEngagementPoint",
            new Vector3(-2f, 0f),
            new Vector3(3f, 0f),
            1f,
            0f);

        Assert.That(point.x, Is.GreaterThan(0f));
        Assert.That(Vector3.Distance(point, new Vector3(3f, 0f)), Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void MovementIsClampedOnlyByArenaEdges()
    {
        var combatantType = System.Type.GetType("PrototypeCombatant, Assembly-CSharp");
        Assert.That(combatantType, Is.Not.Null);

        var clamped = (Vector3)InvokeStatic(
            combatantType,
            "ClampToArena",
            new Vector3(10f, -10f, 0f));

        Assert.That(clamped, Is.EqualTo(new Vector3(4.2f, -4.2f, 0f)));
    }

    [UnityTest]
    public IEnumerator ArenaHasNoCenterDivider()
    {
        SceneManager.LoadScene("SampleScene");
        yield return null;
        var combatPrototype = System.Type.GetType("CombatPrototype, Assembly-CSharp");
        combatPrototype.GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        yield return null;

        Assert.That(GameObject.Find("Center Rift"), Is.Null);
    }

    [Test]
    public void RangedClassesKeepVisibleDistanceFromFrontline()
    {
        var rulesType = System.Type.GetType("PrototypeCombatClassRules, Assembly-CSharp");
        var classType = System.Type.GetType("PrototypeCombatClass, Assembly-CSharp");
        Assert.That(rulesType, Is.Not.Null);
        Assert.That(classType, Is.Not.Null);

        Assert.That(
            InvokeStatic(rulesType, "GetAttackRange", System.Enum.Parse(classType, "Support")),
            Is.EqualTo(3.6f));
        Assert.That(
            InvokeStatic(rulesType, "GetAttackRange", System.Enum.Parse(classType, "Mage")),
            Is.EqualTo(4f));
        Assert.That(
            InvokeStatic(rulesType, "GetAttackRange", System.Enum.Parse(classType, "Archer")),
            Is.EqualTo(4.4f));
    }

    [UnityTest]
    public IEnumerator CombatantDoesNotRepositionAwayFromTargetWhenAlreadyInRange()
    {
        SceneManager.LoadScene("SampleScene");
        yield return null;
        var combatPrototype = System.Type.GetType("CombatPrototype, Assembly-CSharp");
        var battleType = System.Type.GetType("PrototypeBattle, Assembly-CSharp");
        var combatantType = System.Type.GetType("PrototypeCombatant, Assembly-CSharp");
        combatPrototype.GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        yield return null;

        var battle = Object.FindFirstObjectByType(battleType);
        var allies = (System.Array)battleType.GetProperty("Allies").GetValue(battle);
        var enemies = (System.Array)battleType.GetProperty("Enemies").GetValue(battle);
        var ally = allies.GetValue(0);
        var target = enemies.GetValue(2);
        var targetPosition = ((Component)target).transform.position;
        var stoppingDistance = (float)InvokeStatic(
            combatantType,
            "CombatStoppingDistance",
            GetInstanceProperty<float>(ally, "AttackRange"),
            combatantType.GetField("bodySize", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ally),
            combatantType.GetField("bodySize", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target));
        var inRangePosition = targetPosition + Vector3.left * (stoppingDistance * 0.5f);
        ((Component)ally).transform.position = inRangePosition;

        var targetField = combatantType.GetField("<Target>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        battleType.GetField("<IsFinished>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(battle, false);
        var attackCooldown = combatantType.GetField("attackCooldown", BindingFlags.NonPublic | BindingFlags.Instance);
        var activeSkillCooldown = combatantType.GetField("activeSkillCooldown", BindingFlags.NonPublic | BindingFlags.Instance);
        var energy = combatantType.GetField("energy", BindingFlags.NonPublic | BindingFlags.Instance);
        var currentHealth = combatantType.GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);
        var stunRemaining = combatantType.GetField("stunRemaining", BindingFlags.NonPublic | BindingFlags.Instance);
        var pendingAction = combatantType.GetField("pendingAction", BindingFlags.NonPublic | BindingFlags.Instance);
        var update = combatantType.GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
        currentHealth.SetValue(target, GetInstanceProperty<int>(target, "MaxHealth"));
        targetField.SetValue(ally, target);
        currentHealth.SetValue(ally, GetInstanceProperty<int>(ally, "MaxHealth"));
        attackCooldown.SetValue(ally, 99f);
        activeSkillCooldown.SetValue(ally, 99f);
        energy.SetValue(ally, 0f);
        stunRemaining.SetValue(ally, 0f);
        pendingAction.SetValue(ally, null);
        update.Invoke(ally, null);

        Assert.That(((Component)ally).transform.position, Is.EqualTo(inRangePosition));
    }

    [Test]
    public void TargetPreferenceMatchesCombatRole()
    {
        var battleType = System.Type.GetType("PrototypeBattle, Assembly-CSharp");
        var classType = System.Type.GetType("PrototypeCombatClass, Assembly-CSharp");
        Assert.That(battleType, Is.Not.Null);
        Assert.That(classType, Is.Not.Null);

        Assert.That(
            InvokeStatic(battleType, "PreferredTargetRow", System.Enum.Parse(classType, "Tanker")).ToString(),
            Is.EqualTo("Front"));
        Assert.That(
            InvokeStatic(battleType, "PreferredTargetRow", System.Enum.Parse(classType, "Assassin")).ToString(),
            Is.EqualTo("Back"));
        Assert.That(
            InvokeStatic(battleType, "PreferredTargetRow", System.Enum.Parse(classType, "Archer")),
            Is.Null);
    }

    [Test]
    public void BossArtAddsAuraAndCrown()
    {
        var combatantType = System.Type.GetType("PrototypeCombatant, Assembly-CSharp");
        var battleType = System.Type.GetType("PrototypeBattle, Assembly-CSharp");
        var definitionType = System.Type.GetType("CombatantDefinition, Assembly-CSharp");
        var teamType = System.Type.GetType("PrototypeTeam, Assembly-CSharp");
        var rowType = System.Type.GetType("PrototypeFormationRow, Assembly-CSharp");
        Assert.That(combatantType, Is.Not.Null);
        Assert.That(battleType, Is.Not.Null);
        Assert.That(definitionType, Is.Not.Null);
        Assert.That(teamType, Is.Not.Null);
        Assert.That(rowType, Is.Not.Null);

        var definition = Resources.Load("Combatants/Enemies/RaiderGuard", definitionType);
        Assert.That(definition, Is.Not.Null);

        var unitObject = new GameObject("Boss Art Test");
        var battleObject = new GameObject("Boss Battle Test");
        var unit = unitObject.AddComponent(combatantType);
        var battle = battleObject.AddComponent(battleType);
        var texture = new Texture2D(1, 1);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);
        var initialize = combatantType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance);
        initialize.Invoke(unit, new object[]
        {
            definition,
            System.Enum.Parse(rowType, "Front"),
            System.Enum.Parse(teamType, "Enemies"),
            battle,
            sprite,
            1f,
            true
        });

        Assert.That(unitObject.transform.Find("Boss Aura"), Is.Not.Null);
        Assert.That(unitObject.transform.Find("Boss Crown"), Is.Not.Null);
        Assert.That(unitObject.transform.Find("Boss Phase Aura"), Is.Not.Null);
        Assert.That(unitObject.transform.Find("Boss Orbit"), Is.Not.Null);
        var body = unitObject.transform.Find("Body").GetComponent<SpriteRenderer>();
        Assert.That(body.sprite.texture.name, Does.StartWith("Krag_Boss64_"));
        Object.DestroyImmediate(unitObject);
        Object.DestroyImmediate(battleObject);
        Object.DestroyImmediate(sprite);
        Object.DestroyImmediate(texture);
    }

    [Test]
    public void ExistingSaveSkipsFirstTimeOnboardingDuringMigration()
    {
        PlayerPrefs.SetInt(SaveVersionKey, 2);
        PlayerPrefs.DeleteKey(OnboardingStepKey);

        var saveSystem = System.Type.GetType("PrototypeSaveSystem, Assembly-CSharp");
        Assert.That(saveSystem, Is.Not.Null);
        var migrate = saveSystem.GetMethod("Migrate", BindingFlags.Public | BindingFlags.Static);
        Assert.That(migrate, Is.Not.Null);
        migrate.Invoke(null, null);

        Assert.That(PlayerPrefs.GetInt(SaveVersionKey), Is.EqualTo(6));
        Assert.That(PlayerPrefs.GetInt(OnboardingStepKey), Is.EqualTo(6));
    }

    [Test]
    public void LegacyNameMigrationsRenameAndMergeHeroData()
    {
        PlayerPrefs.SetInt(SaveVersionKey, 3);
        PlayerPrefs.SetString(ProgressionKey,
            "{\"version\":2,\"characters\":[" +
            "{\"characterName\":\"Nova\",\"level\":27,\"experience\":9,\"stars\":4,\"shards\":70,\"basicSkillLevel\":6,\"passiveSkillLevel\":7,\"activeSkillLevel\":8,\"ultimateSkillLevel\":9,\"weaponLevel\":5,\"armorLevel\":4,\"coreLevel\":3}," +
            "{\"characterName\":\"Fire God Heavenly Demon\",\"level\":12,\"experience\":40,\"stars\":2,\"shards\":10,\"basicSkillLevel\":2,\"passiveSkillLevel\":3,\"activeSkillLevel\":4,\"ultimateSkillLevel\":5,\"weaponLevel\":1,\"armorLevel\":7,\"coreLevel\":2}," +
            "{\"characterName\":\"Brakk\",\"level\":19,\"experience\":11,\"stars\":3,\"shards\":25,\"basicSkillLevel\":3,\"passiveSkillLevel\":4,\"activeSkillLevel\":5,\"ultimateSkillLevel\":6,\"weaponLevel\":2,\"armorLevel\":4,\"coreLevel\":3}]}");
        PlayerPrefs.SetString(GachaKey,
            "{\"version\":2,\"tickets\":20,\"highRarityPity\":0,\"urPity\":0,\"ownedCharacters\":[\"Nova\",\"Ion\",\"Brakk\"],\"history\":[]}");
        PlayerPrefs.SetString(SquadKey,
            "{\"version\":2,\"names\":[\"Nova\",\"Ion\",\"Astra\",\"Lyra\",\"Brakk\"],\"rows\":[1,1,2,2,0]}");
        PlayerPrefs.Save();

        InvokeStatic(System.Type.GetType("PrototypeProgression, Assembly-CSharp"), "ResetCache");
        InvokeStatic(System.Type.GetType("PrototypeGacha, Assembly-CSharp"), "ResetCache");
        InvokeStatic(System.Type.GetType("PrototypeSaveSystem, Assembly-CSharp"), "Migrate");
        InvokeStatic(System.Type.GetType("PrototypeProgression, Assembly-CSharp"), "ResetCache");
        InvokeStatic(System.Type.GetType("PrototypeGacha, Assembly-CSharp"), "ResetCache");

        var progression = System.Type.GetType("PrototypeProgression, Assembly-CSharp");
        var gacha = System.Type.GetType("PrototypeGacha, Assembly-CSharp");
        var progress = InvokeStatic(progression, "Get", FireGodName);
        var kronosProgress = InvokeStatic(progression, "Get", KronosName);
        Assert.That(PlayerPrefs.GetInt(SaveVersionKey), Is.EqualTo(6));
        Assert.That(GetField<int>(progress, "level"), Is.EqualTo(27));
        Assert.That(GetField<int>(progress, "experience"), Is.EqualTo(40));
        Assert.That(GetField<int>(progress, "armorLevel"), Is.EqualTo(7));
        Assert.That(PlayerPrefs.GetString(ProgressionKey), Does.Not.Contain("\"Nova\""));
        Assert.That(GetField<int>(kronosProgress, "level"), Is.EqualTo(19));
        Assert.That(PlayerPrefs.GetString(ProgressionKey), Does.Not.Contain("\"Brakk\""));
        Assert.That(InvokeStatic(gacha, "IsOwned", FireGodName), Is.True);
        Assert.That(InvokeStatic(gacha, "IsOwned", "Nova"), Is.False);
        Assert.That(InvokeStatic(gacha, "IsOwned", KronosName), Is.True);
        Assert.That(InvokeStatic(gacha, "IsOwned", "Brakk"), Is.False);
        Assert.That(PlayerPrefs.GetString(SquadKey), Does.Contain(FireGodName));
        Assert.That(PlayerPrefs.GetString(SquadKey), Does.Not.Contain("\"Nova\""));
        Assert.That(PlayerPrefs.GetString(SquadKey), Does.Contain(AureliaName));
        Assert.That(PlayerPrefs.GetString(SquadKey), Does.Not.Contain("\"Astra\""));
        Assert.That(PlayerPrefs.GetString(SquadKey), Does.Contain(KronosName));
        Assert.That(PlayerPrefs.GetString(SquadKey), Does.Not.Contain("\"Brakk\""));
    }

    [Test]
    public void StagesOneToTenHaveControlledGrowthAndBossesAtFiveAndTen()
    {
        var stageType = System.Type.GetType("PrototypeStageDefinition, Assembly-CSharp");
        Assert.That(stageType, Is.Not.Null);
        var stages = Resources.LoadAll("Stages", stageType);
        var previousMultiplier = 0f;
        var bossCount = 0;

        for (var number = 1; number <= 10; number++)
        {
            object stage = null;
            foreach (var candidate in stages)
            {
                if (GetInstanceProperty<int>(candidate, "Stage") == number)
                {
                    stage = candidate;
                    break;
                }
            }

            Assert.That(stage, Is.Not.Null, $"Missing stage {number}");
            var multiplier = GetInstanceProperty<float>(stage, "EnemyMultiplier");
            Assert.That(multiplier, Is.GreaterThan(previousMultiplier));
            Assert.That(multiplier - previousMultiplier, Is.LessThanOrEqualTo(number == 1 ? 1f : 0.081f));

            var expectedBoss = number == 5 || number == 10;
            Assert.That(GetInstanceProperty<bool>(stage, "IsBoss"), Is.EqualTo(expectedBoss), $"Unexpected boss flag at stage {number}");
            Assert.That(GetInstanceProperty<int>(stage, "GoldReward"), Is.EqualTo((7 + number * 3) * (expectedBoss ? 2 : 1)));
            Assert.That(GetInstanceProperty<int>(stage, "ExperienceReward"), Is.EqualTo((4 + number * 2) * (expectedBoss ? 2 : 1)));
            Assert.That(GetInstanceProperty<int>(stage, "TicketReward"), Is.EqualTo(expectedBoss ? 1 : 0));

            if (expectedBoss)
            {
                bossCount++;
            }

            previousMultiplier = multiplier;
        }

        Assert.That(bossCount, Is.EqualTo(2));
    }

    [Test]
    public void SessionProgressAndSquadSurviveRuntimeReload()
    {
        var session = System.Type.GetType("PrototypeSession, Assembly-CSharp");
        var gameMode = System.Type.GetType("PrototypeGameMode, Assembly-CSharp");
        var dungeonType = System.Type.GetType("PrototypeDungeonType, Assembly-CSharp");
        var definitionType = System.Type.GetType("CombatantDefinition, Assembly-CSharp");
        var rowType = System.Type.GetType("PrototypeFormationRow, Assembly-CSharp");
        Assert.That(session, Is.Not.Null);
        Assert.That(gameMode, Is.Not.Null);
        Assert.That(dungeonType, Is.Not.Null);
        Assert.That(definitionType, Is.Not.Null);
        Assert.That(rowType, Is.Not.Null);

        PlayerPrefs.SetInt(GoldKey, 100);
        PlayerPrefs.SetInt(ExperienceKey, 200);
        PlayerPrefs.SetInt(MaterialsKey, 300);
        InvokeStatic(session, "SetIdleStage", 7);
        InvokeStatic(session, "SetChallengePending", true);
        InvokeStatic(session, "AddIdleRewards", 11, 22);
        InvokeStatic(session, "AddReward", System.Enum.Parse(dungeonType, "Materials"), 33);

        var rosterObjects = Resources.LoadAll("Combatants", definitionType);
        Assert.That(rosterObjects, Has.Length.GreaterThanOrEqualTo(5));
        var roster = System.Array.CreateInstance(definitionType, rosterObjects.Length);
        var squad = System.Array.CreateInstance(definitionType, 5);
        var rows = System.Array.CreateInstance(rowType, 5);
        for (var index = 0; index < rosterObjects.Length; index++)
        {
            roster.SetValue(rosterObjects[index], index);
        }
        for (var index = 0; index < squad.Length; index++)
        {
            squad.SetValue(rosterObjects[index], index);
            rows.SetValue(GetInstanceProperty<object>(rosterObjects[index], "FormationRow"), index);
        }

        InvokeStatic(
            session,
            "Configure",
            System.Enum.Parse(gameMode, "Idle"),
            System.Enum.Parse(dungeonType, "Credits"),
            1,
            false,
            squad,
            rows);
        InvokeStatic(session, "ResetRuntime");

        Assert.That(GetStaticProperty<int>(session, "IdleStage"), Is.EqualTo(7));
        Assert.That(GetStaticProperty<bool>(session, "IdleChallengePending"), Is.True);
        Assert.That(GetStaticProperty<int>(session, "Gold"), Is.EqualTo(111));
        Assert.That(GetStaticProperty<int>(session, "Experience"), Is.EqualTo(222));
        Assert.That(GetStaticProperty<int>(session, "Materials"), Is.EqualTo(333));

        var loadArguments = new object[] { roster, null, null };
        var loaded = (bool)session.GetMethod("TryGetSquad", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, loadArguments);
        Assert.That(loaded, Is.True);
        var loadedSquad = (System.Array)loadArguments[1];
        var loadedRows = (System.Array)loadArguments[2];
        for (var index = 0; index < squad.Length; index++)
        {
            Assert.That(
                GetInstanceProperty<string>(loadedSquad.GetValue(index), "DisplayName"),
                Is.EqualTo(GetInstanceProperty<string>(squad.GetValue(index), "DisplayName")));
            Assert.That(loadedRows.GetValue(index), Is.EqualTo(rows.GetValue(index)));
        }
    }

    [Test]
    public void OfflineRewardsArePositiveAndCappedAtEightHours()
    {
        var session = System.Type.GetType("PrototypeSession, Assembly-CSharp");
        Assert.That(session, Is.Not.Null);
        PlayerPrefs.SetInt(IdleStageKey, 4);
        PlayerPrefs.SetInt(IdleBossPendingKey, 0);
        PlayerPrefs.SetInt(GoldKey, 100);
        PlayerPrefs.SetInt(ExperienceKey, 200);
        PlayerPrefs.SetInt(LastIdleClaimKey, (int)System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 24 * 60 * 60);

        var gold = (int)InvokeStatic(session, "GetUnclaimedIdleGold");
        var experience = (int)InvokeStatic(session, "GetUnclaimedIdleExperience");
        Assert.That(gold, Is.EqualTo(2304));
        Assert.That(experience, Is.EqualTo(1411));

        var claimArguments = new object[] { 0, 0 };
        session.GetMethod("ClaimIdleRewards", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, claimArguments);
        Assert.That(claimArguments[0], Is.EqualTo(gold));
        Assert.That(claimArguments[1], Is.EqualTo(experience));
        Assert.That(GetStaticProperty<int>(session, "Gold"), Is.EqualTo(100 + gold));
        Assert.That(GetStaticProperty<int>(session, "Experience"), Is.EqualTo(200 + experience));

        PlayerPrefs.SetInt(LastIdleClaimKey, (int)System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60);
        Assert.That(InvokeStatic(session, "GetUnclaimedIdleGold"), Is.EqualTo(0));
        Assert.That(InvokeStatic(session, "GetUnclaimedIdleExperience"), Is.EqualTo(0));
    }

    [UnityTest]
    public IEnumerator ExistingSoakRunnerSurvives100BattleFarmTransitions()
    {
        SceneManager.LoadScene("SampleScene");
        yield return null;
        var combatPrototype = System.Type.GetType("CombatPrototype, Assembly-CSharp");
        combatPrototype.GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        yield return null;

        var soakType = System.Type.GetType("PrototypeSoakTest, Assembly-CSharp");
        Assert.That(soakType, Is.Not.Null);
        var run = soakType.GetMethod("Run", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(run, Is.Not.Null);
        yield return (IEnumerator)run.Invoke(null, null);

        Assert.That(Object.FindObjectsByType(
            System.Type.GetType("PrototypeBattle, Assembly-CSharp"),
            FindObjectsSortMode.None), Has.Length.EqualTo(1));
    }

    private static RectTransform RequireSection(string name, float minimumHeight)
    {
        var gameObject = GameObject.Find(name);
        Assert.That(gameObject, Is.Not.Null, $"Missing Battle/Home section: {name}");

        var rect = gameObject.GetComponent<RectTransform>();
        Assert.That(rect, Is.Not.Null, $"{name} must use a RectTransform");
        Assert.That(rect.sizeDelta.y, Is.GreaterThanOrEqualTo(minimumHeight));
        return rect;
    }

    private static float Top(RectTransform rect)
    {
        return -rect.anchoredPosition.y;
    }

    private static Button FindButton(RectTransform parent, string label)
    {
        foreach (var button in parent.GetComponentsInChildren<Button>(true))
        {
            if (button.GetComponentInChildren<Text>().text == label)
            {
                return button;
            }
        }

        Assert.Fail($"Missing button: {label}");
        return null;
    }

    private static bool HasText(RectTransform parent, string value)
    {
        foreach (var text in parent.GetComponentsInChildren<Text>(true))
        {
            if (text.text.Contains(value))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAudioClip(Camera camera, string clipName)
    {
        foreach (var source in camera.GetComponents<AudioSource>())
        {
            if (source.clip != null && source.clip.name == clipName)
            {
                return true;
            }
        }

        return false;
    }

    private static int CountNamedChildren(RectTransform parent, string name)
    {
        var count = 0;
        foreach (var child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                count++;
            }
        }

        return count;
    }

    private static Transform FindNamedChild(RectTransform parent, string name)
    {
        foreach (var child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }

    private static void AssertDetailedSpriteSet(object spriteSet, int spriteSize, int attackFrames = 6)
    {
        var expectedFrames = spriteSize == 68
            ? new Dictionary<string, int>
            {
                { "Idle", 8 }, { "Run", 13 }, { "Attack", 8 }, { "Skill", 13 },
                { "Ultimate", 13 }, { "Hit", 9 }, { "Death", 9 }
            }
            : new Dictionary<string, int>
            {
                { "Idle", 4 }, { "Run", 6 }, { "Attack", attackFrames }, { "Skill", 8 },
                { "Ultimate", 10 }, { "Hit", 3 }, { "Death", 6 }
            };

        foreach (var pair in expectedFrames)
        {
            var frames = GetSpriteFrames(spriteSet, pair.Key);
            Assert.That(frames, Has.Length.EqualTo(pair.Value), pair.Key);
            foreach (var frame in frames)
            {
                Assert.That(frame.rect.width, Is.EqualTo(spriteSize));
                Assert.That(frame.rect.height, Is.EqualTo(spriteSize));
                Assert.That(frame.texture.filterMode, Is.EqualTo(FilterMode.Point));
            }
        }
    }

    private static Sprite[] GetSpriteFrames(object spriteSet, string fieldName)
    {
        var field = spriteSet.GetType().GetField(
            fieldName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null, $"Missing sprite state: {fieldName}");
        return (Sprite[])field.GetValue(spriteSet);
    }

    private static void AssertMinimumTouchTargets(RectTransform parent)
    {
        foreach (var button in parent.GetComponentsInChildren<Button>(true))
        {
            var size = button.GetComponent<RectTransform>().sizeDelta;
            Assert.That(
                size.x,
                Is.GreaterThanOrEqualTo(44f),
                $"Touch target is too narrow: {button.GetComponentInChildren<Text>().text}");
            Assert.That(
                size.y,
                Is.GreaterThanOrEqualTo(44f),
                $"Touch target is too short: {button.GetComponentInChildren<Text>().text}");
        }
    }

    private static void RestoreInt(string key, bool existed, int value)
    {
        if (existed)
        {
            PlayerPrefs.SetInt(key, value);
        }
        else
        {
            PlayerPrefs.DeleteKey(key);
        }
    }

    private static void RestoreString(string key, bool existed, string value)
    {
        if (existed)
        {
            PlayerPrefs.SetString(key, value);
        }
        else
        {
            PlayerPrefs.DeleteKey(key);
        }
    }

    private static object InvokeStatic(System.Type type, string method, params object[] arguments)
    {
        var methodInfo = type.GetMethod(
            method,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(methodInfo, Is.Not.Null, $"Missing method: {type.Name}.{method}");
        return methodInfo.Invoke(null, arguments);
    }

    private static object InvokeInstance(object instance, string method, params object[] arguments)
    {
        var methodInfo = instance.GetType().GetMethod(
            method,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.That(methodInfo, Is.Not.Null, $"Missing method: {instance.GetType().Name}.{method}");
        return methodInfo.Invoke(instance, arguments);
    }

    private static T GetField<T>(object instance, string field)
    {
        var fieldInfo = instance.GetType().GetField(
            field,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.That(fieldInfo, Is.Not.Null, $"Missing field: {instance.GetType().Name}.{field}");
        return (T)fieldInfo.GetValue(instance);
    }

    private static void SetField(object instance, string field, object value)
    {
        var fieldInfo = instance.GetType().GetField(
            field,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.That(fieldInfo, Is.Not.Null, $"Missing field: {instance.GetType().Name}.{field}");
        fieldInfo.SetValue(instance, value);
    }

    private static T GetStaticProperty<T>(System.Type type, string property)
    {
        var propertyInfo = type.GetProperty(property, BindingFlags.Public | BindingFlags.Static);
        Assert.That(propertyInfo, Is.Not.Null, $"Missing property: {type.Name}.{property}");
        return (T)propertyInfo.GetValue(null);
    }

    private static T GetInstanceProperty<T>(object instance, string property)
    {
        var propertyInfo = instance.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance);
        Assert.That(propertyInfo, Is.Not.Null, $"Missing property: {instance.GetType().Name}.{property}");
        return (T)propertyInfo.GetValue(instance);
    }
}
