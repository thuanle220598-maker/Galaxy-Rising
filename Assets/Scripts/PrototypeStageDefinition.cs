using UnityEngine;

[CreateAssetMenu(fileName = "Stage", menuName = "Idle Galaxy Rising/Stage Definition")]
public sealed class PrototypeStageDefinition : ScriptableObject
{
    [SerializeField, Min(1)] private int stage = 1;
    [SerializeField] private bool boss;
    [SerializeField, Min(1f)] private float enemyMultiplier = 1f;
    [SerializeField, Min(0)] private int goldReward = 7;
    [SerializeField, Min(0)] private int experienceReward = 4;
    [SerializeField, Min(0)] private int ticketReward;

    public int Stage => stage;
    public bool IsBoss => boss;
    public float EnemyMultiplier => enemyMultiplier;
    public int GoldReward => goldReward;
    public int ExperienceReward => experienceReward;
    public int TicketReward => ticketReward;

#if UNITY_EDITOR
    public void EditorConfigure(int number, bool isBoss, float multiplier, int gold, int experience, int tickets)
    {
        stage = number;
        boss = isBoss;
        enemyMultiplier = multiplier;
        goldReward = gold;
        experienceReward = experience;
        ticketReward = tickets;
    }
#endif
}
