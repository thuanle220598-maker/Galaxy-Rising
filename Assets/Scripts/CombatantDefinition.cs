using UnityEngine;

public enum PrototypeSkillKit
{
    None,
    Nova,
    Ion,
    Astra,
    Krag,
    Vex,
    Rook,
    Lyra,
    Brakk,
    Hex,
    Nyx,
    Mira,
    Drake
}

public enum PrototypeSpecies
{
    Unknown,
    Human,
    Dragon,
    Cosmic,
    Machine,
    Ocean,
    Fantasy
}

public enum PrototypeCombatClass
{
    Unknown,
    Tanker,
    Fighter,
    Assassin,
    Mage,
    Archer,
    Support
}

public enum PrototypeFormationRow
{
    Front,
    Middle,
    Back
}

public enum PrototypeRarity
{
    R,
    SR,
    SSR,
    UR
}

public static class PrototypeCombatClassRules
{
    public static float GetAttackRange(PrototypeCombatClass combatClass)
    {
        switch (combatClass)
        {
            case PrototypeCombatClass.Tanker: return 1.1f;
            case PrototypeCombatClass.Fighter: return 1.35f;
            case PrototypeCombatClass.Assassin: return 1f;
            case PrototypeCombatClass.Mage: return 3f;
            case PrototypeCombatClass.Archer: return 3.4f;
            case PrototypeCombatClass.Support: return 2.8f;
            default: return 1.2f;
        }
    }

    public static PrototypeFormationRow GetFormationRow(PrototypeCombatClass combatClass)
    {
        switch (combatClass)
        {
            case PrototypeCombatClass.Tanker:
            case PrototypeCombatClass.Fighter:
                return PrototypeFormationRow.Front;
            case PrototypeCombatClass.Assassin:
            case PrototypeCombatClass.Mage:
                return PrototypeFormationRow.Middle;
            default:
                return PrototypeFormationRow.Back;
        }
    }
}

[CreateAssetMenu(fileName = "Combatant", menuName = "Idle Galaxy Rising/Combatant Definition")]
public sealed class CombatantDefinition : ScriptableObject
{
    [SerializeField] private string displayName = "Combatant";
    [SerializeField] private PrototypeSpecies species;
    [SerializeField] private PrototypeCombatClass combatClass;
    [SerializeField] private PrototypeRarity rarity;
    [SerializeField] private Color bodyColor = Color.white;
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField, Min(1)] private int attack = 20;
    [SerializeField, Min(0)] private int defense = 5;
    [SerializeField, Min(0.1f)] private float moveSpeed = 2f;
    [SerializeField, Min(0.1f)] private float attackInterval = 1f;
    [SerializeField] private PrototypeSkillKit skillKit;
    [SerializeField] private int formationOrder;
    [SerializeField] private SkillDefinition skillDefinition;
    [Header("Optional imported sprite frames")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] runFrames;
    [SerializeField] private Sprite[] attackFrames;
    [SerializeField] private Sprite[] skillFrames;
    [SerializeField] private Sprite[] hitFrames;
    [SerializeField] private Sprite[] deathFrames;

    public string DisplayName => displayName;
    public PrototypeSpecies Species => species;
    public PrototypeCombatClass CombatClass => combatClass;
    public PrototypeRarity Rarity => rarity;
    public Color BodyColor => bodyColor;
    public int MaxHealth => maxHealth;
    public int Attack => attack;
    public int Defense => defense;
    public float MoveSpeed => moveSpeed;
    public float AttackRange => PrototypeCombatClassRules.GetAttackRange(combatClass);
    public PrototypeFormationRow FormationRow => PrototypeCombatClassRules.GetFormationRow(combatClass);
    public float AttackInterval => attackInterval;
    public PrototypeSkillKit SkillKit => skillKit;
    public int FormationOrder => formationOrder;
    public SkillDefinition Skills => skillDefinition != null
        ? skillDefinition
        : PrototypeSkillCatalog.Get(skillKit);

    internal bool HasImportedSprites => idleFrames != null && idleFrames.Length > 0;
    internal Sprite[] IdleFrames => idleFrames;
    internal Sprite[] RunFrames => runFrames;
    internal Sprite[] AttackFrames => attackFrames;
    internal Sprite[] SkillFrames => skillFrames;
    internal Sprite[] HitFrames => hitFrames;
    internal Sprite[] DeathFrames => deathFrames;

#if UNITY_EDITOR
    public void EditorConfigure(
        string characterName,
        PrototypeSpecies characterSpecies,
        PrototypeCombatClass characterClass,
        PrototypeRarity characterRarity,
        Color color,
        int health,
        int attackPower,
        int characterDefense,
        float speed,
        float interval,
        PrototypeSkillKit kit,
        int order)
    {
        displayName = characterName;
        species = characterSpecies;
        combatClass = characterClass;
        rarity = characterRarity;
        bodyColor = color;
        maxHealth = health;
        attack = attackPower;
        defense = characterDefense;
        moveSpeed = speed;
        attackInterval = interval;
        skillKit = kit;
        formationOrder = order;
    }
#endif
}
