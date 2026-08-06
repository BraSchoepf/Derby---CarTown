using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class UnlockRegistry
{
    static HashSet<string> unlockedRewardIds = new HashSet<string>();
    const string SaveKey = "UnlockedRewards";

    public static void Load()
    {
        unlockedRewardIds.Clear();
        string saved = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(saved)) return;

        foreach (var id in saved.Split(','))
            if (!string.IsNullOrEmpty(id)) unlockedRewardIds.Add(id);
    }

    public static void Unlock(string rewardId)
    {
        unlockedRewardIds.Add(rewardId);
        Save();
    }

    public static bool IsUnlocked(string rewardId) => unlockedRewardIds.Contains(rewardId);

    static void Save()
    {
        PlayerPrefs.SetString(SaveKey, string.Join(",", unlockedRewardIds));
        PlayerPrefs.Save();
    }

    // Nuevo: reset completo
    public static void ResetAll()
    {
        unlockedRewardIds.Clear();
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }
}