using UnityEngine;

internal static class PrototypeSaveSystem
{
    private const int CurrentVersion = 2;
    private const string VersionKey = "Prototype.SaveVersion";
    private static readonly string[] Keys =
    {
        VersionKey,
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

        // Version 2 adds persistent squad data; older progression remains compatible.
        PlayerPrefs.SetInt(VersionKey, CurrentVersion);
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
