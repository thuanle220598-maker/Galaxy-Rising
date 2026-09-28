using UnityEngine;

internal static class PrototypeSaveSystem
{
    private const int CurrentVersion = 6;
    private const string VersionKey = "Prototype.SaveVersion";
    internal const string OnboardingStepKey = "Prototype.OnboardingStep";
    internal const int OnboardingCompleteStep = 6;
    private static readonly string[] Keys =
    {
        VersionKey,
        OnboardingStepKey,
        "Prototype.IdleStage",
        "Prototype.IdleBossPending",
        "Prototype.LastIdleClaim",
        "Prototype.Gold",
        "Prototype.Experience",
        "Prototype.Materials",
        "Prototype.Squad",
        "Prototype.CharacterProgression",
        "Prototype.Gacha"
    };

    public static void Migrate()
    {
        var version = PlayerPrefs.GetInt(VersionKey, 0);
        if (version >= CurrentVersion)
        {
            return;
        }

        // Existing saves skip first-time onboarding; new and reset saves start at step zero.
        if (version < 3)
        {
            PlayerPrefs.SetInt(OnboardingStepKey, version == 0 ? 0 : OnboardingCompleteStep);
        }
        if (version < 4)
        {
            PrototypeProgression.MigrateCharacterName(
                PrototypeCharacterNames.LegacyNova,
                PrototypeCharacterNames.FireGodHeavenlyDemon);
            PrototypeGacha.MigrateCharacterName(
                PrototypeCharacterNames.LegacyNova,
                PrototypeCharacterNames.FireGodHeavenlyDemon);
            PrototypeSession.MigrateCharacterName(
                PrototypeCharacterNames.LegacyNova,
                PrototypeCharacterNames.FireGodHeavenlyDemon);
        }
        if (version < 5)
        {
            PrototypeProgression.MigrateCharacterName(
                PrototypeCharacterNames.LegacyAstra,
                PrototypeCharacterNames.Aurelia);
            PrototypeGacha.MigrateCharacterName(
                PrototypeCharacterNames.LegacyAstra,
                PrototypeCharacterNames.Aurelia);
            PrototypeSession.MigrateCharacterName(
                PrototypeCharacterNames.LegacyAstra,
                PrototypeCharacterNames.Aurelia);
        }
        if (version < 6)
        {
            PrototypeProgression.MigrateCharacterName(
                PrototypeCharacterNames.LegacyBrakk,
                PrototypeCharacterNames.Kronos);
            PrototypeGacha.MigrateCharacterName(
                PrototypeCharacterNames.LegacyBrakk,
                PrototypeCharacterNames.Kronos);
            PrototypeSession.MigrateCharacterName(
                PrototypeCharacterNames.LegacyBrakk,
                PrototypeCharacterNames.Kronos);
        }
        PlayerPrefs.SetInt(VersionKey, CurrentVersion);
        PlayerPrefs.Save();
    }

    public static int OnboardingStep => Mathf.Clamp(
        PlayerPrefs.GetInt(OnboardingStepKey, 0),
        0,
        OnboardingCompleteStep);

    public static void AdvanceOnboarding(int expectedStep)
    {
        if (OnboardingStep != expectedStep || expectedStep >= OnboardingCompleteStep)
        {
            return;
        }

        PlayerPrefs.SetInt(OnboardingStepKey, expectedStep + 1);
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        foreach (var key in Keys)
        {
            PlayerPrefs.DeleteKey(key);
        }

        PlayerPrefs.Save();
        PrototypeProgression.ResetCache();
        PrototypeGacha.ResetCache();
        PrototypeSession.ResetRuntime();
        Migrate();
    }
}
