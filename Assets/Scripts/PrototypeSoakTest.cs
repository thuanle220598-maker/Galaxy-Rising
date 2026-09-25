using System;
using System.Collections;
using UnityEngine;

internal sealed class PrototypeSoakTest : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartWhenRequested()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-prototypeSoak") < 0)
        {
            return;
        }

        var gameObject = new GameObject("Prototype Soak Test");
        DontDestroyOnLoad(gameObject);
        gameObject.AddComponent<PrototypeSoakTest>().StartCoroutine(Run());
    }

    private static IEnumerator Run()
    {
        yield return null;
        var startMemory = GC.GetTotalMemory(true);
        for (var iteration = 0; iteration < 100; iteration++)
        {
            var battle = FindFirstObjectByType<PrototypeBattle>();
            if (battle == null)
            {
                Debug.LogError($"SOAK FAILED: battle missing at iteration {iteration}.");
                yield break;
            }

            CombatPrototype.RestartIdleBattle(battle.gameObject, iteration % 2 == 1);
            yield return null;
            if (FindObjectsByType<PrototypeBattle>(FindObjectsSortMode.None).Length != 1)
            {
                Debug.LogError($"SOAK FAILED: invalid battle count at iteration {iteration}.");
                yield break;
            }
        }

        var growth = GC.GetTotalMemory(true) - startMemory;
        Debug.Log($"SOAK PASSED: 100 battle/farm transitions, managed memory growth {growth / 1024f:0.0} KB.");
    }
}
