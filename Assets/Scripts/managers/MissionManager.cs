using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
class MissionSaveEntry
{
    public string missionId;
    public bool completed;
}

[System.Serializable]
class MissionSaveData
{
    public List<MissionSaveEntry> entries = new();
}

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance;

    public MissionSO[] allMissions;

    Dictionary<string, bool> completedMissions = new();
    const string SaveKey = "MissionProgress";

    public event Action<MissionSO> OnMissionCompleted;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        UnlockRegistry.Load();
        LoadProgress();
    }

    public MissionSO GetActiveMissionFor(GameModeSO mode, MapDataSO map)
    {
        return allMissions.FirstOrDefault(m => m.gameMode == mode && m.map == map && !IsCompleted(m.missionId));
    }

    public bool IsCompleted(string missionId) => completedMissions.TryGetValue(missionId, out bool done) && done;

    public void ReportResult(GameModeSO mode, MapDataSO map, MissionObjectiveType type, float achievedValue)
    {
        var mission = allMissions.FirstOrDefault(m =>
            m.gameMode == mode && m.map == map && m.objectiveType == type && !IsCompleted(m.missionId));

        if (mission == null) return;
        if (!EvaluateObjective(mission, achievedValue)) return;

        completedMissions[mission.missionId] = true;

        foreach (var reward in mission.rewards)
            if (!string.IsNullOrEmpty(reward.rewardId))
                UnlockRegistry.Unlock(reward.rewardId);

        SaveProgress();
        OnMissionCompleted?.Invoke(mission);
    }

    bool EvaluateObjective(MissionSO mission, float value)
    {
        return mission.objectiveType switch
        {
            MissionObjectiveType.DriftScoreThreshold => value >= mission.targetValue,
            MissionObjectiveType.RaceTimeUnder => value <= mission.targetValue,
            MissionObjectiveType.FinishPlacement => value <= mission.targetValue,
            MissionObjectiveType.SurvivalTime => value >= mission.targetValue,
            _ => false
        };
    }

    public float GetModeCompletionPercent(GameModeSO mode)
    {
        var missionsForMode = allMissions.Where(m => m.gameMode == mode).ToList();
        if (missionsForMode.Count == 0) return 0f;

        int completedCount = missionsForMode.Count(m => IsCompleted(m.missionId));
        return (float)completedCount / missionsForMode.Count * 100f;
    }

    void SaveProgress()
    {
        var data = new MissionSaveData();
        foreach (var kvp in completedMissions)
            data.entries.Add(new MissionSaveEntry { missionId = kvp.Key, completed = kvp.Value });

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    void LoadProgress()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json)) return;

        var data = JsonUtility.FromJson<MissionSaveData>(json);
        foreach (var entry in data.entries)
            completedMissions[entry.missionId] = entry.completed;
    }
    public void ResetProgress()
    {
        completedMissions.Clear();
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();

        UnlockRegistry.ResetAll();
    }
}