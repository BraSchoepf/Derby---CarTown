using UnityEngine;

public enum GameModeCategory { Racing, Demolition }

[CreateAssetMenu(fileName = "NewGameMode", menuName = "Modes/Game Mode")]
public class GameModeSO : ScriptableObject
{
    [Header("Identidad")]
    public string modeName;
    public Sprite icon;
    public GameModeCategory category;

    public DrivingProfileSO drivingProfile;

    [Header("Reglas generales")]
    public bool enableDamage = true;
    public bool requiresCheckpoints = false;
    public int lapsDefault = 1;

    [Header("Equipos")]
    public bool supportsTeams = false;
    public int[] teamSizeOptions;
}