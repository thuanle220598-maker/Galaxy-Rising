using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
internal sealed class PrototypeGachaSave
{
    public int version = 2;
    public int tickets = 20;
    public int highRarityPity;
    public int urPity;
    public List<string> ownedCharacters = new List<string>();
    public List<string> history = new List<string>();
}

internal sealed class PrototypeSummonResult
{
    public CombatantDefinition Character;
    public bool IsNew;
    public int Shards;
}

internal static class PrototypeGacha
{
    private const string SaveKey = "Prototype.Gacha";
    private const int HighRarityPityLimit = 20;
    private const int UrPityLimit = 50;
    private static readonly string[] StarterCharacters = { "Nova", "Ion", "Astra", "Lyra", "Brakk" };
    private static PrototypeGachaSave save;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetCache()
    {
        save = null;
    }

    public static int Tickets { get { EnsureLoaded(); return save.tickets; } }
    public static int HighRarityPity { get { EnsureLoaded(); return save.highRarityPity; } }
    public static int UrPity { get { EnsureLoaded(); return save.urPity; } }
    public static int HighRarityPityRemaining => HighRarityPityLimit - HighRarityPity;
    public static int UrPityRemaining => UrPityLimit - UrPity;
    public static int OwnedCount { get { EnsureLoaded(); return save.ownedCharacters.Count; } }
    public static IList<string> History { get { EnsureLoaded(); return save.history; } }

    public static bool IsOwned(string characterName)
    {
        EnsureLoaded();
        return save.ownedCharacters.Contains(characterName);
    }

    public static CombatantDefinition[] GetOwnedRoster(CombatantDefinition[] roster)
    {
        EnsureLoaded();
        var owned = new List<CombatantDefinition>();
        foreach (var character in roster)
        {
            if (IsOwned(character.DisplayName))
            {
                owned.Add(character);
            }
        }
        return owned.ToArray();
    }

    public static void AddTickets(int amount)
    {
        EnsureLoaded();
        save.tickets += Mathf.Max(0, amount);
        Save();
    }

    public static PrototypeSummonResult[] TrySummon(CombatantDefinition[] roster, int count)
    {
        EnsureLoaded();
        if ((count != 1 && count != 10) || roster == null || roster.Length == 0 || save.tickets < count)
        {
            return new PrototypeSummonResult[0];
        }

        save.tickets -= count;
        var results = new PrototypeSummonResult[count];
        for (var index = 0; index < count; index++)
        {
            var rarity = RollRarity();
            var character = PickCharacter(roster, rarity);
            var isNew = !save.ownedCharacters.Contains(character.DisplayName);
            var shards = isNew ? 0 : DuplicateShards(character.Rarity);
            if (isNew)
            {
                save.ownedCharacters.Add(character.DisplayName);
            }
            else
            {
                PrototypeProgression.AddShards(character.DisplayName, shards);
            }

            results[index] = new PrototypeSummonResult
            {
                Character = character,
                IsNew = isNew,
                Shards = shards
            };
            save.history.Insert(
                0,
                $"{character.Rarity} {character.DisplayName} · {(isNew ? "NEW" : "+" + shards + " shards")}");
        }

        if (save.history.Count > 30)
        {
            save.history.RemoveRange(30, save.history.Count - 30);
        }
        Save();
        return results;
    }

    private static PrototypeRarity RollRarity()
    {
        save.highRarityPity++;
        save.urPity++;
        PrototypeRarity rarity;
        if (save.urPity >= UrPityLimit)
        {
            rarity = PrototypeRarity.UR;
        }
        else if (save.highRarityPity >= HighRarityPityLimit)
        {
            rarity = PrototypeRarity.SSR;
        }
        else
        {
            var roll = UnityEngine.Random.value;
            rarity = roll < 0.03f
                ? PrototypeRarity.UR
                : roll < 0.15f
                    ? PrototypeRarity.SSR
                    : roll < 0.45f ? PrototypeRarity.SR : PrototypeRarity.R;
        }

        if (rarity >= PrototypeRarity.SSR)
        {
            save.highRarityPity = 0;
        }
        if (rarity == PrototypeRarity.UR)
        {
            save.urPity = 0;
        }
        return rarity;
    }

    private static CombatantDefinition PickCharacter(CombatantDefinition[] roster, PrototypeRarity rarity)
    {
        var candidates = new List<CombatantDefinition>();
        foreach (var character in roster)
        {
            if (character.Rarity == rarity)
            {
                candidates.Add(character);
            }
        }
        return candidates.Count == 0
            ? roster[UnityEngine.Random.Range(0, roster.Length)]
            : candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private static int DuplicateShards(PrototypeRarity rarity)
    {
        switch (rarity)
        {
            case PrototypeRarity.UR: return 80;
            case PrototypeRarity.SSR: return 40;
            case PrototypeRarity.SR: return 20;
            default: return 10;
        }
    }

    private static void EnsureLoaded()
    {
        if (save != null)
        {
            return;
        }

        try
        {
            var json = PlayerPrefs.GetString(SaveKey, string.Empty);
            save = string.IsNullOrEmpty(json)
                ? new PrototypeGachaSave()
                : JsonUtility.FromJson<PrototypeGachaSave>(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Gacha save was invalid and has been recreated: {exception.Message}");
            save = new PrototypeGachaSave();
        }

        if (save == null || save.ownedCharacters == null || save.history == null)
        {
            save = new PrototypeGachaSave();
        }

        save.version = 2;

        var changed = false;
        foreach (var starter in StarterCharacters)
        {
            if (!save.ownedCharacters.Contains(starter))
            {
                save.ownedCharacters.Add(starter);
                changed = true;
            }
        }
        if (changed)
        {
            Save();
        }
    }

    private static void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save));
        PlayerPrefs.Save();
    }
}

internal sealed class PrototypeGachaScreen : MonoBehaviour
{
    private CombatantDefinition[] roster;
    private Action close;
    private PrototypeSummonResult[] results = new PrototypeSummonResult[0];
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;

    public void Initialize(CombatantDefinition[] availableRoster, Action closeAction)
    {
        roster = availableRoster;
        close = closeAction;
    }

    private void OnGUI()
    {
        if (roster == null)
        {
            return;
        }

        CreateStyles();
        var previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 360f, Screen.height / 640f, 1f));
        GUI.Box(new Rect(0f, 0f, 360f, 640f), string.Empty);
        GUI.Label(new Rect(20f, 14f, 320f, 30f), "GALAXY SUMMON", titleStyle);
        GUI.Label(
            new Rect(20f, 48f, 320f, 24f),
            $"Tickets {PrototypeGacha.Tickets} · Owned {PrototypeGacha.OwnedCount}/{roster.Length}",
            labelStyle);
        GUI.Label(new Rect(20f, 76f, 320f, 22f), "R 55% · SR 30% · SSR 12% · UR 3%", labelStyle);
        GUI.Label(
            new Rect(20f, 102f, 320f, 22f),
            $"SSR pity {PrototypeGacha.HighRarityPityRemaining} · UR pity {PrototypeGacha.UrPityRemaining}",
            labelStyle);

        var previousEnabled = GUI.enabled;
        GUI.enabled = PrototypeGacha.Tickets >= 1;
        if (GUI.Button(new Rect(34f, 136f, 130f, 38f), "SUMMON 1 · 1 TICKET"))
        {
            results = PrototypeGacha.TrySummon(roster, 1);
        }
        GUI.enabled = PrototypeGacha.Tickets >= 10;
        if (GUI.Button(new Rect(196f, 136f, 130f, 38f), "SUMMON 10 · 10 TICKETS"))
        {
            results = PrototypeGacha.TrySummon(roster, 10);
        }
        GUI.enabled = previousEnabled;

        GUI.Box(new Rect(20f, 184f, 320f, 246f), "LATEST SUMMON", labelStyle);
        if (results.Length == 0)
        {
            GUI.Label(new Rect(32f, 222f, 296f, 28f), "Use tickets to recruit heroes.", labelStyle);
        }
        else
        {
            for (var index = 0; index < results.Length; index++)
            {
                var result = results[index];
                GUI.Label(
                    new Rect(32f, 214f + index * 20f, 296f, 18f),
                    $"{result.Character.Rarity} · {result.Character.DisplayName} · {(result.IsNew ? "NEW HERO" : "+" + result.Shards + " SHARDS")}",
                    labelStyle);
            }
        }

        GUI.Box(new Rect(20f, 438f, 320f, 126f), "RECENT HISTORY", labelStyle);
        var historyCount = Mathf.Min(5, PrototypeGacha.History.Count);
        for (var index = 0; index < historyCount; index++)
        {
            GUI.Label(new Rect(32f, 466f + index * 18f, 296f, 17f), PrototypeGacha.History[index], labelStyle);
        }

        if (GUI.Button(new Rect(110f, 590f, 140f, 38f), "BACK TO IDLE"))
        {
            close();
            Destroy(this);
        }
        GUI.matrix = previousMatrix;
    }

    private void CreateStyles()
    {
        if (titleStyle != null)
        {
            return;
        }

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = new Color(1f, 0.78f, 0.2f);
        labelStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 10,
            wordWrap = true
        };
        labelStyle.normal.textColor = Color.white;
    }
}
