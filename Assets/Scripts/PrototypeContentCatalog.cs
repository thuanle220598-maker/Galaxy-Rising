using System.Collections.Generic;
using UnityEngine;

internal static class PrototypeContentCatalog
{
    private static Dictionary<int, PrototypeStageDefinition> stages;
    private static Dictionary<PrototypeDungeonType, PrototypeDungeonDefinition> dungeons;

    public static PrototypeStageDefinition GetStage(int stage)
    {
        EnsureLoaded();
        PrototypeStageDefinition definition;
        return stages.TryGetValue(stage, out definition) ? definition : null;
    }

    public static bool IsBossStage(int stage)
    {
        var definition = GetStage(stage);
        return definition != null ? definition.IsBoss : stage % 5 == 0;
    }

    public static float GetStageMultiplier(int stage)
    {
        var definition = GetStage(stage);
        return definition != null ? definition.EnemyMultiplier : 1f + (stage - 1) * 0.08f;
    }

    public static int GetStageGold(int stage)
    {
        var definition = GetStage(stage);
        return definition != null ? definition.GoldReward : 5 + stage * 2;
    }

    public static int GetStageExperience(int stage)
    {
        var definition = GetStage(stage);
        return definition != null ? definition.ExperienceReward : 3 + stage;
    }

    public static int GetStageTickets(int stage)
    {
        var definition = GetStage(stage);
        return definition != null ? definition.TicketReward : stage % 10 == 0 ? 1 : 0;
    }

    public static PrototypeDungeonDefinition GetDungeon(PrototypeDungeonType type)
    {
        EnsureLoaded();
        PrototypeDungeonDefinition definition;
        return dungeons.TryGetValue(type, out definition) ? definition : null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        stages = null;
        dungeons = null;
    }

    private static void EnsureLoaded()
    {
        if (stages != null)
        {
            return;
        }

        stages = new Dictionary<int, PrototypeStageDefinition>();
        foreach (var definition in Resources.LoadAll<PrototypeStageDefinition>("Stages"))
        {
            stages[definition.Stage] = definition;
        }

        dungeons = new Dictionary<PrototypeDungeonType, PrototypeDungeonDefinition>();
        foreach (var definition in Resources.LoadAll<PrototypeDungeonDefinition>("Dungeons"))
        {
            dungeons[definition.Type] = definition;
        }
    }
}
