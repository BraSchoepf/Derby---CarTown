using UnityEngine;

public enum MissionObjectiveType
{
    DriftScoreThreshold,
    RaceTimeUnder,
    FinishPlacement,
    SurvivalTime
}

[CreateAssetMenu(fileName = "NewMission", menuName = "Missions/Mission")]
public class MissionSO : ScriptableObject
{
    public string missionId; // único — usalo para persistencia, no cambiar una vez publicado
    public string missionName;
    [TextArea] public string description;

    public GameModeSO gameMode;
    public MapDataSO map;

    public MissionObjectiveType objectiveType;
    public float targetValue;

    public RewardSO[] rewards;
}