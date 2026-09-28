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
            case PrototypeCombatClass.Mage: return 4f;
            case PrototypeCombatClass.Archer: return 4.4f;
            case PrototypeCombatClass.Support: return 3.6f;
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

public static class PrototypeCharacterNames
{
    public const string LegacyNova = "Nova";
    public const string FireGodHeavenlyDemon = "Fire God Heavenly Demon";
    public const string LegacyAstra = "Astra";
    public const string Aurelia = "Aurelia";
    public const string LegacyBrakk = "Brakk";
    public const string Kronos = "Kronos";
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
    [SerializeField] private bool sourceFacesLeft;
    [Header("Optional imported sprite frames")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] runFrames;
    [SerializeField] private Sprite[] attackFrames;
    [SerializeField] private Sprite[] skillFrames;
    [SerializeField] private Sprite[] ultimateFrames;
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
    internal bool SourceFacesLeft => sourceFacesLeft;
    public SkillDefinition Skills => skillDefinition != null
        ? skillDefinition
        : PrototypeSkillCatalog.Get(skillKit);

    internal bool HasImportedSprites => idleFrames != null && idleFrames.Length > 0;
    internal bool HasStaticImportedSprite => HasImportedSprites &&
        (runFrames == null || runFrames.Length == 0) &&
        (attackFrames == null || attackFrames.Length == 0) &&
        (skillFrames == null || skillFrames.Length == 0) &&
        (ultimateFrames == null || ultimateFrames.Length == 0) &&
        (hitFrames == null || hitFrames.Length == 0) &&
        (deathFrames == null || deathFrames.Length == 0);
    internal Sprite[] IdleFrames => idleFrames;
    internal Sprite[] RunFrames => runFrames;
    internal Sprite[] AttackFrames => attackFrames;
    internal Sprite[] SkillFrames => skillFrames;
    internal Sprite[] UltimateFrames => ultimateFrames;
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

    public void EditorConfigureStaticSprite(Sprite sprite, bool facesLeft = false)
    {
        sourceFacesLeft = facesLeft;
        idleFrames = sprite == null ? null : new[] { sprite };
        runFrames = null;
        attackFrames = null;
        skillFrames = null;
        ultimateFrames = null;
        hitFrames = null;
        deathFrames = null;
    }

    public void EditorConfigureAnimationFrames(
        Sprite[] idle,
        Sprite[] run,
        Sprite[] attack,
        Sprite[] skill,
        Sprite[] ultimate,
        Sprite[] hit,
        Sprite[] death,
        bool facesLeft = false)
    {
        sourceFacesLeft = facesLeft;
        idleFrames = idle;
        runFrames = run;
        attackFrames = attack;
        skillFrames = skill;
        ultimateFrames = ultimate;
        hitFrames = hit;
        deathFrames = death;
    }
#endif
}
