using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PrototypeSkillData
{
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;
    [SerializeField, Min(0f)] private float cooldown;
    [SerializeField, Min(0.1f)] private float actionDuration = 0.4f;
    [SerializeField, Range(0.05f, 0.95f)] private float hitFrame = 0.5f;
    [SerializeField, Min(0f)] private float powerMultiplier = 1f;
    [SerializeField, Min(0)] private int energyGain;

    public string DisplayName => displayName;
    public string Description => description;
    public float Cooldown => cooldown;
    public float ActionDuration => actionDuration;
    public float HitFrame => hitFrame;
    public float PowerMultiplier => powerMultiplier;
    public int EnergyGain => energyGain;

#if UNITY_EDITOR
    public PrototypeSkillData(
        string name,
        string details,
        float skillCooldown,
        float duration,
        float normalizedHitFrame,
        float power,
        int gainedEnergy = 0)
    {
        displayName = name;
        description = details;
        cooldown = skillCooldown;
        actionDuration = duration;
        hitFrame = normalizedHitFrame;
        powerMultiplier = power;
        energyGain = gainedEnergy;
    }
#endif
}

[CreateAssetMenu(fileName = "Skill Set", menuName = "Idle Galaxy Rising/Skill Definition")]
public sealed class SkillDefinition : ScriptableObject
{
    [SerializeField] private PrototypeSkillKit skillKit;
    [SerializeField] private PrototypeSkillData basic;
    [SerializeField] private PrototypeSkillData passive;
    [SerializeField] private PrototypeSkillData passive2;
    [SerializeField] private PrototypeSkillData passive3;
    [SerializeField] private PrototypeSkillData active;
    [SerializeField] private PrototypeSkillData ultimate;

    public PrototypeSkillKit SkillKit => skillKit;
    public PrototypeSkillData Basic => basic;
    public PrototypeSkillData Passive => passive;
    public PrototypeSkillData Passive2 => HasContent(passive2) ? passive2 : null;
    public PrototypeSkillData Passive3 => HasContent(passive3) ? passive3 : null;
    public PrototypeSkillData Active => active;
    public PrototypeSkillData Ultimate => ultimate;

    public string Summary
    {
        get
        {
            var rows = new List<string>
            {
                $"Basic - {basic.DisplayName}: {basic.Description}",
                $"Passive 1 - {passive.DisplayName}: {passive.Description}"
            };
            if (Passive2 != null)
            {
                rows.Add($"Passive 2 - {Passive2.DisplayName}: {Passive2.Description}");
            }
            if (Passive3 != null)
            {
                rows.Add($"Passive 3 - {Passive3.DisplayName}: {Passive3.Description}");
            }
            rows.Add($"Active - {active.DisplayName}: {active.Description}");
            rows.Add($"Ultimate - {ultimate.DisplayName}: {ultimate.Description}");
            return string.Join("\n", rows);
        }
    }

    private static bool HasContent(PrototypeSkillData skill)
    {
        return skill != null && !string.IsNullOrEmpty(skill.DisplayName);
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        PrototypeSkillKit kit,
        PrototypeSkillData basicSkill,
        PrototypeSkillData passiveSkill,
        PrototypeSkillData activeSkill,
        PrototypeSkillData ultimateSkill,
        PrototypeSkillData passiveSkill2 = null,
        PrototypeSkillData passiveSkill3 = null)
    {
        skillKit = kit;
        basic = basicSkill;
        passive = passiveSkill;
        passive2 = passiveSkill2;
        passive3 = passiveSkill3;
        active = activeSkill;
        ultimate = ultimateSkill;
    }
#endif
}

internal static class PrototypeSkillCatalog
{
    private static Dictionary<PrototypeSkillKit, SkillDefinition> definitions;

    public static SkillDefinition Get(PrototypeSkillKit kit)
    {
        EnsureLoaded();
        SkillDefinition definition;
        return definitions.TryGetValue(kit, out definition) ? definition : null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        definitions = null;
    }

    private static void EnsureLoaded()
    {
        if (definitions != null)
        {
            return;
        }

        definitions = new Dictionary<PrototypeSkillKit, SkillDefinition>();
        foreach (var definition in Resources.LoadAll<SkillDefinition>("Skills"))
        {
            definitions[definition.SkillKit] = definition;
        }
    }
}
