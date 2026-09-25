using UnityEngine;

[CreateAssetMenu(fileName = "Dungeon", menuName = "Idle Galaxy Rising/Dungeon Definition")]
public sealed class PrototypeDungeonDefinition : ScriptableObject
{
    [SerializeField] private PrototypeDungeonType type;
    [SerializeField, Min(1f)] private float baseReward = 100f;
    [SerializeField, Min(0f)] private float rewardPerLevel = 100f;
    [SerializeField, Min(1f)] private float timeLimit = 60f;
    [SerializeField, Min(0f)] private float enemyGrowthPerLevel = 0.18f;

    public PrototypeDungeonType Type => type;
    public float TimeLimit => timeLimit;

    public int GetReward(int level)
    {
        return Mathf.RoundToInt(baseReward + Mathf.Max(0, level - 1) * rewardPerLevel);
    }

    public float GetEnemyMultiplier(int level)
    {
        return 1f + Mathf.Max(0, level - 1) * enemyGrowthPerLevel;
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        PrototypeDungeonType dungeonType,
        float initialReward,
        float levelReward,
        float seconds,
        float enemyGrowth)
    {
        type = dungeonType;
        baseReward = initialReward;
        rewardPerLevel = levelReward;
        timeLimit = seconds;
        enemyGrowthPerLevel = enemyGrowth;
    }
#endif
}
