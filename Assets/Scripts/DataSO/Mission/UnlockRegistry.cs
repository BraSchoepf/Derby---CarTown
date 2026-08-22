using System.Collections.Generic;
using UnityEngine;

public static class UnlockRegistry
{
    static Dictionary<int, HashSet<string>> unlockedByPlayer = new Dictionary<int, HashSet<string>>();

    static string SaveKeyFor(int playerIndex) => $"UnlockedRewards_P{playerIndex}";

    public static void Load()
    {
        unlockedByPlayer.Clear();
        LoadForPlayer(1);
        LoadForPlayer(2);
    }

    static void LoadForPlayer(int playerIndex)
    {
        var set = new HashSet<string>();
        string saved = PlayerPrefs.GetString(SaveKeyFor(playerIndex), "");
        if (!string.IsNullOrEmpty(saved))
        {
            foreach (var id in saved.Split(','))
                if (!string.IsNullOrEmpty(id)) set.Add(id);
        }
        unlockedByPlayer[playerIndex] = set;
    }

    public static void Unlock(string rewardId, int playerIndex)
    {
        if (!unlockedByPlayer.ContainsKey(playerIndex))
            unlockedByPlayer[playerIndex] = new HashSet<string>();
        unlockedByPlayer[playerIndex].Add(rewardId);
        Save(playerIndex);
    }

    public static bool IsUnlocked(string rewardId, int playerIndex)
    {
        return unlockedByPlayer.TryGetValue(playerIndex, out var set) && set.Contains(rewardId);
    }

    static void Save(int playerIndex)
    {
        var set = unlockedByPlayer[playerIndex];
        PlayerPrefs.SetString(SaveKeyFor(playerIndex), string.Join(",", set));
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        unlockedByPlayer.Clear();
        PlayerPrefs.DeleteKey(SaveKeyFor(1));
        PlayerPrefs.DeleteKey(SaveKeyFor(2));
        PlayerPrefs.Save();
    }
}